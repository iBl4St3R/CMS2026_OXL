// Badanie G69/G55: które ogłoszenia aut mają zdjęcia z paczki, a które zostały ze zdjęciem z gry. Tylko odczyt (Photos() może dopisać zdjęcia staremu ogłoszeniu, jak strona).
var panel = CMS2026_OXL.OXLWebPage.Backend;
if (panel == null) { Print("panel null"); return; }
var list = panel.GetActiveListings();
Print("decoder=" + CMS2026_OXL.CarPhotoPack.DecoderAvailable + " listings=" + list.Count);
int stock = 0;
foreach (var l in list)
{
    var ph = CMS2026_OXL.CarPhotoPack.Photos(l);
    if (ph.Count == 0) stock++;
    string first = ph.Count > 0 ? ph[0] : (l.PhotoFiles != null && l.PhotoFiles.Count > 0 ? "OLD:" + l.PhotoFiles[0] : "-");
    Print($"{l.InternalId}#{l.CarConfig} {l.Archetype} body={l.BodyCondition:F2} folder={CMS2026_OXL.CarPhotoPack.ConditionFolder(l)} color={l.Color} photos={ph.Count} {first}");
}
Print("stock=" + stock);
