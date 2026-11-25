// ItemStoryBuilder.cs
// Opisy sprzedawców dla silników, części, narzędzi i dekoracji (site.md 9.4): powód sprzedaży jak w życiu (spirala kredytowa, upgrade,
// rozbity tył, wyprzedaż warsztatu) + styl archetypu. Sprzedawcy kłamią w opisie (stan), zdjęcie jest katalogowe ("Stock photo").
// Teksty dla gracza: angielski, ASCII, bez myślników.

using System;

namespace CMS2026_OXL
{
    public static class ItemStoryBuilder
    {
        private static readonly string[] EngineReasons =
        {
            "Pulled it out of my car after somebody wrecked the rear end. The engine was never touched by the crash.",
            "Went for a bigger engine in my project, so this one has to go.",
            "The bank is breathing down my neck, so I am selling everything I do not need right now.",
            "Clearing out my late uncle's garage. No idea about the history, sold as found.",
            "Bought it for a project that never happened. It has been sitting on a stand since.",
            "Came out of a parts car that I am scrapping. It ran when the car was parked.",
            "Swapped it out during a rebuild. Old unit, still turns over by hand.",
            "Moving abroad at the end of the month, everything must go.",
            "My wife wants the garage back. Make me an offer and take it away.",
        };

        private static readonly string[] PartReasons =
        {
            "Spare from my own build, ended up using a different one.",
            "Took it off a car I was parting out after a crash.",
            "Upgraded to a performance part, so the original is up for grabs.",
            "Leftover from a job, the customer changed their mind.",
            "Need cash for rent this month, so the spare parts shelf is going.",
            "Found it while cleaning the workshop. Not sure which car it came from.",
            "Bought the wrong one for my car and the shop will not take it back.",
        };

        private static readonly string[] ToolReasons =
        {
            "Closing my workshop for good, everything goes.",
            "Got a newer model, this one is surplus.",
            "Moving to a smaller place and there is simply no room for it.",
            "Bought two by mistake, the second one is still unused.",
            "Divorce sale. Do not ask, just take it.",
            "Inherited a whole workshop, I only need half of it.",
            "Retiring after thirty years in the trade.",
            "Redoing the whole workshop in a new style, the old stuff has to go.",
            "Behind on my loan payments, selling what I can live without.",
            "Picked it up at an auction with a pile of other stuff, never used it.",
            "My landlord wants the space back by Friday.",
            "Used it on one project and it has been gathering dust since.",
        };

        public static string Build(ItemListing l, Random rng)
        {
            string reason = l.Kind switch
            {
                OXLItemKind.Engine => EngineReasons[rng.Next(EngineReasons.Length)],
                OXLItemKind.Part => PartReasons[rng.Next(PartReasons.Length)],
                _ => ToolReasons[rng.Next(ToolReasons.Length)],
            };

            string cond = l.HasCondition ? $" Condition: {ConditionWord(l.ApparentCondition)}." : "";
            string style;
            if (l.IsGem)
                style = "Need it gone today, so it is priced to sell. First one to pay takes it.";
            else
                style = l.Archetype switch
                {
                    SellerArchetype.Honest => l.HasCondition ? $"I would say it is honestly around {(int)Math.Round(l.ApparentCondition * 100f)} percent. Happy to answer questions." : "Works as it should. Happy to answer questions.",
                    SellerArchetype.Wrecker => l.HasCondition ? "Has not been looked at in years, but it should be fine. Sold as is." : "A bit dusty and scratched, but it does the job.",
                    SellerArchetype.Dealer => "Professionally checked, top quality, fair price. No time wasters please.",
                    // Scammer L1 krzyczy i pogania; L2 brzmi spokojniej, ale dalej "nówka" bez szczegółów i z pośpiechem (scammer.md 5).
                    _ => l.Level >= 2 ? "Used only a few times, works perfectly. Selling because I am moving next week, so the price is low." : "Perfect condition, barely used. First come first served, payment upfront only.",
                };
            return $"{reason}{cond} {style}";
        }

        public static string ConditionWord(float c) =>
            c >= 0.85f ? "like new" :
            c >= 0.70f ? "very good" :
            c >= 0.50f ? "good, normal wear" :
            c >= 0.30f ? "worn, but works" : "for parts or rebuild";

