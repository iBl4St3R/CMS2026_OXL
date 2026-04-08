using MelonLoader;
using System;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Reflection;

namespace CMS2026_OXL
{
    public static class GameBridge
    {
        public enum SpawnResult { Success, NoFreeSlot, NoCarDebug, SpawnFailed }

        public static void SpawnCar(string internalId, Action<SpawnResult> onDone)
        {
            MelonCoroutines.Start(DoSpawn(ToGameId(internalId), onDone));
        }

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


        private static IEnumerator DoSpawn(string gameCarId, Action<SpawnResult> onDone)
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
                onDone?.Invoke(SpawnResult.Success);
            }
            else
            {
                onDone?.Invoke(SpawnResult.SpawnFailed);
            }
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