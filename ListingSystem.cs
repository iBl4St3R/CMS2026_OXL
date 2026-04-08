using MelonLoader;

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
            int price = rng.Next(def.MinPrice, def.MaxPrice + 1);

            return new CarListing
            {
                Make = def.Make,
                Model = def.Model,
                ImageFolder = def.ImageFolder,
                Year = year,
                Registration = GenReg(rng)//last
                Price = price,
                SellerNote = note,
                ExpiresAt = _gameTime + ttl,
                InternalId = def.InternalId + "_" + rng.Next(1000, 9999),
                Mileage = rng.Next(4000, 195000),
                Location = Locations[rng.Next(Locations.Length)],
                DeliveryHours = rng.Next(1, 37),
            };
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


        private static readonly string[] Locations =
{
    "Ashford Creek", "Dunmore Hill", "Crestwick", "Barlow Falls",
    "Tyndall Cross", "Greystone Bay", "Portwick", "Aldenmoor",
    "Fenwick Hollow", "Clarendon Rise", "Saltbury", "Wexmoor",
    "Hadleigh Point", "Thorngate", "Ivybridge End", "Coldwater Bluff",
    "Elmshire", "Brackenford", "Southmere", "Galloway Reach"
};


        private static string GenReg(System.Random rng)
        {
            const string L = "ABCDEFGHJKLMNPRSTVWXYZ";
            return $"{L[rng.Next(L.Length)]}{L[rng.Next(L.Length)]}" +
                   $"{rng.Next(100, 999)}" +
                   $"{L[rng.Next(L.Length)]}{rng.Next(10, 99)}";
        }



    }
}