        // ── Tytuł ogłoszenia pisany przez sprzedawcę (site.md 11.1) ─────────────────
        // Gracz rozpoznaje sprzedawcę po stylu: dealer krzyczy wielkimi literami, scammer obiecuje cuda, wrecker nic nie gwarantuje.
        public static string Title(string name, SellerArchetype a, bool gem, bool hasCondition, int seed, int level = 1)
        {
            var rng = new Random(seed);
            string[] pool;
            if (gem) pool = new[] { "{0}, need it gone today", "{0} cheap, moving next week", "{0}, priced to sell" };
            else pool = a switch
            {
                SellerArchetype.Honest => hasCondition ? new[] { "{0}, honest condition, see description", "{0} used, works", "Selling {0}", "{0}, removed from my project" } : new[] { "{0}, used, works fine", "Selling {0}", "{0} in good shape" },
                SellerArchetype.Wrecker => hasCondition ? new[] { "{0} sold as is", "old {0} from the barn", "{0}, no idea if it works, cheap", "{0} for repair or parts" } : new[] { "{0}, a bit scratched, cheap", "{0} dusty but ok", "{0} must go" },
                SellerArchetype.Dealer => new[] { "{1} * CHECKED * BEST PRICE *", "{1} * PRO QUALITY * FAST SHIPPING", "{1} TOP CONDITION * WARRANTY *", "{1} * LIKE NEW * INVOICE" },
                _ => level >= 2 ? new[] { "{0}, like new, quick sale", "{0} barely used, moving out", "Selling {0}, perfect condition" } : new[] { "{0} LIKE NEW!!! cheap, must go today", "{0} PERFECT CONDITION 100% urgent sale", "{0} barely used!!! first come first served", "NEW {0} only today, bargain!!!" },
            };
            return string.Format(pool[rng.Next(pool.Length)], name, (name ?? "").ToUpperInvariant());
        }

        /// <summary>Tytuł auta pisany przez sprzedawcę; seed z ID ogłoszenia, więc stały między odświeżeniami.</summary>
        public static string CarTitle(string make, string model, int year, SellerArchetype a, int seed)
        {
            var rng = new Random(seed);
            string car = $"{make} {model} {year}";
            string[] pool = a switch
            {
                SellerArchetype.Honest => new[] { "{0}, honest car, see description", "{0} daily driver, known history", "Selling my {0}" },
                SellerArchetype.Wrecker => new[] { "{0} needs some love", "{0} project car, sold as is", "{0} sat for years, cheap" },
                SellerArchetype.Dealer => new[] { "{1} * FULL SERVICE * LOW MILES * SWAP", "{1} * IMPORTED * FIRST OWNER *", "{1} * PERFECT * WARRANTY * FINANCE" },
                _ => new[] { "{0} PERFECT CONDITION NO RUST urgent!!!", "{0} like new, cheap, must go today", "{0} 100% original, first come first served" },
            };
            return string.Format(pool[rng.Next(pool.Length)], car, car.ToUpperInvariant());
        }

        // ── Odpowiedź sprzedawcy na "Message seller" (mail) ───────────────────────────
        public static string SellerReply(SellerArchetype a, bool gem, bool hasCondition, float apparent, int level = 1)
        {
            if (gem) return "Yes, still here. I am moving next week, that is why the price is low. First one to pay gets it.";
            return a switch
            {
                SellerArchetype.Honest => hasCondition ? $"Hi, yes it is still available. Condition is as I wrote, around {(int)Math.Round(apparent * 100f)} percent. You can buy it right away on OXL." : "Hi, yes it is still available and works fine. You can buy it right away on OXL.",
                SellerArchetype.Wrecker => "yes available. it sat for a while, i dont really know much about it. price is final, sold as is",
                SellerArchetype.Dealer => "Good day. The item is in excellent condition and professionally inspected. The price is firm. Many people are asking, so please decide quickly.",
                _ => level >= 2 ? "Hi, yes it is still available and in great condition. A few people are asking, so if you buy now I can send it today." : "Hello my friend!! Item is perfect, like new 100%. I have 3 other buyers today so buy now please. Payment upfront only, no viewing, I am abroad right now.",
            };
        }
    }
}
