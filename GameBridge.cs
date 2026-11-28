// GameBridge.cs
// Spawns a car into the game world and applies wear based on CarListing.
// ClassifyAndWear uses PartCatalog.Classify() — no more string-contains guessing.

using MelonLoader;
using System;
using System.Collections;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class GameBridge
    {
        public enum SpawnResult { Success, NoFreeSlot, NoCarDebug, SpawnFailed }

        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Główna metoda — przyjmuje pełny listing z kondycją i usterkami.</summary>
        public static void SpawnCar(CarListing listing, Action<SpawnResult> onDone)
        {
            MelonCoroutines.Start(DoSpawn(ToGameId(listing.InternalId), listing, onDone));
        }

        /// <summary>Alias dla wstecznej kompatybilności (condition only, no faults).</summary>
        public static void SpawnCar(string internalId, float condition, Action<SpawnResult> onDone)
        {
            var dummy = new CarListing
            {
                InternalId = internalId,
                ActualCondition = condition,
                Faults = FaultFlags.None,
                Archetype = SellerArchetype.Honest,
            };
            MelonCoroutines.Start(DoSpawn(ToGameId(internalId), dummy, onDone));
        }

        // ── Płatności przez bank Redline (BlastCoreOS 0.2.0, bank.md 1 i 3.3) ─────────
        // Strony w przeglądarce płacą z konta w banku, nigdy z portfela gry. Gracz bez środków na koncie dostaje
        // komunikat z adresem banku (wpłata gotówki w bankomacie redline.bank).
        public const string BankModId = "oxl";

        /// <summary>Ile gracz może wydać z konta; -1, gdy bank niegotowy (poza garażem albo zapis gry nieznany).</summary>
        public static long GetBankAvailable()
        {
            try { return BlastCoreOS.BlastCoreBankAPI.IsReady ? BlastCoreOS.BlastCoreBankAPI.GetAvailable() : -1L; }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL] Bank GetAvailable failed: {ex.Message}"); return -1L; }
        }

        /// <summary>Pobiera cenę auta z konta. Wołane dopiero po udanym spawnie (kontrakt kupna). Błąd zawsze w logu.</summary>
        public static bool ChargeForCar(CarListing listing, out string reason)
        {
            reason = "";
            try
            {
                bool ok = BlastCoreOS.BlastCoreBankAPI.TryCharge(BankModId, listing.Price, $"{listing.Make} {listing.Model} {listing.Year}", out reason);
                if (!ok) OXLPlugin.Log.Warning($"[OXL] Bank charge {listing.Price} for {listing.InternalId} refused after delivery: {reason}");
                return ok;
            }
            catch (Exception ex) { reason = ex.Message; OXLPlugin.Log.Warning($"[OXL] Bank charge {listing.Price} failed: {ex.Message}"); return false; }
        }

        // ── Czy wersję auta da się bezpiecznie sprzedać (G12, G48, Docs/Research/Market.md) ─────
        // Bezpieczna = jest w MarketCatalog (silnik, felgi i opony fabryczne w tej wersji gry). Inne spawnują się, ale psują zapis gracza.
        // Gdy dane jeszcze niegotowe: false (lepiej nie sprzedać auta niż zepsuć zapis). Stare ogłoszenia z listings.dat też przez to przechodzą.
        public static bool IsVersionSpawnable(string internalId, int config)
        {
            try { return MarketCatalog.EnsureBuilt() && MarketCatalog.TryGet($"{ToGameId(internalId)}#{config}", out var e) && e.Category == MarketCategory.Cars; }
            catch (Exception ex) { OXLLog.Msg($"[OXL:GEN] IsVersionSpawnable {internalId} cfg={config} failed: {ex.Message}"); return false; }
        }

        public static bool HasFreeSlot()
        {
            try
            {
                return Il2CppCMS.Garage.CarLoaderPlaces.Get().GetPlaceForLoadCar() != null;
            }
            catch
            {
                return System.Linq.Enumerable.Any(GetLoaders(),
                    cl => string.IsNullOrWhiteSpace(cl.CarID) && !cl.IsCarLoaded());
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SPAWN COROUTINE
        // ══════════════════════════════════════════════════════════════════════

        private static IEnumerator DoSpawn(string gameCarId, CarListing listing, Action<SpawnResult> onDone)
        {
            // Miejsce z gry (GetPlaceForLoadCar) potrafi być "wolne", ale nie przyjmować auta (2026-11-28: po zapełnieniu parkingu skryptem
            // dwa puste CarLoadery z IsCarLoaded()=true i done=true ignorowały LoadCar). Dlatego czekamy na CarID, nie na done (done jest
            // już true przed wczytaniem), a gdy miejsce auta nie przyjmie, próbujemy kolejnego pustego. Żadne nie przyjęło = parking pełny.
            var first = Il2CppCMS.Garage.CarLoaderPlaces.Get().GetPlaceForLoadCar();
            if (first == null) { onDone?.Invoke(SpawnResult.NoFreeSlot); yield break; }
            var candidates = new List<Il2CppCMS.Core.Car.CarLoader> { first };
            foreach (var cl in GetLoaders()) if (cl != null && cl.Pointer != first.Pointer && string.IsNullOrWhiteSpace(cl.CarID) && cl.gameObject.GetComponent<Il2Cpp.CarDebug>() != null) candidates.Add(cl);

            Il2CppCMS.Core.Car.CarLoader free = null;
            bool sawDebug = false;
            foreach (var place in candidates)
            {
                var debugComp = place.gameObject.GetComponent<Il2Cpp.CarDebug>();
                if (debugComp == null) continue;
                sawDebug = true;
                OXLLog.Msg($"[OXL:BUY] LoadCar {gameCarId} cfg={listing.CarConfig} on {place.gameObject.name} (loaded={place.IsCarLoaded()} done={place.done})");
                try { debugComp.LoadCar(gameCarId, listing.CarConfig); }
                catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:BUY] LoadCar on {place.gameObject.name} failed: {ex.Message}"); continue; }

                // Wczytanie trwa 0,1–0,4 s (Player.log). 3 s bez CarID = to miejsce auta nie przyjmie.
                float wait = 3f;
                while (string.IsNullOrWhiteSpace(place.CarID) && wait > 0f) { wait -= 0.1f; yield return new WaitForSeconds(0.1f); }
                if (string.IsNullOrWhiteSpace(place.CarID)) { OXLPlugin.Log.Warning($"[OXL:BUY] {place.gameObject.name} did not take the car, trying the next free place."); continue; }
                float timeout = 10f;
                while (!place.done && timeout > 0f) { timeout -= 0.1f; yield return new WaitForSeconds(0.1f); }
                free = place;
                break;
            }
            if (free == null) { onDone?.Invoke(sawDebug ? SpawnResult.NoFreeSlot : SpawnResult.NoCarDebug); yield break; }

            yield return new WaitForFixedUpdate();

            try
            {
                free.ChangePosition(Il2Cpp.CarPlace.Entrance1, true);
                free.GetIl2CppType().GetMethod("SetBonesDone")?.Invoke(free, null);
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Anchor failed: {ex.Message}");
            }

            yield return new WaitForEndOfFrame();

            if (!string.IsNullOrWhiteSpace(free.CarID))
            {
                yield return ApplyWear(free, listing);
                onDone?.Invoke(SpawnResult.Success);
            }
            else
            {
                onDone?.Invoke(SpawnResult.SpawnFailed);
            }
        }

        // ── Helper: kolor lakieru — wydzielony żeby dzielić między ApplyWear i ApplyWreckerWear ──
        private static void TrySetCarColor(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
        {
            try
            {
                var allowedColors = cl.AllowedColors;
                if (allowedColors == null || allowedColors.Count == 0) return;

                Color targetColor = OXLCarMarket.HexColor(listing);
                int bestIdx = 0;
                float bestDist = float.MaxValue;
                for (int ci = 0; ci < allowedColors.Count; ci++)
                {
                    var c = allowedColors[ci].Color;
                    float r = c.r > 1f ? c.r / 255f : c.r;
                    float g = c.g > 1f ? c.g / 255f : c.g;
                    float b = c.b > 1f ? c.b / 255f : c.b;
                    float dist = Mathf.Abs(r - targetColor.r)
                               + Mathf.Abs(g - targetColor.g)
                               + Mathf.Abs(b - targetColor.b);
                    if (dist < bestDist) { bestDist = dist; bestIdx = ci; }
                }
                Il2CppCMS.Core.Car.CarLoaderExtension.SetRandomCarColor(
                    cl, allowedColors[bestIdx], false);
            }
            catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL] SetColor failed: {ex.Message}"); }
        }

        // ── Helper: historia pojazdu (przebieg, rok, tablica) ──────────────────────
        private static void ApplyCarHistory(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
        {
            // 2027: CarInfoData to struktura sieciowa; ref z interopu daje kopię (G1), więc przebieg przez GameCarAccess, rok przez SetYear.
            // Gracz widzi w karcie auta przebieg i rocznik z ogłoszenia.
            try
            {
                if (cl.IsCarLoaded())
                {
                    bool mileageOk = GameCarAccess.TrySetMileage(cl, (uint)Mathf.Max(0, listing.Mileage));
                    cl.SetYear((ushort)Mathf.Clamp(listing.Year, 1960, 2027));
                    GameCarAccess.TryReadCarInfo(cl, out uint m, out ushort y);
                    OXLLog.Msg($"[OXL:WEAR] car info: mileage {listing.Mileage} set={mileageOk} read={m}, year {listing.Year} read={y}");
                }
            }
            catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL] SetCarInfo failed: {ex.Message}"); }
            if (!string.IsNullOrEmpty(listing.Registration))
            {
                try
                {
                    cl.SetNewLicensePlateNumber(listing.Registration, Il2CppCMS.Core.Car.CarLoader.LicensePlateSide.Front);
                    cl.SetNewLicensePlateNumber(listing.Registration, Il2CppCMS.Core.Car.CarLoader.LicensePlateSide.Rear);
                }
                catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL] SetLicensePlate failed: {ex.Message}"); }
            }
        }

        // 2027: części karoserii szuka GameCarAccess (dzieci auta albo scena z filtrem po właścicielu). Log mówi, skąd i ile.
        private static System.Collections.Generic.List<Il2CppCMS.Core.Car.Body.CarBodyPart> GetBodyParts(Il2CppCMS.Core.Car.CarLoader cl)
        {
            var parts = GameCarAccess.GetBodyParts(cl, out string source);
            OXLLog.Msg($"[OXL:WEAR] body parts for {cl.CarID}: {parts.Count} ({source})");
            return parts;
        }

        // Zamiennik SetConditionOnBody z 2026 (w 2027 brak): jedna kondycja na wszystkie blachy, potem pętle per część ją nadpisują.
        private static void SetBodyCondition(Il2CppCMS.Core.Car.CarLoader cl, float value)
        {
            try
            {
                var parts = GetBodyParts(cl);
                for (int i = 0; i < parts.Count; i++) if (parts[i] != null) parts[i].SetCondition(value);
            }
            catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL:WEAR] SetBodyCondition failed: {ex.Message}"); }
        }


        // ══════════════════════════════════════════════════════════════════════
        //  APPLY WEAR
        // ══════════════════════════════════════════════════════════════════════

        private static IEnumerator ApplyWear(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
        {
            // ── Kolor lakieru ─────────────────────────────────────────────────────
            try
            {
                var allowedColors = cl.AllowedColors;
                if (allowedColors != null && allowedColors.Count > 0)
                {
                    Color targetColor = OXLCarMarket.HexColor(listing);
                    int bestIdx = 0;
                    float bestDist = float.MaxValue;
                    for (int ci = 0; ci < allowedColors.Count; ci++)
                    {
                        var c = allowedColors[ci].Color;
                        float r = c.r > 1f ? c.r / 255f : c.r;
                        float g = c.g > 1f ? c.g / 255f : c.g;
                        float b = c.b > 1f ? c.b / 255f : c.b;
                        float dist = Mathf.Abs(r - targetColor.r)
                                   + Mathf.Abs(g - targetColor.g)
                                   + Mathf.Abs(b - targetColor.b);
                        if (dist < bestDist) { bestDist = dist; bestIdx = ci; }
                    }
                    Il2CppCMS.Core.Car.CarLoaderExtension.SetRandomCarColor(
                        cl, allowedColors[bestIdx], false);
                }
            }
            catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL] SetColor failed: {ex.Message}"); }

            yield return new WaitForEndOfFrame();


            // ── SCAMMER — totalny wrak, pomijamy normalną logikę ─────────────────
            if (listing.Archetype == SellerArchetype.Scammer)
            {
                yield return ApplyScammerWear(cl, listing);
                yield break;  // ← nie idzie dalej do normalnego ApplyWear
            }

            // ── WRECKER — wrak po zaniedbaniu, osobna ścieżka ──────────────────
            if (listing.Archetype == SellerArchetype.Wrecker)
            {
                yield return ApplyWreckerWear(cl, listing);
                yield break;
            }

			// ── Honest — osobna ścieżka ─────────
			if (listing.Archetype == SellerArchetype.Honest)
			{
				yield return ApplyHonestWear(cl, listing);
				yield break;
			}


			float actual = Mathf.Clamp(listing.ActualCondition, 0.02f, 1.0f);
            var faults = listing.Faults;
            float mechBase = Mathf.Clamp(actual * UnityEngine.Random.Range(0.82f, 0.98f), 0.01f, 1.0f);
            float bodyBase = Mathf.Clamp(listing.BodyCondition * UnityEngine.Random.Range(0.93f, 1.00f), 0.01f, 1.0f);
            float exhaustableMax = actual > 0.75f ? 0.40f : 0.18f;

            float startFloor = (listing.Archetype, listing.ArchetypeLevel) switch
            {
                (SellerArchetype.Honest, _) => 0.20f,
                (SellerArchetype.Dealer, 1) => 0.40f,
                (SellerArchetype.Dealer, 2) => 0.40f,
                (SellerArchetype.Dealer, 3) => 0.40f,
                _ => 0.0f,
            };

            // ── KROK 1: Karoseria — Dev_RepairAllBody MUSI być przed indexed parts ─
            // Dev_RepairAllBody może resetować część indexed parts (zawieszenie, strukturalne).
            // Dlatego najpierw naprawiamy i ustawiamy karoserię, a indexed parts na końcu.
            cl.RepairAllBodyParts();
            yield return new WaitForEndOfFrame();

            var cp = GetBodyParts(cl);
            if (cp != null)
            {
                for (int i = 0; i < cp.Count; i++)
                {
                    if (cp[i] == null) continue;
                    string name = cp[i].name ?? "";
                    float wear = WearForPart(name, bodyBase, exhaustableMax, 0f, faults, isBodyPart: true);
                    cp[i].SetCondition(wear);

                    if (!IsGlass(name))
                    {
                        float dent = listing.Archetype switch
                        {
                            SellerArchetype.Honest => Mathf.Clamp(1.0f - bodyBase, 0f, 0.80f),
                            SellerArchetype.Wrecker => Mathf.Clamp(1.0f - bodyBase + 0.15f, 0f, 0.90f),
                            SellerArchetype.Dealer => Mathf.Clamp(1.0f - bodyBase, 0f, 0.20f),
                            SellerArchetype.Scammer => UnityEngine.Random.Range(0.65f, 1.0f),
                            _ => Mathf.Clamp(1.0f - bodyBase, 0f, 0.80f),
                        };
                        try { cp[i].Dent = dent * UnityEngine.Random.Range(0.6f, 1.0f); } catch { }

                        float dust = listing.Archetype switch
                        {
                            SellerArchetype.Honest => 0f,
                            SellerArchetype.Wrecker => Mathf.Clamp(1.0f - bodyBase + 0.10f, 0f, 0.85f),
                            SellerArchetype.Dealer => 0f,
                            SellerArchetype.Scammer => UnityEngine.Random.Range(0.70f, 1.0f),
                            _ => 0f,
                        };
                        if (dust > 0f) try { cp[i].Dust = dust; } catch { }
                    }
                }
            }

            SetBodyCondition(cl, bodyBase);
            cl.SetConditionOnDetails(bodyBase);
            if (bodyBase < 0.50f) cl.EnableRustOnAllParts(true);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // ── KROK 2: IndexedParts — PO wszystkich repair calls ─────────────────
            // Dopiero teraz ustawiamy mechanikę. Żaden repair call już po tym nie nastąpi.
            var ip = cl.GetAllPartScriptsCopy();
            if (ip != null)
            {
                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    string id = ip[i].id ?? "";
                    float wear = WearForPart(id, mechBase, exhaustableMax, startFloor, faults, isBodyPart: false);
                    try { ip[i].SetCondition(wear, false); } catch { }
                }
            }
            cl.ClearEnginePartsConditionCache();

            // ── KROK 3: StartFloor — Dealer/Honest muszą mieć odpalające auto ─────
            if (startFloor > 0f)
            {
                var startCritical = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "akumulator", "akumulator_4", "akumulator_5",
            "v8_rozrusznik_1", "r4_rozrusznik_1",
            "alternator", "i6_old_alternator",
            "ecu_1", "ecu_3", "ecu_4",
            "v6_231_listwa_wtryskowa", "v8_350_listwa_wtryskowa",
            "v8_filtr_oleju", "r4_filtr_oleju",
            "filtr_paliwa_1", "pompa_1",
            "v6_231_rozdzielaczZaplonu", "v8_350_rozdzielaczZaplonu",
            "v8_zz_lancuch", "v6_231_pasek",
            "v8_350_lancuch", "v8_350_pasek_1", "v8_350_pasek_2",
            "v8_350_napinacz", "v8_350_rolkaWalka",
            "i4_lancuch",
            "v6_231_pompa_wody", "v8_350_pompa_wody", "r4_pompa_wody",
            "fuseMedium_1", "fuseMedium_2", "fuseMedium_3",
            "relay_1", "relay_2", "relay_3",
            "fuseBox_1_bottom", "fuseBox_1_top",
            "fuseBox_2_bottom", "fuseBox_2_top",
        };

                if (ip != null)
                {
                    for (int i = 0; i < ip.Count; i++)
                    {
                        if (ip[i] == null) continue;
                        if (!startCritical.Contains(ip[i].id ?? "")) continue;
                        if (ip[i].Condition < startFloor)
                        {
                            float g = startFloor + UnityEngine.Random.Range(0f, 0.08f);
                            try { ip[i].SetCondition(g, false); } catch { }
                        }
                    }
                }

                const float EngineFloor = 0.22f;
                var startCats = new HashSet<WearCat>
        {
            WearCat.SparkPlug, WearCat.FilterFuel,
            WearCat.CylinderHead, WearCat.CamValve,
            WearCat.Crankshaft, WearCat.Piston,
            WearCat.Throttle, WearCat.Injector,
            WearCat.Relay, WearCat.Fuse,
            WearCat.TimingChain, WearCat.TimingTensioner, WearCat.TimingRoller,
            WearCat.WaterPump, WearCat.Radiator, WearCat.CoolingFan,
            WearCat.Clutch, WearCat.Flywheel,
        };

                if (ip != null)
                {
                    for (int i = 0; i < ip.Count; i++)
                    {
                        if (ip[i] == null) continue;
                        var cat = PartCatalog.Classify(ip[i].id ?? "");
                        if (!startCats.Contains(cat)) continue;
                        if (ip[i].Condition < EngineFloor)
                        {
                            float b = EngineFloor + UnityEngine.Random.Range(0f, 0.04f);
                            try { ip[i].SetCondition(b, false); } catch { }
                        }
                    }
                }
                cl.ClearEnginePartsConditionCache();
            }

            // ── KROK 4: Chassis — absolutnie ostatni krok ─────────────────────────
            float chassisTarget = listing.Archetype switch
            {
                SellerArchetype.Wrecker => Mathf.Clamp(mechBase * 0.60f, 0.01f, 0.55f),
                SellerArchetype.Scammer => Mathf.Clamp(mechBase * 0.50f, 0.01f, 0.45f),
                SellerArchetype.Dealer => Mathf.Clamp(mechBase * 0.70f, 0.10f, 0.65f),
                _ => Mathf.Clamp(mechBase * 0.90f, 0.05f, 0.90f),
            };

            bool chassisSet = false;
            try
            {
                var carDebug = cl.gameObject.GetComponent<Il2Cpp.CarDebug>();
                if (carDebug != null)
                {
                    carDebug.SetFrameCondition(chassisTarget);
                    chassisSet = true;
                    OXLPlugin.Log.Msg($"[OXL:WEAR] chassis={chassisTarget:P0} via SetFrameCondition");
                }
            }
            catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL:WEAR] SetFrameCondition failed: {ex.Message}"); }

            // ── Historia pojazdu ──────────────────────────────────────────────────
            ApplyCarHistory(cl, listing);

            yield return new WaitForFixedUpdate();

            OXLPlugin.Log.Msg($"[OXL:WEAR] ══ WEAR APPLIED ═══════════════════════════");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Actual={actual:P0} MechBase={mechBase:P0} BodyBase={bodyBase:P0}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Arch={listing.Archetype} L{listing.ArchetypeLevel} StartFloor={startFloor:P0}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Chassis={chassisTarget:P0} set={chassisSet}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Faults={listing.Faults}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] ════════════════════════════════════════════");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  WEAR CALCULATOR — oparty na PartCatalog, bez string-contains
        // ══════════════════════════════════════════════════════════════════════
        // Zamiast 10 metod IsXxx() — jedna ścieżka: Classify → WearCat → float.
        // isBodyPart=true: traktuj jako część karoserii (carParts), nie mechanikę.


        private static IEnumerator ApplyScammerWear(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
        {
            var flags = System.Reflection.BindingFlags.Public
                      | System.Reflection.BindingFlags.Instance
                      | System.Reflection.BindingFlags.NonPublic;

            var dbg = cl.gameObject.GetComponent<Il2Cpp.CarDebug>();

            // ── Krok 1: SalvageCar — bazowa destrukcja ────────────────────────────
            if (dbg != null)
            {
                try
                {
                    dbg.GetType().GetMethod("SalvageCar", flags)?.Invoke(dbg, null);
                    OXLPlugin.Log.Msg("[OXL:WRECKER] SalvageCar() called");
                }
                catch (Exception ex)
                {
                    OXLPlugin.Log.Msg($"[OXL:WRECKER] SalvageCar failed: {ex.Message}");
                }
            }

            yield return new WaitForSeconds(0.2f);

            // ── Krok 2: IndexedParts → 0.02 ──────────────────────────────────────
            var ip = cl.GetAllPartScriptsCopy();
            if (ip != null)
            {
                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    try { ip[i].SetCondition(0.02f, false); }
                    catch { }
                }
                cl.ClearEnginePartsConditionCache();
                OXLPlugin.Log.Msg($"[OXL:WRECKER] {ip.Count} indexedParts → 0.02");
            }

            yield return new WaitForEndOfFrame();

            // ── Krok 3: CarParts → 0.02 ──────────────────────────────────────────
            var cp = GetBodyParts(cl);
            if (cp != null)
            {
                for (int i = 0; i < cp.Count; i++)
                {
                    if (cp[i] == null) continue;
                    try { cp[i].SetCondition(0.02f); } catch { }
                }
                SetBodyCondition(cl, 0.02f);
                cl.SetConditionOnDetails(0.02f);
                OXLPlugin.Log.Msg($"[OXL:WRECKER] {cp.Count} carParts → 0.02");
            }

            yield return new WaitForFixedUpdate();

            // ── Krok 4: Frame + Details → 0.02 ───────────────────────────────────
            if (dbg != null)
            {
                try { dbg.SetFrameCondition(0.02f); } catch { }
                try { dbg.SetDetailsCondition(0.02f); } catch { }
                OXLPlugin.Log.Msg("[OXL:WRECKER] Frame + Details → 0.02");
            }

            // ── Krok 5: DrainFluids ───────────────────────────────────────────────
            if (dbg != null)
            {
                try
                {
                    dbg.GetType()
                       .GetMethod("DrainFluids", flags)
                       ?.Invoke(dbg, new object[] { true });
                    OXLPlugin.Log.Msg("[OXL:WRECKER] DrainFluids called");
                }
                catch (Exception ex)
                {
                    OXLPlugin.Log.Msg($"[OXL:WRECKER] DrainFluids failed: {ex.Message}");
                }
            }

            yield return new WaitForFixedUpdate();

            // ── Krok 6: Historia pojazdu (przebieg, rok, tablica) ─────────────────
            ApplyCarHistory(cl, listing);

            OXLPlugin.Log.Msg(
                $"[OXL:WRECKER] ══ WRECKER WEAR DONE ═══════════════════" +
                $"\n  Arch={listing.Archetype} L{listing.ArchetypeLevel}" +
                $"\n  Apparent={listing.ApparentCondition:P0} Actual={listing.ActualCondition:P0}" +
                $"\n  All parts → 0.02, fluids drained");
        }


		private static IEnumerator ApplyWreckerWear(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
		{
			int level = listing.ArchetypeLevel;

			cl.RepairAllBodyParts();
			yield return new WaitForEndOfFrame();

			// Body: zawsze 0-30% (wrak), ale nie flat 0.02
			float bodyWear = UnityEngine.Random.Range(0.01f, 0.30f);


			SetBodyCondition(cl, bodyWear);
			cl.SetConditionOnDetails(bodyWear);
			cl.EnableRustOnAllParts(true);

			yield return new WaitForFixedUpdate();


			var cp = GetBodyParts(cl);
			if (cp != null)
			{
				for (int i = 0; i < cp.Count; i++)
				{
					if (cp[i] == null) continue;
					string name = cp[i].name ?? "";
					float partWear = UnityEngine.Random.Range(0.01f, 0.30f);
					cp[i].SetCondition(partWear);

					if (!IsGlass(name))
					{
						// Dent tylko jeśli body naprawdę złe — jeśli > 25%, część nie jest wygięta
						if (partWear <= 0.25f)
						{
							float dent = UnityEngine.Random.Range(0.50f, 0.90f) * UnityEngine.Random.Range(0.70f, 1.00f);
							try { cp[i].Dent = dent; } catch { }
						}

						float dust = UnityEngine.Random.Range(0.60f, 0.85f);
						try { cp[i].Dust = dust; } catch { }
					}
				}
			}

			yield return new WaitForFixedUpdate();

			var ip = cl.GetAllPartScriptsCopy();
			if (ip != null)
			{
				if (level == 3)
				{
					// L3 Handlarz: zabrał wszystko co dobre → wyrównane niskie 10-35%
					for (int i = 0; i < ip.Count; i++)
					{
						if (ip[i] == null) continue;
						var cat = PartCatalog.Classify(ip[i].id ?? "");
						if (cat == WearCat.Hardware || cat == WearCat.Structural) continue;
						float wear = UnityEngine.Random.Range(0.10f, 0.35f);
						try { ip[i].SetCondition(wear, false); } catch { }
					}
					OXLLog.Msg($"[OXL:WRECKER-L3] IndexedParts → 10-35% (stripped by trader, no surprises)");
				}
				else if (level == 2)
				{
					// L2 "Zna auto": równomiernie 20-45%, brak pozytywnych niespodzianek
					for (int i = 0; i < ip.Count; i++)
					{
						if (ip[i] == null) continue;
						var cat = PartCatalog.Classify(ip[i].id ?? "");
						if (cat == WearCat.Hardware || cat == WearCat.Structural) continue;
						float wear = UnityEngine.Random.Range(0.20f, 0.45f);
						try { ip[i].SetCondition(wear, false); } catch { }
					}
					OXLLog.Msg($"[OXL:WRECKER-L2] IndexedParts → 20-45% (knows car, cherry-picked parts removed)");
				}
				else
				{
					// L1 "Barn find": rozrzut 20-55% + pozytywne niespodzianki
					int[] indices = new int[ip.Count];
					for (int i = 0; i < ip.Count; i++) indices[i] = i;
					FisherYates(indices);

					for (int i = 0; i < ip.Count; i++)
					{
						if (ip[i] == null) continue;
						var cat = PartCatalog.Classify(ip[i].id ?? "");
						if (cat == WearCat.Hardware || cat == WearCat.Structural) continue;
						float wear = UnityEngine.Random.Range(0.20f, 0.55f);
						try { ip[i].SetCondition(wear, false); } catch { }
					}

					// Pozytywne niespodzianki: 3-5 dużych podzespołów na 55-80%
					int surpriseCount = UnityEngine.Random.Range(3, 6);
					int done = 0;
					WearCat[] surpriseable = {
				WearCat.Gearbox, WearCat.Differential, WearCat.DriveShaft,
				WearCat.EngineBlock, WearCat.Crankshaft, WearCat.CylinderHead,
				WearCat.Shock, WearCat.Subframe, WearCat.Hub
			};
					var surpriseSet = new HashSet<WearCat>(surpriseable);

					for (int k = 0; k < ip.Count && done < surpriseCount; k++)
					{
						int idx = indices[k];
						if (ip[idx] == null) continue;
						var cat = PartCatalog.Classify(ip[idx].id ?? "");
						if (!surpriseSet.Contains(cat)) continue;
						float good = UnityEngine.Random.Range(0.55f, 0.80f);
						try { ip[idx].SetCondition(good, false); } catch { }
						OXLLog.Msg($"[OXL:WRECKER-L1] ★ Barn find surprise: '{ip[idx].id}' → {good:P0}");
						done++;
					}
					OXLLog.Msg($"[OXL:WRECKER-L1] Surprise pass: {done} parts upgraded");
				}

				cl.ClearEnginePartsConditionCache();
			}

			// Chassis: wrak, ale nie zawsze zero
			float chassisWear = level == 3
				? UnityEngine.Random.Range(0.08f, 0.30f)   // handlarz — zabrał co mógł ze struktury też
				: UnityEngine.Random.Range(0.02f, 0.28f);

			try
			{
				var dbg = cl.gameObject.GetComponent<Il2Cpp.CarDebug>();
				if (dbg != null) dbg.SetFrameCondition(chassisWear);
			}
			catch { }

			ApplyCarHistory(cl, listing);
			yield return new WaitForFixedUpdate();
		}

		private static IEnumerator ApplyHonestWear(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
		{
			int level = listing.ArchetypeLevel;
			float actual = Mathf.Clamp(listing.ActualCondition, 0.02f, 1.0f);
			var faults = listing.Faults;

			// Body — zawsze odpowiada actual condition, Honest nie ukrywa stanu
			float bodyBase = Mathf.Clamp(actual * UnityEngine.Random.Range(0.90f, 1.00f), 0.02f, 0.95f);

			cl.RepairAllBodyParts();
			yield return new WaitForEndOfFrame();

			SetBodyCondition(cl, bodyBase);
			cl.SetConditionOnDetails(bodyBase);
			if (bodyBase < 0.50f) cl.EnableRustOnAllParts(true);

			yield return new WaitForFixedUpdate();

			// Karoseria
			var cp = GetBodyParts(cl);
			if (cp != null)
			{
				for (int i = 0; i < cp.Count; i++)
				{
					if (cp[i] == null) continue;
					string name = cp[i].name ?? "";
					float wear = WearForPart(name, bodyBase, 0.40f, 0f, faults, isBodyPart: true);
					cp[i].SetCondition(wear);

					if (!IsGlass(name))
					{
						float dent = Mathf.Clamp(1.0f - bodyBase, 0f, 0.80f);
						try { cp[i].Dent = dent * UnityEngine.Random.Range(0.6f, 1.0f); } catch { }
					}
				}
			}
			
			yield return new WaitForFixedUpdate();

			// IndexedParts — różna logika per level
			var ip = cl.GetAllPartScriptsCopy();
			if (ip != null)
			{
				// Shuffle dla L1 i L3 (niespodzianki)
				int[] indices = new int[ip.Count];
				for (int i = 0; i < ip.Count; i++) indices[i] = i;
				if (level == 1 || level == 3)
					FisherYates(indices);

				for (int i = 0; i < ip.Count; i++)
				{
					if (ip[i] == null) continue;
					string id = ip[i].id ?? "";
					var cat = PartCatalog.Classify(id);
					float wear = HonestWearForCategory(cat, level, actual, faults, i, ip.Count, indices);
					try { ip[i].SetCondition(wear, false); } catch { }
				}

				// L1: pozytywne niespodzianki — 3-5 losowych indexedParts na 70-85%
				if (level == 1)
				{
					int surpriseCount = UnityEngine.Random.Range(3, 6);
					int done = 0;
					for (int k = 0; k < ip.Count && done < surpriseCount; k++)
					{
						int idx = indices[k];
						if (ip[idx] == null) continue;
						var cat = PartCatalog.Classify(ip[idx].id ?? "");
						if (cat == WearCat.Hardware || cat == WearCat.Structural) continue;
						if (cat == WearCat.SparkPlug || cat == WearCat.FilterOil ||
							cat == WearCat.FilterFuel || cat == WearCat.FilterAir) continue;
						float good = UnityEngine.Random.Range(0.70f, 0.85f);
						try { ip[idx].SetCondition(good, false); } catch { }
						OXLLog.Msg($"[OXL:HONEST-L1] ★ Positive surprise: '{ip[idx].id}' → {good:P0}");
						done++;
					}
				}

				// L2/L3: jedna negatywna niespodzianka (jeden system gorszy niż reszta)
				if (level == 2 || level == 3)
				{
					// Losuj jeden WearCat (nie hardware/structural) i obniż go o 20-35%
					WearCat[] surpriseCats = {
				WearCat.Shock, WearCat.Clutch, WearCat.TimingChain,
				WearCat.BrakeFriction, WearCat.Alternator, WearCat.WaterPump
			};
					WearCat badCat = surpriseCats[UnityEngine.Random.Range(0, surpriseCats.Length)];
					// L3 może mieć 1-3 złe systemy
					int badCount = level == 3 ? UnityEngine.Random.Range(1, 4) : 1;
					int applied = 0;
					for (int i = 0; i < ip.Count && applied < badCount; i++)
					{
						if (ip[i] == null) continue;
						if (PartCatalog.Classify(ip[i].id ?? "") != badCat) continue;
						float current = ip[i].Condition;
						float penalty = UnityEngine.Random.Range(0.20f, 0.35f);
						float bad = Mathf.Max(0.05f, current - penalty);
						try { ip[i].SetCondition(bad, false); } catch { }
						applied++;
					}
				}


                const float HonestStartFloor = 0.22f;
                // Krok 1: explicit ID-based (akumulator, rozrusznik, ECU, rozrząd, pompa wody)
                var honestStartCritical = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "akumulator", "akumulator_4", "akumulator_5",
    "v8_rozrusznik_1", "r4_rozrusznik_1",
    "alternator", "i6_old_alternator",
    "ecu_1", "ecu_3", "ecu_4",
    "v6_231_listwa_wtryskowa", "v8_350_listwa_wtryskowa",
    "v8_filtr_oleju", "r4_filtr_oleju",
    "filtr_paliwa_1", "pompa_1",
    "v6_231_rozdzielaczZaplonu", "v8_350_rozdzielaczZaplonu",
    "v8_zz_lancuch", "v6_231_pasek",
    "v8_350_lancuch", "v8_350_pasek_1", "v8_350_pasek_2",
    "v8_350_napinacz", "v8_350_rolkaWalka",
    "i4_lancuch",
    "v6_231_pompa_wody", "v8_350_pompa_wody", "r4_pompa_wody",
    "fuseMedium_1", "fuseMedium_2", "fuseMedium_3",
    "relay_1", "relay_2", "relay_3",
    "fuseBox_1_bottom", "fuseBox_1_top",
    "fuseBox_2_bottom", "fuseBox_2_top",
};

                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    if (!honestStartCritical.Contains(ip[i].id ?? "")) continue;
                    if (ip[i].Condition < HonestStartFloor)
                    {
                        float g = HonestStartFloor + UnityEngine.Random.Range(0f, 0.08f);
                        try { ip[i].SetCondition(g, false); } catch { }
                    }
                }

                // Krok 2: category-based (głowica, wał, tłoki, przepustnica, wtrysk, cewka)
                var honestStartCats = new HashSet<WearCat>
{
    // ── Silnik — blok, wał, tłoki, głowica, wałek ────────────────────────
    WearCat.EngineBlock,        // ← DODANE
    WearCat.Crankshaft,
    WearCat.Piston,
    WearCat.CylinderHead,
    WearCat.CamValve,

    // ── Rozrząd ───────────────────────────────────────────────────────────
    WearCat.TimingChain,
    WearCat.TimingTensioner,
    WearCat.TimingRoller,

    // ── Zapłon / wtrysk ───────────────────────────────────────────────────
    WearCat.SparkPlug,          
    WearCat.IgnitionCoil,
    WearCat.Distributor,
    WearCat.Injector,
    WearCat.Throttle,

    // ── Paliwo ────────────────────────────────────────────────────────────
    WearCat.FuelPump,          

    // ── Elektryka ─────────────────────────────────────────────────────────
    WearCat.Battery,
    WearCat.Starter,
    WearCat.Alternator,
    WearCat.Ecu,
    WearCat.Relay,
    WearCat.Fuse,

    // ── Chłodzenie (przegrzanie = silnik nie pracuje) ─────────────────────
    WearCat.WaterPump,
    WearCat.Radiator,
    WearCat.CoolingFan,

    // ── Sprzęgło (bez niego auto nie ruszy z miejsca) ─────────────────────
    WearCat.Clutch,
    WearCat.Flywheel,

    // ── Skrzynia biegów (bez niej auto nie ruszy) ─────────────────────────
    WearCat.Gearbox,           
};

                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    var cat = PartCatalog.Classify(ip[i].id ?? "");
                    if (!honestStartCats.Contains(cat)) continue; 

                    bool isTimingPart = cat == WearCat.TimingChain
                                     || cat == WearCat.TimingTensioner
                                     || cat == WearCat.TimingRoller;
                    if (isTimingPart && faults.HasFlag(FaultFlags.TimingBelt)) continue;

                    if (cat == WearCat.CylinderHead && faults.HasFlag(FaultFlags.HeadGasket)) continue;

                    if (ip[i].Condition < HonestStartFloor)
                    {
                        float g = HonestStartFloor + UnityEngine.Random.Range(0f, 0.06f);
                        try { ip[i].SetCondition(g, false); } catch { }
                        OXLLog.Msg($"[OXL:HONEST] startFloor: '{ip[i].id}' → {g:P0}");
                    }
                }

                cl.ClearEnginePartsConditionCache();
            }

			// Chassis
			float chassisTarget = Mathf.Clamp(actual * UnityEngine.Random.Range(0.85f, 1.0f), 0.05f, 0.95f);
			try
			{
				var dbg = cl.gameObject.GetComponent<Il2Cpp.CarDebug>();
				if (dbg != null) dbg.SetFrameCondition(chassisTarget);
			}
			catch { }

			ApplyCarHistory(cl, listing);
			yield return new WaitForFixedUpdate();
		}


		private static float WearForPart(string partId, float baseCondition, float exhaustableMax,float startFloor, FaultFlags faults, bool isBodyPart)
        {
            if (isBodyPart && IsGlass(partId))
            {
                if (faults.HasFlag(FaultFlags.GlassDamage))
                    return UnityEngine.Random.value < 0.40f
                        ? UnityEngine.Random.Range(0f, 0.08f)
                        : baseCondition;
                return baseCondition;
            }

            // ── Opony i felgi — nie ma ich w PartCatalog, obsługujemy tu ─────────
            // Opony zużywają się jak materiały eksploatacyjne.
            // Felgi są trwalsze ale też ulegają zużyciu.
            string idLow = (partId ?? "").ToLower();
            if (idLow.StartsWith("tire_") || idLow.StartsWith("tire "))
                return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.30f, 0.80f),
                           Mathf.Max(0.01f, baseCondition * 0.20f), 0.85f);
            if (idLow.StartsWith("rim_"))
                return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.95f),
                           Mathf.Max(0.01f, baseCondition * 0.30f), 0.95f);

            WearCat cat = PartCatalog.Classify(partId);

            if (cat == WearCat.Hardware || cat == WearCat.Structural)
            {
                if (isBodyPart)
                    return Mathf.Clamp(
                        baseCondition * UnityEngine.Random.Range(0.80f, 1.00f),
                        Mathf.Max(0.01f, baseCondition * 0.30f),
                        1.0f);
                return 1.0f;
            }

            // Dynamiczny floor — skaluje się z baseCondition zamiast być stały.
            // Przy actual=2%: floor≈0.01. Przy actual=50%: floor≈0.15. Przy actual=90%: floor≈0.27.
            // Zapobiega sytuacji gdzie wrak ma części na 30% tylko przez stałe clamp minimum.
            float dynFloor = Mathf.Max(0.01f, baseCondition * 0.30f);
            float dynFloorHigh = Mathf.Max(0.01f, baseCondition * 0.40f); // dla trwalszych części

            return cat switch
            {
                // ── Materiały eksploatacyjne ──────────────────────────────────────────
                WearCat.SparkPlug => Mathf.Max(startFloor,
                                           UnityEngine.Random.Range(0f, exhaustableMax)),
                WearCat.FilterOil => Mathf.Max(startFloor * 0.5f,
                                           UnityEngine.Random.Range(0f, exhaustableMax)),
                WearCat.FilterFuel => Mathf.Max(startFloor * 0.6f,
                                           UnityEngine.Random.Range(0f, exhaustableMax)),
                WearCat.FilterAir => Mathf.Max(startFloor * 0.6f,
                                           UnityEngine.Random.Range(0f, exhaustableMax * 1.5f)),

                // ── Rozrząd ───────────────────────────────────────────────────────────
                WearCat.TimingChain => faults.HasFlag(FaultFlags.TimingBelt)
                    ? Mathf.Max(startFloor,
                           UnityEngine.Random.Range(0f, 0.12f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.80f),
                           dynFloor, 0.80f),

                WearCat.TimingTensioner => faults.HasFlag(FaultFlags.TimingBelt)
                    ? Mathf.Max(startFloor * 0.80f,
                           UnityEngine.Random.Range(0f, 0.10f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.75f),
                           dynFloor, 0.75f),

                WearCat.TimingRoller => faults.HasFlag(FaultFlags.TimingBelt)
                    ? Mathf.Max(startFloor * 0.80f,
                           UnityEngine.Random.Range(0f, 0.10f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.78f),
                           dynFloor, 0.78f),

                // ── Hamulce ───────────────────────────────────────────────────────────
                WearCat.BrakeFriction => faults.HasFlag(FaultFlags.BrakesGone)
                    ? UnityEngine.Random.Range(0f, 0.05f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.30f, 0.70f),
                           dynFloor, 0.70f),

                WearCat.BrakeDisc => faults.HasFlag(FaultFlags.BrakesGone)
                    ? UnityEngine.Random.Range(0.02f, 0.20f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.85f),
                           dynFloor, 0.85f),

                WearCat.BrakeCaliper => faults.HasFlag(FaultFlags.BrakesGone)
                    ? UnityEngine.Random.Range(0.05f, 0.30f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f),
                           dynFloor, 1.0f),

                WearCat.BrakeBooster => faults.HasFlag(FaultFlags.BrakesGone)
                    ? UnityEngine.Random.Range(0.10f, 0.45f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f),
                           dynFloor, 1.0f),

                WearCat.AbsSystem => faults.HasFlag(FaultFlags.BrakesGone)
                    ? UnityEngine.Random.Range(0.05f, 0.35f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f),
                           dynFloor, 1.0f),

                // ── Zawieszenie ───────────────────────────────────────────────────────
                WearCat.Shock => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? UnityEngine.Random.Range(0f, 0.20f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.80f),
                           dynFloor, 0.80f),

                WearCat.Spring => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? UnityEngine.Random.Range(0.02f, 0.35f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                           dynFloor, 0.90f),

                WearCat.Bushing => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? Mathf.Max(startFloor * 0.1f,
                           UnityEngine.Random.Range(0f, 0.25f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.35f, 0.75f),
                           dynFloor, 0.75f),

                WearCat.Wishbone => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? UnityEngine.Random.Range(0.02f, 0.35f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                           dynFloor, 0.90f),

                WearCat.Stabilizer => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? UnityEngine.Random.Range(0f, 0.30f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.45f, 0.85f),
                           dynFloor, 0.85f),

                WearCat.Steering => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? UnityEngine.Random.Range(0f, 0.25f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                           dynFloor, 0.90f),

                // Knuckle/Hub/Subframe — trwałe, ale nie odporne na skrajnie złe actual
                WearCat.Knuckle => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f),
                                        dynFloorHigh, 1.0f),
                WearCat.Hub => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f),
                                        dynFloorHigh, 1.0f),
                WearCat.Bearing => faults.HasFlag(FaultFlags.SuspensionWorn)
                    ? UnityEngine.Random.Range(0f, 0.20f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.85f),
                           dynFloor, 0.85f),
                WearCat.Subframe => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.80f, 1.0f),
                                        dynFloorHigh, 1.0f),

                // ── Sprzęgło ─────────────────────────────────────────────────────────
                WearCat.Clutch => Mathf.Max(startFloor,
                                        Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.30f, 0.75f),
                                            dynFloor, 0.75f)),
                WearCat.Flywheel => Mathf.Max(startFloor,
                                        Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f),
                                            dynFloorHigh, 1.0f)),

                // ── Silnik ────────────────────────────────────────────────────────────
                WearCat.EngineBlock => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f),
                                            dynFloorHigh, 1.0f),
                WearCat.Crankshaft => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 0.98f),
                                            dynFloorHigh, 1.0f),
                WearCat.Piston => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f),
                                            dynFloor, 0.95f),

                WearCat.CylinderHead => faults.HasFlag(FaultFlags.HeadGasket)
                    ? Mathf.Max(startFloor * 0.75f,
                           UnityEngine.Random.Range(0f, 0.08f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 0.95f),
                           dynFloorHigh, 0.95f),

                WearCat.CamValve => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.92f),
                                        dynFloor, 0.92f),

                // ── Układ wydechowy ───────────────────────────────────────────────────
                WearCat.ExhaustPipe => faults.HasFlag(FaultFlags.ExhaustRusted)
                    ? Mathf.Max(startFloor * 0.5f,
                           UnityEngine.Random.Range(0f, 0.18f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.80f),
                           dynFloor, 0.80f),

                WearCat.ExhaustManifold => faults.HasFlag(FaultFlags.ExhaustRusted)
                    ? Mathf.Max(startFloor * 0.5f,
                           UnityEngine.Random.Range(0.02f, 0.25f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                           dynFloor, 0.90f),

                WearCat.Muffler => faults.HasFlag(FaultFlags.ExhaustRusted)
                    ? Mathf.Max(startFloor * 0.4f,
                           UnityEngine.Random.Range(0f, 0.12f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.45f, 0.85f),
                           dynFloor, 0.85f),

                WearCat.Catalyst => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.85f),
                                        dynFloor, 0.85f),

                // ── Chłodzenie ────────────────────────────────────────────────────────
                WearCat.Radiator => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.95f),
                                           dynFloor, 0.95f),
                WearCat.CoolingFan => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 1.0f),
                                           dynFloor, 1.0f),
                WearCat.WaterPump => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.90f),
                                           dynFloor, 0.90f),
                WearCat.CoolantSystem => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f),
                                           dynFloor, 1.0f),

                // ── Elektryka ─────────────────────────────────────────────────────────
                WearCat.Alternator => Mathf.Max(startFloor,
                    faults.HasFlag(FaultFlags.ElectricalFault)
                        ? UnityEngine.Random.Range(0f, 0.20f)
                        : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 1.0f),
                              dynFloor, 1.0f)),

                WearCat.Battery => Mathf.Max(startFloor + 0.05f,
                    faults.HasFlag(FaultFlags.ElectricalFault)
                        ? UnityEngine.Random.Range(0f, 0.15f)
                        : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.85f),
                              dynFloor, 0.85f)),

                WearCat.Starter => Mathf.Max(startFloor + 0.08f,
                    faults.HasFlag(FaultFlags.ElectricalFault)
                        ? UnityEngine.Random.Range(0.02f, 0.30f)
                        : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f),
                              dynFloor, 1.0f)),

                WearCat.Ecu => Mathf.Max(startFloor,
                    faults.HasFlag(FaultFlags.ElectricalFault)
                        ? UnityEngine.Random.Range(0f, 0.20f)
                        : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f),
                              dynFloor, 1.0f)),

                WearCat.Relay => faults.HasFlag(FaultFlags.ElectricalFault)
                    ? UnityEngine.Random.Range(0f, 0.25f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f),
                           dynFloor, 1.0f),

                WearCat.Fuse => faults.HasFlag(FaultFlags.ElectricalFault)
                    ? UnityEngine.Random.Range(0f, 0.20f)
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f),
                           dynFloor, 1.0f),

                WearCat.IgnitionCoil => Mathf.Max(startFloor,
                    faults.HasFlag(FaultFlags.ElectricalFault)
                        ? UnityEngine.Random.Range(0f, 0.18f)
                        : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                              dynFloor, 0.90f)),

                WearCat.Distributor => Mathf.Max(startFloor,
                    faults.HasFlag(FaultFlags.ElectricalFault)
                        ? UnityEngine.Random.Range(0f, 0.20f)
                        : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                              dynFloor, 0.90f)),

                // ── Napęd ─────────────────────────────────────────────────────────────
                WearCat.Gearbox => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f),
                                            dynFloor, 0.95f),
                WearCat.DriveShaft => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.92f),
                                            dynFloor, 0.92f),
                WearCat.Differential => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f),
                                            dynFloor, 1.0f),
                WearCat.TransferCase => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f),
                                            dynFloor, 1.0f),

                // ── Układ dolotowy / paliwowy ─────────────────────────────────────────
                WearCat.Intake => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f),
                                        dynFloor, 1.0f),
                WearCat.Throttle => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f),
                                        dynFloor, 0.95f),
                WearCat.Injector => Mathf.Max(startFloor * 0.4f,
                                        Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.45f, 0.88f),
                                            dynFloor, 0.88f)),
                WearCat.FuelPump => Mathf.Max(startFloor,
                                        Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.90f),
                                            dynFloor, 0.90f)),
                WearCat.FuelTank => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f),
                                        dynFloor, 1.0f),
                WearCat.Turbo => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.85f),
                                        dynFloor, 0.85f),

                // ── Wspomaganie ───────────────────────────────────────────────────────
                WearCat.PowerSteering => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f),
                                             dynFloor, 0.90f),

                // ── Fallback ──────────────────────────────────────────────────────────
                _ => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.85f, 1.0f),
                         dynFloor, 1.0f),
            };
        }

		// ══════════════════════════════════════════════════════════════════════
		//  HELPERS
		// ══════════════════════════════════════════════════════════════════════

		private static float HonestWearForCategory(WearCat cat, int level,float actual, FaultFlags faults, int idx, int total, int[] shuffled)
		{
			if (cat == WearCat.Hardware || cat == WearCat.Structural) return 1.0f;

			float dynFloor = Mathf.Max(0.01f, actual * 0.25f);

			bool isConsumable = cat == WearCat.SparkPlug || cat == WearCat.FilterOil ||
								cat == WearCat.FilterFuel || cat == WearCat.FilterAir;

			// Materiały eksploatacyjne — zawsze złe (wszystkie levele)
			if (isConsumable)
				return UnityEngine.Random.Range(0.05f, 0.25f);

			// Zakresy mechBase per level
			float lo, hi;
			switch (level)
			{
				case 1: lo = 0.35f; hi = 0.75f; break;  // szeroki rozrzut
				case 2: lo = 0.35f; hi = 0.60f; break;  // równomierny, bez zaskoczeń
				default: lo = 0.35f; hi = 0.50f; break;  // L3: wąski, przewidywalny
			}

			float base_ = actual * UnityEngine.Random.Range(lo, hi);
			return Mathf.Clamp(base_, dynFloor, hi + 0.05f);
		}


		private static void FisherYates(int[] arr)
		{
			for (int i = arr.Length - 1; i > 0; i--)
			{
				int j = UnityEngine.Random.Range(0, i + 1);
				(arr[i], arr[j]) = (arr[j], arr[i]);
			}
		}


		/// <summary>Czy część karoserii to szkło/optyka — obsługiwane binarnie, nie przez PartCatalog.</summary>
		private static bool IsGlass(string id)
        {
            string low = id.ToLower();
            return low.Contains("window") || low.Contains("mirror") || low.Contains("headlight") ||
                   low.Contains("taillight") || low.Contains("szyba") || low.Contains("lusterko") ||
                   low.Contains("reflektor") || low.Contains("lamp") || low.Contains("glass");
        }

        private static Il2CppCMS.Core.Car.CarLoader[] GetLoaders()
        {
            var type = Il2CppInterop.Runtime.Il2CppType.Of<Il2CppCMS.Core.Car.CarLoader>();
            return System.Linq.Enumerable.ToArray(
                System.Linq.Enumerable.Select(
                    UnityEngine.Object.FindObjectsOfType(type, true),
                    x => x.TryCast<Il2CppCMS.Core.Car.CarLoader>()));
        }

        // InternalId ogłoszenia = "{id auta w grze}_{4 cyfry}", np. "car_boltnevada_4821" (tylko dane gry, bez mapy aut 2026; 2026-11-28).
        internal static string ToGameId(string internalId)
        {
            if (string.IsNullOrEmpty(internalId)) return "";
            int us = internalId.LastIndexOf('_');
            if (us > 0 && internalId.Length - us - 1 == 4 && int.TryParse(internalId.Substring(us + 1), out _)) return internalId.Substring(0, us);
            return internalId;
        }
    }
}