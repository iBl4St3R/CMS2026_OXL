// G87 (2026-11-28): stan części silnika kupionego auta według kategorii PartCatalog (średnia i zakres), plus części jeszcze w Structural.
// Auto: CarLoader podany w nazwie (tu #CarLoader5). Tylko odczyt.
var cl = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Core.Car.CarLoader>().FirstOrDefault(c => c.gameObject.name == "#CarLoader5");
if (cl == null || cl.engineHandle == null) { Print("[probe] no car/engine"); return; }
var ps = cl.engineHandle.GetComponentsInChildren<Il2CppCMS.Core.Car.PartScript>(true).Where(p => !p.IsUnmounted).ToList();
Print($"[probe] {cl.CarID} engine={cl.GetEngineName()} parts={ps.Count}");
foreach (var g in ps.GroupBy(p => CMS2026_OXL.PartCatalog.Classify(p.id)).OrderBy(g => g.Key.ToString()))
{
    var c = g.Select(p => p.CreateItem().Condition / 255f).ToList();
    Print($"[probe]   {g.Key}: n={c.Count} avg={c.Average():P0} min={c.Min():P0} max={c.Max():P0}" + (g.Key == CMS2026_OXL.WearCat.Structural ? " [" + string.Join(",", g.Select(p => p.id).Distinct()) + "]" : ""));
}
