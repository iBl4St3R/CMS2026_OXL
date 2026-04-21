// SellerProfile.cs
// Generates and caches seller avatars + nicknames for CarListings.
// Avatar LRU cache: 8 textures in memory.
// Avatar selection history: 30 slots (no repeat within window).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CMS2026_OXL
{
    public class SellerProfile
    {
        // ── Avatar file lists ─────────────────────────────────────────────────
        private readonly string[] _malePaths;
        private readonly string[] _femalePaths;

        // ── Selection history (avoid repeats) ────────────────────────────────
        private const int HistorySize = 30;
        private readonly Queue<string> _recentPaths = new();

        // ── Texture LRU cache ─────────────────────────────────────────────────
        private const int TextureCacheSize = 8;
        private readonly Dictionary<string, Texture2D> _texCache = new();
        private readonly LinkedList<string> _texLru = new();

        // ── Nickname pools ────────────────────────────────────────────────────────
        private static readonly string[] MalePrefixes =
        {
    // Oryginalne i osobowość
    "Honest", "Shady", "Rusty", "Greasy", "Crazy", "Big", "Old", "Fast", "Smiling", "Trusty",
    "Dodgy", "Lucky", "Dirty", "Slick", "Cheap", "Fair", "Crooked", "Bent", "Smug", "Broke",
    "Tired", "Grumpy", "Bald", "Sweaty", "Flashy", "Busted", "Sketchy", "Solid", "Turbo", "Nitro",
    // Handlarze i biznes
    "Deals", "Bargain", "Discount", "Premium", "Elite", "Budget", "Stealy", "Hustling", "Sly", "Cunning",
    "Rich", "Poor", "Desperate", "Greedy", "Sleepy", "Loud", "Silent", "Strict", "Easy", "Hard",
    // Techniczne i stan
    "Smoking", "Leaky", "Knocking", "Rattling", "Ticking", "Burnt", "Fried", "Frozen", "Seized", "Dented",
    "Scratched", "Primer", "Matte", "Glossy", "Polished", "Wrecked", "Totaled", "Salvaged", "Mint", "Pristine",
    // Klimat uliczny/garażowy
    "Street", "Alley", "Backyard", "Junkyard", "Highway", "Drift", "Drag", "Racing", "Cruising", "Low",
    "High", "Heavy", "Light", "Wide", "Narrow", "Short", "Long", "Mad", "Wild", "Savage",
    // Przymiotniki różne
    "Mean", "Kind", "Odd", "Rare", "Classic", "Vintage", "Modern", "Custom", "Stock", "Factory",
    "Manual", "Auto", "Geared", "Boosted", "Tunned", "Stanced", "Slammed", "Lifted", "Beaten", "Banged",
    "Shiny", "Crusty", "Muddy", "Oily", "Filthy", "Clean", "Pure", "Raw", "Rough", "Tough",
    "Quick", "Rapid", "Slow", "Steady", "Cranky", "Wonky", "Shifting", "Drifting", "Burning", "Spinning",
    "Grimy", "Dusty", "Stale", "Fresh", "Golden", "Silver", "Iron", "Steel", "Chrome", "Carbon",
    "Ghost", "Shadow", "Phantom", "Spirit", "Demon", "Devil", "Angel", "Saint", "Sinful", "Holy",
    "Big-Block", "Small-Block", "Twin", "V8", "Rotary", "Flat", "Boxer", "Inline", "Diesel", "Electric"
};

        private static readonly string[] FemalePrefixes =
        {
    // Oryginalne i styl
    "Shiny", "Swift", "Lucky", "Slick", "Clever", "Classy", "Smart", "Sharp", "Quick", "Sassy",
    "Clean", "Mint", "Busy", "Foxy", "Boss", "Iron", "Rusty", "Honest", "Sweet", "Tough",
    "Fair", "Sneaky", "Proud", "Neat", "Flashy", "Pristine", "Turbo", "Smooth", "Aero", "Elite",
    // Charakter i biznes
    "Greedy", "Thrifty", "Spendy", "Rich", "Wealthy", "Broke", "Dashing", "Grand", "Royal", "Noble",
    "Graceful", "Fierce", "Wild", "Calm", "Cool", "Hot", "Firey", "Icy", "Cold", "Warm",
    // Techniczne i wygląd
    "Glossy", "Metallic", "Chrome", "Pearl", "Satin", "Matte", "Carbon", "Alloy", "Forged", "Billet",
    "Sparkly", "Glowing", "Radiant", "Neon", "Electric", "Silent", "Loud", "Fast", "Speedy", "Rapid",
    // Uliczne i profesjonalne
    "Urban", "City", "Metro", "Country", "Track", "Road", "Drift", "Circuit", "Sprint", "Enduro",
    "Custom", "Original", "Pure", "Prime", "Top", "Pro", "Expert", "Master", "Chief", "Queen",
    // Różne i opisowe
    "Bold", "Brave", "Little", "Tiny", "Huge", "Major", "Minor", "First", "Last", "Only",
    "Retro", "Mod", "New", "Aged", "Old", "Young", "Ancient", "Future", "Digital", "Analog",
    "Heavy", "Light", "Slim", "Hard", "Soft", "Blue", "Red", "Black", "White", "Gold",
    "Silver", "Copper", "Brass", "Bronze", "Velvet", "Silk", "Leather", "Denim", "Steel", "Glass",
    "Mighty", "Strong", "Steady", "Swift", "Nimble", "Agile", "Fast", "Active", "Deep", "Bright"
};

        private static readonly string[] SharedNouns =
        {
    // Oryginalne i biznesy
    "Wheels", "Motors", "Garage", "Deals", "Auto", "Wrench", "Imports", "Classics", "Traders", "Sales",
    "Junk", "Yard", "Scrap", "Flips", "Exhaust", "Parts", "Lot", "Steals", "Bargains", "Salvage",
    "Wreckers", "Dismantlers", "Customs", "Rides", "Projects", "Market", "Exports", "JDM", "Euro", "Muscle",
    // Części mechaniczne
    "Pistons", "Valves", "Gaskets", "Rods", "Cams", "Cranks", "Blocks", "Heads", "Turbos", "Blowers",
    "Injectors", "Plugs", "Wires", "Belts", "Hoses", "Filters", "Pumps", "Fans", "Radiators", "Coils",
    "Clutches", "Gears", "Diffs", "Axles", "Shafts", "Joints", "Hubs", "Bearings", "Brakes", "Pads",
    "Rotors", "Calipers", "Struts", "Shocks", "Springs", "Bushings", "Arms", "Links", "Bars", "Racks",
    // Nadwozie i wnętrze
    "Fenders", "Bumpers", "Hoods", "Doors", "Panels", "Trunks", "Roofs", "Windows", "Mirrors", "Lights",
    "Seats", "Wheels", "Tires", "Rims", "Alloys", "Steering", "Dash", "Gauges", "Pedals", "Shifters",
    // Slang i miejsca
    "Barns", "Sheds", "Shacks", "Stalls", "Bays", "Docks", "Hubs", "Points", "Spots", "Zones",
    "Addicts", "Enthusiasts", "Heads", "Nuts", "Freaks", "Geeks", "Kings", "Lords", "Pro", "Masters",
    "Speed", "Power", "Torque", "Boost", "Nitrous", "Fuel", "Oil", "Lube", "Gas", "Diesel",
    "Rods", "Sleds", "Beaters", "Sleepers", "Gassers", "Tuners", "Racers", "Drivers", "Pilots", "Owners",
    "Dealers", "Brokers", "Agents", "Scouts", "Finders", "Keepers", "Sellers", "Buyers", "Traders", "Swappers"
};

        private static readonly string[] MaleNouns =
        {
    // Oryginalne i klasyki
    "Dave", "Pete", "Mike", "Hank", "Rex", "Tony", "Vince", "Frank", "Boris", "Jimmy",
    "Mick", "Vlad", "Chuck", "Ray", "Gus", "Bob", "Al", "Norm", "Clint", "Larry",
    "Gary", "Terry", "Jerry", "Barry", "Harry", "Stan", "Carl", "Mack", "Benny", "Joe",
    // Nowe imiona
    "Arnie", "Bernie", "Charlie", "Danny", "Eddie", "Freddie", "Georgie", "Hughie", "Ivan", "Jake",
    "Kenny", "Lenny", "Monty", "Nate", "Ollie", "Phil", "Quint", "Rick", "Saul", "Tim",
    "Uri", "Victor", "Walt", "Xavier", "Yuri", "Zack", "Arthur", "Bill", "Don", "Earl",
    "Felix", "Gabe", "Herb", "Igor", "Jack", "Karl", "Leo", "Max", "Ned", "Oscar",
    "Paul", "Ralph", "Steve", "Tom", "Uly", "Vern", "Will", "Zane", "Bruce", "Duke",
    "Earl", "Floyd", "Grant", "Heath", "Ike", "Jed", "Kirk", "Lance", "Mitch", "Nash",
    "Otto", "Pierce", "Reid", "Seth", "Tate", "Vance", "Wade", "Ace", "Buck", "Cash",
    "Dash", "Edge", "Flint", "Gunner", "Hunter", "Iron", "Jax", "Kane", "Link", "Maverick",
    "Rocco", "Sledge", "Tank", "Wolf", "Axel", "Blade", "Cutter", "Diesel", "Gear", "Hammer",
    "Rusty", "Spike", "Turbo", "Zod", "Biff", "Buzz", "Chip", " Kip", "Skip", "Ziggy"
};

        private static readonly string[] FemaleNouns =
        {
    // Oryginalne i klasyki
    "Lisa", "Kay", "Dee", "Rosa", "Vera", "Roxy", "Sue", "Mia", "Jess", "Rita",
    "Chloe", "Angie", "Nina", "Lucy", "Eva", "Gina", "Tina", "Tara", "Sara", "Cara",
    "Mara", "Lara", "Donna", "Brenda", "Linda", "Meg", "Pam", "Kim", "Jen", "Beth",
    // Nowe imiona
    "Alice", "Belle", "Cathy", "Dora", "Elsa", "Faye", "Gwen", "Hope", "Iris", "June",
    "Kate", "Lana", "Maya", "Nora", "Olive", "Pearl", "Quinn", "Rose", "Skye", "Tess",
    "Uma", "Vi", "Willow", "Xena", "Yana", "Zoe", "Amy", "Bonnie", "Cleo", "Daisy",
    "Ember", "Flora", "Gia", "Hazel", "Ivy", "Jade", "Kiki", "Luna", "Misty", "Nova",
    "Opal", "Piper", "Ruby", "Sage", "Trudy", "Vada", "Wren", "Zelda", "Amber", "Blair",
    "Crystal", "Dawn", "Eve", "Faith", "Ginger", "Holly", "Joy", "Kelly", "Lily", "Molly",
    "Nancy", "Penny", "Roxie", "Sasha", "Trixie", "Vicky", "Wendy", "Abby", "Becca", "Candy",
    "Dolly", "Ellie", "Gigi", "Heidi", "Izzy", "Jojo", "Lulu", "Mimi", "Nana", "Pippa",
    "Sassy", "Tilly", "Zuzu", "Kat", "Lexi", "Maddy", "Nat", "Ria", "Steff", "Val",
    "Aria", "Brea", "Cora", "Dara", "Eira", "Fara", "Gala", "Hala", "Ila", "Jala"
};

        // ── Singleton-style factory ───────────────────────────────────────────
        public SellerProfile(string modsRoot)
        {
            string maleDir = Path.Combine(modsRoot, "avatars", "male");
            string femaleDir = Path.Combine(modsRoot, "avatars", "female");

            _malePaths = LoadPngList(maleDir);
            _femalePaths = LoadPngList(femaleDir);

            OXLPlugin.Log.Msg(
                $"[SellerProfile] Found {_malePaths.Length} male, " +
                $"{_femalePaths.Length} female avatars.");
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a (texture, nickname) pair for a listing.
        /// Gender is derived from archetype for slight thematic consistency,
        /// but mostly random.
        /// Texture may be null if files are missing.
        /// </summary>
        public (Texture2D tex, string nick, string path) Generate(CarListing listing, System.Random rng)
        {
            bool preferMale = listing.Archetype switch
            {
                SellerArchetype.Dealer => rng.Next(0, 3) != 0,
                SellerArchetype.Scammer => rng.Next(0, 3) != 0,
                _ => rng.Next(0, 2) == 0,
            };

            string path = PickPath(preferMale, rng);
            Texture2D tex = path != null ? GetOrLoad(path) : null;
            string nick = GenerateNick(rng, preferMale);

            return (tex, nick, path ?? "");
        }

        /// <summary>
        /// Evict textures beyond the LRU limit.
        /// Call after closing the detail overlay to free memory.
        /// </summary>
        public void Evict()
        {
            while (_texCache.Count > TextureCacheSize && _texLru.Count > 0)
            {
                string oldest = _texLru.Last.Value;
                _texLru.RemoveLast();
                if (_texCache.TryGetValue(oldest, out var t))
                {
                    // Do NOT destroy — Unity marked DontUnloadUnusedAsset.
                    // Just drop reference; GC handles it when hideFlags reset.
                    _texCache.Remove(oldest);
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private string PickPath(bool preferMale, System.Random rng)
        {
            string[] pool = preferMale ? _malePaths : _femalePaths;
            string[] other = preferMale ? _femalePaths : _malePaths;

            // Merge pools if chosen one is empty
            if (pool.Length == 0) pool = other;
            if (pool.Length == 0) return null;

            // Build candidate list excluding recent history
            var candidates = pool.Where(p => !_recentPaths.Contains(p)).ToArray();

            // If history has used everything, fall back to full pool
            if (candidates.Length == 0) candidates = pool;

            string chosen = candidates[rng.Next(candidates.Length)];

            // Update history
            _recentPaths.Enqueue(chosen);
            while (_recentPaths.Count > HistorySize)
                _recentPaths.Dequeue();

            return chosen;
        }

        /// <summary>Returns texture for an already-known path (e.g. loaded from save).</summary>
        public Texture2D GetCachedOrLoad(string path) => string.IsNullOrEmpty(path) ? null : GetOrLoad(path);

        private Texture2D GetOrLoad(string path)
        {
            if (_texCache.TryGetValue(path, out var cached))
            {
                // Promote to front of LRU
                _texLru.Remove(path);
                _texLru.AddFirst(path);
                return cached;
            }

            var tex = LoadTexture(path);
            if (tex == null) return null;

            _texCache[path] = tex;
            _texLru.AddFirst(path);
            Evict();
            return tex;
        }

        private static Texture2D LoadTexture(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                byte[] bytes = File.ReadAllBytes(path);
                bool isPng = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
                var fmt = isPng ? TextureFormat.RGBA32 : TextureFormat.RGB24;
                var tex = new Texture2D(2, 2, fmt, false);

                var il2b = new Il2CppInterop.Runtime.InteropTypes
                               .Arrays.Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                var icType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName == "UnityEngine.ImageConversion");

                var loadImg = icType?.GetMethods()
                    .FirstOrDefault(m => m.Name == "LoadImage"
                                      && m.GetParameters().Length == 2);

                if (loadImg == null) return null;

                bool ok = (bool)loadImg.Invoke(null, new object[] { tex, il2b });
                if (ok)
                    tex.hideFlags = HideFlags.DontUnloadUnusedAsset; // ← GC guard
                return ok ? tex : null;
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg(
                    $"[SellerProfile] Texture load error ({Path.GetFileName(path)}): {ex.Message}");
                return null;
            }
        }

        private string GenerateNick(System.Random rng, bool preferMale)
        {
            string[] prefixes = preferMale ? MalePrefixes : FemalePrefixes;
            string prefix = prefixes[rng.Next(prefixes.Length)];

            // 50% szans: własna pula płciowa, 50%: wspólna
            string[] nounPool = rng.Next(0, 2) == 0
                ? (preferMale ? MaleNouns : FemaleNouns)
                : SharedNouns;
            string noun = nounPool[rng.Next(nounPool.Length)];

            int style = rng.Next(0, 10);

            if (style < 3)
                return prefix + noun;

            if (style < 7)
                return prefix + noun + rng.Next(0, 100).ToString();

            return prefix + "_" + noun + rng.Next(1, 999).ToString();
        }

        private static string[] LoadPngList(string dir)
        {
            if (!Directory.Exists(dir)) return Array.Empty<string>();
            return Directory.GetFiles(dir, "*.png")
                            .OrderBy(f => f)
                            .ToArray();
        }
    }
}