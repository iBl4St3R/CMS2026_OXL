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

    public class CarSpec
    {
        public string CarId { get; set; } = "";
        public string CarName { get; set; } = "";
        public CarSpecData AutoDetected { get; set; } = new();
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
            string medRes = Path.Combine(modsRoot, "CarImages_MEDRES");
            if (!Directory.Exists(medRes)) return;

            foreach (var carDir in Directory.GetDirectories(medRes))
            {
                string specPath = Path.Combine(carDir, "car_spec.json");
                if (!File.Exists(specPath)) continue;

                try
                {
                    var spec = ParseSpec(File.ReadAllText(specPath));
                    if (spec != null && !string.IsNullOrEmpty(spec.CarId))
                    {
                        _specs[spec.CarId] = spec;
                        OXLPlugin.Log.Msg(
                            $"[CarSpecLoader] Loaded spec for '{spec.CarId}' from {specPath}");
                    }
                }
                catch (Exception ex)
                {
                    OXLPlugin.Log.Msg(
                        $"[CarSpecLoader] Failed to load {specPath}: {ex.Message}");
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

            return spec;
        }

        private static string ReadString(string json, string key)
        {
            // Matches: "key": "value"
            var m = Regex.Match(json,
                $@"""{Regex.Escape(key)}""\s*:\s*""([^""\\]*(?:\\.[^""\\]*)*)""");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Strips Unity rich-text tags like &lt;color=#...&gt; and &lt;size=-6&gt;.</summary>
        private static string StripTags(string s) =>
            Regex.Replace(s, @"<[^>]+>", "").Trim();
    }
}
