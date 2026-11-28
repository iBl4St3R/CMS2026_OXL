// Silnik z OXL bez modelu (2026-11-28): hipoteza formatu części w grupie silnika: ID = "część(k)" (k = które wystąpienie tej części na liście), NormalID = "część".
// Wysyła od razu silnik engine25_r4_sohc_fwd w tym formacie, stan ok. 60%. ZMIENIA STAN: darmowa paczka.
string engineId = "engine25_r4_sohc_fwd";
var parts = Il2CppCMS.Core.GameInventory.Instance.EngineDataProvider.EnginePartsLookup[engineId].EngineParts;
var eng = new Il2CppCMS.Player.Containers.EngineGroupItem(engineId);
var seen = new System.Collections.Generic.Dictionary<string, int>();
for (int i = 0; i < parts.Count; i++)
{
    string p = parts[i];
    seen.TryGetValue(p, out int k); seen[p] = k + 1;
    var it = new Il2CppCMS.Player.Containers.Item(p + "(" + k + ")");
    it.NormalID = p;
    it.Condition = (byte)UnityEngine.Random.Range(130, 175);
    it.IsExamined = false;
    eng.ItemList.Add(it);
}
Print($"[probe] built {eng.ItemList.Count} parts, first ID={eng.ItemList[0].ID} normal={eng.ItemList[0].NormalID}, piston={eng.ItemList.ToArray().First(x => x.NormalID == "tlok_8").ID}");
var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
var d = new Il2CppCMS.Containers.Delivery(255, Il2CppCMS.UI.Logic.ShopType.UsedParts, Il2CppCMS.Multiplayer.NetworkHelpers.GetPlayerName());
var di = new Il2CppCMS.Containers.DeliveryItem();
di.Data = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.FromIBaseItem(eng.TryCast<Il2CppCMS.Player.Containers.IBaseItem>());
di.Amount = 1;
d.AddItem(di);
dm.OrderDelivery(d);
dm.RPC_DeliverWaitingInstantDeliveries();
Print($"[probe] delivered engine id={d.ID}");
