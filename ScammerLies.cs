// ScammerLies.cs
// Kłamstwa scammera w ogłoszeniu auta (Docs/Design/scammer.md 2 i 3). Prawdziwe dane zostają w zapisie (dostawa, zdjęcia, kolor lakieru);
// strona i opis pokazują wartości "Shown", czyli to, co napisał sprzedawca. Każda niezgodność jest do sprawdzenia przez gracza:
// zdjęcia (kolor), dane wersji w grze i wiki AutoFreaks (wersja, silnik, moc).
//   L1: bez zmian, oszust widać po opisie i ocenie 1-2.
//   L2: jedna niezgodność, łatwa do wychwycenia: moc 5x większa.
//   L3: 1 albo 2 losowe z czterech: kolor, silnik, wymyślona wersja, moc 5x; ocena 3 albo 3.5.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CMS2026_OXL
{
    public static class ScammerLies
    {
        // Wymyślone wersje (tekst sprzedawcy, nie dane gry): brzmią jak topowe odmiany, których żadne auto w grze nie ma.
        private static readonly string[] FakeVersions =
        {
            "DEMON", "Super XL", "GT Ultimate", "RS Turbo Plus", "Black Edition X", "Hyper Sport", "V-MAX Competition", "Turbo S Evolution",
            "Track Monster", "Platinum Executive", "Street King", "Nitro Edition", "Apex Predator", "Phantom GTR", "Diamond Limited", "Twin Turbo Beast",
        };

        // Nazwy kolorów takie, jakie daje OXLSite.ColorName dla prawdziwego lakieru.
        private static readonly string[] ColorNames = { "Black", "White", "Silver", "Gray", "Beige", "Red", "Dark red", "Orange", "Brown", "Yellow", "Green", "Dark green", "Teal", "Blue", "Dark blue", "Purple", "Pink" };

        private const float PowerLie = 5f;

        /// <summary>Przy generowaniu, po ustaleniu koloru (paczka zdjęć): losuje kłamstwa scammera L2/L3. Innym archetypom czyści pola.</summary>
        public static void Roll(CarListing l, MarketEntry e, Random rng)
        {
            l.FakeColor = l.FakeEngine = l.FakeVersion = "";
            l.FakeHpMult = 0f;
            if (l.Archetype != SellerArchetype.Scammer || l.ArchetypeLevel < 2) return;

            bool hasHp = e != null && e.Hp > 0;
            if (l.ArchetypeLevel == 2)
            {
                if (hasHp) l.FakeHpMult = PowerLie;
            }
            else
            {
                var options = new List<Action>
                {
                    () => { string real = RealColorName(l); var pool = ColorNames.Where(c => !string.Equals(c, real, StringComparison.OrdinalIgnoreCase)).ToArray(); l.FakeColor = pool[rng.Next(pool.Length)]; },
                    () => l.FakeVersion = FakeVersions[rng.Next(FakeVersions.Length)],
                };
                string engine = PickForeignEngine(e, rng);
                if (engine != null) options.Add(() => l.FakeEngine = engine);
                if (hasHp) options.Add(() => l.FakeHpMult = PowerLie);
                // 1 albo 2 niezgodności naraz (scammer.md 5.1): wszystkie cztery byłyby za łatwe do wychwycenia.
                int count = rng.Next(1, 3);
                foreach (var pick in options.OrderBy(_ => rng.Next()).Take(count)) pick();
            }
            OXLLog.Msg($"[OXL:SCAM] {l.InternalId} L{l.ArchetypeLevel} lies: color='{l.FakeColor}' engine='{l.FakeEngine}' version='{l.FakeVersion}' hpMult={l.FakeHpMult:F0}");
        }

        // Silnik, którego ta wersja mieć nie może: fabryczny silnik innej wersji z gry, mocniejszy (sprzedawca się chwali). Z danych gry, bez listy w kodzie.
        private static string PickForeignEngine(MarketEntry e, Random rng)
        {
            if (e == null) return null;
            var others = MarketCatalog.In(MarketCategory.Cars).Where(x => x.EngineName.Length > 0 && !string.Equals(x.EngineName, e.EngineName, StringComparison.OrdinalIgnoreCase)).ToList();
            var stronger = others.Where(x => x.Hp > e.Hp).ToList();
            var pool = (stronger.Count > 0 ? stronger : others).Select(x => x.EngineName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return pool.Count > 0 ? pool[rng.Next(pool.Count)] : null;
        }

        private static string RealColorName(CarListing l) => OXLSite.ColorName(OXLPanel.HexColor(l));

        // ── Co pokazuje strona i opis ────────────────────────────────────────────
        public static string ShownColor(CarListing l) => !string.IsNullOrEmpty(l?.FakeColor) ? l.FakeColor : RealColorName(l);
        public static string ShownVersion(CarListing l, MarketEntry e) => !string.IsNullOrEmpty(l?.FakeVersion) ? l.FakeVersion : e?.VersionName ?? "";
        public static string ShownEngine(CarListing l, MarketEntry e) => !string.IsNullOrEmpty(l?.FakeEngine) ? l.FakeEngine : e?.EngineName ?? "";
        public static int ShownHp(CarListing l, MarketEntry e) => e == null || e.Hp <= 0 ? 0 : l != null && l.FakeHpMult > 0f ? (int)Math.Round(e.Hp * l.FakeHpMult) : e.Hp;

        /// <summary>
        /// Zdanie z "danymi" auta dopisywane do opisu scammera L2/L3. Miesza prawdę z kłamstwem (wersja, silnik, moc, kolor tak jak na stronie),
        /// więc niezgodność nie rzuca się w oczy samym istnieniem zdania. Pusty tekst dla innych.
        /// </summary>
        public static string SpecsSentence(CarListing l, MarketEntry e)
        {
            if (l == null || l.Archetype != SellerArchetype.Scammer || l.ArchetypeLevel < 2) return "";
            var parts = new List<string>();
            string ver = ShownVersion(l, e), eng = ShownEngine(l, e);
            int hp = ShownHp(l, e);
            if (ver.Length > 0) parts.Add($"the {ver} version");
            if (eng.Length > 0) parts.Add($"{eng} engine");
            if (hp > 0) parts.Add($"{hp} HP");
            parts.Add($"{ShownColor(l).ToLowerInvariant()} paint");
            return l.ArchetypeLevel == 2
                ? $"Specs: {string.Join(", ", parts)}. Very fast car, you will see."
                : $"Full specification: {string.Join(", ", parts)}. All documents in order.";
        }
    }
}
