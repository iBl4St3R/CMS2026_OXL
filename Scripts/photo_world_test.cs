// photo_world (G53, G54): fotograf paczek zdjęć OXL w świecie (rebuild.md etap 2). ZMIENIA STAN AUTA: osobny zapis, nie zapisuj gry potem.
// Ty wybierasz auto (wjeżdżasz nim do świata), skrypt robi resztę: każdy kolor fabryczny x 3 przedziały stanu x kilka losowych miejsc z PhotoSpots.txt
// (photo_spots.cs) x kilka ujęć. Zdjęcia: Mods/CMS2026_OXL/Resources/CarPhotos/{carId}/{cfg}/{kolor}/{stan}/{miejsce}_{nr}.jpg, 1280x720 JPG q92.
// Celowo 2-3x za dużo zdjęć: nieudane (przenikanie z otoczeniem) usuwasz ręcznie. WebP: konwersja ImageMagick poza grą.
// Wznawianie: stan, który ma już zdjęcia, jest pomijany. Stop: przytrzymaj END. Po starcie zamknij konsolę i nie ruszaj myszy.
// Wersja auta: OXL sprzedaje tylko bezpieczne wersje (MarketCatalog); niebezpieczna jest pomijana z komunikatem.

bool TEST = true;    // krótki test (2 kolory, 1 stan, 2 miejsca, 3 ujęcia); pełna sesja: photo_world.cs

MelonLoader.MelonCoroutines.Start(PhotoRoutine());

