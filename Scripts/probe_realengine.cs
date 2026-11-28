// Silnik z OXL bez modelu (2026-11-28): jak gra nazywa części prawdziwego silnika. Auto z parkingu: engineHandle -> PartScripts (id, CreateItem().ID),
// i grupa z PartScript.CreateGroupItem() korzenia silnika. Tylko odczyt.
var cl = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Core.Car.CarLoader>().FirstOrDefault(c => !string.IsNullOrEmpty(c.CarID) && c.engineHandle != null);
if (cl == null) { Print("[probe] no car with engine"); return; }
var eng = cl.engineHandle;
var ps = eng.GetModulesPartScripts();
Print($"[probe] {cl.CarID} engine='{cl.GetEngineName()}' moduleScripts={ps?.Length}");
for (int i = 0; ps != null && i < Math.Min(12, ps.Length); i++)
{
    var p = ps[i]; if (p == null) continue;
    string itemId = ""; try { itemId = p.CreateItem()?.ID; } catch { }
    string grp = ""; try { var g = p.CreateGroupItem(); grp = g == null ? "null" : $"{g.ID} items={g.ItemList?.Count} first={(g.ItemList != null && g.ItemList.Count > 0 ? g.ItemList[0].ID : "")}"; } catch (Exception ex) { grp = "ERR " + ex.Message; }
    Print($"[probe]   ps.id='{p.id}' item='{itemId}' group={grp}");
}
var parts = eng.GetComponentsInChildren<Il2CppCMS.Core.Car.PartScript>(true);
Print($"[probe] all PartScripts under engine: {parts.Length}; sample ids: {string.Join(", ", parts.Take(15).Select(x => x.id))}");
