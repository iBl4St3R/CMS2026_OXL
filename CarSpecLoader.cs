using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CMS2026_OXL
{
    public class CarSpecData
    {
        public string EngineType { get; set; } = "";
        public string EnginePower { get; set; } = "";
        public string EngineTorque { get; set; } = "";
        public string Weight { get; set; } = "";
        public string Drivetrain { get; set; } = "";
        public string Origin { get; set; } = "";
        public string RimFront { get; set; } = "";
        public string TireFront { get; set; } = "";
        public string RimRear { get; set; } = "";
        public string TireRear { get; set; } = "";
        public string Rarity { get; set; } = "";
    }

    public class CarPriceModel
    {
        public int BaseValue { get; set; }
        public int MaxParts { get; set; }
        public int MaxFrame { get; set; }
        public int MaxBonus { get; set; }
    }

    public class ArchetypePrice
    {
        public int Chassis { get; set; }
        public int Body { get; set; }
        public int Km { get; set; }
        public int Price { get; set; }
    }

    public class CarSpec
    {
        public string CarId { get; set; } = "";
        public string CarName { get; set; } = "";
        public string[] ColorNames { get; set; } = Array.Empty<string>(); 
        public CarSpecData AutoDetected { get; set; } = new();
        public CarPriceModel PriceModel { get; set; } = new();
        public Dictionary<string, ArchetypePrice> ArchetypePrices
            = new(StringComparer.OrdinalIgnoreCase);
    }


    /// <summary>
    /// Loads car_spec.json files from CarImages_MEDRES/{carFolder}/car_spec.json
    /// at startup and provides fast lookup by carId.
    /// </summary>
    public class CarSpecLoader
    {
        private readonly Dictionary<string, CarSpec> _specs = new(StringComparer.OrdinalIgnoreCase);

        // Klucz pomocniczy
        private static string SpecKey(string carId, int configIdx) => $"{carId}:{configIdx}";

        // carId aliases — internal listing IDs → game carId used in spec files
        private static readonly Dictionary<string, string> IdAliases =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "car_dnb_censor",        "car_dnbcensor"          },
            { "car_katagiri_tamago",   "car_katagiritamagobp"   },
            { "car_luxor_streamliner", "car_luxorstreamlinermk3"},
            { "car_mayen_m5",          "car_mayenm5"            },
            { "car_salem_aries",       "car_salemariesmk3"      },
        };

        public CarSpecLoader(string modsRoot)
        {
            string[] roots = {
        Path.Combine(modsRoot, "CarImages_MEDRES"),
        Path.Combine(modsRoot, "CarImages_LOWRES"),
    };

            // carId:config → już załadowany (MEDRES ma priorytet)
            var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (var carDir in Directory.GetDirectories(root))
                {
                    // Sprawdź czy są numeryczne subdir (config structure)
                    bool hasConfigDirs = false;
                    foreach (var d in Directory.GetDirectories(carDir))
                    {
                        if (int.TryParse(Path.GetFileName(d), out _))
                        { hasConfigDirs = true; break; }
                    }

                    if (hasConfigDirs)
                    {
                        // Nowa struktura: carDir/0/car_spec.json, carDir/1/car_spec.json ...
                        foreach (var configDir in Directory.GetDirectories(carDir))
                        {
                            string configName = Path.GetFileName(configDir);
                            if (!int.TryParse(configName, out int configIdx)) continue;

                            string specPath = Path.Combine(configDir, "car_spec.json");
                            if (!File.Exists(specPath)) continue;

                            TryLoadSpec(specPath, configIdx, loaded);
                        }
                    }
                    else
                    {
                        // Stara/płaska struktura — config 0
                        string specPath = Path.Combine(carDir, "car_spec.json");
                        if (File.Exists(specPath))
                            TryLoadSpec(specPath, 0, loaded);
                    }
                }
            }
        }

        private void TryLoadSpec(string specPath, int configIdx, HashSet<string> loaded)
        {
            try
            {
                var spec = ParseSpec(File.ReadAllText(specPath));
                if (spec == null || string.IsNullOrEmpty(spec.CarId)) return;

                string key = SpecKey(spec.CarId, configIdx);
                if (loaded.Contains(key)) return;

                _specs[key] = spec;
                loaded.Add(key);

                OXLPlugin.Log.Msg(
                    $"[CarSpecLoader] Loaded spec for '{spec.CarId}' cfg={configIdx}" +
                    $" from {Path.GetFileName(Path.GetDirectoryName(specPath))}: " +
                    $"carName='{spec.CarName}' priceModel=(base={spec.PriceModel.BaseValue}" +
                    $" parts={spec.PriceModel.MaxParts}) " +
                    $"archetypeKeys=[{string.Join(", ", spec.ArchetypePrices.Keys)}]");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[CarSpecLoader] Failed to load {specPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Zwraca spec dla danego internalId i carConfig.
        /// Najpierw szuka dokładnego config, potem config=0, potem cokolwiek.
        /// </summary>
        public CarSpec Get(string internalId, int carConfig = 0)
        {
            string baseId = Regex.Replace(internalId, @"_\d+$", "");
            string lookupId = IdAliases.TryGetValue(baseId, out var alias) ? alias : baseId;

            // 1. Exact config
            if (_specs.TryGetValue(SpecKey(lookupId, carConfig), out var spec))
                return spec;

            // 2. Fallback config 0
            if (carConfig != 0 && _specs.TryGetValue(SpecKey(lookupId, 0), out spec))
            {
                OXLPlugin.Log.Msg(
                    $"[CarSpecLoader] cfg={carConfig} not found for '{lookupId}', using cfg=0");
                return spec;
            }

            // 3. Any config
            foreach (var kvp in _specs)
            {
                if (kvp.Key.StartsWith(lookupId + ":", StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }

            return new CarSpec { CarId = lookupId, AutoDetected = new CarSpecData() };
        }

        // ── Minimal JSON parser — no external deps needed ─────────────────────
        // Handles the flat structure we generate. Not a general-purpose parser.
        private static CarSpec ParseSpec(string json)
        {
            var spec = new CarSpec();
            spec.AutoDetected = new CarSpecData();

            spec.CarId = ReadString(json, "carId") ?? "";
            spec.CarName = ReadString(json, "carName") ?? "";

            // ── colors array ─────────────────────────────────────────────────────────
            int colorsIdx = json.IndexOf("\"colors\"", StringComparison.Ordinal);
            if (colorsIdx >= 0)
            {
                int arrOpen = json.IndexOf('[', colorsIdx);
                int arrClose = json.IndexOf(']', arrOpen);
                if (arrOpen >= 0 && arrClose > arrOpen)
                {
                    string arrBlock = json.Substring(arrOpen, arrClose - arrOpen + 1);
                    var names = new List<string>();

                    // Znajdź każdy obiekt { ... } w tablicy
                    int pos = 0;
                    while (pos < arrBlock.Length)
                    {
                        int objOpen = arrBlock.IndexOf('{', pos);
                        if (objOpen < 0) break;
                        int objClose = arrBlock.IndexOf('}', objOpen);
                        if (objClose < 0) break;

                        string obj = arrBlock.Substring(objOpen, objClose - objOpen + 1);

                        // Czytaj "index" żeby znać pozycję
                        string indexStr = ReadString(obj, "index");  // to nie zadziała — index jest liczbą
                                                                     // Użyj ReadInt zamiast:
                        int colorIdx = ReadInt(obj, "index");
                        string name = ReadString(obj, "name") ?? "";

                        // Upewnij się że lista jest wystarczająco długa
                        while (names.Count <= colorIdx)
                            names.Add("");
                        names[colorIdx] = name;

                        pos = objClose + 1;
                    }

                    spec.ColorNames = names.ToArray();
                }
            }


            // Find autoDetected block
            int blockStart = json.IndexOf("\"autoDetected\"", StringComparison.Ordinal);
            if (blockStart < 0) return spec;

            int braceOpen = json.IndexOf('{', blockStart);
            int braceClose = json.IndexOf('}', braceOpen);
            if (braceOpen < 0 || braceClose < 0) return spec;

            string block = json.Substring(braceOpen, braceClose - braceOpen + 1);

            spec.AutoDetected.EngineType = StripTags(ReadString(block, "engineType") ?? "");
            spec.AutoDetected.EnginePower = StripTags(ReadString(block, "enginePower") ?? "");
            spec.AutoDetected.EngineTorque = StripTags(ReadString(block, "engineTorque") ?? "");
            spec.AutoDetected.Weight = StripTags(ReadString(block, "weight") ?? "");
            spec.AutoDetected.Drivetrain = StripTags(ReadString(block, "drivetrain") ?? "");
            spec.AutoDetected.Origin = StripTags(ReadString(block, "origin") ?? "");
            spec.AutoDetected.RimFront = StripTags(ReadString(block, "rimFront") ?? "");
            spec.AutoDetected.TireFront = StripTags(ReadString(block, "tireFront") ?? "");
            spec.AutoDetected.RimRear = StripTags(ReadString(block, "rimRear") ?? "");
            spec.AutoDetected.TireRear = StripTags(ReadString(block, "tireRear") ?? "");
            spec.AutoDetected.Rarity = StripTags(ReadString(block, "rarity") ?? "");

            // ── priceModel ────────────────────────────────────────────────────────────
            int pmIdx = json.IndexOf("\"priceModel\"", StringComparison.Ordinal);
            if (pmIdx >= 0)
            {
                int pmOpen = json.IndexOf('{', pmIdx);
                int pmClose = json.IndexOf('}', pmOpen);
                if (pmOpen >= 0 && pmClose > pmOpen)
                {
                    string pm = json.Substring(pmOpen, pmClose - pmOpen + 1);
                    spec.PriceModel.BaseValue = ReadInt(pm, "baseValue");
                    spec.PriceModel.MaxParts = ReadInt(pm, "maxParts");
                    spec.PriceModel.MaxFrame = ReadInt(pm, "maxFrame");
                    spec.PriceModel.MaxBonus = ReadInt(pm, "maxBonus");
                }
            }

            // ── archetypePrices ───────────────────────────────────────────────────────
            int apIdx = json.IndexOf("\"archetypePrices\"", StringComparison.Ordinal);
            if (apIdx >= 0)
            {
                // Znajdź zewnętrzny { ... } bloku archetypePrices
                int apOpen = json.IndexOf('{', apIdx);
                int depth = 0, apClose = apOpen;
                for (int i = apOpen; i < json.Length; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}') { if (--depth == 0) { apClose = i; break; } }
                }
                string apBlock = json.Substring(apOpen, apClose - apOpen + 1);

                // Klucze muszą pasować do konwencji JSON — te same co w pliku
                string[] keys = {
                "wreckerL1", "wreckerL2", "wreckerL3",
                "dealerL1",    "dealerL2",    "dealerL3",
                "honestL1",    "honestL2",    "honestL3",
                "scammerL1",   "scammerL2",   "scammerL3",
                };

                foreach (string key in keys)
                {
                    int kIdx = apBlock.IndexOf($"\"{key}\"", StringComparison.Ordinal);
                    if (kIdx < 0) continue;
                    int innerOpen = apBlock.IndexOf('{', kIdx);
                    int innerClose = apBlock.IndexOf('}', innerOpen);
                    if (innerOpen < 0 || innerClose < innerOpen) continue;
                    string inner = apBlock.Substring(innerOpen, innerClose - innerOpen + 1);
                    spec.ArchetypePrices[key] = new ArchetypePrice
                    {
                        Chassis = ReadInt(inner, "chassis"),
                        Body = ReadInt(inner, "body"),
                        Km = ReadInt(inner, "km"),
                        Price = ReadInt(inner, "price"),
                    };
                }
            }


            return spec;
        }

        private static string ReadString(string json, string key)
        {
            // Matches: "key": "value"
            var m = Regex.Match(json,
                $@"""{Regex.Escape(key)}""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static int ReadInt(string json, string key)
        {
            var m = Regex.Match(json, $@"""{Regex.Escape(key)}""\s*:\s*(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        /// <summary>Strips Unity rich-text tags like &lt;color=#...&gt; and &lt;size=-6&gt;.</summary>
        private static string StripTags(string s) =>
            Regex.Replace(s, @"<[^>]+>", "").Trim();
    }
}
