using CMS2026UITKFramework;
using MelonLoader;
using System;
using System.Linq;
using UnityEngine;

namespace CMS2026_OXL
{
    public class OXLPanel
    {

        //palety z loga
        //zlote litery loga #f8e58f
        //kolor kasy #389757
        //#cccdcf biala czcionka - mala pod logiem

        // ── Paleta ────────────────────────────────────────────────────────────
        private static readonly Color PageBg = new Color(0.035f, 0.059f, 0.106f, 1.00f);
        private static readonly Color Border = new Color(0.220f, 0.592f, 0.341f, 0.40f);
        private static readonly Color TextGray = new Color(0.420f, 0.480f, 0.500f, 1.00f);
        private static readonly Color OXLGreen = new Color(0.220f, 0.592f, 0.341f, 1.00f);
        private static readonly Color BtnDark = new Color(0.075f, 0.110f, 0.180f, 1.00f);
        private static readonly Color BtnDarkHi = new Color(0.110f, 0.170f, 0.260f, 1.00f);
        private static readonly Color SearchBdr = new Color(0.220f, 0.592f, 0.341f, 0.55f);
        private static readonly Color ChromeAddrBg = new Color(0.18f, 0.18f, 0.18f, 1.00f);
        private static readonly Color Transp = new Color(0f, 0f, 0f, 0f);

        private const float PanelW = 1456f;
        private const float PanelH = 980f;

        // ContentW = PanelW - Pad*3 - SbW   (Pad=6, SbW=6 zgodnie z UIPanelBuilder)
        private const float ContentW = PanelW - 6f * 3f - 6f;   // = PanelW - 24

        private UIPanel _panel;
        private ListingSystem _listings = new ListingSystem();

        private Action<string> SpawnCar => id => OXLPlugin.Log.Msg($"[OXL] TODO spawn: {id}");
        private Action<int> DeductMoney => amt => OXLPlugin.Log.Msg($"[OXL] TODO deduct: {amt}");


