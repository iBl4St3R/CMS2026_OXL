// Silnik z OXL bez modelu (2026-11-28): jak sklep gry pakuje cały silnik. Czyta paczki w kolejce (WaitingDelivery) i dla grup silnika wypisuje:
// typ, ID, UID, liczbę części i dla pierwszych części ID, NormalID, UID, stan, MountObjectData (ścieżka, liczba śrub). Tylko odczyt.
var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
var w = dm.WaitingDelivery;
Print($"[probe] waiting={w?.Count}");
for (int i = 0; w != null && i < w.Count; i++)
{
    var d = w[i];
    for (int j = 0; d.DeliveryItems != null && j < d.DeliveryItems.Count; j++)
    {
        var bi = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.ToIBaseItem(d.DeliveryItems[j].Data);
        var obj = bi.TryCast<Il2CppSystem.Object>();
        string t = obj == null ? "?" : obj.GetIl2CppType().Name;
        Print($"[probe] delivery {d.ID} shop={d.ShopType} hour={d.Hour} item type={t} id={bi.ID}");
        var g = bi.TryCast<Il2CppCMS.Player.Containers.GroupItem>();
        if (g == null) continue;
        Print($"[probe]   group uid={g.UID} items={g.ItemList?.Count}");
        for (int k = 0; g.ItemList != null && k < Math.Min(15, g.ItemList.Count); k++)
        {
            var it = g.ItemList[k]; var m = it.MountObjectData;
            Print($"[probe]   ID='{it.ID}' normal='{it.NormalID}' uid={it.UID} cond={it.Condition} exam={it.IsExamined} new={it.IsNew} mount={(m == null ? "null" : $"path='{m.ParentPath}' bolts={m.Condition?.Length}")}");
        }
    }
}
