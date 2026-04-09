using Il2CppCMS.UI.Logic;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class GameBridge
    {
        public enum SpawnResult { Success, NoFreeSlot, NoCarDebug, SpawnFailed }

        public static void SpawnCar(string internalId, float condition, Action<SpawnResult> onDone)
        {
            MelonCoroutines.Start(DoSpawn(ToGameId(internalId), condition, onDone));
        }

        // ── stare przeciażenie dla kompatybilności ─────────────────────────────
        public static void SpawnCar(string internalId, Action<SpawnResult> onDone)
            => SpawnCar(internalId, 1.0f, onDone);

        public static void DeductMoney(int amount)
        {
            try
            {
                Il2CppCMS.Shared.SharedGameDataManager.Instance.AddMoneyRpc(-amount);
            }
            catch { }
        }

        public static bool HasFreeSlot()
        {
            return GetLoaders().Any(cl => string.IsNullOrWhiteSpace(cl.CarID) && !cl.modelLoaded);
        }

        private static Il2CppCMS.Core.Car.CarLoader[] GetLoaders()
        {
            var type = Il2CppInterop.Runtime.Il2CppType.Of<Il2CppCMS.Core.Car.CarLoader>();
            return UnityEngine.Object.FindObjectsOfType(type, true) // true - tak jak w REPL
                .Select(x => x.TryCast<Il2CppCMS.Core.Car.CarLoader>())
                .ToArray();
        }


        private static IEnumerator DoSpawn(string gameCarId, float condition, Action<SpawnResult> onDone)
        {
            var loaders = GetLoaders();
            var free = loaders.FirstOrDefault(cl => string.IsNullOrWhiteSpace(cl.CarID) && !cl.modelLoaded);

            if (free == null) { onDone?.Invoke(SpawnResult.NoFreeSlot); yield break; }

            // 1. Czyścimy loader przez Reflection (jak w REPL)
            try
            {
                var unload = free.GetIl2CppType().GetMethod("UnloadCar");
                unload?.Invoke(free, null);
            }
            catch { }

            yield return new WaitForSeconds(0.2f);
            yield return new WaitForFixedUpdate();

            // 2. Ładujemy auto
            var debugComp = free.gameObject.GetComponent<Il2Cpp.CarDebug>();
            if (debugComp == null) { onDone?.Invoke(SpawnResult.NoCarDebug); yield break; }

            debugComp.LoadCar(gameCarId, gameCarId == "car_mayenm5" ? 1 : 0);

            // 3. Czekamy na model
            float timeout = 10f;
            while (!free.done && timeout > 0f)
            {
                timeout -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForFixedUpdate();

            // --- KLUCZOWY FIX DLA ForceStayObjectToMark ---
            // Musimy przenieść auto do konkretnego slotu, inaczej SaveSystem go nie widzi i wywala NRE
            try
            {
                // Entrance1 to bezpieczny slot startowy
                free.ChangePosition(Il2Cpp.CarPlace.Entrance1, true);

                // Wymuszamy finalizację przez Reflection (SetBonesDone)
                free.GetIl2CppType().GetMethod("SetBonesDone")?.Invoke(free, null);
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] Anchor failed: {ex.Message}");
            }

            yield return new WaitForEndOfFrame();

            if (!string.IsNullOrWhiteSpace(free.CarID))
            {
                // ── Aplikuj zużycie ───────────────────────────────────────────
                yield return ApplyWear(free, condition);
                onDone?.Invoke(SpawnResult.Success);
            }
            else
            {
                onDone?.Invoke(SpawnResult.SpawnFailed);
            }
        }


        private static IEnumerator ApplyWear(Il2CppCMS.Core.Car.CarLoader cl, float condition)
        {
            condition = Mathf.Clamp(condition, 0.02f, 1.0f);

            // Mechanika jest trochę gorsza niż karoseria (ukryte usterki)
            float mechWear = Mathf.Clamp(condition * UnityEngine.Random.Range(0.80f, 1.00f), 0.02f, 1.0f);
            float bodyWear = Mathf.Clamp(condition * UnityEngine.Random.Range(0.90f, 1.05f), 0.02f, 1.0f);

            // ── indexedParts (silnik + podwozie + elektryka) ──────────────────
            var ip = cl.indexedParts;
            if (ip != null)
            {
                for (int i = 0; i < ip.Count; i++)
                {
                    if (ip[i] == null) continue;
                    // Krytyczne części silnika zużywają się bardziej
                    string id = ip[i].id.ToLower();
                    float w = mechWear;
                    if (id.Contains("tarcza") || id.Contains("sprzeg") || id.Contains("swieca")
                        || id.Contains("filtr") || id.Contains("pasek") || id.Contains("lancuch"))
                        w = Mathf.Clamp(mechWear * UnityEngine.Random.Range(0.60f, 1.10f), 0.02f, 1.0f);
                    ip[i].condition = w;
                }
            }
            cl.ClearEnginePartsConditionCache();

            // ── carParts (karoseria) ──────────────────────────────────────────
            cl.Dev_RepairAllBody();
            yield return new UnityEngine.WaitForEndOfFrame();

            var cp = cl.carParts;
            if (cp != null)
            {
                for (int i = 0; i < cp.Count; i++)
                {
                    if (cp[i] == null) continue;
                    // Szyby i lusterka psują się losowo niezależnie
                    string name = cp[i].name.ToLower();
                    float w = bodyWear;
                    if (name.Contains("window") || name.Contains("mirror"))
                        w = UnityEngine.Random.value < 0.15f ? UnityEngine.Random.Range(0.0f, 0.3f) : bodyWear;
                    cl.SetCondition(cp[i], w);
                }
            }
            cl.SetConditionOnBody(bodyWear);
            cl.SetConditionOnDetails(bodyWear);
            if (bodyWear < 0.5f) cl.SwitchRusted(bodyWear);

            // ── Historia ──────────────────────────────────────────────────────
            var cid = cl.CarInfoData;
            if (cid != null)
            {
                // Przebieg wyliczamy z kondycji — im gorszy stan tym więcej km
                uint mileage = (uint)Mathf.RoundToInt(Mathf.Lerp(220000, 3000, condition)
                               * UnityEngine.Random.Range(0.85f, 1.15f));
                cid.Mileage = mileage;

                // Rok — starsze auto przy gorszej kondycji
                int currentYear = 2026;
                int age = Mathf.RoundToInt(Mathf.Lerp(25, 1, condition) * UnityEngine.Random.Range(0.8f, 1.2f));
                cid.Year = (ushort)Mathf.Clamp(currentYear - age, 1980, 2025);
            }

            yield return new UnityEngine.WaitForFixedUpdate();
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