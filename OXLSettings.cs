using System;
using System.IO;
using UnityEngine;

namespace CMS2026_OXL
{
    public enum Difficulty { Easy, Normal, Hard }

    public static class OXLSettings
    {
        public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

        public static bool UseMiles { get; private set; } = true;

        public static void SetUseMiles(bool value)
        {
            UseMiles = value;
            Save();
        }

        public static string FormatMileage(int mileage) => UseMiles
        ? (mileage >= 1000 ? $"{mileage / 1000}k mi" : $"{mileage} mi")
        : (mileage >= 1000 ? $"{mileage * 1609 / 1000 / 1000}k km" : $"{mileage * 1609 / 1000} km");

        /// Easy   = ceny 15% niższe   (większa marża, lepszy start)
        /// Normal = ceny bazowe z JSON (zamierzony balans)
        /// Hard   = ceny 20% wyższe   (ciasne marże, kara za błędy)
        public static float PriceMultiplier => CurrentDifficulty switch
        {
            Difficulty.Easy => 0.85f,
            Difficulty.Hard => 1.20f,
            _ => 1.00f,   // Normal = baseline, bez korekty
        };

        // ── Ścieżka do pliku ──────────────────────────────────────────────────
        private static string ConfigPath =>
            Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "CMS2026_OXL.cfg");

        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════

        public static void Set(Difficulty d)
        {
            CurrentDifficulty = d;
            Save();
        }

        /// <summary>
        /// Wczytuje config z dysku. Jeśli plik nie istnieje — tworzy go z domyślnymi.
        /// Wywoływać raz przy starcie, np. z OXLPlugin.OnSceneWasLoaded.
        /// </summary>
        public static void Load()
        {
            try
            {
                string path = ConfigPath;

                if (!File.Exists(path))
                {
                    CurrentDifficulty = Difficulty.Normal;
                    Save(); // stwórz plik z defaultem
                    OXLPlugin.Log.Msg("[OXL] Config not found — created with defaults.");
                    return;
                }

                foreach (var rawLine in File.ReadAllLines(path))
                {
                    string line = rawLine.Trim();

                    // Pomijamy komentarze i puste linie
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    // Format: key = value
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim().ToLower();
                    string value = line.Substring(eq + 1).Trim();

                    if (key == "difficulty")
                    {
                        if (Enum.TryParse(value, ignoreCase: true, out Difficulty parsed))
                            CurrentDifficulty = parsed;
                        else
                            OXLPlugin.Log.Msg($"[OXL] Config: unknown difficulty '{value}', using Normal.");
                    }

                    if (key == "use_miles")
                        UseMiles = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                OXLPlugin.Log.Msg($"[OXL] Config loaded — difficulty={CurrentDifficulty}");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Config load error: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PRIVATE
        // ══════════════════════════════════════════════════════════════════════

        private static void Save()
        {
            try
            {
                string path = ConfigPath;

                // Upewnij się że folder istnieje
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                File.WriteAllText(path,
    "# OXL — Online eX-Owner Lies — Configuration\n" +
    "# \n" +
    "# difficulty: Easy | Normal | Hard\n" +
    "#   Easy   = listing prices 15% lower  (higher profit margins)\n" +
    "#   Normal = base prices from JSON      (intended game balance)\n" +
    "#   Hard   = listing prices 20% higher (tight margins, less forgiving)\n" +
    $"use_miles = {UseMiles}\n" +
    "# \n" +
    $"difficulty = {CurrentDifficulty}\n"
);


                OXLPlugin.Log.Msg($"[OXL] Config saved — difficulty={CurrentDifficulty}");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Config save error: {ex.Message}");
            }
        }
    }
}