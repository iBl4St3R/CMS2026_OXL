using MelonLoader;
using System;
using System.Collections;
using UnityEngine;


namespace CMS2026_OXL
{
    public static class GameBridge
    {
        public enum SpawnResult { Success, NoFreeSlot, NoCarDebug, SpawnFailed }

        // ── Publiczne API ─────────────────────────────────────────────────────

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
                // fallback na starą metodę jeśli API niedostępne
                return System.Linq.Enumerable.Any(GetLoaders(),
                    cl => string.IsNullOrWhiteSpace(cl.CarID) && !cl.modelLoaded);
            }
        }

        // ── Spawn coroutine ───────────────────────────────────────────────────

        private static IEnumerator DoSpawn(string gameCarId, CarListing listing, Action<SpawnResult> onDone)
        {
            var free = Il2CppCMS.Garage.CarLoaderPlaces.Get().GetPlaceForLoadCar();

            if (free == null) { onDone?.Invoke(SpawnResult.NoFreeSlot); yield break; }

            try
            {
                free.GetIl2CppType().GetMethod("UnloadCar")?.Invoke(free, null);
            }
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
        //  APPLY WEAR — cztery grupy części
        // ══════════════════════════════════════════════════════════════════════

        private static IEnumerator ApplyWear(Il2CppCMS.Core.Car.CarLoader cl, CarListing listing)
        {
            float actual = Mathf.Clamp(listing.ActualCondition, 0.02f, 1.0f);
            var faults = listing.Faults;

            float mechBase = Mathf.Clamp(actual * UnityEngine.Random.Range(0.82f, 0.98f), 0.02f, 1.0f);
            float bodyBase = Mathf.Clamp(actual * UnityEngine.Random.Range(0.88f, 1.04f), 0.02f, 1.0f);
            float exhaustableMax = actual > 0.75f ? 0.40f : 0.18f;
            float group2Mult = listing.Archetype == SellerArchetype.Dealer ? 0.40f : 1.0f;

            
            

            // ── INDEXEDPARTS ──────────────────────────────────────────────────────
            var ip = cl.indexedParts;
            if (ip != null)
            {
                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    string id = ip[i].id?.ToLower() ?? "";
                    float wear = ClassifyAndWear(id, mechBase, exhaustableMax, group2Mult, faults, false);
                    try { ip[i].SetCondition(wear); } catch { ip[i].condition = wear; }
                }
            }
            cl.ClearEnginePartsConditionCache();

            // ── CARPARTS — kondycja + dent + dust ────────────────────────────────
            // Dev_RepairAllBody najpierw — czyści poprzedni stan wizualny
            cl.Dev_RepairAllBody();
            yield return new WaitForEndOfFrame();

            var cp = cl.carParts;
            if (cp != null)
            {
                for (int i = 0; i < cp.Count; i++)
                {
                    if (cp[i] == null) continue;
                    string name = cp[i].name?.ToLower() ?? "";
                    float wear = ClassifyAndWear(name, bodyBase, exhaustableMax, group2Mult, faults, true);

                    cl.SetCondition(cp[i], wear);

                    // Denty i kurz — osobny system wizualny
                    // Tylko na karoserii, nie na szybach/lampach
                    if (!IsGlass(name, true))
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

                        if (dust > 0f)
                            try { cl.EnableDust(cp[i], dust); } catch { }
                    }
                }
            }

            // ── Stan ogólny karoserii ─────────────────────────────────────────────
            cl.SetConditionOnBody(bodyBase);
            cl.SetConditionOnDetails(bodyBase);
            if (bodyBase < 0.50f) cl.SwitchRusted(bodyBase);

            yield return new WaitForFixedUpdate();

            try { cl.UpdateCarBodyParts(); } catch { }
            try { cl.SetupCarSupport(); } catch { }

            yield return new WaitForFixedUpdate();

            // ── Historia pojazdu ──────────────────────────────────────────────────
            var cid = cl.CarInfoData;
            if (cid != null)
            {
                uint mileage = (uint)Mathf.RoundToInt(
                    Mathf.Lerp(220000, 3000, actual) * UnityEngine.Random.Range(0.85f, 1.15f));
                cid.Mileage = mileage;

                int age = Mathf.RoundToInt(Mathf.Lerp(28, 1, actual) * UnityEngine.Random.Range(0.80f, 1.20f));
                cid.Year = (ushort)Mathf.Clamp(2026 - age, 1980, 2025);
            }

            yield return new WaitForFixedUpdate();

            OXLPlugin.Log.Msg($"[OXL:WEAR] ══ WEAR APPLIED ═════════════════");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Actual={actual:P0}  MechBase={mechBase:P0}  BodyBase={bodyBase:P0}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Arch={listing.Archetype}  ExhaustMax={exhaustableMax:P0}  G2Mult={group2Mult:F2}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] Faults={listing.Faults}");
            OXLPlugin.Log.Msg($"[OXL:WEAR] ════════════════════════════════════");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  KLASYFIKACJA CZĘŚCI — rdzeń logiki zużycia
        // ══════════════════════════════════════════════════════════════════════

        private static float ClassifyAndWear(
            string partId,
            float baseCondition,
            float exhaustableMax,
            float group2Mult,
            FaultFlags faults,
            bool isBodyPart)
        {
            // ── GRUPA III — Optyka i szkło (binarne) ──────────────────────────
            if (IsGlass(partId, isBodyPart))
            {
                if (faults.HasFlag(FaultFlags.GlassDamage))
                    // 40% szans na rozbicie, reszta normalna kondycja
                    return UnityEngine.Random.value < 0.40f ? UnityEngine.Random.Range(0.0f, 0.05f) : baseCondition;
                else
                    // Bez flagi — małe ryzyko losowe (7%)
                    return UnityEngine.Random.value < 0.07f ? UnityEngine.Random.Range(0.0f, 0.10f) : baseCondition;
            }

            // ── GRUPA I — Materiały eksploatacyjne ────────────────────────────
            if (IsExhaustable(partId))
            {
                // Pasek rozrządu — specjalna pułapka
                if (IsTimingBelt(partId))
                {
                    if (faults.HasFlag(FaultFlags.TimingBelt))
                        return UnityEngine.Random.Range(0.0f, 0.08f); // pułapka: 0–8%
                    else
                        return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.5f, 0.85f), 0.10f, exhaustableMax);
                }

                // Filtry, świece — zawsze niskie
                return UnityEngine.Random.Range(0.0f, exhaustableMax);
            }

            // ── GRUPA I — Hamulce (fault-driven) ─────────────────────────────
            if (IsBrakePart(partId))
            {
                if (faults.HasFlag(FaultFlags.BrakesGone))
                    return UnityEngine.Random.Range(0.0f, 0.12f); // praktycznie brak
                else
                    return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.55f, 0.90f), 0.05f, exhaustableMax + 0.20f);
            }

            // ── GRUPA II — Ukryta mechanika ───────────────────────────────────
            if (IsSuspension(partId))
            {
                if (faults.HasFlag(FaultFlags.SuspensionWorn))
                    return Mathf.Clamp(baseCondition * group2Mult * UnityEngine.Random.Range(0.20f, 0.45f), 0.02f, 0.35f);
                else
                    return Mathf.Clamp(baseCondition * group2Mult * UnityEngine.Random.Range(0.70f, 1.0f), 0.05f, 1.0f);
            }

            if (IsExhaust(partId))
            {
                if (faults.HasFlag(FaultFlags.ExhaustRusted))
                    return  UnityEngine.Random.Range(0.0f, 0.15f);
                else
                    return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.60f, 0.95f), 0.05f, 1.0f);
            }

            if (IsElectrical(partId))
            {
                if (faults.HasFlag(FaultFlags.ElectricalFault))
                    return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.15f, 0.40f), 0.02f, 0.40f);
                else
                    return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.75f, 1.0f), 0.10f, 1.0f);
            }

            if (faults.HasFlag(FaultFlags.HeadGasket) && IsHeadGasketRelated(partId))
                return UnityEngine.Random.Range(0.0f, 0.10f);

            // ── GRUPA IV — Twarda mechanika (domyślna) ────────────────────────
            // Minimum 25% — silnik fizycznie istnieje, tylko wrecker może zejść niżej
            float hardMin = 0.25f;
            return Mathf.Clamp(baseCondition * UnityEngine.Random.Range(0.85f, 1.0f), hardMin, 1.0f);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  KLASYFIKATORY CZĘŚCI — słowa kluczowe z silnika gry
        // ══════════════════════════════════════════════════════════════════════

        private static bool IsTimingBelt(string id) =>
            id.Contains("pasek") || id.Contains("lancuch") ||
            id.Contains("belt") || id.Contains("chain") ||
            id.Contains("timing");

        private static bool IsExhaustable(string id) =>
            IsTimingBelt(id) ||
            id.Contains("filtr") || id.Contains("filter") ||
            id.Contains("swieca") || id.Contains("spark") ||
            id.Contains("olej") || id.Contains("oil") ||
            id.Contains("pasek") || id.Contains("klinowy");

        private static bool IsBrakePart(string id) =>
            id.Contains("klocek") || id.Contains("brake_pad") || id.Contains("pad") ||
            id.Contains("tarcza") && (id.Contains("ham") || id.Contains("brake")) ||
            id.Contains("zacisk") || id.Contains("caliper");

        private static bool IsSuspension(string id) =>
            id.Contains("amortyzator") || id.Contains("shock") || id.Contains("strut") ||
            id.Contains("tuleja") || id.Contains("bushing") ||
            id.Contains("wahacz") || id.Contains("control_arm") ||
            id.Contains("draze") || id.Contains("tie_rod") ||
            id.Contains("stabilizator") || id.Contains("sway") || id.Contains("link") ||
            id.Contains("sprezyna") || id.Contains("spring");

        private static bool IsExhaust(string id) =>
            id.Contains("tlumik") || id.Contains("muffler") || id.Contains("exhaust") ||
            id.Contains("rura") || id.Contains("katalizator") || id.Contains("catalyst");

        private static bool IsElectrical(string id) =>
            id.Contains("alternator") || id.Contains("akumulator") || id.Contains("battery") ||
            id.Contains("rozrusznik") || id.Contains("starter") ||
            id.Contains("cewka") || id.Contains("coil") ||
            id.Contains("lambda") || id.Contains("sensor") || id.Contains("czujnik");

        private static bool IsHeadGasketRelated(string id) =>
            id.Contains("uszczelka") || id.Contains("gasket") ||
            id.Contains("glowica") || id.Contains("head") ||
            id.Contains("blok") || id.Contains("block");

        private static bool IsGlass(string id, bool isBodyPart)
        {
            if (!isBodyPart) return false;
            return id.Contains("window") || id.Contains("szyba") ||
                   id.Contains("mirror") || id.Contains("lusterko") ||
                   id.Contains("reflektor") || id.Contains("headlight") ||
                   id.Contains("lamp") || id.Contains("klosz") ||
                   id.Contains("glass");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

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