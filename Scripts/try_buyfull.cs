// G71 (2026-11-28): kupno najtańszego auta przez TryBuyCar przy pełnym parkingu. Oczekiwane: odmowa "parking full", saldo bez zmian, ogłoszenie zostaje.
// ZMIENIA STAN tylko przy wolnym miejscu (wtedy auto kupione naprawdę).
var panel = CMS2026_OXL.OXLWebPage.Backend;
var list = panel.GetActiveListings();
var l = list.OrderBy(x => x.Price).First();
Print($"[probe] buy {l.Make} {l.Model} {l.Year} ${l.Price} listings={list.Count} bank={CMS2026_OXL.GameBridge.GetBankAvailable()}");
bool started = panel.TryBuyCar(l, (ok, msg) => MelonLoader.MelonLogger.Msg($"[probe-buy] done ok={ok} msg='{msg}' listings={panel.GetActiveListings().Count} bank={CMS2026_OXL.GameBridge.GetBankAvailable()} still={panel.GetActiveListings().Contains(l)}"), out string m);
Print($"[probe] started={started} message='{m}'");
