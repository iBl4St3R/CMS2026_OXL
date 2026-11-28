// Zakładka Parts (2026-11-28): wszystkie części z BlastCoreGameDataAPI.GetParts po grupach sklepu: liczba, ceny, karoseria, dostępność, przykłady. Tylko odczyt.
var parts = BlastCoreOS.BlastCoreGameDataAPI.GetParts().Where(p => p != null).ToList();
Print($"[probe] parts={parts.Count} price0={parts.Count(p => p.Price <= 0)}");
foreach (var g in parts.GroupBy(p => (p.ShopGroup ?? "") + " | " + (p.ShopName ?? "")).OrderByDescending(g => g.Count()))
{
    var pr = g.Select(p => p.Price).OrderBy(x => x).ToList();
    string ex = string.Join(", ", g.OrderByDescending(p => p.Price).Take(4).Select(p => $"{p.Name}[{p.Id}] ${p.Price}"));
    Print($"[probe] {g.Key}: n={g.Count()} body={g.Count(p => p.IsBody)} avail={string.Join("/", g.GroupBy(p => p.Availability).Select(a => a.Key + ":" + a.Count()))} price {pr.First()}..{pr.Last()} med {pr[pr.Count / 2]} | {ex}");
}
var all = parts.Where(p => p.Price > 0).Select(p => p.Price).OrderBy(x => x).ToList();
Print($"[probe] p10={all[all.Count / 10]} p25={all[all.Count / 4]} p50={all[all.Count / 2]}");
