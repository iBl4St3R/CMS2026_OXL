using MelonLoader;
using UnityEngine;

namespace CMS2026_OXL
{
    public class ListingSystem
    {
        // ── Car definitions ───────────────────────────────────────────────────
        private class CarDef
        {
            public string Make, Model, ImageFolder, InternalId;
            public int MinYear, MaxYear, MinPrice, MaxPrice;
        }

        private static readonly CarDef[] CarDefs =
       {
            new CarDef { Make = "DNB",      Model = "Censor",           ImageFolder = "DNB Censor",
                         InternalId = "car_dnb_censor",
                         MinYear = 2000, MaxYear = 2015, MinPrice = 3000,  MaxPrice = 9000  },
            new CarDef { Make = "Katagiri", Model = "Tamago BP",         ImageFolder = "Katagiri Tamago BP",
                         InternalId = "car_katagiri_tamago",
                         MinYear = 1995, MaxYear = 2010, MinPrice = 2000,  MaxPrice = 7000  },
            new CarDef { Make = "Luxor",    Model = "Streamliner Mk3",   ImageFolder = "Luxor Streamliner Mk3",
                         InternalId = "car_luxor_streamliner",
                         MinYear = 2005, MaxYear = 2020, MinPrice = 6000,  MaxPrice = 14000 },
            new CarDef { Make = "Mayen",    Model = "M5",                ImageFolder = "Mayen M5",
                         InternalId = "car_mayen_m5",
                         MinYear = 2010, MaxYear = 2022, MinPrice = 8000,  MaxPrice = 18000 },
            new CarDef { Make = "Salem",    Model = "Aries MK3",         ImageFolder = "Salem Aries MK3",
                         InternalId = "car_salem_aries",
                         MinYear = 1998, MaxYear = 2012, MinPrice = 2500,  MaxPrice = 8500  },
        };

        private static readonly string[] SellerNotes =
        {
            "One owner. Never had any issues. Selling due to relocation.",
            "Runs great! Only needs a little TLC. Priced to sell.",
            "My grandma drove it to church on Sundays. Very gentle use.",
            "Small knocking sound but mechanic said it's nothing serious.",
            "Just had full service done. Everything works. Trust me.",
            "Selling because I bought a new one. Nothing wrong with it.",
        };

        public List<CarListing> ActiveListings { get; private set; } = new();

        private float _gameTime = 0f;
        public float GameTime => _gameTime;

        public void Tick(float deltaTime)
        {
            _gameTime += deltaTime;

            ActiveListings.RemoveAll(l => l.ExpiresAt <= _gameTime);

            while (ActiveListings.Count < 4)
                ActiveListings.Add(GenerateListing());

            // FIX CS0104: jawne UnityEngine.Random.value
            if (ActiveListings.Count < 10 && UnityEngine.Random.value < 0.002f)
                ActiveListings.Add(GenerateListing());
        }

        private CarListing GenerateListing()
        {
            var rng = new System.Random();
            var def = CarDefs[rng.Next(CarDefs.Length)];
            var note = SellerNotes[rng.Next(SellerNotes.Length)];
            var ttl = UnityEngine.Random.Range(120f, 600f);
            int year = rng.Next(def.MinYear, def.MaxYear + 1);

            // ── Kondycja → cena ───────────────────────────────────────────────
            // Losujemy kondycję (0.05–0.95), potem liczymy cenę z niej.
            // Rozkład: dużo aut w złym/średnim stanie, mało prawie nowych.
            float t = (float)BetaSample(rng, alpha: 1.8, beta: 3.5); // skośny w lewo
            float condition = Mathf.Clamp(t, 0.05f, 0.95f);

            // cena = lerp(MinPrice, MaxPrice, condition) + szum ±10%
            float basePricef = Mathf.Lerp(def.MinPrice, def.MaxPrice, condition);
            float noise = 1f + (float)(rng.NextDouble() * 0.20 - 0.10);
            int price = Mathf.RoundToInt(basePricef * noise / 50f) * 50; // zaokrągl do 50
            price = Mathf.Clamp(price, def.MinPrice, def.MaxPrice);

            // Przebieg odwrotnie proporcjonalny do kondycji
            int mileage = Mathf.RoundToInt(Mathf.Lerp(180000, 4000, condition)
                          * (1f + (float)(rng.NextDouble() * 0.30 - 0.15)));
            mileage = Mathf.Max(500, mileage);

            return new CarListing
            {
                Registration = GenReg(rng),
                Make = def.Make,
                Model = def.Model,
                ImageFolder = def.ImageFolder,
                Year = year,
                Price = price,
                Condition = condition,
                SellerNote = note,
                ExpiresAt = _gameTime + ttl,
                InternalId = def.InternalId + "_" + rng.Next(1000, 9999),
                Mileage = mileage,
                Location = Locations[rng.Next(Locations.Length)],
                DeliveryHours = rng.Next(1, 37),
            };
        }

        // Beta distribution sample — daje ładny skośny rozkład kondycji
        private static double BetaSample(System.Random rng, double alpha, double beta)
        {
            double x = GammaSample(rng, alpha);
            double y = GammaSample(rng, beta);
            return x / (x + y);
        }

        private static double GammaSample(System.Random rng, double shape)
        {
            // Marsaglia–Tsang
            if (shape < 1.0) return GammaSample(rng, shape + 1.0) * Math.Pow(rng.NextDouble(), 1.0 / shape);
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


        private static double NextGaussian(System.Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }



        public bool TryPurchase(CarListing listing, Action<string> spawnCar, Action<int> deductMoney)
        {
            if (!ActiveListings.Contains(listing)) return false;

            spawnCar(listing.InternalId);
            deductMoney(listing.Price);
            ActiveListings.Remove(listing);

            OXLPlugin.Log.Msg($"[OXL] Purchased: {listing.Make} {listing.Model} for ${listing.Price}");
            return true;
        }

        private static string GenReg(System.Random rng)
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