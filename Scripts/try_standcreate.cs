// Silnik z OXL bez modelu (2026-11-28): wzorzec gry. Na pustym stojaku EngineStand.CreateEngine(id), po chwili grupa na stojaku: ID, UID, NormalID, stan, ile zamontowanych.
// ZMIENIA STAN: na pierwszym pustym stojaku powstaje nowy silnik I4 AQ4C (funkcja stojaka "Create engine").
var s = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Garage.Tools.EngineStand>().FirstOrDefault(x => !x.HaveEngine());
if (s == null) { Print("[probe] no empty stand"); return; }
s.CreateEngine("engine25_r4_sohc_fwd");
System.Collections.IEnumerator Wait()
{
    for (int i = 0; i < 180; i++) yield return null;
    var g = s.GetGroupOnEngineStand();
    var all = s.engine == null ? null : s.engine.GetComponentsInChildren<Il2CppCMS.Core.Car.PartScript>(true);
    MelonLoader.MelonLogger.Msg($"[probe-stand] HaveEngine={s.HaveEngine()} group={g?.ID} type={g?.GetIl2CppType().Name} items={g?.ItemList?.Count} groupUID={g?.UID} unmounted={(all == null ? -1 : all.Count(p => p.IsUnmounted))}/{all?.Length}");
    for (int i = 0; g?.ItemList != null && i < Math.Min(12, g.ItemList.Count); i++) { var it = g.ItemList[i]; MelonLoader.MelonLogger.Msg($"[probe-stand]   ID='{it.ID}' normal='{it.NormalID}' uid={it.UID} stack={it.StackUID} cond={it.Condition} exam={it.IsExamined} quality={it.Quality} new={it.IsNew}"); }
}
MelonLoader.MelonCoroutines.Start(Wait());
Print("[probe] CreateEngine called on " + s.gameObject.name);
