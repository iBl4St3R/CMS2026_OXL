// Felgi i opony (2026-11-28): co daje Helper.GetRandomWheelData, czy ustawienie Item.WheelData się trzyma i jak gra to opisuje (UIHelper). Tylko odczyt (bez dostawy).
for (int i = 0; i < 5; i++) { var v = Il2Cpp.Helper.GetRandomWheelData(); Print($"[probe] random wheel x={v.x} y={v.y} z={v.z}"); }
foreach (var id in new[] { "tire_standard", "rim_46B" })
{
    var it = new Il2CppCMS.Player.Containers.Item(id);
    var w = new Il2Cpp.WheelData();
    w.Width = 225; w.Profile = 75; w.Size = 15; w.ET = 30;
    it.WheelData = w;
    var back = it.WheelData;
    string ts = "", rs = "";
    try { ts = Il2CppCMS.Helpers.UIHelper.GetTireDataString(back); } catch (Exception ex) { ts = "ERR " + ex.Message; }
    try { rs = Il2CppCMS.Helpers.UIHelper.GetRimDataString(back); } catch (Exception ex) { rs = "ERR " + ex.Message; }
    Print($"[probe] {id}: back size={back.Size} width={back.Width} profile={back.Profile} ET={back.ET} tireStr='{ts}' rimStr='{rs}'");
}
