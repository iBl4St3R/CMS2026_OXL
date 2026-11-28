// Ceny całych silników (2026-11-28): katalog podaje 5005 dla każdego (zastępcza). Dla każdego silnika z EngineShop: suma cen nowych części,
// Helper.GetPrice i Helper.GetItemSellPrice grupy złożonej jak w OXLDelivery przy stanie 100%, 70%, 30%. Tylko odczyt (nic nie wysyła).
var inv = Il2CppCMS.Core.GameInventory.Instance;
var look = inv.EngineDataProvider.EnginePartsLookup;
var shop = BlastCoreOS.BlastCoreGameDataAPI.GetParts().Where(p => p != null).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
Func<string, float, Il2CppCMS.Player.Containers.IBaseItem> build = (id, cond) =>
{
    var cfg = look[id];
    var eng = new Il2CppCMS.Player.Containers.EngineGroupItem(id);
    for (int i = 0; i < cfg.EngineParts.Count; i++)
    {
        string p = cfg.EngineParts[i], path = cfg.EnginePartsPaths[i];
        var it = new Il2CppCMS.Player.Containers.Item(path.Substring(path.LastIndexOf('/') + 1));
        it.NormalID = p; it.Condition = (byte)Math.Max(1, Math.Round(cond * 255)); it.IsExamined = true;
        eng.ItemList.Add(it);
    }
    return eng.TryCast<Il2CppCMS.Player.Containers.IBaseItem>();
};
foreach (var e in BlastCoreOS.BlastCoreGameDataAPI.GetEngines().Where(x => x != null && x.Available && x.BuyableComplete))
{
    if (!look.ContainsKey(e.Id)) continue;
    var cfg = look[e.Id];
    int sum = 0; for (int i = 0; i < cfg.EngineParts.Count; i++) if (shop.TryGetValue(cfg.EngineParts[i], out var sp)) sum += sp.Price;
    string s = "";
    foreach (var c in new[] { 1f, 0.7f, 0.3f }) { var it = build(e.Id, c); int price = Il2Cpp.Helper.GetPrice(it, 1f); int sell = Il2Cpp.Helper.GetItemSellPrice(it); s += $" | {c:P0}: price={price} sell={sell}"; }
    Print($"[probe] {e.Id} ({e.Name}) parts={cfg.EngineParts.Count} sumNew={sum} catalog={e.Price}{s}");
}
