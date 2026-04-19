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
        private readonly Dictionary<string, CarSpec> _specs =
            new(StringComparer.OrdinalIgnoreCase);

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
            // Try MEDRES first, fall back to LOWRES
            string[] roots = {
        Path.Combine(modsRoot, "CarImages_MEDRES"),
        Path.Combine(modsRoot, "CarImages_LOWRES"),
    };

            var loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (var carDir in Directory.GetDirectories(root))
                {
                    string specPath = Path.Combine(carDir, "car_spec.json");
                    if (!File.Exists(specPath)) continue;

                    try
                    {
                        var spec = ParseSpec(File.ReadAllText(specPath));
                        if (spec == null || string.IsNullOrEmpty(spec.CarId)) continue;

                        // Skip if already loaded from a higher-priority root
                        if (loaded.Contains(spec.CarId)) continue;

                        _specs[spec.CarId] = spec;
                        loaded.Add(spec.CarId);

                        OXLPlugin.Log.Msg(
                            $"[CarSpecLoader] Loaded spec for '{spec.CarId}' from {Path.GetFileName(root)}: " +
                            $"priceModel=(base={spec.PriceModel.BaseValue} parts={spec.PriceModel.MaxParts}) " +
                            $"archetypeKeys=[{string.Join(", ", spec.ArchetypePrices.Keys)}]");
                    }
                    catch (Exception ex)
                    {
                        OXLPlugin.Log.Msg($"[CarSpecLoader] Failed to load {specPath}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Returns spec for a listing's internalId (e.g. "car_salem_aries_1234").
        /// Returns an empty CarSpec (all fields "—") if not found.
        /// </summary>
        public CarSpec Get(string internalId)
        {
            // Strip the trailing _NNNN suffix to get base id
            string baseId = Regex.Replace(internalId, @"_\d+$", "");

            // Try alias first
            string lookupId = IdAliases.TryGetValue(baseId, out var alias) ? alias : baseId;

            if (_specs.TryGetValue(lookupId, out var spec)) return spec;

            // Return empty spec so callers never get null
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
                "neglectedL1", "neglectedL2", "neglectedL3",
                "dealerL1",    "dealerL2",    "dealerL3",
                "honestL1",    "honestL2",    "honestL3",
                "wreckerL1",   "wreckerL2",   "wreckerL3",
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
