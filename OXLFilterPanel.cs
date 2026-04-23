// OXLFilterPanel.cs — rozbudowany panel filtrów + sortowania.
//
// Zmiany względem v1:
// • Dropdowns zamiast cycle-buttonów dla list wartości
// • Opcje budowane dynamicznie z ActiveListings + CarSpecLoader (OptionsProvider)
// • Nowe filtry: Engine category, Drivetrain, Rarity, Color, Tire size,
//                Power min, Torque min, Weight max, Mileage max
// • Tekstowe inputy dla zakresów numerycznych
// • Sortowanie (SortDropdown) po prawej stronie
// • Wysokość ~306 px (42 bar + 264 body)
//
// UI rysowane w clipperze (overflow:Hidden) który animuje wysokość na Toggle.
// Popupy dropdownów są overlayami panelu (renderują nad wszystkim, nie są
// klipowane przez clipper). BuildDropdown() zna globalYBase filter panelu
// w przestrzeni UIPanel, więc popup pozycjonuje się globalnie poprawnie.

using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CMS2026UITKFramework;

namespace CMS2026_OXL
{
    public sealed class OXLFilterPanel
    {
        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC DTO
        // ══════════════════════════════════════════════════════════════════════

        public enum ListingSort
        {
            TimeLeft,       // domyślny: najmniej czasu pierwsze
            PriceAsc,
            PriceDesc,
            YearDesc,
            YearAsc,
            MileageAsc,
            RatingDesc,
        }

        public sealed class FilterCriteria
        {
            public readonly string Make;
            public readonly string EngineCategory;
            public readonly string Drivetrain;
            public readonly string Rarity;
            public readonly string Color;
            public readonly string TireSize;

            public readonly int MinYear, MaxYear;
            public readonly int MinPrice, MaxPrice;
            public readonly int MaxMileage;
            public readonly int MinPower;
            public readonly int MinTorque;
            public readonly int MaxWeight;

            public readonly int CondTier;   // 0=All 1=Poor 2=Fair 3=Good
            public readonly int MinRating;
            public readonly ListingSort Sort;

            public FilterCriteria() { Sort = ListingSort.TimeLeft; }

            public FilterCriteria(
                string make, string engCat, string drv, string rarity,
                string color, string tire,
                int yF, int yT, int pL, int pH, int mMax,
                int pwMin, int tqMin, int wtMax,
                int cond, int rat, ListingSort sort)
            {
                Make = make; EngineCategory = engCat; Drivetrain = drv;
                Rarity = rarity; Color = color; TireSize = tire;
                MinYear = yF; MaxYear = yT;
                MinPrice = pL; MaxPrice = pH;
                MaxMileage = mMax;
                MinPower = pwMin; MinTorque = tqMin; MaxWeight = wtMax;
                CondTier = cond; MinRating = rat;
                Sort = sort;
            }

            public bool IsEmpty =>
                string.IsNullOrEmpty(Make) &&
                string.IsNullOrEmpty(EngineCategory) &&
                string.IsNullOrEmpty(Drivetrain) &&
                string.IsNullOrEmpty(Rarity) &&
                string.IsNullOrEmpty(Color) &&
                string.IsNullOrEmpty(TireSize) &&
                MinYear == 0 && MaxYear == 0 &&
                MinPrice == 0 && MaxPrice == 0 &&
                MaxMileage == 0 &&
                MinPower == 0 && MinTorque == 0 && MaxWeight == 0 &&
                CondTier == 0 && MinRating == 0;

