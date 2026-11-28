// Badanie SpawnFailed (2026-11-28): wolny CarLoader ma carId='' ale IsCarLoaded()=true i ignoruje CarDebug.LoadCar. Próba: DeleteCar(Generic), potem LoadCar; obserwacja CarID i done.
// ZMIENIA STAN: stawia auto na wolnym miejscu (bez pieniędzy).
var free = Il2CppCMS.Garage.CarLoaderPlaces.Get().GetPlaceForLoadCar();
if (free == null) { Print("[probe] no free place"); return; }
var dbg = free.gameObject.GetComponent<Il2Cpp.CarDebug>();
if (dbg == null) { Print("[probe] no CarDebug"); return; }
Print($"[probe] before {free.gameObject.name} carId='{free.CarID}' done={free.done} loaded={free.IsCarLoaded()}");
try { free.DeleteCar(Il2Cpp.DeleteCarReason.Generic); Print($"[probe] after DeleteCar carId='{free.CarID}' done={free.done} loaded={free.IsCarLoaded()}"); } catch (Exception ex) { Print($"[probe] DeleteCar ERR {ex.Message}"); }
System.Collections.IEnumerator Watch()
{
    for (int i = 0; i < 10; i++) yield return null;
    Print($"[probe] 10 frames after DeleteCar carId='{free.CarID}' done={free.done} loaded={free.IsCarLoaded()}");
    dbg.LoadCar("car_mayenm5", 1);
    string last = "";
    float t0 = UnityEngine.Time.realtimeSinceStartup;
    for (int i = 0; i < 600; i++)
    {
        string s = $"carId='{free.CarID}' done={free.done} loaded={free.IsCarLoaded()}";
        if (s != last) { Print($"[probe] f{i} t={UnityEngine.Time.realtimeSinceStartup - t0:F2}s {s}"); last = s; }
        yield return null;
    }
    Print("[probe] watch end");
}
MelonLoader.MelonCoroutines.Start(Watch());
