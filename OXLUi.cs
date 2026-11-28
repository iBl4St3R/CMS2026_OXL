// OXLUi.cs
// Małe klocki UI nowej strony OXL (site.md 3): pudełka, teksty, przyciski, obrazki z gry, pole tekstowe. Wszystko absolutnie
// pozycjonowane w kontenerze przeglądarki BlastCoreOS, kolory w jednym miejscu (OXLSite.Theme). Tylko ASCII w tekstach (zasada 8).

using System;
using BlastCoreOS;
using CMS2026UITKFramework;
using UnityEngine;

namespace CMS2026_OXL
{
    internal static class OXLUi
    {
        private static Font _font, _plainFont;
        private static bool _fontResolved;

        // Pogrubione: Rubik z gry. Zwykły tekst: LiberationSans (krój typu Arial, ostrzejszy w małych rozmiarach na monitorze w grze;
        // użytkownik 2026-11-25: cienki Rubik na białym tle wyglądał na rozmazany). Brak czcionki = domyślna frameworka.
        private static void ApplyFont(object style, bool bold = true)
        {
            if (!_fontResolved)
            {
                _fontResolved = true;
                try { _font = UIRuntime.FindFont("Rubik-VariableFont_wght"); } catch { _font = null; }
                try { _plainFont = UIRuntime.FindFont("LiberationSans"); } catch { _plainFont = null; }
            }
            var f = bold ? _font : (_plainFont ?? _font);
            if (f != null) S.Font(style, f); else S.Font(style);
        }

        public static object Box(object parent, float x, float y, float w, float h, Color bg, float radius = 0f, Color? border = null, float borderWidth = 0f)
        {
            var ve = UIRuntime.NewVE();
            var s = UIRuntime.GetStyle(ve);
            S.Position(s, "Absolute");
            S.Left(s, x); S.Top(s, y); S.Width(s, w); S.Height(s, h);
            S.BgColor(s, bg);
            if (radius > 0f) S.BorderRadius(s, radius);
            if (border.HasValue && borderWidth > 0f) { S.BorderColor(s, border.Value); S.BorderWidth(s, borderWidth); }
            S.Overflow(s, "Hidden");
            UIRuntime.AddChild(parent, ve);
            return ve;
        }

        public static UILabelHandle Text(UIPanel panel, object parent, string text, float x, float y, float w, float h, Color color, int size, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft, bool wrap = false)
        {
            var lbl = panel.AddLabelToContainer(parent, text ?? "", x, y, w, h, color);
            lbl.SetFontSize(size);
            var s = UIRuntime.GetStyle(UIRuntime.WrapVE(lbl.GetRawPtr()));
            ApplyFont(s, bold);
            S.FontBold(s, bold);
            S.TextAlign(s, align);
            if (wrap) S.WhiteSpaceNormal(s);
            return lbl;
        }

        /// <summary>Przybliżona liczba linii tekstu zawijanego w szerokości w (do liczenia wysokości strony).</summary>
        public static int Lines(string text, float w, int size)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            int perLine = Math.Max(10, (int)(w / (size * 0.52f)));
            int lines = 0;
            foreach (var para in text.Split('\n')) lines += Math.Max(1, (para.Length + perLine - 1) / perLine);
            return lines;
        }

        public static IntPtr Button(UIPanel panel, BlastCoreWebPageContext ctx, object parent, string text, float x, float y, float w, float h, Color bg, Color hover, Color fg, int size, Action onClick, float radius = 6f, bool bold = true)
        {
            Action click = onClick == null ? null : BlastCoreSoundLibrary.WithClick(ctx.AudioSource, onClick);
            var ptr = panel.AddButtonToContainer(parent, text ?? "", x, y, w, h, bg, click);
            var s = UIRuntime.GetStyle(UIRuntime.WrapVE(ptr));
            ApplyFont(s, bold);
            S.FontSize(s, size);
            S.FontBold(s, bold);
            S.Color(s, fg);
            S.BorderWidth(s, 0f);
            S.BorderRadius(s, radius);
            panel.WireHover(ptr, bg, hover, hover);
            return ptr;
        }

        /// <summary>Całe pudełko (karta ogłoszenia, kafel) jako przycisk: hover zmienia tło.</summary>
        public static void Clickable(UIPanel panel, BlastCoreWebPageContext ctx, object ve, Color normal, Color hover, Action onClick)
        {
            var ptr = UIRuntime.GetPtr(ve);
            panel.WireClick(ptr, BlastCoreSoundLibrary.WithClick(ctx.AudioSource, onClick));
            panel.WireHover(ptr, normal, hover, hover);
        }

