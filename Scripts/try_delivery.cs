// try_delivery (G14): czy OXL umie nadać paczkę przez system przesyłek gry (site.md 9.5). ZMIENIA STAN: zapisz grę przed.
// Nadaje jedną paczkę "UsedParts" z drogą częścią (stan ~40%) i jedną "DecorationsTools" z krzesłem, na najbliższą godzinę dostaw.
// Sprawdza też, czy EngineGroupItem(id) sam wypełnia części silnika (bez nadawania). Cofnięcie: wczytaj zapis.
var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
if (dm == null) { Print("[probe] no DeliveryManager"); return; }
int before = dm.WaitingDelivery?.Count ?? -1;
byte hour = 10; try { hour = dm.GetNextDeliveryTime(); } catch (Exception ex) { Print($"[probe] next ERR {ex.Message}"); }
string target = ""; try { if (dm.WaitingDelivery != null && dm.WaitingDelivery.Count > 0) target = dm.WaitingDelivery[0].DeliveryTarget; } catch { }
Print($"[probe] before waiting={before} hour={hour} target='{target}'");

Func<Il2CppCMS.Player.Containers.IBaseItem, uint, Il2CppCMS.Containers.DeliveryItem> mk = (bi, amount) =>
{
    var di = new Il2CppCMS.Containers.DeliveryItem();
    di.Data = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.FromIBaseItem(bi);
    di.Amount = amount;
    return di;
};

// ── Część: blok silnika V6 (2100), stan obniżony ──
try
{
    var item = new Il2CppCMS.Player.Containers.Item("v6_231_blok");
    byte condBefore = item.Condition;
    item.Condition = 100;
    item.IsExamined = false;
    var bi = item.TryCast<Il2CppCMS.Player.Containers.IBaseItem>();
    Print($"[probe] part id={item.ID} uid={item.UID} condBefore={condBefore} condAfter={item.Condition} show={bi?.GetConditionToShow()} name='{bi?.GetLocalizedName()}'");
    var d = new Il2CppCMS.Containers.Delivery(hour, Il2CppCMS.UI.Logic.ShopType.UsedParts, target);
    d.AddItem(mk(bi, 1));
    dm.OrderDelivery(d);
    Print($"[probe] ordered UsedParts id={d.ID} instant={d.IsInstant()} size={d.GetPackageSize()}");
}
catch (Exception ex) { Print($"[probe] part ERR {ex}"); }

// ── Dekoracja: krzesło ──
try
{
    var deco = new Il2CppCMS.Player.Containers.CustomizableItem("Chair_1");
    var bi = deco.TryCast<Il2CppCMS.Player.Containers.IBaseItem>();
    Print($"[probe] deco id={bi?.ID} name='{bi?.GetLocalizedName()}'");
    var d = new Il2CppCMS.Containers.Delivery(hour, Il2CppCMS.UI.Logic.ShopType.DecorationsTools, target);
    d.AddItem(mk(bi, 1));
    dm.OrderDelivery(d);
    Print($"[probe] ordered DecorationsTools id={d.ID} instant={d.IsInstant()}");
}
catch (Exception ex) { Print($"[probe] deco ERR {ex}"); }

// ── Silnik: tylko sprawdzenie zawartości ──
try
{
    var eng = new Il2CppCMS.Player.Containers.EngineGroupItem("engine25_v6_231_rwd");
    Print($"[probe] engine id={eng.ID} items={eng.ItemList?.Count ?? -1} size={eng.Size} cond={eng.GetConditionToShow()} name='{eng.GetLocalizedName()}'");
}
catch (Exception ex) { Print($"[probe] engine ERR {ex}"); }

int after = dm.WaitingDelivery?.Count ?? -1;
Print($"[probe] after waiting={after}");
for (int i = 0; dm.WaitingDelivery != null && i < dm.WaitingDelivery.Count; i++) { var w = dm.WaitingDelivery[i]; Print($"[probe] waiting id={w.ID} hour={w.Hour} shop={w.ShopType} items={w.DeliveryItems?.Count}"); }
Print("[probe] done");
