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

            _panel.SetUpdateCallback(dt => _listings.Tick(dt));
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
            AddIconButton(row, _icoMenu, "\u22EE", 36f, () => { });

            _panel.AddSeparator(Border);
        }

        // helper: ikona lub fallback tekstowy
        private void AddIconButton(UIRowBuilder row, Texture2D icon, string fallbackChar,float width, Action onClick)
        {
            if (icon != null)
            {
                var ve = UIRuntime.NewVE();
                var st = UIRuntime.GetStyle(ve);
                S.Width(st, width);
                S.Height(st, width); // kwadrat
                S.BorderRadius(st, 4f);
                UIRuntime.SetBackgroundImage(ve, icon);

                var ptr = UIRuntime.GetPtr(ve);
                row.AddRaw(ve, width + 2f);
                _panel.WireHover(ptr, Transp, BtnDark, BtnDarkHi);
                // klik przez PointerDown — jeśli framework ma WireClick, użyj go
                // (na razie ikony nawigacji nie potrzebują akcji)
            }
            else
            {
                var btn = row.AddButton(fallbackChar, width, onClick, Transp);
                btn.SetTextColor(TextGray);
                _panel.WireHover(btn.GetRawPtr(), Transp, BtnDark, BtnDarkHi);
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
                "Szukaj aut, marek, rocznik\u00f3w...",
                onSubmit: q => OXLPlugin.Log.Msg($"[OXL] Search: {q}"),
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
            catRow.AddLabel("\U0001F697  Samochody osobowe", width: 190f, color: OXLGreen);
            catRow.AddLabel("\U0001F527  Cz\u0119\u015Bci (WIP)", width: 150f, color: TextGray);
            catRow.AddLabel("\U0001F690  Dostawcze (WIP)", width: 150f, color: TextGray);

            _panel.AddSpace(10f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(20f);

            var wip = _panel.AddLabel("\u2014 Reszta strony w budowie (WIP) \u2014", TextGray);
            wip.SetFontSize(11);
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

        // ── Widoczność ────────────────────────────────────────────────────────
        public void Open() { _panel?.SetVisible(true); }
        public void Close() { _panel?.SetVisible(false); }
        public void Toggle() { _panel?.Toggle(); }
    }
}