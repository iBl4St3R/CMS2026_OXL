using MelonLoader;

namespace CMS2026_OXL
{
    public class ListingSystem
    {
        private static readonly string[] KnownCarIds =
        {
            "car_placeholder_01",
            "car_placeholder_02",
            "car_placeholder_03",
            "car_placeholder_04",
            "car_placeholder_05",
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
            var id = KnownCarIds[rng.Next(KnownCarIds.Length)];
            var note = SellerNotes[rng.Next(SellerNotes.Length)];
            // FIX CS0104: jawne UnityEngine.Random.Range
            var ttl = UnityEngine.Random.Range(120f, 600f);

            return new CarListing
            {
                Make = "Unknown",
                Model = id,
                Year = rng.Next(1990, 2020),
                Price = rng.Next(1500, 15000),
                SellerNote = note,
                ExpiresAt = _gameTime + ttl,
                InternalId = id,
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
    }
}