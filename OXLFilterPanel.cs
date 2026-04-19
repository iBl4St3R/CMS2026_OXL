// OXLFilterPanel.cs  ─────────────────────────────────────────────────────────
// Animated slide-down filter strip for the OXL listing page.
//
// STEP 1 — Self-contained UI + state.  No wiring to the listing system yet.
// STEP 2 — Subscribe to OnFiltersApplied → call OXLPanel.RefreshListings().
// ─────────────────────────────────────────────────────────────────────────────

using MelonLoader;
using System;
using System.Collections;
using UnityEngine;
using CMS2026UITKFramework;

namespace CMS2026_OXL
{
    public sealed class OXLFilterPanel
    {
        // ══════════════════════════════════════════════════════════════════════
        //  FILTER CRITERIA — immutable snapshot committed on Apply
        // ══════════════════════════════════════════════════════════════════════

        public sealed class FilterCriteria
        {
            public readonly string Make;       // "" = All
            public readonly int MinYear;    // 0  = Any
            public readonly int MaxYear;
            public readonly int MinPrice;   // 0  = Any
            public readonly int MaxPrice;
            public readonly int CondTier;   // 0=All 1=Poor(<30%) 2=Fair 3=Good(>70%)
            public readonly int MinRating;  // 0=Any 3=3★+ 4=4★+ 5=5★

            public FilterCriteria() { }

            public FilterCriteria(string make, int yF, int yT,
                                  int pL, int pH, int cond, int rat)
            {
                Make = make; MinYear = yF; MaxYear = yT;
                MinPrice = pL; MaxPrice = pH;
                CondTier = cond; MinRating = rat;
            }

            public bool IsEmpty =>
                string.IsNullOrEmpty(Make) &&
                MinYear == 0 && MaxYear == 0 &&
                MinPrice == 0 && MaxPrice == 0 &&
                CondTier == 0 && MinRating == 0;

            public int ActiveCount
            {
                get
                {
                    int n = 0;
                    if (!string.IsNullOrEmpty(Make)) n++;
                    if (MinYear > 0 || MaxYear > 0) n++;
                    if (MinPrice > 0 || MaxPrice > 0) n++;
                    if (CondTier > 0) n++;
                    if (MinRating > 0) n++;
                    return n;
                }
            }
        }

        // ── Public interface ──────────────────────────────────────────────────
        public FilterCriteria Current { get; private set; } = new FilterCriteria();
        public bool IsOpen => _isOpen;

        /// <summary>
        /// Raised when Apply is clicked.
        /// STEP 2: wire to ApplyFilters() + RefreshListings() in OXLPanel.
        /// </summary>
        public Action OnFiltersApplied;

        // ══════════════════════════════════════════════════════════════════════
        //  LAYOUT
        // ══════════════════════════════════════════════════════════════════════

        private const float PW = 1456f;  // matches OXLPanel.PanelW
        private const float BarH = 42f;    // toggle bar — always visible
        private const float BodyH = 126f;   // content area when open
        private const float TotalH = BarH + BodyH;  // = 168f
        private const float AnimSec = 0.20f;

        // Content Y positions (relative to clipper top)
        // [BarH=42] [pad=10] [lbl=12] [gap=4] [ctrl=30] [gap=14] [lbl=12] [gap=4] [ctrl=30] [pad=10]
        private const float R1LblY = BarH + 10f;           // 52
        private const float R1CtrlY = R1LblY + 12f + 4f;  // 68
        private const float R2LblY = R1CtrlY + 30f + 14f; // 112
        private const float R2CtrlY = R2LblY + 12f + 4f;  // 128

        // ══════════════════════════════════════════════════════════════════════
        //  PALETTE  (mirrors OXLPanel)
        // ══════════════════════════════════════════════════════════════════════

