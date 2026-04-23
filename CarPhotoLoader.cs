using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CMS2026_OXL
{
    public class CarPhotoLoader
    {
        private const int MAX_CACHED = 80;
        private const int MAX_THUMB_CACHED = 30;  // miniatury na liście aukcji
        private readonly Dictionary<string, Texture2D> _thumbCache = new();
        private readonly LinkedList<string> _thumbLru = new();


        // ── Indeks: imageFolderName → lista dostępnych config indeksów ────────────
        private readonly Dictionary<string, int[]> _availableConfigs = new();


        private readonly string _lowResRoot;
        private readonly string _medResRoot;

        // Indeks: imageFolderName → colorName → configIdx → folderName
        // np. "DNB Censor" → "black" → 0 → "00_000000"
        private readonly Dictionary<string,                        // car folder
                           Dictionary<string,                      // color name
                           Dictionary<int, string>>> _colorIndex   // config → folder
            = new();


        private readonly Dictionary<string, Texture2D> _cache = new();
        private readonly LinkedList<string> _lruOrder = new();
        private readonly Dictionary<string, Texture2D> _fallbacks = new();

        public CarPhotoLoader(string modsRoot,Dictionary<string, (string carId, string[] colors)> colorRegistry)
        {
            _lowResRoot = Path.Combine(modsRoot, "CarImages_LOWRES");
            _medResRoot = Path.Combine(modsRoot, "CarImages_MEDRES");
            BuildColorIndex(colorRegistry);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INDEX BUILD — skanuje foldery przy starcie
        // ══════════════════════════════════════════════════════════════════════

        private void BuildColorIndex(Dictionary<string, (string carId, string[] colors)> colorRegistry)
        {
            var roots = new[] { _medResRoot, _lowResRoot };

            foreach (var kvp in colorRegistry)
            {
                string imageFolderName = kvp.Key;
                string[] colorNames = kvp.Value.colors;

                if (_colorIndex.ContainsKey(imageFolderName)) continue;

                var carColorMap = new Dictionary<string, Dictionary<int, string>>(
                    StringComparer.OrdinalIgnoreCase);

                var detectedConfigs = new HashSet<int>();  // ← NOWE

                foreach (var root in roots)
                {
                    string carPath = Path.Combine(root, imageFolderName);
                    if (!Directory.Exists(carPath)) continue;

                    bool hasConfigSubdirs = HasConfigSubdirs(carPath);

                    if (hasConfigSubdirs)
                    {
                        foreach (var configDir in Directory.GetDirectories(carPath).OrderBy(d => d))
                        {
                            string configName = Path.GetFileName(configDir);
                            if (!int.TryParse(configName, out int configIdx)) continue;

                            detectedConfigs.Add(configIdx);  // ← NOWE
                            IndexColorDirs(configDir, colorNames, configIdx, carColorMap, imageFolderName);
                        }
                    }
                    else
                    {
                        detectedConfigs.Add(0);  // ← NOWE: płaska struktura = tylko config 0
                        IndexColorDirs(carPath, colorNames, 0, carColorMap, imageFolderName);
                    }
                }

                _colorIndex[imageFolderName] = carColorMap;

                // zapisz wykryte konfigi
                _availableConfigs[imageFolderName] = detectedConfigs.OrderBy(x => x).ToArray();

                OXLPlugin.Log.Msg(
                    $"[PhotoLoader] BuildIndex '{imageFolderName}': " +
                    $"{carColorMap.Count} colors, configs=[{string.Join(",", _availableConfigs[imageFolderName])}]");
            }
        }

        /// <summary>
        /// Zwraca tablicę dostępnych config indeksów dla danego imageFolderName.
        /// Jeśli folder nieznany, zwraca [0] jako bezpieczny fallback.
        /// </summary>
        public int[] GetAvailableConfigs(string imageFolderName)
        {
            if (_availableConfigs.TryGetValue(imageFolderName, out var configs) && configs.Length > 0)
                return configs;
            return new[] { 0 };
        }

        /// <summary>
        /// Returns true if carPath contains numeric subdirectories (config indices)
        /// rather than colour folders directly.
        /// </summary>
        private static bool HasConfigSubdirs(string carPath)
        {
            foreach (var dir in Directory.GetDirectories(carPath))
            {
                string name = Path.GetFileName(dir);
                if (int.TryParse(name, out _)) return true;
            }
            return false;
        }

        /// <summary>
        /// Reads colour folders (NN_XXXXXX) from <paramref name="basePath"/>
        /// and registers them in <paramref name="carColorMap"/> under
        /// <paramref name="configIdx"/>.
        /// Reads color_map.txt if present; falls back to NN_ prefix parsing.
        /// </summary>
        private void IndexColorDirs(
            string basePath,
            string[] colorNames,
            int configIdx,
            Dictionary<string, Dictionary<int, string>> carColorMap,
            string imageFolderName)
        {
            string mapFile = Path.Combine(basePath, "color_map.txt");

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
                            $"[PhotoLoader] color_map.txt '{imageFolderName}' cfg={configIdx} " +
                            $"line {i} ('{folderName}') — no color name at index {i}, skip");
                        continue;
                    }

                    string colorName = colorNames[i];
                    if (!carColorMap.TryGetValue(colorName, out var cfgMap))
                    {
                        cfgMap = new Dictionary<int, string>();
                        carColorMap[colorName] = cfgMap;
                    }
                    cfgMap[configIdx] = folderName;

                    OXLPlugin.Log.Msg(
                        $"[PhotoLoader] map.txt '{imageFolderName}' cfg={configIdx}" +
                        $" idx={i} → color='{colorName}' → folder='{folderName}'");
                }
            }
            else
            {
                // Fallback: parse NN_ prefix
                var colorDirs = Directory.GetDirectories(basePath).OrderBy(d => d).ToArray();
                foreach (var dir in colorDirs)
                {
                    string folderName = Path.GetFileName(dir);
                    int underscoreIdx = folderName.IndexOf('_');
                    if (underscoreIdx < 1) continue;
                    if (!int.TryParse(folderName.Substring(0, underscoreIdx), out int colorIdx)) continue;
                    if (colorIdx < 0 || colorIdx >= colorNames.Length) continue;

                    string colorName = colorNames[colorIdx];
                    if (!carColorMap.TryGetValue(colorName, out var cfgMap))
                    {
                        cfgMap = new Dictionary<int, string>();
                        carColorMap[colorName] = cfgMap;
                    }
                    cfgMap[configIdx] = folderName;

                    OXLPlugin.Log.Msg(
                        $"[PhotoLoader] fallback '{imageFolderName}' cfg={configIdx}" +
                        $" idx={colorIdx} → color='{colorName}' → folder='{folderName}'");
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════

        public List<Texture2D> GetPhotosFromFiles(List<string> files)
        {
            var result = new List<Texture2D>();
            foreach (var file in files)
            {
                var tex = GetOrLoad(MakeCacheKey(file), file);
                if (tex != null) result.Add(tex);
            }
            return result;
        }


        public List<Texture2D> GetPhotos(CarListing listing, bool preferMed = true)
        {
            // Jeśli listing ma zapisane pliki — użyj ich bezpośrednio
            if (listing.PhotoFiles != null && listing.PhotoFiles.Count > 0)
                return GetPhotosFromFiles(listing.PhotoFiles);

            // Fallback: szukaj na bieżąco (np. dla starych zapisów)
            string sessionPath = ResolveSessionPath(listing, preferMed);
            if (sessionPath == null) return Fallback(listing.ImageFolder);

            var allFiles = GetJpgFiles(sessionPath);
            var files = PickPhotoSelection(allFiles);

            if (files.Count == 0) return Fallback(listing.ImageFolder);

            var result = new List<Texture2D>();
            foreach (var file in files)
            {
                var tex = GetOrLoad(MakeCacheKey(file), file);
                if (tex != null) result.Add(tex);
            }
            return result.Count > 0 ? result : Fallback(listing.ImageFolder);
        }

        

        

        /// <summary>
        /// Wybiera zestaw plików zdjęć dla listingu — raz przy generowaniu oferty.
        /// Nie ładuje tekstur, tylko zwraca ścieżki.
        /// </summary>
        /// <summary>
        /// Selects photo file paths once at listing-generation time.
        /// Files are stored in CarListing.PhotoFiles and reused for all subsequent loads.
        /// This ensures the same session/photos are shown every time the listing is opened.
        /// </summary>
        public List<string> SelectPhotoFiles(CarListing listing)
        {
            string sessionPath = ResolveSessionPath(listing, preferMed: true);

            // If MEDRES session not found, try LOWRES
            if (sessionPath == null)
                sessionPath = ResolveSessionPath(listing, preferMed: false);

            if (sessionPath == null) return new List<string>();

            var allFiles = GetJpgFiles(sessionPath);
            return PickPhotoSelection(allFiles);
        }



        


        public Texture2D GetThumbnail(CarListing listing)
        {
            // Preferuj MINI z pierwszego pliku w PhotoFiles
            if (listing.PhotoFiles != null && listing.PhotoFiles.Count > 0)
            {
                string miniPath = ToMiniPath(listing.PhotoFiles[0]);
                if (miniPath != null && File.Exists(miniPath))
                {
                    string key = "MINI_" + MakeCacheKey(miniPath);
                    if (_thumbCache.TryGetValue(key, out var cached))
                    {
                        TouchLru(_thumbLru, key);
                        return cached;
                    }
                    var tex = LoadTexture(miniPath);
                    if (tex != null)
                    {
                        _thumbCache[key] = tex;
                        _thumbLru.AddFirst(key);
                        EvictThumbs();
                        return tex;
                    }
                }
            }

            // Fallback: live-resolve MINI z MEDRES
            string session = ResolveSessionPath(listing, preferMed: true);
            if (session != null)
            {
                string miniFile = Path.Combine(session, "001MINI.jpg");
                if (File.Exists(miniFile))
                {
                    string key = "MINI_" + MakeCacheKey(miniFile);
                    if (_thumbCache.TryGetValue(key, out var c2))
                    {
                        TouchLru(_thumbLru, key);
                        return c2;
                    }
                    var tex = LoadTexture(miniFile);
                    if (tex != null)
                    {
                        _thumbCache[key] = tex;
                        _thumbLru.AddFirst(key);
                        EvictThumbs();
                        return tex;
                    }
                }
            }

            var photos = GetPhotos(listing, preferMed: false);
            return photos.FirstOrDefault() ?? GetFallback(listing.ImageFolder);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  RESOLUTION HELPERS
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Resolves the full path to a randomly-chosen session folder for the listing.
        /// Takes config, color, and condition into account.
        /// Returns null if nothing found.
        /// </summary>
        private string ResolveSessionPath(CarListing listing, bool preferMed)
        {
            string carName = listing.ImageFolder;
            string condFolder = PhotoConditionHelper.ToFolderName(
                                   PhotoConditionHelper.Resolve(listing));
            int config = listing.CarConfig;

            string colorFolderName = ResolveColorFolderName(carName, listing.Color, config);

            OXLPlugin.Log.Msg(
                $"[PhotoLoader] ResolveSession: car={carName} color={listing.Color}" +
                $" config={config} cond={condFolder} → colorFolder={colorFolderName ?? "NULL"}");

            var roots = preferMed
                ? new[] { _medResRoot, _lowResRoot }
                : new[] { _lowResRoot };

            foreach (var root in roots)
            {
                string carPath = Path.Combine(root, carName);
                if (!Directory.Exists(carPath)) continue;

                bool hasConfigs = HasConfigSubdirs(carPath);

                string colorBase;
                if (hasConfigs)
                {
                    // Try exact config first, then fall back to any config
                    string configDir = Path.Combine(carPath, config.ToString());
                    if (!Directory.Exists(configDir))
                    {
                        // Fall back to first available config dir
                        configDir = Directory.GetDirectories(carPath)
                            .Where(d => int.TryParse(Path.GetFileName(d), out _))
                            .OrderBy(d => d)
                            .FirstOrDefault();
                        if (configDir == null) continue;

                        OXLPlugin.Log.Msg(
                            $"[PhotoLoader] Config {config} not found for '{carName}', " +
                            $"falling back to '{Path.GetFileName(configDir)}'");
                    }
                    colorBase = colorFolderName != null
                        ? Path.Combine(configDir, colorFolderName)
                        : null;

                    if (colorBase == null || !Directory.Exists(colorBase))
                        colorBase = Directory.GetDirectories(configDir).FirstOrDefault();
                }
                else
                {
                    // Flat (old) structure
                    colorBase = colorFolderName != null
                        ? Path.Combine(carPath, colorFolderName)
                        : null;

                    if (colorBase == null || !Directory.Exists(colorBase))
                        colorBase = Directory.GetDirectories(carPath).FirstOrDefault();
                }

                if (colorBase == null) continue;

                string condPath = Path.Combine(colorBase, condFolder);
                if (!Directory.Exists(condPath))
                    condPath = FallbackCondPath(colorBase);

                if (condPath == null) continue;

                string session = PickSession(condPath);
                if (session != null) return session;
            }

            return null;
        }

        /// <summary>
        /// Returns the color folder name (e.g. "05_969DA0") for a given car, color, and config.
        /// Falls back to any available folder if the exact config is not indexed.
        /// </summary>
        private string ResolveColorFolderName(string carName, string colorName, int config)
        {
            if (!_colorIndex.TryGetValue(carName, out var colorMap)) return null;
            if (!colorMap.TryGetValue(colorName, out var cfgMap)) return null;

            // Exact config
            if (cfgMap.TryGetValue(config, out var folder)) return folder;

            // Fall back to config 0
            if (cfgMap.TryGetValue(0, out folder))
            {
                OXLPlugin.Log.Msg(
                    $"[PhotoLoader] ResolveColorFolderName: config={config} not found for " +
                    $"car='{carName}' color='{colorName}', using config=0");
                return folder;
            }

            // Fall back to any config
            folder = cfgMap.Values.FirstOrDefault();
            OXLPlugin.Log.Msg(
                $"[PhotoLoader] ResolveColorFolderName: falling back to first available " +
                $"config for car='{carName}' color='{colorName}'");
            return folder;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private static string[] GetJpgFiles(string sessionPath) =>
        Directory.Exists(sessionPath)
            ? Directory.GetFiles(sessionPath, "*.jpg")
                       .Where(f => !f.EndsWith("MINI.jpg",
                                   StringComparison.OrdinalIgnoreCase))
                       .OrderBy(f => f)
                       .ToArray()
            : Array.Empty<string>();

        private static string ToMiniPath(string fullPath)
        {
            // "001.jpg" → "001MINI.jpg" in same directory
            string dir = Path.GetDirectoryName(fullPath);
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string mini = Path.Combine(dir, name + "MINI.jpg");
            return File.Exists(mini) ? mini : null;
        }

        private static List<string> PickPhotoSelection(string[] allFiles)
        {
            if (allFiles.Length == 0) return new List<string>();

            int maxAvailable = allFiles.Length;
            int targetCount = UnityEngine.Random.Range(5, Mathf.Min(9, maxAvailable + 1));
            targetCount = Mathf.Max(targetCount, Mathf.Min(2, maxAvailable));

            var result = new List<string>();

            bool takeBothBase = maxAvailable >= 2 && UnityEngine.Random.value < 0.55f;
            result.Add(allFiles[0]);
            if (takeBothBase && maxAvailable >= 2) result.Add(allFiles[1]);

            var remaining = allFiles.Skip(takeBothBase ? 2 : 1).ToList();
            for (int i = remaining.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (remaining[i], remaining[j]) = (remaining[j], remaining[i]);
            }
            result.AddRange(remaining.Take(targetCount - result.Count));

            // Base shots first
            var baseShots = result.Where(f => Array.IndexOf(allFiles, f) < 2).ToList();
            var otherShots = result.Where(f => Array.IndexOf(allFiles, f) >= 2).ToList();
            return baseShots.Concat(otherShots).ToList();
        }


        private static string PickSession(string condPath)
        {
            if (!Directory.Exists(condPath)) return null;
            var sessions = Directory.GetDirectories(condPath);
            if (sessions.Length == 0) return null;
            return sessions[UnityEngine.Random.Range(0, sessions.Length)];
        }

        private string FallbackCondPath(string colorPath) => Directory.GetDirectories(colorPath).FirstOrDefault();

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
                // fallback.png może być w carPath lub w carPath/0/ (config dir)
                string carPath = Path.Combine(root, carName);
                string[] tries = {
                Path.Combine(carPath, "fallback.png"),
                Path.Combine(carPath, "0",  "fallback.png"),
                Path.Combine(carPath, "1",  "fallback.png"),
            };
                foreach (var path in tries)
                {
                    if (!File.Exists(path)) continue;
                    var tex = LoadTexture(path);
                    if (tex != null)
                    {
                        tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
                        _fallbacks[carName] = tex;
                        return tex;
                    }
                }
            }
            return null;
        }

        private static void TouchLru(LinkedList<string> lru, string key)
        {
            lru.Remove(key);
            lru.AddFirst(key);
        }

        private Texture2D GetOrLoad(string key, string filePath)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                TouchLru(_lruOrder, key);
                return cached;
            }

            var tex = LoadTexture(filePath);
            if (tex == null) return null;

            _cache[key] = tex;
            _lruOrder.AddFirst(key);
            Evict();
            return tex;
        }

        private void EvictThumbs()
        {
            while (_thumbCache.Count > MAX_THUMB_CACHED && _thumbLru.Count > 0)
            {
                string oldest = _thumbLru.Last.Value;
                _thumbLru.RemoveLast();
                if (_thumbCache.TryGetValue(oldest, out var tex))
                {
                    if (tex != null) UnityEngine.Object.Destroy(tex);
                    _thumbCache.Remove(oldest);
                }
            }
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

        public struct CacheInfo
        {
            public int Count;
            public float EstimatedMB;
            public int LruCount;
            public int FallbackCount;
            public int ThumbCount;
            public float ThumbMB;
        }


        public CacheInfo GetCacheInfo()
        {
            float mb = 0f;
            foreach (var tex in _cache.Values)
            {
                if (tex == null) continue;
                int bpp = tex.format == TextureFormat.RGBA32 ? 4 : 3;
                mb += tex.width * tex.height * bpp / 1024f / 1024f;
            }
            float thumbMb = 0f;
            foreach (var tex in _thumbCache.Values)
            {
                if (tex == null) continue;
                int bpp = tex.format == TextureFormat.RGBA32 ? 4 : 3;
                thumbMb += tex.width * tex.height * bpp / 1024f / 1024f;
            }
            return new CacheInfo
            {
                Count = _cache.Count,
                EstimatedMB = mb,
                LruCount = _lruOrder.Count,
                FallbackCount = _fallbacks.Count,
                ThumbCount = _thumbCache.Count,
                ThumbMB = thumbMb,
            };
        }

        // ── LoadTexture (bez zmian względem oryginału) ────────────────────────
        private static string MakeCacheKey(string fullPath) => fullPath.Replace('\\', '/').ToLower();

        private static Texture2D LoadTexture(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                byte[] bytes = File.ReadAllBytes(path);
                bool isPng = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
                var fmt = isPng ? TextureFormat.RGBA32 : TextureFormat.RGB24;
                var tex = new Texture2D(2, 2, fmt, false);

                var il2b = new Il2CppInterop.Runtime.InteropTypes
                               .Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                var icType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
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