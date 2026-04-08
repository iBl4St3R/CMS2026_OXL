using CMS2026UITKFramework;
using MelonLoader;
using System;
using System.Collections.Generic;
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
        private static readonly Color TextDim = new Color(0.180f, 0.230f, 0.280f, 1.00f); // ciemny placeholder
        private static readonly Color OXLGreen = new Color(0.220f, 0.592f, 0.341f, 1.00f);
        private static readonly Color BtnDark = new Color(0.075f, 0.110f, 0.180f, 1.00f);
        private static readonly Color BtnDarkHi = new Color(0.110f, 0.170f, 0.260f, 1.00f);
        private static readonly Color SearchBdr = new Color(0.220f, 0.592f, 0.341f, 0.55f);
        private static readonly Color InputBg = new Color(0.030f, 0.055f, 0.095f, 1.00f);
        private static readonly Color Transp = new Color(0f, 0f, 0f, 0f);

        private const float PanelW = 1456f;
        private const float PanelH = 980f;
        private const float ContentW = PanelW - 24f; // PanelW - Pad*3 - SbW

        // Szerokość sekcji centralnej — trochę szersza niż logo (logo ~580)
        private const float CenterW = 680f;

        private UIPanel _panel;
        private readonly ListingSystem _listings = new ListingSystem();


        // ── Listing page ──────────────────────────────────────────────────────────
        private IntPtr _listingPagePtr;
        private IntPtr _listingRowsContainerPtr;
        private UILabelHandle _pageCountLabel;
        private int _currentPage = 0;
        private const int RowsPerPage = 7;
        private const float RowH = 90f;
        private const float RowGap = 1f;

        private readonly Dictionary<string, UILabelHandle> _timerLabels = new();
        private bool _buyClickConsumed = false;

        // ── Detail overlay ────────────────────────────────────────────────────────
        private IntPtr _detailOverlayPtr;
        private UILabelHandle _detailTitle;
        private UILabelHandle _detailYear;
        private UILabelHandle _detailNote;
        private UILabelHandle _detailTimer;
        private UILabelHandle _detailPrice;
        private IntPtr _detailBuyPtr;
        private CarListing _detailListing;



        // ── Menu & pages ──────────────────────────────────────────────────────
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
            "Controls\n  F9  —  toggle the OXL panel\n  oxl_open  —  console command\n\n" +
            "Tip: listings expire over time — check back often for new deals.",

            "[Coming soon]\n\nPlanned options:\n  • Currency display\n  • Auction refresh rate\n  • Notification preferences",

            "OXL — Online eX-Owner Lies\nVersion: 0.1.0\nAuthor: Blaster\n\n" +
            "Part of the CMS 2026 modding ecosystem.\n" +
            "Built on _CMS2026_UITK_Framework.\n\n" +
            "github.com/iBl4St3R/CMS2026-OXL"
        };



        // Cache ikon
        private Texture2D _icoPrev, _icoNext, _icoRef, _icoSecured, _icoMenu;

        private Action<string> SpawnCar => id => OXLPlugin.Log.Msg($"[OXL] TODO spawn: {id}");
        private Action<int> DeductMoney => amt => OXLPlugin.Log.Msg($"[OXL] TODO deduct: {amt}");

        // ── Build ─────────────────────────────────────────────────────────────
        public void Build()
        {
            LoadIcons();

            float x = Mathf.Max(0f, (Screen.width - PanelW) / 2f);
            float y = Mathf.Max(0f, (Screen.height - PanelH) / 2f);

            _panel = FrameworkAPI.CreatePanel(title: "OXL", x: x, y: y, width: PanelW, height: PanelH);

            _panel.AddTitleButton("\u2014", () => { }, new Color(0.15f, 0.18f, 0.25f, 1f));
            _panel.AddTitleButton("\u25A1", () => { }, new Color(0.15f, 0.18f, 0.25f, 1f));
            _panel.AddTitleButton("\u2715", Close, new Color(0.55f, 0.10f, 0.10f, 1f));
            _panel.Build(sortOrder: 9000);

            _panel.SetDraggable(false);
            _panel.SetScrollbarVisible(false);

            var pve = UIRuntime.WrapVE(_panel.GetPanelRawPtr());
            var pst = UIRuntime.GetStyle(pve);
            S.BgColor(pst, PageBg);
            S.BorderRadius(pst, 8f);
            S.BorderWidth(pst, 1f);
            S.BorderColor(pst, Border);

            BuildAddressBar();
            BuildContent();

            _panel.SetUpdateCallback(dt =>
            {
                int before = _listings.ActiveListings.Count;
                _listings.Tick(dt);
                if (_listings.ActiveListings.Count != before)
                    RefreshListings();
                UpdateTimers();
            });

            BuildListingPage();
            BuildDetailOverlay();

            _panel.SetVisible(false);
        }

        // ── Pasek adresu z ikonami ────────────────────────────────────────────
        private void BuildAddressBar()
        {
            _panel.AddSeparator(Border);

            var row = _panel.AddRow(height: 44f, gap: 4f);
            row.AddSpace(8f);

            // Ikony nawigacji — używamy UIRuntime.NewVE z teksturą lub fallback tekst
            AddIconButton(row, _icoPrev, "\u2190", 36f, () => { });
            AddIconButton(row, _icoNext, "\u2192", 36f, () => { });
            AddIconButton(row, _icoRef, "\u21BB", 36f, () => { });

            row.AddSpace(8f);

            // Ikona kłódki przed URL
            if (_icoSecured != null)
            {
                var lockVE = UIRuntime.NewVE();
                var lockSt = UIRuntime.GetStyle(lockVE);
                S.Width(lockSt, 24f);
                S.Height(lockSt, 24f);
                UIRuntime.SetBackgroundImage(lockVE, _icoSecured);
                row.AddRaw(lockVE, 28f);
            }

            // Pasek URL
            float urlW = row.RemainingWidth - 48f;
            var urlLabel = row.AddLabel("  oxl.pl/home", width: urlW, color: TextGray);
            var urlVE = UIRuntime.WrapVE(urlLabel.GetRawPtr());
            var urlSt = UIRuntime.GetStyle(urlVE);
            S.BgColor(urlSt, new Color(0.05f, 0.08f, 0.14f, 1f));
            S.BorderRadius(urlSt, 20f);
            S.BorderWidth(urlSt, 1f);
            S.BorderColor(urlSt, SearchBdr);
            S.Padding(urlSt, 6f);

            row.AddSpace(6f);

            // Menu ikona
            AddIconButton(row, _icoMenu, "\u22EE", 36f, () => ToggleMenu());

            _panel.AddSeparator(Border);

            BuildMenuDropdown();    
            BuildPageOverlay();    
        }

        // helper: ikona lub fallback tekstowy
        private IntPtr AddIconButton(UIRowBuilder row, Texture2D icon, string fallbackChar,float width, Action onClick)
        {
            if (icon != null)
            {
                var ve = UIRuntime.NewVE();
                var st = UIRuntime.GetStyle(ve);
                S.Width(st, width); S.Height(st, width);
                S.BorderRadius(st, 4f);
                UIRuntime.SetBackgroundImage(ve, icon);
                var ptr = UIRuntime.GetPtr(ve);
                row.AddRaw(ve, width + 2f);
                _panel.WireHover(ptr, Transp, BtnDark, BtnDarkHi);
                if (onClick != null) _panel.WireClick(ptr, onClick);  // ← wire click
                return ptr;
            }
            else
            {
                var btn = row.AddButton(fallbackChar, width, onClick, Transp);
                btn.SetTextColor(TextGray);
                _panel.WireHover(btn.GetRawPtr(), Transp, BtnDark, BtnDarkHi);
                return btn.GetRawPtr();
            }
        }

        // ── Treść strony ──────────────────────────────────────────────────────
        private void BuildContent()
        {
            _panel.AddSpace(40f);

            // ── Logo ──────────────────────────────────────────────────────────
            Texture2D logo = TryLoadLogo();
            if (logo != null)
            {
                const float ImgW = 580f;
                const float ImgH = 200f;
                var logoImg = _panel.AddImage(logo, ImgW, ImgH);
                // FIX #1: wyśrodkowanie obrazu — przestaw Left po dodaniu
                float imgLeft = (ContentW - ImgW) / 2f;
                var imgVE = UIRuntime.WrapVE(logoImg.GetRawPtr());
                S.Left(UIRuntime.GetStyle(imgVE), imgLeft);
            }
            else
            {
                var lbl = _panel.AddLabel("OXL", OXLGreen, height: 80f);
                lbl.SetFontSize(68);
            }

            _panel.AddSpace(8f);
            // ── Pole wyszukiwania — szersze niż logo, wyśrodkowane ─────────────


            // Używamy row z przestrzenią po bokach żeby wyśrodkować input
            float searchSide = (ContentW - CenterW) / 2f;

            // Wrapper — AddTextInput jest na UIPanel, więc po dodaniu
            // ustawiamy mu width i left przez UIRuntime
            var searchHandle = _panel.AddTextInput(
                 "",          // no built-in placeholder — handled by SetFakePlaceholder
                 onSubmit: q => { if (!string.IsNullOrEmpty(q)) OXLPlugin.Log.Msg($"[OXL] Search: {q}"); },
                 height: 48f);

            // Zawęź i wyśrodkuj pole input przez styl
            var sVE = UIRuntime.WrapVE(searchHandle.GetRawPtr());
            var sSt = UIRuntime.GetStyle(sVE);
            S.Width(sSt, CenterW);
            S.Left(sSt, searchSide);
            S.BgColor(sSt, InputBg);
            S.BorderRadius(sSt, 24f);
            S.BorderWidth(sSt, 1f);
            S.BorderColor(sSt, SearchBdr);
            S.Padding(sSt, 10f);


            // Fake placeholder — gray hint, clears on focus, restores on blur
            searchHandle.SetFakePlaceholder("Search for vehicles, parts or tools", TextGray, Color.white);

            // ── Przyciski + filtry ─────────────────────────────────────────────
            _panel.AddSpace(14f);

            // Rząd: [Szukaj w OXL]  [Marka ▾]  [Rok ▾]  wyśrodkowany
            var btnRow = _panel.AddRow(height: 36f, gap: 10f);
            float totalBtns = 140f + 130f + 110f + 10f * 2; // szukaj + 2 filtry + gapy
            float bside = (btnRow.RemainingWidth - totalBtns) / 2f;
            btnRow.AddSpace(bside);

            var bSearch = btnRow.AddButton("Szukaj w OXL", width: 140f,
                onClick: () => OXLPlugin.Log.Msg("[OXL] Search"), bgColor: BtnDark);
            bSearch.SetTextColor(OXLGreen);
            _panel.WireHover(bSearch.GetRawPtr(), BtnDark, BtnDarkHi, SearchBdr);

            // Dropdown: Marka
            btnRow.AddDropdown("Marka", new[] {"Wszystkie", "APlaceholder", "BPlaceholder", "FPlaceholder", "HPlaceholder", "MPlaceholder", "TPlaceholder", "VPlaceholder" },
                selectedIndex: 0,
                onChanged: i => OXLPlugin.Log.Msg($"[OXL] Make filter: {i}"),
                width: 130f);

            // Dropdown: Rok
            var years = new List<string> { "Dowolny rok" };
            for (int y = 2020; y >= 1990; y--) years.Add(y.ToString());
            btnRow.AddDropdown("Rok od", years.ToArray(),
                selectedIndex: 0,
                onChanged: i => OXLPlugin.Log.Msg($"[OXL] Year filter: {i}"),
                width: 110f);

            // ── Kategorie ─────────────────────────────────────────────────────
            _panel.AddSpace(36f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(10f);

            var catRow = _panel.AddRow(height: 28f, gap: 0f);
            float cSide = (catRow.RemainingWidth - 490f) / 2f;
            catRow.AddSpace(cSide);

            var carCatLbl = catRow.AddLabel("\U0001F697  Samochody osobowe", width: 190f, color: OXLGreen);
            _panel.WireHover(carCatLbl.GetRawPtr(), Transp, BtnDark, BtnDarkHi);
            _panel.WireClick(carCatLbl.GetRawPtr(), ShowListingPage);

            catRow.AddLabel("\U0001F527  Cz\u0119\u015Bci (WIP)", width: 150f, color: TextGray);
            catRow.AddLabel("\U0001F690  Dostawcze (WIP)", width: 150f, color: TextGray);

            _panel.AddSpace(10f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(20f);

            var wip = _panel.AddLabel("\u2014 Reszta strony w budowie (WIP) \u2014", TextGray);
            wip.SetFontSize(11);
        }


        // ── Menu dropdown ─────────────────────────────────────────────────────
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
            // TitleH(24) + sep(~14) + row(44) + sep gap ≈ 88 in panel coords
            float dropTop = 88f;
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
                var ptr = _panel.AddButtonToContainer(drop,
                    PageTitles[i], 0f, 3f + i * ItemH, DropW, ItemH,
                    BtnDark, () => { ToggleMenu(); ShowPage(idx); });
                _panel.WireHover(ptr, BtnDark, BtnDarkHi, SearchBdr);
            }
        }

        // ── Page overlay ──────────────────────────────────────────────────────
        private void BuildPageOverlay()
        {
            const float TopBarH = 44f;
            const float Pad = 16f;

            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, 24f);          // below title bar
            S.Width(os, PanelW); S.Height(os, PanelH - 24f);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _pageOverlayPtr = UIRuntime.GetPtr(overlay);

            // ── Top bar ───────────────────────────────────────────────────────
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, TopBarH);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            var backPtr = _panel.AddButtonToContainer(topBar,
                "\u2190  Back", Pad, 6f, 100f, TopBarH - 12f,
                BtnDark, HidePage);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            _pageTitleLbl = _panel.AddLabelToContainer(topBar,
                "", 130f, 0f, PanelW - 150f, TopBarH, OXLGreen);
            _pageTitleLbl.SetFontSize(18);
            S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(_pageTitleLbl.GetRawPtr())),
                        TextAnchor.MiddleLeft);

            // Separator under top bar
            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, TopBarH);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(overlay, sep);

            // Body text
            _pageBodyLbl = _panel.AddLabelToContainer(overlay,
                "", Pad, TopBarH + Pad, PanelW - Pad * 2f, PanelH - TopBarH - 24f - Pad * 2f,
                TextGray);
            _pageBodyLbl.SetFontSize(13);
        }

        private void ShowPage(int index)
        {
            if (_pageOverlayPtr == IntPtr.Zero) return;
            _pageTitleLbl?.SetText(PageTitles[index]);
            _pageBodyLbl?.SetText(PageBodies[index]);
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_pageOverlayPtr)), true);
        }

        private void HidePage()
        {
            if (_pageOverlayPtr == IntPtr.Zero) return;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_pageOverlayPtr)), false);
        }


        // ── Ładowanie ikon 64×64 z Resources/icons/ ──────────────────────────
        private void LoadIcons()
        {
            // Budujemy ścieżkę w ten sam sprawdzony sposób co przy logo
            string iconDir = System.IO.Path.Combine(
                UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "icons");

            _icoPrev = TryLoadTexture(System.IO.Path.Combine(iconDir, "previous.png"));
            _icoNext = TryLoadTexture(System.IO.Path.Combine(iconDir, "next.png"));
            _icoRef = TryLoadTexture(System.IO.Path.Combine(iconDir, "ref.png"));
            _icoSecured = TryLoadTexture(System.IO.Path.Combine(iconDir, "secured.png"));
            _icoMenu = TryLoadTexture(System.IO.Path.Combine(iconDir, "ModMenu.png"));
        }

        private Texture2D TryLoadLogo()
        {
            // Budujemy ścieżkę od folderu Data gry, cofamy się o jeden (..) do folderu głównego gry,
            // a potem wchodzimy w strukturę folderów Twojego moda.
            string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "Images", "logo.png");

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

                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                var il2b = new Il2CppInterop.Runtime.InteropTypes
                    .Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                var icType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .FirstOrDefault(t => t.FullName == "UnityEngine.ImageConversion");

                if (icType == null) return null;

                var loadImg = icType.GetMethods()
                    .FirstOrDefault(m => m.Name == "LoadImage"
                                     && m.GetParameters().Length == 2);
                if (loadImg == null) return null;

                bool ok = (bool)loadImg.Invoke(null, new object[] { tex, il2b });
                return ok ? tex : null;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Texture load error ({Path.GetFileName(path)}): {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  LISTING PAGE
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildListingPage()
        {
            // ── Overlay ──────────────────────────────────────────────────────────
            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, 24f);
            S.Width(os, PanelW); S.Height(os, PanelH - 24f);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _listingPagePtr = UIRuntime.GetPtr(overlay);

            // ── Top bar ───────────────────────────────────────────────────────────
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, 44f);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Powrót", 12f, 6f, 110f, 32f, BtnDark, HideListingPage);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            var titleLbl = _panel.AddLabelToContainer(
                topBar, "\U0001F697  Samochody osobowe — aktywne aukcje",
                140f, 0f, 600f, 44f, OXLGreen);
            titleLbl.SetFontSize(15);

            // Sep
            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, 44f);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(overlay, sep);

            // ── Rows container ────────────────────────────────────────────────────
            const float PaginationH = 46f;
            float rowsTop = 50f;
            float rowsH = PanelH - 24f - rowsTop - PaginationH;

            var rowsVE = UIRuntime.NewVE();
            var rcs = UIRuntime.GetStyle(rowsVE);
            S.Position(rcs, "Absolute");
            S.Left(rcs, 0f); S.Top(rcs, rowsTop);
            S.Width(rcs, PanelW); S.Height(rcs, rowsH);
            S.Overflow(rcs, "Hidden");
            UIRuntime.AddChild(overlay, rowsVE);
            _listingRowsContainerPtr = UIRuntime.GetPtr(rowsVE);

            // ── Pagination bar ────────────────────────────────────────────────────
            BuildPaginationBar(overlay, PanelH - 24f - PaginationH);
        }

        // ── Single auction row ────────────────────────────────────────────────────
        private void BuildListingRow(object container, CarListing listing, float yOffset)
        {
            const float Pad = 16f;
            const float ImgW = 80f;
            const float ImgH = 62f;
            const float RightW = 180f;

            // ── Row background ───────────────────────────────────────────────────
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

            // ── Bottom separator ─────────────────────────────────────────────────
            var rowSepVE = UIRuntime.NewVE();
            var rss = UIRuntime.GetStyle(rowSepVE);
            S.Position(rss, "Absolute");
            S.Left(rss, Pad); S.Top(rss, RowH - 1f);
            S.Width(rss, PanelW - Pad * 2f); S.Height(rss, 1f);
            S.BgColor(rss, new Color(0.15f, 0.22f, 0.32f, 0.5f));
            UIRuntime.AddChild(container, rowSepVE);

            // ── Thumbnail placeholder ─────────────────────────────────────────────
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

            // Ikona w thumbnailu — jedyne miejsce gdzie zostaje AddLabelToContainer z object
            var iconLbl = _panel.AddLabelToContainer(imgBox,
                "\U0001F697", 0f, 0f, ImgW, ImgH,
                new Color(0.28f, 0.40f, 0.52f, 1f));
            iconLbl.SetFontSize(24);

            // ── Content ───────────────────────────────────────────────────────────
            float contentX = Pad + ImgW + 14f;
            float contentW = PanelW - contentX - RightW - Pad * 2f;

            // Title
            var titleLbl = _panel.AddLabelToContainer(rowPtr,
                $"{listing.Make} {listing.Model}  •  {listing.Year}",
                contentX, 10f, contentW, 24f, Color.white);
            titleLbl.SetFontSize(16);

            // Seller note
            string note = listing.SellerNote.Length > 80
                ? listing.SellerNote.Substring(0, 77) + "..."
                : listing.SellerNote;
            var noteLbl = _panel.AddLabelToContainer(rowPtr,
                $"\"{note}\"",
                contentX, 38f, contentW, 20f, TextGray);
            noteLbl.SetFontSize(12);

            // Timer
            float remaining = listing.ExpiresAt - _listings.GameTime;
            Color timerColor = remaining < 120f
                ? new Color(0.95f, 0.55f, 0.20f, 1f)
                : new Color(0.45f, 0.65f, 0.85f, 1f);
            var timerLbl = _panel.AddLabelToContainer(rowPtr,
                FormatTimer(listing),
                contentX, 62f, 240f, 20f, timerColor);
            timerLbl.SetFontSize(12);
            _timerLabels[listing.InternalId] = timerLbl;

            // ── Right side ────────────────────────────────────────────────────────
            float rightX = PanelW - RightW - Pad;

            // Price
            var priceLbl = _panel.AddLabelToContainer(rowPtr,
                $"${listing.Price:N0}",
                rightX, 10f, RightW, 30f, OXLGreen);
            priceLbl.SetFontSize(20);

            // BUY button
            var buyPtr = _panel.AddButtonToContainer(rowPtr,
                "KUP ▶",
                rightX + RightW - 130f, 46f, 130f, 34f,
                OXLGreen,
                () =>
                {
                    _buyClickConsumed = true;
                    if (_listings.TryPurchase(listing, SpawnCar, DeductMoney))
                        RefreshListings();
                });
            _panel.WireHover(buyPtr,
                OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
        }

        // ── Pagination bar ────────────────────────────────────────────────────────
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
            S.Left(ss, 0f); S.Top(ss, 0f);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(bar, sep);

            float cx = PanelW / 2f;

            var prevPtr = _panel.AddButtonToContainer(
                bar, "◀  Poprzednia", cx - BtnW - 70f, 7f, BtnW, 32f, BtnDark,
                () => { if (_currentPage > 0) { _currentPage--; RefreshListings(); } });
            _panel.WireHover(prevPtr, BtnDark, BtnDarkHi, SearchBdr);

            _pageCountLabel = _panel.AddLabelToContainer(
                bar, "1 / 1", cx - 30f, 0f, 60f, BarH, TextGray);
            _pageCountLabel.SetFontSize(13);

            var nextPtr = _panel.AddButtonToContainer(
                bar, "Następna  ▶", cx + 70f, 7f, BtnW, 32f, BtnDark,
                () =>
                {
                    int total = TotalPages();
                    if (_currentPage < total - 1) { _currentPage++; RefreshListings(); }
                });
            _panel.WireHover(nextPtr, BtnDark, BtnDarkHi, SearchBdr);
        }

        // ── Rebuild current page ──────────────────────────────────────────────────
        private void RefreshListings()
        {
            if (_listingRowsContainerPtr == IntPtr.Zero) return;

            var container = UIRuntime.WrapVE(_listingRowsContainerPtr);
            UIRuntime.VisualElementType.GetMethod("Clear")?.Invoke(container, null);
            _timerLabels.Clear();

            var all = _listings.ActiveListings;
            _currentPage = Mathf.Clamp(_currentPage, 0, Mathf.Max(0, TotalPages() - 1));
            _pageCountLabel?.SetText($"{_currentPage + 1} / {TotalPages()}");

            int start = _currentPage * RowsPerPage;
            int end = Mathf.Min(start + RowsPerPage, all.Count);

            if (all.Count == 0)
            {
                var lbl = Activator.CreateInstance(UIRuntime.LabelType);
                var s = UIRuntime.GetStyle(lbl);
                S.Position(s, "Absolute");
                S.Left(s, 0f); S.Top(s, 260f);
                S.Width(s, PanelW); S.Height(s, 40f);
                S.Color(s, TextDim);
                S.Font(s);
                S.TextAlign(s, TextAnchor.MiddleCenter);
                UIRuntime.LabelType.GetProperty("text")
                    .SetValue(lbl, "\u2014  Brak aktywnych aukcji  \u2014");
                UIRuntime.AddChild(container, lbl);
                return;
            }

            for (int i = start; i < end; i++)
                BuildListingRow(container, all[i], (i - start) * (RowH + RowGap));
        }

        private int TotalPages()
            => Mathf.Max(1, Mathf.CeilToInt(
                   _listings.ActiveListings.Count / (float)RowsPerPage));

        // ── Live timer updates ────────────────────────────────────────────────────
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
            // also update detail overlay timer if open
            if (_detailListing != null && _detailTimer != null)
                _detailTimer.SetText(FormatTimer(_detailListing));
        }

        private string FormatTimer(CarListing listing)
        {
            float rem = listing.ExpiresAt - _listings.GameTime;
            if (rem <= 0f) return "Aukcja zakończona";
            int m = (int)(rem / 60f);
            int s = (int)(rem % 60f);
            string icon = rem < 60f ? "\u26a0 " : "\u23f1 ";
            return $"{icon}Wygasa za {m}:{s:D2}";
        }

        // ── Visibility ────────────────────────────────────────────────────────────
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

        // ═══════════════════════════════════════════════════════════════════════
        //  DETAIL OVERLAY
        // ═══════════════════════════════════════════════════════════════════════

        private void BuildDetailOverlay()
        {
            var overlay = UIRuntime.NewVE();
            var os = UIRuntime.GetStyle(overlay);
            S.Position(os, "Absolute");
            S.Left(os, 0f); S.Top(os, 24f);
            S.Width(os, PanelW); S.Height(os, PanelH - 24f);
            S.BgColor(os, PageBg);
            S.Overflow(os, "Hidden");
            S.Display(os, false);
            _panel.AddOverlayToPanel(overlay);
            _detailOverlayPtr = UIRuntime.GetPtr(overlay);

            // Top bar
            var topBar = UIRuntime.NewVE();
            var ts = UIRuntime.GetStyle(topBar);
            S.Position(ts, "Absolute");
            S.Left(ts, 0f); S.Top(ts, 0f);
            S.Width(ts, PanelW); S.Height(ts, 44f);
            S.BgColor(ts, new Color(0.05f, 0.08f, 0.14f, 1f));
            UIRuntime.AddChild(overlay, topBar);

            var backPtr = _panel.AddButtonToContainer(
                topBar, "\u2190  Lista aukcji", 12f, 6f, 140f, 32f, BtnDark, HideDetail);
            _panel.WireHover(backPtr, BtnDark, BtnDarkHi, SearchBdr);

            _detailTitle = _panel.AddLabelToContainer(
                topBar, "", 170f, 0f, PanelW - 200f, 44f, Color.white);
            _detailTitle.SetFontSize(18);

            var sep = UIRuntime.NewVE();
            var ss = UIRuntime.GetStyle(sep);
            S.Position(ss, "Absolute");
            S.Left(ss, 0f); S.Top(ss, 44f);
            S.Width(ss, PanelW); S.Height(ss, 1f);
            S.BgColor(ss, Border);
            UIRuntime.AddChild(overlay, sep);

            // Large image placeholder
            const float BigImgW = 480f;
            const float BigImgH = 320f;
            float imgLeft = 48f;
            float imgTop = 70f;

            var imgBox = UIRuntime.NewVE();
            var ibs = UIRuntime.GetStyle(imgBox);
            S.Position(ibs, "Absolute");
            S.Left(ibs, imgLeft); S.Top(ibs, imgTop);
            S.Width(ibs, BigImgW); S.Height(ibs, BigImgH);
            S.BgColor(ibs, new Color(0.07f, 0.11f, 0.18f, 1f));
            S.BorderRadius(ibs, 10f);
            S.BorderWidth(ibs, 1f);
            S.BorderColor(ibs, new Color(0.18f, 0.28f, 0.42f, 0.8f));
            UIRuntime.AddChild(overlay, imgBox);

            var imgIcon = Activator.CreateInstance(UIRuntime.LabelType);
            var iils = UIRuntime.GetStyle(imgIcon);
            S.Position(iils, "Absolute");
            S.Left(iils, 0f); S.Top(iils, 0f);
            S.Width(iils, BigImgW); S.Height(iils, BigImgH);
            S.Color(iils, new Color(0.20f, 0.30f, 0.40f, 1f));
            S.Font(iils);
            S.TextAlign(iils, TextAnchor.MiddleCenter);
            S.FontSize(iils, 64);
            UIRuntime.LabelType.GetProperty("text").SetValue(imgIcon, "\U0001F697");
            UIRuntime.AddChild(imgBox, imgIcon);

            // Info panel (right of image)
            float infoX = imgLeft + BigImgW + 40f;
            float infoW = PanelW - infoX - 48f;

            _detailYear = _panel.AddLabelToContainer(
                overlay, "", infoX, imgTop, infoW, 28f, TextGray);
            _detailYear.SetFontSize(14);

            _detailNote = _panel.AddLabelToContainer(
                overlay, "", infoX, imgTop + 50f, infoW, 160f, TextGray);
            _detailNote.SetFontSize(14);

            _detailTimer = _panel.AddLabelToContainer(
                overlay, "", infoX, imgTop + 220f, infoW, 28f,
                new Color(0.45f, 0.65f, 0.85f, 1f));
            _detailTimer.SetFontSize(14);

            _detailPrice = _panel.AddLabelToContainer(
                overlay, "", infoX, imgTop + 258f, infoW, 48f, OXLGreen);
            _detailPrice.SetFontSize(32);

            _detailBuyPtr = _panel.AddButtonToContainer(
                overlay, "KUP TERAZ  ▶",
                infoX, imgTop + 316f, 220f, 52f,
                OXLGreen, () =>
                {
                    if (_detailListing == null) return;
                    if (_listings.TryPurchase(_detailListing, SpawnCar, DeductMoney))
                    {
                        HideDetail();
                        RefreshListings();
                    }
                });
            _panel.WireHover(_detailBuyPtr,
                OXLGreen,
                new Color(0.28f, 0.70f, 0.42f, 1f),
                new Color(0.16f, 0.48f, 0.28f, 1f));
        }

        private void ShowDetail(CarListing listing)
        {
            if (_detailOverlayPtr == IntPtr.Zero) return;
            _detailListing = listing;

            _detailTitle?.SetText($"{listing.Make} {listing.Model}");
            _detailYear?.SetText($"Rok produkcji: {listing.Year}");
            _detailNote?.SetText($"\"{listing.SellerNote}\"");
            _detailTimer?.SetText(FormatTimer(listing));
            _detailPrice?.SetText($"${listing.Price:N0}");

            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailOverlayPtr)), true);
        }

        private void HideDetail()
        {
            if (_detailOverlayPtr == IntPtr.Zero) return;
            _detailListing = null;
            S.Display(UIRuntime.GetStyle(UIRuntime.WrapVE(_detailOverlayPtr)), false);
        }


        // ── Widoczność ────────────────────────────────────────────────────────
        public void Open() { _panel?.SetVisible(true); }
        public void Close() { _panel?.SetVisible(false); }
        public void Toggle() { _panel?.Toggle(); }
    }
}