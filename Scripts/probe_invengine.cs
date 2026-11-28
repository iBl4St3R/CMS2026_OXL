// Silnik z OXL bez modelu (2026-11-28): co leży w ekwipunku gracza (Inventory.GetGroups): ID, typ, liczba części, stan. Tylko odczyt.
var invs = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Player.Inventory>();
Print($"[probe] inventories={invs.Length}");
foreach (var inv in invs)
{
    var groups = inv.GetGroups();
    Print($"[probe] {inv.gameObject.name}: groups={groups?.Count}");
    for (int i = 0; groups != null && i < groups.Count; i++)
    {
        var g = groups[i];
        string t = g.GetIl2CppType().Name;
        int n = g.ItemList == null ? -1 : g.ItemList.Count;
        string first = n > 0 ? g.ItemList[0].ID : "";
        Print($"[probe]   {g.ID} type={t} items={n} cond={g.GetConditionToShow()} first={first}");
    }
}