        public void Build()
        {
            // FIX #4: Wyśrodkowanie na ekranie
            float x = Mathf.Max(0f, (Screen.width - PanelW) / 2f);
            float y = Mathf.Max(0f, (Screen.height - PanelH) / 2f);

            _panel = FrameworkAPI.CreatePanel(title: "OXL", x: x, y: y,width: PanelW, height: PanelH);

            // Przyciski title bara
            _panel.AddTitleButton("\u2014", () => { }, new Color(0.15f, 0.18f, 0.25f, 1f));
            _panel.AddTitleButton("\u25A1", () => { }, new Color(0.15f, 0.18f, 0.25f, 1f));
            _panel.AddTitleButton("\u2715", Close, new Color(0.55f, 0.10f, 0.10f, 1f));

            _panel.Build(sortOrder: 9000);

            // FIX #3: wyłącz drag — okno otwieramy tylko przez F10/X
            _panel.SetDraggable(false);
            _panel.SetScrollbarVisible(false);

            // Styl panelu
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

        // ── Pasek adresu ──────────────────────────────────────────────────────
        private void BuildAddressBar()
        {
            _panel.AddSeparator(Border);

            var row = _panel.AddRow(height: 40f, gap: 4f);
            row.AddSpace(6f);

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
            float urlW = row.RemainingWidth - 36f;
            var urlLabel = row.AddLabel("  \U0001F512  oxl.pl/home", width: urlW, color: TextGray);
            var urlVE = UIRuntime.WrapVE(urlLabel.GetRawPtr());
            var urlSt = UIRuntime.GetStyle(urlVE);
            S.BgColor(urlSt, new Color(0.05f, 0.08f, 0.14f, 1f));
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
            _panel.AddSpace(50f);

            // ── Logo ──────────────────────────────────────────────────────────
            Texture2D logo = TryLoadLogo();
            if (logo != null)
            {
                const float ImgW = 580f;
                const float ImgH = 200f;  // proporcjonalnie do 580×286 skalowanego

                var logoImg = _panel.AddImage(logo, ImgW, ImgH);

                // FIX #1: wyśrodkowanie obrazu — przestaw Left po dodaniu
                float imgLeft = (ContentW - ImgW) / 2f;
                var imgVE = UIRuntime.WrapVE(logoImg.GetRawPtr());
                S.Left(UIRuntime.GetStyle(imgVE), imgLeft);
            }
            else
            {
                // Fallback tekstowy — wyśrodkowany
                var logoLbl = _panel.AddLabel("OXL", OXLGreen, height: 80f);
                logoLbl.SetFontSize(68);

                // FIX #1: TextAlign center na labelu
                var lve = UIRuntime.WrapVE(logoLbl.GetRawPtr());
                //S.TextAlign(UIRuntime.GetStyle(lve), TextAnchor.MiddleCenter);
            }

            // Podtytuł — wyśrodkowany
            var logoSub = _panel.AddLabel("Online eX-Owner Lies", TextGray);
            logoSub.SetFontSize(13);
            var subVE = UIRuntime.WrapVE(logoSub.GetRawPtr());
            //S.TextAlign(UIRuntime.GetStyle(subVE), TextAnchor.MiddleCenter);

            _panel.AddSpace(28f);

            // ── Pole wyszukiwania ─────────────────────────────────────────────
            _panel.AddTextInput(
                "Szukaj aut, marek, roczników...",
                onSubmit: query => OXLPlugin.Log.Msg($"[OXL] Search: {query}"),
                height: 44f);

            _panel.AddSpace(14f);

            // Przyciski — wyśrodkowane przez równy AddSpace
            var btnRow = _panel.AddRow(height: 36f, gap: 8f);
            float bside = (btnRow.RemainingWidth - 248f) / 2f;
            btnRow.AddSpace(bside);
            var bSearch = btnRow.AddButton("Szukaj w OXL", width: 120f,
                onClick: () => OXLPlugin.Log.Msg("[OXL] Search"), bgColor: BtnDark);
            var bLucky = btnRow.AddButton("Mam szcz\u0119\u015Bcie", width: 120f,
                onClick: () => OXLPlugin.Log.Msg("[OXL] Lucky"), bgColor: BtnDark);
            bSearch.SetTextColor(TextGray);
            bLucky.SetTextColor(TextGray);
            _panel.WireHover(bSearch.GetRawPtr(), BtnDark, BtnDarkHi, SearchBdr);
            _panel.WireHover(bLucky.GetRawPtr(), BtnDark, BtnDarkHi, SearchBdr);

            // ── Kategorie ─────────────────────────────────────────────────────
            _panel.AddSpace(36f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(10f);

            var catRow = _panel.AddRow(height: 28f, gap: 0f);
            float catSide = (catRow.RemainingWidth - 490f) / 2f;
            catRow.AddSpace(catSide);
            catRow.AddLabel("\U0001F697  Samochody osobowe", width: 190f, color: OXLGreen);
            catRow.AddLabel("\U0001F527  Cz\u0119\u015Bci (WIP)", width: 150f, color: TextGray);
            catRow.AddLabel("\U0001F690  Dostawcze (WIP)", width: 150f, color: TextGray);

            _panel.AddSpace(10f);
            _panel.AddSeparator(Border);
            _panel.AddSpace(20f);

            var wip = _panel.AddLabel("\u2014 Reszta strony w budowie (WIP) \u2014", TextGray);
            wip.SetFontSize(11);
            // FIX #1: wyśrodkowanie labelu WIP
            //S.TextAlign(UIRuntime.GetStyle(UIRuntime.WrapVE(wip.GetRawPtr())),
            //            TextAnchor.MiddleCenter);
        }

        // ── Ładowanie logo.png ────────────────────────────────────────────────
        private Texture2D TryLoadLogo()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    Application.dataPath, "..", "Mods", "CMS2026_OXL", "logo.png");

                if (!System.IO.File.Exists(path))
                {
                    OXLPlugin.Log.Msg($"[OXL] logo.png not found: {path}");
                    return null;
                }

                byte[] bytes = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                var il2Bytes = new Il2CppInterop.Runtime.InteropTypes
                    .Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2Bytes[i] = bytes[i];

                var icType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName == "UnityEngine.ImageConversion");

                if (icType == null) return null;

                var loadImg = icType.GetMethods()
                    .FirstOrDefault(m => m.Name == "LoadImage"
                                     && m.GetParameters().Length == 2);
                if (loadImg == null) return null;

                bool ok = (bool)loadImg.Invoke(null, new object[] { tex, il2Bytes });
                if (ok)
                {
                    OXLPlugin.Log.Msg("[OXL] logo.png loaded OK");
                    return tex;
                }
                return null;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] logo.png error: {ex.Message}");
                return null;
            }
        }

        // ── Widoczność ────────────────────────────────────────────────────────
        public void Open()
        {
            if (_panel == null) return;

            // FIX #4: przelicz pozycję przy każdym otwarciu (zmiana rozdzielczości)
            // Uwaga: po Build() pozycja jest już ustawiona; to jest opcjonalny refresh
            _panel.SetVisible(true);
        }

        public void Close()
        {
            if (_panel == null) return;
            _panel.SetVisible(false);
        }

        public void Toggle()
        {
            if (_panel == null) return;
            _panel.Toggle();
        }
    }
}