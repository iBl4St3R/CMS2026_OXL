// OXLSettings.cs — kompletny plik po zmianach

using System;
using System.IO;
using UnityEngine;

namespace CMS2026_OXL
{
    public enum Difficulty { Easy, Normal, Hard }

    public static class OXLSettings
    {
        // ── Difficulty ────────────────────────────────────────────────────────
        public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

        public static bool UseMiles { get; private set; } = true;

        public static void SetUseMiles(bool value) { UseMiles = value; Save(); }

        public static string FormatMileage(int mileage) => UseMiles
            ? (mileage >= 1000 ? $"{mileage / 1000}k mi" : $"{mileage} mi")
            : (mileage >= 1000 ? $"{mileage * 1609 / 1000 / 1000}k km" : $"{mileage * 1609 / 1000} km");

        public static float PriceMultiplier => CurrentDifficulty switch
        {
            Difficulty.Easy => 0.85f,
            Difficulty.Hard => 1.20f,
            _ => 1.00f,
        };

        // ── ListingGenConfig snapshot — ładowany przy starcie ─────────────────
        // Przechowuje ostatnio zapisane ustawienia generatora.
        // OXLPanel inicjalizuje swój draft z tych wartości przy Build().
        public static ListingGenConfig SavedGenConfig { get; private set; } = new ListingGenConfig();

        // ── Ścieżka ───────────────────────────────────────────────────────────
        private static string ConfigPath =>
            Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "CMS2026_OXL.cfg");

        // ═════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═════════════════════════════════════════════════════════════════════

        public static void Set(Difficulty d) { CurrentDifficulty = d; Save(); }

        /// <summary>
        /// Zapisuje ListingGenConfig do SavedGenConfig i od razu do pliku.
        /// Wywoływane z OXLPanel.SaveListingGenSettings().
        /// </summary>
        public static void SaveGenConfig(ListingGenConfig config)
        {
            SavedGenConfig = config ?? new ListingGenConfig();
            Save();
        }

