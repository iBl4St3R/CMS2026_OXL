// Silnik z OXL bez modelu (2026-11-28): wzorzec gry (silnik wyjęty z auta) ma MountObjectData przy każdej części, zwykle pusty (ścieżka '', 0 śrub).
// Daje pusty MountObjectData każdej części silnika testowego engine25_r4_sohc_fwd w ekwipunku. ZMIENIA STAN: przedmiot w ekwipunku.
var inv = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Player.Inventory>().First();
var engines = inv.GetEngines();
int fixedN = 0;
for (int e = 0; e < engines.Count; e++)
{
    var eg = engines[e].TryCast<Il2CppCMS.Player.Containers.EngineGroupItem>();
    if (eg == null || eg.ID != "engine25_r4_sohc_fwd") continue;
    for (int k = 0; k < eg.ItemList.Count; k++)
    {
        var it = eg.ItemList[k];
        if (it.MountObjectData != null) continue;
        var m = new Il2CppCMS.Containers.MountObjectData();
        m.ParentPath = "";
        m.Condition = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ushort>(0);
        m.IsStuck = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<bool>(0);
        m.Dirtness = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ushort>(0);
        it.MountObjectData = m;
        fixedN++;
    }
    Print($"[probe] {eg.ID} uid={eg.UID}: mount data set on {fixedN} parts, check first={(eg.ItemList[0].MountObjectData == null ? "null" : "ok")}");
}
