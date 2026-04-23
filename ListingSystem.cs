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
        private readonly CarSpecLoader _specLoader;

        // ── Konfiguracja — domyślna, nadpisywana przez ApplyConfig() ─────────────
        private ListingGenConfig _config = new ListingGenConfig();

        /// <summary>Stosuje nową konfigurację. Aktywne ogłoszenia nie są ruszane.</summary>
        public void ApplyConfig(ListingGenConfig config)
        {
            _config = config ?? new ListingGenConfig();
            OXLLog.Msg($"[OXL:LGSYS] Config applied — max={_config.MaxListings}" +
                       $" chance={_config.GenChancePct}% batch={_config.GenMin}–{_config.GenMax}" +
                       $" dur={_config.DurMinSec:F0}s–{_config.DurMaxSec:F0}s" +
                       $" archW=[{string.Join(",", _config.ArchWeights)}]");
        }

        // ── Stan tickowania ───────────────────────────────────────────────────────
        private bool _initialSeeded = false;
        private float _lastGenCheckTime = 0f;

        // Interwał sprawdzania generacji = 1 "godzina gry" w sekundach Unity
        private const float GenCheckInterval = ListingGenConfig.SecondsPerGameHour;

		// ── Konstruktor ──────────────────────────────────
		public ListingSystem(CarPhotoLoader photoLoader, CarSpecLoader specLoader, SellerProfile sellerProfile)
		{
			_photoLoader = photoLoader;
			_specLoader = specLoader;
			_sellerProfile = sellerProfile;
		}


		private class CarDef
        {
            public string Make, Model, ImageFolder, InternalId;
            public int MinYear, MaxYear, MinPrice, MaxPrice;
        }

        private static readonly CarDef[] CarDefs =
        {
            new CarDef { Make = "DNB",      Model = "Censor",         ImageFolder = "DNB Censor",          InternalId = "car_dnb_censor",          MinYear = 1991, MaxYear = 1992, MinPrice = 8000,  MaxPrice = 28000 },
            new CarDef { Make = "Katagiri", Model = "Tamago BP",       ImageFolder = "Katagiri Tamago BP",  InternalId = "car_katagiri_tamago",      MinYear = 2000, MaxYear = 2005, MinPrice = 3500,  MaxPrice = 12000 },
            new CarDef { Make = "Luxor",    Model = "Streamliner Mk3", ImageFolder = "Luxor Streamliner Mk3", InternalId = "car_luxor_streamliner",  MinYear = 1992, MaxYear = 1996, MinPrice = 4000,  MaxPrice = 14000 },
            new CarDef { Make = "Mayen",    Model = "M5",              ImageFolder = "Mayen M5",            InternalId = "car_mayen_m5",             MinYear = 2008, MaxYear = 2015, MinPrice = 8000,  MaxPrice = 18000 },
            new CarDef { Make = "Salem",    Model = "Aries MK3",       ImageFolder = "Salem Aries MK3",     InternalId = "car_salem_aries",          MinYear = 2003, MaxYear = 2008, MinPrice = 4500,  MaxPrice = 15000 },
        };

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVE LISTINGS
        // ══════════════════════════════════════════════════════════════════════

        // ── Aktywne ogłoszenia ────────────────────────────────────────────────────
        public List<CarListing> ActiveListings { get; private set; } = new();
        private float _gameTime = 0f;
        private double _gameTimeOrigin = -1.0;
        public float GameTime => _gameTime;


        private bool _originFromRealTM = false;
        private readonly SellerProfile _sellerProfile;

        // ═════════════════════════════════════════════════════════════════════════
        //  TICK
        // ═════════════════════════════════════════════════════════════════════════

        private float _tmWaitTimer = 0f;
        private const float TmWaitSec = 3f;

        public void Tick(float deltaTime)
        {
            double gameSecs = GameTimeProvider.TotalGameSeconds;

            if (_gameTimeOrigin < 0.0)
            {
                if (GameTimeProvider.IsReadingFromTM)
                {
                    // TM instance found — safe to anchor here
                    _gameTimeOrigin = gameSecs;
                    _originFromRealTM = true;
                    OXLLog.Msg($"[OXL:LGSYS] GameTime origin (TM)={gameSecs:F1}s ({gameSecs / 3600.0:F2}h)");
                }
                else
                {
                    // TM not yet returning real values — wait
                    _tmWaitTimer += deltaTime;
                    if (_tmWaitTimer < TmWaitSec)
                        return;
                    // Hard timeout: TM never showed up, use fallback
                    _gameTimeOrigin = gameSecs;
                    _originFromRealTM = false;
                    OXLLog.Msg($"[OXL:LGSYS] GameTime origin (fallback after {_tmWaitTimer:F1}s)={gameSecs:F1}s");
                }
            }
            else if (!_originFromRealTM && GameTimeProvider.IsReadingFromTM)
            {
                // TM became available AFTER we anchored to a fallback value.
                // Re-anchor to prevent the huge _gameTime jump that wipes all listings.
                float elapsed = _gameTime;
                foreach (var l in ActiveListings)
                    l.ExpiresAt = Mathf.Max(0f, l.ExpiresAt - elapsed);
                _gameTimeOrigin = gameSecs;
                _originFromRealTM = true;
                _gameTime = 0f;
                _lastGenCheckTime = 0f;
                OXLLog.Msg($"[OXL:LGSYS] Re-anchored to TM={gameSecs:F1}s (fallback elapsed={elapsed:F1}s)");
            }

            _gameTime = (float)(gameSecs - _gameTimeOrigin);
            if (_gameTime < 0f) _gameTime = 0f;

            // reszta IDENTYCZNA jak dotychczas od "Usuń wygasłe" wzwyż
            int before = ActiveListings.Count;
            ActiveListings.RemoveAll(l => l.ExpiresAt <= _gameTime);
            if (ActiveListings.Count != before)
                Save();

            if (!_initialSeeded)
            {
                int seedCount = UnityEngine.Mathf.Min(4, _config.MaxListings);
                while (ActiveListings.Count < seedCount)
                    ActiveListings.Add(GenerateListing());
                _initialSeeded = true;
                _lastGenCheckTime = _gameTime;
                return;
            }

            if (_gameTime - _lastGenCheckTime >= GenCheckInterval)
            {
                _lastGenCheckTime = _gameTime;
                TryGenerateBatch();
            }
        }

        /// <summary>Wymusza sprawdzenie generacji przy następnym Tick().</summary>
        public void ForceCheckNow()
        {
            _lastGenCheckTime = _gameTime - GenCheckInterval;
            OXLLog.Msg("[OXL:LGSYS] ForceCheckNow — next Tick will trigger TryGenerateBatch()");
        }

        public void Save() => ListingPersistence.Save(ActiveListings, _gameTime);

        public void LoadSaved()
        {
            var loaded = ListingPersistence.Load(_gameTime);
            if (loaded.Count == 0) return;

            ActiveListings = loaded;
            _initialSeeded = true;        // nie seeduj ponownie przy pierwszym Tick()
            _lastGenCheckTime = _gameTime;   // reset interwału generacji
            OXLLog.Msg($"[OXL:LGSYS] Restored {loaded.Count} listings from previous session.");
        }

        /// <summary>
        /// Rzut szansą, potem generacja batcha.
        /// Wywoływany co GenCheckInterval lub przez ForceGenerate.
        /// </summary>
        private void TryGenerateBatch()
        {
            if (ActiveListings.Count >= _config.MaxListings) return;
            if (UnityEngine.Random.Range(0, 100) >= _config.GenChancePct) return;

            int batchSize = UnityEngine.Random.Range(_config.GenMin, _config.GenMax + 1);
            int canAdd = _config.MaxListings - ActiveListings.Count;
            batchSize = UnityEngine.Mathf.Min(batchSize, canAdd);

            OXLLog.Msg($"[OXL:LGSYS] Batch: +{batchSize} (cap={_config.MaxListings} current={ActiveListings.Count})");

            for (int i = 0; i < batchSize; i++)
                ActiveListings.Add(GenerateListing());

            Save();  // ← zapis po każdym batchu
        }

        public void ForceGenerate()
        {
            if (ActiveListings.Count >= _config.MaxListings)
            {
                OXLLog.Msg($"[OXL:LGSYS] ForceGenerate skipped — at cap ({_config.MaxListings})");
                return;
            }
            ActiveListings.Add(GenerateListing());
            Save();  
        }




        // ══════════════════════════════════════════════════════════════════════
        //  GENEROWANIE LISTINGU
        // ══════════════════════════════════════════════════════════════════════

        private CarListing GenerateListing()
        {
            var rng = new Random();
            var def = CarDefs[rng.Next(CarDefs.Length)];
            int year = rng.Next(def.MinYear, def.MaxYear + 1);


            // ── Config — wykryty dynamicznie z folderów przez CarPhotoLoader ──────────
            int carConfig = 0;
            if (_photoLoader != null)
            {
                var availableConfigs = _photoLoader.GetAvailableConfigs(def.ImageFolder);
                carConfig = availableConfigs[rng.Next(availableConfigs.Length)];
            }

            // ── Kolor — czytany dynamicznie z CarPhotoLoader ─────────────────────
            string color = "white";
            int colorIndex = 0;

            if (_photoLoader != null)
            {
                var availableColors = _photoLoader.GetAvailableColors(def.ImageFolder, carConfig);
                if (availableColors.Length > 0)
                    color = availableColors[rng.Next(availableColors.Length)];
                else
                    OXLPlugin.Log.Msg($"[OXL:GEN] No colors found for '{def.ImageFolder}' cfg={carConfig}, using white");

                colorIndex = _photoLoader.GetColorIndex(def.ImageFolder, color, carConfig);
            }

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
            // Pobierz dane cenowe ze spec dla tego auta
            var spec = _specLoader?.Get(def.InternalId, carConfig);
            var archetypePrices = spec?.ArchetypePrices;
            string specSource = (archetypePrices != null && archetypePrices.Count > 0)
                ? $"JSON ({archetypePrices.Count} keys)"
                : "FALLBACK (no spec data)";

            int price = RollPrice(def, archetype, level, apparent, actual, year, faults, rng, archetypePrices);
            int fairValue = CalcFairValue(actual, archetypePrices);
            if (fairValue <= 0)
                fairValue = Mathf.Max(300, Mathf.RoundToInt(
                    Mathf.Lerp(def.MinPrice, def.MaxPrice, actual) * YearFactor(year)
                    * OXLSettings.PriceMultiplier / 50f) * 50);

            OXLLog.Msg($"[OXL:PRICE] ── PRICE CALC ──────────────────────────────");
            OXLLog.Msg($"[OXL:PRICE] Car:       {def.Make} {def.Model} {year}");
            OXLLog.Msg($"[OXL:PRICE] SpecSrc:   {specSource}");
            OXLLog.Msg($"[OXL:PRICE] Archetype: {archetype} L{level}  key={ArchetypeKey(archetype, level)}");
            OXLLog.Msg($"[OXL:PRICE] Apparent:  {apparent:P0}  Actual: {actual:P0}");
            OXLLog.Msg($"[OXL:PRICE] Price:     ${price:N0}   FairValue: ${fairValue:N0}");
            OXLLog.Msg($"[OXL:PRICE] ─────────────────────────────────────────────");

            // ── TTL — czas życia aukcji ────────────────────────────────────────
            float ttl = RollTTL(archetype, level, rng);

            // ── Rating sprzedawcy ──────────────────────────────────────────────
            int rating = RollSellerRating(archetype, level, rng);

            // ── Przebieg ───────────────────────────────────────────────────────
            int mileage = Mathf.RoundToInt(Mathf.Lerp(180000, 4000, actual) * (1f + (float)(rng.NextDouble() * 0.30 - 0.15)));
            mileage = Mathf.Max(500, mileage);

            // ── L3 Veteran nigdy nie sprzedaje złomu ──────────────────────────
            if (archetype == SellerArchetype.Honest && level == 3)
                actual = Mathf.Max(actual, 0.32f);

			

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
				SellerNote = "",
                ExpiresAt = _gameTime + ttl,
                InternalId = def.InternalId + "_" + rng.Next(1000, 9999),
                Mileage = mileage,
                Location = Locations[rng.Next(Locations.Length)],
                DeliveryHours = rng.Next(1, 37),
                SellerRating = rating,
                Color = color,
                ColorIndex = colorIndex,
                CarConfig = carConfig,
            };


            // Photo files wybrane raz, z uwzględnieniem konfigu
            listing.PhotoFiles = _photoLoader?.SelectPhotoFiles(listing) ?? new List<string>();

            var (avatarTex, nick, avatarPath) = _sellerProfile.Generate(listing, rng);
			listing.SellerNick = nick;
			listing.AvatarPath = avatarPath;

			// ── Nota sprzedawcy ────────────────────────────────────────────────
			listing.SellerNote = SellerNoteBuilder.BuildNote(listing, rng);

			OXLPlugin.Log.Msg($"[OXL:GEN] {def.Make} {def.Model} {year} | Arch={archetype} L{level} | Rating={rating}★ | Apparent={apparent:P0} Actual={actual:P0} Body={bodyCondition:P0} | Price=${price:N0} Fair=${fairValue:N0} ({(fairValue > 0 ? (float)price / fairValue : 0):F2}x) | Faults={faults} | Color={color} | TTL={ttl:F0}s");


            return listing;
        }

        public static Dictionary<string, (string carId, string[] colors)> GetColorRegistry(CarSpecLoader specLoader)
        {
            var result = new Dictionary<string, (string, string[])>();
            foreach (var def in CarDefs)
            {
                // Czytaj kolory ze spec cfg=0 (wszystkie konfigi mają te same kolory per auto)
                var spec = specLoader?.Get(def.InternalId, 0);

                if (spec != null && !string.IsNullOrEmpty(spec.CarId)
                    && spec.ColorNames != null && spec.ColorNames.Length > 0)
                {
                    var validNames = spec.ColorNames.Where(n => !string.IsNullOrEmpty(n)).ToArray();
                    if (validNames.Length > 0)
                    {
                        result[def.ImageFolder] = (def.InternalId, validNames);
                        OXLPlugin.Log.Msg(
                            $"[OXL:REGISTRY] '{def.InternalId}': {validNames.Length} colors from spec");
                        continue;
                    }
                }

                OXLPlugin.Log.Msg(
                    $"[OXL:REGISTRY] No colors from spec for '{def.InternalId}' — car will use folder fallback");
            }
            return result;
        }

        // ═════════════════════════════════════════════════════════════════════════
        //  ARCHETYPE + LEVEL — config-driven
        // ═════════════════════════════════════════════════════════════════════════

        private SellerArchetype RollArchetype(Random rng)
        {
            int[] w = _config.ArchWeights;
            int total = w[0] + w[1] + w[2] + w[3];
            if (total <= 0) return SellerArchetype.Wrecker; // guard

            int roll = rng.Next(0, total);

            if (roll < w[0]) return SellerArchetype.Honest;
            roll -= w[0];
            if (roll < w[1]) return SellerArchetype.Wrecker;
            roll -= w[1];
            if (roll < w[2]) return SellerArchetype.Dealer;
            return SellerArchetype.Scammer;
        }

        private int RollLevel(SellerArchetype arch, Random rng)
        {
            // SellerArchetype enum: Honest=0, wrecker=1, Dealer=2, scammer=3
            int archIdx = (int)arch;
            int[] w = _config.LvlWeights[archIdx];
            int total = w[0] + w[1] + w[2];
            if (total <= 0) return 1; // guard

            int roll = rng.Next(0, total);

            if (roll < w[0]) return 1;
            roll -= w[0];
            if (roll < w[1]) return 2;
            return 3;
        }

        // ── ApparentCondition — co gracz widzi w ogłoszeniu ──────────────────
        // Dealer i scammer kłamią na poziomie apparent (zdjęcia, opis)
        // Honest i wrecker — apparent ≈ actual (bez manipulacji)

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
                SellerArchetype.Wrecker => level switch
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
                SellerArchetype.Scammer => level switch
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
                SellerArchetype.Wrecker => level switch
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
                SellerArchetype.Scammer => level switch
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

                // Wrecker: zaniedbana karoseria — ciut gorsza niż actual
                SellerArchetype.Wrecker => (float)(rng.NextDouble() * 0.30),

                // Dealer: karoseria naprawiona niezależnie od mechaniki — to jest ich "produkt"
                SellerArchetype.Dealer => level switch
                {
                    1 => (float)(0.60 + rng.NextDouble() * 0.15),  // 0.60–0.75 — Backyard: umył, podmalował
                    2 => (float)(0.75 + rng.NextDouble() * 0.15),  // 0.75–0.90 — Pro: profesjonalny detailing
                    _ => (float)(0.90 + rng.NextDouble() * 0.09),  // 0.90–0.99 — Criminal: perfekcyjne, nie do odróżnienia
                },

                // Scammer: próbuje ukryć — L3 wygląda OK, L1 łatwy do wykrycia
                SellerArchetype.Scammer => level switch
                {
                    1 => Mathf.Clamp(actual * (float)(0.90 + rng.NextDouble() * 0.20), 0.05f, 0.75f),
                    2 => (float)(0.40 + rng.NextDouble() * 0.25),  // 0.40–0.65 — średni wygląd, trudniejszy do wykrycia
                    _ => (float)(0.62 + rng.NextDouble() * 0.25),  // 0.62–0.87 — Expert: posprzątany, odmalowany
                },

                _ => actual,
            };
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  PRICE — oparta na danych z car_spec_*.json
        //  archetypePrices: null = fallback do starego systemu
        // ══════════════════════════════════════════════════════════════════════════

        private static readonly Dictionary<string, float> ArchetypeRefActual = new()
{
    // Szacowane "typowe" actual condition dla każdego archetypu+poziomu
    // (odpowiada chassis/body w JSON: Honest L1 = chassis 50, body 45)
    { "wreckerL1", 0.04f },
    { "wreckerL2", 0.10f },
    { "wreckerL3", 0.25f },
    { "honestL1",    0.38f },
    { "honestL2",    0.60f },
    { "honestL3",    0.88f },
    // Dealer i Wrecker nie skalują — ich cena zależy od narracji, nie od actual
};

        private static string ArchetypeKey(SellerArchetype arch, int level) =>
            (arch, level) switch
            {
                (SellerArchetype.Wrecker, 1) => "wreckerL1",
                (SellerArchetype.Wrecker, 2) => "wreckerL2",
                (SellerArchetype.Wrecker, 3) => "wreckerL3",
                (SellerArchetype.Dealer, 1) => "dealerL1",
                (SellerArchetype.Dealer, 2) => "dealerL2",
                (SellerArchetype.Dealer, 3) => "dealerL3",
                (SellerArchetype.Honest, 1) => "honestL1",
                (SellerArchetype.Honest, 2) => "honestL2",
                (SellerArchetype.Honest, 3) => "honestL3",
                (SellerArchetype.Scammer, 1) => "scammerL1",
                (SellerArchetype.Scammer, 2) => "scammerL2",
                (SellerArchetype.Scammer, 3) => "scammerL3",
                _ => "honestL2",
            };

        private static int RollPrice(CarDef def, SellerArchetype arch, int level,float apparent, float actual, int year, FaultFlags faults,Random rng,Dictionary<string, ArchetypePrice> archetypePrices)
        {
            string key = ArchetypeKey(arch, level);

            ArchetypePrice ap = null;
            archetypePrices?.TryGetValue(key, out ap);

            if (ap == null || ap.Price <= 0)
            {
                OXLLog.Msg($"[OXL:PRICE]   → FALLBACK (archetypePrices null={archetypePrices == null}, key={key})");
                return RollPriceFallback(def, arch, level, apparent, actual, year, faults, rng);
            }

            OXLLog.Msg($"[OXL:PRICE]   → JSON path: key={key} ap.Price={ap.Price} chassis={ap.Chassis} body={ap.Body}");

            float price;

            // ── HONEST: CalcFairValue × poziomowy mnożnik ─────────────────────────────
            if (arch == SellerArchetype.Honest)
            {
                int fair = CalcFairValue(actual, archetypePrices);
                if (fair <= 0) fair = ap.Price;

                float levelMult = level switch
                {
                    1 => (float)(0.75 + rng.NextDouble() * 0.15),  // 0.75–0.90×
                    2 => (float)(0.90 + rng.NextDouble() * 0.15),  // 0.90–1.05×
                    _ => (float)(1.00 + rng.NextDouble() * 0.15),  // 1.00–1.15×
                };

                price = fair * levelMult;

                // Twardy ceiling: Honest nigdy nie sprzedaje powyżej 75% max Honest L3.
                // Gwarantuje że gracz zawsze ma ~25% budżetu na części + marżę zysku.
                // Stosowany PRZED szumem i diffMult żeby ceiling był bezwzględny.
                archetypePrices.TryGetValue("honestL3", out var h3cap);
                if (h3cap != null && h3cap.Price > 0)
                {
                    float ceiling = h3cap.Price * 0.75f;
                    if (price > ceiling)
                    {
                        OXLLog.Msg($"[OXL:PRICE]   Honest ceiling hit: {price:F0} → {ceiling:F0}  (75% of h3={h3cap.Price})");
                        price = ceiling;
                    }
                }

                OXLLog.Msg($"[OXL:PRICE]   Honest: fair={fair} levelMult={levelMult:F3} → {price:F0}");

                // Rabat za znane usterki — Honest ujawnia je w opisie
                if (faults != FaultFlags.None)
                {
                    int faultCount = CountBits((int)faults);
                    float discPerFault = level switch { 1 => 0.11f, 2 => 0.08f, _ => 0.06f };
                    float discount = Mathf.Clamp(1f - faultCount * discPerFault, 0.52f, 0.93f);
                    float priceBeforeFaults = price;
                    price *= discount;
                    OXLLog.Msg($"[OXL:PRICE]   faultDisc: faults={faults} count={faultCount} disc={discount:F3}  {priceBeforeFaults:F0} → {price:F0}");
                }
            }
            // ── Wrecker: zawsze tanio — właściciel chce się pozbyć ─────────────────
            else if (arch == SellerArchetype.Wrecker)
            {
                int fair = CalcFairValue(actual, archetypePrices);
                if (fair <= 0) fair = ap.Price;

                float wreckerDisc = level switch
                {
                    1 => (float)(0.20 + rng.NextDouble() * 0.15),
                    2 => (float)(0.40 + rng.NextDouble() * 0.20),
                    _ => (float)(0.50 + rng.NextDouble() * 0.20),
                };

                price = fair * wreckerDisc;

                OXLLog.Msg($"[OXL:PRICE]   Wrecker L{level}: fair={fair} disc={wreckerDisc:F3} → {price:F0}");

                // ── absFloor (Podejście B) ─────────────────────────────────────────────
                // Problem: CalcFairValue przy actual=2-10% zwraca wartość ~130-630,
                // ale gra odkupuje auto bazując na Parts Value (~9-22k), nie na actual.
                // Wrecker L1/L2 spawnuje z częściami 20-55% → Parts Value >> JSON wreckerL1.price.
                // Floor oparty na honestL1 gwarantuje że OXL nie sprzedaje poniżej wartości odkupu.
                //
                //   L1 = 0.50 × honestL1  →  Katagiri: 6020,  Salem: 10491,  DNB: 17293
                //   L2 = 0.60 × honestL1  →  Katagiri: 7224,  Salem: 12590,  DNB: 20751
                //   L3 = 0.70 × honestL1  →  Katagiri: 8428,  Salem: 14688,  DNB: 24210
                if (archetypePrices.TryGetValue("honestL1", out var h1floor) && h1floor != null && h1floor.Price > 0)
                {
                    float absFloorMult = level switch { 1 => 0.50f, 2 => 0.60f, _ => 0.70f };
                    float absFloor = h1floor.Price * absFloorMult;
                    if (price < absFloor)
                    {
                        OXLLog.Msg($"[OXL:PRICE]   Wrecker absFloor hit L{level}: {price:F0} → {absFloor:F0}" +
                                   $"  ({absFloorMult:P0} × h1={h1floor.Price})");
                        price = absFloor;
                    }
                    else
                    {
                        OXLLog.Msg($"[OXL:PRICE]   Wrecker absFloor OK: {price:F0} >= {absFloor:F0}");
                    }
                }
            }
            // ── DEALER: wycena od apparent — sprzedaje wygląd, nie mechanikę ─────────
            else if (arch == SellerArchetype.Dealer)
            {
                // fairApparent = ile gracz zapłaciłby u Honest za auto W TAKIM STANIE WIZUALNYM.
                // Dealer naprawia karoserię do apparent, więc ta cena jest "uzasadniona" wizualnie.
                // Pułapka polega na tym że actual (mechanika) jest dużo niższe.
                int fairApparent = CalcFairValue(apparent, archetypePrices);
                if (fairApparent <= 0) fairApparent = ap.Price;

                // L1 Backyard  : lekko poniżej rynku apparent — "uczciwa cena", nic podejrzanego
                // L2 Pro       : rynkowa, bardzo przekonująca
                // L3 Criminal  : odrobinę taniej niż fair apparent — "okazja" = przynęta
                float markup = level switch
                {
                    1 => (float)(0.85 + rng.NextDouble() * 0.15), // 0.85–1.00×
                    2 => (float)(0.90 + rng.NextDouble() * 0.15), // 0.90–1.05×
                    _ => (float)(0.72 + rng.NextDouble() * 0.13), // 0.72–0.85× — "trochę taniej od Honest"
                };

                price = fairApparent * markup;
                OXLLog.Msg($"[OXL:PRICE]   Dealer L{level}: fairApparent={fairApparent} markup={markup:F3} → {price:F0}");
            }
            // ── SCAMMER: wycena od apparent — kłamstwo, różne style na każdym poziomie
            else
            {
				// SCAMMER też wycenia od apparent (kłamie na poziomie zdjęć/opisu).
				// Różnica vs Dealer: mniej profesjonalne ukrycie, inne sygnały w cenie.
				int fairApparent = CalcFairValue(apparent, archetypePrices);
                if (fairApparent <= 0) fairApparent = ap.Price;

                // L1 Amateur      : drożej niż powinno być za ten wygląd → czerwona flaga
                // L2 Intermediate : rynkowa, podszywa się pod Dealera lub Honest
                // L3 Expert       : minimalnie taniej niż Honest → "okazja" nie do odróżnienia
                float markup = level switch
                {
                    1 => (float)(1.10 + rng.NextDouble() * 0.30), // 1.10–1.40× — przepłata, sygnał
                    2 => (float)(0.88 + rng.NextDouble() * 0.22), // 0.88–1.10× — rynkowa, brak sygnału
                    _ => (float)(0.75 + rng.NextDouble() * 0.15), // 0.75–0.90× — "lekko taniej od Honest"
                };

                price = fairApparent * markup;
                OXLLog.Msg($"[OXL:PRICE]   SCAMMER L{level}: fairApparent={fairApparent} markup={markup:F3} → {price:F0}");

            }

            // ── Szum ±6% — wszystkie archetypy ───────────────────────────────────────
            float noiseVal = 1f + (float)(rng.NextDouble() * 0.12 - 0.06);
            float priceBeforeNoise = price;
            price *= noiseVal;
            OXLLog.Msg($"[OXL:PRICE]   noise: {noiseVal:F3}  {priceBeforeNoise:F0} → {price:F0}");

            // ── Mnożnik trudności — stosowany do WSZYSTKICH archetypów (spójność) ─────
            // Normal=1.00 (baseline), Easy=0.85 (tańsze), Hard=1.20 (droższe)
            float diffMult = OXLSettings.PriceMultiplier;
            float priceBeforeDiff = price;
            price *= diffMult;
            OXLLog.Msg($"[OXL:PRICE]   diffMult: {diffMult:F2} ({OXLSettings.CurrentDifficulty})  {priceBeforeDiff:F0} → {price:F0}");

            // ── Zaokrąglenie i floor ──────────────────────────────────────────────────
            int rounded = Mathf.RoundToInt(price / 50f) * 50;
            int floorPrice = Mathf.Max(300, Mathf.RoundToInt(ap.Price * 0.10f / 50f) * 50);
            int final = Mathf.Max(rounded, floorPrice);

            // ── Log oczekiwanej marży — czy aukcja jest opłacalna? ────────────────────
            // Przybliżone: max sprzedaży gry ≈ honestL3.Price × 1.20 (km=0, pełna naprawa)
            // Koszt naprawy ≈ 38% różnicy między actual a max (empiryczna estymacja)
            if (archetypePrices.TryGetValue("honestL3", out var h3log) && h3log != null)
            {
                int approxMaxSell = Mathf.RoundToInt(h3log.Price * 1.20f);
                int approxRepair = Mathf.RoundToInt(approxMaxSell * (1f - Mathf.Clamp(actual, 0f, 1f)) * 0.38f);
                int approxMargin = approxMaxSell - final - approxRepair;
                string verdict = approxMargin > 0 ? "OK" : "LOSS";
                OXLLog.Msg($"[OXL:PRICE]   margin≈{approxMargin} [{verdict}]" +
                           $"  (sell~{approxMaxSell} − buy {final} − repair~{approxRepair})");
            }

            OXLLog.Msg($"[OXL:PRICE]   final: rounded={rounded} floor={floorPrice} → {final}");
            return final;
        }

        // Fallback: stary system dla aut bez pliku spec (zabezpieczenie)
        private static int RollPriceFallback(
            CarDef def, SellerArchetype arch, int level,
            float apparent, float actual, int year, FaultFlags faults, Random rng)
        {
            float ageFactor = YearFactor(year);
            float baseCondition = (arch == SellerArchetype.Dealer || arch == SellerArchetype.Scammer)
                ? apparent : actual;
            float basePricef = Mathf.Lerp(def.MinPrice, def.MaxPrice, baseCondition) * ageFactor;

            float mult = (arch, level) switch
            {
                (SellerArchetype.Honest, 1) => (float)(0.78 + rng.NextDouble() * 0.12),
                (SellerArchetype.Honest, 2) => (float)(0.93 + rng.NextDouble() * 0.13),
                (SellerArchetype.Honest, 3) => (float)(1.05 + rng.NextDouble() * 0.11),
                (SellerArchetype.Wrecker, 1) => (float)(0.72 + rng.NextDouble() * 0.28),
                (SellerArchetype.Wrecker, 2) => (float)(0.58 + rng.NextDouble() * 0.28),
                (SellerArchetype.Wrecker, 3) => (float)(0.45 + rng.NextDouble() * 0.75),
                (SellerArchetype.Dealer, 1) => (float)(1.68 + rng.NextDouble() * 0.30),
                (SellerArchetype.Dealer, 2) => (float)(1.95 + rng.NextDouble() * 0.35),
                (SellerArchetype.Dealer, 3) => (float)(2.30 + rng.NextDouble() * 0.40),
                (SellerArchetype.Scammer, 1) => (float)(0.65 + rng.NextDouble() * 0.25),
                (SellerArchetype.Scammer, 2) => (float)(0.88 + rng.NextDouble() * 0.30),
                (SellerArchetype.Scammer, 3) => (float)(1.08 + rng.NextDouble() * 0.40),
                _ => 1.0f,
            };

            float noise = 1f + (float)(rng.NextDouble() * 0.06 - 0.03);
            int price = Mathf.RoundToInt(basePricef * mult * noise * OXLSettings.PriceMultiplier / 50f) * 50;

            int scaledMin = Math.Max(300, Mathf.RoundToInt(def.MinPrice * OXLSettings.PriceMultiplier * 0.25f));
            int scaledMax = Mathf.RoundToInt(def.MaxPrice * OXLSettings.PriceMultiplier * 2.2f);
            price = Mathf.Clamp(price, scaledMin, scaledMax);

            if (arch == SellerArchetype.Honest && faults != FaultFlags.None)
            {
                int faultCount = CountBits((int)faults);
                float discPerFault = level switch { 1 => 0.11f, 2 => 0.08f, _ => 0.06f };
                float discount = Mathf.Clamp(1f - faultCount * discPerFault, 0.52f, 0.93f);
                price = Mathf.RoundToInt(price * discount / 50f) * 50;
            }

            return Mathf.Max(price, 300);
        }

        // Uczciwa wartość rynkowa — interpolacja między honest cenami z JSON
        private static int CalcFairValue(float actual,
    Dictionary<string, ArchetypePrice> prices)
        {
            if (prices == null || prices.Count == 0) return 0;

            bool h1ok = prices.TryGetValue("honestL1", out var h1);
            bool h2ok = prices.TryGetValue("honestL2", out var h2);
            bool h3ok = prices.TryGetValue("honestL3", out var h3);

            if (!h1ok || !h2ok || !h3ok) return 0;

            float val;
            if (actual < 0.38f)
            {
                float t = actual / 0.38f;
                val = Mathf.Lerp(h1.Price * 0.25f, h1.Price, t);
            }
            else if (actual < 0.70f)
            {
                float t = (actual - 0.38f) / 0.32f;
                val = Mathf.Lerp(h1.Price, h2.Price, t);
            }
            else
            {
                float t = (actual - 0.70f) / 0.30f;
                val = Mathf.Lerp(h2.Price, h3.Price, Mathf.Clamp01(t));
            }

            return Mathf.Max(300, Mathf.RoundToInt(val / 50f) * 50);
        }

        // ~1.2% deprecjacji rocznie — max 50% wartości bazowej (nawet 40-letnie auto ma swój floor)
        private static float YearFactor(int year) =>
            Mathf.Clamp(1.0f - (2026 - year) * 0.012f, 0.50f, 1.0f);

        private static int CountBits(int v) { int c = 0; while (v != 0) { c += v & 1; v >>= 1; } return c; }

        // ══════════════════════════════════════════════════════════════════════
        //  TTL — czas życia aukcji
        // ══════════════════════════════════════════════════════════════════════

        private float RollTTL(SellerArchetype arch, int level, Random rng)
        {
            float lo = _config.DurMinSec;
            float hi = _config.DurMaxSec;

            // Każdy archetype preferuje inny zakres w obrębie [lo, hi]:
            //   Wrecker — szybko znika (dolne 0–50% zakresu)
            //   Honest    — środek (30–80%)
            //   Scammer   — środek-dolny (20–70%)
            //   Dealer    — cierpliwy (50–100%)
            // Wyższy level = bardziej skrajne zachowanie.

            float rangeMid = (lo + hi) * 0.5f;
            float rangeH = (hi - lo) * 0.5f;

            (float center, float spread) flavor = (arch, level) switch
            {
                (SellerArchetype.Wrecker, 1) => (lo + rangeH * 0.35f, rangeH * 0.30f),
                (SellerArchetype.Wrecker, 2) => (lo + rangeH * 0.25f, rangeH * 0.25f),
                (SellerArchetype.Wrecker, 3) => (lo + rangeH * 0.15f, rangeH * 0.15f), // hoarder: bardzo krótko

                (SellerArchetype.Honest, 1) => (rangeMid - rangeH * 0.15f, rangeH * 0.45f),
                (SellerArchetype.Honest, 2) => (rangeMid, rangeH * 0.40f),
                (SellerArchetype.Honest, 3) => (rangeMid + rangeH * 0.15f, rangeH * 0.35f),

                (SellerArchetype.Scammer, 1) => (lo + rangeH * 0.40f, rangeH * 0.40f),
                (SellerArchetype.Scammer, 2) => (rangeMid - rangeH * 0.10f, rangeH * 0.40f),
                (SellerArchetype.Scammer, 3) => (rangeMid + rangeH * 0.10f, rangeH * 0.35f),

                (SellerArchetype.Dealer, 1) => (rangeMid + rangeH * 0.10f, rangeH * 0.40f),
                (SellerArchetype.Dealer, 2) => (rangeMid + rangeH * 0.25f, rangeH * 0.35f),
                (SellerArchetype.Dealer, 3) => (hi - rangeH * 0.20f, rangeH * 0.20f), // criminal: czeka na ofiarę

                _ => (rangeMid, rangeH * 0.50f),
            };

            float result = flavor.center + (float)(rng.NextDouble() * 2.0 - 1.0) * flavor.spread;
            return UnityEngine.Mathf.Clamp(result, lo, hi);
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

                (SellerArchetype.Wrecker, 1) => rng.Next(0, 10) < 6 ? 3 : 4,           // 60% = 3★
                (SellerArchetype.Wrecker, 2) => rng.Next(0, 10) < 5 ? 2 : 3,           // 50% = 2★ — buyers complained
                (SellerArchetype.Wrecker, 3) => rng.Next(0, 10) < 6 ? 2 : 1,           // 60% = 2★, 40% = 1★ — hoarder: niezadowoleni klienci

                (SellerArchetype.Dealer, 1) => rng.Next(0, 10) < 5 ? 3 : 4,           // backyard: ok ale nie imponujący
                (SellerArchetype.Dealer, 2) => rng.Next(0, 10) < 6 ? 4 : 5,           // pro: kupiony / wypracowany rating
                (SellerArchetype.Dealer, 3) => rng.Next(0, 10) < 8 ? 5 : 4,           // criminal: sfałszowany 5★, prawie zawsze

                (SellerArchetype.Scammer, 1) => rng.Next(0, 10) < 7 ? 1 : 2,           // amator: widać że oszust
                (SellerArchetype.Scammer, 2) => rng.Next(0, 10) < 5 ? 2 : 3,           // intermediate: trochę lepiej
                (SellerArchetype.Scammer, 3) => rng.Next(0, 10) < 7 ? 4 : 5,           // expert: sfałszowany rating, wygląda jak Dealer

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
                (SellerArchetype.Wrecker, 1) => actual < 0.45 ? 0.20 : 0.05,
                (SellerArchetype.Wrecker, 2) => actual < 0.45 ? 0.28 : 0.08,
                (SellerArchetype.Wrecker, 3) => actual < 0.50 ? 0.35 : 0.12,  // hoarder nie wie kiedy ostatnio wymieniony
                (SellerArchetype.Dealer, 1) => actual < 0.35 ? 0.25 : 0.10,
                (SellerArchetype.Dealer, 2) => actual < 0.35 ? 0.35 : 0.18,  // pro ukrywa
                (SellerArchetype.Dealer, 3) => actual < 0.30 ? 0.50 : 0.30,  // criminal: pasek wymieniony licznikiem nie stanem
                (SellerArchetype.Scammer, 1) => actual < 0.30 ? 0.30 : 0.10,
                (SellerArchetype.Scammer, 2) => actual < 0.25 ? 0.40 : 0.18,
                (SellerArchetype.Scammer, 3) => 0.55,  // expert: prawie zawsze rozrząd to bomba zegarowa
                _ => 0.10,
            };
            if (rng.NextDouble() < timingChance) f |= FaultFlags.TimingBelt;

            // HeadGasket — bardzo poważna, rzadka, wyższy poziom dealera/wreckera = większa szansa
            double headChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.25 ? 0.15 : 0.02,
                (SellerArchetype.Wrecker, _) => actual < 0.30 ? 0.12 : 0.03,
                (SellerArchetype.Dealer, 1) => actual < 0.30 ? 0.08 : 0.02,
                (SellerArchetype.Dealer, 2) => actual < 0.30 ? 0.18 : 0.06,
                (SellerArchetype.Dealer, 3) => actual < 0.25 ? 0.35 : 0.15,  // criminal: główna atrakcja
                (SellerArchetype.Scammer, 1) => actual < 0.20 ? 0.10 : 0.03,
                (SellerArchetype.Scammer, 2) => actual < 0.20 ? 0.22 : 0.08,
                (SellerArchetype.Scammer, 3) => 0.45,  // expert: prawdopodobnie głowica jest skończona
                _ => 0.03,
            };
            if (rng.NextDouble() < headChance) f |= FaultFlags.HeadGasket;

            // SuspensionWorn — powszechna, rośnie z zaniedbaniem i poziomem
            double suspChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.50 ? 0.35 : 0.10,
                (SellerArchetype.Wrecker, 1) => actual < 0.55 ? 0.40 : 0.15,
                (SellerArchetype.Wrecker, 2) => actual < 0.55 ? 0.50 : 0.22,
                (SellerArchetype.Wrecker, 3) => actual < 0.55 ? 0.62 : 0.30,
                (SellerArchetype.Dealer, 1) => actual < 0.40 ? 0.30 : 0.08,
                (SellerArchetype.Dealer, 2) => actual < 0.35 ? 0.40 : 0.12,
                (SellerArchetype.Dealer, 3) => actual < 0.30 ? 0.55 : 0.20,  // ukryte za świeżymi amortyzatorami z przodu
                (SellerArchetype.Scammer, 1) => actual < 0.30 ? 0.35 : 0.12,
                (SellerArchetype.Scammer, 2) => actual < 0.25 ? 0.50 : 0.22,
                (SellerArchetype.Scammer, 3) => 0.65,
                _ => 0.15,
            };
            if (rng.NextDouble() < suspChance) f |= FaultFlags.SuspensionWorn;

            // BrakesGone — typowe dla Wrecker i Scammer
            double brakeChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.40 ? 0.25 : 0.05,
                (SellerArchetype.Wrecker, 1) => actual < 0.50 ? 0.35 : 0.10,
                (SellerArchetype.Wrecker, 2) => actual < 0.50 ? 0.45 : 0.18,
                (SellerArchetype.Wrecker, 3) => actual < 0.50 ? 0.55 : 0.25,
                (SellerArchetype.Dealer, 1) => actual < 0.35 ? 0.15 : 0.04,
                (SellerArchetype.Dealer, 2) => actual < 0.30 ? 0.20 : 0.06,
                (SellerArchetype.Dealer, 3) => actual < 0.25 ? 0.30 : 0.10,  // nowe klocki z przodu, tył = katastrofa
                (SellerArchetype.Scammer, 1) => actual < 0.25 ? 0.30 : 0.10,
                (SellerArchetype.Scammer, 2) => actual < 0.20 ? 0.42 : 0.18,
                (SellerArchetype.Scammer, 3) => 0.55,
                _ => 0.10,
            };
            if (rng.NextDouble() < brakeChance) f |= FaultFlags.BrakesGone;

            // ExhaustRusted — wiek + zaniedbanie
            double exhaustChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.45 ? 0.30 : 0.08,
                (SellerArchetype.Wrecker, 1) => actual < 0.50 ? 0.35 : 0.12,
                (SellerArchetype.Wrecker, 2) => actual < 0.50 ? 0.45 : 0.18,
                (SellerArchetype.Wrecker, 3) => actual < 0.50 ? 0.55 : 0.25,
                (SellerArchetype.Dealer, _) => actual < 0.35 ? 0.20 : 0.05,  // dealer wymienia tłumik żeby nie słyszałeś
                (SellerArchetype.Scammer, 1) => actual < 0.30 ? 0.28 : 0.10,
                (SellerArchetype.Scammer, 2) => actual < 0.25 ? 0.40 : 0.18,
                (SellerArchetype.Scammer, 3) => 0.50,
                _ => 0.12,
            };
            if (rng.NextDouble() < exhaustChance) f |= FaultFlags.ExhaustRusted;

            // ElectricalFault — typowe dla starszych aut i Wrecker
            double elecChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.45 ? 0.20 : 0.05,
                (SellerArchetype.Wrecker, 1) => actual < 0.50 ? 0.22 : 0.07,
                (SellerArchetype.Wrecker, 2) => actual < 0.50 ? 0.30 : 0.12,
                (SellerArchetype.Wrecker, 3) => actual < 0.50 ? 0.40 : 0.18,
                (SellerArchetype.Dealer, 2) => actual < 0.35 ? 0.22 : 0.08,
                (SellerArchetype.Dealer, 3) => actual < 0.30 ? 0.35 : 0.15,
                (SellerArchetype.Scammer, 1) => actual < 0.25 ? 0.25 : 0.08,
                (SellerArchetype.Scammer, 2) => actual < 0.20 ? 0.38 : 0.15,
                (SellerArchetype.Scammer, 3) => 0.55,
                _ => 0.07,
            };
            if (rng.NextDouble() < elecChance) f |= FaultFlags.ElectricalFault;

            // GlassDamage — widoczna, Dealer i Wrecker L3 ukrywają przez dobre zdjęcia
            double glassChance = (arch, level) switch
            {
                (SellerArchetype.Honest, _) => actual < 0.45 ? 0.25 : 0.05,
                (SellerArchetype.Wrecker, _) => actual < 0.50 ? 0.30 : 0.08,
                (SellerArchetype.Dealer, 1) => actual < 0.40 ? 0.10 : 0.02,  // backyard naprawia szyby żeby wyglądało
                (SellerArchetype.Dealer, 2) => 0.02,
                (SellerArchetype.Dealer, 3) => 0.01,  // criminal: auto wygląda doskonale
                (SellerArchetype.Scammer, 1) => actual < 0.30 ? 0.35 : 0.12,
                (SellerArchetype.Scammer, 2) => actual < 0.25 ? 0.25 : 0.10,
                (SellerArchetype.Scammer, 3) => 0.05,  // expert: czyste szyby, wszystko inne do wymiany
                _ => 0.08,
            };
            if (rng.NextDouble() < glassChance) f |= FaultFlags.GlassDamage;

            return f;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SELEKCJA OPISU
        // ══════════════════════════════════════════════════════════════════════

        private static string SelectNote(SellerArchetype arch, int level, FaultFlags faults, float actual, Random rng)
        {
            if (arch == SellerArchetype.Honest)
            {
                // Priorytet 1: znana poważna usterka — zawsze ujawniana wprost
                foreach (var flag in new[]
                {
                FaultFlags.HeadGasket, FaultFlags.TimingBelt,
                FaultFlags.ElectricalFault, FaultFlags.SuspensionWorn, FaultFlags.BrakesGone
        })
                {
                    if (faults.HasFlag(flag) && NotesHonestFault.TryGetValue(flag, out var specific))
                        return specific;
                }

                // Priorytet 2: auto nie odpala (L1/L2 mogą sprzedawać nieodpalające, L3 nigdy)
                bool likelyNonStarting = actual < 0.12f;
                if (likelyNonStarting && level < 3)
                {
                    string[] pool = level == 1 ? NotesHonestNovice_NonStarting : NotesHonestExperienced_NonStarting;
                    return pool[rng.Next(pool.Length)];
                }

                // Priorytet 3: noty tiered wg kondycji × poziom
                string[] notePool = (level, actual) switch
                {
                    (1, < 0.30f) => NotesHonestNovice_Bad,
                    (1, < 0.60f) => NotesHonestNovice_Mid,
                    (1, _) => NotesHonestNovice_Good,

                    (2, < 0.30f) => NotesHonestExperienced_Bad,
                    (2, < 0.60f) => NotesHonestExperienced_Mid,
                    (2, _) => NotesHonest,                  // istniejąca pula, dobra dla good

                    (3, < 0.60f) => NotesHonestVeteran_Mid,
                    (3, _) => NotesHonestVeteran,           // istniejąca pula

                    _ => NotesHonest,
                };

                return notePool[rng.Next(notePool.Length)];
            }

            // Reszta archetypów — bez zmian
            string[] otherPool = (arch, level) switch
            {
                (SellerArchetype.Wrecker, 1) => NotesWreckerBarnFind,
                (SellerArchetype.Wrecker, 2) => NotesWreckerKnows,
                (SellerArchetype.Wrecker, 3) => NotesWreckerTrader,
                (SellerArchetype.Dealer, 1) => NotesDealer,
                (SellerArchetype.Dealer, 2) => NotesDealerPro,
                (SellerArchetype.Dealer, 3) => NotesDealerCriminal,
                (SellerArchetype.Scammer, 1) => NotesScammer,
                (SellerArchetype.Scammer, 2) => NotesScammerIntermediate,
                (SellerArchetype.Scammer, 3) => NotesScammerExpert,
                _ => NotesWreckerBarnFind,
            };

            return otherPool[rng.Next(otherPool.Length)];
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

        // ── Honest — Non-starting (L1 / L2 wiedzą że auto nie odpala) ─────────────
        private static readonly string[] NotesHonestNovice_NonStarting =
        {
    "Car does not start. Tried jumping it, nothing happened. Selling as-is.",
    "Has not started in a while. No idea what is wrong. Priced cheap for someone who knows.",
    "Engine turns over but will not fire. I give up trying to fix it. Yours if you want it.",
    "Does not run. Was working two years ago then just stopped one day. I am not a mechanic.",
    "Non-runner. I bought it like this thinking I could sort it. I could not. Honest price.",
};

        private static readonly string[] NotesHonestExperienced_NonStarting =
        {
    "Car does not start — fault is known, priced accordingly. Not a hidden surprise.",
    "Non-runner. Has not started since the fault developed. Selling transparently — no games.",
    "Engine cranks but will not fire. I have not diagnosed further. Price reflects a non-runner.",
    "Does not start. Repair estimate was more than the car is worth to me. Someone else's project.",
    "Non-runner, priced as such. Everything I know about the fault is in the listing.",
};

        // ── Honest L1 Novice — Bad (0–30%) ────────────────────────────────────────
        private static readonly string[] NotesHonestNovice_Bad =
        {
    "Starts but barely. Something is wrong, I just cannot figure out what.",
    "Runs, but not great. Has some issues I cannot properly describe.",
    "Drove it a few times and problems kept appearing. Selling before more things go wrong.",
    "Not in good shape but it moves. I tried to price it honestly for what it is.",
    "Makes noises I cannot explain. Not sure if it is serious. Priced low just in case.",
    "Had my uncle look at it and he said it needs work. He did not say what exactly.",
    "Rough around the edges but it has been mostly reliable. Probably needs attention soon.",
};

        // ── Honest L1 Novice — Mid (30–60%) ──────────────────────────────────────
        private static readonly string[] NotesHonestNovice_Mid =
        {
    "Starts and drives fine. Some wear but nothing obvious that needs fixing right now.",
    "Been reliable for me. Not perfect but never let me down. Selling because I got a new one.",
    "Runs well day to day. I am not very car savvy so I cannot say much more than that.",
    "Daily driver for two years. Never broke down. Probably needs a service at some point.",
    "Good enough I think. A mechanic friend said it needs a few small things but nothing urgent.",
    "Not sure what to write. Car works, drives fine, never had big problems with it.",
    "Bought it two years ago from a private sale. Has done me well. Time to move on.",
};

        // ── Honest L1 Novice — Good (60–100%) ─────────────────────────────────────
        private static readonly string[] NotesHonestNovice_Good =
        {
    "Pretty good condition I think. Always started, never gave me trouble.",
    "Selling because I do not need two cars. This one has been really good to me.",
    "I took care of it. Regular oil changes, always garaged. Should be fine for a long time.",
    "Not sure what else to say. Car works, looks decent, just needs a new owner.",
    "My parents bought it new and I inherited it. Kept in good condition as far as I know.",
    "First time selling a car. I think it is in good shape but I could be wrong. Come look.",
    "Never had a problem with it. A bit sad to sell but I need the money.",
};

        // ── Honest L2 Experienced — Bad (0–30%) ───────────────────────────────────
        private static readonly string[] NotesHonestExperienced_Bad =
        {
    "Rough condition — I will not pretend otherwise. Good project car priced to match.",
    "Mechanically tired. Everything I know about its condition is disclosed. No hidden surprises.",
    "Several things need attention. Drives but needs work — price reflects that honestly.",
    "Not in great shape. Full disclosure: it has issues. Priced for what it actually is.",
    "Worn but salvageable. The right buyer with the right tools will get good value here.",
    "I have owned worse and fixed them. This one needs money spent, price set accordingly.",
    "Honest about the condition — it is rough. But it is what it is and the price shows that.",
};

        // ── Honest L2 Experienced — Mid (30–60%) ──────────────────────────────────
        private static readonly string[] NotesHonestExperienced_Mid =
        {
    "Standard condition for the age. Nothing critical needed, routine maintenance due.",
    "Decent runner. A few small things to sort but nothing that will leave you stranded.",
    "Used regularly, serviced when needed. What you see is what you get.",
    "Fair example. Not perfect but everything works as it should. Sensible asking price.",
    "Had it serviced last year. Running well. Selling because I am upgrading.",
    "Solid daily driver. Shows its age a little but mechanically sound where it counts.",
    "Honest private sale. Some wear expected at this mileage. Nothing that surprises me.",
};

        // ── Honest L3 Veteran — Mid (30–60%) ──────────────────────────────────────
        private static readonly string[] NotesHonestVeteran_Mid =
        {
    "Fair condition — I have documented everything done to it. Nothing hidden, price reflects reality.",
    "Not concourse, but maintained properly. Full service history, every receipt. No surprises.",
    "Selling with complete paperwork. Some wear for the age, all accounted for and priced in.",
    "I know what this car needs and the price reflects it. Receipts available on viewing.",
    "Honest assessment: fair condition, fair price. Fifty-odd cars sold and I have never misled a buyer.",
    "Has done good miles. Service history complete. The wear you see is the wear there is — nothing more.",
    "Priced at market for the actual condition, not the aspirational one. Viewing encouraged.",
};




        // ── Honest L2 Experienced — Good (60–100%) — zachowana stara pula ────
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

        // ── Honest L3 Veteran — Good (60–100%) — zachowana stara pula ─────────
        private static readonly string[] NotesHonestVeteran =
        {
            "Fifty-two cars sold over twenty years, all described exactly as they are. This is no different.",
            "Full documented service history, every stamp present. Price reflects genuine condition — not aspirational.",
            "I know what I have and I know what it is worth. Viewing encouraged. Lowballers ignored politely.",
            "If something needs doing I will tell you before you ask. Nothing worse than wasting someone's time.",
            "Genuine sale, no rush, no games. I would rather it go to someone who appreciates it than sell it fast.",
            "Sold with the same honesty I would want if I were buying. Priced at market, not above it.",
        };

        // ── Wrecker L1 — Casual ─────────────────────────────────────────────
        private static readonly string[] NotesWreckerBarnFind =
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

		// ── Wrecker L2 — Busy ───────────────────────────────────────────────
		private static readonly string[] NotesWreckerKnows =
        {
            "Selling quickly, no time for viewings. What you see is what you get, photos are recent enough.",
            "Listed three cars this week. Don't ask me which service was done on which. Price is price.",
            "Works. Drives. Stops. That covers the basics. I have four kids and no time for more detail.",
            "If it needs anything doing you'll find out when you buy it. I certainly haven't checked.",
            "Condition: it exists and moves. Everything else is a discovery process for the new owner.",
            "Selling fast, no negotiation, no sob stories. Collect this week or I relist.",
            "I drove it yesterday and it was fine. Two days ago also fine. Before that I can't recall.",
        };

		// ── Wrecker L3 — Hoarder ───────────────────────────────────────────
		private static readonly string[] NotesWreckerTrader =
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

		// ── Scammer L1 — Amateur ─────────────────────────────────────────────
		private static readonly string[] NotesScammer =
        {
            "Selling fast, no return, cash only, I leave the country on Wednesday.",
            "Small issues but nothing major i think. Price negotiable with Farget gift code.",
            "Please no time wasters. Genuine buyers only. Send bank transfer first for viewing slot.",
            "Good car for the money. Small smoke on start but goes away. Normal for the age.",
            "Warranty provided by me personally. Does not cover engine, gearbox, bodywork, or other parts.",
            "Previous owner was scientist. He drove only to work. His work was four hundred kilometres away.",
            "I will not be reachable after purchase. Not because scam. I am just very busy man.",
        };

		// ── Scammer L2 — Intermediate ─────────────────────────────────────────
		private static readonly string[] NotesScammerIntermediate =
        {
            "Selling on behalf of family member who is currently overseas. They authorised me to handle everything.",
            "Just had a full service done last month — all receipts available at collection.",
            "Minor oil seep from valve cover, normal for age, not a problem. Priced to reflect.",
            "Car is in storage, photos taken this morning. Collection only, no delivery.",
            "Bought at auction, selling privately. No history but runs and drives well.",
            "Owner relocated abroad. I have power of attorney and all relevant paperwork.",
            "Photos are accurate. A few small things to sort but nothing a competent buyer can't handle.",
        };

		// ── Scammer L3 — Expert ───────────────────────────────────────────────
		private static readonly string[] NotesScammerExpert =
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

      


        /// <summary>
        /// Maps carId → array of color names IN THE SAME ORDER as color_map.txt lines.
        /// Index 0 = folder 00_XXXXXX, index 1 = 01_XXXXXX, etc.
        /// These names are used for photo lookup AND for the color swatch in UI (via HexColor).
        /// </summary>
        




    }
}