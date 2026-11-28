// Pkt 4 (2026-11-28): czy PartCatalog.Classify (tabela z 5 aut 2026) rozpoznaje części nowych aut. Dla każdego auta: części z Fits tego auta
// (BlastCoreGameDataAPI) i ile wpada do Structural (zapas dla nieznanych). Wypisuje nieznane dla Bolta i Lacroixa. Tylko odczyt.
var parts = BlastCoreOS.BlastCoreGameDataAPI.GetParts().Where(p => p != null).ToList();
foreach (var car in new[] { "car_boltnevada", "car_lacroixmodiquer", "car_mayenm5", "car_dnbcensor", "car_katagiritamagobp", "car_luxorstreamlinermk3", "car_salemariesmk3" })
{
    var mine = parts.Where(p => p.Fits != null && p.Fits.Any(f => f.CarId == car)).ToList();
    var structural = mine.Where(p => CMS2026_OXL.PartCatalog.Classify(p.Id) == CMS2026_OXL.WearCat.Structural).ToList();
    Print($"[probe] {car}: parts={mine.Count} structural={structural.Count} body={structural.Count(p => p.IsBody)}");
    if (car == "car_boltnevada" || car == "car_lacroixmodiquer")
        Print("[probe]   non-body structural: " + string.Join(", ", structural.Where(p => !p.IsBody).Select(p => p.Id + "[" + p.ShopGroup + "]")));
}
