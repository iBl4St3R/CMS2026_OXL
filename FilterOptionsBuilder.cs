// FilterOptionsBuilder.cs
// Buduje listy opcji filtrów dynamicznie z ActiveListings + CarSpecLoader.
// Wywoływane przy każdym otwarciu panelu filtrów — opcje odzwierciedlają
// aktualny stan rynku. Zero hardcoded marek / rarity / drivetrain.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CMS2026_OXL
{
    public class FilterOptions
    {
        public string[] Makes = Array.Empty<string>();
        public string[] EngineCategories = Array.Empty<string>();
        public string[] Drivetrains = Array.Empty<string>();
        public string[] Rarities = Array.Empty<string>();
        public string[] Colors = Array.Empty<string>();
        public string[] TireSizes = Array.Empty<string>();

        // Tylko do podpowiedzi placeholder/tooltip — nie ograniczają inputu
        public (int min, int max) YearRange = (0, 0);
        public (int min, int max) PriceRange = (0, 0);
        public (int min, int max) MileageRange = (0, 0);
        public (int min, int max) PowerRange = (0, 0);
        public (int min, int max) TorqueRange = (0, 0);
        public (int min, int max) WeightRange = (0, 0);
    }

    public static class FilterOptionsBuilder
    {
        public static FilterOptions Build(
            IReadOnlyList<CarListing> listings,
            CarSpecLoader specLoader)
        {
            var opt = new FilterOptions();
            if (listings == null || listings.Count == 0) return opt;

            var makes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var engines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var drives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tires = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int minYr = int.MaxValue, maxYr = int.MinValue;
            int minPr = int.MaxValue, maxPr = int.MinValue;
            int minMi = int.MaxValue, maxMi = int.MinValue;
            int minPw = int.MaxValue, maxPw = int.MinValue;
            int minTq = int.MaxValue, maxTq = int.MinValue;
            int minWt = int.MaxValue, maxWt = int.MinValue;

            foreach (var l in listings)
            {
                if (!string.IsNullOrEmpty(l.Make)) makes.Add(l.Make);
                if (!string.IsNullOrEmpty(l.Color)) colors.Add(l.Color);

                if (l.Year > 0) { minYr = Math.Min(minYr, l.Year); maxYr = Math.Max(maxYr, l.Year); }
                if (l.Price > 0) { minPr = Math.Min(minPr, l.Price); maxPr = Math.Max(maxPr, l.Price); }
                if (l.Mileage > 0) { minMi = Math.Min(minMi, l.Mileage); maxMi = Math.Max(maxMi, l.Mileage); }

                var spec = specLoader?.Get(l.InternalId, l.CarConfig);
                var ad = spec?.AutoDetected;
                if (ad == null) continue;

                var engCat = ExtractEngineCategory(ad.EngineType);
                if (!string.IsNullOrEmpty(engCat)) engines.Add(engCat);

                var drv = NormalizeDrivetrain(ad.Drivetrain);
                if (!string.IsNullOrEmpty(drv)) drives.Add(drv);

                if (!string.IsNullOrEmpty(ad.Rarity)) rarities.Add(ad.Rarity.Trim());

                var tire = ExtractTireSize(ad.TireFront);
                if (!string.IsNullOrEmpty(tire)) tires.Add(tire);

                int pw = ExtractPower(ad.EnginePower);
                if (pw > 0) { minPw = Math.Min(minPw, pw); maxPw = Math.Max(maxPw, pw); }

                int tq = ExtractTorque(ad.EngineTorque);
                if (tq > 0) { minTq = Math.Min(minTq, tq); maxTq = Math.Max(maxTq, tq); }

                int wt = ExtractWeight(ad.Weight);
                if (wt > 0) { minWt = Math.Min(minWt, wt); maxWt = Math.Max(maxWt, wt); }
            }

            opt.Makes = makes.OrderBy(x => x).ToArray();
            opt.EngineCategories = engines.OrderBy(x => x).ToArray();
            opt.Drivetrains = drives.OrderBy(DrvOrderKey).ToArray();
            opt.Rarities = rarities.OrderBy(RarityOrderKey).ToArray();
            opt.Colors = colors.OrderBy(x => x).ToArray();
            opt.TireSizes = tires.OrderBy(x => x).ToArray();

            opt.YearRange = minYr <= maxYr ? (minYr, maxYr) : (0, 0);
            opt.PriceRange = minPr <= maxPr ? (minPr, maxPr) : (0, 0);
            opt.MileageRange = minMi <= maxMi ? (minMi, maxMi) : (0, 0);
            opt.PowerRange = minPw <= maxPw ? (minPw, maxPw) : (0, 0);
            opt.TorqueRange = minTq <= maxTq ? (minTq, maxTq) : (0, 0);
            opt.WeightRange = minWt <= maxWt ? (minWt, maxWt) : (0, 0);

            return opt;
        }

        // ── Parsery — wszystkie tolerancyjne na pusty/nietypowy wejście ───────

        private static readonly Regex _rxPower = new(@"(\d+)\s*HP", RegexOptions.IgnoreCase);
        private static readonly Regex _rxTorque = new(@"(\d+)\s*N\.?m", RegexOptions.IgnoreCase);
        private static readonly Regex _rxWeight = new(@"(\d[\d\s,\.]*)\s*kg", RegexOptions.IgnoreCase);
        private static readonly Regex _rxTire = new(@"(\d{3}/\d{2}/\d{2})");
        // Engine categories commonly used in the game: V6/V8/I4/R4/R6/W12/H4/Flat-6/Rotary
        private static readonly Regex _rxEngCat = new(
            @"^\s*(V\d+|I\d+|R\d+|W\d+|H\d+|Flat-?\d+|Rotary|Wankel)\b",
            RegexOptions.IgnoreCase);

        public static int ExtractPower(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            var m = _rxPower.Match(s);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        public static int ExtractTorque(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            var m = _rxTorque.Match(s);
            return m.Success && int.TryParse(m.Groups[1].Value, out int v) ? v : 0;
        }

        public static int ExtractWeight(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            var m = _rxWeight.Match(s);
            if (!m.Success) return 0;
            string num = Regex.Replace(m.Groups[1].Value, @"[\s,\.]", "");
            return int.TryParse(num, out int v) ? v : 0;
        }

        public static string ExtractTireSize(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var m = _rxTire.Match(s);
            return m.Success ? m.Groups[1].Value : null;
        }

        public static string ExtractEngineCategory(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var m = _rxEngCat.Match(s);
            return m.Success ? m.Groups[1].Value.ToUpperInvariant() : null;
        }

        public static string NormalizeDrivetrain(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            string u = s.Trim().ToUpperInvariant().Replace(" ", "");
            return u switch
            {
                "4X4" or "AWD" => u == "4X4" ? "4x4" : "AWD",
                "FWD" => "FWD",
                "RWD" => "RWD",
                _ => s.Trim(),
            };
        }

        // Stabilny sort: FWD, RWD, AWD, 4x4 — reszta alfabetycznie
        private static string DrvOrderKey(string d) => d switch
        {
            "FWD" => "1FWD",
            "RWD" => "2RWD",
            "AWD" => "3AWD",
            "4x4" => "4_4x4",
            _ => "9" + d,
        };

        // Common → Uncommon → Rare → Legendary
        private static string RarityOrderKey(string r) => (r ?? "").ToLowerInvariant() switch
        {
            var s when s.Contains("common") => "1" + r,
            var s when s.Contains("uncommon") => "2" + r,
            var s when s.Contains("rare") => "3" + r,
            var s when s.Contains("legendary") => "4" + r,
            _ => "9" + r,
        };
    }
}