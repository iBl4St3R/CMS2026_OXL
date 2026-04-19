using CMS2026UITKFramework;
using Il2Cpp;
using Il2CppCMS.Core;
using Il2CppCMS.Core.Car.Containers;
using Il2CppCMS.UI;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CMS2026_OXL
{
    public class OXLPanel
    {
        // ── Paleta ────────────────────────────────────────────────────────────
        private static readonly Color PageBg = new Color(0.035f, 0.059f, 0.106f, 1.00f);
        private static readonly Color Border = new Color(0.220f, 0.592f, 0.341f, 0.40f);
        private static readonly Color TextGray = new Color(0.420f, 0.480f, 0.500f, 1.00f);
        private static readonly Color TextDim = new Color(0.180f, 0.230f, 0.280f, 1.00f);
        private static readonly Color OXLGreen = new Color(0.220f, 0.592f, 0.341f, 1.00f);
        private static readonly Color BtnDark = new Color(0.075f, 0.110f, 0.180f, 1.00f);
        private static readonly Color BtnDarkHi = new Color(0.110f, 0.170f, 0.260f, 1.00f);
        private static readonly Color SearchBdr = new Color(0.220f, 0.592f, 0.341f, 0.55f);
        private static readonly Color InputBg = new Color(0.030f, 0.055f, 0.095f, 1.00f);
        private static readonly Color Transp = new Color(0f, 0f, 0f, 0f);
        private static readonly Color FooterBg = new Color(0.030f, 0.068f, 0.048f, 1.00f);
        private static readonly Color TagBg = new Color(0.055f, 0.090f, 0.145f, 1.00f);
        private static readonly Color TagBdr = new Color(0.150f, 0.280f, 0.200f, 0.55f);

        private const float PanelW = 1456f;
        private const float PanelH = 980f;
        private const float ContentW = PanelW - 24f;
        private const float CenterW = 680f;

        // ── Address bar overlay layout ─────────────────────────────────────────
        // UIPanel title bar = 24px. Address bar overlay sits right below it.
        // AddrBarH: 1px sep + 4px gap + 44px row + 3px gap = 52px.
        // All page overlays start at OverlayTop = 24 + 52 = 76px in panel space.
        private const float TitleBarH = 24f;
        private const float AddrBarH = 52f;
        private const float OverlayTop = TitleBarH + AddrBarH; // 76f

        private UIPanel _panel;
        private ListingSystem _listings; // nie inicjalizuj tu

        private OXLFilterPanel _filterPanel;

        // ── Search & filter state ─────────────────────────────────────────────
        private UITextInputHandle _searchInput;
        private UIDropdownHandle _makeDropdown;
        private UIDropdownHandle _yearDropdown;
        private List<CarListing> _filteredListings; // null = no filter, show all

        private const string PlaceholderText = "Search for vehicles, parts or tools";
        private static readonly string[] MakeOptions =
        {
            "All", "APlaceholder", "BPlaceholder", "FPlaceholder",
            "HPlaceholder", "MPlaceholder", "TPlaceholder", "VPlaceholder"
        };

        // ── Listing page ──────────────────────────────────────────────────────
        private IntPtr _listingPagePtr;
        private IntPtr _listingRowsContainerPtr;
        private UILabelHandle _pageCountLabel;
        private int _currentPage = 0;
        private const int RowsPerPage = 8;
        private const float RowH = 90f;
        private const float RowGap = 1f;

        private bool _listingPageWasOpen = false;//flaga display

        private int _lastKnownListingCount = -1;

        private UILabelHandle _listingMoneyLbl;
        private string FormatMetaLine(CarListing listing)
        {
            float rem = listing.ExpiresAt - _listings.GameTime;
            string timer = rem <= 0f
                ? "Auction ended"
                : FormatTimeRemaining(rem);

            string mi = listing.Mileage >= 1000
                ? $"{listing.Mileage / 1000}k mi"
                : $"{listing.Mileage} mi";

            return $"{timer}  ·  {listing.Location}  ·  {mi}  ·  {listing.Year}  ·  ~{listing.DeliveryHours}h delivery";
        }

        private readonly Dictionary<string, UILabelHandle> _timerLabels = new();
        private bool _buyClickConsumed = false;


        // ── Photo gallery state ───────────────────────────────────────────────────
        private CarPhotoLoader _photoLoader;
        private List<Texture2D> _galleryPhotos = new();
        private int _galleryIndex = 0;

        // ── Gallery UI elements ───────────────────────────────────────────────────
        private IntPtr _galleryMainImgPtr;    // główne zdjęcie 820×462
        private IntPtr _galleryPrevBtnPtr;
        private IntPtr _galleryNextBtnPtr;
        private IntPtr _galleryThumbsRowPtr;  // kontener miniaturek
        private UILabelHandle _galleryCounterLbl;

        private const float ImgW = 820f;
        private const float ImgH = 462f;
        private const float ThumbW = 80f;
        private const float ThumbH = 45f;
        private const float ThumbGap = 6f;

        // ── Detail overlay ────────────────────────────────────────────────────
        private IntPtr _detailOverlayPtr;
        private UILabelHandle _detailTitle;
        private UILabelHandle _detailYear;
        private UILabelHandle _detailTimer;
        private UILabelHandle _detailPrice;
        private IntPtr _detailBuyPtr;
        private CarListing _detailListing;

        private UILabelHandle _detailListedLbl;
        private UILabelHandle _detailSellerNote;
        private UILabelHandle _detailLocationLbl;
        private IntPtr _detailSpecsContainerPtr;

        private UILabelHandle _detailSellerStars;

        private UILabelHandle _detailBalanceLbl;

        // ── Menu & static pages ───────────────────────────────────────────────
        private IntPtr _menuDropdownPtr;
        private bool _menuOpen = false;
        private IntPtr _pageOverlayPtr;
        private UILabelHandle _pageTitleLbl;
        private UILabelHandle _pageBodyLbl;

        private static readonly string[] PageTitles = { "Help", "Settings", "About" };
        private static readonly string[] PageBodies =
        {
            "OXL is an in-game car auction marketplace for Car Mechanic Simulator 2026.\n\n" +
            "Browse active listings, buy cars, fix them up and sell for profit.\n\n" +
            "Controls\n  F10  —  toggle the OXL panel\n  oxl_open  —  console command\n\n" +
            "Tip: listings expire over time — check back often for new deals.",

            "[Coming soon]\n\nPlanned options:\n  • Currency display\n  • Auction refresh rate\n  • Notification preferences",

            "OXL — Online eX-Owner Lies\nVersion: 0.4.1\nAuthor: iBlaster\n\n" +
            "Built on _CMS2026_UITK_Framework.\n\n" +
            "github.com/iBl4St3R/CMS2026-OXL\n\n" +
             "— Icons —\n" +
            "Hamburger icon by See Icons · Flaticon\n" +
            "Lock icon by verry purnomo · Flaticon\n" +
            "Next icon by Gajah Mada · Flaticon\n" +
            "Refresh icon by riajulislam · Flaticon\n" +
            "Previous icon by Slidicon · Flaticon\n" +
            "flaticon.com"
        };

        // ── Icon cache ────────────────────────────────────────────────────────
        private Texture2D _icoPrev, _icoNext, _icoRef, _icoSecured, _icoMenu;

        // ── button cache ────────────────────────────────────────────────────────
        private Texture2D _passengerCars, _carParts, _workshopItems, _decorations;




        // klucz: "DNB Censor/cyan_good" itp.
        private readonly Dictionary<string, Texture2D> _carImages = new();

        // Właściwość pomocnicza do sprawdzania stanu z zewnątrz
        private bool _isVisible; // Zmienna śledząca stan

        public bool IsVisible => _isVisible;


        // ── Listing Generation Settings ───────────────────────────────────────────────
        private IntPtr _listingGenOverlayPtr;
        private IntPtr _archLevelOverlayPtr;
        private int _archLevelIdx;   // który archetype jest aktualnie edytowany (0–3)

        // ── Draft state — aplikowany dopiero po Save ──────────────────────────────────
        private int _draftMaxListings = 20;   // 1–50
        private int _draftGenChancePct = 50;   // 0–100 %
        private int _draftGenMin = 1;    // 1–16
        private int _draftGenMax = 4;    // 1–16
        private int _draftDurMinH = 12;   // 1–167 h
        private int _draftDurMaxH = 36;   // 2–168 h

        // [0]=Honest [1]=Neglected [2]=Dealer [3]=Wrecker — linked, sum=100, step=5
        private readonly int[] _draftArchW = { 20, 35, 30, 15 };

        // [arch][L1/L2/L3] — linked per archetype, sum=100, step=5
        private readonly int[][] _draftLvlW =
        {
    new[] { 40, 35, 25 },  // Honest
    new[] { 35, 35, 30 },  // Neglected
    new[] { 30, 40, 30 },  // Dealer
    new[] { 45, 30, 25 },  // Wrecker
};

        // ── UI handle refs — aktualizowane live przy zmianie wartości ────────────────
        private UILabelHandle _lgMaxLbl, _lgChanceLbl;
        private UILabelHandle _lgGenMinLbl, _lgGenMaxLbl;
        private UILabelHandle _lgDurMinLbl, _lgDurMaxLbl;
        private UILabelHandle _lgArchSumLbl;
        private UILabelHandle[] _lgArchLbls = new UILabelHandle[4];
        private IntPtr[] _lgArchBarFillPtrs = new IntPtr[4];
        private float _lgArchBarMaxW;   // obliczana raz podczas build
        private UILabelHandle _lgLvlTitleLbl, _lgLvlSumLbl;
        private UILabelHandle[] _lgLvlLbls = new UILabelHandle[3];
        private IntPtr[] _lgLvlBarFillPtrs = new IntPtr[3];
        private float _lgLvlBarMaxW;


        // ── Settings overlay ──────────────────────────────────────────────────────
        private IntPtr _settingsOverlayPtr;
        private UILabelHandle _diffEasyLbl;
        private UILabelHandle _diffNormalLbl;
        private UILabelHandle _diffHardLbl;

       

        // ── Settings difficulty card ptrs (do aktualizacji bordera) ──────────
        private IntPtr _diffEasyCardPtr;
        private IntPtr _diffNormalCardPtr;
        private IntPtr _diffHardCardPtr;

        // ── Lock state — archetype i level ───────────────────────────────────────
        private readonly bool[] _draftArchLocked = { false, false, false, false };
        private readonly IntPtr[] _lgArchLockBtnPtrs = new IntPtr[4];

        private readonly bool[][] _draftLvlLocked =
        {
    new[] { false, false, false },
    new[] { false, false, false },
    new[] { false, false, false },
    new[] { false, false, false },
};
        private readonly IntPtr[] _lgLvlLockBtnPtrs = new IntPtr[3];


        static readonly Color LockBgOff = new Color(0.06f, 0.10f, 0.18f, 1f);  // odblokowany
        static readonly Color LockBgOn = new Color(0.28f, 0.05f, 0.05f, 1f);  // zablokowany — ciemny czerwony
        static readonly Color LockBdrOn = new Color(0.80f, 0.20f, 0.20f, 0.80f);
        static readonly Color LockBdrOff = new Color(0.15f, 0.25f, 0.38f, 0.40f);

        private IntPtr _lgViewportPtr;
        private IntPtr _lgContentPtr;
        private float _lgScrollY = 0f;
        private float _lgContentHeight = 0f;
        private const float LgScrollStep = 40f;



        private CarSpecLoader _specLoader;
        // ══════════════════════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════════════════════

        public void Build()
        {
            LoadIcons();

            // ── Załaduj draft z zapisanego configu ────────────────────────────────
            var saved = OXLSettings.SavedGenConfig;
            _draftMaxListings = saved.MaxListings;
            _draftGenChancePct = saved.GenChancePct;
            _draftGenMin = saved.GenMin;
            _draftGenMax = saved.GenMax;
            _draftDurMinH = Mathf.RoundToInt(saved.DurMinSec / ListingGenConfig.SecondsPerGameHour);
            _draftDurMaxH = Mathf.RoundToInt(saved.DurMaxSec / ListingGenConfig.SecondsPerGameHour);
            Array.Copy(saved.ArchWeights, _draftArchW, 4);
            for (int a = 0; a < 4; a++)
                Array.Copy(saved.LvlWeights[a], _draftLvlW[a], 3);

            string modsRoot = Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources");
            _photoLoader = new CarPhotoLoader(modsRoot, ListingSystem.GetColorRegistry());
            _specLoader = new CarSpecLoader(modsRoot);
            _listings = new ListingSystem(_photoLoader, _specLoader);

            // ── Aplikuj zapisany config od razu przy starcie ──────────────────────────
            _listings.ApplyConfig(OXLSettings.SavedGenConfig);
            _listings.LoadSaved();

            OXLPlugin.Log.Msg($"[OXL] ListingSystem initialized with saved config" +
                              $" — max={OXLSettings.SavedGenConfig.MaxListings}" +
                              $" chance={OXLSettings.SavedGenConfig.GenChancePct}%");




            float x = Mathf.Max(0f, (Screen.width - PanelW) / 2f);
            float y = Mathf.Max(0f, (Screen.height - PanelH) / 2f);

            // ZMIANA: UIPanel.Create zamiast FrameworkAPI.CreatePanel
            // CreatePanel teraz auto-wywołuje Build() wewnętrznie — tu chcemy
            // dodać przyciski tytułu PRZED Build(), więc wywołujemy je ręcznie.
            _panel = UIPanel.Create("OXL", x, y, PanelW, PanelH);
            _panel.AddTitleButton("\u2014", () => { }, new Color(0.15f, 0.18f, 0.25f, 1f));
            _panel.AddTitleButton("\u25A1", () => { }, new Color(0.15f, 0.18f, 0.25f, 1f));
            _panel.AddTitleButton("\u2715", Close, new Color(0.55f, 0.10f, 0.10f, 1f));
            _panel.Build(sortOrder: 9000);   // ← jeden Build, z przyciskami już w liście
            _panel.SetDraggable(false);
            _panel.SetScrollbarVisible(false);

            var pve = UIRuntime.WrapVE(_panel.GetPanelRawPtr());
            var pst = UIRuntime.GetStyle(pve);
            S.BgColor(pst, PageBg);
            S.BorderRadius(pst, 8f);
            S.BorderWidth(pst, 1f);
            S.BorderColor(pst, Border);

            // ── Content first (home page scrollable area) ───────────────────
            BuildContent();

            // ── Page overlays — added BEFORE address bar so they render under it ──
            BuildPageOverlay();     // Help / Settings / About
            BuildListingPage();     // car auction list
            BuildDetailOverlay();   // single listing detail


            BuildListingGenSettingsOverlay();   
            BuildArchLevelOverlay();           


            // ── Address bar LAST — renders on top of all overlays ───────────
            // BuildMenuDropdown is called from inside BuildAddressBar, also last,
            // so the dropdown itself renders on top of the address bar. ✓

            BuildAlertOverlay();
            BuildSettingsOverlay();
            BuildAddressBar();



            _panel.SetUpdateCallback(dt =>
            {
                UpdateTimers();
            });

            _panel.SetVisible(false);
        }


        // ══════════════════════════════════════════════════════════════════════
        //  ADDRESS BAR  (overlay — always visible on every page)
        // ══════════════════════════════════════════════════════════════════════

        private void BuildAddressBar()
        {
            // Fixed-position overlay: Top = TitleBarH, Height = AddrBarH.
            // Added last → UIToolkit renders it above all other overlays.
            var bar = UIRuntime.NewVE();
            var bs = UIRuntime.GetStyle(bar);
            S.Position(bs, "Absolute");
            S.Left(bs, 0f); S.Top(bs, TitleBarH);
            S.Width(bs, PanelW); S.Height(bs, AddrBarH);
            S.BgColor(bs, PageBg);
            S.Overflow(bs, "Hidden");

            // Top separator
            AddSepToContainer(bar, 0f);

            const float RowY = 5f;
            const float RowH = 44f;
            float cx = 8f;

            // Nav buttons
            cx = AddIconBtnToBar(bar, cx, RowY, RowH, _icoPrev, "\u2190", 36f, () => { });
            cx = AddIconBtnToBar(bar, cx, RowY, RowH, _icoNext, "\u2192", 36f, () => { });
            cx = AddIconBtnToBar(bar, cx, RowY, RowH, _icoRef, "\u21BB", 36f, () => { });
            cx += 8f;

            // Lock icon
            if (_icoSecured != null)
            {
                var lockVE = UIRuntime.NewVE();
                var ls = UIRuntime.GetStyle(lockVE);
                S.Position(ls, "Absolute");
                S.Left(ls, cx); S.Top(ls, RowY + (RowH - 24f) * 0.5f);
                S.Width(ls, 24f); S.Height(ls, 24f);
                UIRuntime.SetBackgroundImage(lockVE, _icoSecured);
                UIRuntime.AddChild(bar, lockVE);
                cx += 28f;
            }

            // URL bar — stretches to fill space, leaves room for menu button
            const float MenuBtnW = 38f;
            float urlW = PanelW - cx - MenuBtnW - 10f;
            var urlLbl = _panel.AddLabelToContainer(
                bar, "  oxl.com/home",
                cx, RowY + (RowH - 28f) * 0.5f, urlW, 28f, TextGray);
            var urlVE = UIRuntime.WrapVE(urlLbl.GetRawPtr());
            var urlSt = UIRuntime.GetStyle(urlVE);
            S.BgColor(urlSt, new Color(0.05f, 0.08f, 0.14f, 1f));
            S.BorderRadius(urlSt, 14f);
            S.BorderWidth(urlSt, 1f);
            S.BorderColor(urlSt, SearchBdr);
            S.Padding(urlSt, 6f);
            cx += urlW + 6f;

            // Menu button
            AddIconBtnToBar(bar, cx, RowY, RowH, _icoMenu, "\u22EE", MenuBtnW, () => ToggleMenu());

            // Bottom separator
            AddSepToContainer(bar, AddrBarH - 1f);

            // Register with panel — LAST so it renders above page overlays
            _panel.AddOverlayToPanel(bar);

            // Menu dropdown added after bar → renders above bar ✓
            BuildMenuDropdown();
        }

        /// <summary>Adds a 1px horizontal separator to an arbitrary container VE.</summary>
        private void AddSepToContainer(object container, float y)
        {
            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, y);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(container, sep);
        }

        /// <summary>
        /// Adds a single icon/text button to the address-bar container.
        /// Returns the next X cursor position.
        /// </summary>
        private float AddIconBtnToBar(object container, float x, float rowY, float rowH,
                                      Texture2D icon, string fallback, float width, Action onClick)
        {
            float topOffset = rowY + (rowH - width) * 0.5f;

            if (icon != null)
            {
                var ve = UIRuntime.NewVE();
                var st = UIRuntime.GetStyle(ve);
                S.Position(st, "Absolute");
                S.Left(st, x); S.Top(st, topOffset);
                S.Width(st, width); S.Height(st, width);
                S.BorderRadius(st, 4f);
                UIRuntime.SetBackgroundImage(ve, icon);
                UIRuntime.AddChild(container, ve);
                var ptr = UIRuntime.GetPtr(ve);
                _panel.WireHover(ptr, Transp, BtnDark, BtnDarkHi);
                if (onClick != null) _panel.WireClick(ptr, onClick);
            }
            else
            {
                var ptr = _panel.AddButtonToContainer(
                    container, fallback, x, rowY, width, rowH, Transp, onClick);
                _panel.WireHover(ptr, Transp, BtnDark, BtnDarkHi);
            }

            return x + width + 2f;
        }


        // ══════════════════════════════════════════════════════════════════════
        //  HOME PAGE CONTENT
        // ══════════════════════════════════════════════════════════════════════

        private void BuildContent()
        {
            // Reserve space so the fixed address-bar overlay doesn't cover content.
            // Viewport top in panel space = TitleH(24) + Pad(6) = 30.
            // Address bar bottom in panel space = OverlayTop = 76.
            // Reserve = 76 - 30 = 46px.
            _panel.AddSpace(46f);


            // ── Alpha Warning Banner (overlay tło + labele w flow) ────────────────

            // 1. Overlay - tło czerwone, absolutne, przyklejone do góry content area
            var warnOverlay = UIRuntime.NewVE();
            var wos = UIRuntime.GetStyle(warnOverlay);
            S.Position(wos, "Absolute");
            S.Left(wos, 0f);
            S.Top(wos, 46f + 24f + 6f); // TitleH + Pad + po AddSpace(46)
            S.Width(wos, ContentW);
            S.Height(wos, 78f);
            S.BgColor(wos, new Color(0.22f, 0.04f, 0.03f, 1f));
            S.BorderWidth(wos, 1f);
            S.BorderColor(wos, new Color(0.80f, 0.18f, 0.08f, 0.70f));


            // 2. Labele przez normalny flow (content)
            _panel.AddSpace(4f);

            var w1 = _panel.AddLabel(
                "⚠  THIS MOD IS IN ALPHA — MANY BUGS EXIST AND NUMEROUS FEATURES ARE NOT YET FUNCTIONAL  ⚠",
                new Color(1.0f, 0.52f, 0.12f, 1f), height: 36f);
            w1.SetFontSize(24);
            var w1ve = UIRuntime.WrapVE(w1.GetRawPtr());
            S.TextAlign(UIRuntime.GetStyle(w1ve), TextAnchor.MiddleCenter);
            S.BgColor(UIRuntime.GetStyle(w1ve), new Color(0.22f, 0.04f, 0.03f, 1f));

            var w2 = _panel.AddLabel(
                "THIS MOD WILL NOT BE AVAILABLE OR SUPPORTED (IN DEMO) ONCE THE FULL GAME IS RELEASED FOR PURCHASE",
                new Color(1.0f, 0.35f, 0.25f, 1f), height: 36f);
            w2.SetFontSize(24);
            var w2ve = UIRuntime.WrapVE(w2.GetRawPtr());
            S.TextAlign(UIRuntime.GetStyle(w2ve), TextAnchor.MiddleCenter);
            S.BgColor(UIRuntime.GetStyle(w2ve), new Color(0.22f, 0.04f, 0.03f, 1f));

            var w3 = _panel.AddLabel(
                "SAVE YOUR GAME BEFORE USE  ·  USE AT YOUR OWN RISK  ·  REPORT BUGS ON NEXUSMODS OR GITHUB",
                new Color(0.70f, 0.28f, 0.20f, 0.85f), height: 36f);
            w3.SetFontSize(24);
            var w3ve = UIRuntime.WrapVE(w3.GetRawPtr());
            S.TextAlign(UIRuntime.GetStyle(w3ve), TextAnchor.MiddleCenter);
            S.BgColor(UIRuntime.GetStyle(w3ve), new Color(0.22f, 0.04f, 0.03f, 1f));

            _panel.AddSpace(6f);




            // ── Logo ──────────────────────────────────────────────────────────
            Texture2D logo = TryLoadLogo();
            if (logo != null)
            {
                const float ImgW = 580f;
                const float ImgH = 286f;
                var logoImg = _panel.AddImage(logo, ImgW, ImgH);
                float imgLeft = (ContentW - ImgW) / 2f;
                S.Left(UIRuntime.GetStyle(UIRuntime.WrapVE(logoImg.GetRawPtr())), imgLeft);
            }
            else
            {
                var lbl = _panel.AddLabel("OXL", OXLGreen, height: 80f);
                lbl.SetFontSize(68);
            }

            _panel.AddSpace(8f);

            // ── Search field ──────────────────────────────────────────────────
            float searchSide = (ContentW - CenterW) / 2f;

            _searchInput = _panel.AddTextInput(
                placeholder: "",
                onSubmit: _ => ExecuteSearch(),
                height: 48f);

            var sVE = UIRuntime.WrapVE(_searchInput.GetRawPtr());
            var sSt = UIRuntime.GetStyle(sVE);
            S.Width(sSt, CenterW);
            S.Left(sSt, searchSide);
            S.BgColor(sSt, InputBg);
            S.BorderRadius(sSt, 24f);
            S.BorderWidth(sSt, 1f);
            S.BorderColor(sSt, SearchBdr);
            S.Padding(sSt, 10f);

            _searchInput.SetFakePlaceholder(PlaceholderText, TextGray, Color.white);

            // ── Action / filter row ───────────────────────────────────────────
            _panel.AddSpace(14f);
            var btnRow = _panel.AddRow(height: 36f, gap: 10f);

            float totalBtns = 140f + 130f + 110f + 10f * 2;
            float bside = (btnRow.RemainingWidth - totalBtns) / 2f;
            btnRow.AddSpace(bside);

            var bSearch = btnRow.AddButton("Search OXL", 140f,
                onClick: ExecuteSearch, bgColor: BtnDark);
            bSearch.SetTextColor(OXLGreen);
            _panel.WireHover(bSearch.GetRawPtr(), BtnDark, BtnDarkHi, SearchBdr);

            // Make dropdown — store handle for filter reads
            _makeDropdown = btnRow.AddDropdown(
                "Make", MakeOptions,
                selectedIndex: 0,
                onChanged: _ => { /* filter applied on search */ },
                width: 130f);

            // Year dropdown — store handle
            var years = new List<string> { "Any year" };
            for (int yr = 2020; yr >= 1990; yr--) years.Add(yr.ToString());
            _yearDropdown = btnRow.AddDropdown(
                "Year from", years.ToArray(),
                selectedIndex: 0,
                onChanged: _ => { /* filter applied on search */ },
                width: 110f);

            // ── Categories ────────────────────────────────────────────────────
            _panel.AddSpace(36f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(10f);


            // 1. Parametry dopasowane do szerokości 1456f
            float itemWidth = 360f;   // Szerokość kafelka (możesz zmienić na mniejszą, np. 300f)
            float itemHeight = 46f;
            float gap = 0f;          // Nieco większy odstęp dla lepszej czytelności
            int itemCount = 4;

            // 2. Obliczamy całkowitą szerokość zawartości
            float totalContentWidth = (itemCount * itemWidth) + ((itemCount - 1) * gap);




            // 4. Budowa wiersza
            var catRow = _panel.AddRow(height: itemHeight, gap: gap);
            catRow.AddSpace(8); // Lewy margines

            // Passenger Cars
            var carCatLbl = catRow.AddLabel("\U0001F697  Passenger Cars", width: itemWidth, color: OXLGreen);
            UIRuntime.SetBackgroundImage(UIRuntime.WrapVE(carCatLbl.GetRawPtr()), _passengerCars);
            _panel.WireHover(carCatLbl.GetRawPtr(), Transp, BtnDark, BtnDarkHi);
            _panel.WireClick(carCatLbl.GetRawPtr(), ShowAllListings);

            // Parts
            var partsCatLbl = catRow.AddLabel("\U0001F527  Parts", width: itemWidth, color: TextGray);
            UIRuntime.SetBackgroundImage(UIRuntime.WrapVE(partsCatLbl.GetRawPtr()), _carParts);

            // Workshop Items
            var itemsCatLbl = catRow.AddLabel("\U0001F690  Workshop Items", width: itemWidth, color: TextGray);
            UIRuntime.SetBackgroundImage(UIRuntime.WrapVE(itemsCatLbl.GetRawPtr()), _workshopItems);

            // Decorations
            var decorCatLbl = catRow.AddLabel("\U0001F4E6  Decorations", width: itemWidth, color: TextGray);
            UIRuntime.SetBackgroundImage(UIRuntime.WrapVE(decorCatLbl.GetRawPtr()), _decorations);

            catRow.AddSpace(8); // Prawy margines

            _panel.AddSpace(10f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(20f);

            var wip = _panel.AddLabel("\u2014 Rest of page under construction \u2014", TextGray);
            wip.SetFontSize(11);

            _panel.AddSpace(20f);
            _panel.AddSeparator(new Color(0.15f, 0.42f, 0.24f, 0.45f));

            var footerLbl = _panel.AddLabel(
                "OXL \u2014 Online eX-Owner Lies  \u00B7  v0.4.1  \u00B7  \u00A9 iBlaster  \u00B7  github.com/iBl4St3R/CMS2026-OXL",
                new Color(0.22f, 0.48f, 0.30f, 0.70f),
                height: 32f);
            footerLbl.SetFontSize(10);

            var footVE = UIRuntime.WrapVE(footerLbl.GetRawPtr());
            S.BgColor(UIRuntime.GetStyle(footVE), FooterBg);
            S.TextAlign(UIRuntime.GetStyle(footVE), TextAnchor.MiddleCenter);
        }


        // ══════════════════════════════════════════════════════════════════════
        //  SEARCH / FILTER
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Reads search field + dropdown state, builds _filteredListings,
        /// then opens the listing page to show results.
        /// </summary>
        private void ExecuteSearch()
        {
            ApplyFilters();
            ShowListingPage();
        }

        /// <summary>
        /// Filters _listings.ActiveListings into _filteredListings.
        /// If no filter criteria are active, _filteredListings is set to null
        /// (meaning "show all" — avoids copying the list when not needed).
        /// </summary>
        private void ApplyFilters()
        {
            string query = (_searchInput?.GetValue() ?? "").Trim();
            if (query == PlaceholderText) query = "";
            query = query.ToLower();

            int makeIdx = _makeDropdown?.SelectedIndex ?? 0;
            string makeFilter = makeIdx == 0 ? "" : MakeOptions[makeIdx];

            // Year dropdown: index 0 = any, 1 = 2020, 2 = 2019, …
            int yearIdx = _yearDropdown?.SelectedIndex ?? 0;
            int minYear = yearIdx == 0 ? 0 : (2021 - yearIdx); // 2020 - (yearIdx-1)

            bool noFilter = string.IsNullOrEmpty(query)
                         && string.IsNullOrEmpty(makeFilter)
                         && minYear == 0;

            if (noFilter)
            {
                _filteredListings = null;
                return;
            }

            _filteredListings = _listings.ActiveListings.Where(l =>
            {
                bool textOk = string.IsNullOrEmpty(query)
                    || l.Make.ToLower().Contains(query)
                    || l.Model.ToLower().Contains(query)
                    || l.Year.ToString().Contains(query);

                bool makeOk = string.IsNullOrEmpty(makeFilter)
                    || l.Make.Equals(makeFilter, StringComparison.OrdinalIgnoreCase);

                bool yearOk = minYear == 0 || l.Year >= minYear;

                return textOk && makeOk && yearOk;
            }).ToList();
        }

        /// <summary>
        /// Filtruje listings wg kryteriów z OXLFilterPanel.
        /// Zwraca null jeśli żadne filtry nie są aktywne (= pokaż wszystko).
        /// </summary>
        private List<CarListing> ApplyFilterCriteria(OXLFilterPanel.FilterCriteria c)
        {
            if (c == null || c.IsEmpty) return null;

            return _listings.ActiveListings.Where(l =>
            {
                bool makeOk = string.IsNullOrEmpty(c.Make)
                    || l.Make.Equals(c.Make, StringComparison.OrdinalIgnoreCase);

                bool yearOk = (c.MinYear == 0 || l.Year >= c.MinYear)
                           && (c.MaxYear == 0 || l.Year <= c.MaxYear);

                bool priceOk = (c.MinPrice == 0 || l.Price >= c.MinPrice)
                            && (c.MaxPrice == 0 || l.Price <= c.MaxPrice);

                bool condOk = c.CondTier == 0
                    || (c.CondTier == 1 && l.ApparentCondition < 0.30f)
                    || (c.CondTier == 2 && l.ApparentCondition >= 0.30f && l.ApparentCondition < 0.70f)
                    || (c.CondTier == 3 && l.ApparentCondition >= 0.70f);

                bool ratingOk = c.MinRating == 0 || l.SellerRating >= c.MinRating;

                return makeOk && yearOk && priceOk && condOk && ratingOk;
            }).ToList();
        }



        /// <summary>Clears filter and shows all listings.</summary>
        private void ShowAllListings()
        {
            _filteredListings = null;
            _currentPage = 0;
            ShowListingPage();
        }


        // ══════════════════════════════════════════════════════════════════════
        //  MENU DROPDOWN
        // ══════════════════════════════════════════════════════════════════════

        private void ToggleMenu()
        {
            _menuOpen = !_menuOpen;
            if (_menuDropdownPtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_menuDropdownPtr)), _menuOpen);
        }

        private void BuildMenuDropdown()
        {
            const float DropW = 140f;
            const float ItemH = 36f;
            // Position just below the address bar, right-aligned
            float dropTop = OverlayTop + 4f;
            float dropLeft = PanelW - DropW - 8f;

            var drop = UIRuntime.NewVE();
            var ds = UIRuntime.GetStyle(drop);
            S.Position(ds, "Absolute");
            S.Left(ds, dropLeft); S.Top(ds, dropTop);
            S.Width(ds, DropW); S.Height(ds, ItemH * PageTitles.Length + 6f);
            S.BgColor(ds, new Color(0.07f, 0.10f, 0.16f, 0.98f));
            S.BorderRadius(ds, 6f);
            S.BorderColor(ds, Border); S.BorderWidth(ds, 1f);
            S.Overflow(ds, "Hidden");
            S.Display(ds, false);
            _panel.AddOverlayToPanel(drop);
            _menuDropdownPtr = UIRuntime.GetPtr(drop);

            for (int i = 0; i < PageTitles.Length; i++)
            {
                int idx = i;
                var ptr = _panel.AddButtonToContainer(
                    drop, PageTitles[i],
                    0f, 3f + i * ItemH, DropW, ItemH,
                    BtnDark, () => { ToggleMenu(); ShowPage(idx); });
                _panel.WireHover(ptr, BtnDark, BtnDarkHi, SearchBdr);
            }
        }


        // ══════════════════════════════════════════════════════════════════════
        //  STATIC PAGE OVERLAY  (Help / Settings / About)
        // ══════════════════════════════════════════════════════════════════════

        private void BuildPageOverlay()
        {
            const float TopBarH = 44f;
            const float Pad = 16f;

            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, OverlayTop);
            S.Width(os, PanelW); S.Height(os, PanelH - OverlayTop);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _pageOverlayPtr = UIRuntime.GetPtr(overlay);

            // Top bar
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, TopBarH);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            // ← HidePage, nie HideListingPage
            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Back", 12f, 6f, 110f, 32f, BtnDark, HidePage);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            // ← _pageTitleLbl, dynamicznie ustawiany przez ShowPage()
            _pageTitleLbl = _panel.AddLabelToContainer(
                topBar, "", 130f, 0f, PanelW - 150f, TopBarH, OXLGreen);
            _pageTitleLbl.SetFontSize(18);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_pageTitleLbl.GetRawPtr())),
                TextAnchor.MiddleLeft);

            // Separator
            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, TopBarH);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(overlay, sep);

            // Body
            _pageBodyLbl = _panel.AddLabelToContainer(
                overlay, "",
                Pad, TopBarH + Pad,
                PanelW - Pad * 2f, PanelH - OverlayTop - TopBarH - Pad * 2f,
                TextGray);
            _pageBodyLbl.SetFontSize(13);

            const float FootH = 32f;
            BuildFooter(overlay, PanelH - OverlayTop - FootH);
        }

        private void ShowPage(int index)
        {
            if (_pageOverlayPtr == IntPtr.Zero) return;

            // Settings (index 1) ma własny dedykowany overlay
            if (index == 1) { ShowSettings(); return; }

            HideListingPage();
            HideDetail();

            _pageTitleLbl?.SetText(PageTitles[index]);
            _pageBodyLbl?.SetText(PageBodies[index]);
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_pageOverlayPtr)), true);
        }

        private void HidePage()
        {
            if (_pageOverlayPtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_pageOverlayPtr)), false);
        }


        // ══════════════════════════════════════════════════════════════════════
        //  LISTING PAGE OVERLAY
        // ══════════════════════════════════════════════════════════════════════

        private void BuildListingPage()
        {
            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, OverlayTop);
            S.Width(os, PanelW); S.Height(os, PanelH - OverlayTop);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _listingPagePtr = UIRuntime.GetPtr(overlay);

            // Top bar
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, 44f);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            // Back
            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Back", 12f, 6f, 110f, 32f, BtnDark, HideListingPage);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            // Title
            var titleLbl = _panel.AddLabelToContainer(
                topBar, "\U0001F697  Passenger Cars \u2014 active listings",
                130f, 0f, PanelW - 350f, 44f, OXLGreen);
            titleLbl.SetFontSize(17);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(titleLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            // Balance
            _listingMoneyLbl = _panel.AddLabelToContainer(
                topBar, "Balance: ---",
                PanelW - 220f, 0f, 210f, 44f,
                new Color(0.55f, 0.90f, 0.55f, 1f));
            _listingMoneyLbl.SetFontSize(15);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingMoneyLbl.GetRawPtr())),
                TextAnchor.MiddleRight);

            // Sep
            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, 44f);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(overlay, sep);

            // ── Stałe layoutu ─────────────────────────────────────────────────────
            const float FilterBarH = 42f;   // OXLFilterPanel.BarH — zwinięty pasek
            const float FilterTop = 45f;   // 44 topbar + 1 sep
            const float PaginationH = 46f;
            const float FootH = 32f;

            float rowsTop = FilterTop + FilterBarH;  // = 87f
            float availH = PanelH - OverlayTop - rowsTop - PaginationH - FootH;

            // ── Rows container ────────────────────────────────────────────────────
            var rowsVE = UIRuntime.NewVE();
            var rcs = UIRuntime.GetStyle(rowsVE);
            S.Position(rcs, "Absolute");
            S.Left(rcs, 0f); S.Top(rcs, rowsTop);
            S.Width(rcs, PanelW); S.Height(rcs, availH);
            S.Overflow(rcs, "Hidden");
            UIRuntime.AddChild(overlay, rowsVE);
            _listingRowsContainerPtr = UIRuntime.GetPtr(rowsVE);

            // ── Pagination ────────────────────────────────────────────────────────
            float paginationTop = PanelH - OverlayTop - FootH - PaginationH;
            BuildPaginationBar(overlay, paginationTop);

            // ── Footer ────────────────────────────────────────────────────────────
            BuildFooter(overlay, PanelH - OverlayTop - FootH);

            // ── Filter panel OSTATNI → renderuje się nad rowsVE w z-order ────────
            _filterPanel = new OXLFilterPanel();
            _filterPanel.Build(_panel, overlay, FilterTop);
            _filterPanel.OnFiltersApplied += () =>
            {
                _filteredListings = ApplyFilterCriteria(_filterPanel.Current);
                _currentPage = 0;
                RefreshListings();
            };
        }

        // ── Single auction row ────────────────────────────────────────────────
        private void BuildListingRow(object container, CarListing listing, float yOffset)
        {
            const float Pad = 16f;
            const float ImgW = 125f;
            const float ImgH = 62f;
            const float RightW = 262f; // stars + price + buy w jednej linii

            // ── Row background ────────────────────────────────────────────────────
            var rowBg = UIRuntime.NewVE();
            var rbs = UIRuntime.GetStyle(rowBg);
            S.Position(rbs, "Absolute");
            S.Left(rbs, 0f); S.Top(rbs, yOffset);
            S.Width(rbs, PanelW); S.Height(rbs, RowH);
            S.BgColor(rbs, new Color(0.042f, 0.066f, 0.114f, 1f));
            UIRuntime.AddChild(container, rowBg);
            var rowPtr = UIRuntime.GetPtr(rowBg);

            _panel.WireHover(rowPtr,
                new Color(0.042f, 0.066f, 0.114f, 1f),
                new Color(0.070f, 0.110f, 0.180f, 1f),
                new Color(0.090f, 0.140f, 0.220f, 1f));
            _panel.WireClick(rowPtr, () =>
            {
                if (_buyClickConsumed) { _buyClickConsumed = false; return; }
                ShowDetail(listing);
            });

            // ── Separator — FIXED: yOffset uwzględniony ───────────────────────────
            var rowSep = UIRuntime.NewVE();
            var rss = UIRuntime.GetStyle(rowSep);
            S.Position(rss, "Absolute");
            S.Left(rss, Pad); S.Top(rss, yOffset + RowH - 1f);
            S.Width(rss, PanelW - Pad * 2f); S.Height(rss, 1f);
            S.BgColor(rss, new Color(0.15f, 0.22f, 0.32f, 0.35f));
            UIRuntime.AddChild(container, rowSep);

            // ── Thumbnail ─────────────────────────────────────────────────────────
            var imgBox = UIRuntime.NewVE();
            var ibs = UIRuntime.GetStyle(imgBox);
            S.Position(ibs, "Absolute");
            S.Left(ibs, Pad); S.Top(ibs, (RowH - ImgH) / 2f);
            S.Width(ibs, ImgW); S.Height(ibs, ImgH);
            S.BgColor(ibs, new Color(0.08f, 0.13f, 0.20f, 1f));
            S.BorderRadius(ibs, 6f);
            S.BorderWidth(ibs, 1f);
            S.BorderColor(ibs, new Color(0.15f, 0.25f, 0.38f, 0.7f));
            UIRuntime.AddChild(rowBg, imgBox);

            Texture2D thumbTex = _photoLoader?.GetThumbnail(listing);
            if (thumbTex != null)
            {
                UIRuntime.SetBackgroundImage(imgBox, thumbTex);
            }
            else
            {
                // fallback emoji
                var iconLbl = _panel.AddLabelToContainer(
                    imgBox, "\U0001F697", 0f, 0f, ImgW, ImgH,
                    new Color(0.28f, 0.40f, 0.52f, 1f));
                iconLbl.SetFontSize(24);
            }

            // ── Content area ──────────────────────────────────────────────────────
            float contentX = Pad + ImgW + 14f;
            float contentW = PanelW - contentX - RightW - Pad * 2f;

            // Tytuł — największy
            var titleLbl = _panel.AddLabelToContainer(
                rowPtr, $"{listing.Make} {listing.Model}  \u2022  {listing.Year}",
                contentX, 8f, contentW, 26f, Color.white);
            titleLbl.SetFontSize(18);

            // Nota sprzedawcy — bardziej widoczna
            string note = listing.SellerNote.Length > 95
                ? listing.SellerNote.Substring(0, 92) + "..."
                : listing.SellerNote;
            var noteLbl = _panel.AddLabelToContainer(
                rowPtr, $"\"{note}\"",
                contentX, 34f, contentW, 18f,
                new Color(0.72f, 0.76f, 0.80f, 1f));
            noteLbl.SetFontSize(12);

            // Dolna linia: timer · lokacja · przebieg · rok · dostawa
            float rem = listing.ExpiresAt - _listings.GameTime;
            Color metaColor = rem < 120f
                ? new Color(0.95f, 0.55f, 0.20f, 1f)
                : new Color(0.45f, 0.65f, 0.85f, 1f);

            var metaLbl = _panel.AddLabelToContainer(
                rowPtr, FormatMetaLine(listing),
                contentX, 60f, contentW, 18f, metaColor);
            metaLbl.SetFontSize(11);
            _timerLabels[listing.InternalId] = metaLbl;

            // ── Prawa strona: [★★★★★] [$22,250] [BUY ▶] — jedna linia ───────────
            float rightX = PanelW - RightW - Pad;
            float lineY = (RowH - 34f) / 2f;  // środek pionowy wiersza

            // Gwiazdki
            var starsLbl = _panel.AddLabelToContainer(
                rowPtr, FormatStars(listing.SellerRating),
                rightX, lineY + 2f, 68f, 30f,
                StarColor(listing.SellerRating));
            starsLbl.SetFontSize(13);

            // Cena
            var priceLbl = _panel.AddLabelToContainer(
                rowPtr, $"${listing.Price:N0}",
                rightX + 70f, lineY, 98f, 34f, OXLGreen);
            priceLbl.SetFontSize(19);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(priceLbl.GetRawPtr())),
                TextAnchor.MiddleLeft);

            // BUY button
            var buyPtr = _panel.AddButtonToContainer(
                rowPtr, "BUY \u25BA",
                rightX + 170f, lineY, 92f, 34f,
                OXLGreen,
                () => { _buyClickConsumed = true; ExecutePurchase(listing); });
            _panel.WireHover(buyPtr,
                OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
        }

        // ── Pagination bar ────────────────────────────────────────────────────
        private void BuildPaginationBar(object overlay, float yTop)
        {
            const float BarH = 46f;
            const float BtnW = 150f;

            var bar = UIRuntime.NewVE();
            var bs = UIRuntime.GetStyle(bar);
            S.Position(bs, "Absolute");
            S.Left(bs, 0f); S.Top(bs, yTop);
            S.Width(bs, PanelW); S.Height(bs, BarH);
            S.BgColor(bs, new Color(0.035f, 0.055f, 0.090f, 1f));
            UIRuntime.AddChild(overlay, bar);

            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, BarH - 1f);   // ← was 0f, now BarH-1
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(bar, sep);

            float cx = PanelW / 2f;

            var prevPtr = _panel.AddButtonToContainer(
                bar, "\u25C4  Previous", cx - BtnW - 70f, 7f, BtnW, 32f, BtnDark,
                () => { if (_currentPage > 0) { _currentPage--; RefreshListings(); } });
            _panel.WireHover(prevPtr, BtnDark, BtnDarkHi, SearchBdr);

            _pageCountLabel = _panel.AddLabelToContainer(
                bar, "1 / 1", cx - 30f, 0f, 60f, BarH, TextGray);
            _pageCountLabel.SetFontSize(13);

            var nextPtr = _panel.AddButtonToContainer(
                bar, "Next  \u25BA", cx + 70f, 7f, BtnW, 32f, BtnDark,
                () =>
                {
                    if (_currentPage < TotalPages() - 1) { _currentPage++; RefreshListings(); }
                });
            _panel.WireHover(nextPtr, BtnDark, BtnDarkHi, SearchBdr);
        }

        // ── Rebuild current page ──────────────────────────────────────────────
        private void RefreshListings()
        {
            try
            {
                int bal = (int)Il2CppCMS.Shared.SharedGameDataManager.Instance.money;
                _listingMoneyLbl?.SetText($"Balance:  ${bal:N0}");
            }
            catch { _listingMoneyLbl?.SetText("Balance: ---"); }


            if (_listingRowsContainerPtr == IntPtr.Zero) return;

            var container = UIRuntime.WrapVE(_listingRowsContainerPtr);
            UIRuntime.VisualElementType.GetMethod("Clear")?.Invoke(container, null);
            _timerLabels.Clear();

            // Posortowana kopia — zawsze lokalna, nie nadpisuje _filteredListings
            var all = (_filteredListings ?? _listings.ActiveListings)
                .OrderBy(l => l.ExpiresAt)
                .ToList();

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(all.Count / (float)RowsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
            _pageCountLabel?.SetText($"{_currentPage + 1} / {totalPages}");

            if (all.Count == 0)
            {
                string emptyMsg = _filteredListings != null
                    ? "\u2014  No results for selected criteria  \u2014"
                    : "\u2014  No active listings  \u2014";

                var lbl = Activator.CreateInstance(UIRuntime.LabelType);
                var s = UIRuntime.GetStyle(lbl);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, 260f);
                S.Width(s, PanelW); S.Height(s, 40f);
                S.Color(s, TextDim); S.Font(s);
                S.TextAlign(s, TextAnchor.MiddleCenter);
                UIRuntime.LabelType.GetProperty("text").SetValue(lbl, emptyMsg);
                UIRuntime.AddChild(container, lbl);
                return;
            }

            int start = _currentPage * RowsPerPage;
            int end = Mathf.Min(start + RowsPerPage, all.Count);
            for (int i = start; i < end; i++)
                BuildListingRow(container, all[i], (i - start) * (RowH + RowGap));
        }

        private int TotalPages()
        {
            int count = (_filteredListings ?? _listings.ActiveListings).Count;
            return Mathf.Max(1, Mathf.CeilToInt(count / (float)RowsPerPage));
        }

        // ── Timer refresh ─────────────────────────────────────────────────────
        private void UpdateTimers()
        {
            if (_timerLabels.Count == 0) return;
            foreach (var listing in _listings.ActiveListings)
            {
                if (!_timerLabels.TryGetValue(listing.InternalId, out var lbl)) continue;
                float rem = listing.ExpiresAt - _listings.GameTime;
                lbl.SetText(FormatMetaLine(listing));
                lbl.SetColor(rem < 120f
                    ? new Color(0.95f, 0.55f, 0.20f, 1f)
                    : new Color(0.45f, 0.65f, 0.85f, 1f));
            }
            if (_detailListing != null)
                _detailTimer?.SetText(FormatTimer(_detailListing));
        }

        private string FormatTimer(CarListing listing)
        {
            float rem = listing.ExpiresAt - _listings.GameTime;
            if (rem <= 0f) return "Auction ended";

            return FormatTimeRemaining(rem);
        }


        private static string FormatTimeRemaining(float seconds)
        {
            if (seconds <= 0f) return "Auction ended";

            int total = (int)seconds;
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;

            if (total >= 86400)  // 24h+
            {
                int days = total / 86400;
                return days == 1 ? "1 day left" : $"{days} days left";
            }

            if (total >= 3600)   // 1h+
            {
                return h == 1 ? "1 hour left" : $"{h} hours left";
            }

            return $"{m}:{s:D2}";
        }


        // ── Listing page visibility ───────────────────────────────────────────
        private void ShowListingPage()
        {
            if (_listingPagePtr == IntPtr.Zero) return;
            _currentPage = 0;
            RefreshListings();
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingPagePtr)), true);
            _listingPageWasOpen = true;
        }

        private void HideListingPage()
        {
            if (_listingPagePtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingPagePtr)), false);
            _listingPageWasOpen = false;
        }


        // ══════════════════════════════════════════════════════════════════════
        //  DETAIL OVERLAY
        // ══════════════════════════════════════════════════════════════════════

        private void BuildDetailOverlay()
        {
            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, OverlayTop);
            S.Width(os, PanelW); S.Height(os, PanelH - OverlayTop);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _detailOverlayPtr = UIRuntime.GetPtr(overlay);

            // ── Top bar ───────────────────────────────────────────────────────────
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, 44f);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Listings", 12f, 6f, 140f, 32f, BtnDark, HideDetail);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            // Balance — prawy górny róg (gdzie był timer)
            _detailBalanceLbl = _panel.AddLabelToContainer(
                topBar, "Balance: ---", PanelW - 240f, 0f, 228f, 44f,
                new Color(0.55f, 0.90f, 0.55f, 1f));
            _detailBalanceLbl.SetFontSize(14);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailBalanceLbl.GetRawPtr())),
                TextAnchor.MiddleRight);

            var topSep = UIRuntime.NewVE();
            var tss = UIRuntime.GetStyle(topSep);
            S.Position(tss, "Absolute");
            S.Left(tss, 0f); S.Top(tss, 44f);
            S.Width(tss, PanelW); S.Height(tss, 1f);
            S.BgColor(tss, Border);
            UIRuntime.AddChild(overlay, topSep);

            // ════════════════════════════════════════════════════════════════
            //  LAYOUT  Left=image 820px   Right=info od x=868
            // ════════════════════════════════════════════════════════════════
            const float ContentTop = 56f;
            const float ImgX = 24f;
            const float ImgW = 820f;
            const float ImgH = 462f;
            const float RightX = ImgX + ImgW + 24f;      // 868
            const float RightW = PanelW - RightX - 12f;  // ~576

            // ── GALLERY — główne zdjęcie + strzałki + miniatury ──────────────────────
            var mainImgBox = UIRuntime.NewVE();
            var mibs = UIRuntime.GetStyle(mainImgBox);
            S.Position(mibs, "Absolute");
            S.Left(mibs, ImgX); S.Top(mibs, ContentTop);
            S.Width(mibs, ImgW); S.Height(mibs, ImgH);
            S.BgColor(mibs, new Color(0.05f, 0.08f, 0.13f, 1f));
            S.BorderRadius(mibs, 6f);
            UIRuntime.AddChild(overlay, mainImgBox);
            _galleryMainImgPtr = UIRuntime.GetPtr(mainImgBox);

            // Strzałka wstecz
            _galleryPrevBtnPtr = _panel.AddButtonToContainer(
                mainImgBox, "‹",
                4f, (ImgH - 48f) / 2f, 36f, 48f,
                new Color(0f, 0f, 0f, 0.45f),
                () => GalleryStep(-1));
            StyleGalleryArrow(_galleryPrevBtnPtr);

            // Strzałka naprzód
            _galleryNextBtnPtr = _panel.AddButtonToContainer(
                mainImgBox, "›",
                ImgW - 40f, (ImgH - 48f) / 2f, 36f, 48f,
                new Color(0f, 0f, 0f, 0.45f),
                () => GalleryStep(1));
            StyleGalleryArrow(_galleryNextBtnPtr);

            // Licznik zdjęć
            _galleryCounterLbl = _panel.AddLabelToContainer(
                mainImgBox, "1 / 1",
                ImgW - 60f, ImgH - 22f, 56f, 18f,
                new Color(1f, 1f, 1f, 0.55f));
            _galleryCounterLbl.SetFontSize(10);

            // Miniatury pod głównym zdjęciem
            var thumbsRow = UIRuntime.NewVE();
            var trs = UIRuntime.GetStyle(thumbsRow);
            S.Position(trs, "Absolute");
            S.Left(trs, ImgX); S.Top(trs, ContentTop + ImgH + 6f);
            S.Width(trs, ImgW); S.Height(trs, ThumbH);
            UIRuntime.AddChild(overlay, thumbsRow);
            _galleryThumbsRowPtr = UIRuntime.GetPtr(thumbsRow);

            // ── Specs panel under thumbnails ──────────────────────────────────────
            const float SpecsTop = ContentTop + ImgH + 6f + ThumbH + 10f; // po miniaturach
            float specsH = PanelH - OverlayTop - SpecsTop - 32f;          // do footera

            var specsContainer = UIRuntime.NewVE();
            var sps = UIRuntime.GetStyle(specsContainer);
            S.Position(sps, "Absolute");
            S.Left(sps, ImgX);
            S.Top(sps, SpecsTop);
            S.Width(sps, ImgW);          // ta sama szerokość co galeria: 820px
            S.Height(sps, specsH);
            S.Overflow(sps, "Hidden");
            UIRuntime.AddChild(overlay, specsContainer);
            _detailSpecsContainerPtr = UIRuntime.GetPtr(specsContainer);

            // ── RIGHT COLUMN ──────────────────────────────────────────────────
            float ry = ContentTop;

            // Nazwa + Cena w jednej linii
            const float TitleRowH = 44f;

            _detailTitle = _panel.AddLabelToContainer(
                overlay, "", RightX, ry, RightW * 0.58f, TitleRowH, Color.white);
            _detailTitle.SetFontSize(24);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailTitle.GetRawPtr())),
                TextAnchor.MiddleLeft);

            _detailPrice = _panel.AddLabelToContainer(
                overlay, "", RightX + RightW * 0.58f, ry, RightW * 0.42f, TitleRowH, OXLGreen);
            _detailPrice.SetFontSize(26);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailPrice.GetRawPtr())),
                TextAnchor.MiddleRight);
            ry += TitleRowH + 2f;

            // Expires in — pod nazwą/ceną
            _detailTimer = _panel.AddLabelToContainer(
                overlay, "", RightX, ry, RightW, 20f,
                new Color(0.45f, 0.65f, 0.85f, 1f));
            _detailTimer.SetFontSize(12);
            ry += 26f;

            // ── BUY NOW ───────────────────────────────────────────────────────
            _detailBuyPtr = _panel.AddButtonToContainer(
                overlay, "BUY NOW  \u25BA",
                RightX, ry, RightW, 48f,
                OXLGreen,
                () => { if (_detailListing == null) return; ExecutePurchase(_detailListing); HideDetail(); });
            _panel.WireHover(_detailBuyPtr,
                OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
            ry += 56f;

            // ── Seller card ───────────────────────────────────────────────────
            const float CardH = 72f;
            const float AvatarS = 64f;

            var sellerCard = UIRuntime.NewVE();
            var scs = UIRuntime.GetStyle(sellerCard);
            S.Position(scs, "Absolute");
            S.Left(scs, RightX); S.Top(scs, ry);
            S.Width(scs, RightW); S.Height(scs, CardH);
            S.BgColor(scs, new Color(0.042f, 0.072f, 0.115f, 1f));
            S.BorderRadius(scs, 8f);
            S.BorderWidth(scs, 1f);
            S.BorderColor(scs, new Color(0.15f, 0.28f, 0.20f, 0.45f));
            UIRuntime.AddChild(overlay, sellerCard);

            var avatarBox = UIRuntime.NewVE();
            var avs = UIRuntime.GetStyle(avatarBox);
            S.Position(avs, "Absolute");
            S.Left(avs, 6f); S.Top(avs, (CardH - AvatarS) / 2f);
            S.Width(avs, AvatarS); S.Height(avs, AvatarS);
            S.BgColor(avs, new Color(0.08f, 0.14f, 0.22f, 1f));
            S.BorderRadius(avs, 6f);
            S.BorderWidth(avs, 1f);
            S.BorderColor(avs, new Color(0.18f, 0.32f, 0.22f, 0.5f));
            UIRuntime.AddChild(sellerCard, avatarBox);

            var avatarLbl = _panel.AddLabelToContainer(
                avatarBox, "?", 0f, 0f, AvatarS, AvatarS,
                new Color(0.30f, 0.45f, 0.35f, 0.8f));
            avatarLbl.SetFontSize(28);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(avatarLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            float tx = AvatarS + 14f;

            var sellerTypeLbl = _panel.AddLabelToContainer(
                sellerCard, "PRIVATE SELLER",
                tx, 8f, 200f, 14f,
                new Color(0.38f, 0.55f, 0.42f, 0.80f));
            sellerTypeLbl.SetFontSize(9);

            var sellerNameLbl = _panel.AddLabelToContainer(
                sellerCard, "Anonymous",
                tx, 22f, 180f, 22f, Color.white);
            sellerNameLbl.SetFontSize(15);

            var sellerStarsLbl = _panel.AddLabelToContainer(
                sellerCard, FormatStars(3),
                tx, 46f, 100f, 18f, StarColor(3));
            sellerStarsLbl.SetFontSize(13);
            _detailSellerStars = sellerStarsLbl;

            _detailListedLbl = _panel.AddLabelToContainer(
                sellerCard, "Member since 2024",
                tx + 108f, 46f, 180f, 18f, TextGray);
            _detailListedLbl.SetFontSize(10);

            var msgPtr = _panel.AddButtonToContainer(
                sellerCard, "\u2709  Message",
                RightW - 116f, (CardH - 32f) / 2f, 108f, 32f,
                new Color(0.06f, 0.12f, 0.22f, 1f),
                () => { /* TODO */ });
            _panel.WireHover(msgPtr,
                new Color(0.06f, 0.12f, 0.22f, 1f),
                new Color(0.10f, 0.20f, 0.34f, 1f),
                SearchBdr);
            ry += CardH + 8f;

            // ── Location card ─────────────────────────────────────────────────
            const float LocCardH = 72f;

            var locCard = UIRuntime.NewVE();
            var lcs = UIRuntime.GetStyle(locCard);
            S.Position(lcs, "Absolute");
            S.Left(lcs, RightX); S.Top(lcs, ry);
            S.Width(lcs, RightW); S.Height(lcs, LocCardH);
            S.BgColor(lcs, new Color(0.042f, 0.072f, 0.115f, 1f));
            S.BorderRadius(lcs, 8f);
            S.BorderWidth(lcs, 1f);
            S.BorderColor(lcs, new Color(0.15f, 0.28f, 0.20f, 0.45f));
            UIRuntime.AddChild(overlay, locCard);

            var pinBox = UIRuntime.NewVE();
            var pbs = UIRuntime.GetStyle(pinBox);
            S.Position(pbs, "Absolute");
            S.Left(pbs, 6f); S.Top(pbs, (LocCardH - AvatarS) / 2f);
            S.Width(pbs, AvatarS); S.Height(pbs, AvatarS);
            S.BgColor(pbs, new Color(0.08f, 0.14f, 0.22f, 1f));
            S.BorderRadius(pbs, 6f);
            S.BorderWidth(pbs, 1f);
            S.BorderColor(pbs, new Color(0.18f, 0.32f, 0.22f, 0.5f));
            UIRuntime.AddChild(locCard, pinBox);

            var pinLbl = _panel.AddLabelToContainer(
                pinBox, "\U0001F4CD", 0f, 0f, AvatarS, AvatarS,
                new Color(0.22f, 0.59f, 0.34f, 0.9f));
            pinLbl.SetFontSize(22);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(pinLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            var locHeader = _panel.AddLabelToContainer(
                locCard, "LOCATION",
                AvatarS + 14f, 10f, 200f, 14f,
                new Color(0.38f, 0.55f, 0.42f, 0.80f));
            locHeader.SetFontSize(9);

            _detailLocationLbl = _panel.AddLabelToContainer(
                locCard, "", AvatarS + 14f, 26f, 260f, 24f, Color.white);
            _detailLocationLbl.SetFontSize(16);

            // Delivery — prawa strona, większe i jaśniejsze
            _detailYear = _panel.AddLabelToContainer(
                locCard, "", RightW - 210f, 0f, 202f, LocCardH,
                new Color(0.65f, 0.72f, 0.78f, 1f));
            _detailYear.SetFontSize(13);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailYear.GetRawPtr())),
                TextAnchor.MiddleRight);
            ry += LocCardH + 8f;

            // ── Seller note — rozciągnięty do footera ─────────────────────────────
            const float FooterH = 32f;
            const float BottomPad = 8f;
            float panelBottom = PanelH - OverlayTop - FooterH - BottomPad;
            float noteH = panelBottom - ry;
            if (noteH < 60f) noteH = 60f;

            var noteBox = UIRuntime.NewVE();
            var nbs = UIRuntime.GetStyle(noteBox);
            S.Position(nbs, "Absolute");
            S.Left(nbs, RightX); S.Top(nbs, ry);
            S.Width(nbs, RightW); S.Height(nbs, noteH);
            S.BgColor(nbs, new Color(0.040f, 0.072f, 0.118f, 1f));
            S.BorderRadius(nbs, 8f);
            S.BorderWidth(nbs, 1f);
            S.BorderColor(nbs, new Color(0.22f, 0.52f, 0.32f, 0.45f));
            UIRuntime.AddChild(overlay, noteBox);

            var quoteIcon = _panel.AddLabelToContainer(
                noteBox, "\u201C", 10f, 2f, 26f, 28f,
                new Color(0.22f, 0.59f, 0.34f, 0.45f));
            quoteIcon.SetFontSize(30);

            _detailSellerNote = _panel.AddLabelToContainer(
                noteBox, "",
                30f, 10f, RightW - 42f, noteH - 20f,
                new Color(0.80f, 0.84f, 0.86f, 1f));
            _detailSellerNote.SetFontSize(13);



            const float FootH = 32f;
            BuildFooter(overlay, PanelH - OverlayTop - FootH);
        }


        private void StyleGalleryArrow(IntPtr ptr)
        {
            var st = UIRuntime.GetStyle(UIRuntime.WrapVE(ptr));
            S.BorderRadius(st, 6f);
            S.FontSize(st, 22);
            // Tekst wyśrodkowany
            S.TextAlign(st, TextAnchor.MiddleCenter);
        }

        private void GalleryStep(int delta)
        {
            if (_galleryPhotos.Count == 0) return;
            _galleryIndex = (_galleryIndex + delta + _galleryPhotos.Count) % _galleryPhotos.Count;

            // Aktualizuj counter i thumbs od razu
            _galleryCounterLbl?.SetText($"{_galleryIndex + 1} / {_galleryPhotos.Count}");
            RefreshGalleryThumbs();

            // Animowane przejście głównego zdjęcia
            if (_galleryMainImgPtr != IntPtr.Zero)
            {
                var mainVE = UIRuntime.WrapVE(_galleryMainImgPtr);
                Texture2D next = _galleryPhotos[_galleryIndex];
                MelonCoroutines.Start(GalleryFadeSwap(mainVE, next));
            }
        }
        private void RefreshGallery()
        {
            if (_galleryMainImgPtr == IntPtr.Zero) return;

            // Główne zdjęcie
            var mainVE = UIRuntime.WrapVE(_galleryMainImgPtr);
            Texture2D tex = _galleryPhotos.Count > 0 ? _galleryPhotos[_galleryIndex] : null;
            UIRuntime.SetBackgroundImage(mainVE, tex);

            // Licznik
            _galleryCounterLbl?.SetText($"{_galleryIndex + 1} / {Mathf.Max(1, _galleryPhotos.Count)}");

            // Strzałki
            bool moreThanOne = _galleryPhotos.Count > 1;
            if (_galleryPrevBtnPtr != IntPtr.Zero)
                S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_galleryPrevBtnPtr)), moreThanOne);
            if (_galleryNextBtnPtr != IntPtr.Zero)
                S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_galleryNextBtnPtr)), moreThanOne);

            // Miniaturki
            RefreshGalleryThumbs();
        }

        private void RefreshGalleryThumbs()
        {
            if (_galleryThumbsRowPtr == IntPtr.Zero) return;
            var thumbsVE = UIRuntime.WrapVE(_galleryThumbsRowPtr);
            UIRuntime.VisualElementType.GetMethod("Clear")?.Invoke(thumbsVE, null);

            if (_galleryPhotos.Count == 0) return;

            float totalThumbsW = _galleryPhotos.Count * ThumbW + (_galleryPhotos.Count - 1) * ThumbGap;
            float startX = (ImgW - totalThumbsW) / 2f;

            for (int i = 0; i < _galleryPhotos.Count; i++)
            {
                int idx = i;
                bool isActive = (idx == _galleryIndex);

                var thumb = UIRuntime.NewVE();
                var ts = UIRuntime.GetStyle(thumb);
                S.Position(ts, "Absolute");
                S.Left(ts, startX + idx * (ThumbW + ThumbGap));
                S.Top(ts, 0f);
                S.Width(ts, ThumbW);
                S.Height(ts, ThumbH);
                S.BorderRadius(ts, 4f);
                S.BorderWidth(ts, isActive ? 2f : 1f);
                S.BorderColor(ts, isActive ? OXLGreen : new Color(0.20f, 0.30f, 0.42f, 0.60f));
                S.BgColor(ts, new Color(0.07f, 0.11f, 0.18f, 1f));
                UIRuntime.SetBackgroundImage(thumb, _galleryPhotos[idx]);
                UIRuntime.AddChild(thumbsVE, thumb);

                var thumbPtr = UIRuntime.GetPtr(thumb);
                _panel.WireClick(thumbPtr, () => { _galleryIndex = idx; RefreshGallery(); });
                _panel.WireHover(thumbPtr,
                    new Color(0.07f, 0.11f, 0.18f, 1f),
                    new Color(0.12f, 0.18f, 0.28f, 1f),
                    OXLGreen);
            }
        }

        private void ShowDetail(CarListing listing)
        {
            _detailListing = listing;

            // ── Ładuj zdjęcia lazy ────────────────────────────────────────────────
            _galleryPhotos = listing.PhotoFiles.Count > 0
     ? (_photoLoader?.GetPhotosFromFiles(listing.PhotoFiles) ?? new List<Texture2D>())
     : (_photoLoader?.GetPhotos(listing, preferMed: true) ?? new List<Texture2D>());
            _galleryIndex = 0;
            RefreshGallery();

            // ── Reszta pól (bez zmian) ────────────────────────────────────────────
            _detailTitle?.SetText($"{listing.Make} {listing.Model}");
            _detailPrice?.SetText($"${listing.Price:N0}");
            _detailTimer?.SetText(FormatTimer(listing));
            _detailSellerNote?.SetText($"\"{listing.SellerNote}\"");
            _detailLocationLbl?.SetText(listing.Location);
            _detailYear?.SetText($"Listed  \u00B7  ~{listing.DeliveryHours}h delivery");
            _detailSellerStars?.SetText(FormatStars(listing.SellerRating));
            _detailSellerStars?.SetColor(StarColor(listing.SellerRating));

            try
            {
                int bal = (int)Il2CppCMS.Shared.SharedGameDataManager.Instance.money;
                _detailBalanceLbl?.SetText($"Balance:  ${bal:N0}");
            }
            catch { _detailBalanceLbl?.SetText("Balance: ---"); }

            if (_detailSpecsContainerPtr != IntPtr.Zero)
            {
                var specsVE = UIRuntime.WrapVE(_detailSpecsContainerPtr);
                UIRuntime.VisualElementType.GetMethod("Clear")?.Invoke(specsVE, null);
                BuildDetailSpecsTags(specsVE, listing);
            }

            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailOverlayPtr)), true);
        }



        private void HideDetail()
        {
            if (_detailOverlayPtr == IntPtr.Zero) return;
            _detailListing = null;
            _galleryPhotos.Clear();     // ← puszcza referencje
            _photoLoader?.Evict();      // ← LRU cleanup
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailOverlayPtr)), false);
        }


        // ══════════════════════════════════════════════════════════════════════
        //  TEXTURE LOADING
        // ══════════════════════════════════════════════════════════════════════

        private void LoadIcons()
        {
            string buttonDir = Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "buttons");

            _passengerCars = TryLoadTexture(Path.Combine(buttonDir, "PassengerCars.png"));
            _carParts = TryLoadTexture(Path.Combine(buttonDir, "CarParts.png"));
            _workshopItems = TryLoadTexture(Path.Combine(buttonDir, "WorkshopItems.png"));
            _decorations = TryLoadTexture(Path.Combine(buttonDir, "Decorations.png"));



            string iconDir = Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "icons");

            _icoPrev = TryLoadTexture(Path.Combine(iconDir, "previous.png"));
            _icoNext = TryLoadTexture(Path.Combine(iconDir, "next.png"));
            _icoRef = TryLoadTexture(Path.Combine(iconDir, "ref.png"));
            _icoSecured = TryLoadTexture(Path.Combine(iconDir, "secured.png"));
            _icoMenu = TryLoadTexture(Path.Combine(iconDir, "ModMenu.png"));

            string carImgRoot = Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "CarImages");

            foreach (var folder in new[] { "DNB Censor", "Katagiri Tamago BP", "Luxor Streamliner Mk3", "Mayen M5", "Salem Aries MK3" })
            {
                string dir = Path.Combine(carImgRoot, folder);
                if (!Directory.Exists(dir)) continue;

                foreach (var file in Directory.GetFiles(dir, "*.png"))
                {
                    var tex = TryLoadTexture(file);
                    if (tex == null) continue;
                    // klucz = "DNB Censor/cyan_good"
                    string key = folder + "/" + Path.GetFileNameWithoutExtension(file);
                    _carImages[key] = tex;
                }
            }
        }


        private Texture2D TryLoadLogo()
        {
            string path = Path.Combine(
                Application.dataPath, "..", "Mods", "CMS2026_OXL",
                "Resources", "Images", "logo.png");
            return TryLoadTexture(path);
        }

        private Texture2D TryLoadTexture(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    OXLPlugin.Log.Msg($"[OXL] Not found: {path}");
                    return null;
                }

                // Wczytywanie surowych bajtów z pliku
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                // Konwersja na format akceptowany przez Il2Cpp
                var il2b = new Il2CppInterop.Runtime.InteropTypes
                               .Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                // Szukanie klasy ImageConversion w dostępnych paczkach (Reflection)
                var icType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName == "UnityEngine.ImageConversion");

                if (icType == null) return null;

                // Szukanie odpowiedniej metody LoadImage
                var loadImg = icType.GetMethods()
                    .FirstOrDefault(m => m.Name == "LoadImage"
                                      && m.GetParameters().Length == 2);

                if (loadImg == null) return null;

                // --- TUTAJ ZASTOSOWANE ZMIANY ---

                // Wykonujemy metodę i zapisujemy wynik do zmiennej bool
                bool result = (bool)loadImg.Invoke(null, new object[] { tex, il2b });

                if (result)
                {
                    // Zapobiegamy usunięciu tekstury przez silnik Unity podczas czyszczenia pamięci
                    tex.hideFlags = UnityEngine.HideFlags.DontUnloadUnusedAsset;
                }

                // Informacja w konsoli o tym, czy ładowanie się udało
                OXLPlugin.Log.Msg($"[OXL] LoadImage result={result}");

                // Zwracamy gotową teksturę lub null
                return result ? tex : null;

            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Texture load error ({Path.GetFileName(path)}): {ex.Message}");
                return null;
            }
        }

        // Dodaj do StyleGalleryArrow lub oddzielna metoda
        private void AnimateGalleryTransition(object mainVE, Texture2D newTex, bool goRight)
        {
            // Fade out → swap texture → fade in
            // UIToolkit nie ma built-in translate animation przez C# API w Il2Cpp,
            // więc używamy prostego coroutine opacity trick
            MelonCoroutines.Start(GalleryFadeSwap(mainVE, newTex));
        }

        private System.Collections.IEnumerator GalleryFadeSwap(object veObj, Texture2D newTex)
        {
            // Fade out (3 kroki)
            for (float a = 1f; a >= 0f; a -= 0.33f)
            {
                // UIToolkit opacity przez style
                var st = UIRuntime.GetStyle(veObj);
                // Tymczasowo przyciemniamy tło
                S.BgColor(st, new Color(0f, 0f, 0f, a * 0.7f));
                yield return new WaitForSeconds(0.03f);
            }

            UIRuntime.SetBackgroundImage(veObj, newTex);

            // Fade in
            for (float a = 0f; a <= 1f; a += 0.33f)
            {
                var st = UIRuntime.GetStyle(veObj);
                S.BgColor(st, new Color(0f, 0f, 0f, (1f - a) * 0.7f));
                yield return new WaitForSeconds(0.03f);
            }

            // Reset do czystego tła
            var finalSt = UIRuntime.GetStyle(veObj);
            S.BgColor(finalSt, new Color(0f, 0f, 0f, 0f));
        }

        




        // ── Seller rating helpers ─────────────────────────────────────────────────
        private static string FormatStars(int rating)
        {
            rating = Mathf.Clamp(rating, 1, 5);
            return new string('★', rating) + new string('☆', 5 - rating);
        }

        private static Color StarColor(int rating) => rating switch
        {
            5 => new Color(0.22f, 0.75f, 0.40f, 1f),
            4 => new Color(0.55f, 0.80f, 0.30f, 1f),
            3 => new Color(0.85f, 0.72f, 0.20f, 1f),
            2 => new Color(0.90f, 0.45f, 0.15f, 1f),
            _ => new Color(0.90f, 0.20f, 0.20f, 1f),
        };


        //footer builder
        private void BuildFooter(object container, float yTop)
        {
            const float FootH = 32f;

            var foot = UIRuntime.NewVE();
            var fs = UIRuntime.GetStyle(foot);
            S.Position(fs, "Absolute");
            S.Left(fs, 0f); S.Top(fs, yTop);
            S.Width(fs, PanelW); S.Height(fs, FootH);
            S.BgColor(fs, FooterBg);
            UIRuntime.AddChild(container, foot);

            var sepTop = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sepTop);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, 0f);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, new Color(0.15f, 0.42f, 0.24f, 0.45f));
            UIRuntime.AddChild(foot, sepTop);

            // Main footer text
            var lbl = _panel.AddLabelToContainer(
                foot,
                "OXL \u2014 Online eX-Owner Lies  \u00B7  v0.4.1  \u00B7  \u00A9 iBlaster  \u00B7  github.com/iBl4St3R/CMS2026-OXL",
                0f, 0f, PanelW, FootH,
                new Color(0.22f, 0.48f, 0.30f, 0.70f));
            lbl.SetFontSize(10);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())),
                        TextAnchor.MiddleCenter);

            // "powered by" — mniejszy, bardziej wyszarzony, przyklejony do prawej
            var poweredLbl = _panel.AddLabelToContainer(
                foot,
                "powered by UITK Framework 0.2.1",
                0f, 0f, PanelW - 10f, FootH,
                new Color(0.18f, 0.32f, 0.22f, 0.45f));
            poweredLbl.SetFontSize(9);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(poweredLbl.GetRawPtr())),
                        TextAnchor.MiddleRight);
        }


        // ══════════════════════════════════════════════════════════════════════
        //  DETAIL SPECS PANEL  — siatka kafelków pod galerią
        // ══════════════════════════════════════════════════════════════════════

        private void BuildDetailSpecsTags(object container, CarListing listing)
        {
            var spec = _specLoader?.Get(listing.InternalId) ?? new CarSpec();
            var d = spec.AutoDetected;

            // Condition text + colour
            string condText = listing.ApparentCondition >= 0.70f ? "Good"
                             : listing.ApparentCondition >= 0.40f ? "Fair" : "Poor";
            var condColor = listing.ApparentCondition >= 0.70f
                                 ? new Color(0.22f, 0.75f, 0.40f, 1f)
                                 : listing.ApparentCondition >= 0.40f
                                     ? new Color(0.85f, 0.72f, 0.20f, 1f)
                                     : new Color(0.90f, 0.28f, 0.18f, 1f);

            // Mileage string
            string mi = listing.Mileage >= 1000? $"{listing.Mileage / 1000}k mi" : $"{listing.Mileage} mi";

            // Tile definitions: (label, value, widthWeight, accentColor?)
            // widthWeight: 1 = normal (~190px), 2 = wide (~390px)
            var tiles = new (string label, string value, int w, Color? accent)[]
            {
        // Row 1 — identity
        ("MODEL",        $"{listing.Make} {listing.Model}", 2, null),
        ("YEAR",          listing.Year.ToString(),          1, null),
        ("PLATE",         listing.Registration,             1, null),

        // Row 2 — condition
        ("MILEAGE",       mi,                               1, null),
        ("CONDITION",     condText,                         1, condColor),
        ("COLOR",         CapFirst(listing.Color),          1, null),
        ("RARITY",        Or(d.Rarity, "—"),                1, RarityColor(d.Rarity)),

        // Row 3 — powertrain
        ("ENGINE",        Or(d.EngineType, "—"),            2, null),
        ("DRIVETRAIN",    Or(d.Drivetrain, "—"),            1, null),
        ("WEIGHT",        Or(d.Weight, "—"),                1, null),

        // Row 4 — power
        ("POWER",         Or(d.EnginePower, "—"),           2, null),
        ("TORQUE",        Or(d.EngineTorque, "—"),          2, null),

        // Row 5 — tyres
        ("TYRES FRONT",   Or(d.TireFront, "—"),             2, null),
        ("TYRES REAR",    Or(d.TireRear,  "—"),             2, null),
            };

            // Layout: total width = ImgW (820px), gap = 8px, 4 columns = ~195px each
            const float Gap = 8f;
            const float TileH = 52f;
            const float RowGap = 6f;
            const float UnitW = (820f - Gap * 3f) / 4f;  // ≈198px per unit
            const float PadLeft = 12f;
            const float PadTop = 6f;

            float cx = 0f, cy = 0f;

            foreach (var (label, value, ww, accent) in tiles)
            {
                float tileW = UnitW * ww + Gap * (ww - 1);

                // Wrap if doesn't fit
                if (cx > 0 && cx + tileW > 820f + 0.5f)
                {
                    cx = 0f;
                    cy += TileH + RowGap;
                }

                // Background card
                var card = UIRuntime.NewVE();
                var cs = UIRuntime.GetStyle(card);
                S.Position(cs, "Absolute");
                S.Left(cs, cx); S.Top(cs, cy);
                S.Width(cs, tileW); S.Height(cs, TileH);
                S.BgColor(cs, new Color(0.042f, 0.072f, 0.115f, 1f));
                S.BorderRadius(cs, 6f);
                S.BorderWidth(cs, 1f);
                S.BorderColor(cs, new Color(0.16f, 0.30f, 0.22f, 0.50f));
                UIRuntime.AddChild(container, card);

                // Label (small, dimmed header)
                var lbl = _panel.AddLabelToContainer(
                    card, label,
                    PadLeft, PadTop, tileW - PadLeft, 16f,
                    new Color(0.35f, 0.50f, 0.40f, 0.80f));
                lbl.SetFontSize(9);

                // Value
                Color valueColor = accent ?? Color.white;
                var val = _panel.AddLabelToContainer(
                    card, value,
                    PadLeft, PadTop + 18f, tileW - PadLeft * 2f, TileH - PadTop - 20f,
                    valueColor);
                val.SetFontSize(13);

                // Color swatch — shown only for COLOR tile
                if (label == "COLOR")
                {
                    Color swatchCol = HexColor(listing.Color);
                    var swatch = UIRuntime.NewVE();
                    var ss = UIRuntime.GetStyle(swatch);
                    S.Position(ss, "Absolute");
                    S.Left(ss, tileW - 28f); S.Top(ss, (TileH - 18f) / 2f);
                    S.Width(ss, 18f); S.Height(ss, 18f);
                    S.BgColor(ss, swatchCol);
                    S.BorderRadius(ss, 3f);
                    S.BorderWidth(ss, 1f);
                    S.BorderColor(ss, new Color(1f, 1f, 1f, 0.15f));
                    UIRuntime.AddChild(card, swatch);
                }

                cx += tileW + Gap;
            }
        }


        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string Or(string s, string fallback) =>
            string.IsNullOrWhiteSpace(s) ? fallback : s;

        private static string CapFirst(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static Color? RarityColor(string rarity)
        {
            if (string.IsNullOrEmpty(rarity)) return null;
            string r = rarity.ToLower();
            if (r.Contains("rzadk") || r.Contains("rare"))
                return new Color(0.55f, 0.30f, 0.90f, 1f); // fiolet
            if (r.Contains("powsz") || r.Contains("common"))
                return new Color(0.50f, 0.60f, 0.65f, 1f); // szary
            return new Color(0.22f, 0.75f, 0.40f, 1f);     // zielony default
        }

        public static Color HexColor(string colorName)
        {
            return colorName?.ToLower() switch
            {
                "white" => new Color(0.93f, 0.93f, 0.93f),
                "black" => new Color(0.08f, 0.08f, 0.08f),
                "red" => new Color(0.80f, 0.10f, 0.10f),
                "red2" => new Color(0.80f, 0.07f, 0.12f),
                "darkred" => new Color(0.50f, 0.05f, 0.05f),
                "silver" => new Color(0.70f, 0.72f, 0.74f),
                "gray" => new Color(0.45f, 0.45f, 0.45f),
                "gray2" => new Color(0.53f, 0.55f, 0.55f),
                "darkgray" => new Color(0.30f, 0.30f, 0.30f),
                "charcoal" => new Color(0.22f, 0.22f, 0.22f),
                "nearblack" => new Color(0.10f, 0.10f, 0.10f),
                "nearblack2" => new Color(0.12f, 0.13f, 0.14f),
                "cyan" => new Color(0.10f, 0.75f, 0.80f),
                "lightblue" => new Color(0.50f, 0.70f, 0.90f),
                "lightblue2" => new Color(0.62f, 0.79f, 0.87f),
                "blue" => new Color(0.10f, 0.20f, 0.80f),
                "darkblue" => new Color(0.05f, 0.10f, 0.50f),
                "navy" => new Color(0.05f, 0.08f, 0.38f),
                "green" => new Color(0.10f, 0.60f, 0.20f),
                "darkgreen" => new Color(0.05f, 0.35f, 0.10f),
                "teal" => new Color(0.10f, 0.55f, 0.52f),
                "darkteal" => new Color(0.05f, 0.35f, 0.32f),
                "gold" => new Color(0.85f, 0.68f, 0.10f),
                "beige" => new Color(0.90f, 0.85f, 0.72f),
                "cream" => new Color(0.95f, 0.92f, 0.80f),
                "offwhite" => new Color(0.92f, 0.90f, 0.86f),
                "maroon" => new Color(0.50f, 0.05f, 0.15f),
                "darkmaroon" => new Color(0.32f, 0.02f, 0.08f),
                "rust" => new Color(0.70f, 0.28f, 0.05f),
                "purple" => new Color(0.55f, 0.10f, 0.80f),
                "darkpurple" => new Color(0.35f, 0.05f, 0.50f),
                "pink" => new Color(0.90f, 0.45f, 0.65f),
                _ => new Color(0.55f, 0.55f, 0.55f),
            };
        }



        //carspawn
        private void ExecutePurchase(CarListing listing)
        {
            // ── Walidacja PRZED jakąkolwiek zmianą stanu ──────────────────────────
            if (!GameBridge.HasFreeSlot())
            {
                ShowAlert("Parking lot is full.\nMake room before purchasing.");
                return;  // ← listing zostaje, kasa zostaje, nic się nie dzieje
            }

            int balance = 0;
            try { balance = (int)Il2CppCMS.Shared.SharedGameDataManager.Instance.money; }
            catch { }

            if (balance < listing.Price)
            {
                ShowAlert($"Not enough funds.\n\nRequired:  ${listing.Price:N0}\nAvailable: ${balance:N0}");
                return;  // ← listing zostaje
            }

            if (!_listings.ActiveListings.Contains(listing)) return;

            // ── Wszystko OK — dopiero teraz usuwamy listing ───────────────────────
            _listings.ActiveListings.Remove(listing);
            _listings.Save();

            OXLLog.Msg($"[OXL:BUY] ══ PURCHASE ══════════════════════");
            OXLLog.Msg($"[OXL:BUY] Car:      {listing.Make} {listing.Model} {listing.Year}");
            OXLLog.Msg($"[OXL:BUY] Price:    ${listing.Price:N0}");
            OXLLog.Msg($"[OXL:BUY] Seller:   {listing.Archetype} | {listing.SellerRating}★ | \"{listing.SellerNote}\"");
            OXLLog.Msg($"[OXL:BUY] Cond:     Apparent={listing.ApparentCondition:P0}  Actual={listing.ActualCondition:P0}");
            OXLLog.Msg($"[OXL:BUY] Faults:   {listing.Faults}");
            OXLLog.Msg($"[OXL:BUY] Mileage:  {listing.Mileage:N0} mi  |  Location: {listing.Location}  |  Delivery: ~{listing.DeliveryHours}h");
            OXLLog.Msg($"[OXL:BUY] Color:    {listing.Color}  |  Plate: {listing.Registration}");
            OXLLog.Msg($"[OXL:BUY] ════════════════════════════════════");
            OXLLog.Msg($"");
            OXLLog.Msg($"[OXL] Purchasing: {listing.Make} {listing.Model} for ${listing.Price}");

            GameBridge.SpawnCar(listing, result =>
            {
                OXLLog.Msg($"[OXL] SpawnResult: {result}");
                if (result == GameBridge.SpawnResult.Success)
                {
                    GameBridge.DeductMoney(listing.Price);
                }
                else
                {
                    // Spawn się nie udał — zwróć listing i pieniędzy nie ruszaj
                    _listings.ActiveListings.Add(listing);
                    ShowAlert($"Delivery failed ({result}).\nThe listing has been restored.");
                }
            });

            if (_filteredListings != null) ApplyFilters();
            RefreshListings();
        }

        // ── Alert overlay ─────────────────────────────────────────────────────────
        private IntPtr _alertOverlayPtr;
        private UILabelHandle _alertMessageLbl;

        private void BuildAlertOverlay()
        {
            const float W = 400f;
            const float H = 160f;
            float ax = (PanelW - W) / 2f;
            float ay = (PanelH - H) / 2f;

            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, ax); S.Top(os, ay);
            S.Width(os, W); S.Height(os, H);
            S.BgColor(os, new Color(0.06f, 0.09f, 0.15f, 1f));
            S.BorderRadius(os, 10f);
            S.BorderWidth(os, 1f);
            S.BorderColor(os, new Color(0.80f, 0.25f, 0.15f, 0.80f));
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _alertOverlayPtr = UIRuntime.GetPtr(overlay);

            _alertMessageLbl = _panel.AddLabelToContainer(
                overlay, "",
                16f, 20f, W - 32f, 72f,
                new Color(0.95f, 0.85f, 0.75f, 1f));
            _alertMessageLbl.SetFontSize(15);
            S.TextAlign(UIRuntime.GetStyle(
                UIRuntime.WrapVE(_alertMessageLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            var okPtr = _panel.AddButtonToContainer(
                overlay, "OK",
                W / 2f - 50f, H - 50f, 100f, 34f,
                new Color(0.15f, 0.22f, 0.35f, 1f),
                () => S.Display(UIRuntime.GetStyle(
                    UIRuntime.WrapVE(_alertOverlayPtr)), false));
            _panel.WireHover(okPtr,
                new Color(0.15f, 0.22f, 0.35f, 1f),
                new Color(0.22f, 0.32f, 0.50f, 1f),
                SearchBdr);
        }

        private void ShowAlert(string message)
        {
            if (_alertOverlayPtr == IntPtr.Zero) return;
            _alertMessageLbl?.SetText(message);
            S.Display(UIRuntime.GetStyle(
                UIRuntime.WrapVE(_alertOverlayPtr)), true);
        }

        private string ResolveImageKey(CarListing listing)
        {
            string condition = listing.ActualCondition >= 0.45f ? "good" : "worn";
            string preferred = $"{listing.ImageFolder}/{listing.Color}_{condition}";
            if (_carImages.ContainsKey(preferred)) return preferred;

            // fallback: jakikolwiek plik z tego folderu
            string any = _carImages.Keys.FirstOrDefault(k => k.StartsWith(listing.ImageFolder + "/"));
            return any;
        }

        private void BuildSettingsOverlay()
        {
            const float TopBarH = 44f;
            const float Pad = 24f;

            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, OverlayTop);
            S.Width(os, PanelW); S.Height(os, PanelH - OverlayTop);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _settingsOverlayPtr = UIRuntime.GetPtr(overlay);

            // ── Top bar ───────────────────────────────────────────────────────────
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, TopBarH);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Back", 12f, 6f, 110f, 32f, BtnDark, HideSettings);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            var titleLbl = _panel.AddLabelToContainer(
                topBar, "Settings", 130f, 0f, 300f, TopBarH, OXLGreen);
            titleLbl.SetFontSize(18);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(titleLbl.GetRawPtr())),
                TextAnchor.MiddleLeft);

            // Sep
            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, TopBarH);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(overlay, sep);

            // ── Section: Difficulty ───────────────────────────────────────────────
            float cy = TopBarH + Pad;

            var sectionLbl = _panel.AddLabelToContainer(
                overlay, "DIFFICULTY",
                Pad, cy, 300f, 18f,
                new Color(0.38f, 0.55f, 0.42f, 0.80f));
            sectionLbl.SetFontSize(10);
            cy += 22f;

            var descLbl = _panel.AddLabelToContainer(
                overlay,
                "Adjusts listing prices relative to the baseline.\n" +
                "Easy = prices 15% lower.  Normal = base prices (recommended).  Hard = prices 20% higher.",
                Pad, cy, PanelW - Pad * 2f, 40f,
                Color.white);
            descLbl.SetFontSize(12);
            cy += 50f;

            // ── Three difficulty buttons side by side ────────────────────────────
            const float BtnW = 200f;
            const float BtnH = 64f;
            const float BtnGap = 16f;
            float totalBtnsW = BtnW * 3 + BtnGap * 2;
            float bx = (PanelW - totalBtnsW) / 2f;

            BuildDiffButton(overlay, bx, cy, BtnW, BtnH, Difficulty.Easy,"EASY", "Listings 15% cheaper", new Color(0.20f, 0.60f, 0.35f, 1f),
            ref _diffEasyLbl, ref _diffEasyCardPtr);
            BuildDiffButton(overlay, bx + BtnW + BtnGap, cy, BtnW, BtnH, Difficulty.Normal,"NORMAL", "Base prices (recommended)", new Color(0.55f, 0.75f, 0.90f, 1f),
            ref _diffNormalLbl, ref _diffNormalCardPtr);
            BuildDiffButton(overlay, bx + (BtnW + BtnGap) * 2, cy, BtnW, BtnH, Difficulty.Hard,"HARD", "Listings 20% more expensive", new Color(0.90f, 0.45f, 0.20f, 1f),
            ref _diffHardLbl, ref _diffHardCardPtr);

            cy += BtnH + 32f;

            // ── Note ──────────────────────────────────────────────────────────────
            var noteLbl = _panel.AddLabelToContainer(
                overlay,
                "Change takes effect on the next generated listing.\n" +
                "Applies to newly generated listings only.\nActive listings keep their original prices.",
                Pad, cy, PanelW - Pad * 2f, 40f,
                Color.white);
            noteLbl.SetFontSize(11);


            cy += BtnH + 20f;  // po trzech przyciskach trudności (ta linia może już być)

            // ── Separator przed nawigacją ─────────────────────────────────────────────────
            var navSep = UIRuntime.NewVE();
            {
                var navSepSt = UIRuntime.GetStyle(navSep);
                S.Position(navSepSt, "Absolute");
                S.Left(navSepSt, 0f); S.Top(navSepSt, cy);
                S.Width(navSepSt, PanelW); S.Height(navSepSt, 1f);
                S.BgColor(navSepSt, Border);
            }
            UIRuntime.AddChild(overlay, navSep);
            UIRuntime.AddChild(overlay, navSep);
            cy += 1f + 20f;

            // ── Nawigacja do Listing Generation Settings ──────────────────────────────────
            var lgNavPtr = _panel.AddButtonToContainer(
                overlay,
                "⚙  Listing Generation Settings  →",
                Pad, cy, 320f, 40f, BtnDark,
                ShowListingGenSettings);
            _panel.WireHover(lgNavPtr, BtnDark, BtnDarkHi, SearchBdr);

            var lgNavDesc = _panel.AddLabelToContainer(
                overlay,
                "Max listings · generation rate · duration · archetype & level weights",
                Pad + 328f, cy, PanelW - Pad * 2f - 336f, 40f,
                TextGray);
            lgNavDesc.SetFontSize(11);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lgNavDesc.GetRawPtr())),
                TextAnchor.MiddleLeft);

            cy += 40f + 20f;

            const float FootH = 32f;
            BuildFooter(overlay, PanelH - OverlayTop - FootH);
        }

        private void BuildDiffButton(object overlay,float x, float y, float w, float h,Difficulty diff, string label, string sublabel,
            Color accentColor, ref UILabelHandle outLbl, ref IntPtr outCardPtr)
        {
            bool isActive = OXLSettings.CurrentDifficulty == diff;

            Color bgNormal = isActive
                ? new Color(0.06f, 0.14f, 0.10f, 1f)
                : new Color(0.05f, 0.09f, 0.15f, 1f);
            Color borderColor = isActive
                ? accentColor
                : new Color(0.15f, 0.28f, 0.20f, 0.45f);

            // Card background
            var card = UIRuntime.NewVE();
            var cs = UIRuntime.GetStyle(card);
            S.Position(cs, "Absolute");
            S.Left(cs, x); S.Top(cs, y);
            S.Width(cs, w); S.Height(cs, h);
            S.BgColor(cs, bgNormal);
            S.BorderRadius(cs, 8f);
            S.BorderWidth(cs, isActive ? 2f : 1f);
            S.BorderColor(cs, borderColor);
            UIRuntime.AddChild(overlay, card);
            var cardPtr = UIRuntime.GetPtr(card);

            outCardPtr = UIRuntime.GetPtr(card);


            _panel.WireHover(cardPtr, bgNormal,
                new Color(0.08f, 0.16f, 0.14f, 1f),
                accentColor with { a = 0.30f });

            _panel.WireClick(cardPtr, () =>
            {
                OXLSettings.Set(diff);
                RefreshDiffButtons();
            });

            // Label (difficulty name)
            var mainLbl = _panel.AddLabelToContainer(
                card, label, 0f, 10f, w, 26f, accentColor);
            mainLbl.SetFontSize(18);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(mainLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            // Sublabel (description)
            outLbl = _panel.AddLabelToContainer(
                card, sublabel, 0f, 38f, w, 18f, Color.white);
            outLbl.SetFontSize(11);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(outLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);
        }

        /// <summary>
        /// Rebuilds the visual state of all three difficulty buttons
        /// to reflect the current OXLSettings.CurrentDifficulty.
        /// Called after the player clicks a button.
        /// </summary>
        private void RefreshDiffButtons()
        {
            RefreshSingleDiffCard(
                _diffEasyCardPtr, _diffEasyLbl,
                Difficulty.Easy, new Color(0.20f, 0.60f, 0.35f, 1f));

            RefreshSingleDiffCard(
                _diffNormalCardPtr, _diffNormalLbl,
                Difficulty.Normal, new Color(0.55f, 0.75f, 0.90f, 1f));

            RefreshSingleDiffCard(
                _diffHardCardPtr, _diffHardLbl,
                Difficulty.Hard, new Color(0.90f, 0.45f, 0.20f, 1f));
        }

        private void RefreshSingleDiffCard(IntPtr cardPtr, UILabelHandle lbl,
                                    Difficulty diff, Color accentColor)
        {
            if (cardPtr == IntPtr.Zero) return;

            bool active = OXLSettings.CurrentDifficulty == diff;
            var st = UIRuntime.GetStyle(UIRuntime.WrapVE(cardPtr));

            S.BorderWidth(st, active ? 2f : 1f);
            S.BorderColor(st, active
                ? accentColor
                : new Color(0.15f, 0.28f, 0.20f, 0.45f));
            S.BgColor(st, active
                ? new Color(0.06f, 0.14f, 0.10f, 1f)
                : new Color(0.05f, 0.09f, 0.15f, 1f));

            lbl?.SetColor(active ? accentColor : new Color(0.55f, 0.62f, 0.68f, 1f));
        }

        private void ShowSettings()
        {
            if (_settingsOverlayPtr == IntPtr.Zero) return;
            HideListingPage();
            HideDetail();
            HidePage();
            RefreshDiffButtons();
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_settingsOverlayPtr)), true);
        }

        private void HideSettings()
        {
            if (_settingsOverlayPtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_settingsOverlayPtr)), false);
        }

        // ══════════════════════════════════════════════════════════════════════════════
        //  LISTING GENERATION SETTINGS OVERLAY
        //  Overlay 1 z 2: główne ustawienia + wagi archetypów.
        //  Przycisk "L1/L2/L3 ▶" na każdym wierszu otwiera Overlay 2.
        // ══════════════════════════════════════════════════════════════════════════════

        private void BuildListingGenSettingsOverlay()
        {
            const float TopBarH = 44f;
            const float FootH = 32f;
            const float Pad = 28f;
            const float Sg = 8f;
            const float Mg = 14f;
            const float SecH = 16f;
            const float CtrlH = 44f;

            var ov = UIRuntime.NewVE();
            {
                var os = UIRuntime.GetStyle(ov);
                S.Position(os, "Absolute");
                S.Left(os, 0f); S.Top(os, OverlayTop);
                S.Width(os, PanelW); S.Height(os, PanelH - OverlayTop);
                S.BgColor(os, PageBg);
                S.Overflow(os, "Hidden");
                S.Display(os, false);
            }
            _panel.AddOverlayToPanel(ov);
            _listingGenOverlayPtr = UIRuntime.GetPtr(ov);

            float scrollTop = TopBarH + 1f;
            float scrollViewH = PanelH - OverlayTop - FootH - scrollTop;

            // ── VIEWPORT (clipper) ────────────────────────────────────────────────
            var viewport = UIRuntime.NewVE();
            {
                var vs = UIRuntime.GetStyle(viewport);
                S.Position(vs, "Absolute");
                S.Left(vs, 0f);
                S.Top(vs, scrollTop);
                S.Width(vs, PanelW);
                S.Height(vs, scrollViewH);
                S.Overflow(vs, "Hidden");
            }
            UIRuntime.AddChild(ov, viewport);
            _lgViewportPtr = UIRuntime.GetPtr(viewport);

            // ── CONTENT (scrollowany) ─────────────────────────────────────────────
            var content = UIRuntime.NewVE();
            {
                var cs = UIRuntime.GetStyle(content);
                S.Position(cs, "Absolute");
                S.Left(cs, 0f);
                S.Top(cs, 0f);
                S.Width(cs, PanelW);
            }
            UIRuntime.AddChild(viewport, content);
            _lgContentPtr = UIRuntime.GetPtr(content);
            _lgScrollY = 0f;

            // Podpięcie wheel event na viewport
            LgWireScroll(viewport);

            object sc = content;

            // ── Footer i TopBar (poza viewport, bezpośrednio w ov) ───────────────
            BuildFooter(ov, PanelH - OverlayTop - FootH);
            LgBuildTopBar(ov, "Listing Generation Settings", HideListingGenSettings, out _);
            LgSep(ov, TopBarH);

            // ══ Zawartość — cy od 0 (relatywnie do content VE) ════════════════════
            float cy = Mg;

            LgSectionLabel(sc, "LISTING CAP", Pad, cy);
            cy += SecH + Sg;
            LgDescLabel(sc, "No new listings will be generated once the active count reaches this limit.", Pad, cy);
            cy += 22f + Sg;

            _lgMaxLbl = LgNumControl(sc, Pad, cy, PanelW - Pad * 2f, CtrlH,
                "Max listings on the board:", () => _draftMaxListings,
                v => _draftMaxListings = v, 1, 50, " listings");
            cy += CtrlH + Mg;
            LgSep(sc, cy); cy += 1f + Mg;

            LgSectionLabel(sc, "GENERATION RATE", Pad, cy); cy += SecH + Sg;
            LgDescLabel(sc, "Chance of generating a new listing each in-game hour.", Pad, cy);
            cy += 22f + Sg;

            _lgChanceLbl = LgNumControl(sc, Pad, cy, PanelW - Pad * 2f, CtrlH,
                "Generation chance (per in-game hour):", () => _draftGenChancePct,
                v => _draftGenChancePct = v, 0, 100, "%", step: 5);
            cy += CtrlH + Sg;

            _lgGenMinLbl = LgNumControl(sc, Pad, cy, PanelW - Pad * 2f, CtrlH,
                "Min listings per batch:", () => _draftGenMin,
                v => _draftGenMin = Mathf.Min(v, _draftGenMax),
                1, 16, " pcs");
            cy += CtrlH + Sg;

            _lgGenMaxLbl = LgNumControl(sc, Pad, cy, PanelW - Pad * 2f, CtrlH,
                "Max listings per batch:", () => _draftGenMax,
                v => _draftGenMax = Mathf.Max(v, _draftGenMin),
                1, 16, " pcs");
            cy += CtrlH + Mg;
            LgSep(sc, cy); cy += 1f + Mg;

            LgSectionLabel(sc, "LISTING DURATION (in-game hours)", Pad, cy); cy += SecH + Sg;
            LgDescLabel(sc, "How long a listing stays active. Min must be lower than Max.", Pad, cy);
            cy += 22f + Sg;

            _lgDurMinLbl = LgNumControl(sc, Pad, cy, PanelW - Pad * 2f, CtrlH,
                "Min duration:", () => _draftDurMinH,
                v => _draftDurMinH = Mathf.Clamp(v, 1, _draftDurMaxH - 1),
                1, 167, "h");
            cy += CtrlH + Sg;

            _lgDurMaxLbl = LgNumControl(sc, Pad, cy, PanelW - Pad * 2f, CtrlH,
                "Max duration:", () => _draftDurMaxH,
                v => _draftDurMaxH = Mathf.Clamp(v, _draftDurMinH + 1, 168),
                2, 168, "h");
            cy += CtrlH + Mg;
            LgSep(sc, cy); cy += 1f + Mg;

            LgSectionLabel(sc, "ARCHETYPE CHANCE (sum = 100%)", Pad, cy);

            _lgArchSumLbl = _panel.AddLabelToContainer(
                sc, "Σ = 100%", PanelW - Pad - 130f, cy, 130f, SecH,
                new Color(0.22f, 0.75f, 0.40f, 1f));
            _lgArchSumLbl.SetFontSize(11);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_lgArchSumLbl.GetRawPtr())), TextAnchor.MiddleRight);
            cy += SecH + Sg;

            LgDescLabel(sc, "Probability of each archetype. Linked — sum always = 100%. Step: 5%.", Pad, cy);
            cy += 22f + Sg;
            LgDescLabel(sc, "Click L1/L2/L3 ▶ to configure the level distribution within each archetype.", Pad, cy);
            cy += 22f + Sg;

            string[] archNames = { "Honest", "Neglected", "Dealer", "Scammer" };
            Color[] archAccents =
            {
        new Color(0.22f, 0.75f, 0.40f, 1f),
        new Color(0.85f, 0.72f, 0.20f, 1f),
        new Color(0.55f, 0.75f, 0.90f, 1f),
        new Color(0.90f, 0.45f, 0.20f, 1f),
    };
            float archRowW = PanelW - Pad * 2f;
            const float ArchRowH = 54f;

            for (int a = 0; a < 4; a++)
            {
                BuildLgArchRow(sc, a, archNames[a], archAccents[a], Pad, cy, archRowW, ArchRowH);
                cy += ArchRowH + (a < 3 ? 5f : 0f);
            }
            cy += Mg;
            LgSep(sc, cy); cy += 1f + Mg;

            const float SaveW = 240f;
            const float SaveH = 48f;
            var savePtr = _panel.AddButtonToContainer(
                sc, "✔  SAVE SETTINGS",
                (PanelW - SaveW) / 2f, cy, SaveW, SaveH,
                OXLGreen, SaveListingGenSettings);
            _panel.WireHover(savePtr, OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));

            var hintLbl = _panel.AddLabelToContainer(
                sc,
                "Changes apply to newly generated listings only  ·  Saved to CMS2026_OXL.cfg",
                0f, cy + SaveH + 6f, PanelW, 18f,
                new Color(0.55f, 0.68f, 0.58f, 1f));
            hintLbl.SetFontSize(10);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(hintLbl.GetRawPtr())), TextAnchor.MiddleCenter);

            cy += SaveH + 18f + 20f;

            // ── Ustaw wysokość contentu ───────────────────────────────────────────
            _lgContentHeight = cy;
            S.Height(UIRuntime.GetStyle(UIRuntime.WrapVE(_lgContentPtr)), cy);
        }


        private void LgWireScroll(object viewport)
        {
            try
            {
                var ue = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "UnityEngine.UIElementsModule");
                if (ue == null) { OXLPlugin.Log.Msg("[OXL] LgWireScroll: UIElements assembly not found"); return; }

                var trickle = ue.GetType("UnityEngine.UIElements.TrickleDown");
                var wheelType = ue.GetType("UnityEngine.UIElements.WheelEvent");
                var regBase = UIRuntime.VisualElementType.GetMethods()
                    .First(m => m.Name == "RegisterCallback"
                             && m.IsGenericMethod
                             && m.GetParameters().Length == 2);

                var wheelReg = regBase.MakeGenericMethod(wheelType);
                Action<UnityEngine.UIElements.WheelEvent> wheelH = evt =>
                {
                    LgScroll(evt.delta.y * LgScrollStep);
                    try
                    {
                        var stopProp = ue.GetType("UnityEngine.UIElements.EventBase")
                            ?.GetMethod("StopPropagation");
                        stopProp?.Invoke(evt, null);
                    }
                    catch { }
                };

                wheelReg.Invoke(viewport, new object[] {
            Il2CppInterop.Runtime.DelegateSupport
                .ConvertDelegate<UnityEngine.UIElements.EventCallback<UnityEngine.UIElements.WheelEvent>>(wheelH),
            Enum.Parse(trickle, "TrickleDown")
        });
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] LgWireScroll failed: {ex.Message}");
            }
        }


        private void LgScroll(float delta)
        {
            if (_lgContentPtr == IntPtr.Zero) return;
            float scrollViewH = PanelH - OverlayTop - 44f - 1f - 32f;
            float maxScroll = Mathf.Max(0f, _lgContentHeight - scrollViewH);
            _lgScrollY = Mathf.Clamp(_lgScrollY + delta, 0f, maxScroll);
            S.Top(UIRuntime.GetStyle(UIRuntime.WrapVE(_lgContentPtr)), -_lgScrollY);
        }


        private void BuildLgArchRow(object overlay, int ai, string name, Color accent,
    float x, float y, float w, float rowH)
        {
            const float NameW = 130f;
            const float ValW = 72f;
            const float BtnW = 36f;
            const float EditW = 156f;
            const float Gp = 6f;

            var card = UIRuntime.NewVE();
            {
                var cs = UIRuntime.GetStyle(card);
                S.Position(cs, "Absolute");
                S.Left(cs, x); S.Top(cs, y);
                S.Width(cs, w); S.Height(cs, rowH);
                S.BgColor(cs, TagBg);
                S.BorderRadius(cs, 6f);
                S.BorderWidth(cs, 1f);
                S.BorderColor(cs, TagBdr);
            }
            UIRuntime.AddChild(overlay, card);

            var nameLbl = _panel.AddLabelToContainer(card, name, 12f, 0f, NameW, rowH, accent);
            nameLbl.SetFontSize(14);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(nameLbl.GetRawPtr())), TextAnchor.MiddleLeft);

            float barX = 12f + NameW + Gp;
            float barH = 10f;
            float barY = (rowH - barH) / 2f;
            float barW = w - barX - Gp - ValW - Gp - BtnW - Gp - BtnW - Gp - BtnW - Gp - EditW - 12f;
            _lgArchBarMaxW = barW;

            var barBg = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(barBg);
                S.Position(s, "Absolute");
                S.Left(s, barX); S.Top(s, barY);
                S.Width(s, barW); S.Height(s, barH);
                S.BgColor(s, new Color(0.04f, 0.07f, 0.12f, 1f));
                S.BorderRadius(s, 5f);
            }
            UIRuntime.AddChild(card, barBg);

            var barFill = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(barFill);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, 0f);
                S.Width(s, barW * (_draftArchW[ai] / 100f));
                S.Height(s, barH);
                S.BgColor(s, accent with { a = 0.75f });
                S.BorderRadius(s, 5f);
            }
            UIRuntime.AddChild(barBg, barFill);
            _lgArchBarFillPtrs[ai] = UIRuntime.GetPtr(barFill);

            float valX = barX + barW + Gp;
            _lgArchLbls[ai] = _panel.AddLabelToContainer(
                card, $"{_draftArchW[ai]}%", valX, 0f, ValW, rowH, Color.white);
            _lgArchLbls[ai].SetFontSize(15);
            {
                var vs = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgArchLbls[ai].GetRawPtr()));
                S.BgColor(vs, new Color(0.04f, 0.07f, 0.13f, 1f));
                S.BorderRadius(vs, 4f);
                S.TextAlign(vs, TextAnchor.MiddleCenter);
            }

            float minX = valX + ValW + Gp;
            float plsX = minX + BtnW + Gp;
            float lockX = plsX + BtnW + Gp;
            float editX = lockX + BtnW + Gp;
            float btnY = (rowH - 30f) / 2f;

            int ai2 = ai;

            var minusPtr = _panel.AddButtonToContainer(card, "−",
                minX, btnY, BtnW, 30f, BtnDark, () =>
                {
                    if (_draftArchLocked[ai2]) return;
                    AdjustLinked(_draftArchW, ai2, _draftArchW[ai2] - 5, 0, 100, _draftArchLocked);
                    RefreshLgArchDisplays();
                });
            _panel.WireHover(minusPtr, BtnDark, BtnDarkHi, SearchBdr);

            var plusPtr = _panel.AddButtonToContainer(card, "+",
                plsX, btnY, BtnW, 30f, BtnDark, () =>
                {
                    if (_draftArchLocked[ai2]) return;
                    AdjustLinked(_draftArchW, ai2, _draftArchW[ai2] + 5, 0, 100, _draftArchLocked);
                    RefreshLgArchDisplays();
                });
            _panel.WireHover(plusPtr, BtnDark, BtnDarkHi, SearchBdr);

            Color lockBgNormal = _draftArchLocked[ai2] ? LockBgOn : LockBgOff;

            var lockPtr = _panel.AddButtonToContainer(card, "🔒",
                lockX, btnY, BtnW, 30f, lockBgNormal,
                () =>
                {
                    _draftArchLocked[ai2] = !_draftArchLocked[ai2];
                    bool isLocked = _draftArchLocked[ai2];
                    Color newBg = isLocked ? LockBgOn : LockBgOff;

                    var st = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgArchLockBtnPtrs[ai2]));
                    S.BgColor(st, newBg);
                    S.BorderColor(st, isLocked ? LockBdrOn : LockBdrOff);

                    _panel.WireHover(_lgArchLockBtnPtrs[ai2],
                        newBg,
                        new Color(0.18f, 0.08f, 0.08f, 1f),
                        new Color(0.50f, 0.10f, 0.10f, 0.50f));
                });
            _lgArchLockBtnPtrs[ai] = lockPtr;
            _panel.WireHover(lockPtr, lockBgNormal,
                new Color(0.18f, 0.08f, 0.08f, 1f),
                new Color(0.50f, 0.10f, 0.10f, 0.50f));

            var editPtr = _panel.AddButtonToContainer(card, "L1 / L2 / L3  ▶",
                editX, btnY, EditW, 30f,
                new Color(0.06f, 0.10f, 0.18f, 1f),
                () => ShowArchLevel(ai2));
            _panel.WireHover(editPtr,
                new Color(0.06f, 0.10f, 0.18f, 1f),
                new Color(0.10f, 0.16f, 0.26f, 1f),
                accent with { a = 0.50f });
        }

        // ══════════════════════════════════════════════════════════════════════════════
        //  ARCHETYPE LEVEL OVERLAY
        //  Overlay 2 z 2: rozkład L1/L2/L3 dla wybranego archetypu.
        // ══════════════════════════════════════════════════════════════════════════════

        private void BuildArchLevelOverlay()
        {
            const float TopBarH = 44f;
            const float FootH = 32f;
            const float Pad = 28f;
            const float Sg = 10f;
            const float Mg = 22f;
            const float SecH = 16f;
            const float LevelRowH = 64f;

            var ov = UIRuntime.NewVE();
            {
                var os = UIRuntime.GetStyle(ov);
                S.Position(os, "Absolute");
                S.Left(os, 0f); S.Top(os, OverlayTop);
                S.Width(os, PanelW); S.Height(os, PanelH - OverlayTop);
                S.BgColor(os, PageBg);
                S.Overflow(os, "Hidden");
                S.Display(os, false);
            }
            _panel.AddOverlayToPanel(ov);
            _archLevelOverlayPtr = UIRuntime.GetPtr(ov);

            // ── Zawartość — dodana PRZED topbarem (niżej w z-order) ─────────────────
            float cy = TopBarH + 1f + Mg;

            LgSectionLabel(ov, "LEVEL DISTRIBUTION (sum = 100%)", Pad, cy);

            _lgLvlSumLbl = _panel.AddLabelToContainer(
                ov, "Σ = 100%",
                PanelW - Pad - 130f, cy, 130f, SecH,
                new Color(0.22f, 0.75f, 0.40f, 1f));
            _lgLvlSumLbl.SetFontSize(11);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_lgLvlSumLbl.GetRawPtr())),
                TextAnchor.MiddleRight);
            cy += SecH + Sg;

            LgDescLabel(ov,
                "Chance for each experience level within the selected archetype. Linked — sum always = 100%. Step: 5%.",
                Pad, cy);
            cy += 22f + Mg;

            string[] lvlLabels =
            {
        "L1  —  Novice",
        "L2  —  Experienced",
        "L3  —  Veteran",
    };
            Color[] lvlAccents =
            {
        new Color(0.45f, 0.65f, 0.85f, 1f),
        new Color(0.55f, 0.80f, 0.55f, 1f),
        new Color(0.85f, 0.55f, 0.75f, 1f),
    };

            float rowW = PanelW - Pad * 2f;
            const float NameW = 500f;
            const float ValW = 72f;
            const float BtnW = 36f;
            const float Gp = 6f;

            // ── Uwzględnia 3 przyciski: −, +, 🔒 ─────────────────────────────────────
            _lgLvlBarMaxW = rowW - 12f - NameW - Gp - ValW - Gp - BtnW - Gp - BtnW - Gp - BtnW - 12f;

            for (int li = 0; li < 3; li++)
            {
                BuildLgLevelRow(ov, li, lvlLabels[li], lvlAccents[li],
                    Pad, cy, rowW, LevelRowH);
                cy += LevelRowH + 8f;
            }
            cy += Mg;
            LgSep(ov, cy); cy += 1f + Mg;

            const float SaveW = 260f;
            const float SaveH = 48f;
            var savePtr = _panel.AddButtonToContainer(
                 ov, "✔  SAVE LEVELS",
                (PanelW - SaveW) / 2f, cy, SaveW, SaveH,
                OXLGreen, () => { SaveListingGenSettings(); HideArchLevel(); });
            _panel.WireHover(savePtr, OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));

            var backLinkLbl = _panel.AddLabelToContainer(
                ov, "← Go back to Listing Generation to save all settings together",
                0f, cy + SaveH + 6f, PanelW, 18f,
                new Color(0.35f, 0.48f, 0.38f, 0.60f));
            backLinkLbl.SetFontSize(10);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(backLinkLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            // ── Footer przed topbarem ─────────────────────────────────────────────────
            BuildFooter(ov, PanelH - OverlayTop - FootH);

            // ── Top bar OSTATNI → renderuje się na wierzchu ───────────────────────────
            LgBuildTopBar(ov, "", HideArchLevel, out object topBar);
            LgSep(ov, TopBarH);

            // Tytuł dynamiczny — dodany do topBar po jego utworzeniu
            _lgLvlTitleLbl = _panel.AddLabelToContainer(
                topBar, "—", 132f, 0f, 600f, TopBarH, OXLGreen);
            _lgLvlTitleLbl.SetFontSize(18);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_lgLvlTitleLbl.GetRawPtr())),
                TextAnchor.MiddleLeft);
        }

        // ── Wiersz poziomu: [nazwa] [pasek] [wartość%] [−] [+] ───────────────────────

        private void BuildLgLevelRow(
    object overlay, int li, string label, Color accent,
    float x, float y, float w, float rowH)
        {
            const float NameW = 500f;
            const float ValW = 72f;
            const float BtnW = 36f;
            const float Gp = 6f;

            var card = UIRuntime.NewVE();
            {
                var cs = UIRuntime.GetStyle(card);
                S.Position(cs, "Absolute");
                S.Left(cs, x); S.Top(cs, y);
                S.Width(cs, w); S.Height(cs, rowH);
                S.BgColor(cs, TagBg);
                S.BorderRadius(cs, 6f);
                S.BorderWidth(cs, 1f);
                S.BorderColor(cs, TagBdr);
            }
            UIRuntime.AddChild(overlay, card);

            var nameLbl = _panel.AddLabelToContainer(card, label, 12f, 0f, NameW, rowH, accent);
            nameLbl.SetFontSize(13);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(nameLbl.GetRawPtr())), TextAnchor.MiddleLeft);

            float barX = 12f + NameW + Gp;
            float barH = 10f;
            float barW = _lgLvlBarMaxW;
            float barY = (rowH - barH) / 2f;

            var barBg = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(barBg);
                S.Position(s, "Absolute");
                S.Left(s, barX); S.Top(s, barY);
                S.Width(s, barW); S.Height(s, barH);
                S.BgColor(s, new Color(0.04f, 0.07f, 0.12f, 1f));
                S.BorderRadius(s, 5f);
            }
            UIRuntime.AddChild(card, barBg);

            var barFill = UIRuntime.NewVE();
            {
                var s = UIRuntime.GetStyle(barFill);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, 0f);
                S.Width(s, barW * (_draftLvlW[0][li] / 100f));
                S.Height(s, barH);
                S.BgColor(s, accent with { a = 0.75f });
                S.BorderRadius(s, 5f);
            }
            UIRuntime.AddChild(barBg, barFill);
            _lgLvlBarFillPtrs[li] = UIRuntime.GetPtr(barFill);

            float valX = barX + barW + Gp;
            _lgLvlLbls[li] = _panel.AddLabelToContainer(
                card, $"{_draftLvlW[0][li]}%", valX, 0f, ValW, rowH, Color.white);
            _lgLvlLbls[li].SetFontSize(15);
            {
                var vs = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgLvlLbls[li].GetRawPtr()));
                S.BgColor(vs, new Color(0.04f, 0.07f, 0.13f, 1f));
                S.BorderRadius(vs, 4f);
                S.TextAlign(vs, TextAnchor.MiddleCenter);
            }

            float minX = valX + ValW + Gp;
            float plsX = minX + BtnW + Gp;
            float lockX = plsX + BtnW + Gp;   // ← kłódka
            float btnY = (rowH - 30f) / 2f;
            int li2 = li;

            // ── Przycisk − ────────────────────────────────────────────────────────────
            var minusPtr = _panel.AddButtonToContainer(card, "−",
                minX, btnY, BtnW, 30f, BtnDark, () =>
                {
                    if (_draftLvlLocked[_archLevelIdx][li2]) return;
                    AdjustLinked(_draftLvlW[_archLevelIdx], li2,
                        _draftLvlW[_archLevelIdx][li2] - 5, 0, 100,
                        _draftLvlLocked[_archLevelIdx]);
                    RefreshLgLvlDisplays();
                });
            _panel.WireHover(minusPtr, BtnDark, BtnDarkHi, SearchBdr);

            // ── Przycisk + ────────────────────────────────────────────────────────────
            var plusPtr = _panel.AddButtonToContainer(card, "+",
                plsX, btnY, BtnW, 30f, BtnDark, () =>
                {
                    if (_draftLvlLocked[_archLevelIdx][li2]) return;
                    AdjustLinked(_draftLvlW[_archLevelIdx], li2,
                        _draftLvlW[_archLevelIdx][li2] + 5, 0, 100,
                        _draftLvlLocked[_archLevelIdx]);
                    RefreshLgLvlDisplays();
                });
            _panel.WireHover(plusPtr, BtnDark, BtnDarkHi, SearchBdr);

            // ── Kłódka toggle ─────────────────────────────────────────────────────────
            Color lockBgNormal = _draftLvlLocked[_archLevelIdx][li2] ? LockBgOn : LockBgOff;

            var lockPtr = _panel.AddButtonToContainer(card, "\U0001F512",
                lockX, btnY, BtnW, 30f, lockBgNormal,
                () =>
                {
                    _draftLvlLocked[_archLevelIdx][li2] = !_draftLvlLocked[_archLevelIdx][li2];
                    bool isLocked = _draftLvlLocked[_archLevelIdx][li2];
                    Color newBg = isLocked ? LockBgOn : LockBgOff;

                    var st = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgLvlLockBtnPtrs[li2]));
                    S.BgColor(st, newBg);
                    S.BorderColor(st, isLocked ? LockBdrOn : LockBdrOff);

                    // ← Re-wire
                    _panel.WireHover(_lgLvlLockBtnPtrs[li2],
                        newBg,
                        new Color(0.18f, 0.08f, 0.08f, 1f),
                        new Color(0.50f, 0.10f, 0.10f, 0.50f));
                });
            _lgLvlLockBtnPtrs[li] = lockPtr;
            _panel.WireHover(lockPtr, lockBgNormal,
                new Color(0.18f, 0.08f, 0.08f, 1f),
                new Color(0.50f, 0.10f, 0.10f, 0.50f));
        }


        // ══════════════════════════════════════════════════════════════════════════════
        //  SHARED LAYOUT HELPERS
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>Buduje górny pasek overlay. Zwraca referencję do topBar VE (do ew. dopisania dynamicznych elementów).</summary>
        private void LgBuildTopBar(object overlay, string title, Action onBack, out object topBarVE)
        {
            var topBar = UIRuntime.NewVE();
            {
                var ts = UIRuntime.GetStyle(topBar);
                S.Position(ts, "Absolute");
                S.Left(ts, 0f); S.Top(ts, 0f);
                S.Width(ts, PanelW); S.Height(ts, 44f);
                S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            }
            UIRuntime.AddChild(overlay, topBar);
            topBarVE = topBar;

            var backPtr = _panel.AddButtonToContainer(
                topBar, "← Back", 12f, 6f, 110f, 32f, BtnDark, onBack);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            if (!string.IsNullOrEmpty(title))
            {
                var titleLbl = _panel.AddLabelToContainer(
                    topBar, title, 132f, 0f, 600f, 44f, OXLGreen);
                titleLbl.SetFontSize(18);
                S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(titleLbl.GetRawPtr())),
                    TextAnchor.MiddleLeft);
            }
        }

        private void LgSectionLabel(object parent, string text, float x, float cy)
        {
            var lbl = _panel.AddLabelToContainer(
                parent, text, x, cy, PanelW - x * 2f, 18f,          // height: 16→18
                new Color(0.45f, 0.70f, 0.52f, 1.00f));              // jaśniejszy zielony
            lbl.SetFontSize(11);                                      // 10→11
        }

        private void LgDescLabel(object parent, string text, float x, float cy)
        {
            var lbl = _panel.AddLabelToContainer(
                parent, text, x, cy, PanelW - x * 2f, 22f,          // height: 20→22
                new Color(0.78f, 0.82f, 0.84f, 1.00f));              // jaśniej niż Color.white (pełna biel jest za ostra)
            lbl.SetFontSize(12);                                      // 11→12
        }

        private void LgSep(object parent, float y)
        {
            var sep = UIRuntime.NewVE();
            {
                var ss = UIRuntime.GetStyle(sep);
                S.Position(ss, "Absolute");
                S.Left(ss, 0f); S.Top(ss, y);
                S.Width(ss, PanelW); S.Height(ss, 1f);
                S.BgColor(ss, Border);
            }
            UIRuntime.AddChild(parent, sep);
        }

        /// <summary>
        /// Kontrolka numeryczna: [tekst etykiety ...] [−] [wartość] [+].
        /// Zwraca uchwyt do etykiety wartości.
        /// </summary>
        private UILabelHandle LgNumControl(
    object parent, float x, float y, float w, float h,
    string labelText, Func<int> getV, Action<int> setV,
    int lo, int hi, string suffix = "", int step = 1)   // ← step
        {
            const float BtnW = 34f;
            const float ValW = 82f;
            const float Gp = 5f;
            float lblW = w - BtnW - Gp - ValW - Gp - BtnW;

            var lbl = _panel.AddLabelToContainer(parent, labelText, x, y, lblW, h,
                new Color(0.65f, 0.72f, 0.76f, 1.00f));
            lbl.SetFontSize(13);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())), TextAnchor.MiddleLeft);

            var valRef = new UILabelHandle[1];

            float bMinX = x + lblW;
            float valX = bMinX + BtnW + Gp;
            float bPluX = valX + ValW + Gp;
            float btnY = y + (h - 30f) / 2f;

            var minusPtr = _panel.AddButtonToContainer(parent, "−",
                bMinX, btnY, BtnW, 30f, BtnDark, () =>
                {
                    int v = Mathf.Clamp(getV() - step, lo, hi);
                    setV(v);
                    valRef[0]?.SetText(getV() + suffix);
                });
            _panel.WireHover(minusPtr, BtnDark, BtnDarkHi, SearchBdr);

            var valLbl = _panel.AddLabelToContainer(
                parent, getV() + suffix, valX, y, ValW, h, Color.white);
            valLbl.SetFontSize(14);
            {
                var vs = UIRuntime.GetStyle(UIRuntime.WrapVE(valLbl.GetRawPtr()));
                S.BgColor(vs, new Color(0.04f, 0.07f, 0.13f, 1f));
                S.BorderRadius(vs, 4f);
                S.TextAlign(vs, TextAnchor.MiddleCenter);
            }
            valRef[0] = valLbl;

            var plusPtr = _panel.AddButtonToContainer(parent, "+",
                bPluX, btnY, BtnW, 30f, BtnDark, () =>
                {
                    int v = Mathf.Clamp(getV() + step, lo, hi);
                    setV(v);
                    valRef[0]?.SetText(getV() + suffix);
                });
            _panel.WireHover(plusPtr, BtnDark, BtnDarkHi, SearchBdr);

            return valLbl;
        }


        // ══════════════════════════════════════════════════════════════════════════════
        //  LINKED SLIDER LOGIC
        // ══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Zmienia arr[idx] na newVal (clamp do [lo, hi]), a pozostałe elementy
        /// kompensują różnicę tak, by suma tablicy pozostała niezmieniona.
        /// Bierze/daje od elementu z największym zapasem (największy gdy zmniejszamy,
        /// najmniejszy gdy zwiększamy) — jeden krok na iterację.
        /// </summary>
        private static void AdjustLinked(int[] arr, int idx, int newVal, int lo, int hi, bool[] locked = null)
        {
            newVal = Mathf.RoundToInt(newVal / 5f) * 5;
            newVal = Mathf.Clamp(newVal, lo, hi);
            if (newVal == arr[idx]) return;

            int delta = newVal - arr[idx];

            // ── Sprawdź ile faktycznie możemy zabrać/dać z odblokowanych ────────────
            if (delta > 0) // zwiększamy idx → musimy zabrać od innych
            {
                int canTake = 0;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i == idx || (locked != null && locked[i])) continue;
                    canTake += arr[i] - lo;
                }
                canTake = (canTake / 5) * 5;  // zaokrąglij w dół do wielokrotności 5
                newVal = Mathf.Min(newVal, arr[idx] + canTake);
                newVal = Mathf.RoundToInt(newVal / 5f) * 5;
                newVal = Mathf.Clamp(newVal, lo, hi);
                if (newVal == arr[idx]) return;
                delta = newVal - arr[idx];
            }
            else // zmniejszamy idx → musimy dać innym
            {
                int canGive = 0;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i == idx || (locked != null && locked[i])) continue;
                    canGive += hi - arr[i];
                }
                canGive = (canGive / 5) * 5;
                newVal = Mathf.Max(newVal, arr[idx] - canGive);
                newVal = Mathf.RoundToInt(newVal / 5f) * 5;
                newVal = Mathf.Clamp(newVal, lo, hi);
                if (newVal == arr[idx]) return;
                delta = newVal - arr[idx];
            }

            arr[idx] = newVal;
            int rem = -delta;

            int guard = 0;
            while (rem != 0 && guard++ < 200)
            {
                int pick = -1;
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i == idx) continue;
                    if (locked != null && locked[i]) continue;
                    if (rem < 0 && arr[i] - 5 < lo) continue;
                    if (rem > 0 && arr[i] + 5 > hi) continue;
                    if (pick < 0) { pick = i; continue; }
                    pick = rem < 0
                        ? (arr[i] > arr[pick] ? i : pick)
                        : (arr[i] < arr[pick] ? i : pick);
                }
                if (pick < 0) break;
                if (rem < 0) { arr[pick] -= 5; rem += 5; }
                else { arr[pick] += 5; rem -= 5; }
            }
        }

        private void RefreshLgArchDisplays()
        {
            int sum = _draftArchW[0] + _draftArchW[1] + _draftArchW[2] + _draftArchW[3];
            for (int i = 0; i < 4; i++)
            {
                _lgArchLbls[i]?.SetText($"{_draftArchW[i]}%");
                if (_lgArchBarFillPtrs[i] == IntPtr.Zero) continue;
                var st = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgArchBarFillPtrs[i]));
                S.Width(st, _lgArchBarMaxW * (_draftArchW[i] / 100f));
            }
            _lgArchSumLbl?.SetText($"Σ = {sum}%");
            _lgArchSumLbl?.SetColor(sum == 100
                ? new Color(0.22f, 0.75f, 0.40f, 1f)
                : new Color(0.95f, 0.45f, 0.20f, 1f));
        }

        private void RefreshLgLvlDisplays()
        {
            int[] w = _draftLvlW[_archLevelIdx];
            int sum = w[0] + w[1] + w[2];
            for (int i = 0; i < 3; i++)
            {
                _lgLvlLbls[i]?.SetText($"{w[i]}%");
                if (_lgLvlBarFillPtrs[i] == IntPtr.Zero) continue;
                var st = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgLvlBarFillPtrs[i]));
                S.Width(st, _lgLvlBarMaxW * (w[i] / 100f));
            }
            _lgLvlSumLbl?.SetText($"Σ = {sum}%");
            _lgLvlSumLbl?.SetColor(sum == 100
                ? new Color(0.22f, 0.75f, 0.40f, 1f)
                : new Color(0.95f, 0.45f, 0.20f, 1f));
        }


        // ══════════════════════════════════════════════════════════════════════════════
        //  NAVIGATION
        // ══════════════════════════════════════════════════════════════════════════════

        private void ShowListingGenSettings()
        {
            if (_listingGenOverlayPtr == IntPtr.Zero) return;
            HideSettings();
            HideListingPage();
            HideDetail();
            HidePage();

            // Reset scroll do góry przy każdym otwarciu
            _lgScrollY = 0f;
            if (_lgContentPtr != IntPtr.Zero)
                S.Top(UIRuntime.GetStyle(UIRuntime.WrapVE(_lgContentPtr)), 0f);

            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingGenOverlayPtr)), true);
        }

        private void HideListingGenSettings()
        {
            if (_listingGenOverlayPtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingGenOverlayPtr)), false);
            ShowSettings(); // powrót do głównych ustawień
        }

        private void ShowArchLevel(int archIdx)
        {
            if (_archLevelOverlayPtr == IntPtr.Zero) return;
            _archLevelIdx = archIdx;

            // Reset wizualny kłódek — każdy archetype ma niezależny stan
            // (stan w _draftLvlLocked[][] jest już per-arch, więc tylko odśwież kolory)
            for (int i = 0; i < 3; i++)
            {
                if (_lgLvlLockBtnPtrs[i] == IntPtr.Zero) continue;
                bool lk = _draftLvlLocked[archIdx][i];
                var st = UIRuntime.GetStyle(UIRuntime.WrapVE(_lgLvlLockBtnPtrs[i]));
                S.BgColor(st, lk ? LockBgOn : LockBgOff);
                S.BorderColor(st, lk ? LockBdrOn : LockBdrOff);
            }

            string[] names = { "Honest", "Neglected", "Dealer", "Scammer" };
            _lgLvlTitleLbl?.SetText($"{names[archIdx]}  —  Level Distribution");
            RefreshLgLvlDisplays();

            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_archLevelOverlayPtr)), true);
        }

        private void HideArchLevel()
        {
            if (_archLevelOverlayPtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_archLevelOverlayPtr)), false);
        }

        private void SaveListingGenSettings()
        {
            var config = new ListingGenConfig
            {
                MaxListings = _draftMaxListings,
                GenChancePct = _draftGenChancePct,
                GenMin = _draftGenMin,
                GenMax = _draftGenMax,
                DurMinSec = _draftDurMinH * ListingGenConfig.SecondsPerGameHour,
                DurMaxSec = _draftDurMaxH * ListingGenConfig.SecondsPerGameHour,
                ArchWeights = (int[])_draftArchW.Clone(),
                LvlWeights = _draftLvlW.Select(w => (int[])w.Clone()).ToArray(),
            };

            _listings?.ApplyConfig(config);
            OXLSettings.SaveGenConfig(config);   // ← to jest nowe, reszta bez zmian

            OXLLog.Msg($"[OXL:LGSETTINGS] ══ LISTING GEN SETTINGS SAVED ══════════════");
            OXLLog.Msg($"[OXL:LGSETTINGS] MaxListings  : {_draftMaxListings}");
            OXLLog.Msg($"[OXL:LGSETTINGS] GenChance    : {_draftGenChancePct}% / {ListingGenConfig.SecondsPerGameHour:F0}s");
            OXLLog.Msg($"[OXL:LGSETTINGS] GenBatch     : {_draftGenMin}–{_draftGenMax}");
            OXLLog.Msg($"[OXL:LGSETTINGS] Duration     : {_draftDurMinH}h–{_draftDurMaxH}h" +
                       $" ({config.DurMinSec:F0}s–{config.DurMaxSec:F0}s)");
            OXLLog.Msg($"[OXL:LGSETTINGS] ArchWeights  : " +
                       $"H={_draftArchW[0]}% N={_draftArchW[1]}% D={_draftArchW[2]}% W={_draftArchW[3]}%");
            for (int a = 0; a < 4; a++)
                OXLLog.Msg($"[OXL:LGSETTINGS] LvlWeights[{a}]: " +
                           $"L1={_draftLvlW[a][0]}% L2={_draftLvlW[a][1]}% L3={_draftLvlW[a][2]}%");
            OXLLog.Msg($"[OXL:LGSETTINGS] ═══════════════════════════════════════════════");
        }


        private object TryMakeScrollView(object parentVE, float left, float top, float w, float h)
        {
            try
            {
                var svType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.FullName == "UnityEngine.UIElements.ScrollView");
                if (svType == null) return null;

                var sv = Activator.CreateInstance(svType);
                var st = UIRuntime.GetStyle(sv);
                S.Position(st, "Absolute");
                S.Left(st, left); S.Top(st, top);
                S.Width(st, w); S.Height(st, h);
                UIRuntime.AddChild(parentVE, sv);
                return sv;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] ScrollView create failed: {ex.Message}");
                return null;
            }
        }

        private static object GetScrollContent(object sv)
        {
            try { return sv?.GetType().GetProperty("contentContainer")?.GetValue(sv); }
            catch { return null; }
        }

        private static void SetScrollContentHeight(object contentVE, float height)
        {
            try { S.Height(UIRuntime.GetStyle(contentVE), height); }
            catch { }
        }

        private object MakeFallbackContainer(object parentVE, float top, float h)
        {
            var fb = UIRuntime.NewVE();
            var fbs = UIRuntime.GetStyle(fb);
            S.Position(fbs, "Absolute");
            S.Left(fbs, 0f); S.Top(fbs, top);
            S.Width(fbs, PanelW); S.Height(fbs, h);
            S.Overflow(fbs, "Hidden");
            UIRuntime.AddChild(parentVE, fb);
            return fb;
        }



        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════


        public void Open()
        {
            if (_panel == null) return;
            _panel.SetVisible(true);
            _isVisible = true;
            SetPlayerInput(false);

            if (_listingPageWasOpen)
            {
                if (_filteredListings != null) ApplyFilters();
                _lastKnownListingCount = _listings?.ActiveListings.Count ?? 0;
                RefreshListings();
            }
        }

        public void Close()
        {
            if (_panel == null) return;
            _panel.SetVisible(false);
            _isVisible = false;
            SetPlayerInput(true);
        }

        private static void SetPlayerInput(bool enabled)
{
    try
    {
        var pi = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Player.Controller.PlayerInput>();
        if (pi != null) pi.enabled = enabled;
    }
    catch (Exception ex)
    {
        OXLPlugin.Log.Warning($"[OXL] SetPlayerInput({enabled}): {ex.Message}");
    }

    try
    {
        // Blokuj też InputActionAsset — to samo co robi konsola
        var assetType = System.Type.GetType(
            "UnityEngine.InputSystem.InputActionAsset, Unity.InputSystem");
        if (assetType == null) return;

        var all = UnityEngine.Resources.FindObjectsOfTypeAll(
            Il2CppInterop.Runtime.Il2CppType.From(assetType));

        foreach (var raw in all)
        {
            var asset = Activator.CreateInstance(assetType, new object[] { raw.Pointer });
            var maps  = assetType.GetProperty("actionMaps")?.GetValue(asset);
            if (maps == null) continue;

            int count  = (int)maps.GetType().GetProperty("Count").GetValue(maps);
            var indexer = maps.GetType().GetProperty("Item");

            for (int i = 0; i < count; i++)
            {
                var m    = indexer.GetValue(maps, new object[] { i });
                var name = (string)m.GetType().GetProperty("name").GetValue(m);
                if (name != "UI Common") continue;
                m.GetType().GetMethod(enabled ? "Enable" : "Disable").Invoke(m, null);
                break;
            }
        }
    }
    catch { /* InputSystem może nie być dostępny */ }
}


        // ── Console API helpers ───────────────────────────────────────────────────
        public List<CarListing> GetActiveListings() => _listings?.ActiveListings;
        public float GetGameTime() => _listings?.GameTime ?? 0f;
        public CarListing GetCurrentDetailListing() => _detailListing;

        public void CloseDetail()
        {
            HideDetail();
        }

        public void GenerateListings(int count)
        {
            for (int i = 0; i < count; i++)
                _listings?.ForceGenerate();
        }

        public void TickSystem(float dt)
        {
            if (_listings == null) return;
            _listings.Tick(dt);

            int current = _listings.ActiveListings.Count;
            if (current != _lastKnownListingCount)
            {
                _lastKnownListingCount = current;
                try
                {
                    if (_filteredListings != null) ApplyFilters();
                    if (_isVisible) RefreshListings(); // odśwież tylko gdy panel widoczny
                }
                catch (Exception ex)
                {
                    OXLLog.Warn($"[OXL:TICK] RefreshListings failed: {ex.Message}");
                }
            }
        }

        public CarPhotoLoader.CacheInfo GetPhotoCacheInfo() => _photoLoader?.GetCacheInfo() ?? default;

        // Metoda do wygodnego przełączania
        public void Toggle()
        {
            if (IsVisible) Close();
            else Open();
        }

        public void ForceGenCheck() => _listings?.ForceCheckNow();
        public void SaveListings() => _listings?.Save();
    }
}