// Pkt 4 (2026-11-28): podgląd nowej reguły PartCatalog (prefiks silnika) przed buildem: tabela przez refleksję, ta sama logika co w Classify.
var t = (System.Collections.Generic.Dictionary<string, CMS2026_OXL.WearCat>)typeof(CMS2026_OXL.PartCatalog).GetField("_table", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
var byBase = new System.Collections.Generic.Dictionary<string, CMS2026_OXL.WearCat>(); var amb = new System.Collections.Generic.HashSet<string>();
foreach (var kv in t) { var p = kv.Key.Split('_'); for (int k = 1; k <= 3 && k < p.Length; k++) { string rest = string.Join("_", p, k, p.Length - k); if (rest.Length < 4 || amb.Contains(rest)) continue; if (byBase.TryGetValue(rest, out var prev) && prev != kv.Value) { byBase.Remove(rest); amb.Add(rest); } else byBase[rest] = kv.Value; } }
Func<string, CMS2026_OXL.WearCat> cls = id =>
{
    var c = CMS2026_OXL.PartCatalog.Classify(id);
    if (c != CMS2026_OXL.WearCat.Structural) return c;
    string norm = System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(id.ToLower(), @"(_\d+|_double)+$", ""), @"\d+$", "");
    var segs = norm.Split('_');
    for (int k = 1; k <= 3 && k < segs.Length; k++) { string rest = string.Join("_", segs, k, segs.Length - k); for (string r = rest; r.Length >= 4; r = r.Contains("_") ? r.Substring(0, r.LastIndexOf('_')) : "") { if (t.TryGetValue(r, out var x)) return x; if (byBase.TryGetValue(r, out x)) return x; } }
    return CMS2026_OXL.WearCat.Structural;
};
var parts = BlastCoreOS.BlastCoreGameDataAPI.GetParts().Where(p => p != null && p.Fits != null && p.Fits.Any(f => f.CarId == "car_boltnevada") && !p.IsBody).ToList();
Print("[probe] bolt now classified: " + string.Join(", ", parts.Where(p => CMS2026_OXL.PartCatalog.Classify(p.Id) == CMS2026_OXL.WearCat.Structural && cls(p.Id) != CMS2026_OXL.WearCat.Structural).Select(p => p.Id + "=" + cls(p.Id))));
Print("[probe] bolt still structural: " + string.Join(", ", parts.Where(p => cls(p.Id) == CMS2026_OXL.WearCat.Structural).Select(p => p.Id)));
