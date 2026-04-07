using CMS2026UITKFramework;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;

namespace CMS2026_OXL
{
    public class OXLPanel
    {
        // ── Paleta — ciemny motyw (OXL logo style) ───────────────────────────
        private static readonly Color PageBg = new Color(0.05f, 0.07f, 0.10f, 1.00f); // prawie-czarny
        private static readonly Color ChromeBg = new Color(0.07f, 0.09f, 0.14f, 1.00f); // ciemny chrome
        private static readonly Color Border = new Color(0.10f, 0.28f, 0.28f, 1.00f); // teal border
        private static readonly Color TextDark = new Color(0.88f, 0.95f, 0.95f, 1.00f); // jasny tekst
        private static readonly Color TextGray = new Color(0.45f, 0.58f, 0.60f, 1.00f); // szary tekst
        private static readonly Color OXLTeal = new Color(0.08f, 0.78f, 0.72f, 1.00f); // cyan glow (jak logo)
        private static readonly Color OXLGreen = new Color(0.08f, 0.82f, 0.38f, 1.00f); // zielony akcent
        private static readonly Color CloseRed = new Color(0.65f, 0.12f, 0.12f, 1.00f);
        private static readonly Color BtnDark = new Color(0.10f, 0.14f, 0.20f, 1.00f);
        private static readonly Color BtnDarkHi = new Color(0.14f, 0.22f, 0.30f, 1.00f);
        private static readonly Color Transp = new Color(0.00f, 0.00f, 0.00f, 0.00f);
        private static readonly Color SearchBdr = new Color(0.08f, 0.55f, 0.55f, 0.60f); // teal border
        private static readonly Color White = new Color(1.00f, 1.00f, 1.00f, 1.00f);

        private UIPanel _panel;
        private ListingSystem _listings = new ListingSystem();

        private Action<string> SpawnCar => id => OXLPlugin.Log.Msg($"[OXL] TODO spawn: {id}");
        private Action<int> DeductMoney => amt => OXLPlugin.Log.Msg($"[OXL] TODO deduct: {amt}");

