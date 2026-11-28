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
		public ListingSystem(SellerProfile sellerProfile)
		{
			_sellerProfile = sellerProfile;
		}


		// Auto do wyceny: z wersji w danych gry (MarketCatalog), bez listy aut w kodzie.
		private class CarDef
        {
            public string Make, Model, InternalId;
            public int MinYear, MaxYear, MinPrice, MaxPrice;
        }


        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVE LISTINGS
        // ══════════════════════════════════════════════════════════════════════

        // ── Aktywne ogłoszenia ────────────────────────────────────────────────────
        public List<CarListing> ActiveListings { get; private set; } = new();
        private float _gameTime = 0f;
        private double _gameTimeOrigin = double.NaN;
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

            //OXLLog.Msg($"[OXL:LGSYS:DBG] Tick gameSecs={gameSecs:F1} originNaN={double.IsNaN(_gameTimeOrigin)} isReadingFromTM={GameTimeProvider.IsReadingFromTM}");

            if (double.IsNaN(_gameTimeOrigin))
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
            else if (_originFromRealTM && gameSecs + 60.0 < _gameTimeOrigin)
            {
                // Safety net: origin ended up in the future (bad TM read).
                // Re-anchor to the current (sane) value.
                OXLLog.Msg($"[OXL:LGSYS] Origin was in the future ({_gameTimeOrigin:F1}s), re-anchoring to TM={gameSecs:F1}s");
                _gameTimeOrigin = gameSecs;
                _gameTime = 0f;
                _lastGenCheckTime = 0f;
            }

            _gameTime = (float)(gameSecs - _gameTimeOrigin);
            if (_gameTime < 0f) _gameTime = 0f;

            ApplyPendingRestore();

            if (!_justRestored)  // ← pomiń RemoveAll w ticku restore
            {
                int before = ActiveListings.Count;
                ActiveListings.RemoveAll(l => l.ExpiresAt <= _gameTime);
                if (ActiveListings.Count != before)
                    Save();
            }
            _justRestored = false;

            if (!_initialSeeded)
            {
                // Od razu docelowa liczba aukcji z ustawień (użytkownik 2026-11-26: przy 4 na start widział tylko 5 aukcji).
                int seedCount = _config.MaxListings;
                while (ActiveListings.Count < seedCount && AddGenerated()) { }
                _initialSeeded = true;
                // Zapis od razu: gra zatwierdza tylko pliki już na dysku (site.md 17); bez tego nowa kariera po zapisie gry miała inne auta.
                Save();
                _lastGenCheckTime = _gameTime;
                return;
            }

            if (_gameTime - _lastGenCheckTime >= GenCheckInterval)
            {
                _lastGenCheckTime = _gameTime;
                TryGenerateBatch();
            }
        }

        private bool _justRestored = false;
        private void ApplyPendingRestore()
        {
            if (_pendingRestore == null) return;
            if (double.IsNaN(_gameTimeOrigin)) return;

            var list = new List<CarListing>();
            foreach (var (l, rem) in _pendingRestore)
            {
                if (rem <= 0f) continue;
                float actualRemaining = (float)(rem - _gameTimeOrigin);
                if (actualRemaining <= 0f) continue;
                l.ExpiresAt = _gameTime + actualRemaining;
                list.Add(l);
            }
            ActiveListings = list;
            _lastGenCheckTime = _gameTime;
            _pendingRestore = null;
            _justRestored = true;  // ← flaga
            OXLLog.Msg($"[OXL:LGSYS] Restored {ActiveListings.Count} listings at _gameTime={_gameTime:F1}s");
        }


        /// <summary>Wymusza sprawdzenie generacji przy następnym Tick().</summary>
        public void ForceCheckNow()
        {
            _lastGenCheckTime = _gameTime - GenCheckInterval;
            OXLLog.Msg("[OXL:LGSYS] ForceCheckNow — next Tick will trigger TryGenerateBatch()");
        }

        public void Save() => ListingPersistence.Save(ActiveListings, _gameTime, _gameTimeOrigin);
        private List<(CarListing listing, double remaining)> _pendingRestore = null;


        /// <summary>
        /// Slot zapisu znany (OXLSaveSlot, site.md 17): ogłoszenia aut tego zapisu od zera. Czas gry kotwiczony na nowo (inny zapis = inny czas),
        /// brak pliku = nowa kariera, generator od razu dokłada do celu z ustawień.
        /// </summary>
        public void ReloadForSlot()
        {
            ActiveListings = new List<CarListing>();
            _gameTimeOrigin = double.NaN;
            _originFromRealTM = false;
            _tmWaitTimer = 0f;
            _gameTime = 0f;
            _lastGenCheckTime = 0f;
            _initialSeeded = false;
            _justRestored = false;
            _pendingRestore = null;
            LoadSaved();
        }

        public void LoadSaved()
        {
            // Załaduj RAW — ExpiresAt będzie ustawiane po anchoring
            _pendingRestore = ListingPersistence.LoadRaw();
            if (_pendingRestore == null || _pendingRestore.Count == 0) return;
            _initialSeeded = true;
            OXLLog.Msg($"[OXL:LGSYS] Queued {_pendingRestore.Count} listings for restore after anchor.");
        }



        /// <summary>
        /// Rzut szansą, potem generacja batcha.
        /// Wywoływany co GenCheckInterval lub przez ForceGenerate.
        /// </summary>
        private void TryGenerateBatch()
        {
            if (ActiveListings.Count >= _config.MaxListings) return;
            // System utrzymuje docelową liczbę aukcji (ustawienia OXL): brakujące dokłada od razu, bez rzutu szansą.

            int batchSize = _config.MaxListings - ActiveListings.Count;
            int canAdd = _config.MaxListings - ActiveListings.Count;
            batchSize = UnityEngine.Mathf.Min(batchSize, canAdd);

            OXLLog.Msg($"[OXL:LGSYS] Batch: +{batchSize} (cap={_config.MaxListings} current={ActiveListings.Count})");

            for (int i = 0; i < batchSize; i++)
                if (!AddGenerated()) break;

            Save();  // ← zapis po każdym batchu
        }

        public void ForceGenerate()
        {
            if (ActiveListings.Count >= _config.MaxListings)
            {
                OXLLog.Msg($"[OXL:LGSYS] ForceGenerate skipped — at cap ({_config.MaxListings})");
                return;
            }
            if (!AddGenerated()) return;
            Save();  
        }




        // ══════════════════════════════════════════════════════════════════════
        //  GENEROWANIE LISTINGU
        // ══════════════════════════════════════════════════════════════════════

        // Dodaje nowe ogłoszenie; false, gdy nie ma bezpiecznej wersji auta (pętle generowania się wtedy zatrzymują).
        private bool AddGenerated()
        {
            var l = GenerateListing();
            if (l == null) return false;
            ActiveListings.Add(l);
            return true;
        }

        private CarListing GenerateListing()
        {
            var rng = new Random();

            // ── Auto i wersja: wszystkie bezpieczne wersje z danych gry (MarketCatalog, site.md 9.3), także auta, których mod nie zna ──
            // Gracz zobaczy każde auto gry (w demie 7, w pełnej grze ponad 100) bez zmian w kodzie. Wszystko z danych gry
            // (użytkownik 2026-11-28: bez car_spec.json i listy aut w kodzie); zdjęcia tylko z paczki 2027 (CarPhotoPack), bez niej zdjęcie z gry.
            if (!MarketCatalog.EnsureBuilt()) { OXLPlugin.Log.Warning("[OXL:GEN] Game data not ready; listing skipped."); return null; }
            var versions = MarketCatalog.In(MarketCategory.Cars).ToList();
            if (versions.Count == 0) { OXLPlugin.Log.Warning("[OXL:GEN] No spawnable car version; listing skipped."); return null; }
            var entry = versions[rng.Next(versions.Count)];
            int carConfig = entry.Version;
            int value = Math.Max(1000, entry.Price);
            // Widełki ceny auta z wartości wersji w grze (carValue): używane 10–34% wartości nowego, potem archetyp i stan (RollPrice).
            var def = new CarDef { Make = entry.Make, Model = entry.Model, InternalId = entry.CarId, MinYear = entry.YearMin, MaxYear = Math.Max(entry.YearMin, entry.YearMax), MinPrice = value / 10, MaxPrice = value * 34 / 100 };
            int year = rng.Next(def.MinYear, def.MaxYear + 1);

            // ── Kolor: losowy kolor fabryczny wersji ("#RRGGBB"); paczka zdjęć niżej może go zmienić na kolor ze zdjęć ──
            string color = "white";
            int colorIndex = 0;
            if (entry.Colors != null && entry.Colors.Length > 0)
                color = entry.Colors[rng.Next(entry.Colors.Length)];

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
            int fairValue = Mathf.Max(300, Mathf.RoundToInt(
                Mathf.Lerp(def.MinPrice, def.MaxPrice, actual) * YearFactor(year)
                * OXLSettings.PriceMultiplier / 50f) * 50);

            OXLLog.Msg($"[OXL:PRICE] ── PRICE CALC ──────────────────────────────");
            OXLLog.Msg($"[OXL:PRICE] Car:       {def.Make} {def.Model} {year} (game value ${value:N0})");
            OXLLog.Msg($"[OXL:PRICE] Archetype: {archetype} L{level}");
            OXLLog.Msg($"[OXL:PRICE] Apparent:  {apparent:P0}  Actual: {actual:P0}");
            OXLLog.Msg($"[OXL:PRICE] Price:     ${price:N0}   FairValue: ${fairValue:N0}");
            OXLLog.Msg($"[OXL:PRICE] ─────────────────────────────────────────────");

            // ── TTL — czas życia aukcji ────────────────────────────────────────
            float ttl = RollTTL(archetype, level, rng);

            // ── Rating sprzedawcy ──────────────────────────────────────────────
            float rating = RollSellerRating(archetype, level, rng);

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


            // Paczka zdjęć 2027: kolor z paczki i zdjęcia jednego miejsca w stanie karoserii auta (CarPhotoPack, zasada 5).
            // Bez paczki dla tego auta strona pokazuje zdjęcie z gry ze znaczkiem STOCK PHOTO (site.md 10).
            listing.PhotoFiles = new List<string>();
            CarPhotoPack.TryAssign(listing, entry.CarId, rng);
            // Kłamstwa scammera L2/L3 (scammer.md 3) po ustaleniu koloru, przed opisem, który z nich korzysta.
            ScammerLies.Roll(listing, entry, rng);

            var (avatarTex, nick, avatarPath) = _sellerProfile.Generate(listing, rng);
			listing.SellerNick = nick;
			listing.AvatarPath = avatarPath;

			// ── Nota sprzedawcy ────────────────────────────────────────────────
			listing.SellerNote = SellerNoteBuilder.BuildNote(listing, rng);
			string specs = ScammerLies.SpecsSentence(listing, entry);
			if (specs.Length > 0) listing.SellerNote += "\n\n" + specs;

            OXLLog.Msg($"[OXL:GEN] {def.Make} {def.Model} {year} | Arch={archetype} L{level} | Rating={rating}★ | Apparent={apparent:P0} Actual={actual:P0} Body={bodyCondition:P0} | Price=${price:N0} Fair=${fairValue:N0} ({(fairValue > 0 ? (float)price / fairValue : 0):F2}x) | Faults={faults} | Color={color} | TTL={ttl:F0}s");


            return listing;
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
        //  PRICE: widełki z wartości wersji auta w grze (carValue), archetyp, stan, rocznik i usterki
        // ══════════════════════════════════════════════════════════════════════════
        private static int RollPrice(
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

        private static float RollSellerRating(SellerArchetype arch, int level, Random rng)
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
                (SellerArchetype.Scammer, 3) => rng.Next(0, 10) < 5 ? 3f : 3.5f,      // expert: przeciętna ocena, nie budzi podejrzeń (scammer.md 2.3)

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

        internal static readonly string[] Locations =
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