System.Collections.IEnumerator PhotoRoutine()
{
    string root = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "CarPhotos");
    string spotsFile = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "PhotoSpots.txt");
    int outW = 1280, outH = 720, jpgQuality = 92;
    int spotsPerCond = TEST ? 2 : 4, shotsPerSpot = TEST ? 3 : 5;
    var conds = new (string folder, float min, float max)[] { ("030", 0.05f, 0.30f), ("3060", 0.30f, 0.60f), ("60100", 0.60f, 1.00f) };
    // Ujęcia zewnętrzne ze starego fotografa 2026 (az: kąt wokół auta, dist, elev, punkt patrzenia względem auta, roll).
    var shots = new (float az, float dist, float elev, float lkX, float lkY, float lkZ, float roll)[] {
        ( 18f, 4.0f, 1.4f,  0.00f, 0.50f,  0.00f, 0f), ( 36f, 4.2f, 1.4f, 0.00f, 0.50f, 0.00f, 0f), (335f, 4.5f, 1.4f, -0.80f, 0.50f, 0f, 0f),
        (330f, 3.7f, 1.4f, -0.80f, -0.50f, 0.00f, 4f), (272f, 5.0f, 1.4f, -4.40f, -0.50f, 0f, 4f), (205f, 4.2f, 0.6f, 0.00f, 0.55f, -0.35f, 0f),
        (158f, 4.2f, 1.0f,  0.35f, 0.55f, -1.25f, 0f), ( 95f, 4.7f, 0.8f, 1.80f, 0.50f, -0.35f, 0f), ( 38f, 4.2f, 0.8f, 1.95f, 0.05f, -1.95f, 0f),
        ( 21f, 4.4f, 1.5f,  1.95f, -0.35f, -1.95f, -1f), (326f, 3.9f, 1.2f, 0.80f, 0.50f, 1.75f, -12f), (209f, 5.7f, 0.9f, -0.35f, 0.35f, -0.55f, 2f),
    };
    var rng = new System.Random();
    var inv = System.Globalization.CultureInfo.InvariantCulture;
    System.Func<bool> stop = () => UnityEngine.Input.GetKey(UnityEngine.KeyCode.End);

    // ── Miejsca: 38 zapisanych przez użytkownika 2026-11-25 (photo_spots.cs, świat 2027) + nowe z PhotoSpots.txt (bez powtórek) ──
    var spots = new System.Collections.Generic.List<(UnityEngine.Vector3 pos, float rotY)>
    {
        (new UnityEngine.Vector3(-109.057f, 11.710f, -264.237f), 290.2f), (new UnityEngine.Vector3(-181.057f, 11.417f, -161.387f), 181.3f), (new UnityEngine.Vector3(-131.328f, 11.141f, -143.368f), 0.7f),
        (new UnityEngine.Vector3(-133.738f, 10.363f, -39.783f), 43.1f), (new UnityEngine.Vector3(-159.047f, 11.113f, 9.236f), 221.7f), (new UnityEngine.Vector3(-132.968f, 11.905f, 71.573f), 226.5f),
        (new UnityEngine.Vector3(-160.062f, 12.078f, 92.768f), 178.9f), (new UnityEngine.Vector3(-155.387f, 12.555f, 150.332f), 271.5f), (new UnityEngine.Vector3(3.584f, 0.065f, 91.844f), 177.1f),
        (new UnityEngine.Vector3(7.579f, 0.065f, 62.783f), 90.4f), (new UnityEngine.Vector3(-50.244f, 0.065f, -62.880f), 268.6f), (new UnityEngine.Vector3(-22.881f, 0.065f, -21.168f), 1.3f),
        (new UnityEngine.Vector3(-116.159f, 5.164f, 34.134f), 106.1f), (new UnityEngine.Vector3(-102.105f, 5.164f, 118.275f), 266.9f), (new UnityEngine.Vector3(-50.318f, 0.065f, 99.231f), 311.4f),
        (new UnityEngine.Vector3(-63.716f, 0.065f, 120.175f), 274.0f), (new UnityEngine.Vector3(-57.579f, 0.121f, 37.336f), 269.7f), (new UnityEngine.Vector3(-28.858f, 0.115f, 65.478f), 360.0f),
        (new UnityEngine.Vector3(-76.112f, 2.549f, -44.945f), 180.1f), (new UnityEngine.Vector3(31.658f, 0.109f, 65.956f), 359.1f), (new UnityEngine.Vector3(60.156f, 0.065f, 110.874f), 358.1f),
        (new UnityEngine.Vector3(69.238f, 0.065f, 85.677f), 90.7f), (new UnityEngine.Vector3(51.064f, 0.065f, -14.511f), 243.7f), (new UnityEngine.Vector3(57.041f, 0.115f, -32.901f), 181.0f),
        (new UnityEngine.Vector3(44.857f, 0.065f, -154.171f), 249.1f), (new UnityEngine.Vector3(43.905f, 0.065f, -187.067f), 178.3f), (new UnityEngine.Vector3(46.063f, 0.065f, -270.894f), 180.7f),
        (new UnityEngine.Vector3(115.854f, 0.065f, -272.486f), 90.4f), (new UnityEngine.Vector3(127.846f, 0.065f, -362.294f), 354.0f), (new UnityEngine.Vector3(89.189f, 0.065f, -3.873f), 268.0f),
        (new UnityEngine.Vector3(89.258f, 0.065f, -23.669f), 88.3f), (new UnityEngine.Vector3(97.220f, 0.065f, 88.959f), 359.6f), (new UnityEngine.Vector3(132.967f, 0.065f, 77.521f), 92.0f),
        (new UnityEngine.Vector3(128.510f, 0.115f, -119.719f), 181.7f), (new UnityEngine.Vector3(320.233f, 0.065f, -34.571f), 359.6f), (new UnityEngine.Vector3(218.004f, 0.065f, 41.240f), 359.8f),
        (new UnityEngine.Vector3(196.829f, 0.065f, 110.672f), 179.8f), (new UnityEngine.Vector3(155.977f, 0.065f, 116.775f), 268.8f),
    };
    if (System.IO.File.Exists(spotsFile))
        foreach (var line in System.IO.File.ReadAllLines(spotsFile))
        {
            var f = line.Trim().Split(';');
            if (f.Length < 4 || line.StartsWith("#")) continue;
            try
            {
                var p = new UnityEngine.Vector3(float.Parse(f[0], inv), float.Parse(f[1], inv), float.Parse(f[2], inv));
                if (!spots.Any(s => UnityEngine.Vector3.Distance(s.pos, p) < 0.5f)) spots.Add((p, float.Parse(f[3], inv)));
            }
            catch { }
        }
    if (spots.Count == 0) { Print($"[photo] no spots: record them with photo_spots.cs first"); yield break; }

    // ── Auto, którym jedziesz ──
    UnityEngine.Transform car = null;
    var scpm = UnityEngine.GameObject.Find("SceneCarPhysicsManager");
    if (scpm != null) car = scpm.transform.Find("Car (Owner 1)");
    if (car == null)
    {
        float bestD = 15f; var mc = UnityEngine.Camera.main;
        foreach (var rb0 in UnityEngine.Object.FindObjectsOfType<UnityEngine.Rigidbody>())
        {
            if (rb0 == null || rb0.isKinematic || rb0.mass < 500f) continue;
            float d = mc != null ? UnityEngine.Vector3.Distance(rb0.transform.position, mc.transform.position) : 0f;
            if (d < bestD) { bestD = d; car = rb0.transform; }
        }
    }
    if (car == null) { Print("[photo] no car found: drive your car into the world and run again"); yield break; }
    var rb = car.GetComponent<UnityEngine.Rigidbody>();

    // CarLoader tego auta: ten, którego karoseria jest najbliżej (na ulicach stoją też inne auta).
    CarLoader cl = null; float bestCl = 6f;
    foreach (var o in UnityEngine.Object.FindObjectsOfType(Il2CppType.Of<CarLoader>(), true))
    {
        var l = o.TryCast<CarLoader>();
        if (l == null || string.IsNullOrWhiteSpace(l.CarID)) continue;
        var parts = CMS2026_OXL.GameCarAccess.GetBodyParts(l, out _);
        if (parts.Count == 0) continue;
        float d = UnityEngine.Vector3.Distance(parts[0].transform.position, car.position);
        if (d < bestCl) { bestCl = d; cl = l; }
    }
    if (cl == null) { Print("[photo] CarLoader of your car not found"); yield break; }
    string carId = cl.CarID; int cfg = cl.configVersion;
    if (!CMS2026_OXL.MarketCatalog.EnsureBuilt() || !CMS2026_OXL.MarketCatalog.TryGet($"{carId}#{cfg}", out var entry))
    { Print($"[photo] {carId} cfg {cfg} is not a version OXL sells (not in this game build or data not ready), skipped"); yield break; }
    Print($"[photo] start: {entry.Name} ({carId} cfg {cfg}), spots={spots.Count}, test={TEST}");

    // ── Kamera i UI ──
    var cam = UnityEngine.GameObject.Find("MainCamera");
    if (cam == null) { Print("[photo] MainCamera not found"); yield break; }
    var camDrivers = new System.Collections.Generic.List<UnityEngine.Behaviour>();
    foreach (var bh in cam.GetComponents<UnityEngine.Behaviour>())
    {
        if (bh == null) continue;
        string tn = bh.GetIl2CppType()?.FullName ?? "";
        if (tn.Contains("CinemachineBrain") || tn.Contains("FixedCamera") || tn.Contains("CameraController")) camDrivers.Add(bh);
    }
    var hide = new[] { UnityEngine.GameObject.Find("!!UI") }.Where(g => g != null).ToList();
    // Nakładki typu licznik (nazwy z 2026 już nie istnieją): wszystkie aktywne Canvasy z "Dials" / "Speedometer" / "Gauge" w nazwie.
    foreach (var cv in UnityEngine.Object.FindObjectsOfType<UnityEngine.Canvas>()) { string n = cv.gameObject.name; if (n.Contains("Dials") || n.Contains("Speedometer") || n.Contains("Gauge")) hide.Add(cv.gameObject); }
    // Postać gracza w aucie (2026: CarPlayerModel; w 2027 inna nazwa): każdy model z kośćmi (SkinnedMeshRenderer) blisko auta, liczone przy każdym miejscu.
    var hiddenSkins = new System.Collections.Generic.List<UnityEngine.Renderer>();
    // Opony też mają kości (test 2026-11-25: rim_46B/tire, szerokosc, srodek zniknęły): część auta = pod felgą "rim_", oponą "tire" albo kołem. Gracz: "Man 3(Clone)/Character".
    System.Func<UnityEngine.Transform, bool> partOfCar = tr =>
    {
        for (var x = tr; x != null; x = x.parent) { string n = x.name.ToLowerInvariant(); if (n.StartsWith("rim_") || n.StartsWith("tire") || n.Contains("wheel")) return true; }
        return false;
    };
    System.Action hideSkins = () =>
    {
        foreach (var sk in UnityEngine.Object.FindObjectsOfType<UnityEngine.SkinnedMeshRenderer>())
            if (sk != null && sk.enabled && UnityEngine.Vector3.Distance(sk.bounds.center, car.position) < 6f && !partOfCar(sk.transform)) { sk.enabled = false; hiddenSkins.Add(sk); }
    };
    System.Action showSkins = () => { foreach (var r in hiddenSkins) if (r != null) r.enabled = true; hiddenSkins.Clear(); };
    Print($"[photo] camera drivers off during shots: {string.Join(", ", camDrivers.Select(b => b.GetIl2CppType().Name))}; hidden: {string.Join(", ", hide.Select(g => g.name))}");

    var colors = cl.AllowedColors;
    int colorCount = colors == null ? 0 : colors.Count;
    if (TEST) colorCount = System.Math.Min(2, colorCount);
    int saved = 0, skipped = 0;

    for (int ci = 0; ci < colorCount && !stop(); ci++)
    {
        var col = colors[ci].Color;
        System.Func<float, int> b255 = x => UnityEngine.Mathf.RoundToInt(UnityEngine.Mathf.Clamp01(x > 1f ? x / 255f : x) * 255f);
        string colorFolder = $"{ci:D2}_{b255(col.r):X2}{b255(col.g):X2}{b255(col.b):X2}";
        try { Il2CppCMS.Core.Car.CarLoaderExtension.SetRandomCarColor(cl, colors[ci], false); } catch (System.Exception ex) { Print($"[photo] color: {ex.Message}"); }
        yield return new UnityEngine.WaitForSeconds(0.5f);

        foreach (var cd in (TEST ? conds.Take(1) : conds))
        {
            if (stop()) break;
            string dir = System.IO.Path.Combine(root, carId, cfg.ToString(), colorFolder, cd.folder);
            if (System.IO.Directory.Exists(dir) && System.IO.Directory.GetFiles(dir, "*.jpg").Length > 0) { skipped++; continue; }
            System.IO.Directory.CreateDirectory(dir);

            // Stan widoczny na zdjęciu (zasada 5: zdjęcie = stan): blachy, wgniecenia, kurz, rdza poniżej 50%.
            float c = UnityEngine.Random.Range(cd.min, cd.max);
            try { cl.RepairAllBodyParts(); } catch { }
            yield return new UnityEngine.WaitForEndOfFrame();
            try
            {
                foreach (var p in CMS2026_OXL.GameCarAccess.GetBodyParts(cl, out _))
                {
                    if (p == null) continue;
                    string n = p.name.ToLowerInvariant();
                    bool glass = n.Contains("window") || n.Contains("glass") || n.Contains("light");
                    p.SetCondition(UnityEngine.Mathf.Clamp01(c + UnityEngine.Random.Range(-0.08f, 0.08f)));
                    // Kurz tylko na wrakach (030): właściciel auta w stanie 30% i wyżej myje je przed zdjęciami (użytkownik 2026-11-25).
                    if (!glass) { p.Dent = UnityEngine.Mathf.Clamp01((1f - c) * UnityEngine.Random.Range(0.3f, 0.9f)); p.Dust = cd.folder == "030" ? UnityEngine.Mathf.Clamp01((1f - c) * UnityEngine.Random.Range(0.2f, 0.8f)) : 0f; }
                    else p.Dust = cd.folder == "030" ? p.Dust : 0f;
                }
                cl.EnableRustOnAllParts(c < 0.5f);
            }
            catch (System.Exception ex) { Print($"[photo] wear: {ex.Message}"); }
            yield return new UnityEngine.WaitForSeconds(0.5f);

            var pick = Enumerable.Range(0, spots.Count).OrderBy(_ => rng.Next()).Take(spotsPerCond).ToList();
            foreach (int spi in pick)
            {
                if (stop()) break;
                // Teleport jak w 2026: rigidbody kinematyczne na czas przestawienia i zdjęć, żeby auto nie odjechało.
                foreach (var d in camDrivers) d.enabled = true;
                if (rb != null) { rb.isKinematic = true; rb.linearVelocity = UnityEngine.Vector3.zero; rb.angularVelocity = UnityEngine.Vector3.zero; }
                yield return new UnityEngine.WaitForFixedUpdate();
                car.position = spots[spi].pos + new UnityEngine.Vector3(0f, 0.05f, 0f);
                car.rotation = UnityEngine.Quaternion.Euler(0f, spots[spi].rotY, 0f);
                yield return new UnityEngine.WaitForSeconds(0.9f);   // świat i cienie dociągają się po teleporcie

                foreach (var d in camDrivers) d.enabled = false;
                foreach (var g in hide) g.SetActive(false);
                hideSkins();
                if (saved == 0) Print($"[photo] hidden near the car: {string.Join(", ", hiddenSkins.Select(r => r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name).Distinct().Take(12))}");
                yield return new UnityEngine.WaitForEndOfFrame();
                var shotIdx = Enumerable.Range(0, shots.Length).OrderBy(_ => rng.Next()).Take(shotsPerSpot).ToList();
                int nr = 0;
                foreach (int si in shotIdx)
                {
                    var s = shots[si];
                    var dirv = UnityEngine.Quaternion.Euler(0f, s.az, 0f) * car.forward;
                    var look = UnityEngine.Quaternion.Euler(0f, car.eulerAngles.y, 0f) * new UnityEngine.Vector3(s.lkX, s.lkY, s.lkZ);
                    cam.transform.position = car.position + dirv * s.dist + new UnityEngine.Vector3(0f, s.elev, 0f);
                    cam.transform.LookAt(car.position + look);
                    if (s.roll != 0f) cam.transform.Rotate(0f, 0f, s.roll, UnityEngine.Space.Self);
                    yield return new UnityEngine.WaitForSeconds(0.35f);
                    yield return new UnityEngine.WaitForEndOfFrame();

                    int sw = UnityEngine.Screen.width, sh = UnityEngine.Screen.height;
                    int cw = sw, ch = sw * 9 / 16; if (ch > sh) { ch = sh; cw = sh * 16 / 9; }
                    var full = new UnityEngine.Texture2D(cw, ch, UnityEngine.TextureFormat.RGB24, false);
                    full.ReadPixels(new UnityEngine.Rect((sw - cw) / 2, (sh - ch) / 2, cw, ch), 0, 0);
                    full.Apply();
                    var rt = UnityEngine.RenderTexture.GetTemporary(outW, outH, 0, UnityEngine.RenderTextureFormat.ARGB32);
                    UnityEngine.Graphics.Blit(full, rt);
                    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = rt;
                    var outTex = new UnityEngine.Texture2D(outW, outH, UnityEngine.TextureFormat.RGB24, false);
                    outTex.ReadPixels(new UnityEngine.Rect(0, 0, outW, outH), 0, 0); outTex.Apply();
                    UnityEngine.RenderTexture.active = prev; UnityEngine.RenderTexture.ReleaseTemporary(rt);
                    var jpg = UnityEngine.ImageConversion.EncodeToJPG(outTex, jpgQuality);
                    UnityEngine.Object.Destroy(full); UnityEngine.Object.Destroy(outTex);
                    byte[] bytes = new byte[jpg.Length]; for (int i = 0; i < jpg.Length; i++) bytes[i] = jpg[i];
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"spot{spi:D3}_{nr++:D2}.jpg"), bytes);
                    saved++;
                }
                foreach (var g in hide) g.SetActive(true);
                showSkins();
            }
            Print($"[photo] {carId}#{cfg} {colorFolder} {cd.folder} cond={c:F2} done, saved={saved}");
        }
    }
    foreach (var d in camDrivers) d.enabled = true;
    foreach (var g in hide) g.SetActive(true);
    showSkins();
    if (rb != null) rb.isKinematic = false;
    Print($"[photo] finished {carId}#{cfg}: saved={saved}, skipped conditions={skipped}, stopped={stop()}");
}
