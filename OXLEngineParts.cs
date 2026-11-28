using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CMS2026_OXL
{
    /// <summary>
    /// Które części silnika montuje się tylko przy aucie (PartScript.canMountUnmountOnlyOnCarLoader: skrzynia, rozrusznik, początek wydechu).
    /// Stojak takie części odrzuca, a gra przy wyjmowaniu silnika zostawia je w aucie, więc OXLDelivery ich nie wysyła (silnik jak wyjęty z auta).
    /// Flaga jest tylko na modelu części, nie w danych gry: OXL uczy się jej z modeli w scenie (silniki aut na parkingu, silniki na stojakach)
    /// i pamięta w Mods/CMS2026_OXL/engine_parts.txt. Dane z gry, bez listy w kodzie; nowe silniki z aktualizacji dochodzą same.
    /// </summary>
    public static class OXLEngineParts
    {
        private static readonly HashSet<string> _known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _carOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _loaded;

        private static string DataPath => Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "engine_parts.txt");

        public static bool IsCarOnly(string id) { Load(); return id != null && _carOnly.Contains(id); }

        /// <summary>Skan wszystkich części w scenie (garaż: auta i stojaki). Wołane przy wejściu do garażu i przed złożeniem silnika do paczki.</summary>
        public static void ScanScene()
        {
            Load();
            try
            {
                int before = _known.Count, beforeCar = _carOnly.Count;
                foreach (var p in UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Core.Car.PartScript>(true))
                {
                    string id = p?.id;
                    if (string.IsNullOrEmpty(id)) continue;
                    _known.Add(id);
                    if (p.canMountUnmountOnlyOnCarLoader) _carOnly.Add(id);
                }
                if (_known.Count != before || _carOnly.Count != beforeCar) { Save(); OXLLog.Msg($"[OXL:ENGINE] learned parts: known={_known.Count} carOnly={_carOnly.Count} ({string.Join(", ", _carOnly)})"); }
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ENGINE] scene scan failed: {ex.Message}"); }
        }

        // Format: linia "known\tid" albo "car\tid". Nieznane linie pomijane, uszkodzony plik nie wywraca OXL.
        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(DataPath)) return;
                foreach (var line in File.ReadAllLines(DataPath))
                {
                    var f = line.Split('\t');
                    if (f.Length < 2 || f[1].Length == 0) continue;
                    if (f[0] == "known") _known.Add(f[1]);
                    else if (f[0] == "car") { _known.Add(f[1]); _carOnly.Add(f[1]); }
                }
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ENGINE] load failed: {ex.Message}"); }
        }

        private static void Save()
        {
            try
            {
                var lines = new List<string> { "# OXL: engine parts seen on game models; car = mounted only on a car (not on the engine stand)" };
                lines.AddRange(_known.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Select(id => (_carOnly.Contains(id) ? "car\t" : "known\t") + id));
                File.WriteAllLines(DataPath, lines);
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ENGINE] save failed: {ex.Message}"); }
        }
    }
}
