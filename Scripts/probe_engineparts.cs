// Silnik z OXL bez modelu na stojaku (2026-11-28): z czego składa się każdy cały silnik w EnginePartsLookup (liczba części, ile ma HaveDLC, przykłady). Tylko odczyt.
var inv = Il2CppCMS.Core.GameInventory.Instance;
var look = inv.EngineDataProvider.EnginePartsLookup;
foreach (var id in new[] { "engine25_i4_head1_turbo_fwd_stroker", "engine25_r4_sohc_fwd", "engine25_v6_231_1carb_rwd", "engine25_v8_350_head1_rwd", "engine25_v8_d62_turbo_rwd" })
{
    if (!look.ContainsKey(id)) { Print($"[probe] {id}: not in lookup"); continue; }
    var parts = look[id].EngineParts;
    int n = parts == null ? 0 : parts.Count, dlc = 0;
    string ex = "";
    for (int i = 0; i < n; i++) { bool ok = false; try { ok = inv.HaveDLC(parts[i]); } catch { } if (ok) dlc++; if (i < 6) ex += parts[i] + (ok ? "" : "(!)") + ", "; }
    bool edp = inv.EngineDataProvider.TryGet(id, out var pd);
    Print($"[probe] {id}: parts={n} haveDLC={dlc} physics={edp} haveDLC(engine)={inv.HaveDLC(id)} | {ex}");
}