            public int ActiveCount
            {
                get
                {
                    int n = 0;
                    if (!string.IsNullOrEmpty(Make)) n++;
                    if (!string.IsNullOrEmpty(EngineCategory)) n++;
                    if (!string.IsNullOrEmpty(Drivetrain)) n++;
                    if (!string.IsNullOrEmpty(Rarity)) n++;
                    if (!string.IsNullOrEmpty(Color)) n++;
                    if (!string.IsNullOrEmpty(TireSize)) n++;
                    if (MinYear > 0 || MaxYear > 0) n++;
                    if (MinPrice > 0 || MaxPrice > 0) n++;
                    if (MaxMileage > 0) n++;
                    if (MinPower > 0) n++;
                    if (MinTorque > 0) n++;
                    if (MaxWeight > 0) n++;
                    if (CondTier > 0) n++;
                    if (MinRating > 0) n++;
                    return n;
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────
        public FilterCriteria Current { get; private set; } = new FilterCriteria();
        public bool IsOpen => _isOpen;
        public Action OnFiltersApplied;
        /// <summary>OXLPanel dostarcza — wywoływany przy każdym Toggle(opening).</summary>
        public Func<FilterOptions> OptionsProvider;

        // ══════════════════════════════════════════════════════════════════════
        //  LAYOUT
        // ══════════════════════════════════════════════════════════════════════

        private const float PW = 1456f;
        private const float BarH = 42f;
        private const float BodyH = 264f;
        private const float TotalH = BarH + BodyH;
        private const float AnimSec = 0.22f;

        // Rows (y relative to clipper top)
        private const float R1Lbl = BarH + 12f;    //  54
        private const float R1Ct = R1Lbl + 16f;   //  70
        private const float R2Lbl = R1Ct + 30f + 14f;   // 114
        private const float R2Ct = R2Lbl + 16f;        // 130
        private const float R3Lbl = R2Ct + 30f + 14f;   // 174
        private const float R3Ct = R3Lbl + 16f;        // 190
        private const float R4Ct = R3Ct + 30f + 16f;   // 236
        private const float CtrlH = 30f;

        // ══════════════════════════════════════════════════════════════════════
        //  PALETTE
        // ══════════════════════════════════════════════════════════════════════

        static readonly Color BgStrip = new Color(0.036f, 0.060f, 0.106f, 1f);
        static readonly Color BgInput = new Color(0.040f, 0.068f, 0.118f, 1f);
        static readonly Color BgInput2 = new Color(0.030f, 0.055f, 0.095f, 1f);
        static readonly Color Grn = new Color(0.220f, 0.592f, 0.341f, 1f);
        static readonly Color Bdr = new Color(0.220f, 0.592f, 0.341f, 0.40f);
        static readonly Color BdrDim = new Color(0.150f, 0.280f, 0.200f, 0.45f);
        static readonly Color BtnNorm = new Color(0.075f, 0.110f, 0.180f, 1f);
        static readonly Color BtnHov = new Color(0.110f, 0.170f, 0.260f, 1f);
        static readonly Color BtnActv = new Color(0.055f, 0.140f, 0.080f, 1f);
        static readonly Color GrayTxt = new Color(0.420f, 0.480f, 0.500f, 1f);
        static readonly Color LabelClr = new Color(0.380f, 0.550f, 0.420f, 0.80f);
        static readonly Color BadgeClr = new Color(0.550f, 0.800f, 0.550f, 1f);
        static readonly Color CondPoor = new Color(0.90f, 0.28f, 0.18f, 1f);
        static readonly Color CondFair = new Color(0.85f, 0.72f, 0.20f, 1f);
        static readonly Color CondGood = new Color(0.22f, 0.75f, 0.40f, 1f);

        static readonly string[] CondLabels = { "All", "Poor", "Fair", "Good" };
        static readonly Color[] CondColors = { GrayTxt, CondPoor, CondFair, CondGood };
        static readonly string[] StarLabels = { "Any", "3★+", "4★+", "5★" };
        static readonly int[] StarVals = { 0, 3, 4, 5 };

        static readonly string[] SortLabels =
        {
            "Ending soonest",
            "Price: low → high",
            "Price: high → low",
            "Newest year",
            "Oldest year",
            "Lowest mileage",
            "Best rated",
        };
        static readonly ListingSort[] SortVals =
        {
            ListingSort.TimeLeft,
            ListingSort.PriceAsc,
            ListingSort.PriceDesc,
            ListingSort.YearDesc,
            ListingSort.YearAsc,
            ListingSort.MileageAsc,
            ListingSort.RatingDesc,
        };

        // ══════════════════════════════════════════════════════════════════════
        //  PRIVATE STATE
        // ══════════════════════════════════════════════════════════════════════

        private UIPanel _owner;
        private IntPtr _clipperPtr;
        private float _globalYBase;     // clipper top in UIPanel space
        private bool _isOpen = false;
        private bool _animBusy = false;

        // Last-built options (refreshed on Toggle-open)
        private FilterOptions _opts = new FilterOptions();

        // ── Draft state (before Apply) ───────────────────────────────────────
        private string _drMake, _drEngCat, _drDrv, _drRarity, _drColor, _drTire;
        private int _drCond, _drRat, _drSortIdx;

        // Text input objects — raw VE, read on Apply
        private object _tiYearFrom, _tiYearTo;
        private object _tiPriceMin, _tiPriceMax;
        private object _tiMileageMax;
        private object _tiPowerMin, _tiTorqueMin, _tiWeightMax;

        // Dropdown control refs (value labels — rebuilt per-open)
        private DropdownCtrl _ddMake, _ddEngCat, _ddDrv, _ddRarity, _ddColor, _ddTire, _ddSort;

        // Buttons for button-group selections
        private IntPtr[] _condPtrs = new IntPtr[4];
        private IntPtr[] _ratPtrs = new IntPtr[4];

        // Top-bar labels
        private UILabelHandle _arrowLbl;
        private UILabelHandle _badgeLbl;

        // Active popup tracker (only one open at a time)
        private IntPtr _activePopupPtr = IntPtr.Zero;

        // ══════════════════════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Buduje filter strip wewnątrz parentVE na y=yTop.
        /// <paramref name="globalYBase"/> = absolute Y of yTop in UIPanel space
        /// (potrzebne do pozycjonowania popupów dropdownów jako panel overlays).
        /// Zwraca zwiniętą wysokość (BarH).
        /// </summary>
        public float Build(UIPanel owner, object parentVE, float yTop, float globalYBase)
        {
            _owner = owner;
            _globalYBase = globalYBase;

            // Clipper — wysokość animowana
            var clip = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(clip);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, yTop);
                S.Width(s, PW); S.Height(s, BarH);
                S.Overflow(s, "Hidden");
                S.BgColor(s, BgStrip);
            }
            UIRuntime.AddChild(parentVE, clip);
            _clipperPtr = UIRuntime.GetPtr(clip);

            AddHLine(clip, 0f);
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
            const float Zone = 240f;
            const float Pad = 8f;
            float cy = (BarH - CtrlH) * 0.5f;

            var zone = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(zone);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, 0f);
                S.Width(s, Zone); S.Height(s, BarH);
            }
            UIRuntime.AddChild(parent, zone);
            var zonePtr = UIRuntime.GetPtr(zone);
            _owner.WireHover(zonePtr, new Color(0, 0, 0, 0), A(BtnNorm, 0.50f), BtnHov);
            _owner.WireClick(zonePtr, Toggle);

