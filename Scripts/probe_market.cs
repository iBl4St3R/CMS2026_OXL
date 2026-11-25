// probe_market (G13): dane gry pod nową stronę OXL (site.md 4, 6). Tylko odczyt. Uruchom w garażu.
// A: czy silniki wersji aut są w pełni dostępne w demie (G12: DNB Censor cfg 1 i 2 psują zapis). B: katalog części (grupy, ceny, najdroższe).
// C: narzędzia i dekoracje (GarageObjectsProvider). D: miniatury. E: system przesyłek gry (DeliveryManager).
var inv = Il2CppCMS.Core.GameInventory.Instance;
Print($"[probe] inv={(inv != null)}");
var edp = inv?.EngineDataProvider;
var thumbs = inv?.ThumbnailsProvider;
Func<string, string> thumbOf = id => { try { if (thumbs == null) return "noProvider"; return thumbs.TryGetThumbnail(id, out var sp) && sp != null ? $"yes({sp.name})" : "no"; } catch (Exception ex) { return "ERR " + ex.Message; } };

// ── A: silniki wersji aut ─────────────────────────────────────────────────
try
{
    var cars = UnityEngine.Object.FindObjectOfType<Il2Cpp.CarBundleLoader>(true)?.CarNamesData;
    for (int ci = 0; cars != null && ci < cars.Count; ci++)
    {
        var data = cars[ci];
        if (data?.CarConfigs == null) continue;
        for (int i = 0; i < data.CarConfigs.Count; i++)
        {
            var ini = data.CarConfigs[i]; string engine = "";
            for (int k = 0; ini?.Entries != null && k < ini.Entries.Count; k++) { var e = ini.Entries[k]; if (e.Section == "6_engine" && e.ID == "type") engine = (e.Value ?? "").Trim(); }
            bool phys = false; string physName = ""; try { phys = edp != null && edp.TryGet(engine, out var pd); if (phys) { edp.TryGet(engine, out var pd2); physName = pd2 != null ? pd2.name : "null"; } } catch (Exception ex) { physName = "ERR " + ex.Message; }
            bool dlc = false; try { dlc = inv.HaveDLC(engine); } catch { }
            bool inCatalog = false; try { inCatalog = inv.ExistsInPartProperty(engine); } catch { }
            string carThumb = "?"; try { var s = thumbs?.GetCarThumb(data.CarID, i); carThumb = s == null ? "null" : (s == thumbs.GetNoImageCar() ? "noImage" : s.name); } catch (Exception ex) { carThumb = "ERR " + ex.Message; }
            Print($"[probe] A car {data.CarID} cfg={i} engine={engine} physData={phys} ({physName}) haveDLC={dlc} inCatalog={inCatalog} engineThumb={thumbOf(engine)} carThumb={carThumb}");
        }
    }
}
catch (Exception ex) { Print($"[probe] A ERR {ex.Message}"); }

// ── B: katalog części ────────────────────────────────────────────────────
try
{
    var ppl = inv.PartPropertyList;
    Print($"[probe] B parts={ppl.Count}");
    var groups = new System.Collections.Generic.Dictionary<string, int[]>(); // count, min, max, dlc, avail!=0
    var all = new System.Collections.Generic.List<Il2Cpp.PartProperty>();
    foreach (var kv in ppl)
    {
        var p = kv.Value; if (p == null) continue; all.Add(p);
        string g = (p.ShopGroup ?? "") + "/" + (p.ShopName ?? "");
        if (!groups.TryGetValue(g, out var a)) { a = new[] { 0, int.MaxValue, 0, 0, 0 }; groups[g] = a; }
        a[0]++; a[1] = Math.Min(a[1], p.Price); a[2] = Math.Max(a[2], p.Price); if (p.HaveDLC) a[3]++; if ((int)p.Availability != 0) a[4]++;
    }
    foreach (var kv in groups.OrderByDescending(x => x.Value[2])) Print($"[probe] B group {kv.Key} count={kv.Value[0]} price={kv.Value[1]}..{kv.Value[2]} haveDLC={kv.Value[3]} availNon0={kv.Value[4]}");
    var avail = all.GroupBy(p => p.Availability.ToString()).Select(g => $"{g.Key}={g.Count()}");
    Print($"[probe] B availability {string.Join(" ", avail)}");
    var prices = all.Select(p => (int)p.Price).OrderBy(x => x).ToList();
    Print($"[probe] B price p50={prices[prices.Count / 2]} p75={prices[prices.Count * 3 / 4]} p90={prices[prices.Count * 9 / 10]} p97={prices[prices.Count * 97 / 100]} max={prices[prices.Count - 1]}");
    foreach (var p in all.OrderByDescending(p => p.Price).Take(25)) Print($"[probe] B top {p.ID} '{p.LocalizedName}' price={p.Price} group={p.ShopGroup}/{p.ShopName} carID={p.CarID} body={p.IsBody} dlc={p.HaveDLC} avail={p.Availability} thumb={thumbOf(p.ID)}");
}
catch (Exception ex) { Print($"[probe] B ERR {ex.Message}"); }

// ── C: narzędzia i dekoracje ─────────────────────────────────────────────
try
{
    var gop = inv.GarageObjectsProvider;
    var objs = gop?.GetAllCustomizableObjects();
    Print($"[probe] C garageObjects loaded={gop?.DataLoaded} count={(objs == null ? -1 : objs.Count)}");
    for (int i = 0; objs != null && i < objs.Count; i++)
    {
        var o = objs[i]; if (o == null) continue;
        string cat = "?"; try { cat = inv.GetCustomizableItemCategory(o.ObjectID).ToString(); } catch { }
        string name = "?"; try { name = inv.GetCustomizableItemLocalizeName(o.ObjectID); } catch { }
        Print($"[probe] C obj {o.ObjectID} '{name}' type={o.ObjectType} cat={cat} price={o.Price} brand={o.Brand} mod={o.IsMod} thumb={thumbOf(o.ObjectID)}");
    }
}
catch (Exception ex) { Print($"[probe] C ERR {ex.Message}"); }

// ── E: przesyłki gry ─────────────────────────────────────────────────────
try
{
    var dm = UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>();
    Print($"[probe] E deliveryManager={(dm != null)}");
    if (dm != null)
    {
        try { var hours = Il2CppCMS.Managers.DeliveryManager.GetDeliveryHours(); var hs = new System.Collections.Generic.List<string>(); for (int i = 0; i < hours.Length; i++) hs.Add(hours[i].ToString()); Print($"[probe] E deliveryHours={string.Join(",", hs)} next={dm.GetNextDeliveryTime()} afterTime={Il2CppCMS.Managers.DeliveryManager.IsAfterDeliveryTime()} empty={dm.IsEmpty()}"); } catch (Exception ex) { Print($"[probe] E hours ERR {ex.Message}"); }
        var w = dm.WaitingDelivery;
        Print($"[probe] E waiting={(w == null ? -1 : w.Count)}");
        for (int i = 0; w != null && i < w.Count; i++)
        {
            var d = w[i];
            Print($"[probe] E delivery id={d.ID} hour={d.Hour} shop={d.ShopType} target='{d.DeliveryTarget}' items={d.DeliveryItems?.Count} instant={d.IsInstant()}");
            for (int j = 0; d.DeliveryItems != null && j < d.DeliveryItems.Count && j < 5; j++) { var di = d.DeliveryItems[j]; var bi = di.Data?.BaseItem; Print($"[probe] E   item amount={di.Amount} id={bi?.ID}"); }
        }
    }
}
catch (Exception ex) { Print($"[probe] E ERR {ex.Message}"); }
Print("[probe] done");
