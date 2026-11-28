// Badanie G40/G51: czy paczki OXL przekazane kurierowi gry dalej czekają (WaitingDelivery), czy już przyszły. Tylko odczyt.
var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
if (dm == null) { Print("[probe] no DeliveryManager"); return; }
int n = dm.WaitingDelivery == null ? -1 : dm.WaitingDelivery.Count;
Print($"[probe] waiting={n}");
for (int i = 0; i < n; i++) { var w = dm.WaitingDelivery[i]; Print($"[probe] waiting id={w.ID} hour={w.Hour} shop={w.ShopType} target='{w.DeliveryTarget}' items={w.DeliveryItems?.Count}"); }