        /// <summary>Sprite z gry wpisany w pudełko z zachowaniem proporcji (contain), wyśrodkowany. null = napis "No photo".</summary>
        public static void Sprite(UIPanel panel, object parent, Sprite sprite, float x, float y, float w, float h, Color placeholderText)
        {
            if (sprite == null) { Text(panel, parent, "No photo", x, y, w, h, placeholderText, 13, false, TextAnchor.MiddleCenter); return; }
            float sw = sprite.rect.width, sh = sprite.rect.height;
            float scale = Math.Min(w / Math.Max(1f, sw), h / Math.Max(1f, sh));
            float dw = sw * scale, dh = sh * scale;
            var ve = Box(parent, x + (w - dw) / 2f, y + (h - dh) / 2f, dw, dh, new Color(0f, 0f, 0f, 0f));
            UIRuntime.SetBackgroundSprite(ve, sprite);
        }

        public static void Texture(object parent, Texture2D tex, float x, float y, float w, float h, float radius = 0f)
        {
            if (tex == null) return;
            var ve = Box(parent, x, y, w, h, new Color(0f, 0f, 0f, 0f), radius);
            UIRuntime.SetBackgroundImage(ve, tex);
        }

        // ── Gwiazdki oceny sprzedawcy ────────────────────────────────────────────
        // Rysowane w kodzie: znak gwiazdki font UI Toolkitu pokazuje jako kwadrat (zasada 8), a mod nie ma pliku z ikoną.
        private static Texture2D _starFull, _starHalf, _starEmpty;

        /// <summary>5 gwiazdek oceny (połówki od 0,5) w rzędzie od (x, y); zwraca szerokość rzędu, żeby obok postawić liczbę.</summary>
        public static float Stars(object parent, float rating, float x, float y, float size)
        {
            if (_starFull == null) { _starFull = StarTexture(1f); _starHalf = StarTexture(0.5f); _starEmpty = StarTexture(0f); }
            float step = size + 3f;
            for (int i = 0; i < 5; i++)
            {
                float fill = Mathf.Clamp01(rating - i);
                var tex = fill >= 0.75f ? _starFull : fill >= 0.25f ? _starHalf : _starEmpty;
                Texture(parent, tex, x + i * step, y, size, size);
            }
            return 5f * step - 3f;
        }

