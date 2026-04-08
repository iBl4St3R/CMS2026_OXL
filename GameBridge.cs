using MelonLoader;
using System;
using System.Collections;
using UnityEngine;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

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
                OXLPlugin.Log.Msg($"[OXL] Deducted ${amount}");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL] DeductMoney failed: {ex.Message}");
            }
        }

        public static bool HasFreeSlot()
        {
            foreach (var cl in GetLoaders())
                if (string.IsNullOrWhiteSpace(cl.CarID) && !cl.modelLoaded)
                    return true;
            return false;
        }

        // ── Il2Cpp interop: CarLoader nie dziedziczy po UnityEngine.Object
        //    w kontekście kompilatora — używamy niegenericowej wersji + cast
        private static Il2CppCMS.Core.Car.CarLoader[] GetLoaders()
        {
            // Obejście CS0311 — używamy niegenericowej wersji FindObjectsOfType
            var type = Il2CppInterop.Runtime.Il2CppType.Of<Il2CppCMS.Core.Car.CarLoader>();
            var objs = UnityEngine.Object.FindObjectsOfType(type, true);
            var result = new Il2CppCMS.Core.Car.CarLoader[objs.Length];
            for (int i = 0; i < objs.Length; i++)
                result[i] = objs[i].TryCast<Il2CppCMS.Core.Car.CarLoader>();
            return result;
        }

        private static IEnumerator DoSpawn(string gameCarId, Action<SpawnResult> onDone)
        {
            Il2CppCMS.Core.Car.CarLoader free = null;
            foreach (var cl in GetLoaders())
            {
                if (cl == null) continue;
                if (string.IsNullOrWhiteSpace(cl.CarID) && !cl.modelLoaded && free == null)
                    free = cl;
            }

            if (free == null) { onDone?.Invoke(SpawnResult.NoFreeSlot); yield break; }

            Il2Cpp.CarDebug dbg = null;
            try { dbg = free.gameObject.GetComponent<Il2Cpp.CarDebug>(); }
            catch { }

            if (dbg == null) { onDone?.Invoke(SpawnResult.NoCarDebug); yield break; }

            dbg.LoadCar(gameCarId, GetVariant(gameCarId));  // ← wariant zależy od auta

            float timeout = 10f;
            while (!free.done && timeout > 0f)
            {
                timeout -= 0.1f;
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForEndOfFrame();

            // POPRAWKA: zapisz auto jeśli done=true LUB CarID jest ustawione,
            // nie tylko gdy modelLoaded=true — niektóre auta demo mają błędy zasobów
            // (np. rim_46) ale i tak stoją na parkingu
            bool succeeded = free.modelLoaded || !string.IsNullOrWhiteSpace(free.CarID);

            if (succeeded)
            {
                free.ChangePosition(Il2Cpp.CarPlace.Entrance1, true);
                OXLPlugin.Log.Msg($"[OXL] Spawned {gameCarId} OK (modelLoaded={free.modelLoaded})");

                if (!OXLPlugin.PurchasedCarIds.Contains(gameCarId))
                    OXLPlugin.PurchasedCarIds.Add(gameCarId);

                onDone?.Invoke(SpawnResult.Success);
            }
            else
            {
                OXLPlugin.Log.Msg($"[OXL] Spawn failed for {gameCarId} (timeout={timeout <= 0f})");
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
            OXLPlugin.Log.Msg($"[OXL] Unknown internalId: {internalId}");
            return internalId;
        }

        private static int GetVariant(string gameCarId)
        {
            // Mayen M5 wymaga wariantu 1 — jedyny wyjątek w demo
            if (gameCarId == "car_mayenm5") return 1;
            return 0;
        }
    }
}