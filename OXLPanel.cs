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

        private UILabelHandle _listingMoneyLbl;
        private string FormatMetaLine(CarListing listing)
        {
            float rem = listing.ExpiresAt - _listings.GameTime;
            string timer = rem <= 0f ? "Auction ended"
                : $"Expires in {(int)(rem / 60f)}:{(int)(rem % 60f):D2}";
            string mi = listing.Mileage >= 1000
                ? $"{listing.Mileage / 1000}k mi" : $"{listing.Mileage} mi";
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

            "OXL — Online eX-Owner Lies\nVersion: 0.3.0\nAuthor: iBlaster\n\n" +
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


        // ── Settings overlay ──────────────────────────────────────────────────────
        private IntPtr _settingsOverlayPtr;
        private UILabelHandle _diffEasyLbl;
        private UILabelHandle _diffNormalLbl;
        private UILabelHandle _diffHardLbl;

       

        // ── Settings difficulty card ptrs (do aktualizacji bordera) ──────────
        private IntPtr _diffEasyCardPtr;
        private IntPtr _diffNormalCardPtr;
        private IntPtr _diffHardCardPtr;


        private CarSpecLoader _specLoader;
        // ══════════════════════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════════════════════

        public void Build()
        {
            LoadIcons();

            string modsRoot = Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources");
            _photoLoader = new CarPhotoLoader(modsRoot, ListingSystem.GetColorRegistry());
            _listings = new ListingSystem(_photoLoader);
            _specLoader = new CarSpecLoader(modsRoot);


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


            // ── Address bar LAST — renders on top of all overlays ───────────
            // BuildMenuDropdown is called from inside BuildAddressBar, also last,
            // so the dropdown itself renders on top of the address bar. ✓

            BuildAlertOverlay();
            BuildSettingsOverlay();
            BuildAddressBar();



            _panel.SetUpdateCallback(dt =>
            {
                int before = _listings.ActiveListings.Count;
                _listings.Tick(dt);
                if (_listings.ActiveListings.Count != before)
                {
                    // Re-apply filter so expired listings are removed from results
                    if (_filteredListings != null) ApplyFilters();
                    RefreshListings();
                }
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
                "OXL \u2014 Online eX-Owner Lies  \u00B7  v0.3.0  \u00B7  \u00A9 Blaster  \u00B7  github.com/iBl4St3R/CMS2026-OXL",
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

            // Title — wyśrodkowany między Back a Balance
            var titleLbl = _panel.AddLabelToContainer(
                topBar, "\U0001F697  Passenger Cars \u2014 active listings",
                130f, 0f, PanelW - 350f, 44f, OXLGreen);
            titleLbl.SetFontSize(17);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(titleLbl.GetRawPtr())),
                TextAnchor.MiddleCenter);

            // Balance — prawy róg
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

            // Rows container
            const float PaginationH = 46f;
            const float FootH = 32f;
            float rowsTop = 50f;   // below top bar + separator
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

            // ── Pagination (ABOVE footer line) ───────────────────────────────────
            float paginationTop = PanelH - OverlayTop - FootH - PaginationH;
            BuildPaginationBar(overlay, paginationTop);

            // ── Footer ────────────────────────────────────────────────────────────
            BuildFooter(overlay, PanelH - OverlayTop - FootH);
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
            int m = (int)(rem / 60f);
            int s = (int)(rem % 60f);
            string icon = rem < 60f ? "\u26A0 " : "\u23F1 ";
            return $"Expires in {m}:{s:D2}";
        }

        // ── Listing page visibility ───────────────────────────────────────────
        private void ShowListingPage()
        {
            if (_listingPagePtr == IntPtr.Zero) return;
            _currentPage = 0;
            RefreshListings();
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingPagePtr)), true);
        }

        private void HideListingPage()
        {
            if (_listingPagePtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_listingPagePtr)), false);
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
                "OXL \u2014 Online eX-Owner Lies  \u00B7  v0.3.0  \u00B7  \u00A9 Blaster  \u00B7  github.com/iBl4St3R/CMS2026-OXL",
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
                 "Easy = 10% cheaper.  Normal = 30% higher (recommended).  Hard = 60% higher.",
                Pad, cy, PanelW - Pad * 2f, 40f,
                TextGray);
            descLbl.SetFontSize(12);
            cy += 50f;

            // ── Three difficulty buttons side by side ────────────────────────────
            const float BtnW = 200f;
            const float BtnH = 64f;
            const float BtnGap = 16f;
            float totalBtnsW = BtnW * 3 + BtnGap * 2;
            float bx = (PanelW - totalBtnsW) / 2f;

            BuildDiffButton(overlay, bx, cy, BtnW, BtnH, Difficulty.Easy,"EASY", "Cars 10% cheaper", new Color(0.20f, 0.60f, 0.35f, 1f),
            ref _diffEasyLbl, ref _diffEasyCardPtr);
            BuildDiffButton(overlay, bx + BtnW + BtnGap, cy, BtnW, BtnH, Difficulty.Normal,"NORMAL", "Cars 30% more expensive", new Color(0.55f, 0.75f, 0.90f, 1f),
            ref _diffNormalLbl, ref _diffNormalCardPtr);
            BuildDiffButton(overlay, bx + (BtnW + BtnGap) * 2, cy, BtnW, BtnH, Difficulty.Hard,"HARD", "Cars 60% more expensive", new Color(0.90f, 0.45f, 0.20f, 1f),
            ref _diffHardLbl, ref _diffHardCardPtr);

            cy += BtnH + 32f;

            // ── Note ──────────────────────────────────────────────────────────────
            var noteLbl = _panel.AddLabelToContainer(
                overlay,
                "Change takes effect on the next generated listing.\n" +
                "Active listings keep their original prices.",
                Pad, cy, PanelW - Pad * 2f, 40f,
                new Color(0.30f, 0.38f, 0.34f, 0.80f));
            noteLbl.SetFontSize(11);

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
                card, sublabel, 0f, 38f, w, 18f, TextGray);
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

            lbl?.SetColor(active ? accentColor : TextDim);
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

        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════


        public void Open()
        {
            if (_panel == null) return;
            _panel.SetVisible(true);
            _isVisible = true;
            SetPlayerInput(false);
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

        public CarPhotoLoader.CacheInfo GetPhotoCacheInfo() => _photoLoader?.GetCacheInfo() ?? default;

        // Metoda do wygodnego przełączania (np. dla klawisza F10)
        public void Toggle()
        {
            if (IsVisible) Close();
            else Open();
        }
    }
}