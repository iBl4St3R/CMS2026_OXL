// Badanie G66 (2026-11-28): która metoda DeliveryManager odpala, gdy gracz bierze/otwiera paczkę. Podpina logujące postfixy Harmony (do restartu gry). Tylko odczyt.
// Harmony przez refleksję: REPL konsoli nie ma referencji do 0Harmony.
static void Post(System.Reflection.MethodBase __originalMethod, object[] __args)
{
    string a = "";
    try { foreach (var x in __args) a += (x == null ? "null" : x.ToString()) + "; "; } catch { }
    MelonLoader.MelonLogger.Msg($"[probe-take] {__originalMethod.Name}({a})");
}
var asm = System.AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "0Harmony");
var hType = asm.GetType("HarmonyLib.Harmony");
var hmType = asm.GetType("HarmonyLib.HarmonyMethod");
object h = Activator.CreateInstance(hType, "oxl.probe.takepackage");
object post = Activator.CreateInstance(hmType, ((Action<System.Reflection.MethodBase, object[]>)Post).Method);
var patch = hType.GetMethods().First(m => m.Name == "Patch" && m.GetParameters().Length == 5);
var t = typeof(Il2CppCMS.Managers.DeliveryManager);
foreach (var name in new[] { "TakeFromStorage", "RPC_TakePackage", "TakeItemsFromPackage", "SendDeliveryCollectEvent", "TakeItems", "RPC_RemovePackage" })
{
    foreach (var m in t.GetMethods())
    {
        if (m.Name != name) continue;
        try { patch.Invoke(h, new object[] { m, null, post, null, null }); Print($"[probe] patched {name}"); } catch (Exception ex) { Print($"[probe] {name} ERR {(ex.InnerException ?? ex).Message}"); }
    }
}
