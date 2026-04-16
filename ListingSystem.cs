// ListingSystem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CMS2026_OXL
{
    public class ListingSystem
    {
        private readonly CarPhotoLoader _photoLoader;

        public ListingSystem(CarPhotoLoader photoLoader)
        {
            _photoLoader = photoLoader;
        }

        private class CarDef
        {
            public string Make, Model, ImageFolder, InternalId;
            public int MinYear, MaxYear, MinPrice, MaxPrice;
        }

        private static readonly CarDef[] CarDefs =
        {
            new CarDef { Make = "DNB",      Model = "Censor",         ImageFolder = "DNB Censor",          InternalId = "car_dnb_censor",          MinYear = 1991, MaxYear = 1992, MinPrice = 3500,  MaxPrice = 12000 },
            new CarDef { Make = "Katagiri", Model = "Tamago BP",       ImageFolder = "Katagiri Tamago BP",  InternalId = "car_katagiri_tamago",      MinYear = 2000, MaxYear = 2005, MinPrice = 3500,  MaxPrice = 12000 },
            new CarDef { Make = "Luxor",    Model = "Streamliner Mk3", ImageFolder = "Luxor Streamliner Mk3", InternalId = "car_luxor_streamliner",  MinYear = 1992, MaxYear = 1996, MinPrice = 4000,  MaxPrice = 14000 },
            new CarDef { Make = "Mayen",    Model = "M5",              ImageFolder = "Mayen M5",            InternalId = "car_mayen_m5",             MinYear = 2008, MaxYear = 2015, MinPrice = 5000,  MaxPrice = 18000 },
            new CarDef { Make = "Salem",    Model = "Aries MK3",       ImageFolder = "Salem Aries MK3",     InternalId = "car_salem_aries",          MinYear = 2003, MaxYear = 2008, MinPrice = 4500,  MaxPrice = 15000 },
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

        public void ForceGenerate()
        {
            ActiveListings.Add(GenerateListing());
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GENEROWANIE LISTINGU
        // ══════════════════════════════════════════════════════════════════════

        private CarListing GenerateListing()
        {
            var rng = new Random();
            var def = CarDefs[rng.Next(CarDefs.Length)];
            int year = rng.Next(def.MinYear, def.MaxYear + 1);

            string baseId = def.InternalId;
            string[] pool = ActiveColors.ContainsKey(baseId) ? ActiveColors[baseId] : new[] { "white" };
            string color = pool[rng.Next(pool.Length)];
            int colorIndex = 0;
            if (AllColors.TryGetValue(def.InternalId, out var allColorNames))
                colorIndex = Array.IndexOf(allColorNames, color);

            var archetype = RollArchetype(rng);
            int level = RollLevel(archetype, rng);

            // ── ApparentCondition — co gracz widzi ────────────────────────────
            float apparent = RollApparent(archetype, level, rng);

            // ── ActualCondition — prawdziwy stan mechaniki ────────────────────
            float honesty = RollHonesty(archetype, level, rng);
            float noise = (float)(rng.NextDouble() * 0.06 - 0.03);
            float actual = Mathf.Clamp(apparent * honesty + noise, 0.02f, 0.97f);

            // ── Usterki ────────────────────────────────────────────────────────
            FaultFlags faults = RollFaults(archetype, level, actual, rng);

            // ── Kondycja karoserii — Dealer naprawia wygląd niezależnie od mechaniki ──
            float bodyCondition = RollBodyCondition(archetype, level, actual, rng);

            // ── Cena ───────────────────────────────────────────────────────────
            int price = RollPrice(def, archetype, level, apparent, actual, year, faults, rng);
            int fairValue = CalcFairValue(def, actual, year);

            // ── TTL — czas życia aukcji ────────────────────────────────────────
            float ttl = RollTTL(archetype, level, rng);

            // ── Rating sprzedawcy ──────────────────────────────────────────────
            int rating = RollSellerRating(archetype, level, rng);

            // ── Przebieg ───────────────────────────────────────────────────────
            int mileage = Mathf.RoundToInt(Mathf.Lerp(180000, 4000, actual) * (1f + (float)(rng.NextDouble() * 0.30 - 0.15)));
            mileage = Mathf.Max(500, mileage);

            // ── Nota sprzedawcy ────────────────────────────────────────────────
            string note = SelectNote(archetype, level, faults, rng);

            var listing = new CarListing
            {
                Registration = GenReg(rng),
                Make = def.Make,
                Model = def.Model,
                ImageFolder = def.ImageFolder,
                Year = year,
                Price = price,
                FairValue = fairValue,
                ApparentCondition = apparent,
                BodyCondition = bodyCondition,
                ActualCondition = actual,
                Archetype = archetype,
                ArchetypeLevel = level,
                Faults = faults,
                SellerNote = note,
                ExpiresAt = _gameTime + ttl,
                InternalId = def.InternalId + "_" + rng.Next(1000, 9999),
                Mileage = mileage,
                Location = Locations[rng.Next(Locations.Length)],
                DeliveryHours = rng.Next(1, 37),
                SellerRating = rating,
                Color = color,
                ColorIndex = colorIndex,
            };

            listing.PhotoFiles = _photoLoader?.SelectPhotoFiles(listing) ?? new List<string>();

            OXLPlugin.Log.Msg($"[OXL:GEN] {def.Make} {def.Model} {year} | Arch={archetype} L{level} | Rating={rating}★ | Apparent={apparent:P0} Actual={actual:P0} Body={bodyCondition:P0} | Price=${price:N0} Fair=${fairValue:N0} ({(fairValue > 0 ? (float)price / fairValue : 0):F2}x) | Faults={faults} | Color={color} | TTL={ttl:F0}s");

            return listing;
        }

        public static Dictionary<string, (string carId, string[] colors)> GetColorRegistry()
        {
            var result = new Dictionary<string, (string, string[])>();
            foreach (var def in CarDefs)
            {
                if (AllColors.TryGetValue(def.InternalId, out var colors))
                    result[def.ImageFolder] = (def.InternalId, colors);
            }
            return result;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ARCHETYPE + LEVEL ROLLS
        // ══════════════════════════════════════════════════════════════════════

        private static SellerArchetype RollArchetype(Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.20) return SellerArchetype.Honest;
            if (r < 0.55) return SellerArchetype.Neglected;
            if (r < 0.85) return SellerArchetype.Dealer;
            return SellerArchetype.Wrecker;
        }

        // Poziomy: 1=novice/casual/backyard/amateur, 2=experienced/busy/pro/intermediate, 3=veteran/hoarder/criminal/expert
        // Wyższy poziom = bardziej charakterystyczny dla archetypu (bardziej wiarygodny uczciwy, głębszy scam dealera)
        private static int RollLevel(SellerArchetype arch, Random rng)
        {
            double r = rng.NextDouble();
            return arch switch
            {
                // Honest: większość to doświadczeni, weterani rzadcy (wysoka reputacja = rzadkość)
                SellerArchetype.Honest => r < 0.40 ? 1 : r < 0.75 ? 2 : 3,
                // Neglected: rozkład równomierny — każdy typ się trafia
                SellerArchetype.Neglected => r < 0.35 ? 1 : r < 0.70 ? 2 : 3,
                // Dealer: pro to norma, criminal rzadki ale niebezpieczny
                SellerArchetype.Dealer => r < 0.30 ? 1 : r < 0.70 ? 2 : 3,
                // Wrecker: amatorzy części, eksperci rzadcy ale najgroźniejsi
                SellerArchetype.Wrecker => r < 0.45 ? 1 : r < 0.75 ? 2 : 3,
                _ => 1,
            };
        }

        // ── ApparentCondition — co gracz widzi w ogłoszeniu ──────────────────
        // Dealer i Wrecker kłamią na poziomie apparent (zdjęcia, opis)
        // Honest i Neglected — apparent ≈ actual (bez manipulacji)

        private static float RollApparent(SellerArchetype arch, int level, Random rng)
        {
            return arch switch
            {
                SellerArchetype.Honest => level switch
                {
                    1 => Mathf.Clamp((float)BetaSample(rng, 1.5, 3.5), 0.08f, 0.80f),  // nowicjusz — często gorszy stan
                    2 => Mathf.Clamp((float)BetaSample(rng, 2.0, 3.0), 0.20f, 0.90f),  // doświadczony — średni/dobry
                    _ => Mathf.Clamp((float)BetaSample(rng, 2.5, 2.0), 0.40f, 0.95f),  // weteran — dobry stan, zasłużony
                },
                SellerArchetype.Neglected => level switch
                {
                    1 => Mathf.Clamp((float)BetaSample(rng, 1.5, 3.0), 0.10f, 0.75f),  // casual — losowo
                    2 => Mathf.Clamp((float)BetaSample(rng, 1.2, 3.5), 0.08f, 0.70f),  // busy — trochę gorzej, nie ma czasu
                    _ => Mathf.Clamp((float)BetaSample(rng, 1.0, 4.0), 0.05f, 0.65f),  // hoarder — zaniedbane, sprzedaje byle co
                },
                SellerArchetype.Dealer => level switch
                {
                    1 => Mathf.Clamp((float)BetaSample(rng, 3.0, 1.5), 0.50f, 0.88f),  // backyard — wypolerowane, widać że amator
                    2 => Mathf.Clamp((float)BetaSample(rng, 3.5, 1.2), 0.65f, 0.95f),  // pro — prawie idealne, bardzo przekonujące
                    _ => Mathf.Clamp((float)BetaSample(rng, 5.0, 1.0), 0.80f, 1.00f),  // criminal — perfekcyjne, nie do odróżnienia
                },
                SellerArchetype.Wrecker => level switch
                {
                    1 => Mathf.Clamp((float)BetaSample(rng, 1.5, 2.5), 0.15f, 0.65f),  // amateur — opis pełen błędów, łatwy do wykrycia
                    2 => Mathf.Clamp((float)BetaSample(rng, 2.5, 2.0), 0.35f, 0.80f),  // intermediate — lepszy storytelling
                    _ => Mathf.Clamp((float)BetaSample(rng, 4.0, 1.5), 0.65f, 0.98f),  // expert — auto wygląda dobrze, w środku katastrofa
                },
                _ => 0.5f,
            };
        }

        // ── Honesty — stosunek actual do apparent ─────────────────────────────
        // 1.0 = pełna uczciwość, 0.1 = kłamstwo na 10% apparent

        private static float RollHonesty(SellerArchetype arch, int level, Random rng)
        {
            return arch switch
            {
                SellerArchetype.Honest => level switch
                {
                    1 => (float)(0.88 + rng.NextDouble() * 0.10),  // 0.88–0.98 — uczciwy ale może nie wiedzieć wszystkiego
                    2 => (float)(0.92 + rng.NextDouble() * 0.07),  // 0.92–0.99
                    _ => (float)(0.96 + rng.NextDouble() * 0.04),  // 0.96–1.00 — weteran zna auto w 100%
                },
                SellerArchetype.Neglected => level switch
                {
                    1 => (float)(0.75 + rng.NextDouble() * 0.20),  // 0.75–0.95 — nie kłamie, po prostu nie wie
                    2 => (float)(0.65 + rng.NextDouble() * 0.25),  // 0.65–0.90 — nie sprawdzał od lat
                    _ => (float)(0.50 + rng.NextDouble() * 0.30),  // 0.50–0.80 — hoarder zgaduje stan
                },
                SellerArchetype.Dealer => level switch
                {
                    1 => (float)(0.35 + rng.NextDouble() * 0.20),  // 0.35–0.55 — backyard: polakierował i tyle
                    2 => (float)(0.20 + rng.NextDouble() * 0.20),  // 0.20–0.40 — pro: głęboki scam
                    _ => (float)(0.05 + rng.NextDouble() * 0.15),  // 0.05–0.20 — criminal: auto to atrapa
                },
                SellerArchetype.Wrecker => level switch
                {
                    1 => (float)(0.25 + rng.NextDouble() * 0.25),  // 0.25–0.50 — amator nie umie zbudować dobrego kłamstwa
                    2 => (float)(0.12 + rng.NextDouble() * 0.18),  // 0.12–0.30 — intermediate: więcej kłamstwa
                    _ => (float)(0.03 + rng.NextDouble() * 0.10),  // 0.03–0.13 — expert: totalna fikcja
                },
                _ => 0.8f,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BODY CONDITION — wygląd fizyczny karoserii/wnętrza po spawnowaniu
        // ══════════════════════════════════════════════════════════════════════

        private static float RollBodyCondition(SellerArchetype arch, int level, float actual, Random rng)
        {
            return arch switch
            {
                // Honest: karoseria odzwierciedla stan faktyczny — bez manipulacji
                SellerArchetype.Honest => Mathf.Clamp(actual * (float)(0.92 + rng.NextDouble() * 0.12), 0.05f, 0.95f),

                // Neglected: zaniedbana karoseria — ciut gorsza niż actual
                SellerArchetype.Neglected => level switch
                {
                    1 => Mathf.Clamp(actual * (float)(0.78 + rng.NextDouble() * 0.22), 0.03f, 0.85f),
                    2 => Mathf.Clamp(actual * (float)(0.68 + rng.NextDouble() * 0.25), 0.02f, 0.78f),
                    _ => Mathf.Clamp(actual * (float)(0.55 + rng.NextDouble() * 0.30), 0.02f, 0.72f),
                },

                // Dealer: karoseria naprawiona niezależnie od mechaniki — to jest ich "produkt"
                SellerArchetype.Dealer => level switch
                {
                    1 => (float)(0.60 + rng.NextDouble() * 0.15),  // 0.60–0.75 — Backyard: umył, podmalował
                    2 => (float)(0.75 + rng.NextDouble() * 0.15),  // 0.75–0.90 — Pro: profesjonalny detailing
                    _ => (float)(0.90 + rng.NextDouble() * 0.09),  // 0.90–0.99 — Criminal: perfekcyjne, nie do odróżnienia
                },

                // Wrecker: próbuje ukryć — L3 wygląda OK, L1 łatwy do wykrycia
                SellerArchetype.Wrecker => level switch
                {
                    1 => Mathf.Clamp(actual * (float)(0.90 + rng.NextDouble() * 0.20), 0.05f, 0.75f),
                    2 => (float)(0.40 + rng.NextDouble() * 0.25),  // 0.40–0.65 — średni wygląd, trudniejszy do wykrycia
                    _ => (float)(0.62 + rng.NextDouble() * 0.25),  // 0.62–0.87 — Expert: posprzątany, odmalowany
                },

                _ => actual,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PRICE
        // ══════════════════════════════════════════════════════════════════════

        private static int RollPrice(CarDef def, SellerArchetype arch, int level, float apparent, float actual, int year, FaultFlags faults, Random rng)
        {
            // Deprecjacja roczna — starsze auta wychodzą taniej z założenia
            float ageFactor = YearFactor(year);

            // Baza: Dealer/Wrecker kłamią więc ich baza to apparent (widoczna jakość)
            // Honest/Neglected wyceniają bliżej tego co faktycznie mają (actual)
            float baseCondition = (arch == SellerArchetype.Dealer || arch == SellerArchetype.Wrecker) ? apparent : actual;
            float basePricef = Mathf.Lerp(def.MinPrice, def.MaxPrice, baseCondition) * ageFactor;

            // Mnożnik per archetype+level
            float mult = (arch, level) switch
            {
                // Honest: nowicjusz wycenia za nisko (nie zna rynku), weteran lekko premium za zaufanie
                (SellerArchetype.Honest, 1) => (float)(0.78 + rng.NextDouble() * 0.12),  // 0.78–0.90 — za tani, nie wie co ma
                (SellerArchetype.Honest, 2) => (float)(0.93 + rng.NextDouble() * 0.13),  // 0.93–1.06 — rynkowa
                (SellerArchetype.Honest, 3) => (float)(1.05 + rng.NextDouble() * 0.11),  // 1.05–1.16 — premium za reputację

                // Neglected: Casual losowo, Busy chce się pozbyć (nisko), Hoarder skrajności
                (SellerArchetype.Neglected, 1) => (float)(0.72 + rng.NextDouble() * 0.28),  // 0.72–1.00 — losowo, nie dba
                (SellerArchetype.Neglected, 2) => (float)(0.58 + rng.NextDouble() * 0.28),  // 0.58–0.86 — Busy: chce zejść szybko
                (SellerArchetype.Neglected, 3) => (float)(0.45 + rng.NextDouble() * 0.75),  // 0.45–1.20 — Hoarder: albo za darmo albo absurd

                // Dealer: wycenia znacznie wyżej — płacisz za scenografię nie za auto
                (SellerArchetype.Dealer, 1) => (float)(1.35 + rng.NextDouble() * 0.25),  // 1.35–1.60 — Backyard: podstawowa nadwyżka
                (SellerArchetype.Dealer, 2) => (float)(1.55 + rng.NextDouble() * 0.30),  // 1.55–1.85 — Pro: znaczna nadwyżka
                (SellerArchetype.Dealer, 3) => (float)(1.85 + rng.NextDouble() * 0.35),  // 1.85–2.20 — Criminal: premium za idealnie ukryty scam

                // Wrecker: Amateur tanio żeby złowić, Expert wygląda jak dobry Dealer
                (SellerArchetype.Wrecker, 1) => (float)(0.65 + rng.NextDouble() * 0.25),  // 0.65–0.90 — tania przynęta
                (SellerArchetype.Wrecker, 2) => (float)(0.88 + rng.NextDouble() * 0.30),  // 0.88–1.18 — rynkowa, trudniej wykryć
                (SellerArchetype.Wrecker, 3) => (float)(1.08 + rng.NextDouble() * 0.40),  // 1.08–1.48 — Expert: premium, nie do odróżnienia od Dealera

                _ => 1.0f,
            };

            float noise = 1f + (float)(rng.NextDouble() * 0.06 - 0.03);
            int price = Mathf.RoundToInt(basePricef * mult * noise * OXLSettings.PriceMultiplier / 50f) * 50;

            // Min: 25% of MinPrice — pozwala na naprawdę tanie auta od Neglected/Wrecker
            int scaledMin = Math.Max(300, Mathf.RoundToInt(def.MinPrice * OXLSettings.PriceMultiplier * 0.25f));
            // Max: 2.2x MaxPrice — Dealer L3 wycenia sporo ponad normalny rynek
            int scaledMax = Mathf.RoundToInt(def.MaxPrice * OXLSettings.PriceMultiplier * 2.2f);
            price = Mathf.Clamp(price, scaledMin, scaledMax);

            // Honest z usterkami: rabat skalowany per liczba usterek — więcej problemów = głębszy rabat
            if (arch == SellerArchetype.Honest && faults != FaultFlags.None)
            {
                int faultCount = CountBits((int)faults);
                float discountPerFault = level switch { 1 => 0.11f, 2 => 0.08f, _ => 0.06f };
                float discount = Mathf.Clamp(1.0f - faultCount * discountPerFault, 0.52f, 0.93f);
                price = Mathf.RoundToInt(price * discount / 50f) * 50;
            }

            return Mathf.Max(price, 300);
        }

        // Uczciwa wartość rynkowa — ile auto NAPRAWDĘ jest warte, niezależnie od archetypu
        private static int CalcFairValue(CarDef def, float actual, int year)
        {
            float val = Mathf.Lerp(def.MinPrice, def.MaxPrice, actual) * YearFactor(year) * OXLSettings.PriceMultiplier;
            return Mathf.Max(300, Mathf.RoundToInt(val / 50f) * 50);
        }

        // ~1.2% deprecjacji rocznie — max 50% wartości bazowej (nawet 40-letnie auto ma swój floor)
        private static float YearFactor(int year) =>
            Mathf.Clamp(1.0f - (2026 - year) * 0.012f, 0.50f, 1.0f);

        private static int CountBits(int v) { int c = 0; while (v != 0) { c += v & 1; v >>= 1; } return c; }

        // ══════════════════════════════════════════════════════════════════════
        //  TTL — czas życia aukcji
        // ══════════════════════════════════════════════════════════════════════

        private static float RollTTL(SellerArchetype arch, int level, Random rng)
        {
            // Neglected sprzedaje szybko (krótkie TTL), Dealer cierpliwy (długie TTL)
            // Wyższy poziom = bardziej skrajne zachowanie
            return (arch, level) switch
            {
                (SellerArchetype.Honest, 1) => UnityEngine.Random.Range(200f, 500f),
                (SellerArchetype.Honest, 2) => UnityEngine.Random.Range(250f, 600f),
                (SellerArchetype.Honest, 3) => UnityEngine.Random.Range(300f, 700f),  // weteran nie śpieszy się

                (SellerArchetype.Neglected, 1) => UnityEngine.Random.Range(90f, 300f),   // casual: sprzedam to szybko
                (SellerArchetype.Neglected, 2) => UnityEngine.Random.Range(60f, 200f),   // busy: nie ma czasu czekać
                (SellerArchetype.Neglected, 3) => UnityEngine.Random.Range(40f, 150f),   // hoarder: wystawia na chwilę, usuwa, wystawia znowu

                (SellerArchetype.Dealer, 1) => UnityEngine.Random.Range(300f, 600f),
                (SellerArchetype.Dealer, 2) => UnityEngine.Random.Range(400f, 700f),
                (SellerArchetype.Dealer, 3) => UnityEngine.Random.Range(500f, 900f),  // criminal: cierpliwy, czeka na ofiarę

                (SellerArchetype.Wrecker, 1) => UnityEngine.Random.Range(120f, 350f),
                (SellerArchetype.Wrecker, 2) => UnityEngine.Random.Range(200f, 500f),
                (SellerArchetype.Wrecker, 3) => UnityEngine.Random.Range(300f, 650f),  // expert: wystawia na długo, wygląda pewnie

                _ => UnityEngine.Random.Range(120f, 600f),
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  RATING
        // ══════════════════════════════════════════════════════════════════════

        private static int RollSellerRating(SellerArchetype arch, int level, Random rng)
        {
            return (arch, level) switch
            {
                (SellerArchetype.Honest, 1) => rng.Next(0, 10) < 5 ? 3 : 4,           // 50% = 3★, 50% = 4★ — nowicjusz, mało opinii
                (SellerArchetype.Honest, 2) => rng.Next(0, 10) < 7 ? 4 : 5,           // 70% = 4★, 30% = 5★
                (SellerArchetype.Honest, 3) => rng.Next(0, 10) < 8 ? 5 : 4,           // 80% = 5★ — weteran z reputacją

                (SellerArchetype.Neglected, 1) => rng.Next(0, 10) < 6 ? 3 : 4,           // 60% = 3★
                (SellerArchetype.Neglected, 2) => rng.Next(0, 10) < 5 ? 2 : 3,           // 50% = 2★ — buyers complained
                (SellerArchetype.Neglected, 3) => rng.Next(0, 10) < 6 ? 2 : 1,           // 60% = 2★, 40% = 1★ — hoarder: niezadowoleni klienci

                (SellerArchetype.Dealer, 1) => rng.Next(0, 10) < 5 ? 3 : 4,           // backyard: ok ale nie imponujący
                (SellerArchetype.Dealer, 2) => rng.Next(0, 10) < 6 ? 4 : 5,           // pro: kupiony / wypracowany rating
                (SellerArchetype.Dealer, 3) => rng.Next(0, 10) < 8 ? 5 : 4,           // criminal: sfałszowany 5★, prawie zawsze

                (SellerArchetype.Wrecker, 1) => rng.Next(0, 10) < 7 ? 1 : 2,           // amator: widać że oszust
                (SellerArchetype.Wrecker, 2) => rng.Next(0, 10) < 5 ? 2 : 3,           // intermediate: trochę lepiej
                (SellerArchetype.Wrecker, 3) => rng.Next(0, 10) < 7 ? 4 : 5,           // expert: sfałszowany rating, wygląda jak Dealer

                _ => 3,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FAULTS
        // ══════════════════════════════════════════════════════════════════════

        private static FaultFlags RollFaults(SellerArchetype arch, int level, float actual, Random rng)
        {
            var f = FaultFlags.None;

            // Bazowe prawdopodobieństwa per archetyp+poziom przy złym stanie (actual < 0.4)
            // Dealer L3 i Wrecker L3 mogą mieć usterki nawet przy "dobrym" apparent
            // bo actual jest bardzo niski względem apparent

            // Szansa na TimingBelt — klasyczna pułapka dla wszystkich
            double timingChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.40 ? 0.30 : 0.08,  // Honest ujawni usterkę w opisie
                (SellerArchetype.Neglected, 1) => actual < 0.45 ? 0.20 : 0.05,
                (SellerArchetype.Neglected, 2) => actual < 0.45 ? 0.28 : 0.08,
                (SellerArchetype.Neglected, 3) => actual < 0.50 ? 0.35 : 0.12,  // hoarder nie wie kiedy ostatnio wymieniony
                (SellerArchetype.Dealer, 1) => actual < 0.35 ? 0.25 : 0.10,
                (SellerArchetype.Dealer, 2) => actual < 0.35 ? 0.35 : 0.18,  // pro ukrywa
                (SellerArchetype.Dealer, 3) => actual < 0.30 ? 0.50 : 0.30,  // criminal: pasek wymieniony licznikiem nie stanem
                (SellerArchetype.Wrecker, 1) => actual < 0.30 ? 0.30 : 0.10,
                (SellerArchetype.Wrecker, 2) => actual < 0.25 ? 0.40 : 0.18,
                (SellerArchetype.Wrecker, 3) => 0.55,  // expert: prawie zawsze rozrząd to bomba zegarowa
                _ => 0.10,
            };
            if (rng.NextDouble() < timingChance) f |= FaultFlags.TimingBelt;

            // HeadGasket — bardzo poważna, rzadka, wyższy poziom dealera/wreckera = większa szansa
            double headChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.25 ? 0.15 : 0.02,
                (SellerArchetype.Neglected, _) => actual < 0.30 ? 0.12 : 0.03,
                (SellerArchetype.Dealer, 1) => actual < 0.30 ? 0.08 : 0.02,
                (SellerArchetype.Dealer, 2) => actual < 0.30 ? 0.18 : 0.06,
                (SellerArchetype.Dealer, 3) => actual < 0.25 ? 0.35 : 0.15,  // criminal: główna atrakcja
                (SellerArchetype.Wrecker, 1) => actual < 0.20 ? 0.10 : 0.03,
                (SellerArchetype.Wrecker, 2) => actual < 0.20 ? 0.22 : 0.08,
                (SellerArchetype.Wrecker, 3) => 0.45,  // expert: prawdopodobnie głowica jest skończona
                _ => 0.03,
            };
            if (rng.NextDouble() < headChance) f |= FaultFlags.HeadGasket;

            // SuspensionWorn — powszechna, rośnie z zaniedbaniem i poziomem
            double suspChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.50 ? 0.35 : 0.10,
                (SellerArchetype.Neglected, 1) => actual < 0.55 ? 0.40 : 0.15,
                (SellerArchetype.Neglected, 2) => actual < 0.55 ? 0.50 : 0.22,
                (SellerArchetype.Neglected, 3) => actual < 0.55 ? 0.62 : 0.30,
                (SellerArchetype.Dealer, 1) => actual < 0.40 ? 0.30 : 0.08,
                (SellerArchetype.Dealer, 2) => actual < 0.35 ? 0.40 : 0.12,
                (SellerArchetype.Dealer, 3) => actual < 0.30 ? 0.55 : 0.20,  // ukryte za świeżymi amortyzatorami z przodu
                (SellerArchetype.Wrecker, 1) => actual < 0.30 ? 0.35 : 0.12,
                (SellerArchetype.Wrecker, 2) => actual < 0.25 ? 0.50 : 0.22,
                (SellerArchetype.Wrecker, 3) => 0.65,
                _ => 0.15,
            };
            if (rng.NextDouble() < suspChance) f |= FaultFlags.SuspensionWorn;

            // BrakesGone — typowe dla Neglected i Wrecker
            double brakeChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.40 ? 0.25 : 0.05,
                (SellerArchetype.Neglected, 1) => actual < 0.50 ? 0.35 : 0.10,
                (SellerArchetype.Neglected, 2) => actual < 0.50 ? 0.45 : 0.18,
                (SellerArchetype.Neglected, 3) => actual < 0.50 ? 0.55 : 0.25,
                (SellerArchetype.Dealer, 1) => actual < 0.35 ? 0.15 : 0.04,
                (SellerArchetype.Dealer, 2) => actual < 0.30 ? 0.20 : 0.06,
                (SellerArchetype.Dealer, 3) => actual < 0.25 ? 0.30 : 0.10,  // nowe klocki z przodu, tył = katastrofa
                (SellerArchetype.Wrecker, 1) => actual < 0.25 ? 0.30 : 0.10,
                (SellerArchetype.Wrecker, 2) => actual < 0.20 ? 0.42 : 0.18,
                (SellerArchetype.Wrecker, 3) => 0.55,
                _ => 0.10,
            };
            if (rng.NextDouble() < brakeChance) f |= FaultFlags.BrakesGone;

            // ExhaustRusted — wiek + zaniedbanie
            double exhaustChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.45 ? 0.30 : 0.08,
                (SellerArchetype.Neglected, 1) => actual < 0.50 ? 0.35 : 0.12,
                (SellerArchetype.Neglected, 2) => actual < 0.50 ? 0.45 : 0.18,
                (SellerArchetype.Neglected, 3) => actual < 0.50 ? 0.55 : 0.25,
                (SellerArchetype.Dealer, _) => actual < 0.35 ? 0.20 : 0.05,  // dealer wymienia tłumik żeby nie słyszałeś
                (SellerArchetype.Wrecker, 1) => actual < 0.30 ? 0.28 : 0.10,
                (SellerArchetype.Wrecker, 2) => actual < 0.25 ? 0.40 : 0.18,
                (SellerArchetype.Wrecker, 3) => 0.50,
                _ => 0.12,
            };
            if (rng.NextDouble() < exhaustChance) f |= FaultFlags.ExhaustRusted;

            // ElectricalFault — typowe dla starszych aut i Wrecker
            double elecChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.45 ? 0.20 : 0.05,
                (SellerArchetype.Neglected, 1) => actual < 0.50 ? 0.22 : 0.07,
                (SellerArchetype.Neglected, 2) => actual < 0.50 ? 0.30 : 0.12,
                (SellerArchetype.Neglected, 3) => actual < 0.50 ? 0.40 : 0.18,
                (SellerArchetype.Dealer, 2) => actual < 0.35 ? 0.22 : 0.08,
                (SellerArchetype.Dealer, 3) => actual < 0.30 ? 0.35 : 0.15,
                (SellerArchetype.Wrecker, 1) => actual < 0.25 ? 0.25 : 0.08,
                (SellerArchetype.Wrecker, 2) => actual < 0.20 ? 0.38 : 0.15,
                (SellerArchetype.Wrecker, 3) => 0.55,
                _ => 0.07,
            };
            if (rng.NextDouble() < elecChance) f |= FaultFlags.ElectricalFault;

            // GlassDamage — widoczna, Dealer i Wrecker L3 ukrywają przez dobre zdjęcia
            double glassChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.45 ? 0.25 : 0.05,
                (SellerArchetype.Neglected, _) => actual < 0.50 ? 0.30 : 0.08,
                (SellerArchetype.Dealer, 1) => actual < 0.40 ? 0.10 : 0.02,  // backyard naprawia szyby żeby wyglądało
                (SellerArchetype.Dealer, 2) => 0.02,
                (SellerArchetype.Dealer, 3) => 0.01,  // criminal: auto wygląda doskonale
                (SellerArchetype.Wrecker, 1) => actual < 0.30 ? 0.35 : 0.12,
                (SellerArchetype.Wrecker, 2) => actual < 0.25 ? 0.25 : 0.10,
                (SellerArchetype.Wrecker, 3) => 0.05,  // expert: czyste szyby, wszystko inne do wymiany
                _ => 0.08,
            };
            if (rng.NextDouble() < glassChance) f |= FaultFlags.GlassDamage;

            return f;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SELEKCJA OPISU
        // ══════════════════════════════════════════════════════════════════════

        private static string SelectNote(SellerArchetype arch, int level, FaultFlags faults, Random rng)
        {
            // Uczciwy zawsze ujawnia usterkę jeśli ją ma
            if (arch == SellerArchetype.Honest)
            {
                foreach (var flag in new[] { FaultFlags.HeadGasket, FaultFlags.TimingBelt, FaultFlags.SuspensionWorn, FaultFlags.BrakesGone, FaultFlags.ElectricalFault })
                {
                    if (faults.HasFlag(flag) && NotesHonestFault.TryGetValue(flag, out var specific))
                        return specific;
                }
            }

            // Per archetype+level — osobne pule dla każdego poziomu
            string[] pool = (arch, level) switch
            {
                (SellerArchetype.Honest, 1) => NotesHonestNovice,
                (SellerArchetype.Honest, 2) => NotesHonest,
                (SellerArchetype.Honest, 3) => NotesHonestVeteran,
                (SellerArchetype.Neglected, 1) => NotesNeglected,
                (SellerArchetype.Neglected, 2) => NotesNeglectedBusy,
                (SellerArchetype.Neglected, 3) => NotesNeglectedHoarder,
                (SellerArchetype.Dealer, 1) => NotesDealer,
                (SellerArchetype.Dealer, 2) => NotesDealerPro,
                (SellerArchetype.Dealer, 3) => NotesDealerCriminal,
                (SellerArchetype.Wrecker, 1) => NotesWrecker,
                (SellerArchetype.Wrecker, 2) => NotesWreckerIntermediate,
                (SellerArchetype.Wrecker, 3) => NotesWreckerExpert,
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
            OXLPlugin.Log.Msg($"[OXL] Purchased: {listing.Make} {listing.Model} | Arch={listing.Archetype} L{listing.ArchetypeLevel} | Apparent={listing.ApparentCondition:P0} Actual={listing.ActualCondition:P0} | Faults={listing.Faults}");
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  NOTES — wszystkie pule opisów
        // ══════════════════════════════════════════════════════════════════════

        private static readonly Dictionary<FaultFlags, string> NotesHonestFault = new()
        {
            { FaultFlags.HeadGasket,    "Coolant mixing with oil, head gasket is gone. Priced accordingly, engine needs full work." },
            { FaultFlags.TimingBelt,    "Timing belt has not been changed in a long time. Needs doing before driving, hence the price." },
            { FaultFlags.SuspensionWorn,"Suspension is tired — shocks are soft and bushings are cracked. Nothing structural, but it needs money spent." },
            { FaultFlags.BrakesGone,    "Brakes are worn down to nothing. Pads need replacing before this goes on a road, hence the price." },
            { FaultFlags.ElectricalFault,"Battery keeps dying and alternator is not charging right. Sold as-is, fault is known." },
        };

        // ── Honest L1 — Novice ────────────────────────────────────────────────
        private static readonly string[] NotesHonestNovice =
        {
            "First time selling a car. Not sure if this is a good price but it feels right.",
            "I think everything works. I drove it to work every day and it always started.",
            "Bought it, didn't end up using it much. Selling as it is. I hope it is ok.",
            "Not sure what to write here. Car runs. Has wheels. Starts most times.",
            "My mechanic said it needs a few things but didn't say what exactly. Priced low just in case.",
            "I am not very car savvy. The dashboard lights are mostly off which is probably good.",
            "Honest sale. I don't know much about cars but I tried to describe it truthfully.",
        };

        // ── Honest L2 — Experienced ───────────────────────────────────────────
        private static readonly string[] NotesHonest =
        {
            "What you see is what you get. No hidden issues, priced to reflect actual condition.",
            "Selling because upgrading. Everything disclosed, nothing hidden. Straightforward sale.",
            "Had it serviced regularly. Recent oil change, tyres have decent tread. Runs well.",
            "Honest private sale. Viewing welcome, no pressure. Ask anything and I will tell you.",
            "Full service history available. Nothing to hide — I kept records of everything done.",
            "Minor cosmetic scratches as shown in photos. Mechanically sound, no warning lights.",
            "Reluctant sale. Good car, just no longer needed. Priced fairly for the condition.",
        };

        // ── Honest L3 — Veteran ───────────────────────────────────────────────
        private static readonly string[] NotesHonestVeteran =
        {
            "Fifty-two cars sold over twenty years, all described exactly as they are. This is no different.",
            "Full documented service history, every stamp present. Price reflects genuine condition — not aspirational.",
            "I know what I have and I know what it is worth. Viewing encouraged. Lowballers ignored politely.",
            "If something needs doing I will tell you before you ask. Nothing worse than wasting someone's time.",
            "Genuine sale, no rush, no games. I would rather it go to someone who appreciates it than sell it fast.",
            "Sold with the same honesty I would want if I were buying. Priced at market, not above it.",
        };

        // ── Neglected L1 — Casual ─────────────────────────────────────────────
        private static readonly string[] NotesNeglected =
        {
            "Good runner, selling as-is. A few things probably need attention but drives fine.",
            "Been sitting more than driving lately. Starts fine, just needs someone to use it.",
            "Owned for years, no major problems. Oil probably needs changing, I'll be honest.",
            "Bit dusty inside. Mechanically it's ok I think. No warning lights on... most of the time.",
            "Air filter probably needs doing. Or maybe it doesn't. We are both finding out together.",
            "I service it every year whether it needs it or not. Last service was 2019.",
            "There is a vibration above 90 km/h. I just don't go above 90. Simple lifestyle adjustment.",
            "Battery is the original from the factory. I think that counts as a feature at this point.",
        };

        // ── Neglected L2 — Busy ───────────────────────────────────────────────
        private static readonly string[] NotesNeglectedBusy =
        {
            "Selling quickly, no time for viewings. What you see is what you get, photos are recent enough.",
            "Listed three cars this week. Don't ask me which service was done on which. Price is price.",
            "Works. Drives. Stops. That covers the basics. I have four kids and no time for more detail.",
            "If it needs anything doing you'll find out when you buy it. I certainly haven't checked.",
            "Condition: it exists and moves. Everything else is a discovery process for the new owner.",
            "Selling fast, no negotiation, no sob stories. Collect this week or I relist.",
            "I drove it yesterday and it was fine. Two days ago also fine. Before that I can't recall.",
        };

        // ── Neglected L3 — Hoarder ───────────────────────────────────────────
        private static readonly string[] NotesNeglectedHoarder =
        {
            "One of eleven. Selling a few to make room. This one starts — I checked last Tuesday I think.",
            "Been under a tarp for a while. Exact duration unclear. Everything original, nothing replaced ever.",
            "I have too many. This was the overflow. Priced to clear space not to make money.",
            "Did it run when parked? Almost certainly yes. Does it run now? Probably yes. One way to find out.",
            "Stored dry. Mostly dry. A corner of the garage leaks but that side of the car looks fine.",
            "Selling the collection in parts. This is part seven. No history, no paperwork, no problem.",
        };

        // ── Dealer L1 — Backyard ─────────────────────────────────────────────
        private static readonly string[] NotesDealer =
        {
            "Freshly detailed, cleaned inside and out. Drives well, no issues noticed.",
            "Touched up the paint and gave it a good wash. Looks and feels much better than before.",
            "Picked it up, sorted it out, passing it on. Drives fine, nothing rattling.",
            "Gave it a full clean and checked the basics. Presentable car at a fair price.",
            "Not a project — I took care of the obvious stuff. Ready to drive away today.",
        };

        // ── Dealer L2 — Pro ───────────────────────────────────────────────────
        private static readonly string[] NotesDealerPro =
        {
            "Full professional valet, recent service, drives like new. First to see will buy.",
            "One owner history sourced, garage kept, every service stamp present. Exceptional example.",
            "Just finished a full inspection — brakes checked, fluid levels topped, all lights functional.",
            "Selling due to family expansion. Viewing will not disappoint, serious buyers only.",
            "Immaculate example. Price is firm because the car speaks for itself. No lowballers.",
            "Turn key ready. Not a project — everything verified and professionally presented.",
            "Private sale from a careful owner. Full documentation available on viewing.",
        };

        // ── Dealer L3 — Criminal ─────────────────────────────────────────────
        private static readonly string[] NotesDealerCriminal =
        {
            "Dealer maintained, full electronic history check passed, mileage verified and correct.",
            "Former company vehicle, low-stress usage. All original components, nothing substituted.",
            "Professionally restored interior and exterior. Mechanicals fully rebuilt to specification.",
            "One of the cleanest examples currently available. Priced below comparable listings for quick sale.",
            "WBAC valued at significantly more. Genuine price drop for this week only. Viewing essential.",
            "I stand behind every car I sell. This is the best I have had in years. Come and see.",
            "Full AA inspection report available on request. Nothing to hide — everything documented.",
        };

        // ── Wrecker L1 — Amateur ─────────────────────────────────────────────
        private static readonly string[] NotesWrecker =
        {
            "Selling fast, no return, cash only, I leave the country on Wednesday.",
            "Small issues but nothing major i think. Price negotiable with Farget gift code.",
            "Please no time wasters. Genuine buyers only. Send bank transfer first for viewing slot.",
            "Good car for the money. Small smoke on start but goes away. Normal for the age.",
            "Warranty provided by me personally. Does not cover engine, gearbox, bodywork, or other parts.",
            "Previous owner was scientist. He drove only to work. His work was four hundred kilometres away.",
            "I will not be reachable after purchase. Not because scam. I am just very busy man.",
        };

        // ── Wrecker L2 — Intermediate ─────────────────────────────────────────
        private static readonly string[] NotesWreckerIntermediate =
        {
            "Selling on behalf of family member who is currently overseas. They authorised me to handle everything.",
            "Just had a full service done last month — all receipts available at collection.",
            "Minor oil seep from valve cover, normal for age, not a problem. Priced to reflect.",
            "Car is in storage, photos taken this morning. Collection only, no delivery.",
            "Bought at auction, selling privately. No history but runs and drives well.",
            "Owner relocated abroad. I have power of attorney and all relevant paperwork.",
            "Photos are accurate. A few small things to sort but nothing a competent buyer can't handle.",
        };

        // ── Wrecker L3 — Expert ───────────────────────────────────────────────
        private static readonly string[] NotesWreckerExpert =
        {
            "Reluctant sale of a genuinely excellent car. Full service history, zero hidden faults.",
            "Owned for six years, maintained obsessively, selling only due to relocation abroad.",
            "One careful private owner from new. Priced at market — no games, no drama.",
            "Had this independently inspected last week. Report shows everything within spec. Available on viewing.",
            "Priced below what I paid because I need it gone this week. This does not happen often.",
            "Will not be undersold. You will not find a better example at this price. Come and verify.",
            "Sold with full documentation. Nothing to disclose because nothing is wrong. Simple as that.",
        };

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private static double BetaSample(Random rng, double alpha, double beta)
        {
            double x = GammaSample(rng, alpha);
            double y = GammaSample(rng, beta);
            return x / (x + y);
        }

        private static double GammaSample(Random rng, double shape)
        {
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

        private static double NextGaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private static string GenReg(Random rng)
        {
            const string L = "ABCDEFGHJKLMNPRSTVWXYZ";
            const string N = "0123456789";
            int digitCount = rng.Next(2, 5);
            int trailCount = rng.Next(2, 3);
            var sb = new System.Text.StringBuilder();
            sb.Append(L[rng.Next(L.Length)]);
            sb.Append(L[rng.Next(L.Length)]);
            for (int i = 0; i < digitCount; i++) sb.Append(N[rng.Next(N.Length)]);
            for (int i = 0; i < trailCount; i++) sb.Append(L[rng.Next(L.Length)]);
            return sb.ToString();
        }

        private static readonly string[] Locations =
        {
            "Ashford Creek", "Dunmore Hill", "Crestwick", "Barlow Falls",
            "Tyndall Cross", "Greystone Bay", "Portwick", "Aldenmoor",
            "Fenwick Hollow", "Clarendon Rise", "Saltbury", "Wexmoor",
            "Hadleigh Point", "Thorngate", "Ivybridge End", "Coldwater Bluff",
            "Elmshire", "Brackenford", "Southmere", "Galloway Reach"
        };

        // ── Kolory — te same co poprzednio ────────────────────────────────────
        private static readonly Dictionary<string, string[]> ActiveColors = new()
        {
            { "car_dnb_censor",          new[] { "white", "black", "red", "silver", "gray", "darkblue" } },
            { "car_katagiri_tamago",     new[] { "white", "silver", "red", "black", "blue", "green" } },
            { "car_luxor_streamliner",   new[] { "white", "black", "cream", "darkblue", "maroon", "silver" } },
            { "car_mayen_m5",            new[] { "white", "black", "silver", "red", "gray", "darkblue" } },
            { "car_salem_aries",         new[] { "white", "silver", "black", "red", "teal", "darkgreen" } },
        };

        private static readonly Dictionary<string, string[]> AllColors = new()
        {
            { "car_dnb_censor",          new[] { "white", "black", "red", "darkred", "silver", "gray", "cyan", "lightblue", "blue", "darkblue", "navy", "green", "darkgreen", "gold", "beige" } },
            { "car_katagiri_tamago",     new[] { "white", "silver", "red", "black", "blue", "green", "orange", "purple", "gray", "darkblue" } },
            { "car_luxor_streamliner",   new[] { "white", "black", "cream", "offwhite", "beige", "darkblue", "maroon", "darkmaroon", "silver", "gray", "teal", "darkteal", "rust" } },
            { "car_mayen_m5",            new[] { "white", "black", "silver", "red", "gray", "darkblue", "darkgreen", "gold", "charcoal", "nearblack", "darkgray", "gray2" } },
            { "car_salem_aries",         new[] { "white", "silver", "black", "red", "darkred", "teal", "darkgreen", "blue", "maroon", "beige", "gray", "pink" } },
        };
    }
}