        static readonly Color BgStrip = new Color(0.036f, 0.060f, 0.106f, 1f);
        static readonly Color BgInput = new Color(0.040f, 0.068f, 0.118f, 1f);
        static readonly Color Grn = new Color(0.220f, 0.592f, 0.341f, 1f);
        static readonly Color Bdr = new Color(0.220f, 0.592f, 0.341f, 0.40f);
        static readonly Color BtnNorm = new Color(0.075f, 0.110f, 0.180f, 1f);
        static readonly Color BtnHov = new Color(0.110f, 0.170f, 0.260f, 1f);
        static readonly Color BtnActv = new Color(0.055f, 0.140f, 0.080f, 1f);
        static readonly Color GrayTxt = new Color(0.420f, 0.480f, 0.500f, 1f);
        static readonly Color LabelClr = new Color(0.380f, 0.550f, 0.420f, 0.80f);
        static readonly Color BadgeClr = new Color(0.550f, 0.800f, 0.550f, 1f);
        static readonly Color CondPoor = new Color(0.90f, 0.28f, 0.18f, 1f);
        static readonly Color CondFair = new Color(0.85f, 0.72f, 0.20f, 1f);
        static readonly Color CondGood = new Color(0.22f, 0.75f, 0.40f, 1f);

        // ══════════════════════════════════════════════════════════════════════
        //  PRESET TABLES
        // ══════════════════════════════════════════════════════════════════════

        static readonly string[] Makes = { "All", "DNB", "Katagiri", "Luxor", "Mayen", "Salem" };
        static readonly int[] YearPre = { 0, 1991, 1993, 1995, 1997, 2000, 2002, 2005, 2008, 2010, 2012, 2015 };
        static readonly int[] PricePre = { 0, 3000, 5000, 8000, 10000, 12000, 15000, 18000, 20000, 25000, 30000 };
        static readonly string[] CondLabels = { "All", "Poor", "Fair", "Good" };
        static readonly Color[] CondColors = { GrayTxt, CondPoor, CondFair, CondGood };
        static readonly string[] StarLabels = { "Any", "3★+", "4★+", "5★" };
        static readonly int[] StarVals = { 0, 3, 4, 5 };

        // ══════════════════════════════════════════════════════════════════════
        //  PRIVATE STATE
        // ══════════════════════════════════════════════════════════════════════

        private UIPanel _owner;
        private IntPtr _clipperPtr;
        private bool _isOpen = false;
        private bool _animBusy = false;

        // Draft values (what the user is editing, before Apply)
        private int _drMake = 0;  // index → Makes[]
        private int _drYearF = 0;  // index → YearPre[]
        private int _drYearT = 0;
        private int _drPriceL = 0;  // index → PricePre[]
        private int _drPriceH = 0;
        private int _drCond = 0;  // index → CondLabels[]
        private int _drRat = 0;  // index → StarLabels[]

        // Updatable label handles
        private UILabelHandle _arrowLbl;
        private UILabelHandle _badgeLbl;
        private UILabelHandle _makeVal;
        private UILabelHandle _yFromVal;
        private UILabelHandle _yToVal;
        private UILabelHandle _pMinVal;
        private UILabelHandle _pMaxVal;

        // Button pointers for background refresh
        private IntPtr[] _condPtrs = new IntPtr[4];
        private IntPtr[] _ratPtrs = new IntPtr[4];

        // ══════════════════════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the filter strip inside <paramref name="parentVE"/> at y = <paramref name="yTop"/>.
        /// Returns the height it occupies when <b>collapsed</b> (= BarH = 42f).
        /// </summary>
        public float Build(UIPanel owner, object parentVE, float yTop)
        {
            _owner = owner;

            // Outer clipper — its Height is what gets animated ─────────────────
            var clip = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(clip);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, yTop);
                S.Width(s, PW); S.Height(s, BarH); // start collapsed
                S.Overflow(s, "Hidden");
                S.BgColor(s, BgStrip);
            }
            UIRuntime.AddChild(parentVE, clip);
            _clipperPtr = UIRuntime.GetPtr(clip);

            // Bottom border (visible only when fully expanded)
            AddHLine(clip, TotalH - 1f);

            BuildToggleBar(clip);
            BuildContent(clip);

            return BarH;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TOGGLE BAR
        // ══════════════════════════════════════════════════════════════════════

