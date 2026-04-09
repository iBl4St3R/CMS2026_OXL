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
using static Il2CppCMS.Platforms.Steam.SteamWorkshopUploader;
using static Il2CppCMS.UI.Logic.TopMenu;

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
        private readonly ListingSystem _listings = new ListingSystem();

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

        private readonly Dictionary<string, UILabelHandle> _timerLabels = new();
        private bool _buyClickConsumed = false;

        // ── Detail overlay ────────────────────────────────────────────────────
        private IntPtr _detailOverlayPtr;
        private UILabelHandle _detailTitle;
        private UILabelHandle _detailYear;
        private UILabelHandle _detailTimer;
        private UILabelHandle _detailPrice;
        private IntPtr _detailBuyPtr;
        private CarListing _detailListing;
        private IntPtr _detailImgBoxPtr;

        private UILabelHandle _detailListedLbl;
        private UILabelHandle _detailSellerNote;
        private UILabelHandle _detailLocationLbl;
        private IntPtr _detailSpecsContainerPtr;

        private UILabelHandle _detailSellerStars;

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

            "OXL — Online eX-Owner Lies\nVersion: 0.1.0\nAuthor: iBlaster\n\n" +
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


        private readonly Dictionary<string, Texture2D> _carImages = new();

        // Właściwość pomocnicza do sprawdzania stanu z zewnątrz
        private bool _isVisible; // Zmienna śledząca stan

        public bool IsVisible => _isVisible;


        //guards



        // ══════════════════════════════════════════════════════════════════════
        //  BUILD
        // ══════════════════════════════════════════════════════════════════════

        public void Build()
        {
            LoadIcons();

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
                "THIS MOD WILL NOT BE AVAILABLE OR SUPPORTED ONCE THE FULL GAME IS RELEASED FOR PURCHASE",
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
                "OXL \u2014 Online eX-Owner Lies  \u00B7  v0.1.0  \u00B7  \u00A9 Blaster  \u00B7  github.com/iBl4St3R/CMS2026-OXL",
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

            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Back",
                Pad, 6f, 100f, TopBarH - 12f,
                BtnDark, HidePage);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

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

            // Chowamy overlaye aukcji, żeby page overlay był na wierzchu
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

            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Back", 12f, 6f, 110f, 32f, BtnDark, HideListingPage);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            var titleLbl = _panel.AddLabelToContainer(
                topBar, "\U0001F697  Passenger Cars — active listings",
                140f, 0f, 700f, 44f, OXLGreen);
            titleLbl.SetFontSize(15);

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
            const float RightW = 180f;

            // Row background
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
                ShowDetail(listing);   // ← listing z closure, nie _detailListing
            });


            // Bottom separator
            var rowSep = UIRuntime.NewVE();
            var rss = UIRuntime.GetStyle(rowSep);
            S.Position(rss, "Absolute");
            S.Left(rss, Pad); S.Top(rss, RowH - 1f);
            S.Width(rss, PanelW - Pad * 2f); S.Height(rss, 1f);
            S.BgColor(rss, new Color(0.15f, 0.22f, 0.32f, 0.5f));
            UIRuntime.AddChild(container, rowSep);

            // Thumbnail
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

            if (_carImages.TryGetValue(listing.ImageFolder, out var carTex))
            {
                UIRuntime.SetBackgroundImage(imgBox, carTex);
                // ScaleMode.ScaleToFit przez styl
                var imsVE = UIRuntime.WrapVE(UIRuntime.GetPtr(imgBox));
                // backgroundSize nie jest w S{} — pomijamy, domyślnie stretch jest ok
            }
            else
            {
                var iconLbl = _panel.AddLabelToContainer(
                    imgBox, "\U0001F697", 0f, 0f, ImgW, ImgH,
                    new Color(0.28f, 0.40f, 0.52f, 1f));
                iconLbl.SetFontSize(24);
            }

            // Content area
            float contentX = Pad + ImgW + 14f;
            float contentW = PanelW - contentX - RightW - Pad * 2f;

            var titleLbl = _panel.AddLabelToContainer(
                rowPtr, $"{listing.Make} {listing.Model}  \u2022  {listing.Year}",
                contentX, 10f, contentW, 24f, Color.white);
            titleLbl.SetFontSize(16);

            var starsRowLbl = _panel.AddLabelToContainer(
            rowPtr,
            FormatStars(listing.SellerRating),
            contentX, 34f,          // ta sama wysokość co noteLbl
            80f, 18f,
            StarColor(listing.SellerRating));
            starsRowLbl.SetFontSize(11);

            string note = listing.SellerNote.Length > 80
                ? listing.SellerNote.Substring(0, 77) + "..."
                : listing.SellerNote;
            var noteLbl = _panel.AddLabelToContainer(rowPtr, $"\"{note}\"", contentX, 34f, contentW, 18f, TextGray);
            noteLbl.SetFontSize(11);

            // ── Metadata row ──────────────────────────────────────────────────────
            string mileageStr = listing.Mileage >= 1000
                ? $"{listing.Mileage / 1000}k mi"
                : $"{listing.Mileage} mi";
            string meta = $"\U0001F4CD {listing.Location}  \u00B7  " +
                          $"\U0001F6E3 {mileageStr}  \u00B7  " +
                          $"\U0001F4C5 {listing.Year}  \u00B7  " +
                          $"\u23F0 ~{listing.DeliveryHours}h delivery";

            var metaLbl = _panel.AddLabelToContainer(
                rowPtr, meta,
                contentX, 54f, contentW, 18f,
                new Color(0.35f, 0.55f, 0.72f, 1f));
            metaLbl.SetFontSize(11);




            float remaining = listing.ExpiresAt - _listings.GameTime;
            Color timerColor = remaining < 120f
                ? new Color(0.95f, 0.55f, 0.20f, 1f)
                : new Color(0.45f, 0.65f, 0.85f, 1f);
            var timerLbl = _panel.AddLabelToContainer(rowPtr, FormatTimer(listing), contentX, 72f, 240f, 16f, timerColor);   // was 62f
            timerLbl.SetFontSize(12);
            _timerLabels[listing.InternalId] = timerLbl;

            // Right side
            float rightX = PanelW - RightW - Pad;

            var priceLbl = _panel.AddLabelToContainer(
                rowPtr, $"${listing.Price:N0}",
                rightX, 10f, RightW, 30f, OXLGreen);
            priceLbl.SetFontSize(20);

            var buyPtr = _panel.AddButtonToContainer(rowPtr, "BUY \u25BA", rightX + RightW - 130f, 46f, 130f, 34f, OXLGreen,
            () =>
            {
                _buyClickConsumed = true;
                ExecutePurchase(listing);
            }
            );
            _panel.WireHover(buyPtr, OXLGreen, new Color(0.28f, 0.70f, 0.42f, 1f), new Color(0.16f, 0.48f, 0.28f, 1f));
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
            if (_listingRowsContainerPtr == IntPtr.Zero) return;

            var container = UIRuntime.WrapVE(_listingRowsContainerPtr);
            UIRuntime.VisualElementType.GetMethod("Clear")?.Invoke(container, null);
            _timerLabels.Clear();

            // Use filtered list if a search is active, otherwise show everything
            var all = _filteredListings ?? _listings.ActiveListings;

            _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, TotalPages() - 1));
            _pageCountLabel?.SetText($"{_currentPage + 1} / {TotalPages()}");

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
                lbl.SetText(FormatTimer(listing));
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
            // ── Root overlay ──────────────────────────────────────────────────
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

            // ── Top bar ───────────────────────────────────────────────────────
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

            _detailTitle = _panel.AddLabelToContainer(
                topBar, "", 170f, 0f, PanelW - 200f, 44f, Color.white);
            _detailTitle.SetFontSize(17);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailTitle.GetRawPtr())),
                        TextAnchor.MiddleLeft);

            var topSep = UIRuntime.NewVE();
            var tss = UIRuntime.GetStyle(topSep);
            S.Position(tss, "Absolute");
            S.Left(tss, 0f); S.Top(tss, 44f);
            S.Width(tss, PanelW); S.Height(tss, 1f);
            S.BgColor(tss, Border);
            UIRuntime.AddChild(overlay, topSep);

            // ════════════════════════════════════════════════════════════════
            //  TWO-COLUMN LAYOUT
            //  Left  : large image          x=24   w=820
            //  Right : info panel           x=868  w=560
            // ════════════════════════════════════════════════════════════════
            const float ContentTop = 56f;
            const float ImgX = 24f;
            const float ImgW = 820f;
            const float ImgH = 462f;
            const float RightX = ImgX + ImgW + 24f;
            const float RightW = PanelW - RightX - 24f;   // ~588

            // ── Large image box ───────────────────────────────────────────────
            var imgBox = UIRuntime.NewVE();
            var ibs = UIRuntime.GetStyle(imgBox);
            S.Position(ibs, "Absolute");
            S.Left(ibs, ImgX); S.Top(ibs, ContentTop);
            S.Width(ibs, ImgW); S.Height(ibs, ImgH);
            S.BgColor(ibs, new Color(0.07f, 0.11f, 0.18f, 1f));
            S.BorderRadius(ibs, 10f);
            S.BorderWidth(ibs, 1f);
            S.BorderColor(ibs, new Color(0.18f, 0.28f, 0.42f, 0.7f));
            UIRuntime.AddChild(overlay, imgBox);
            _detailImgBoxPtr = UIRuntime.GetPtr(imgBox);

            // Fallback emoji
            var imgIcon = Activator.CreateInstance(UIRuntime.LabelType);
            var iils = UIRuntime.GetStyle(imgIcon);
            S.Position(iils, "Absolute");
            S.Left(iils, 0f); S.Top(iils, 0f);
            S.Width(iils, ImgW); S.Height(iils, ImgH);
            S.Color(iils, new Color(0.20f, 0.30f, 0.40f, 1f));
            S.Font(iils); S.TextAlign(iils, TextAnchor.MiddleCenter);
            S.FontSize(iils, 72);
            UIRuntime.LabelType.GetProperty("text").SetValue(imgIcon, "\U0001F697");
            UIRuntime.AddChild(imgBox, imgIcon);

            // ── RIGHT COLUMN ──────────────────────────────────────────────────
            float ry = ContentTop;

            // "Listed X hours ago"
            _detailListedLbl = _panel.AddLabelToContainer(
                overlay, "", RightX, ry, RightW, 20f,
                new Color(0.38f, 0.45f, 0.50f, 1f));
            _detailListedLbl.SetFontSize(11);
            ry += 24f;

            // Make + Model (big title)
            _detailTitle = _panel.AddLabelToContainer(
                overlay, "", RightX, ry, RightW, 36f, Color.white);
            _detailTitle.SetFontSize(22);
            ry += 40f;

            // Price
            _detailPrice = _panel.AddLabelToContainer(
                overlay, "", RightX, ry, RightW, 44f, OXLGreen);
            _detailPrice.SetFontSize(30);
            ry += 52f;

            // Seller note box
            var noteBox = UIRuntime.NewVE();
            var nbs = UIRuntime.GetStyle(noteBox);
            S.Position(nbs, "Absolute");
            S.Left(nbs, RightX); S.Top(nbs, ry);
            S.Width(nbs, RightW); S.Height(nbs, 72f);
            S.BgColor(nbs, new Color(0.045f, 0.080f, 0.130f, 1f));
            S.BorderRadius(nbs, 6f);
            S.BorderWidth(nbs, 1f);
            S.BorderColor(nbs, new Color(0.15f, 0.28f, 0.20f, 0.5f));
            UIRuntime.AddChild(overlay, noteBox);

            _detailSellerNote = _panel.AddLabelToContainer(
                noteBox, "", 12f, 8f, RightW - 24f, 56f, TextGray);
            _detailSellerNote.SetFontSize(12);
            ry += 80f;

            

            // Timer
            _detailTimer = _panel.AddLabelToContainer(
                overlay, "", RightX, ry, RightW, 24f,
                new Color(0.45f, 0.65f, 0.85f, 1f));
            _detailTimer.SetFontSize(12);
            ry += 32f;

            // BUY NOW button — full width
            _detailBuyPtr = _panel.AddButtonToContainer(
                overlay, "BUY NOW  \u25BA",
                RightX, ry, RightW, 52f,
                OXLGreen,
                () =>
                {
                    if (_detailListing == null) return;
                    ExecutePurchase(_detailListing);
                    HideDetail();
                }
                );
            _panel.WireHover(_detailBuyPtr,
                OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
            ry += 60f;

            // ── Seller card ───────────────────────────────────────────────────────────
            var sellerCard = UIRuntime.NewVE();
            var scs = UIRuntime.GetStyle(sellerCard);
            S.Position(scs, "Absolute");
            S.Left(scs, RightX); S.Top(scs, ry);
            S.Width(scs, RightW); S.Height(scs, 72f);
            S.BgColor(scs, new Color(0.042f, 0.072f, 0.115f, 1f));
            S.BorderRadius(scs, 6f);
            S.BorderWidth(scs, 1f);
            S.BorderColor(scs, new Color(0.15f, 0.28f, 0.20f, 0.45f));
            UIRuntime.AddChild(overlay, sellerCard);

            var sellerTypeLbl = _panel.AddLabelToContainer(
    sellerCard, "PRIVATE SELLER",
    12f, 8f, 200f, 16f,
    new Color(0.38f, 0.55f, 0.42f, 0.80f));
            sellerTypeLbl.SetFontSize(9);

            var sellerNameLbl = _panel.AddLabelToContainer(
                sellerCard, "Anonymous",
                12f, 26f, 180f, 24f, Color.white);
            sellerNameLbl.SetFontSize(15);

            // ← deklaracja PRZED przypisaniem do pola
            var sellerStarsLbl = _panel.AddLabelToContainer(
                sellerCard, FormatStars(3),
                200f, 26f, 160f, 24f, StarColor(3));
            sellerStarsLbl.SetFontSize(14);
            _detailSellerStars = sellerStarsLbl;   // ← teraz już po deklaracji ✓

            var sellerYearLbl = _panel.AddLabelToContainer(
                sellerCard, "Member since 2024",
                12f, 50f, 300f, 16f, TextGray);
            sellerYearLbl.SetFontSize(10);
            ry += 80f;

            // ── Location card ─────────────────────────────────────────────────
            var locCard = UIRuntime.NewVE();
            var lcs = UIRuntime.GetStyle(locCard);
            S.Position(lcs, "Absolute");
            S.Left(lcs, RightX); S.Top(lcs, ry);
            S.Width(lcs, RightW); S.Height(lcs, 56f);
            S.BgColor(lcs, new Color(0.042f, 0.072f, 0.115f, 1f));
            S.BorderRadius(lcs, 6f);
            S.BorderWidth(lcs, 1f);
            S.BorderColor(lcs, new Color(0.15f, 0.28f, 0.20f, 0.45f));
            UIRuntime.AddChild(overlay, locCard);

            var locHeader = _panel.AddLabelToContainer(
                locCard, "LOCATION",
                12f, 6f, 200f, 14f,
                new Color(0.38f, 0.55f, 0.42f, 0.80f));
            locHeader.SetFontSize(9);

            _detailLocationLbl = _panel.AddLabelToContainer(
                locCard, "", 12f, 22f, RightW - 24f, 24f, Color.white);
            _detailLocationLbl.SetFontSize(14);

            // ════════════════════════════════════════════════════════════════
            //  SPECS SECTION — below image, full width
            // ════════════════════════════════════════════════════════════════
            float specsTop = ContentTop + ImgH + 18f;

            var specsSep = UIRuntime.NewVE();
            var spss = UIRuntime.GetStyle(specsSep);
            S.Position(spss, "Absolute");
            S.Left(spss, ImgX); S.Top(spss, specsTop - 4f);
            S.Width(spss, PanelW - ImgX * 2f); S.Height(spss, 1f);
            S.BgColor(spss, new Color(0.15f, 0.42f, 0.24f, 0.35f));
            UIRuntime.AddChild(overlay, specsSep);

            var specsHeader = _panel.AddLabelToContainer(
                overlay, "VEHICLE DETAILS",
                ImgX, specsTop + 2f, 300f, 16f,
                new Color(0.22f, 0.48f, 0.30f, 0.65f));
            specsHeader.SetFontSize(10);

            // Specs container (tags rebuilt on each ShowDetail)
            var specsVE = UIRuntime.NewVE();
            var svs = UIRuntime.GetStyle(specsVE);
            S.Position(svs, "Absolute");
            S.Left(svs, ImgX); S.Top(svs, specsTop + 22f);
            S.Width(svs, PanelW - ImgX * 2f);
            S.Height(svs, PanelH - OverlayTop - specsTop - 22f - 34f);
            S.Overflow(svs, "Hidden");
            UIRuntime.AddChild(overlay, specsVE);
            _detailSpecsContainerPtr = UIRuntime.GetPtr(specsVE);

            // Footer
            const float FootH = 32f;
            BuildFooter(overlay, PanelH - OverlayTop - FootH);
        }


        // Because ShowDetail is called repeatedly, the tags will stack on each open.Cleanest fix: add a bool _detailSpecsBuilt = false flag and only call BuildDetailSpecs once, or clear a dedicated specs container before rebuilding — same pattern as RefreshListings() uses Clear().
        private void ShowDetail(CarListing listing)
        {
            if (_detailOverlayPtr == IntPtr.Zero) return;
            _detailListing = listing;

            _detailTitle?.SetText($"{listing.Make} {listing.Model}");
            _detailPrice?.SetText($"${listing.Price:N0}");
            _detailTimer?.SetText(FormatTimer(listing));
            _detailListedLbl?.SetText($"Listed \u2022 {listing.DeliveryHours}h delivery estimate");
            _detailSellerNote?.SetText($"\"{listing.SellerNote}\"");
            _detailLocationLbl?.SetText($"\U0001F4CD  {listing.Location}");
            _detailYear?.SetText($"Year: {listing.Year}");

            _detailSellerStars?.SetText(FormatStars(listing.SellerRating));
            _detailSellerStars?.SetColor(StarColor(listing.SellerRating));

            // Swap image
            if (_detailImgBoxPtr != IntPtr.Zero)
            {
                var imgVE = UIRuntime.WrapVE(_detailImgBoxPtr);
                if (_carImages.TryGetValue(listing.ImageFolder, out var tex))
                    UIRuntime.SetBackgroundImage(imgVE, tex);
                else
                    UIRuntime.SetBackgroundImage(imgVE, null);
            }

            // Rebuild specs tags
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
                var files = Directory.GetFiles(dir, "*.png");
                if (files.Length == 0) continue;
                var tex = TryLoadTexture(files[0]); // zawsze pierwsza .png
                if (tex != null) _carImages[folder] = tex;
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
                "OXL \u2014 Online eX-Owner Lies  \u00B7  v0.1.0  \u00B7  \u00A9 Blaster  \u00B7  github.com/iBl4St3R/CMS2026-OXL",
                0f, 0f, PanelW, FootH,
                new Color(0.22f, 0.48f, 0.30f, 0.70f));
            lbl.SetFontSize(10);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr())),
                        TextAnchor.MiddleCenter);

            // "powered by" — mniejszy, bardziej wyszarzony, przyklejony do prawej
            var poweredLbl = _panel.AddLabelToContainer(
                foot,
                "powered by UITK Framework 0.2.0",
                0f, 0f, PanelW - 10f, FootH,
                new Color(0.18f, 0.32f, 0.22f, 0.45f));
            poweredLbl.SetFontSize(9);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(poweredLbl.GetRawPtr())),
                        TextAnchor.MiddleRight);
        }

        //old keep for a while

        //    private void BuildDetailSpecs(object overlay, CarListing listing)
        //    {
        //        const float StartX = 48f;
        //        const float StartY = 320f;   // below the image+buy block
        //        const float TagH = 28f;
        //        const float TagGapX = 8f;
        //        const float TagGapY = 6f;
        //        const float PadX = 10f;
        //        const float RowMaxW = PanelW - StartX * 2f;

        //        // Fixed placeholder specs — replace with real data later
        //        var specs = new[]
        //        {
        //    $"\U0001F4CB  Reg: {listing.Registration}",
        //    $"\u26FD  Fuel: Petrol",
        //    $"\U0001F697  Body: Sedan",
        //    $"\U0001F3A8  Color: White",
        //    $"\u2699  Engine: 2.0L",
        //    $"\U0001F4AA  Power: 150 hp",
        //    $"\U0001F6E3  Gearbox: Automatic",
        //    $"\U0001F6A6  Condition: Used",
        //    $"\U0001F30D  Origin: Unknown",
        //    $"\u2B05  Drive: FWD",
        //    $"\U0001F511  Doors: 4",
        //    $"\U0001F4CD  Steering: Left",
        //};

        //        float cx = StartX;
        //        float cy = StartY;

        //        foreach (var spec in specs)
        //        {
        //            // Estimate tag width from text length
        //            float tagW = Mathf.Clamp(spec.Length * 7.2f + PadX * 2f, 100f, 320f);

        //            if (cx + tagW > StartX + RowMaxW)
        //            {
        //                cx = StartX;
        //                cy += TagH + TagGapY;
        //            }

        //            var tag = UIRuntime.NewVE();
        //            var ts = UIRuntime.GetStyle(tag);
        //            S.Position(ts, "Absolute");
        //            S.Left(ts, cx); S.Top(ts, cy);
        //            S.Width(ts, tagW); S.Height(ts, TagH);
        //            S.BgColor(ts, TagBg);
        //            S.BorderRadius(ts, 5f);
        //            S.BorderWidth(ts, 1f);
        //            S.BorderColor(ts, TagBdr);
        //            UIRuntime.AddChild(overlay, tag);

        //            var lbl = _panel.AddLabelToContainer(
        //                tag, spec,
        //                PadX, 0f, tagW - PadX, TagH,
        //                new Color(0.55f, 0.78f, 0.62f, 1f));
        //            lbl.SetFontSize(11);

        //            cx += tagW + TagGapX;
        //        }

        //        // Section header above the tags
        //        var header = _panel.AddLabelToContainer(
        //            overlay, "VEHICLE DETAILS",
        //            StartX, StartY - 22f, 300f, 18f,
        //            new Color(0.22f, 0.48f, 0.30f, 0.65f));
        //        header.SetFontSize(10);

        //        // Separator above header
        //        var sep = UIRuntime.NewVE();
        //        var ss = UIRuntime.GetStyle(sep);
        //        S.Position(ss, "Absolute");
        //        S.Left(ss, StartX); S.Top(ss, StartY - 26f);
        //        S.Width(ss, PanelW - StartX * 2f); S.Height(ss, 1f);
        //        S.BgColor(ss, TagBdr);
        //        UIRuntime.AddChild(overlay, sep);
        //    }

        private void BuildDetailSpecsTags(object container, CarListing listing)
        {
            const float TagH = 28f;
            const float TagGapX = 8f;
            const float TagGapY = 7f;
            const float PadX = 12f;
            const float MaxW = PanelW - 48f * 2f;

            string mileageStr = listing.Mileage >= 1000
                ? $"{listing.Mileage / 1000}k mi"
                : $"{listing.Mileage} mi";

            var specs = new[]
            {
        $"\U0001F4CB  Reg: {listing.Registration}",
        $"\U0001F4C5  Year: {listing.Year}",
        $"\U0001F6E3  Mileage: {mileageStr}",
        $"\u26FD  Fuel: Petrol",
        $"\U0001F697  Body: Sedan",
        $"\U0001F3A8  Color: White",
        $"\u2699  Engine: 2.0L",
        $"\U0001F4AA  Power: 150 hp",
        $"\U0001F527  Gearbox: Automatic",
        $"\U0001F6A6  Condition: Used",
        $"\U0001F30D  Origin: Unknown",
        $"\u2B05  Drive: FWD",
        $"\U0001F511  Doors: 4",
        $"\U0001F4CD  Steering: Left",
    };

            float cx = 0f, cy = 0f;

            foreach (var spec in specs)
            {
                float tagW = Mathf.Clamp(spec.Length * 7.0f + PadX * 2f, 110f, 300f);

                if (cx + tagW > MaxW) { cx = 0f; cy += TagH + TagGapY; }

                var tag = UIRuntime.NewVE();
                var ts = UIRuntime.GetStyle(tag);
                S.Position(ts, "Absolute");
                S.Left(ts, cx); S.Top(ts, cy);
                S.Width(ts, tagW); S.Height(ts, TagH);
                S.BgColor(ts, new Color(0.055f, 0.090f, 0.145f, 1f));
                S.BorderRadius(ts, 5f);
                S.BorderWidth(ts, 1f);
                S.BorderColor(ts, new Color(0.150f, 0.280f, 0.200f, 0.55f));
                UIRuntime.AddChild(container, tag);

                var lbl = _panel.AddLabelToContainer(
                    tag, spec, PadX, 0f, tagW - PadX, TagH,
                    new Color(0.55f, 0.78f, 0.62f, 1f));
                lbl.SetFontSize(11);

                cx += tagW + TagGapX;
            }
        }



        //carspawn
        private void ExecutePurchase(CarListing listing)
        {
            if (!GameBridge.HasFreeSlot())
            {
                OXLPlugin.Log.Msg("[OXL] Purchase blocked — no free parking slot.");
                // TODO: toast "Parking full!"
                return;
            }

            if (!_listings.ActiveListings.Contains(listing)) return;

            // Remove from listings first
            _listings.ActiveListings.Remove(listing);
            OXLPlugin.Log.Msg($"[OXL] Purchased: {listing.Make} {listing.Model} for ${listing.Price}");

            // Spawn async, deduct on success
            GameBridge.SpawnCar(listing, result =>
            {
                OXLPlugin.Log.Msg($"[OXL] SpawnResult: {result}");
                if (result == GameBridge.SpawnResult.Success)
                    GameBridge.DeductMoney(listing.Price);
            });

            if (_filteredListings != null) ApplyFilters();
            RefreshListings();
        }


        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════


        public void Open()
        {
            if (_panel == null) return;
            _panel.SetVisible(true);
            _isVisible = true;
        }

        public void Close()
        {
            if (_panel == null) return;
            _panel.SetVisible(false);
            _isVisible = false;
        }


        // Metoda do wygodnego przełączania (np. dla klawisza F10)
        public void Toggle()
        {
            if (IsVisible) Close();
            else Open();
        }
    }
}