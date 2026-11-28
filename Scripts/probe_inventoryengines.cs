// Silnik z OXL bez modelu (2026-11-28): wzorzec gry = silnik wyjęty z auta (w ekwipunku gracza, Inventory.GetEngines). Dla każdej grupy silnika:
// typ, ID, UID, liczba części i dla części ID, NormalID, UID, stan, MountObjectData (ścieżka, liczba śrub). Tylko odczyt.
var inv = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Player.Inventory>().FirstOrDefault();
var engines = inv?.GetEngines();
Print($"[probe] engines in inventory={engines?.Count}");
for (int e = 0; engines != null && e < engines.Count; e++)
{
    var bi = engines[e];
    var obj = bi.TryCast<Il2CppSystem.Object>();
    // EngineGroupItem ma własne ItemList (new), rzut na GroupItem daje null.
    var eg = bi.TryCast<Il2CppCMS.Player.Containers.EngineGroupItem>();
    var list = eg != null ? eg.ItemList : bi.TryCast<Il2CppCMS.Player.Containers.GroupItem>()?.ItemList;
    Print($"[probe] #{e} type={(obj == null ? "?" : obj.GetIl2CppType().Name)} id={bi.ID} uid={bi.UID} items={list?.Count}");
    int withMount = 0, nullMount = 0;
    for (int k = 0; list != null && k < list.Count; k++)
    {
        var it = list[k]; var m = it.MountObjectData;
        if (m == null) nullMount++; else withMount++;
        if (k < 14) Print($"[probe]   ID='{it.ID}' normal='{it.NormalID}' uid={it.UID} cond={it.Condition} exam={it.IsExamined} mount={(m == null ? "null" : $"path='{m.ParentPath}' bolts={m.Condition?.Length} c0={(m.Condition != null && m.Condition.Length > 0 ? m.Condition[0].ToString() : "-")}")}");
    }
    Print($"[probe]   mountData: set={withMount} null={nullMount}");
}
