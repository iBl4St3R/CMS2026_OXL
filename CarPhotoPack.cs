// CarPhotoPack.cs
// Paczka prawdziwych zdjęć aut (rebuild.md 2 i 7, site.md 10): Resources/CarPhotos/{carId}/{wersja}/{NN_RRGGBB}/{030|3060|60100}/spotNNN_MM.webp.
// Paczka jest opcjonalna: bez folderu, bez zdjęć danego koloru i stanu albo bez dekodera ogłoszenie ma zdjęcie z gry ze znaczkiem "Stock photo".
// Loader nie zakłada numeracji ani kompletu (użytkownik kasuje nieudane ujęcia ręcznie): skanuje pliki i grupuje je po miejscu (prefiks "spotNNN"),
// a jedno ogłoszenie dostaje zdjęcia jednego miejsca, więc wszystkie mają to samo tło.
// WebP dekoduje ImageSharp (UserLibs/SixLabors.ImageSharp.dll) w wątku tła; tekstura Unity powstaje dopiero w Tick() na głównym wątku.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CMS2026UITKFramework;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class CarPhotoPack
    {
        // Ścieżki w ogłoszeniu (CarListing.PhotoFiles) są względne do Resources, z tym prefiksem; stare ścieżki paczek 2026 strona pomija.
        public const string Prefix = "CarPhotos/";
        private static readonly string[] Extensions = { ".webp", ".jpg", ".jpeg", ".png" };

        private sealed class ColorDir { public int Index; public string Hex = ""; public Dictionary<string, List<List<string>>> Spots = new Dictionary<string, List<List<string>>>(); }

        private static readonly Dictionary<string, List<ColorDir>> _index = new Dictionary<string, List<ColorDir>>(StringComparer.OrdinalIgnoreCase);
        private static int _decoderState; // 0 = nie sprawdzono, 1 = jest, -1 = brak DLL

        public static string ResourcesRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources"));

        /// <summary>Wejście do garażu: paczka mogła się zmienić (gracz dograł albo usunął zdjęcia), skan od nowa.</summary>
        public static void Invalidate() { _index.Clear(); _tried.Clear(); }

        /// <summary>Czy ImageSharp się ładuje. Brak DLL w UserLibs = cała paczka wyłączona, ogłoszenia ze zdjęciem z gry.</summary>
        public static bool DecoderAvailable
        {
            get
            {
                if (_decoderState == 0)
                {
                    try { _decoderState = Type.GetType("SixLabors.ImageSharp.Image, SixLabors.ImageSharp") != null ? 1 : -1; }
                    catch { _decoderState = -1; }
                    if (_decoderState < 0) OXLPlugin.Log.Warning("[OXL:PHOTO] SixLabors.ImageSharp.dll not found in UserLibs; photo pack disabled, listings use stock photos.");
                }
                return _decoderState > 0;
            }
        }

        /// <summary>
        /// Folder stanu zdjęć wg wyglądu auta po dostawie (zasada 5: zdjęcie = prawdziwy stan). Liczy się karoseria (BodyCondition), bo ją widać na zdjęciu:
        /// dealer odmalowuje (60100), wrecker ma zapuszczone (030), uczciwy pokazuje swój stan.
        /// Wyjątek (decyzja użytkownika 2026-11-26): scammer wrzuca ładne zdjęcia (60100), choć przyjeżdża wrak; zdjęcie katalogowe wyglądało dziwnie.
        /// </summary>
        public static string ConditionFolder(CarListing l)
        {
            if (l == null) return null;
            if (l.Archetype == SellerArchetype.Scammer) return "60100";
            float body = l.BodyCondition;
            return body < 0.30f ? "030" : body < 0.60f ? "3060" : "60100";
        }

        /// <summary>
        /// Przy generowaniu: wybiera kolor z paczki (tylko kolory, które mają zdjęcia w potrzebnym stanie) i zdjęcia jednego miejsca.
        /// Ustawia Color ("#RRGGBB", kolor z gry, więc dostawa trafi w ten sam lakier), ColorIndex i PhotoFiles. False = ogłoszenie bez paczki.
        /// </summary>
        public static bool TryAssign(CarListing l, string carId, System.Random rng)
        {
            try
            {
                string want = ConditionFolder(l);
                if (want == null || !DecoderAvailable) return false;
                // Auto z paczki nigdy nie dostaje zdjęcia z gry (użytkownik 2026-11-26): brak folderu tej wersji = zdjęcia innej wersji tego samego auta
                // (ta sama karoseria), brak zdjęć w tym stanie = najbliższy stan.
                var all = Colors(carId, l.CarConfig);
                if (all.Count == 0) all = OtherVersions(carId, l.CarConfig);
                if (all.Count == 0) return false;
                string cond = NearestConditions(want).FirstOrDefault(c => all.Any(x => x.Spots.ContainsKey(c)));
                if (cond == null) return false;
                var colors = all.Where(c => c.Spots.ContainsKey(cond)).ToList();
                // Ogłoszenie, które już ma kolor (stare, sprzed paczki): ten sam lakier, jeśli paczka go ma, żeby opis i dostawa dalej się zgadzały.
                var same = colors.Where(c => string.Equals("#" + c.Hex, l.Color, StringComparison.OrdinalIgnoreCase)).ToList();
                var color = same.Count > 0 ? same[0] : colors[rng.Next(colors.Count)];
                var spots = color.Spots[cond];
                // Miejsca z kilkoma ujęciami mają pierwszeństwo: galeria z 1 zdjęciem wygląda ubogo.
                var rich = spots.Where(s => s.Count >= 2).ToList();
                var spot = (rich.Count > 0 ? rich : spots)[rng.Next(rich.Count > 0 ? rich.Count : spots.Count)];
                l.Color = "#" + color.Hex;
                l.ColorIndex = color.Index;
                l.PhotoFiles = new List<string>(spot);
                OXLLog.Msg($"[OXL:PHOTO] {carId}#{l.CarConfig} color={color.Index:D2}_{color.Hex} cond={cond} spot={Path.GetFileName(spot[0])} photos={spot.Count}");
                return true;
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:PHOTO] assign {carId}#{l?.CarConfig} failed: {ex.Message}"); return false; }
        }

        /// <summary>Zdjęcia ogłoszenia z paczki, które dalej leżą na dysku (gracz mógł je usunąć). Pusta lista = strona pokazuje zdjęcie z gry.</summary>
        public static List<string> Photos(CarListing l)
        {
            var list = new List<string>();
            if (l?.PhotoFiles == null || !DecoderAvailable) return list;
            string root = ResourcesRoot;
            foreach (var f in l.PhotoFiles)
                if (f != null && f.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(root, f))) list.Add(f);
            // Ogłoszenie bez zdjęć z paczki (sprzed paczki, stara paczka 2026 albo usunięte pliki): dopisujemy je teraz. Losowanie z ziarnem z ID,
            // więc wynik jest stały także bez zapisu; raz na ogłoszenie na sesję (_tried), żeby nie skanować przy każdym odświeżeniu.
            if (list.Count == 0 && l.InternalId != null && _tried.Add(l.InternalId))
            {
                if (TryAssign(l, GameBridge.ToGameId(l.InternalId), new System.Random(OXLUi.StableHash(l.InternalId)))) return Photos(l);
            }
            return list;
        }

        private static readonly HashSet<string> _tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Kolejność stanów, gdy brakuje zdjęć w potrzebnym: najpierw sąsiedni przedział.
        private static string[] NearestConditions(string c) => c == "030" ? new[] { "030", "3060", "60100" } : c == "3060" ? new[] { "3060", "030", "60100" } : new[] { "60100", "3060", "030" };

        // Wszystkie inne wersje auta, które mają zdjęcia (paczka ma zwykle jedną wersję na auto, a OXL sprzedaje kilka).
        private static List<ColorDir> OtherVersions(string carId, int cfg)
        {
            var list = new List<ColorDir>();
            string dir = Path.Combine(ResourcesRoot, "CarPhotos", carId);
            if (!Directory.Exists(dir)) return list;
            foreach (var vd in Directory.GetDirectories(dir))
                if (int.TryParse(Path.GetFileName(vd), out int v) && v != cfg) list.AddRange(Colors(carId, v));
            return list;
        }

        private static List<ColorDir> Colors(string carId, int cfg)
        {
            string key = carId + "#" + cfg;
            if (_index.TryGetValue(key, out var cached)) return cached;
            var list = new List<ColorDir>();
            string dir = Path.Combine(ResourcesRoot, "CarPhotos", carId, cfg.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (Directory.Exists(dir))
            {
                foreach (var cd in Directory.GetDirectories(dir))
                {
                    // Nazwa folderu z fotografa (photo_world.cs): "NN_RRGGBB", NN = indeks w AllowedColors wersji.
                    string name = Path.GetFileName(cd);
                    int us = name.IndexOf('_');
                    if (us <= 0 || name.Length - us - 1 != 6 || !int.TryParse(name.Substring(0, us), out int idx)) continue;
                    var c = new ColorDir { Index = idx, Hex = name.Substring(us + 1).ToUpperInvariant() };
                    foreach (var sd in Directory.GetDirectories(cd))
                    {
                        string cond = Path.GetFileName(sd);
                        var groups = Directory.GetFiles(sd).Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                            .GroupBy(f => SpotOf(Path.GetFileNameWithoutExtension(f)), StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Select(f => Prefix + Path.GetRelativePath(Path.Combine(ResourcesRoot, "CarPhotos"), f).Replace('\\', '/')).ToList())
                            .ToList();
                        if (groups.Count > 0) c.Spots[cond] = groups;
                    }
                    if (c.Spots.Count > 0) list.Add(c);
                }
            }
            _index[key] = list;
            if (list.Count > 0) OXLLog.Msg($"[OXL:PHOTO] pack {key}: {list.Count} colors, {list.Sum(c => c.Spots.Values.Sum(s => s.Sum(x => x.Count)))} photos");
            return list;
        }

        // "spot012_03" -> "spot012"; plik bez "_" to osobne miejsce.
        private static string SpotOf(string name) { int us = name.LastIndexOf('_'); return us > 0 ? name.Substring(0, us) : name; }

        // ── Tekstury: dekodowanie w tle, upload na głównym wątku, mały cache ─────────────────────────

        private sealed class Decoded { public string Key = ""; public int W, H; public byte[] Rgba; }
        private sealed class Pending { public object Ve; }

        // Pełne zdjęcia ok. 3,7 MB (1280x720 RGBA), miniatury 320 px ok. 0,2 MB: limity trzymają pamięć w kilkudziesięciu MB.
        private const int MaxFull = 12, MaxThumb = 96, ThumbWidth = 320, UploadsPerFrame = 2;
        private static readonly Dictionary<string, Texture2D> _cache = new Dictionary<string, Texture2D>();
        private static readonly LinkedList<string> _lru = new LinkedList<string>();
        private static readonly Dictionary<string, List<Pending>> _waiting = new Dictionary<string, List<Pending>>();
        private static readonly ConcurrentQueue<Decoded> _done = new ConcurrentQueue<Decoded>();

        /// <summary>
        /// Zdjęcie z paczki jako tło elementu w prostokącie (x, y, w, h), wpasowane z zachowaniem proporcji 16:9.
        /// Gdy tekstury jeszcze nie ma, element zostaje pusty (tło PhotoBg rodzica) i dostaje obraz po zdekodowaniu, bez przebudowy strony.
        /// </summary>
        public static void Draw(object parent, string rel, bool thumb, float x, float y, float w, float h)
        {
            if (parent == null || string.IsNullOrEmpty(rel)) return;
            float dw = Math.Min(w, h * 16f / 9f), dh = dw * 9f / 16f;
            var ve = OXLUi.Box(parent, x + (w - dw) / 2f, y + (h - dh) / 2f, dw, dh, new Color(0f, 0f, 0f, 0f));
            Request(rel, thumb, ve);
        }

        /// <summary>Dekoduje zdjęcie z wyprzedzeniem (sąsiednie w galerii), żeby strzałka pokazała je od razu.</summary>
        public static void Prefetch(string rel, bool thumb) { if (!string.IsNullOrEmpty(rel)) Request(rel, thumb, null); }

        private static void Request(string rel, bool thumb, object ve)
        {
            string key = (thumb ? "t:" : "f:") + rel;
            if (_cache.TryGetValue(key, out var tex) && tex != null) { Touch(key); if (ve != null) UIRuntime.SetBackgroundImage(ve, tex); return; }
            if (_waiting.TryGetValue(key, out var list)) { if (ve != null) list.Add(new Pending { Ve = ve }); return; }
            _waiting[key] = ve != null ? new List<Pending> { new Pending { Ve = ve } } : new List<Pending>();
            string path = Path.Combine(ResourcesRoot, rel);
            System.Threading.Tasks.Task.Run(() =>
            {
                var d = new Decoded { Key = key };
                try { DecodeRgba(path, thumb ? ThumbWidth : 0, d); }
                catch (Exception ex) { d.Rgba = null; OXLPlugin.Log.Warning($"[OXL:PHOTO] decode '{rel}' failed: {ex.GetType().Name}: {ex.Message}"); }
                _done.Enqueue(d);
            });
        }

        /// <summary>Wywoływane co klatkę (OXLPlugin.OnUpdate): zdekodowane zdjęcia stają się teksturami i trafiają do czekających elementów.</summary>
        public static void Tick()
        {
            for (int n = 0; n < UploadsPerFrame && _done.TryDequeue(out var d); n++)
            {
                _waiting.TryGetValue(d.Key, out var pend);
                _waiting.Remove(d.Key);
                if (d.Rgba == null) continue;
                try
                {
                    var tex = new Texture2D(d.W, d.H, TextureFormat.RGBA32, false);
                    var h = GCHandle.Alloc(d.Rgba, GCHandleType.Pinned);
                    try { tex.LoadRawTextureData(h.AddrOfPinnedObject(), d.Rgba.Length); }
                    finally { h.Free(); }
                    tex.Apply(false, true);   // bez kopii w RAM po wysłaniu na GPU
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    _cache[d.Key] = tex;
                    Touch(d.Key);
                    Evict(d.Key.StartsWith("t:") ? "t:" : "f:", d.Key.StartsWith("t:") ? MaxThumb : MaxFull);
                    if (pend != null) foreach (var p in pend) { try { UIRuntime.SetBackgroundImage(p.Ve, tex); } catch { } }
                }
                catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:PHOTO] texture '{d.Key}' failed: {ex.Message}"); }
            }
        }

        private static void Touch(string key) { _lru.Remove(key); _lru.AddLast(key); }

        private static void Evict(string kind, int max)
        {
            int count = _lru.Count(k => k.StartsWith(kind));
            for (var node = _lru.First; node != null && count > max;)
            {
                var next = node.Next;
                if (node.Value.StartsWith(kind))
                {
                    if (_cache.TryGetValue(node.Value, out var t) && t != null) UnityEngine.Object.Destroy(t);
                    _cache.Remove(node.Value);
                    _lru.Remove(node);
                    count--;
                }
                node = next;
            }
        }

        // Osobna metoda bez inliningu: typy ImageSharp ładują się dopiero tu, więc brak DLL nie psuje reszty moda (DecoderAvailable sprawdza to wcześniej).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DecodeRgba(string path, int targetWidth, Decoded d)
        {
            var opt = targetWidth > 0 ? new SixLabors.ImageSharp.Formats.DecoderOptions { TargetSize = new SixLabors.ImageSharp.Size(targetWidth, targetWidth) } : new SixLabors.ImageSharp.Formats.DecoderOptions();
            using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(opt, path);
            int w = img.Width, h = img.Height, stride = w * 4;
            var src = new byte[stride * h];
            img.CopyPixelDataTo(src);
            // ImageSharp ma wiersze od góry, Unity od dołu: odwrócenie, żeby zdjęcie nie było do góry nogami.
            var dst = new byte[src.Length];
            for (int r = 0; r < h; r++) Buffer.BlockCopy(src, r * stride, dst, (h - 1 - r) * stride, stride);
            d.W = w; d.H = h; d.Rgba = dst;
        }
    }
}
