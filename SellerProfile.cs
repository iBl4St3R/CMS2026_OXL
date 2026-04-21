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

        // ── Nickname pools ────────────────────────────────────────────────────
        private static readonly string[] Prefixes =
{
    // Oryginalne i podobne (Ogólny vibe)
    "Fast", "Rusty", "Dirty", "Lucky", "Slick", "Grease", "Mad", "Iron", "Steel",
    "Quick", "Shifty", "Dodgy", "Honest", "Shady", "Turbo", "Nitro", "Dusty",
    "Banged", "Smooth", "Bent", "Crooked", "Bargain", "Flash", "Sly", "Old",
    "Speedy", "Cranky", "Leaky", "Busted", "Solid",

    // Stan techniczny i wizualny
    "Mint", "Pristine", "Wrecked", "Totaled", "Smashed", "Dented", "Scratched",
    "Polished", "Gleaming", "Filthy", "Clean", "Muddy", "Oily", "Greasy",
    "Squeaky", "Rattling", "Knocking", "Ticking", "Purring", "Roaring", "Smoking",
    "Sparking", "Burned", "Fried", "Melted", "Frozen", "Broken", "Fixed",
    "Repaired", "Scrapped", "Crusty", "Trashy", "Messy", "Classy",

    // Cechy charakteru i biznesowe
    "Cheap", "Fair", "Swindling", "Trusty", "Reliable", "Sketchy", "Sneaky",
    "Clever", "Smart", "Crazy", "Wild", "Psycho", "Happy", "Angry", "Grumpy",
    "Smug", "Proud", "Lazy", "Busy", "Tired", "Generous", "Greedy", "Ruthless",
    "Savage", "Tough", "Premium", "Discount", "Elite", "Budget", "Luxury",

    // Styl i Tuning
    "Boosted", "Tuned", "Stanced", "Slammed", "Lifted", "Custom", "Stock",
    "Factory", "Revved", "Geared", "Driven", "Parked", "Drifting", "Racing",
    "Cruising", "Chopped", "Channeled", "Shaved", "Stripped", "Bare", "Heavy",
    "Light", "Aero", "Forged", "Billet",

    // Kolory, Materiały i Klimat
    "Red", "Black", "Blue", "Silver", "Gold", "Chrome", "Aluminum", "Carbon",
    "Oxidized", "Copper", "Brass", "Uptown", "Downtown", "Backyard", "Street",
    "Alley", "Junkyard", "Highway", "City", "Country", "Desert", "Midnight",
    "Neon", "Ghost", "Phantom", "Shadow", "Epic", "Legendary", "Cursed", "Blessed"
};

        private static readonly string[] Nouns =
        {
    // Oryginalne i Imiona (Klimat warsztatu/komisu)
    "Dave", "Pete", "Mike", "Joe", "Bob", "Al", "Rex", "Benny", "Frank", "Vince",
    "Tony", "Carl", "Mack", "Hank", "Norm", "Clint", "Ray", "Larry", "Gary",
    "Terry", "Jerry", "Barry", "Harry", "Lenny", "Homer", "Barney", "Arthur",
    "Richard", "Edward", "Charles", "William", "James", "John", "Robert",
    "David", "Joseph", "Daniel", "Matthew", "Anthony", "Mark", "Donald",
    "Steven", "Paul", "Andrew", "Kevin", "Brian", "Jason", "Ryan", "Eric", "Stan",

    // Części Silnikowe i Mechaniczne
    "Wheels", "Wrench", "Spanner", "Piston", "Valve", "Gasket", "Jack", "Rod",
    "Cam", "Crank", "Engine", "Motor", "Exhaust", "Muffler", "Pipe", "Header",
    "Intake", "Filter", "Carb", "Injector", "Sparkplug", "Wire", "Battery",
    "Alternator", "Starter", "Radiator", "Hose", "Pump", "Belt", "Pulley",
    "Gear", "Trans", "Clutch", "Flywheel", "Driveshaft", "Axle", "Diff",
    "Bearing", "Hub", "Rotor", "Caliper", "Pad", "Shoe", "Drum", "Line", "Fluid",
    "Oil", "Lube", "Tire", "Rim",

    // Karoseria, Narzędzia i Akcesoria
    "Hubcap", "Fender", "Bumper", "Hood", "Trunk", "Door", "Roof", "Window",
    "Glass", "Mirror", "Seat", "Steering", "Dash", "Console", "Radio", "Speaker",
    "Amp", "Sub", "Wiring", "Fuse", "Relay", "Switch", "Hammer", "Mallet",
    "Socket", "Ratchet", "Grinder", "Welder", "Torch", "Pliers", "Hoist", "Lift",

    // Typ działalności i Miejsca
    "Motors", "Dealer", "Trader", "Garage", "Auto", "Parts", "Sales", "Market",
    "Emporium", "Shack", "Shed", "Barn", "Lot", "Yard", "Scrapyard", "Junkyard",
    "Salvage", "Wreckers", "Dismantlers", "Customs", "Speedshop", "Tuners",
    "Workshop", "Mechanics", "Automotive", "Vehicle", "Car", "Truck", "Bike",
    "Rides", "Deals", "Bargains", "Steals", "Imports", "Exports", "Classics",
    "Muscle", "Exotics", "JDM", "Euro"
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
            string nick = GenerateNick(rng);

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

        private static string GenerateNick(System.Random rng)
        {
            // ~30% chance: pure word combo  e.g. "IronWheels"
            // ~40% chance: word + number    e.g. "RustyDave88"
            // ~30% chance: word_word        e.g. "SlickPiston99"
            string prefix = Prefixes[rng.Next(Prefixes.Length)];
            string noun = Nouns[rng.Next(Nouns.Length)];

            int style = rng.Next(0, 10);

            if (style < 3)
                return prefix + noun;

            if (style < 7)
            {
                int num = rng.Next(0, 100);
                return prefix + noun + num.ToString();
            }

            // underscore + suffix number
            int suffix = rng.Next(1, 999);
            return prefix + "_" + noun + suffix.ToString();
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