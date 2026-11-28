// Silnik na stojaku (2026-11-28): składanie jak w OXLDelivery.BuildEngine po poprawce: ID = ostatni człon EnginePartsPaths ("v8_przepustnica(1)"),
// NormalID = część, MountObjectData.ParentPath = ścieżka gniazda, śruby puste; bez rozrusznika (tylko do auta). ZMIENIA STAN: darmowa paczka z V8 od razu.
string engineId = "engine25_v8_350_head1_rwd";
var cfg = Il2CppCMS.Core.GameInventory.Instance.EngineDataProvider.EnginePartsLookup[engineId];
var eng = new Il2CppCMS.Player.Containers.EngineGroupItem(engineId);
for (int i = 0; i < cfg.EngineParts.Count; i++)
{
    string p = cfg.EngineParts[i], path = cfg.EnginePartsPaths[i];
    if (p == "v8_rozrusznik_1") continue;
    var it = new Il2CppCMS.Player.Containers.Item(path.Substring(path.LastIndexOf('/') + 1));
    it.NormalID = p; it.Condition = 180; it.IsExamined = false;
    var m = new Il2CppCMS.Containers.MountObjectData(); m.ParentPath = path;
    m.Condition = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ushort>(0);
    m.IsStuck = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<bool>(0);
    m.Dirtness = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ushort>(0);
    it.MountObjectData = m;
    eng.ItemList.Add(it);
}
var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
var d = new Il2CppCMS.Containers.Delivery(255, Il2CppCMS.UI.Logic.ShopType.UsedParts, Il2CppCMS.Multiplayer.NetworkHelpers.GetPlayerName());
var di = new Il2CppCMS.Containers.DeliveryItem();
di.Data = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.FromIBaseItem(eng.TryCast<Il2CppCMS.Player.Containers.IBaseItem>());
di.Amount = 1; d.AddItem(di); dm.OrderDelivery(d); dm.RPC_DeliverWaitingInstantDeliveries();
Print($"[probe] V8 with {eng.ItemList.Count} parts (slot IDs from paths), delivery {d.ID}");
