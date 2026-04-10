using System;
using System.IO;
using UnityEngine;

namespace CMS2026_OXL
{
    public enum Difficulty { Easy, Normal, Hard }

    public static class OXLSettings
    {
        public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;


        /// Easy   = listing prices 10% lower  (slight discount)
        /// Normal = prices 30% above baseline (intended balance)
        /// Hard   = listing prices 60% higher (tight margins)
        public static float PriceMultiplier => CurrentDifficulty switch
        {
            Difficulty.Easy => 0.90f,  // -10%
            Difficulty.Hard => 1.60f,  // +60%
            _ => 1.30f,  // Normal +30%
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
                    "#   Easy   = listing prices 30% lower  (more profit margin)\n" +
                    "#   Normal = baseline prices\n" +
                    "#   Hard   = listing prices 30% higher (tighter margins)\n" +
                    "# \n" +
                   $"difficulty = {CurrentDifficulty}\n");

                OXLPlugin.Log.Msg($"[OXL] Config saved — difficulty={CurrentDifficulty}");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Config save error: {ex.Message}");
            }
        }
    }
}