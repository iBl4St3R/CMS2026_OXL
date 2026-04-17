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

        public static void DeductMoney(int amount)
        {
            try { Il2CppCMS.Shared.SharedGameDataManager.Instance.AddMoneyRpc(-amount); }
            catch { }
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
                    cl => string.IsNullOrWhiteSpace(cl.CarID) && !cl.modelLoaded);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SPAWN COROUTINE
        // ══════════════════════════════════════════════════════════════════════

        private static IEnumerator DoSpawn(string gameCarId, CarListing listing, Action<SpawnResult> onDone)
        {
            var free = Il2CppCMS.Garage.CarLoaderPlaces.Get().GetPlaceForLoadCar();
            if (free == null) { onDone?.Invoke(SpawnResult.NoFreeSlot); yield break; }

            try { free.GetIl2CppType().GetMethod("UnloadCar")?.Invoke(free, null); }
            catch { }

            yield return new WaitForSeconds(0.2f);
            yield return new WaitForFixedUpdate();

            var debugComp = free.gameObject.GetComponent<Il2Cpp.CarDebug>();
            if (debugComp == null) { onDone?.Invoke(SpawnResult.NoCarDebug); yield break; }

            debugComp.LoadCar(gameCarId, gameCarId == "car_mayenm5" ? 1 : 0);

            float timeout = 10f;
            while (!free.done && timeout > 0f)
            {
                timeout -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

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

        // ══════════════════════════════════════════════════════════════════════
        //  APPLY WEAR
        // ══════════════════════════════════════════════════════════════════════

        private static IEnumerator ApplyWear(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
        {
            // ── Kolor lakieru ─────────────────────────────────────────────────
            try
            {
                var allowedColors = cl.AllowedColors;
                if (allowedColors != null && allowedColors.Count > 0)
                {
                    Color targetColor = OXLPanel.HexColor(listing.Color); // potrzebujesz public

                    // Znajdź closest color z AllowedColors
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

            float actual = Mathf.Clamp(listing.ActualCondition, 0.02f, 1.0f);
            var faults = listing.Faults;

            float mechBase = Mathf.Clamp(actual * UnityEngine.Random.Range(0.82f, 0.98f), 0.02f, 1.0f);
            // bodyBase pochodzi z BodyCondition — dla Dealera jest wysoki niezależnie od mechaniki
            float bodyBase = Mathf.Clamp(listing.BodyCondition * UnityEngine.Random.Range(0.93f, 1.00f), 0.02f, 1.0f);
            float exhaustableMax = actual > 0.75f ? 0.40f : 0.18f;

            // Dealer przygotowuje auto do jazdy próbnej — krytyczne części startowe mają minimalny floor
            float startFloor = (listing.Archetype, listing.ArchetypeLevel) switch
            {
                (SellerArchetype.Dealer, 1) => 0.40f,
                (SellerArchetype.Dealer, 2) => 0.40f,
                (SellerArchetype.Dealer, 3) => 0.40f,
                _ => 0.0f,
            };


            // ── IndexedParts — mechanika ──────────────────────────────────────
            var ip = cl.indexedParts;
            if (ip != null)
            {
                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    string id = ip[i].id ?? "";
                    float wear = WearForPart(id, mechBase, exhaustableMax, startFloor, faults, isBodyPart: false);
                    try { ip[i].SetCondition(wear); } catch { ip[i].condition = wear; }
                }
            }
            cl.ClearEnginePartsConditionCache();


            if (listing.Archetype == SellerArchetype.Dealer && startFloor > 0f)
            {
                var startCritical = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    // Akumulator / rozrusznik / alternator
    "akumulator", "akumulator_4", "akumulator_5",
    "v8_rozrusznik_1", "r4_rozrusznik_1",
    "alternator", "i6_old_alternator",
    // ECU
    "ecu_1", "ecu_3", "ecu_4",
    // Wtrysk
    "v6_231_listwa_wtryskowa",
    "v8_350_listwa_wtryskowa",
    // Filtry oleju
    "v8_filtr_oleju", "r4_filtr_oleju",
    // Pompa paliwa
    "pompa_1",
    // Rozdzielacze
    "v6_231_rozdzielaczZaplonu",
    "v8_350_rozdzielaczZaplonu",
    // Rozrząd V6/231 + V8_ZZ (Salem, DNB)
    "v8_zz_lancuch", "v6_231_pasek",
    // Rozrząd V8/350 (Luxor) — NOWE
    "v8_350_lancuch",
    "v8_350_pasek_1", "v8_350_pasek_2",
    "v8_350_napinacz",
    "v8_350_rolkaWalka",
    // Rozrząd inne
    "i4_lancuch",
    // Pompy wody
    "v6_231_pompa_wody",
    "v8_350_pompa_wody",
    "r4_pompa_wody",
    // Bezpieczniki i przekaźniki (ElectricalFault) — NOWE
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
                        string id = ip[i].id ?? "";
                        if (!startCritical.Contains(id)) continue;

                        float current = ip[i].condition;
                        float minVal = startFloor;

                        if (current < minVal)
                        {
                            float guaranteed = minVal +
                                UnityEngine.Random.Range(0f, 0.08f);
                            try { ip[i].SetCondition(guaranteed); }
                            catch { ip[i].condition = guaranteed; }
                        }
                    }
                }

                // ── Pass 2: category-based engine floor ──────────────────────
                const float EngineFloor = 0.22f;
                var startCats = new HashSet<WearCat>
                {
                    WearCat.CylinderHead,
                    WearCat.CamValve,
                    WearCat.Crankshaft,
                    WearCat.Piston,
                    WearCat.Throttle,
                    WearCat.Injector,
                    WearCat.Relay,
                    WearCat.Fuse,
                    WearCat.TimingChain,
                    WearCat.TimingTensioner,
                    WearCat.TimingRoller,
                    WearCat.WaterPump,
                    WearCat.Radiator,
                    WearCat.CoolingFan,
                    WearCat.Clutch,
                    WearCat.Flywheel,
                };
                if (ip != null)
                {
                    for (int i = 0; i < ip.Count; i++)
                    {
                        if (ip[i] == null) continue;
                        var cat = PartCatalog.Classify(ip[i].id ?? "");
                        if (!startCats.Contains(cat)) continue;
                        if (ip[i].condition < EngineFloor)
                        {
                            float boosted = EngineFloor + UnityEngine.Random.Range(0f, 0.04f);
                            try { ip[i].SetCondition(boosted); }
                            catch { ip[i].condition = boosted; }
                        }
                    }
                }
                cl.ClearEnginePartsConditionCache();
            }

            // ── CarParts — karoseria + wizualia ───────────────────────────────
            cl.Dev_RepairAllBody();
            yield return new WaitForEndOfFrame();

            var cp = cl.carParts;
            if (cp != null)
            {
                for (int i = 0; i < cp.Count; i++)
                {
                    if (cp[i] == null) continue;
                    string name = cp[i].name ?? "";
                    float wear = WearForPart(name, bodyBase, exhaustableMax, 0f, faults, isBodyPart: true);
                    cl.SetCondition(cp[i], wear);

                    if (!IsGlass(name))
                    {
                        float dent = listing.Archetype switch
                        {
                            SellerArchetype.Honest => Mathf.Clamp(1.0f - bodyBase, 0f, 0.80f),
                            SellerArchetype.Neglected => Mathf.Clamp(1.0f - bodyBase + 0.15f, 0f, 0.90f),
                            SellerArchetype.Dealer => Mathf.Clamp(1.0f - bodyBase, 0f, 0.20f),
                            SellerArchetype.Wrecker => UnityEngine.Random.Range(0.65f, 1.0f),
                            _ => Mathf.Clamp(1.0f - bodyBase, 0f, 0.80f),
                        };
                        try { cl.SetDent(cp[i], dent * UnityEngine.Random.Range(0.6f, 1.0f)); } catch { }

                        float dust = listing.Archetype switch
                        {
                            SellerArchetype.Honest => 0f,
                            SellerArchetype.Neglected => Mathf.Clamp(1.0f - bodyBase + 0.10f, 0f, 0.85f),
                            SellerArchetype.Dealer => 0f,
                            SellerArchetype.Wrecker => UnityEngine.Random.Range(0.70f, 1.0f),
                            _ => 0f,
                        };
                        if (dust > 0f) try { cl.EnableDust(cp[i], dust); } catch { }
                    }
                }
            }

            cl.SetConditionOnBody(bodyBase);
            cl.SetConditionOnDetails(bodyBase);
            if (bodyBase < 0.50f) cl.SwitchRusted(bodyBase);

            yield return new WaitForFixedUpdate();
            try { cl.UpdateCarBodyParts(); } catch { }
            try { cl.SetupCarSupport(); } catch { }
            yield return new WaitForFixedUpdate();

            // ── Historia pojazdu ──────────────────────────────────────────────
            var cid = cl.CarInfoData;
            if (cid != null)
            {
                cid.Mileage = (uint)Mathf.Max(0, listing.Mileage);
                cid.Year = (ushort)Mathf.Clamp(listing.Year, 1960, 2025);
            }

            if (!string.IsNullOrEmpty(listing.Registration))
            {
                try
                {
                    cl.SetNewLicensePlateNumber(listing.Registration, true);
                    cl.SetNewLicensePlateNumber(listing.Registration, false);
                }
                catch (Exception ex) { OXLPlugin.Log.Msg($"[OXL] SetLicensePlate failed: {ex.Message}"); }
            }

            yield return new WaitForFixedUpdate();

            OXLPlugin.Log.Msg($"[OXL:WEAR] ══ WEAR APPLIED ═════════════════════════════════");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Actual={actual:P0}  Body={listing.BodyCondition:P0}  MechBase={mechBase:P0}  BodyBase={bodyBase:P0}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Arch={listing.Archetype} L{listing.ArchetypeLevel}  StartFloor={startFloor:P0}  ExhaustMax={exhaustableMax:P0}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Faults={listing.Faults}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] ════════════════════════════════════════════════");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  WEAR CALCULATOR — oparty na PartCatalog, bez string-contains
        // ══════════════════════════════════════════════════════════════════════
        // Zamiast 10 metod IsXxx() — jedna ścieżka: Classify → WearCat → float.
        // isBodyPart=true: traktuj jako część karoserii (carParts), nie mechanikę.

        private static float WearForPart(string partId, float baseCondition, float exhaustableMax, float startFloor, FaultFlags faults, bool isBodyPart)
        {
            // Szyby, lusterka, reflektory — logika binarna, nie przez katalog
            if (isBodyPart && IsGlass(partId))
            {
                if (faults.HasFlag(FaultFlags.GlassDamage))
                    return UnityEngine.Random.value < 0.40f ? UnityEngine.Random.Range(0f, 0.08f) : baseCondition;
                return baseCondition;
            }

            WearCat cat = PartCatalog.Classify(partId);

            // Hardware i Structural nigdy się nie zużywają — zawsze pełny stan
            if (cat == WearCat.Hardware || cat == WearCat.Structural)
                return 1.0f;

            return cat switch
            {
                // ── Materiały eksploatacyjne — startFloor zapewnia że Dealer ma odpalające auto ───
                WearCat.SparkPlug => Mathf.Max(startFloor, UnityEngine.Random.Range(0f, exhaustableMax)),
                WearCat.FilterOil => Mathf.Max(startFloor * 0.5f,UnityEngine.Random.Range(0f, exhaustableMax)),
                WearCat.FilterFuel => Mathf.Max(startFloor * 0.6f, UnityEngine.Random.Range(0f, exhaustableMax)),


                WearCat.FilterAir => Mathf.Max(startFloor * 0.6f,UnityEngine.Random.Range(0f, exhaustableMax * 1.5f)),

                // ── Rozrząd — TRAP, odpowiada na FaultFlags.TimingBelt ───────────────────
                // startFloor > 0 (Dealer): rozrząd wystarczająco sprawny by odpalić, ale bomba zegarowa
                WearCat.TimingChain => faults.HasFlag(FaultFlags.TimingBelt) ? Mathf.Max(startFloor, UnityEngine.Random.Range(0f, 0.12f)) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.80f), 0.05f, 0.80f),
                WearCat.TimingTensioner => faults.HasFlag(FaultFlags.TimingBelt) ? Mathf.Max(startFloor * 0.80f, UnityEngine.Random.Range(0f, 0.10f)) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.75f), 0.05f, 0.75f),
                WearCat.TimingRoller => faults.HasFlag(FaultFlags.TimingBelt) ? Mathf.Max(startFloor * 0.80f, UnityEngine.Random.Range(0f, 0.10f)) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.78f), 0.05f, 0.78f),

                // ── Hamulce — odpowiada na FaultFlags.BrakesGone ─────────────────────────
                WearCat.BrakeFriction => faults.HasFlag(FaultFlags.BrakesGone) ? UnityEngine.Random.Range(0f, 0.05f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.30f, 0.70f), 0.02f, 0.70f),
                WearCat.BrakeDisc => faults.HasFlag(FaultFlags.BrakesGone) ? UnityEngine.Random.Range(0.05f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.85f), 0.10f, 0.85f),
                WearCat.BrakeCaliper => faults.HasFlag(FaultFlags.BrakesGone) ? UnityEngine.Random.Range(0.10f, 0.30f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f), 0.20f, 1.0f),
                WearCat.BrakeBooster => faults.HasFlag(FaultFlags.BrakesGone) ? UnityEngine.Random.Range(0.20f, 0.45f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f), 0.30f, 1.0f),
                WearCat.AbsSystem => faults.HasFlag(FaultFlags.BrakesGone) ? UnityEngine.Random.Range(0.10f, 0.35f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f), 0.20f, 1.0f),

                // ── Zawieszenie — odpowiada na FaultFlags.SuspensionWorn ─────────────────
                WearCat.Shock => faults.HasFlag(FaultFlags.SuspensionWorn) ? UnityEngine.Random.Range(0f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.80f), 0.05f, 0.80f),
                WearCat.Spring => faults.HasFlag(FaultFlags.SuspensionWorn) ? UnityEngine.Random.Range(0.10f, 0.35f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.15f, 0.90f),
                WearCat.Bushing => faults.HasFlag(FaultFlags.SuspensionWorn)? Mathf.Max(startFloor * 0.1f, UnityEngine.Random.Range(0f, 0.25f))  // min 0.028 dla L2
                : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.35f, 0.75f), 0.05f, 0.75f),
                WearCat.Wishbone => faults.HasFlag(FaultFlags.SuspensionWorn) ? UnityEngine.Random.Range(0.10f, 0.35f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.20f, 0.90f),
                WearCat.Stabilizer => faults.HasFlag(FaultFlags.SuspensionWorn) ? UnityEngine.Random.Range(0.05f, 0.30f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.45f, 0.85f), 0.10f, 0.85f),
                WearCat.Steering => faults.HasFlag(FaultFlags.SuspensionWorn) ? UnityEngine.Random.Range(0.05f, 0.25f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.15f, 0.90f),
                WearCat.Knuckle => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f), 0.30f, 1.0f),
                WearCat.Hub => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f), 0.25f, 1.0f),
                WearCat.Bearing => faults.HasFlag(FaultFlags.SuspensionWorn) ? UnityEngine.Random.Range(0f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.85f), 0.10f, 0.85f),
                WearCat.Subframe => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.80f, 1.0f), 0.40f, 1.0f),

                // ── Sprzęgło / koło zamachowe ─────────────────────────────────────────────
                WearCat.Clutch => Mathf.Max(startFloor,Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.30f, 0.75f), 0.05f, 0.75f)),
                WearCat.Flywheel => Mathf.Max(startFloor,Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f), 0.30f, 1.0f)),

                // ── Silnik — odpowiada na FaultFlags.HeadGasket ──────────────────────────
                WearCat.EngineBlock => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f), 0.30f, 1.0f),
                WearCat.Crankshaft => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 0.98f), 0.25f, 1.0f),
                WearCat.Piston => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f), 0.15f, 0.95f),
                WearCat.CylinderHead => faults.HasFlag(FaultFlags.HeadGasket) ? Mathf.Max(startFloor * 0.75f, UnityEngine.Random.Range(0f, 0.08f)) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 0.95f), 0.20f, 0.95f),
                WearCat.CamValve => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.92f), 0.15f, 0.92f),

                // Układ wydechowy — rury startowe muszą działać
                WearCat.ExhaustPipe => faults.HasFlag(FaultFlags.ExhaustRusted)? Mathf.Max(startFloor * 0.5f, UnityEngine.Random.Range(0f, 0.18f))
                : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.80f), 0.05f, 0.80f),


                WearCat.ExhaustManifold => faults.HasFlag(FaultFlags.ExhaustRusted)? Mathf.Max(startFloor * 0.5f, UnityEngine.Random.Range(0.05f, 0.25f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.15f, 0.90f),

                WearCat.Muffler => faults.HasFlag(FaultFlags.ExhaustRusted)? Mathf.Max(startFloor * 0.4f, UnityEngine.Random.Range(0f, 0.12f))
                    : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.45f, 0.85f), 0.05f, 0.85f),

                // ── Chłodzenie ────────────────────────────────────────────────────────────
                WearCat.Radiator => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.95f), 0.10f, 0.95f),
                WearCat.CoolingFan => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 1.0f), 0.20f, 1.0f),
                WearCat.WaterPump => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.90f), 0.10f, 0.90f),
                WearCat.CoolantSystem => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f), 0.25f, 1.0f),

                // ── Elektryka — startFloor gwarantuje że Dealer ma odpalające auto ────────
                WearCat.Alternator => Mathf.Max(startFloor, faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 1.0f), 0.15f, 1.0f)),
                WearCat.Battery => Mathf.Max(startFloor + 0.05f, faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.15f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.85f), 0.05f, 0.85f)),
                WearCat.Starter => Mathf.Max(startFloor + 0.08f, faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0.05f, 0.30f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f), 0.20f, 1.0f)),
                WearCat.Ecu => Mathf.Max(startFloor, faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f), 0.30f, 1.0f)),
                WearCat.Relay => faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.25f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f), 0.25f, 1.0f),
                WearCat.Fuse => faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f), 0.30f, 1.0f),
                WearCat.IgnitionCoil => Mathf.Max(startFloor, faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.18f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.10f, 0.90f)),
                WearCat.Distributor => Mathf.Max(startFloor, faults.HasFlag(FaultFlags.ElectricalFault) ? UnityEngine.Random.Range(0f, 0.20f) : Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.10f, 0.90f)),

                // ── Napęd ─────────────────────────────────────────────────────────────────
                WearCat.Gearbox => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f), 0.20f, 0.95f),
                WearCat.DriveShaft => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.92f), 0.15f, 0.92f),
                WearCat.Differential => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f), 0.25f, 1.0f),
                WearCat.TransferCase => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.65f, 1.0f), 0.25f, 1.0f),

                // ── Układ dolotowy / paliwowy ─────────────────────────────────────────────
                WearCat.Intake => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.70f, 1.0f), 0.30f, 1.0f),
                WearCat.Throttle => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f), 0.20f, 0.95f),
                WearCat.Injector => Mathf.Max(startFloor * 0.4f,Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.45f, 0.88f), 0.10f, 0.88f)),
                WearCat.FuelPump => Mathf.Max(startFloor, Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.50f, 0.90f), 0.10f, 0.90f)),
                WearCat.FuelTank => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f), 0.35f, 1.0f),
                WearCat.Turbo => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.40f, 0.85f), 0.05f, 0.85f),

                // ── Wspomaganie kierownicy ────────────────────────────────────────────────
                WearCat.PowerSteering => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.15f, 0.90f),

                // ── Fallback — nieznana kategoria, twarda mechanika, minimum 25% ─────────
                _ => Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.85f, 1.0f), 0.25f, 1.0f),
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

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

        private static string ToGameId(string internalId)
        {
            if (internalId.StartsWith("car_dnb_censor")) return "car_dnbcensor";
            if (internalId.StartsWith("car_katagiri_tamago")) return "car_katagiritamagobp";
            if (internalId.StartsWith("car_luxor_streamliner")) return "car_luxorstreamlinermk3";
            if (internalId.StartsWith("car_mayen_m5")) return "car_mayenm5";
            if (internalId.StartsWith("car_salem_aries")) return "car_salemariesmk3";
            return internalId;
        }
    }
}