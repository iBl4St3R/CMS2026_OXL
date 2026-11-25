// probe_versions (G48): które wersje aut mają felgi / opony / inne części spoza dema (zawieszenie wczytywania zapisu 2026-11-25:
// Wheels.SpawnRim -> "The Object you want to instantiate is null"). Tylko odczyt. Uruchom w garażu albo w menu głównym gry.
// Dla każdej wersji: wszystkie wpisy sekcji z kołami (rim, tire, wheel) i czy przedmiot jest w tej wersji gry (HaveDLC / katalog / prefab LoadGo).
var inv = Il2CppCMS.Core.GameInventory.Instance;
Print($"[probe] inv={(inv != null)}");
Func<string, string> avail = id =>
{
    if (string.IsNullOrEmpty(id)) return "-";
    bool dlc = false, cat = false, go = false;
    try { dlc = inv.HaveDLC(id); } catch { }
    try { cat = inv.ExistsInPartProperty(id); } catch { }
    try { go = inv.LoadGo(id) != null; } catch { }
    return $"dlc={dlc} cat={cat} go={go}";
};
try
{
    var cars = UnityEngine.Object.FindObjectOfType<Il2Cpp.CarBundleLoader>(true)?.CarNamesData;
    for (int ci = 0; cars != null && ci < cars.Count; ci++)
    {
        var data = cars[ci];
        if (data?.CarConfigs == null) continue;
        for (int i = 0; i < data.CarConfigs.Count; i++)
        {
            var ini = data.CarConfigs[i];
            string engine = "", name = "";
            var wheel = new System.Collections.Generic.List<string>();
            for (int k = 0; ini?.Entries != null && k < ini.Entries.Count; k++)
            {
                var e = ini.Entries[k];
                string sec = e.Section ?? "", id = e.ID ?? "", v = (e.Value ?? "").Trim();
                if (sec == "6_engine" && id == "type") engine = v;
                if (sec == "0_main" && id == "carVersionName") name = v;
                string s = (sec + " " + id).ToLowerInvariant();
                if (s.Contains("wheel") || s.Contains("rim") || s.Contains("tire") || s.Contains("tyre")) wheel.Add($"{sec}.{id}={v}");
            }
            Print($"[probe] car {data.CarID} cfg={i} '{name}' engine={engine} [{avail(engine)}]");
            foreach (var w in wheel)
            {
                string val = w.Substring(w.IndexOf('=') + 1);
                bool looksId = val.Length > 2 && val.IndexOf(' ') < 0 && !char.IsDigit(val[0]);
                Print($"[probe]   {w}{(looksId ? "  [" + avail(val) + "]" : "")}");
            }
        }
    }
}
catch (Exception ex) { Print($"[probe] ERR {ex}"); }
Print("[probe] done");
