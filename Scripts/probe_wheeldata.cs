// Felgi i opony (2026-11-28): jakie WheelData ma prawdziwe koło auta z parkingu (PartScript.CreateItem) i jak to się ma do [3_wheels] w configu. Tylko odczyt.
var loaders = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Core.Car.CarLoader>();
int shown = 0;
foreach (var cl in loaders)
{
    if (string.IsNullOrEmpty(cl.CarID) || shown >= 2) continue;
    shown++;
    var list = Il2Cpp.CarHelper.GetWheelsPartScripts(cl);
    Print($"[probe] {cl.gameObject.name} {cl.CarID} wheels={list?.Count}");
    for (int i = 0; list != null && i < list.Count && i < 8; i++)
    {
        try { var it = list[i].CreateItem(); var w = it.WheelData; Print($"[probe]   {it.ID}: size={w.Size} width={w.Width} profile={w.Profile} ET={w.ET} balanced={w.IsBalanced} cond={it.Condition}"); }
        catch (Exception ex) { Print($"[probe]   #{i} ERR {ex.Message}"); }
    }
}
