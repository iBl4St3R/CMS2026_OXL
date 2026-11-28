// Silnik z OXL bez modelu (2026-11-28): wszystkie stojaki, grupa silnika (typ, UID, liczba części, zamontowane) i pierwsze części (ID, NormalID, UID, stan). Tylko odczyt.
foreach (var s in UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Garage.Tools.EngineStand>())
{
    if (!s.HaveEngine()) continue;
    var g = s.GetGroupOnEngineStand();
    var all = s.engine == null ? null : s.engine.GetComponentsInChildren<Il2CppCMS.Core.Car.PartScript>(true);
    Print($"[probe] {s.GetCurrentEngineName()}: groupType={g?.GetIl2CppType().Name} groupID={g?.ID} groupUID={g?.UID} items={g?.ItemList?.Count} unmounted={(all == null ? -1 : all.Count(p => p.IsUnmounted))}/{all?.Length}");
    for (int i = 0; g?.ItemList != null && i < Math.Min(8, g.ItemList.Count); i++) { var it = g.ItemList[i]; Print($"[probe]   ID='{it.ID}' normal='{it.NormalID}' uid={it.UID} stack={it.StackUID} cond={it.Condition} exam={it.IsExamined} q={it.Quality} type={it.GetIl2CppType().Name}"); }
}