        private void BuildToggleBar(object parent)
        {
            const float ctrlH = 26f;
            float cy = (BarH - ctrlH) * 0.5f;

            // Top separator
            AddHLine(parent, 0f);

            // Click zone (left 240px of the bar toggles the panel)
            var zone = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(zone);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, 0f);
                S.Width(s, 240f); S.Height(s, BarH);
            }
            UIRuntime.AddChild(parent, zone);
            var zonePtr = UIRuntime.GetPtr(zone);
            _owner.WireHover(zonePtr, new Color(0, 0, 0, 0), A(BtnNorm, 0.50f), BtnHov);
            _owner.WireClick(zonePtr, () => Toggle());

            // ▼ / ▲ arrow
            _arrowLbl = _owner.AddLabelToContainer(zone, "▼", 16f, 0f, 22f, BarH, Grn);
            _arrowLbl.SetFontSize(11);
            MidLeft(_arrowLbl);

            // "FILTRY" title
            var title = _owner.AddLabelToContainer(zone, "FILTERS", 38f, 0f, 90f, BarH, Grn);
            title.SetFontSize(13);
            MidLeft(title);

            // Active-filter badge — appears after Apply, fades in naturally
            _badgeLbl = _owner.AddLabelToContainer(parent, "", 240f, 0f, 700f, BarH, BadgeClr);
            _badgeLbl.SetFontSize(11);
            MidLeft(_badgeLbl);

            // Reset (right edge)
            var resetPtr = _owner.AddButtonToContainer(
                parent, "✕  Reset", PW - 16f - 120f, cy, 120f, ctrlH,
                BtnNorm, () => DoReset());
            _owner.WireHover(resetPtr, BtnNorm, BtnHov, new Color(0.80f, 0.20f, 0.10f, 0.50f));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FILTER CONTENT
        // ══════════════════════════════════════════════════════════════════════

        private void BuildContent(object parent)
        {
            float cx = 16f;

            // ── ROW 1: Make | Year From | Year To | Price Min | Price Max ──────

            SecLbl(parent, "MAKE", cx, R1LblY);
            BuildCycle(parent, cx, R1CtrlY, 180f, 30f,
                Makes,
                () => _drMake, i => _drMake = i,
                out _makeVal);
            cx += 180f + 24f;

            SecLbl(parent, "YEAR FROM", cx, R1LblY);
            BuildCycle(parent, cx, R1CtrlY, 140f, 30f,
                ToStr(YearPre, v => v == 0 ? "Any" : v.ToString()),
                () => _drYearF, i => _drYearF = i,
                out _yFromVal);
            cx += 140f + 8f;

            SecLbl(parent, "TO", cx, R1LblY);
            BuildCycle(parent, cx, R1CtrlY, 140f, 30f,
                ToStr(YearPre, v => v == 0 ? "Any" : v.ToString()),
                () => _drYearT, i => _drYearT = i,
                out _yToVal);
            cx += 140f + 24f;

            SecLbl(parent, "PRICE MIN", cx, R1LblY);
            BuildCycle(parent, cx, R1CtrlY, 160f, 30f,
                ToStr(PricePre, v => v == 0 ? "Any" : $"${v / 1000}k"),
                () => _drPriceL, i => _drPriceL = i,
                out _pMinVal);
            cx += 160f + 8f;

            SecLbl(parent, "MAX", cx, R1LblY);
            BuildCycle(parent, cx, R1CtrlY, 160f, 30f,
                ToStr(PricePre, v => v == 0 ? "Any" : $"${v / 1000}k"),
                () => _drPriceH, i => _drPriceH = i,
                out _pMaxVal);

            // ── ROW 2: Condition | Rating | Apply ─────────────────────────────
            cx = 16f;

            SecLbl(parent, "CONDITION", cx, R2LblY);
            for (int i = 0; i < CondLabels.Length; i++)
            {
                int idx = i;
                bool sel = (_drCond == i);
                _condPtrs[i] = _owner.AddButtonToContainer(
                    parent, CondLabels[i],
                    cx + i * 72f, R2CtrlY, 68f, 30f,
                    sel ? BtnActv : BtnNorm,
                    () => { _drCond = idx; RefreshCondBtns(); });
                _owner.WireHover(_condPtrs[i],
                    sel ? BtnActv : BtnNorm, BtnHov, A(CondColors[i], 0.40f));
            }
            cx += 4 * 72f + 32f;

            SecLbl(parent, "SELLER RATING", cx, R2LblY);
            for (int i = 0; i < StarLabels.Length; i++)
            {
                int idx = i;
                bool sel = (_drRat == i);
                _ratPtrs[i] = _owner.AddButtonToContainer(
                    parent, StarLabels[i],
                    cx + i * 68f, R2CtrlY, 64f, 30f,
                    sel ? BtnActv : BtnNorm,
                    () => { _drRat = idx; RefreshRatBtns(); });
                _owner.WireHover(_ratPtrs[i],
                    sel ? BtnActv : BtnNorm, BtnHov, A(Grn, 0.40f));
            }

            // Apply (right-aligned)
            var applyPtr = _owner.AddButtonToContainer(
                parent, "✔  Apply Filters",
                PW - 16f - 170f, R2CtrlY, 170f, 30f,
                Grn, () => DoApply());
            _owner.WireHover(applyPtr, Grn,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CYCLE CONTROL  [‹] [value label] [›]
        // ══════════════════════════════════════════════════════════════════════

        private void BuildCycle(
            object parent, float x, float y, float w, float h,
            string[] options,
            Func<int> getIdx, Action<int> setIdx,
            out UILabelHandle valLbl)
        {
            const float Aw = 26f;
            float lw = w - Aw * 2f;

            // Array trick: lambdas capture the ref before the label is created
            UILabelHandle[] lRef = new UILabelHandle[1];

            void Refresh()
            {
                lRef[0]?.SetText(options[getIdx()]);
                lRef[0]?.SetColor(getIdx() == 0 ? GrayTxt : Color.white);
            }

            // ‹ previous
            var prevPtr = _owner.AddButtonToContainer(parent, "‹", x, y, Aw, h, BtnNorm, () =>
            {
                setIdx((getIdx() - 1 + options.Length) % options.Length);
                Refresh();
            });
            _owner.WireHover(prevPtr, BtnNorm, BtnHov, Bdr);

            // Value label
            var lbl = _owner.AddLabelToContainer(
                parent, options[getIdx()],
                x + Aw, y, lw, h,
                getIdx() == 0 ? GrayTxt : Color.white);
            lbl.SetFontSize(13);
            S.BgColor(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())), BgInput);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())), TextAnchor.MiddleCenter);
            lRef[0] = lbl;
            valLbl = lbl;

            // › next
            var nextPtr = _owner.AddButtonToContainer(parent, "›", x + Aw + lw, y, Aw, h, BtnNorm, () =>
            {
                setIdx((getIdx() + 1) % options.Length);
                Refresh();
            });
            _owner.WireHover(nextPtr, BtnNorm, BtnHov, Bdr);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ANIMATION
        // ══════════════════════════════════════════════════════════════════════

        public void Toggle()
        {
            if (_animBusy) return;
            MelonCoroutines.Start(AnimPanel(!_isOpen));
        }

        private IEnumerator AnimPanel(bool opening)
        {
            _animBusy = true;
            float from = _isOpen ? TotalH : BarH;
            float to = opening ? TotalH : BarH;
            float t = 0f;

            _arrowLbl?.SetText(opening ? "▲" : "▼");

            while (t < AnimSec)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / AnimSec));
                S.Height(UIRuntime.GetStyle(UIRuntime.WrapVE(_clipperPtr)),
                         Mathf.Lerp(from, to, p));
                yield return null;
            }

            // Snap to final value
            S.Height(UIRuntime.GetStyle(UIRuntime.WrapVE(_clipperPtr)), to);
            _isOpen = opening;
            _animBusy = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIONS
        // ══════════════════════════════════════════════════════════════════════

        private void DoApply()
        {
            // Read from draft, swap if user set From > To
            int yF = YearPre[_drYearF];
            int yT = YearPre[_drYearT];
            if (yF > 0 && yT > 0 && yF > yT) (yF, yT) = (yT, yF);

            int pL = PricePre[_drPriceL];
            int pH = PricePre[_drPriceH];
            if (pL > 0 && pH > 0 && pL > pH) (pL, pH) = (pH, pL);

            Current = new FilterCriteria(
                _drMake == 0 ? "" : Makes[_drMake],
                yF, yT, pL, pH,
                _drCond,
                StarVals[_drRat]);

            UpdateBadge();
            OnFiltersApplied?.Invoke();  // ← Step 2 wiring point

            // Auto-collapse after confirming
            if (_isOpen && !_animBusy)
                MelonCoroutines.Start(AnimPanel(false));
        }

        private void DoReset()
        {
            _drMake = _drYearF = _drYearT = _drPriceL = _drPriceH = _drCond = _drRat = 0;

            void Grey(UILabelHandle l, string text) { l?.SetText(text); l?.SetColor(GrayTxt); }
            Grey(_makeVal, Makes[0]);
            Grey(_yFromVal, "Any");
            Grey(_yToVal, "Any");
            Grey(_pMinVal, "Any");
            Grey(_pMaxVal, "Any");

            RefreshCondBtns();
            RefreshRatBtns();
            DoApply();
        }

        // ── Button group refresh (re-wires hover so bg doesn't revert) ─────────

        private void RefreshCondBtns()
        {
            for (int i = 0; i < _condPtrs.Length; i++)
            {
                if (_condPtrs[i] == IntPtr.Zero) continue;
                bool sel = (_drCond == i);
                Color bg = sel ? BtnActv : BtnNorm;
                var s = UIRuntime.GetStyle(UIRuntime.WrapVE(_condPtrs[i]));
                S.BgColor(s, bg);
                S.BorderWidth(s, sel ? 1f : 0f);
                S.BorderColor(s, sel ? CondColors[i] : new Color(0, 0, 0, 0));
                _owner.WireHover(_condPtrs[i], bg, BtnHov, A(CondColors[i], 0.40f));
            }
        }

        private void RefreshRatBtns()
        {
            for (int i = 0; i < _ratPtrs.Length; i++)
            {
                if (_ratPtrs[i] == IntPtr.Zero) continue;
                bool sel = (_drRat == i);
                Color bg = sel ? BtnActv : BtnNorm;
                var s = UIRuntime.GetStyle(UIRuntime.WrapVE(_ratPtrs[i]));
                S.BgColor(s, bg);
                S.BorderWidth(s, sel ? 1f : 0f);
                S.BorderColor(s, sel ? Grn : new Color(0, 0, 0, 0));
                _owner.WireHover(_ratPtrs[i], bg, BtnHov, A(Grn, 0.40f));
            }
        }

        private void UpdateBadge()
        {
            int n = Current.ActiveCount;
            if (n == 0) { _badgeLbl?.SetText(""); return; }
            string form = n == 1 ? "filter active" : "filters active";
            _badgeLbl?.SetText($"  {n} {form}");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  MICRO-HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private void SecLbl(object parent, string text, float x, float y)
        {
            var l = _owner.AddLabelToContainer(parent, text, x, y, 200f, 12f, LabelClr);
            l.SetFontSize(9);
        }

        private static void MidLeft(UILabelHandle lbl) =>
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())), TextAnchor.MiddleLeft);

        private void AddHLine(object parent, float yPos)
        {
            var v = UIRuntime.NewVE();
            var s = UIRuntime.GetStyle(v);
            S.Position(s, "Absolute");
            S.Left(s, 0f); S.Top(s, yPos);
            S.Width(s, PW); S.Height(s, 1f);
            S.BgColor(s, Bdr);
            UIRuntime.AddChild(parent, v);
        }

        /// <summary>New Color with replaced alpha — safe alternative to C# 'with' on structs.</summary>
        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static string[] ToStr(int[] arr, Func<int, string> fmt)
        {
            var r = new string[arr.Length];
            for (int i = 0; i < arr.Length; i++) r[i] = fmt(arr[i]);
            return r;
        }
    }
}