            _arrowLbl = _owner.AddLabelToContainer(zone, "▼", 16f, 0f, 22f, BarH, Grn);
            _arrowLbl.SetFontSize(11);
            MidLeft(_arrowLbl);

            var title = _owner.AddLabelToContainer(zone, "FILTERS", 38f, 0f, 90f, BarH, Grn);
            title.SetFontSize(13);
            MidLeft(title);

            _badgeLbl = _owner.AddLabelToContainer(parent, "", Zone + Pad, 0f, 800f, BarH, BadgeClr);
            _badgeLbl.SetFontSize(11);
            MidLeft(_badgeLbl);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CONTENT (rows of controls)
        // ══════════════════════════════════════════════════════════════════════

        private void BuildContent(object parent)
        {
            const float L = 16f;
            const float G = 8f;

            // ─────── ROW 1 — IDENTITY (6 dropdowns) ────────────────────────────
            float[] ddWidths = { 200f, 160f, 130f, 140f, 150f, 170f };
            string[] ddLabels = { "MAKE", "ENGINE", "DRIVETRAIN", "RARITY", "COLOR", "TYRE SIZE" };
            float cx = L;

            for (int i = 0; i < 6; i++) { SecLbl(parent, ddLabels[i], cx, R1Lbl); cx += ddWidths[i] + G; }
            cx = L;
            _ddMake = BuildDropdown(parent, cx, R1Ct, ddWidths[0], CtrlH, "Any make", () => _drMake, v => _drMake = v); cx += ddWidths[0] + G;
            _ddEngCat = BuildDropdown(parent, cx, R1Ct, ddWidths[1], CtrlH, "Any engine", () => _drEngCat, v => _drEngCat = v); cx += ddWidths[1] + G;
            _ddDrv = BuildDropdown(parent, cx, R1Ct, ddWidths[2], CtrlH, "Any drive", () => _drDrv, v => _drDrv = v); cx += ddWidths[2] + G;
            _ddRarity = BuildDropdown(parent, cx, R1Ct, ddWidths[3], CtrlH, "Any rarity", () => _drRarity, v => _drRarity = v); cx += ddWidths[3] + G;
            _ddColor = BuildDropdown(parent, cx, R1Ct, ddWidths[4], CtrlH, "Any color", () => _drColor, v => _drColor = v); cx += ddWidths[4] + G;
            _ddTire = BuildDropdown(parent, cx, R1Ct, ddWidths[5], CtrlH, "Any size", () => _drTire, v => _drTire = v);

            // ─────── ROW 2 — YEAR / PRICE / MILEAGE ────────────────────────────
            // [Year from 110] [Year to 110] gap [Price min 140] [Price max 140] gap [Mileage max 160]
            cx = L;
            SecLbl(parent, "YEAR FROM", cx, R2Lbl);
            _tiYearFrom = BuildTextInput(parent, cx, R2Ct, 110f, CtrlH, "");
            cx += 110f + G;

            SecLbl(parent, "YEAR TO", cx, R2Lbl);
            _tiYearTo = BuildTextInput(parent, cx, R2Ct, 110f, CtrlH, "");
            cx += 110f + 24f;

            SecLbl(parent, "PRICE MIN ($)", cx, R2Lbl);
            _tiPriceMin = BuildTextInput(parent, cx, R2Ct, 140f, CtrlH, "");
            cx += 140f + G;

            SecLbl(parent, "PRICE MAX ($)", cx, R2Lbl);
            _tiPriceMax = BuildTextInput(parent, cx, R2Ct, 140f, CtrlH, "");
            cx += 140f + 24f;

            SecLbl(parent, "MILEAGE MAX (mi)", cx, R2Lbl);
            _tiMileageMax = BuildTextInput(parent, cx, R2Ct, 160f, CtrlH, "");

            // ─────── ROW 3 — POWER / TORQUE / WEIGHT / COND / RATING ───────────
            cx = L;
            SecLbl(parent, "POWER MIN (HP)", cx, R3Lbl);
            _tiPowerMin = BuildTextInput(parent, cx, R3Ct, 120f, CtrlH, "");
            cx += 120f + G;

            SecLbl(parent, "TORQUE MIN (Nm)", cx, R3Lbl);
            _tiTorqueMin = BuildTextInput(parent, cx, R3Ct, 120f, CtrlH, "");
            cx += 120f + G;

            SecLbl(parent, "WEIGHT MAX (kg)", cx, R3Lbl);
            _tiWeightMax = BuildTextInput(parent, cx, R3Ct, 130f, CtrlH, "");
            cx += 130f + 24f;

            SecLbl(parent, "CONDITION", cx, R3Lbl);
            for (int i = 0; i < CondLabels.Length; i++)
            {
                int idx = i;
                _condPtrs[i] = _owner.AddButtonToContainer(
                    parent, CondLabels[i],
                    cx + i * 72f, R3Ct, 68f, CtrlH,
                    _drCond == i ? BtnActv : BtnNorm,
                    () => { _drCond = idx; RefreshBtnGroup(_condPtrs, _drCond, CondColors); });
                _owner.WireHover(_condPtrs[i],
                    _drCond == i ? BtnActv : BtnNorm, BtnHov, A(CondColors[i], 0.40f));
            }
            cx += 4 * 72f + 16f;

            SecLbl(parent, "RATING", cx, R3Lbl);
            for (int i = 0; i < StarLabels.Length; i++)
            {
                int idx = i;
                _ratPtrs[i] = _owner.AddButtonToContainer(
                    parent, StarLabels[i],
                    cx + i * 68f, R3Ct, 64f, CtrlH,
                    _drRat == i ? BtnActv : BtnNorm,
                    () => { _drRat = idx; RefreshBtnGroup(_ratPtrs, _drRat, new Color[] { Grn, Grn, Grn, Grn }); });
                _owner.WireHover(_ratPtrs[i],
                    _drRat == i ? BtnActv : BtnNorm, BtnHov, A(Grn, 0.40f));
            }

            // ─────── ROW 4 — SORT (left) + RESET + APPLY (right) ───────────────
            SecLbl(parent, "SORT BY", L, R4Ct - 16f);
            _ddSort = BuildDropdown(
                parent, L, R4Ct, 260f, CtrlH, SortLabels[0],
                () => SortLabels[_drSortIdx],
                v =>
                {
                    int idx = Array.IndexOf(SortLabels, v);
                    _drSortIdx = idx >= 0 ? idx : 0;
                },
                isSort: true);

            var resetPtr = _owner.AddButtonToContainer(
                parent, "✕  Reset", PW - 16f - 170f - G - 120f, R4Ct, 120f, CtrlH,
                BtnNorm, DoReset);
            _owner.WireHover(resetPtr, BtnNorm, BtnHov,
                new Color(0.80f, 0.20f, 0.10f, 0.50f));

            var applyPtr = _owner.AddButtonToContainer(
                parent, "✔  Apply Filters", PW - 16f - 170f, R4Ct, 170f, CtrlH,
                Grn, DoApply);
            _owner.WireHover(applyPtr, Grn,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CUSTOM DROPDOWN
        //  Button z value + ▼. Popup = overlay panelu (nie jest klipowany).
        //  Tylko jeden popup otwarty na raz (_activePopupPtr).
        // ══════════════════════════════════════════════════════════════════════

        private sealed class DropdownCtrl
        {
            public IntPtr ButtonPtr;
            public IntPtr PopupPtr;
            public UILabelHandle ValueLbl;
            public Action<string[]> Rebind;   // odśwież listę opcji
            public Action<string> SetValue; // ustaw zaznaczenie
        }

        /// <summary>
        /// isSort=true: wartość jest już gotowym label (nie sprawdza "Any …").
        /// W obu przypadkach pusty string = "none selected" → pokazuje placeholder.
        /// </summary>
        private DropdownCtrl BuildDropdown(
            object parent, float x, float y, float w, float h,
            string placeholder,
            Func<string> getVal, Action<string> setVal,
            bool isSort = false)
        {
            // ── Button ────────────────────────────────────────────────────────
            var btn = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(btn);
                S.Position(s, "Absolute");
                S.Left(s, x); S.Top(s, y);
                S.Width(s, w); S.Height(s, h);
                S.BgColor(s, BgInput);
                S.BorderRadius(s, 4f);
                S.BorderWidth(s, 1f);
                S.BorderColor(s, BdrDim);
                S.Overflow(s, "Hidden");
            }
            UIRuntime.AddChild(parent, btn);
            IntPtr btnPtr = UIRuntime.GetPtr(btn);

            // Value label
            var valLbl = _owner.AddLabelToContainer(
                btn, placeholder, 8f, 0f, w - 26f, h, GrayTxt);
            valLbl.SetFontSize(12);
            MidLeft(valLbl);

            // Arrow
            var arr = _owner.AddLabelToContainer(btn, "▾", w - 22f, 0f, 18f, h, Grn);
            arr.SetFontSize(12);
            MidLeft(arr);

            _owner.WireHover(btnPtr, BgInput, BgInput2, Bdr);

            // ── Popup (overlay panelu) ────────────────────────────────────────
            var popup = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(popup);
                S.Position(s, "Absolute");
                S.Left(s, x);
                S.Top(s, _globalYBase + y + h + 2f);
                S.Width(s, w); S.Height(s, 0f); // dynamicznie w Rebind
                S.BgColor(s, new Color(0.05f, 0.08f, 0.14f, 0.98f));
                S.BorderRadius(s, 4f);
                S.BorderWidth(s, 1f);
                S.BorderColor(s, Bdr);
                S.Overflow(s, "Hidden");
                S.Display(s, false);
            }
            _owner.AddOverlayToPanel(popup);
            IntPtr popupPtr = UIRuntime.GetPtr(popup);

            var ctrl = new DropdownCtrl
            {
                ButtonPtr = btnPtr,
                PopupPtr = popupPtr,
                ValueLbl = valLbl,
            };

            // Refresh label state
            void RefreshValueLabel()
            {
                string v = getVal() ?? "";
                if (string.IsNullOrEmpty(v))
                {
                    valLbl.SetText(placeholder);
                    valLbl.SetColor(GrayTxt);
                }
                else
                {
                    valLbl.SetText(v);
                    valLbl.SetColor(Color.white);
                }
            }

            ctrl.SetValue = newVal =>
            {
                setVal(newVal);
                RefreshValueLabel();
            };

            // Rebind — przebudowuje listę opcji w popup
            ctrl.Rebind = options =>
            {
                // wyczyść children popup
                UIRuntime.VisualElementType.GetMethod("Clear")?.Invoke(
                    UIRuntime.WrapVE(popupPtr), null);

                // Dla filtrów (nie sort): prefiks "Any …" = czyść zaznaczenie
                var list = new List<string>();
                if (!isSort) list.Add("");  // empty = "any/all"
                if (options != null) list.AddRange(options);

                const float ItemH = 28f;
                for (int i = 0; i < list.Count; i++)
                {
                    int idx = i;
                    string opt = list[i];
                    string display = string.IsNullOrEmpty(opt) ? "— " + placeholder + " —" : opt;

                    var itemPtr = _owner.AddButtonToContainer(
                        UIRuntime.WrapVE(popupPtr), display,
                        0f, idx * ItemH, w, ItemH,
                        new Color(0, 0, 0, 0),
                        () =>
                        {
                            ctrl.SetValue(opt);
                            HidePopup();
                        });
                    _owner.WireHover(itemPtr,
                        new Color(0, 0, 0, 0),
                        new Color(0.10f, 0.16f, 0.26f, 1f),
                        BtnActv);
                }

                // Wysokość popup (max 10 items → scroll nie ma, ale limit)
                float popH = Mathf.Min(list.Count, 10) * ItemH;
                S.Height(UIRuntime.GetStyle(UIRuntime.WrapVE(popupPtr)), popH);

                RefreshValueLabel();
            };

            // Click button → toggle popup
            _owner.WireClick(btnPtr, () =>
            {
                bool isOpen = (_activePopupPtr == popupPtr);
                HidePopup();
                if (!isOpen) ShowPopup(popupPtr);
            });

            return ctrl;
        }

