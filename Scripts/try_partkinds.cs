// Zakładka Parts (2026-11-28): czy kurier OXL (Item(id)) dowozi części spoza dotychczasowego katalogu: karoseria, felga, opona, ABS, wydech, fotel.
// ZMIENIA STAN: darmowe paczki od razu do garażu.
var parts = BlastCoreOS.BlastCoreGameDataAPI.GetParts();
string door = parts.Where(p => p != null && p.IsBody && p.Id.Contains("door")).Select(p => p.Id).FirstOrDefault() ?? "car_mayenm5-hood";
foreach (var id in new[] { "car_mayenm5-hood", door, "rim_46B", "tire_standard", "absModul_1", "w_tlumik_koncowy_6b", "seat_20_leather1" })
{
    var r = CMS2026_OXL.OXLDelivery.Send(CMS2026_OXL.OXLItemKind.Part, id, 0.6f, true);
    Print($"[probe] {id}: ok={r.Ok} id={r.DeliveryId} err='{r.Error}'");
}
