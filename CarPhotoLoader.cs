using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CMS2026_OXL
{
    public class CarPhotoLoader
    {
        private const int MAX_CACHED = 40;
        private const int PHOTOS_PER_SESSION = 6;

        private readonly string _lowResRoot;
        private readonly string _medResRoot;

        // _colorIndex["Mayen M5"]["gray"] = "...CarImages_MEDRES/Mayen M5/05_969DA0"
        // Budowany automatycznie ze skanowania folderów + ActiveColors
        private readonly Dictionary<string, Dictionary<string, string>> _colorIndex = new();

        private readonly Dictionary<string, Texture2D> _cache = new();
        private readonly LinkedList<string> _lruOrder = new();
        private readonly Dictionary<string, Texture2D> _fallbacks = new();

        public CarPhotoLoader(string modsRoot,
            Dictionary<string, (string carId, string[] colors)> colorRegistry)
        {
            _lowResRoot = Path.Combine(modsRoot, "CarImages_LOWRES");
            _medResRoot = Path.Combine(modsRoot, "CarImages_MEDRES");
            BuildColorIndex(colorRegistry);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INDEX BUILD — skanuje foldery przy starcie
        // ══════════════════════════════════════════════════════════════════════

        private void BuildColorIndex(
    Dictionary<string, (string carId, string[] colors)> colorRegistry)
        {
            var roots = new[] { _medResRoot, _lowResRoot };

            foreach (var kvp in colorRegistry)
            {
                string imageFolderName = kvp.Key;
                string[] colorNames = kvp.Value.colors;

                if (_colorIndex.ContainsKey(imageFolderName)) continue;

                // Znajdź folder auta (MEDRES lub LOWRES)
                string carPath = null;
                foreach (var root in roots)
                {
                    string c = Path.Combine(root, imageFolderName);
                    if (Directory.Exists(c)) { carPath = c; break; }
                }
                if (carPath == null) continue;

                var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // ── Próbuj color_map.txt ──────────────────────────────────────────
                string mapFile = Path.Combine(carPath, "color_map.txt");
                if (File.Exists(mapFile))
                {
                    var lines = File.ReadAllLines(mapFile);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string folderName = lines[i].Trim();
                        if (string.IsNullOrEmpty(folderName)) continue;
                        if (i >= colorNames.Length)
                        {
                            OXLPlugin.Log.Msg(
                                $"[PhotoLoader] color_map.txt '{imageFolderName}'" +
                                $" linia {i} ('{folderName}') — brak nazwy koloru (AllColors.Length={colorNames.Length}), skip");
                            continue;
                        }
                        colorMap[colorNames[i]] = folderName;
                        OXLPlugin.Log.Msg(
                            $"[PhotoLoader] map.txt '{imageFolderName}'" +
                            $" idx={i} → color='{colorNames[i]}' → folder='{folderName}'");
                    }
                    OXLPlugin.Log.Msg(
                        $"[PhotoLoader] BuildIndex '{imageFolderName}': {colorMap.Count} kolorów z color_map.txt");
                }
                else
                {
                    // ── Fallback: parsuj NN_ z nazw folderów ─────────────────────
                    OXLPlugin.Log.Msg(
                        $"[PhotoLoader] Brak color_map.txt dla '{imageFolderName}' — używam parsowania NN_");
                    var colorDirs = Directory.GetDirectories(carPath).OrderBy(d => d).ToArray();
                    foreach (var dir in colorDirs)
                    {
                        string folderName = Path.GetFileName(dir);
                        int underscoreIdx = folderName.IndexOf('_');
                        if (underscoreIdx < 1) continue;
                        if (!int.TryParse(folderName.Substring(0, underscoreIdx), out int colorIdx)) continue;
                        if (colorIdx < 0 || colorIdx >= colorNames.Length) continue;
                        colorMap[colorNames[colorIdx]] = folderName;
                        OXLPlugin.Log.Msg(
                            $"[PhotoLoader] fallback '{imageFolderName}'" +
                            $" idx={colorIdx} → color='{colorNames[colorIdx]}' → folder='{folderName}'");
                    }
                }

                _colorIndex[imageFolderName] = colorMap;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════

        public List<Texture2D> GetPhotos(CarListing listing, bool preferMed = true)
        {
            string carName = listing.ImageFolder;
            string condFolder = PhotoConditionHelper.ToFolderName(
                                    PhotoConditionHelper.Resolve(listing));

            string colorFolderName = ResolveColorFolderName(carName, listing.Color);
            OXLPlugin.Log.Msg(
                $"[PhotoLoader] GetPhotos: car={carName} color={listing.Color}" +
                $" cond={condFolder} → folderName={colorFolderName ?? "NULL"}");

            string colorPath = FindColorPath(carName, colorFolderName, preferMed);
            if (colorPath == null)
                return Fallback(carName);

            string condPath = Path.Combine(colorPath, condFolder);
            if (!Directory.Exists(condPath))
                condPath = FallbackCondPath(colorPath);

            if (condPath == null)
                return Fallback(carName);

            string sessionPath = PickSession(condPath);
            if (sessionPath == null)
                return Fallback(carName);

            var files = Directory.GetFiles(sessionPath, "*.jpg")
                .Where(f => !f.EndsWith("MINI.jpg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f)
                .Take(PHOTOS_PER_SESSION)
                .ToList();

            if (files.Count == 0)
                return Fallback(carName);

            var result = new List<Texture2D>();
            foreach (var file in files)
            {
                var tex = GetOrLoad(MakeCacheKey(file), file);
                if (tex != null) result.Add(tex);
            }

            OXLPlugin.Log.Msg(
                $"[PhotoLoader] GetPhotos result: {result.Count} textures from {sessionPath}");

            return result.Count > 0 ? result : Fallback(carName);
        }

        public Texture2D GetThumbnail(CarListing listing)
        {
            string carName = listing.ImageFolder;
            string condFolder = PhotoConditionHelper.ToFolderName(
                                    PhotoConditionHelper.Resolve(listing));

            string colorFolderName = ResolveColorFolderName(carName, listing.Color);
            string colorPath = FindColorPath(carName, colorFolderName, preferMed: true);

            if (colorPath != null && colorPath.StartsWith(_medResRoot))
            {
                string condPath = Path.Combine(colorPath, condFolder);
                if (!Directory.Exists(condPath))
                    condPath = FallbackCondPath(colorPath) ?? condPath;

                string session = PickSession(condPath);
                if (session != null)
                {
                    string miniFile = Path.Combine(session, "001MINI.jpg");
                    if (File.Exists(miniFile))
                        return GetOrLoad("MINI_" + MakeCacheKey(miniFile), miniFile);
                }
            }

            // Fallback: pierwsze zdjęcie LOWRES
            var photos = GetPhotos(listing, preferMed: false);
            return photos.FirstOrDefault() ?? GetFallback(carName);
        }

        public void Evict()
        {
            while (_cache.Count > MAX_CACHED && _lruOrder.Count > 0)
            {
                string oldest = _lruOrder.Last.Value;
                _lruOrder.RemoveLast();
                if (_cache.TryGetValue(oldest, out var tex))
                {
                    if (tex != null) UnityEngine.Object.Destroy(tex);
                    _cache.Remove(oldest);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Zwraca nazwę folderu koloru (np. "05_969DA0") dla danego auta i nazwy koloru.
        /// Null jeśli nie znaleziono w indeksie.
        /// </summary>
        private string ResolveColorFolderName(string carName, string colorName)
        {
            if (_colorIndex.TryGetValue(carName, out var map)
                && map.TryGetValue(colorName, out var folderName))
                return folderName;

            OXLPlugin.Log.Msg(
                $"[PhotoLoader] ResolveColorFolderName: no entry for car='{carName}'" +
                $" color='{colorName}' — using fallback folder");
            return null;
        }

        /// <summary>
        /// Zwraca pełną ścieżkę do folderu koloru, szukając najpierw w MEDRES potem LOWRES.
        /// Jeśli folderName==null — bierze pierwszy dostępny folder (fallback).
        /// </summary>
        private string FindColorPath(string carName, string folderName, bool preferMed)
        {
            var roots = preferMed
                ? new[] { _medResRoot, _lowResRoot }
                : new[] { _lowResRoot };

            foreach (var root in roots)
            {
                string carPath = Path.Combine(root, carName);
                if (!Directory.Exists(carPath)) continue;

                if (folderName != null)
                {
                    string exact = Path.Combine(carPath, folderName);
                    if (Directory.Exists(exact)) return exact;
                }

                // Fallback — pierwszy dostępny folder koloru
                var any = Directory.GetDirectories(carPath).FirstOrDefault();
                if (any != null) return any;
            }

            return null;
        }

        private string FallbackCondPath(string colorPath)
        {
            return Directory.GetDirectories(colorPath).FirstOrDefault();
        }

        private static string PickSession(string condPath)
        {
            if (!Directory.Exists(condPath)) return null;
            var sessions = Directory.GetDirectories(condPath);
            if (sessions.Length == 0) return null;
            return sessions[UnityEngine.Random.Range(0, sessions.Length)];
        }

        private List<Texture2D> Fallback(string carName)
        {
            var tex = GetFallback(carName);
            return tex != null ? new List<Texture2D> { tex } : new List<Texture2D>();
        }

        private Texture2D GetFallback(string carName)
        {
            if (_fallbacks.TryGetValue(carName, out var cached)) return cached;

            foreach (var root in new[] { _medResRoot, _lowResRoot })
            {
                string path = Path.Combine(root, carName, "fallback.png");
                if (!File.Exists(path)) continue;
                var tex = LoadTexture(path);
                if (tex != null)
                {
                    tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    _fallbacks[carName] = tex;
                    return tex;
                }
            }
            return null;
        }

        private Texture2D GetOrLoad(string key, string filePath)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                _lruOrder.Remove(key);
                _lruOrder.AddFirst(key);
                return cached;
            }

            var tex = LoadTexture(filePath);
            if (tex == null) return null;

            _cache[key] = tex;
            _lruOrder.AddFirst(key);
            Evict();
            return tex;
        }

        private static string MakeCacheKey(string fullPath) =>
            fullPath.Replace('\\', '/').ToLower();

        private static Texture2D LoadTexture(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                byte[] bytes = File.ReadAllBytes(path);

                // PNG vs JPG — TextureFormat
                bool isPng = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
                var fmt = isPng ? TextureFormat.RGBA32 : TextureFormat.RGB24;
                var tex = new Texture2D(2, 2, fmt, false);

                var il2b = new Il2CppInterop.Runtime.InteropTypes
                               .Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                var icType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName == "UnityEngine.ImageConversion");

                var loadImg = icType?.GetMethods()
                    .FirstOrDefault(m => m.Name == "LoadImage"
                                      && m.GetParameters().Length == 2);

                if (loadImg == null)
                {
                    OXLPlugin.Log.Msg("[PhotoLoader] LoadTexture: ImageConversion not found");
                    return null;
                }

                bool ok = (bool)loadImg.Invoke(null, new object[] { tex, il2b });
                if (ok) tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                return ok ? tex : null;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg(
                    $"[PhotoLoader] LoadTexture error ({Path.GetFileName(path)}): {ex.Message}");
                return null;
            }
        }
    }
}