// Silnik z OXL bez modelu na stojaku (2026-11-28): co jest na stojaku (nazwa, grupa, liczba części, stan, obiekt silnika). Tylko odczyt.
var stands = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Garage.Tools.EngineStand>();
Print($"[probe] stands={stands.Length}");
foreach (var s in stands)
{
    string name = "", cur = "", des = "";
    try { name = s.GetCurrentEngineName(); } catch (Exception ex) { name = "ERR " + ex.Message; }
    try { cur = s.GetCurrentEngine(); des = s.GetDesiredEngine(); } catch { }
    var g = s.GetGroupOnEngineStand();
    int n = g?.ItemList == null ? -1 : g.ItemList.Count;
    string ids = "";
    for (int i = 0; g?.ItemList != null && i < Math.Min(5, n); i++) ids += g.ItemList[i].ID + ":" + g.ItemList[i].Condition + ", ";
    bool hasEngine = false; try { hasEngine = s.engine != null; } catch { }
    Print($"[probe] {s.gameObject.name}: name='{name}' current='{cur}' desired='{des}' group={g?.ID} items={n} cond={(g == null ? 0 : g.GetConditionToShow())} engineObj={hasEngine} spawnChildren={(s.SpawnPoint == null ? -1 : s.SpawnPoint.childCount)} | {ids}");
}
