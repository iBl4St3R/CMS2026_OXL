// Silnik na stojaku (2026-11-28): czy kolejność części w grupie ma znaczenie. V8 820MAL jak w OXLDelivery, ale części, które stojak
// odrzucił (napinacz, przepustnica, przepustnica I4, rolka), przeniesione na koniec listy. ZMIENIA STAN: darmowa paczka z silnikiem od razu.
string engineId = "engine25_v8_350_head1_rwd";
var late = new System.Collections.Generic.HashSet<string> { "v8_350_napinacz", "v8_przepustnica" };
var parts = Il2CppCMS.Core.GameInventory.Instance.EngineDataProvider.EnginePartsLookup[engineId].EngineParts;
var ids = Enumerable.Range(0, parts.Count).Select(i => parts[i]).Where(p => p != "v8_rozrusznik_1").ToList();
ids = ids.Where(p => !late.Contains(p)).Concat(ids.Where(p => late.Contains(p))).ToList();
var eng = new Il2CppCMS.Player.Containers.EngineGroupItem(engineId);
var seen = new System.Collections.Generic.Dictionary<string, int>();
foreach (var p in ids)
{
    seen.TryGetValue(p, out int k); seen[p] = k + 1;
    var it = new Il2CppCMS.Player.Containers.Item(p + "(" + k + ")");
    it.NormalID = p; it.Condition = 180; it.IsExamined = false;
    var m = new Il2CppCMS.Containers.MountObjectData(); m.ParentPath = "";
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
Print($"[probe] V8 with {eng.ItemList.Count} parts, last: {string.Join(",", ids.Skip(ids.Count - 3))}, delivery {d.ID}");
