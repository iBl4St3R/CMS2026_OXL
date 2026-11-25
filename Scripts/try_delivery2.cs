// try_delivery2 (G16): pełny silnik w paczce, adresat paczki, dostawa natychmiastowa (site.md 9.5). ZMIENIA STAN: zapisz grę przed.
// G14 pokazał: EngineGroupItem(id) jest pusty (0 części), paczki OXL miały pusty DeliveryTarget. Tu silnik wypełniamy częściami z EnginePartsLookup
// (stan ~60%), adresat z NetworkHelpers.GetPlayerName(), i próbujemy dostawy od razu. Cofnięcie: wczytaj zapis.
var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
if (dm == null) { Print("[probe] no DeliveryManager"); return; }
string target = ""; try { target = Il2CppCMS.Multiplayer.NetworkHelpers.GetPlayerName(); } catch (Exception ex) { Print($"[probe] name ERR {ex.Message}"); }
Print($"[probe] target='{target}' waiting={dm.WaitingDelivery?.Count}");

// Jak gra oznacza dostawę natychmiastową: sprawdzamy IsInstant dla kilku godzin bez nadawania.
foreach (byte h in new byte[] { 0, 1, 24, 99, 254, 255 }) { try { var t = new Il2CppCMS.Containers.Delivery(h, Il2CppCMS.UI.Logic.ShopType.UsedParts, target); Print($"[probe] instant test hour={h} isInstant={t.IsInstant()}"); } catch (Exception ex) { Print($"[probe] instant test {h} ERR {ex.Message}"); } }

string engineId = "engine25_v6_231_rwd";
try
{
    var look = Il2CppCMS.Core.GameInventory.Instance.EngineDataProvider.EnginePartsLookup;
    var cfg = look[engineId];
    var eng = new Il2CppCMS.Player.Containers.EngineGroupItem(engineId);
    if (eng.ItemList == null) Print("[probe] engine ItemList null");
    int added = 0;
    for (int i = 0; cfg.EngineParts != null && i < cfg.EngineParts.Count; i++)
    {
        var part = new Il2CppCMS.Player.Containers.Item(cfg.EngineParts[i]);
        part.Condition = (byte)UnityEngine.Random.Range(110, 200);
        eng.ItemList.Add(part);
        added++;
    }
    var bi = eng.TryCast<Il2CppCMS.Player.Containers.IBaseItem>();
    Print($"[probe] engine filled parts={added} items={eng.ItemList.Count} cond={eng.GetConditionToShow()} size={eng.Size} name='{eng.GetLocalizedName()}'");

    byte instantHour = 255;
    var d = new Il2CppCMS.Containers.Delivery(instantHour, Il2CppCMS.UI.Logic.ShopType.UsedParts, target);
    var di = new Il2CppCMS.Containers.DeliveryItem();
    di.Data = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.FromIBaseItem(bi);
    di.Amount = 1;
    d.AddItem(di);
    Print($"[probe] engine delivery hour={instantHour} isInstant={d.IsInstant()} size={d.GetPackageSize()}");
    dm.OrderDelivery(d);
    Print($"[probe] ordered engine id={d.ID} waiting={dm.WaitingDelivery?.Count}");
    try { dm.RPC_DeliverWaitingInstantDeliveries(); Print($"[probe] RPC_DeliverWaitingInstantDeliveries called, waiting={dm.WaitingDelivery?.Count}"); } catch (Exception ex) { Print($"[probe] instant RPC ERR {ex.Message}"); }
}
catch (Exception ex) { Print($"[probe] engine ERR {ex}"); }
for (int i = 0; dm.WaitingDelivery != null && i < dm.WaitingDelivery.Count; i++) { var w = dm.WaitingDelivery[i]; Print($"[probe] waiting id={w.ID} hour={w.Hour} shop={w.ShopType} target='{w.DeliveryTarget}' items={w.DeliveryItems?.Count} instant={w.IsInstant()}"); }
Print("[probe] done");
