// photo_spots (G52): zapis miejsc do sesji zdjęciowej OXL (rebuild.md 2.1). Uruchom raz w świecie, siedząc w aucie; potem jeździsz i:
//   F9  = zapisz miejsce (pozycja i obrót auta, którym jedziesz),   F10 = usuń ostatnie,   END = zakończ.
// Plik: Mods/CMS2026_OXL/PhotoSpots.txt, linia "x;y;z;rotY" (InvariantCulture); dopisuje do istniejących (stare miejsca z 2026 nie pasują: na ulicach stoją teraz auta).
// Auto: SceneCarPhysicsManager/Car (Owner 1) jak w 2026; zapasowo rigidbody najbliżej kamery.

string file = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "PhotoSpots.txt");
var inv = System.Globalization.CultureInfo.InvariantCulture;

System.Func<UnityEngine.Transform> findCar = () =>
{
    var scpm = UnityEngine.GameObject.Find("SceneCarPhysicsManager");
    var t = scpm != null ? scpm.transform.Find("Car (Owner 1)") : null;
    if (t != null) return t;
    var cam = UnityEngine.Camera.main;
    UnityEngine.Transform best = null; float bestD = 15f;
    foreach (var rb in UnityEngine.Object.FindObjectsOfType<UnityEngine.Rigidbody>())
    {
        if (rb == null || rb.isKinematic || rb.mass < 500f) continue;
        float d = cam != null ? UnityEngine.Vector3.Distance(rb.transform.position, cam.transform.position) : 0f;
        if (d < bestD) { bestD = d; best = rb.transform; }
    }
    return best;
};

int count = System.IO.File.Exists(file) ? System.IO.File.ReadAllLines(file).Count(l => l.Trim().Length > 0 && !l.StartsWith("#")) : 0;
var probe = findCar();
Print($"[photo] spots file: {file} ({count} spots). Car: {(probe != null ? probe.name + " at " + probe.position : "NOT FOUND, get in a car in the world")}");
Print("[photo] F9 = save spot, F10 = remove last, END = stop. You can close the console now.");

System.Collections.IEnumerator SpotLoop()
{
    while (true)
    {
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.End)) { Print($"[photo] spots recorder stopped, {count} spots in file"); yield break; }
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F9))
        {
            var car = findCar();
            if (car == null) Print("[photo] no car found, get in a car");
            else
            {
                var p = car.position;
                string line = string.Format(inv, "{0:F3};{1:F3};{2:F3};{3:F1}", p.x, p.y, p.z, car.eulerAngles.y);
                System.IO.File.AppendAllText(file, line + "\n");
                count++;
                Print($"[photo] spot {count} saved: {line} ({car.name})");
            }
        }
        if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F10) && System.IO.File.Exists(file))
        {
            var lines = System.IO.File.ReadAllLines(file).Where(l => l.Trim().Length > 0).ToList();
            if (lines.Count > 0) { lines.RemoveAt(lines.Count - 1); System.IO.File.WriteAllLines(file, lines); count = lines.Count(l => !l.StartsWith("#")); Print($"[photo] last spot removed, {count} left"); }
        }
        yield return null;
    }
}
MelonLoader.MelonCoroutines.Start(SpotLoop());
