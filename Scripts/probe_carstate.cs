// probe_carstate (G6): potwierdza API 2027 użyte w GameBridge po porcie. Tylko odczyt. Uruchom w garażu z przynajmniej 1 autem, PO zbudowaniu OXL (używa CMS2026_OXL.GameCarAccess).
// Sprawdza: części karoserii (skąd i ile), CarInfoData przez natywny getter (przebieg, rok), PartScript, pieniądze, gdzie w scenie leżą CarPartBase.
var loaders = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<CarLoader>());
Print($"[probe] loaders={loaders.Length}");
try { Print($"[probe] money={Il2CppCMS.Shared.SharedGameDataManager.GetMoney()}"); } catch (Exception ex) { Print($"[probe] money ERR {ex.Message}"); }
try
{
    var all = UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<Il2CppCMS.Core.Car.Body.CarPartBase>(), true);
    Print($"[probe] scene CarPartBase={all.Length}");
    var byType = new System.Collections.Generic.Dictionary<string, int>();
    for (int i = 0; i < all.Length; i++)
    {
        var p = all[i].TryCast<Il2CppCMS.Core.Car.Body.CarPartBase>();
        if (p == null) continue;
        string tn = p.GetIl2CppType().Name;
        byType[tn] = byType.TryGetValue(tn, out var n) ? n + 1 : 1;
        if (i < 6)
        {
            string owner = "null"; try { owner = p.CarLoader?.CarID ?? "null"; } catch (Exception ex) { owner = "ERR " + ex.Message; }
            string path = p.name; var t = p.transform.parent; int d = 0; while (t != null && d < 6) { path = t.name + "/" + path; t = t.parent; d++; }
            Print($"[probe] scene part {tn} owner={owner} path={path}");
        }
    }
    foreach (var kv in byType) Print($"[probe] scene type {kv.Key}={kv.Value}");
}
catch (Exception ex) { Print($"[probe] scene ERR {ex.Message}"); }
for (int li = 0; li < loaders.Length; li++)
{
    var cl = loaders[li].TryCast<CarLoader>();
    if (cl == null) continue;
    string id = "";
    try { id = cl.CarID; } catch { }
    if (string.IsNullOrWhiteSpace(id)) { Print($"[probe] loader#{li} empty"); continue; }
    try { Print($"[probe] loader#{li} car={id} loaded={cl.IsCarLoaded()} dent={cl.GetDentValue():F2} dust={cl.GetDustValue():F2} go={cl.gameObject.name}"); } catch (Exception ex) { Print($"[probe] loader#{li} car={id} state ERR {ex.Message}"); }
    try { bool ok = CMS2026_OXL.GameCarAccess.TryReadCarInfo(cl, out uint m, out ushort y); Print($"[probe]   info ok={ok} mileage={m} year={y}"); } catch (Exception ex) { Print($"[probe]   info ERR {ex.Message}"); }
    try
    {
        var bp = CMS2026_OXL.GameCarAccess.GetBodyParts(cl, out string src);
        Print($"[probe]   bodyParts={bp.Count} source={src}");
        for (int i = 0; i < bp.Count && i < 8; i++) { var p = bp[i]; Print($"[probe]   body '{p.name}' cond={p.Condition:F2} dent={p.Dent:F2} dust={p.Dust:F2} active={p.gameObject.activeInHierarchy}"); }
    }
    catch (Exception ex) { Print($"[probe]   bodyParts ERR {ex.Message}"); }
    try { var ps = cl.GetAllPartScriptsCopy(); Print($"[probe]   partScripts={ps.Count}"); } catch (Exception ex) { Print($"[probe]   partScripts ERR {ex.Message}"); }
}
Print("[probe] done");
