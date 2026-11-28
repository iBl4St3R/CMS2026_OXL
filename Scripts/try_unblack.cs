// Naprawa po try_standcreate.cs (2026-11-28): CreateEngine bez okna zostawił czarny ekran z wczytywaniem i zablokowaną postać. Rozjaśnia i odblokowuje.
var faders = UnityEngine.Object.FindObjectsOfType<Il2CppCMS.ScreenFader>(true);
foreach (var f in faders) { try { f.HideLoadingInstant(); f.SetClear(); Print("[fix] fader cleared: " + f.gameObject.name); } catch (Exception ex) { Print("[fix] fader ERR " + ex.Message); } }
foreach (var s in UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Garage.Tools.EngineStand>())
{
    try { s.UnlockPlayer(); } catch (Exception ex) { Print("[fix] unlock ERR " + ex.Message); }
}
Print("[fix] done, faders=" + faders.Length);