        // Gwiazda pięcioramienna 64x64 z wygładzaniem (4x4 próbki na piksel); fill = jaka część od lewej jest złota.
        private static Texture2D StarTexture(float fill)
        {
            const int N = 64;
            var gold = Hex("#F2B01E");
            var gray = Hex("#D3D8DE");
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;   // pierwszy wierzchołek u góry (oś Y tekstury rośnie w górę)
                float r = (i % 2 == 0) ? 30f : 12.2f;
                pts[i] = new Vector2(N / 2f + r * Mathf.Cos(a), N / 2f - 1.5f + r * Mathf.Sin(a));
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int py = 0; py < N; py++)
                for (int px = 0; px < N; px++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (InPolygon(pts, px + (sx + 0.5f) / 4f, py + (sy + 0.5f) / 4f)) hits++;
                    var c = (px + 0.5f) / N <= fill ? gold : gray;
                    c.a = hits / 16f;
                    tex.SetPixel(px, py, c);
                }
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return tex;
        }

        private static bool InPolygon(Vector2[] p, float x, float y)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > y) != (p[j].y > y) && x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x) inside = !inside;
            return inside;
        }

        // ── Pole tekstowe (wyszukiwarka) ─────────────────────────────────────────
        public static object TextField(object parent, float x, float y, float w, float h, string initial, Color bg, Color fg, Color border)
        {
            var tf = Activator.CreateInstance(UIRuntime.TextFieldType);
            var s = UIRuntime.GetStyle(tf);
            S.Position(s, "Absolute");
            S.Left(s, x); S.Top(s, y); S.Width(s, w); S.Height(s, h);
            S.BgColor(s, bg);
            S.BorderRadius(s, 6f);
            S.BorderWidth(s, 1f);
            S.BorderColor(s, border);
            S.Padding(s, 8f);
            S.Color(s, fg);
            ApplyFont(s, false);
            S.FontSize(s, 15);
            UIRuntime.TextFieldType.GetProperty("value")?.SetValue(tf, initial ?? "");
            UIRuntime.AddChild(parent, tf);
            return tf;
        }

        public static string FieldValue(object tf) => (string)(UIRuntime.TextFieldType.GetProperty("value")?.GetValue(tf) ?? "");

        /// <summary>Enter w polu tekstowym (wzór z dawnego OXLHomePage). Błąd tylko w logu: zostaje przycisk.</summary>
        public static void OnEnter(object tf, Action onEnter)
        {
            try
            {
                var trickleType = UIRuntime.UEAsm.GetType("UnityEngine.UIElements.TrickleDown");
                var reg = System.Linq.Enumerable.First(UIRuntime.VisualElementType.GetMethods(), m => m.Name == "RegisterCallback" && m.IsGenericMethod && m.GetParameters().Length == 2).MakeGenericMethod(typeof(UnityEngine.UIElements.KeyDownEvent));
                Action<UnityEngine.UIElements.KeyDownEvent> handler = evt => { if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) onEnter(); };
                var il2cb = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<UnityEngine.UIElements.EventCallback<UnityEngine.UIElements.KeyDownEvent>>(handler);
                reg.Invoke(tf, new object[] { il2cb, Enum.Parse(trickleType, "TrickleDown") });
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:SITE] OnEnter failed: {ex.Message}"); }
        }

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        // ── Obrazki moda z dysku (mapka lokalizacji), z cache ───────────────────────
        private static readonly System.Collections.Generic.Dictionary<string, Texture2D> _textures = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

        public static Texture2D ModImage(string fileName) => LoadPng(System.IO.Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "Images", fileName));

        /// <summary>Grafika z plików BlastCoreOS (Mods/BlastCoreOS/resources/image), np. logo banku Redline w stopce. Brak pliku = null, strona bez obrazka.</summary>
        public static Texture2D BlastCoreImage(string fileName) => LoadPng(System.IO.Path.Combine(Application.dataPath, "..", "Mods", "BlastCoreOS", "resources", "image", fileName));

        private static Texture2D LoadPng(string path)
        {
            if (_textures.TryGetValue(path, out var cached) && cached != null) return cached;
            try
            {
                if (!System.IO.File.Exists(path)) return null;
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var il2 = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2[i] = bytes[i];
                var ic = System.Linq.Enumerable.FirstOrDefault(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == "UnityEngine.ImageConversionModule")?.GetType("UnityEngine.ImageConversion");
                var load = ic?.GetMethod("LoadImage", new[] { typeof(Texture2D), il2.GetType() });
                if (load == null || !(bool)load.Invoke(null, new object[] { tex, il2 })) return null;
                tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                _textures[path] = tex;
                return tex;
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:SITE] image '{path}' failed: {ex.Message}"); return null; }
        }

        /// <summary>
        /// Zdjęcie z gry do pliku PNG dla maila (site.md 13): sprite z atlasu (niekoniecznie czytelny z CPU) kopiowany przez RenderTexture.
        /// Zwraca ścieżkę względną do Resources/Images (np. "mail/item_ab12.png") do znacznika [img=...] poczty BlastCoreOS; null przy błędzie (mail bez zdjęcia).
        /// </summary>
        public static string ExportSprite(Sprite sprite, string name, int maxWidth = 288)
        {
            if (sprite == null) return null;
            try
            {
                var src = sprite.texture;
                Rect r = sprite.textureRect;
                // Mniejsze zdjęcie (użytkownik 2026-11-25: w mailu za duże; 2026-11-28: jeszcze o 20%, 360 na 288 px): najwyżej maxWidth px szerokości, proporcje zachowane.
                float scale = Math.Min(1f, maxWidth / Math.Max(1f, r.width));
                int w = Math.Max(1, (int)(r.width * scale)), h = Math.Max(1, (int)(r.height * scale));
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(src, rt, new Vector2(r.width / src.width, r.height / src.height), new Vector2(r.x / src.width, r.y / src.height));
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                var png = ImageConversion.EncodeToPNG(tex);
                UnityEngine.Object.Destroy(tex);
                if (png == null || png.Length == 0) return null;
                byte[] bytes = new byte[png.Length];
                for (int i = 0; i < png.Length; i++) bytes[i] = png[i];
                string dir = System.IO.Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "Images", "mail");
                System.IO.Directory.CreateDirectory(dir);
                string file = name + ".png";
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, file), bytes);
                return "mail/" + file;
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:MAIL] photo export '{name}' failed: {ex.Message}"); return null; }
        }

        /// <summary>
        /// Opisy sprzedawców (SellerNoteBuilder) mają kolory pod dawne ciemne tło (#cccccc, #ffdd88...): na białej stronie znikały (zrzut ss16).
        /// Kolor zostaje jako wskazówka, ale przyciemniony do czytelnego; szarości stają się zwykłym ciemnym tekstem.
        /// </summary>
        public static string ReadableOnWhite(string rich, Color ink)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            return System.Text.RegularExpressions.Regex.Replace(rich, "<color=([^>]+)>", m =>
            {
                string v = m.Groups[1].Value.Trim('"', '\'');
                if (!ColorUtility.TryParseHtmlString(v, out var c)) return "<color=#" + ColorUtility.ToHtmlStringRGB(ink) + ">";
                Color.RGBToHSV(c, out float h, out float s, out float val);
                if (s < 0.25f) return "<color=#" + ColorUtility.ToHtmlStringRGB(ink) + ">";
                var dark = Color.HSVToRGB(h, Math.Max(s, 0.75f), Math.Min(val, 0.55f));
                return "<color=#" + ColorUtility.ToHtmlStringRGB(dark) + ">";
            });
        }

        /// <summary>Stały hash tekstu (string.GetHashCode w .NET 6 zmienia się między uruchomieniami): ten sam tytuł auta po każdym odświeżeniu.</summary>
        public static int StableHash(string s)
        {
            unchecked { int h = (int)2166136261; foreach (char c in s ?? "") h = (h ^ c) * 16777619; return h & 0x7FFFFFFF; }
        }
    }
}
