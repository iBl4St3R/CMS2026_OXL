using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CMS2026_OXL
{
    public class ListingSystem
    {
        // ── Car definitions — nowe widełki skalibrowane do cen skupu ─────────
        private class CarDef
        {
            public string Make, Model, ImageFolder, InternalId;
            public int MinYear, MaxYear, MinPrice, MaxPrice;
        }

        private static readonly CarDef[] CarDefs =
 {
    new CarDef { Make = "DNB",      Model = "Censor",         ImageFolder = "DNB Censor",
                 InternalId = "car_dnb_censor",
                 MinYear = 1991, MaxYear = 1992, MinPrice = 3500,  MaxPrice = 12000 },
    new CarDef { Make = "Katagiri", Model = "Tamago BP",       ImageFolder = "Katagiri Tamago BP",
                 InternalId = "car_katagiri_tamago",
                 MinYear = 2000, MaxYear = 2005, MinPrice = 3500,  MaxPrice = 12000 },
    new CarDef { Make = "Luxor",    Model = "Streamliner Mk3", ImageFolder = "Luxor Streamliner Mk3",
                 InternalId = "car_luxor_streamliner",
                 MinYear = 1992, MaxYear = 1996, MinPrice = 4000,  MaxPrice = 14000 },
    new CarDef { Make = "Mayen",    Model = "M5",              ImageFolder = "Mayen M5",
                 InternalId = "car_mayen_m5",
                 MinYear = 2008, MaxYear = 2015, MinPrice = 10000, MaxPrice = 26000 },
    new CarDef { Make = "Salem",    Model = "Aries MK3",       ImageFolder = "Salem Aries MK3",
                 InternalId = "car_salem_aries",
                 MinYear = 1994, MaxYear = 1999, MinPrice = 3000,  MaxPrice = 11000 },
};

        // ══════════════════════════════════════════════════════════════════════
        //  PULE OPISÓW — pogrupowane per archetyp
        //  Kilka jest "awaryjnych" — nadpisują losowanie gdy wystąpi konkretna flaga
        // ══════════════════════════════════════════════════════════════════════

        // ── Archetype A — Uczciwy ─────────────────────────────────────────────
        private static readonly string[] NotesHonest =
        {
            "Selling because I'm upgrading. Condition as shown in photos, nothing to hide.",
    "Good runner, price is lower because it needs some work. I've described everything honestly.",
    "Garage kept, single owner. Minor faults accounted for in the price.",
    "No time to fix it myself — selling with known issues, hence the below-market price.",
    "Solid car, just need the cash. Condition matches the photos perfectly.",
    "Not going to pretend it's perfect. What you see is what you get.",
    "Priced honestly. I'd rather sell it cheap than waste your time with nonsense.",
    "Everything I know about this car is in the description. No surprises.",
    "Bought it as a daily, never got around to fixing the small stuff. Price reflects that.",
    "I'm a mechanic myself — I know what this car needs. Hence the fair price.",
        };

        // Notki awaryjne dla Honest — aktywowane przez FaultFlags
        private static readonly Dictionary<FaultFlags, string> NotesHonestFault =
            new Dictionary<FaultFlags, string>
        {
            { FaultFlags.TimingBelt,
        "Full disclosure: timing belt needs replacement soon. Price adjusted accordingly." },
    { FaultFlags.HeadGasket,
        "Engine needs a rebuild — head gasket is gone. Selling as a project car, priced as such." },
    { FaultFlags.SuspensionWorn,
        "Suspension needs attention, slight knocking from the front over bumps. Price reflects this." },
    { FaultFlags.BrakesGone,
        "Pads and discs need immediate replacement — braking is not where it should be." },
    { FaultFlags.ElectricalFault,
        "Electrical gremlin somewhere, possibly alternator. Haven't chased it — price accounts for the risk." },
        };

        // ── Archetype B — Zaniedbany właściciel ──────────────────────────────
        private static readonly string[] NotesNeglected =
        {
            "Drives fine. Did the services myself when I remembered to.",
    "Solid car, knocks a bit when cold but settles after a minute or two.",
    "Selling because I have no time to look after it. Starts every time.",
    "Used it daily, no major drama. Might smoke a little on cold starts.",
    "Cheap to run, never asked for much over the years. Moving on to something newer.",
    "Drove it to work every day for years. It's a tool, not a show car.",
    "Nothing catastrophic ever went wrong with it. Small things here and there.",
    "Honestly can't remember the last full service but it never let me down.",
    "Ran it hard for a few years. Tyres are newer, rest is as-is.",
    "It's not pretty but it always got me where I needed to go.",
    "Previous owner kept receipts, I did not. Still runs well though.",
    "A few warning lights but the mechanic I asked said to just ignore them.",
        };

        // ── Archetype C — Handlarz ────────────────────────────────────────────
        private static readonly string[] NotesDealer =
        {
            "Freshly serviced, everything 100% functional, ready for the road today.",
    "One owner from new, garage kept, drives like it just rolled off the line.",
    "Just finished a full inspection — everything checked and passed without issue.",
    "Professional detailing done. Looks and drives like new.",
    "Selling due to family expansion. Viewing will not disappoint, serious buyers only.",
    "Drives like it's on rails, handling is perfect. First to see will buy.",
    "Regularly maintained by specialists. Nothing to worry about here.",
    "Immaculate example. Price is firm because the car speaks for itself.",
    "Bought it, didn't end up needing it. Stored dry, runs perfectly.",
    "Full history, all stamps, nothing to hide. A genuinely clean example.",
    "Turn key ready. Not a project — everything works as it should.",
    "Private sale from a careful, experienced driver. Condition is exceptional.",
        };

        // ── Archetype D — Złomiarz ────────────────────────────────────────────
        private static readonly string[] NotesWrecker =
        {
             // Gemini picks
    "Sir go to Farget and reedem the code please and i send you the car rocket fast. God bless.",
    "Buy, buy, I happy me happy. Car very good. Happy day :) Send money now.",
    "Hemu very good speed delivery through desert. Pay for gas and I ship car now. Very fast!",
    "My mother car, very little use. Like new. Please pay with fruit cards or Farget codes.",
    "Hemu shipping 24h. Very fast delivery from overseas. Best price for you friend.",
    "I am currently out of country but my brother ship car if you send code from Farget.",

    // My additions
    "Very good car. I cry when I sell. Please send gift card and I cry less.",
    "Car run like cheetah. I am honest man. My cousin verify. Send deposit, we talk.",
    "Sir this is not scam. I am engineer. The smoke is normal for this model. Buy now.",
    "Me and car very close friend. You buy, you also become close friend. PalPal only.",
    "I ship from overseas. You pay shipping + gas + small fee for my time. Very reasonable.",
    "First person to send Farget code get special price. Very limited offer. God is watching.",
    "Engine very quiet because it is calm. This is feature not problem. Trust the process.",
    "I not respond to lowball. I respond to Farget, Amaz, or Steem card. God bless you.",
    "My uncle was mechanic before the incident. He says car is fine. I trust him.",
        };

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVE LISTINGS
        // ══════════════════════════════════════════════════════════════════════

        public List<CarListing> ActiveListings { get; private set; } = new();

        private float _gameTime = 0f;
        public float GameTime => _gameTime;

        public void Tick(float deltaTime)
        {
            _gameTime += deltaTime;
            ActiveListings.RemoveAll(l => l.ExpiresAt <= _gameTime);

            while (ActiveListings.Count < 4)
                ActiveListings.Add(GenerateListing());

            if (ActiveListings.Count < 10 && UnityEngine.Random.value < 0.002f)
                ActiveListings.Add(GenerateListing());
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GENEROWANIE LISTINGU
        // ══════════════════════════════════════════════════════════════════════

        private CarListing GenerateListing()
        {
            var rng = new Random();
            var def = CarDefs[rng.Next(CarDefs.Length)];
            var ttl = UnityEngine.Random.Range(120f, 600f);
            int year = rng.Next(def.MinYear, def.MaxYear + 1);

            // 1. Archetyp sprzedawcy
            var archetype = RollArchetype(rng);

            int rating = RollSellerRating(archetype, rng);

            // 2. ApparentCondition — co gracz widzi (dyktuje cenę)
            //    Dealer wystawia auto wypolerowane (wysoka pozorna kondycja)
            //    Wrecker może wystawić auto w dowolnym stanie z dobrym opisem
            float apparent = archetype switch
            {
                SellerArchetype.Dealer => Mathf.Clamp((float)BetaSample(rng, 2.5, 1.5), 0.45f, 0.95f),
                SellerArchetype.Wrecker => Mathf.Clamp((float)BetaSample(rng, 1.5, 2.5), 0.15f, 0.75f),
                _ => Mathf.Clamp((float)BetaSample(rng, 1.8, 3.5), 0.08f, 0.95f),
            };

            // 3. ActualCondition — rzeczywisty stan mechaniki
            float honesty = RollHonesty(archetype, rng);
            float noise = (float)(rng.NextDouble() * 0.06 - 0.03);
            float actual = Mathf.Clamp(apparent * honesty + noise, 0.02f, 0.97f);

            // 4. Ukryte usterki — losowane z puli właściwej dla archetypu i actual
            FaultFlags faults = RollFaults(archetype, actual, rng);

            // 5. Cena — bazuje na ApparentCondition
            float basePricef = Mathf.Lerp(def.MinPrice, def.MaxPrice, apparent);
            float priceNoise = 1f + (float)(rng.NextDouble() * 0.16 - 0.08);
            int price = Mathf.RoundToInt(basePricef * priceNoise / 50f) * 50;
            price = Mathf.Clamp(price, def.MinPrice, def.MaxPrice);

            // Uczciwy sprzedawca z usterkami daje rabat
            if (archetype == SellerArchetype.Honest && faults != FaultFlags.None)
                price = Mathf.RoundToInt(price * 0.78f / 50f) * 50;

            // 6. Przebieg — z ActualCondition (bardziej prawdziwy)
            int mileage = Mathf.RoundToInt(
                Mathf.Lerp(180000, 4000, actual) * (1f + (float)(rng.NextDouble() * 0.30 - 0.15)));
            mileage = Mathf.Max(500, mileage);

            // 7. Nota sprzedawcy
            string note = SelectNote(archetype, faults, rng);

            return new CarListing
            {
                Registration = GenReg(rng),
                Make = def.Make,
                Model = def.Model,
                ImageFolder = def.ImageFolder,
                Year = year,
                Price = price,
                ApparentCondition = apparent,
                ActualCondition = actual,
                Archetype = archetype,
                Faults = faults,
                SellerNote = note,
                ExpiresAt = _gameTime + ttl,
                InternalId = def.InternalId + "_" + rng.Next(1000, 9999),
                Mileage = mileage,
                Location = Locations[rng.Next(Locations.Length)],
                DeliveryHours = rng.Next(1, 37),
                SellerRating = rating,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ARCHETYP — rozkład i uczciwość
        // ══════════════════════════════════════════════════════════════════════

        private static SellerArchetype RollArchetype(Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.20) return SellerArchetype.Honest;    // 20%
            if (r < 0.55) return SellerArchetype.Neglected; // 35%
            if (r < 0.85) return SellerArchetype.Dealer;    // 30%
            return SellerArchetype.Wrecker;                  // 15%
        }

        private static float RollHonesty(SellerArchetype arch, Random rng)
        {
            return arch switch
            {
                SellerArchetype.Honest => (float)(0.90 + rng.NextDouble() * 0.10), // 0.90–1.00
                SellerArchetype.Neglected => (float)(0.78 + rng.NextDouble() * 0.17), // 0.78–0.95
                SellerArchetype.Dealer => (float)(0.42 + rng.NextDouble() * 0.33), // 0.42–0.75
                SellerArchetype.Wrecker => (float)(0.07 + rng.NextDouble() * 0.28), // 0.07–0.35
                _ => 1.0f
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FAULT ROLLING
        // ══════════════════════════════════════════════════════════════════════

        private static FaultFlags RollFaults(SellerArchetype arch, float actual, Random rng)
        {
            var f = FaultFlags.None;

            // Pasek rozrządu — pułapka nr 1, najważniejsza
            double timingChance = arch switch
            {
                SellerArchetype.Wrecker => 0.55,
                SellerArchetype.Neglected => 0.35,
                SellerArchetype.Dealer => 0.12,
                _ => 0.04, // Honest rzadko zatai
            };
            if (rng.NextDouble() < timingChance) f |= FaultFlags.TimingBelt;

            // Uszczelka głowicy — tylko zły stan + wrecker
            if (actual < 0.30 && arch == SellerArchetype.Wrecker && rng.NextDouble() < 0.30)
                f |= FaultFlags.HeadGasket;

            // Zawieszenie — dealer ukrywa, neglected ignoruje
            double suspChance = arch switch
            {
                SellerArchetype.Dealer => 0.62,
                SellerArchetype.Wrecker => 0.50,
                SellerArchetype.Neglected => 0.38,
                _ => 0.12,
            };
            if (rng.NextDouble() < suspChance) f |= FaultFlags.SuspensionWorn;

            // Hamulce — powszechne u neglected/wrecker
            double brakeChance = arch switch
            {
                SellerArchetype.Honest => 0.18,
                SellerArchetype.Neglected => 0.48,
                SellerArchetype.Dealer => 0.30, // zatuszowane
                SellerArchetype.Wrecker => 0.55,
                _ => 0.20
            };
            if (rng.NextDouble() < brakeChance) f |= FaultFlags.BrakesGone;

            // Tłumik — korozja, zależna od actual
            if (actual < 0.50 && rng.NextDouble() < 0.45) f |= FaultFlags.ExhaustRusted;

            // Elektryka — losowe, każdy archetyp
            if (rng.NextDouble() < 0.14) f |= FaultFlags.ElectricalFault;

            // Szyby / reflektory — zależne od actual
            double glassChance = actual < 0.35 ? 0.35 : 0.07;
            if (rng.NextDouble() < glassChance) f |= FaultFlags.GlassDamage;

            return f;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SELEKCJA OPISU
        // ══════════════════════════════════════════════════════════════════════

        private static string SelectNote(SellerArchetype arch, FaultFlags faults, Random rng)
        {
            // Uczciwy sprzedawca z krytyczną usterką — nota musi to odzwierciedlać
            if (arch == SellerArchetype.Honest)
            {
                // Priorytet: HeadGasket > TimingBelt > Suspension > Brakes > Electrical
                foreach (var flag in new[] {
                    FaultFlags.HeadGasket,
                    FaultFlags.TimingBelt,
                    FaultFlags.SuspensionWorn,
                    FaultFlags.BrakesGone,
                    FaultFlags.ElectricalFault })
                {
                    if (faults.HasFlag(flag) && NotesHonestFault.TryGetValue(flag, out var specific))
                        return specific;
                }
            }

            // Dla pozostałych — losuj z puli archetypu
            string[] pool = arch switch
            {
                SellerArchetype.Honest => NotesHonest,
                SellerArchetype.Neglected => NotesNeglected,
                SellerArchetype.Dealer => NotesDealer,
                SellerArchetype.Wrecker => NotesWrecker,
                _ => NotesNeglected,
            };

            return pool[rng.Next(pool.Length)];
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PURCHASE
        // ══════════════════════════════════════════════════════════════════════

        public bool TryPurchase(CarListing listing, Action<string> spawnCar, Action<int> deductMoney)
        {
            if (!ActiveListings.Contains(listing)) return false;
            spawnCar(listing.InternalId);
            deductMoney(listing.Price);
            ActiveListings.Remove(listing);
            OXLPlugin.Log.Msg(
                $"[OXL] Purchased: {listing.Make} {listing.Model} " +
                $"| Arch={listing.Archetype} " +
                $"| Apparent={listing.ApparentCondition:P0} Actual={listing.ActualCondition:P0} " +
                $"| Faults={listing.Faults}");
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════


        // ── Helper: gwiazdki sprzedawcy ──────────────────────────────────────────
        private static string FormatStars(int rating)
        {
            // Wypełnione ★ + puste ☆ żeby zawsze było 5 znaków — czytelna skala
            return new string('★', rating) + new string('☆', 5 - rating);
        }

        private static Color StarColor(int rating) => rating switch
        {
            5 => new Color(0.22f, 0.75f, 0.40f, 1f), // zielony  — godny zaufania
            4 => new Color(0.55f, 0.80f, 0.30f, 1f), // żółtozielony
            3 => new Color(0.85f, 0.72f, 0.20f, 1f), // żółty    — uważaj
            2 => new Color(0.90f, 0.45f, 0.15f, 1f), // pomarańcz
            _ => new Color(0.90f, 0.20f, 0.20f, 1f), // czerwony — uciekaj
        };



        private static int RollSellerRating(SellerArchetype arch, Random rng)
        {
            // Losujemy z przedziału właściwego dla archetypu
            // Dealer celowo może dostać wysoką ocenę — to jest pułapka
            return arch switch
            {
                SellerArchetype.Honest => rng.Next(0, 10) < 8 ? 5 : 4,        // 80% = 5★, 20% = 4★
                SellerArchetype.Neglected => rng.Next(0, 10) < 6 ? 4 : 3,        // 60% = 4★, 40% = 3★
                SellerArchetype.Dealer => rng.Next(0, 10) switch               // pułapka
                {
                    < 3 => 5,   // 30% = 5★ — bardzo przekonujący
                    < 7 => 4,   // 40% = 4★
                    _ => 3    // 30% = 3★
                },
                SellerArchetype.Wrecker => rng.Next(0, 10) < 7 ? 1 : 2,        // 70% = 1★, 30% = 2★
                _ => 3
            };
        }
        private static double BetaSample(Random rng, double alpha, double beta)
        {
            double x = GammaSample(rng, alpha);
            double y = GammaSample(rng, beta);
            return x / (x + y);
        }

        private static double GammaSample(Random rng, double shape)
        {
            if (shape < 1.0)
                return GammaSample(rng, shape + 1.0) * Math.Pow(rng.NextDouble(), 1.0 / shape);
            double d = shape - 1.0 / 3.0, c = 1.0 / Math.Sqrt(9.0 * d);
            while (true)
            {
                double x, v;
                do { x = NextGaussian(rng); v = 1.0 + c * x; } while (v <= 0);
                v = v * v * v;
                double u = rng.NextDouble();
                if (u < 1.0 - 0.0331 * (x * x) * (x * x)) return d * v;
                if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v))) return d * v;
            }
        }

        private static double NextGaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private static string GenReg(Random rng)
        {
            const string L = "ABCDEFGHJKLMNPRSTVWXYZ";
            return $"{L[rng.Next(L.Length)]}{L[rng.Next(L.Length)]}" +
                   $"{rng.Next(100, 999)}" +
                   $"{L[rng.Next(L.Length)]}{rng.Next(10, 99)}";
        }

        private static readonly string[] Locations =
        {
            "Ashford Creek", "Dunmore Hill", "Crestwick", "Barlow Falls",
            "Tyndall Cross", "Greystone Bay", "Portwick", "Aldenmoor",
            "Fenwick Hollow", "Clarendon Rise", "Saltbury", "Wexmoor",
            "Hadleigh Point", "Thorngate", "Ivybridge End", "Coldwater Bluff",
            "Elmshire", "Brackenford", "Southmere", "Galloway Reach"
        };
    }
}