        public void Build()
        {
            _panel = FrameworkAPI.CreatePanel(
                title: "  \U0001F4BB  OXL — Online eX-Owner Lies",
                x: 100f, y: 40f,
                width: 760f, height: 580f,
                sortOrder: 8000);

            // Przyciski tytułu — przed Build() frameworka (tu używamy kolejności dodawania)
            _panel.AddTitleButton("\u2715", Close,
                new Color(0.65f, 0.12f, 0.12f, 1f));       // ✕ close
            _panel.AddTitleButton("\u25A1", () => { },
                new Color(0.15f, 0.18f, 0.25f, 1f));       // □ max
            _panel.AddTitleButton("\u2014", () => { },
                new Color(0.15f, 0.18f, 0.25f, 1f));       // — min

            _panel.SetScrollbarVisible(false);
            _panel.SetDragWhenScrollable(true); // tytuł teraz jest chrome barem

            // Styl panelu — ciemny
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

        // ── Chrome: pasek adresu ──────────────────────────────────────────────
        private void BuildAddressBar()
        {
            _panel.AddSeparator(Border);

            var row = _panel.AddRow(height: 40f, gap: 4f);

            row.AddSpace(6f);

            // Przyciski nawigacji — przezroczyste, widoczny hover
            var bBack = row.AddButton("\u2190", 28f, () => { }, Transp);
            var bFwd = row.AddButton("\u2192", 28f, () => { }, Transp);
            var bRefresh = row.AddButton("\u21BB", 28f, () => { }, Transp);
            bBack.SetTextColor(TextGray);
            bFwd.SetTextColor(TextGray);
            bRefresh.SetTextColor(TextGray);
            _panel.WireHover(bBack.GetRawPtr(), Transp, BtnDark, BtnDarkHi);
            _panel.WireHover(bFwd.GetRawPtr(), Transp, BtnDark, BtnDarkHi);
            _panel.WireHover(bRefresh.GetRawPtr(), Transp, BtnDark, BtnDarkHi);

            row.AddSpace(6f);

            // Pole URL — zaokrąglony prostokąt
            float urlW = row.RemainingWidth - 36f;
            var urlLabel = row.AddLabel("  \U0001F512  oxl.pl/home", width: urlW, color: TextGray);
            var urlVE = UIRuntime.WrapVE(urlLabel.GetRawPtr());
            var urlSt = UIRuntime.GetStyle(urlVE);
            S.BgColor(urlSt, new Color(0.92f, 0.92f, 0.93f, 1f));
            S.BorderRadius(urlSt, 20f);
            S.BorderWidth(urlSt, 1f);
            S.BorderColor(urlSt, SearchBdr);

            row.AddSpace(4f);
            var bMenu = row.AddButton("\u22EE", 28f, () => { }, Transp);
            bMenu.SetTextColor(TextGray);
            _panel.WireHover(bMenu.GetRawPtr(), Transp, BtnDark, BtnDarkHi);

            _panel.AddSeparator(Border);
        }

        // ── Treść strony ──────────────────────────────────────────────────────
        private void BuildContent()
        {
            // Tło treści — lekko ciepłe białe
            _panel.AddSpace(0f);
            var bgLabel = _panel.AddLabel("", PageBg, height: 460f);
            var bgVE = UIRuntime.WrapVE(bgLabel.GetRawPtr());
            S.BgColor(UIRuntime.GetStyle(bgVE), PageBg);
            _panel.AddSpace(-460f);

            // ── Logo ──────────────────────────────────────────────────────────
            _panel.AddSpace(48f);

            Texture2D logo = TryLoadLogo();
            if (logo != null)
            {
                _panel.AddImage(logo, 0f, 108f);  // 0 = pełna szerokość, 108px wys.
            }
            else
            {
                // Fallback tekstowy (identyczny styl jak Google)
                var logoRow = _panel.AddRow(height: 90f, gap: 0f);
                float logoSide = (logoRow.RemainingWidth - 200f) / 2f;
                logoRow.AddSpace(logoSide);
                var lbl = logoRow.AddLabel("OXL", width: 200f, color: OXLGreen);
                lbl.SetFontSize(68);
            }

            // ── Podtytuł ──────────────────────────────────────────────────────
            _panel.AddSpace(2f);
            var sub = _panel.AddLabel("Online eX-Owner Lies", TextGray);
            sub.SetFontSize(13);
            var subVE = UIRuntime.WrapVE(sub.GetRawPtr());
            //S.TextAlign(UIRuntime.GetStyle(subVE), TextAnchor.MiddleCenter);

            // ── Pole wyszukiwania ─────────────────────────────────────────────
            _panel.AddSpace(28f);

            // Kontener pola search — zaokrąglony, z obramowaniem
            var searchInput = _panel.AddTextInput(
                "Szukaj aut, marek, roczników...",
                onSubmit: query => OXLPlugin.Log.Msg($"[OXL] Search: {query}"),
                height: 44f);

            // ── Przyciski wyszukiwania — wyśrodkowane ─────────────────────────
            _panel.AddSpace(14f);

            var btnRow = _panel.AddRow(height: 36f, gap: 8f);
            float bside = (btnRow.RemainingWidth - 248f) / 2f;
            btnRow.AddSpace(bside);

            var bSearch = btnRow.AddButton("Szukaj w OXL", width: 120f,
                onClick: () => OXLPlugin.Log.Msg("[OXL] Search"),
                bgColor: BtnDark);
            var bLucky = btnRow.AddButton("Mam szcz\u0119\u015Bcie", width: 120f,
                onClick: () => OXLPlugin.Log.Msg("[OXL] Lucky"),
                bgColor: BtnDark);

            bSearch.SetTextColor(TextDark);
            bLucky.SetTextColor(TextDark);
            _panel.WireHover(bSearch.GetRawPtr(), BtnDark, BtnDarkHi, SearchBdr);
            _panel.WireHover(bLucky.GetRawPtr(), BtnDark, BtnDarkHi, SearchBdr);

            // ── Pasek kategorii ───────────────────────────────────────────────
            _panel.AddSpace(36f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(10f);

            var catRow = _panel.AddRow(height: 28f, gap: 0f);
            catRow.AddSpace(48f);

            var cCars = catRow.AddLabel("\U0001F697  Samochody osobowe", width: 190f, color: OXLGreen);
            var cParts = catRow.AddLabel("\U0001F527  Cz\u0119\u015Bci (WIP)", width: 150f, color: TextGray);
            var cVans = catRow.AddLabel("\U0001F690  Dostawcze (WIP)", width: 150f, color: TextGray);

            // Podkreślenie aktywnej kategorii
            var carsVE = UIRuntime.WrapVE(cCars.GetRawPtr());
            var carsSt = UIRuntime.GetStyle(carsVE);
            S.BorderColor(carsSt, OXLGreen);
            S.BorderWidth(carsSt, 2f);   // dolna krawędź jako "tab underline"

            _panel.AddSpace(10f);
            _panel.AddSeparator(Border);

            // ── WIP placeholder ───────────────────────────────────────────────
            _panel.AddSpace(20f);
            var wip = _panel.AddLabel("\u2014 Reszta strony w budowie (WIP) \u2014", TextGray);
            wip.SetFontSize(11);
            var wipVE = UIRuntime.WrapVE(wip.GetRawPtr());
            //S.TextAlign(UIRuntime.GetStyle(wipVE), TextAnchor.MiddleCenter);
        }

        // ── Ładowanie logo.png z folderu Mods ────────────────────────────────
        private Texture2D TryLoadLogo()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
                string path = System.IO.Path.Combine(dir, "logo.png");

                if (!System.IO.File.Exists(path))
                {
                    OXLPlugin.Log.Warning($"[OXL] logo.png not found at: {path}");
                    return null;
                }

                byte[] bytes = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2);

                var icAsm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "UnityEngine.ImageConversionModule");

                if (icAsm == null) { OXLPlugin.Log.Warning("[OXL] ImageConversionModule not found"); return null; }

                var il2b = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                icAsm.GetType("UnityEngine.ImageConversion")
                     .GetMethod("LoadImage", new System.Type[] { typeof(Texture2D), il2b.GetType() })
                     ?.Invoke(null, new object[] { tex, il2b });

                OXLPlugin.Log.Msg("[OXL] logo.png loaded OK");
                return tex;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Warning($"[OXL] logo.png load error: {ex.Message}");
                return null;
            }
        }

        public void Open() { _panel?.SetVisible(true); }
        public void Close() { _panel?.SetVisible(false); }
        public void Toggle() { _panel?.Toggle(); }
    }
}