        private void ShowPopup(IntPtr popupPtr)
        {
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(popupPtr)), true);
            _activePopupPtr = popupPtr;
        }

        private void HidePopup()
        {
            if (_activePopupPtr != IntPtr.Zero)
            {
                S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_activePopupPtr)), false);
                _activePopupPtr = IntPtr.Zero;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CUSTOM TEXT INPUT  (raw UnityEngine.UIElements.TextField)
        //  Wartość czytana tylko na Apply — bez change-callbacków.
        // ══════════════════════════════════════════════════════════════════════

        private object BuildTextInput(
            object parent, float x, float y, float w, float h,
            string initial)
        {
            try
            {
                var tfType = UIRuntime.TextFieldType;
                if (tfType == null) return null;

                object tf = Activator.CreateInstance(tfType);
                if (tf == null) return null;

                var s = UIRuntime.GetStyle(tf);
                S.Position(s, "Absolute");
                S.Left(s, x); S.Top(s, y);
                S.Width(s, w); S.Height(s, h);
                S.BgColor(s, BgInput);
                S.BorderRadius(s, 4f);
                S.BorderWidth(s, 1f);
                S.BorderColor(s, BdrDim);
                S.Padding(s, 6f);
                S.Color(s, Color.white);
                S.FontSize(s, 12);
                try { S.Font(s); } catch { }

                try { tfType.GetProperty("value")?.SetValue(tf, initial ?? ""); } catch { }

                UIRuntime.AddChild(parent, tf);
                return tf;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXLFilter] BuildTextInput failed: {ex.Message}");
                return null;
            }
        }

        private static int ReadIntField(object tf)
        {
            if (tf == null) return 0;
            try
            {
                string v = (string)UIRuntime.TextFieldType.GetProperty("value")?.GetValue(tf) ?? "";
                v = v.Trim().Replace(",", "").Replace(" ", "").Replace("$", "");
                return int.TryParse(v, out int i) && i > 0 ? i : 0;
            }
            catch { return 0; }
        }

        private static void WriteField(object tf, string v)
        {
            if (tf == null) return;
            try { UIRuntime.TextFieldType.GetProperty("value")?.SetValue(tf, v ?? ""); } catch { }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ANIMATION / OPEN / CLOSE
        // ══════════════════════════════════════════════════════════════════════

        public void Toggle()
        {
            if (_animBusy) return;
            bool willOpen = !_isOpen;

            if (willOpen)
            {
                // Refresh opcji dropdown przy każdym otwarciu
                _opts = OptionsProvider?.Invoke() ?? new FilterOptions();
                _ddMake?.Rebind(_opts.Makes);
                _ddEngCat?.Rebind(_opts.EngineCategories);
                _ddDrv?.Rebind(_opts.Drivetrains);
                _ddRarity?.Rebind(_opts.Rarities);
                _ddColor?.Rebind(_opts.Colors);
                _ddTire?.Rebind(_opts.TireSizes);
                _ddSort?.Rebind(SortLabels);
            }
            else
            {
                HidePopup();
            }

            MelonCoroutines.Start(AnimPanel(willOpen));
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

            S.Height(UIRuntime.GetStyle(UIRuntime.WrapVE(_clipperPtr)), to);
            _isOpen = opening;
            _animBusy = false;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  APPLY / RESET
        // ══════════════════════════════════════════════════════════════════════

        private void DoApply()
        {
            HidePopup();

            int yF = ReadIntField(_tiYearFrom);
            int yT = ReadIntField(_tiYearTo);
            int pMin = ReadIntField(_tiPriceMin);
            int pMax = ReadIntField(_tiPriceMax);
            int mMax = ReadIntField(_tiMileageMax);
            int pw = ReadIntField(_tiPowerMin);
            int tq = ReadIntField(_tiTorqueMin);
            int wt = ReadIntField(_tiWeightMax);

            if (yF > 0 && yT > 0 && yF > yT) (yF, yT) = (yT, yF);
            if (pMin > 0 && pMax > 0 && pMin > pMax) (pMin, pMax) = (pMax, pMin);

            Current = new FilterCriteria(
                _drMake, _drEngCat, _drDrv, _drRarity, _drColor, _drTire,
                yF, yT, pMin, pMax, mMax,
                pw, tq, wt,
                _drCond, StarVals[_drRat],
                SortVals[_drSortIdx]);

            UpdateBadge();
            OnFiltersApplied?.Invoke();

            if (_isOpen && !_animBusy)
                MelonCoroutines.Start(AnimPanel(false));
        }

        private void DoReset()
        {
            HidePopup();

            _drMake = _drEngCat = _drDrv = _drRarity = _drColor = _drTire = "";
            _drCond = 0;
            _drRat = 0;
            _drSortIdx = 0;

            _ddMake?.SetValue("");
            _ddEngCat?.SetValue("");
            _ddDrv?.SetValue("");
            _ddRarity?.SetValue("");
            _ddColor?.SetValue("");
            _ddTire?.SetValue("");
            _ddSort?.SetValue(SortLabels[0]);

            WriteField(_tiYearFrom, "");
            WriteField(_tiYearTo, "");
            WriteField(_tiPriceMin, "");
            WriteField(_tiPriceMax, "");
            WriteField(_tiMileageMax, "");
            WriteField(_tiPowerMin, "");
            WriteField(_tiTorqueMin, "");
            WriteField(_tiWeightMax, "");

            RefreshBtnGroup(_condPtrs, _drCond, CondColors);
            RefreshBtnGroup(_ratPtrs, _drRat, new Color[] { Grn, Grn, Grn, Grn });

            DoApply();
        }

        private void RefreshBtnGroup(IntPtr[] ptrs, int selected, Color[] accents)
        {
            for (int i = 0; i < ptrs.Length; i++)
            {
                if (ptrs[i] == IntPtr.Zero) continue;
                bool sel = (selected == i);
                Color bg = sel ? BtnActv : BtnNorm;
                var s = UIRuntime.GetStyle(UIRuntime.WrapVE(ptrs[i]));
                S.BgColor(s, bg);
                S.BorderWidth(s, sel ? 1f : 0f);
                S.BorderColor(s, sel ? accents[i] : new Color(0, 0, 0, 0));
                _owner.WireHover(ptrs[i], bg, BtnHov, A(accents[i], 0.40f));
            }
        }

        private void UpdateBadge()
        {
            int n = Current.ActiveCount;
            string sortSuffix = Current.Sort != ListingSort.TimeLeft
                ? $"  ·  sorted: {SortLabels[_drSortIdx].ToLower()}"
                : "";
            if (n == 0)
            {
                _badgeLbl?.SetText(string.IsNullOrEmpty(sortSuffix) ? "" : sortSuffix.TrimStart(' ', '·'));
                return;
            }
            string form = n == 1 ? "filter active" : "filters active";
            _badgeLbl?.SetText($"  {n} {form}{sortSuffix}");
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

        private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}