        public static void Load()
        {
            try
            {
                string path = ConfigPath;

                if (!File.Exists(path))
                {
                    CurrentDifficulty = Difficulty.Normal;
                    SavedGenConfig = new ListingGenConfig();
                    Save();
                    OXLPlugin.Log.Msg("[OXL] Config not found — created with defaults.");
                    return;
                }

                // Reset do defaults przed parsowaniem
                // (żeby brakujące klucze nie zostawiały śmieci)
                var cfg = new ListingGenConfig();

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim().ToLower();
                    string rawValue = line.Substring(eq + 1);

                    // Strip inline comments (np. "100,0,0  # Honest" → "100,0,0")
                    int commentIdx = rawValue.IndexOf('#');
                    string value = (commentIdx >= 0
                        ? rawValue.Substring(0, commentIdx)
                        : rawValue).Trim();


                    switch (key)
                    {
                        // ── Difficulty / units ───────────────────────────────
                        case "difficulty":
                            if (Enum.TryParse(value, ignoreCase: true, out Difficulty parsed))
                                CurrentDifficulty = parsed;
                            break;

                        case "use_miles":
                            UseMiles = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;

                        // ── ListingGen — skalary ─────────────────────────────
                        case "lg_max_listings":
                            if (int.TryParse(value, out int maxL))
                                cfg.MaxListings = Mathf.Clamp(maxL, 1, 50);
                            break;

                        case "lg_gen_chance_pct":
                            if (int.TryParse(value, out int chance))
                                cfg.GenChancePct = Mathf.Clamp(chance, 0, 100);
                            break;

                        case "lg_gen_min":
                            if (int.TryParse(value, out int gMin))
                                cfg.GenMin = Mathf.Clamp(gMin, 1, 16);
                            break;

                        case "lg_gen_max":
                            if (int.TryParse(value, out int gMax))
                                cfg.GenMax = Mathf.Clamp(gMax, 1, 16);
                            break;

                        case "lg_dur_min_h":
                            if (int.TryParse(value, out int dMinH))
                                cfg.DurMinSec = Mathf.Clamp(dMinH, 1, 167)
                                                * ListingGenConfig.SecondsPerGameHour;
                            break;

                        case "lg_dur_max_h":
                            if (int.TryParse(value, out int dMaxH))
                                cfg.DurMaxSec = Mathf.Clamp(dMaxH, 2, 168)
                                                * ListingGenConfig.SecondsPerGameHour;
                            break;

                        // ── ListingGen — wagi archetypów (4 wartości, CSV) ───
                        // Format: lg_arch_weights = 20,35,30,15
                        case "lg_arch_weights":
                            ParseIntArray(value, cfg.ArchWeights, 0, 100);
                            break;

                        // ── ListingGen — wagi poziomów (3 wartości, CSV) ─────
                        // Format: lg_lvl_weights_0 = 40,35,25  (Honest)
                        //         lg_lvl_weights_1 = 35,35,30  (Wrecker)
                        //         lg_lvl_weights_2 = 30,40,30  (Dealer)
                        //         lg_lvl_weights_3 = 45,30,25  (Scammer)
                        case "lg_lvl_weights_0":
                        case "lg_lvl_weights_1":
                        case "lg_lvl_weights_2":
                        case "lg_lvl_weights_3":
                            int archIdx = int.Parse(key[key.Length - 1].ToString());
                            ParseIntArray(value, cfg.LvlWeights[archIdx], 0, 100);
                            break;
                    }
                }

                // Post-load walidacja spójności
                cfg.GenMin = Mathf.Min(cfg.GenMin, cfg.GenMax);
                cfg.GenMax = Mathf.Max(cfg.GenMin, cfg.GenMax);
                if (cfg.DurMinSec >= cfg.DurMaxSec)
                    cfg.DurMinSec = cfg.DurMaxSec - ListingGenConfig.SecondsPerGameHour;

                SavedGenConfig = cfg;

                OXLPlugin.Log.Msg($"[OXL] Config loaded — difficulty={CurrentDifficulty}" +
                                  $" maxL={cfg.MaxListings} chance={cfg.GenChancePct}%");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Config load error: {ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PRIVATE
        // ═════════════════════════════════════════════════════════════════════

        private static void Save()
        {
            try
            {
                string path = ConfigPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var cfg = SavedGenConfig;

                // Konwertuj sekundy → godziny do pliku (czytelne dla człowieka)
                int durMinH = Mathf.RoundToInt(cfg.DurMinSec / ListingGenConfig.SecondsPerGameHour);
                int durMaxH = Mathf.RoundToInt(cfg.DurMaxSec / ListingGenConfig.SecondsPerGameHour);

                File.WriteAllText(path,
                    "# OXL — Online eX-Owner Lies — Configuration\n" +
                    "#\n" +
                    "# difficulty: Easy | Normal | Hard\n" +
                    "#   Easy   = listing prices 15% lower\n" +
                    "#   Normal = base prices from JSON (recommended)\n" +
                    "#   Hard   = listing prices 20% higher\n" +
                    $"difficulty = {CurrentDifficulty}\n" +
                    $"use_miles = {UseMiles}\n" +
                    "#\n" +
                    "# ── Listing Generation ────────────────────────────────────────────\n" +
                    $"lg_max_listings   = {cfg.MaxListings}\n" +
                    $"lg_gen_chance_pct = {cfg.GenChancePct}\n" +
                    $"lg_gen_min        = {cfg.GenMin}\n" +
                    $"lg_gen_max        = {cfg.GenMax}\n" +
                    $"lg_dur_min_h      = {durMinH}\n" +
                    $"lg_dur_max_h      = {durMaxH}\n" +
                    "#\n" +
                    "# Archetype weights — Honest, Wrecker, Dealer, Scammer (sum=100)\n" +
                    $"lg_arch_weights   = {cfg.ArchWeights[0]},{cfg.ArchWeights[1]},{cfg.ArchWeights[2]},{cfg.ArchWeights[3]}\n" +
                    "#\n" +
                    "# Level weights per archetype — L1, L2, L3 (sum=100 each row)\n" +
                    $"lg_lvl_weights_0  = {cfg.LvlWeights[0][0]},{cfg.LvlWeights[0][1]},{cfg.LvlWeights[0][2]}  # Honest\n" +
                    $"lg_lvl_weights_1  = {cfg.LvlWeights[1][0]},{cfg.LvlWeights[1][1]},{cfg.LvlWeights[1][2]}  # Wrecker\n" +
                    $"lg_lvl_weights_2  = {cfg.LvlWeights[2][0]},{cfg.LvlWeights[2][1]},{cfg.LvlWeights[2][2]}  # Dealer\n" +
                    $"lg_lvl_weights_3  = {cfg.LvlWeights[3][0]},{cfg.LvlWeights[3][1]},{cfg.LvlWeights[3][2]}  # Scammer\n"
                );

                OXLPlugin.Log.Msg($"[OXL] Config saved — difficulty={CurrentDifficulty}" +
                                  $" maxL={cfg.MaxListings}");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Config save error: {ex.Message}");
            }
        }

        /// <summary>
        /// Parsuje "20,35,30,15" → int[] z clamping i guard na długość.
        /// Modyfikuje tablicę in-place — nie nadpisuje jeśli format błędny.
        /// </summary>
        private static void ParseIntArray(string value, int[] target, int lo, int hi)
        {
            var parts = value.Split(',');
            if (parts.Length != target.Length) return;

            var tmp = new int[target.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i].Trim(), out int v)) return;
                tmp[i] = Mathf.Clamp(v, lo, hi);
            }

            // Tylko jeśli wszystkie parse'y się udały — kopiuj
            Array.Copy(tmp, target, target.Length);
        }
    }
}