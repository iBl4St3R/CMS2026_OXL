// probe_engines (G12): skąd w zapisie silniki "V6 322BZ 66B/R 4X4" (engine25_v6_231_scharged_4x4) i "V8 AXK5 370 4X4" (engine25_v8_head1_intake3_NA_4x4),
// których gra nie umie wczytać ("There are problem with following assets"). Tylko odczyt. Uruchom w garażu.
// Wypisuje: dla każdej wersji auta (CarConfigs[i] = CarConfig w OXL) nazwę, napęd i silnik z [6_engine] oraz czy silnik jest w EnginePartsLookup; potem silniki aut stojących w garażu.
var inv = Il2CppCMS.Core.GameInventory.Instance;
var look = inv?.EngineDataProvider?.EnginePartsLookup;
Print($"[probe] engineLookup={(look == null ? -1 : look.Count)}");
Func<string, bool> inLookup = id => { try { return look != null && look.ContainsKey(id); } catch { return false; } };
string[] suspects = { "engine25_v6_231_scharged_4x4", "engine25_v8_head1_intake3_NA_4x4" };
foreach (var s in suspects) Print($"[probe] suspect {s} inLookup={inLookup(s)}");
try
{
    var cars = UnityEngine.Object.FindObjectOfType<Il2Cpp.CarBundleLoader>(true)?.CarNamesData;
    Print($"[probe] cars={(cars == null ? -1 : cars.Count)}");
    for (int ci = 0; cars != null && ci < cars.Count; ci++)
    {
        var data = cars[ci];
        if (data == null || data.CarConfigs == null) continue;
        for (int i = 0; i < data.CarConfigs.Count; i++)
        {
            var ini = data.CarConfigs[i];
            string name = "", drive = "", engine = "", swaps = "", other = "";
            for (int k = 0; ini?.Entries != null && k < ini.Entries.Count; k++)
            {
                var e = ini.Entries[k];
                string sec = e.Section ?? "", id = e.ID ?? "", v = (e.Value ?? "").Trim();
                if (sec == "0_main" && id == "carVersionName") name = v;
                else if (sec == "1_other" && id == "transmissionType") drive = v;
                else if (sec == "6_engine" && id == "type") engine = v;
                else if (sec == "6_engine" && id == "swapoptions") swaps = v;
                else if (sec == "6_engine" || sec == "engines") other += $" {sec}.{id}={v}";
            }
            Print($"[probe] car {data.CarID} cfg={i} name='{name}' drive={drive} engine={engine} inLookup={inLookup(engine)} swaps='{swaps}'{other}");
        }
    }
}
catch (Exception ex) { Print($"[probe] cars ERR {ex.Message}"); }
var loaders = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<CarLoader>());
for (int li = 0; li < loaders.Length; li++)
{
    var cl = loaders[li].TryCast<CarLoader>();
    if (cl == null) continue;
    string id = ""; try { id = cl.CarID; } catch { }
    if (string.IsNullOrWhiteSpace(id)) continue;
    string eng = "?", toLoad = "?";
    try { eng = cl.GetEngineName(); } catch (Exception ex) { eng = "ERR " + ex.Message; }
    try { toLoad = cl.GetNameOfEngineToLoad(); } catch (Exception ex) { toLoad = "ERR " + ex.Message; }
    Print($"[probe] garage loader#{li} car={id} engineName='{eng}' engineToLoad='{toLoad}' inLookup={inLookup(toLoad)}");
}
Print("[probe] done");
