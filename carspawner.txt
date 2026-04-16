// ═══════════════════════════════════════════════════════════════════════════
//  CarSpawner.cs + Full Debug Inspector
// ═══════════════════════════════════════════════════════════════════════════
CMS2026UITKFramework.FrameworkAPI.DestroyPanel("Car Spawner");
CMS2026UITKFramework.FrameworkAPI.DestroyPanel("Car Inspector");

// ── Colors ────────────────────────────────────────────────────────────────
var C_BG    = new UnityEngine.Color(0.06f, 0.07f, 0.10f, 0.97f);
var C_BORD  = new UnityEngine.Color(0.25f, 0.50f, 0.90f, 0.55f);
var C_HEAD  = new UnityEngine.Color(0.55f, 0.78f, 1.00f, 1.00f);
var C_DIM   = new UnityEngine.Color(0.45f, 0.45f, 0.58f, 1.00f);
var C_OK    = new UnityEngine.Color(0.22f, 0.82f, 0.32f, 1.00f);
var C_WARN  = new UnityEngine.Color(1.00f, 0.75f, 0.10f, 1.00f);
var C_BAD   = new UnityEngine.Color(1.00f, 0.25f, 0.20f, 1.00f);
var C_CLOSE = new UnityEngine.Color(0.42f, 0.08f, 0.08f, 1.00f);
var C_GREEN = new UnityEngine.Color(0.08f, 0.26f, 0.12f, 1.00f);
var C_BLUE  = new UnityEngine.Color(0.10f, 0.18f, 0.38f, 1.00f);
var C_INSP  = new UnityEngine.Color(0.38f, 0.84f, 0.48f, 1.00f);
var C_DATA  = new UnityEngine.Color(0.90f, 0.68f, 0.18f, 1.00f);
var C_PURP  = new UnityEngine.Color(0.25f, 0.14f, 0.30f, 1f);
var C_DUST  = new UnityEngine.Color(0.42f, 0.32f, 0.14f, 1f);
var C_RUST  = new UnityEngine.Color(0.38f, 0.20f, 0.06f, 1f);
var C_DBG   = new UnityEngine.Color(0.16f, 0.10f, 0.30f, 1f);

System.Func<float, UnityEngine.Color> CC = v =>
    v >= 0.70f ? C_OK : v >= 0.40f ? C_WARN : C_BAD;

// ── Car List ──────────────────────────────────────────────────────────────
var cars     = new[] { "car_dnbcensor","car_katagiritamagobp","car_luxorstreamlinermk3","car_mayenm5","car_salemariesmk3" };
var carNames = new[] { "DNB Censor","Katagiri Tamago BP","Luxor Streamliner Mk3","Mayen M5","Salem Aries MK3" };

System.Func<Il2CppCMS.Core.Car.CarLoader[]> GetLoaders = () =>
    UnityEngine.Object.FindObjectsOfType<Il2CppCMS.Core.Car.CarLoader>(true);

// ── Classifiers ──────────────────────────────────────────────────────────
System.Func<string, string> Classify = id => {
    if (id.Contains("belt")||id.Contains("chain")||id.Contains("timing")) return "Belt/Chain";
    if (id.Contains("filter")||id.Contains("spark")||id.Contains("oil")) return "Filter/Oil";
    if (id.Contains("brake")||id.Contains("pad")||id.Contains("caliper")||id.Contains("tarcza")) return "Brakes";
    if (id.Contains("shock")||id.Contains("strut")||id.Contains("spring")||id.Contains("wahacz")||id.Contains("bushing")||id.Contains("control_arm")) return "Suspension";
    if (id.Contains("exhaust")||id.Contains("muffler")||id.Contains("catalyst")||id.Contains("rura")) return "Exhaust";
    if (id.Contains("alternator")||id.Contains("battery")||id.Contains("starter")||id.Contains("coil")||id.Contains("sensor")||id.Contains("lambda")) return "Electrical";
    return "Mechanical";
};
System.Func<string, string> ClassifyBody = n => {
    if (n.Contains("window")||n.Contains("mirror")||n.Contains("light")||n.Contains("lamp")||n.Contains("glass")) return "Glass/Light";
    return "Body Panel";
};

// ════════════════════════════════════════════════════════════════════════
//  MAIN PANEL
// ════════════════════════════════════════════════════════════════════════
const int PW = 620, PH = 600, MAX_SLOTS = 10;

var p = CMS2026UITKFramework.UIPanel.Create("Car Spawner", 25, 18, PW, PH);
p.AddTitleButton("✕", () => CMS2026UITKFramework.FrameworkAPI.DestroyPanel("Car Spawner"), C_CLOSE);
p.Build(9999);
p.SetScrollbarVisible(false);
p.SetDragWhenScrollable(true);
{ var ve = CMS2026UITKFramework.UIRuntime.WrapVE(p.GetPanelRawPtr());
  var st = CMS2026UITKFramework.UIRuntime.GetStyle(ve);
  CMS2026UITKFramework.S.BgColor(st, C_BG);
  CMS2026UITKFramework.S.BorderRadius(st, 14f);
  CMS2026UITKFramework.S.BorderColor(st, C_BORD);
  CMS2026UITKFramework.S.BorderWidth(st, 1.5f); }

// ── Status bar ────────────────────────────────────────────────────────────
var rTop = p.AddRow(24f, 4f);
var lblTicker = rTop.AddLabel("↻", 26f, C_DIM);
var lblStat   = rTop.AddLabel("Ready", 480f, C_OK);
lblStat.SetFontSize(11);
p.AddSeparator();
System.Action<string, UnityEngine.Color> SetStatus = (msg, col) => {
    lblStat.SetText(msg); lblStat.SetColor(col); };

// ── SPAWN — na górze ──────────────────────────────────────────────────────
p.AddSpace(4f);
{ var l = p.AddLabel("  SPAWN"); l.SetFontSize(13); l.SetColor(C_HEAD); }
p.AddSpace(3f);

int _selIdx = 0, _inspSlot = 0;

System.Action<Il2CppCMS.Core.Car.CarLoader[]> RefreshSlots = null;
System.Action<int> OpenInspector = null;
System.Func<Il2CppCMS.Core.Car.CarLoader, System.Collections.IEnumerator> FnUnload = null;
System.Func<string, int, System.Collections.IEnumerator>                  FnSpawn  = null;

p.AddDropdown("Car to spawn:", carNames, 0, idx => _selIdx = idx);
var rSpBtns = p.AddRow(28f, 6f);
rSpBtns.AddButton("Spawn into first free slot", 220f, () => {
    SetStatus("Spawning "+carNames[_selIdx]+"...", C_WARN);
    var lds = GetLoaders(); int freeSlot = -1;
    for (int i=0; i<lds.Length; i++) if (string.IsNullOrWhiteSpace(lds[i].CarID)) { freeSlot=i; break; }
    if (freeSlot < 0) { SetStatus("No free slots!", C_BAD); return; }
    MelonLoader.MelonCoroutines.Start(FnSpawn(cars[_selIdx], freeSlot));
}, C_GREEN);
rSpBtns.AddButton("Refresh", 110f, () => {
    RefreshSlots(GetLoaders()); SetStatus("Refreshed", C_OK); }, C_BLUE);

p.AddSeparator(); p.AddSpace(4f);

// ── PARKING SLOTS — pod spawnem ───────────────────────────────────────────
{ var l = p.AddLabel("  PARKING SLOTS"); l.SetFontSize(13); l.SetColor(C_HEAD); }
p.AddSpace(3f);
var rCH = p.AddRow(19f, 4f);
rCH.AddLabel("#", 28f, C_DIM);      rCH.AddLabel("Loader", 120f, C_DIM);
rCH.AddLabel("Car ID", 170f, C_DIM); rCH.AddLabel("State", 66f, C_DIM);
rCH.AddLabel("Avg Cond", 65f, C_DIM); rCH.AddLabel("Actions", 120f, C_DIM);
p.AddSeparator();

var sCarLbl  = new CMS2026UITKFramework.UILabelHandle[MAX_SLOTS];
var sStatLbl = new CMS2026UITKFramework.UILabelHandle[MAX_SLOTS];
var sCondLbl = new CMS2026UITKFramework.UILabelHandle[MAX_SLOTS];
var sRemBtn  = new CMS2026UITKFramework.UIButtonHandle[MAX_SLOTS];
var sInspBtn = new CMS2026UITKFramework.UIButtonHandle[MAX_SLOTS];

var initLds = GetLoaders();
int slotCnt = System.Math.Min(initLds.Length, MAX_SLOTS);

for (int i = 0; i < slotCnt; i++) {
    int ci = i;
    var cl = initLds[i];
    bool occ = !string.IsNullOrWhiteSpace(cl.CarID);
    string sn = cl.name.Length > 16 ? cl.name.Substring(0,16) : cl.name;
    float avgC = 0f;
    if (occ) { try { var ip = cl.indexedParts; if (ip!=null) {
        float s=0f; int n=0;
        for (int j=0;j<ip.Count;j++) if (ip[j]!=null) { s+=ip[j].condition; n++; }
        if (n>0) avgC=s/n; } } catch {} }

    var row = p.AddRow(25f, 4f);
    row.AddLabel((ci+1).ToString(), 28f, C_DIM);
    row.AddLabel(sn, 120f, C_DIM);
    sCarLbl[ci]  = row.AddLabel(occ ? cl.CarID : "— empty —", 170f, occ ? C_OK : C_DIM);
    sStatLbl[ci] = row.AddLabel(occ ? "occupied" : "free", 66f, occ ? C_WARN : C_DIM);
    sCondLbl[ci] = row.AddLabel(occ ? (avgC*100f).ToString("F0")+"%" : "—", 65f, occ ? CC(avgC) : C_DIM);
    sRemBtn[ci] = row.AddButton("Remove", 56f, () => {
        var lds = GetLoaders();
        if (ci>=lds.Length || string.IsNullOrWhiteSpace(lds[ci].CarID)) {
            SetStatus("Slot "+(ci+1)+" already empty", C_DIM); return; }
        MelonLoader.MelonCoroutines.Start(FnUnload(lds[ci]));
    }, C_CLOSE); sRemBtn[ci].SetVisible(occ);
    sInspBtn[ci] = row.AddButton("Inspect", 56f, () => {
        var lds = GetLoaders();
        if (ci>=lds.Length || string.IsNullOrWhiteSpace(lds[ci].CarID)) {
            SetStatus("Slot "+(ci+1)+" empty", C_WARN); return; }
        _inspSlot = ci; OpenInspector(ci);
        SetStatus("Inspecting slot "+(ci+1), C_INSP);
    }, C_BLUE); sInspBtn[ci].SetVisible(occ);
}

// ── Update callback ───────────────────────────────────────────────────────
float _rTimer = 0f; string[] tickF = { "↻","↺" }; int _tFrame = 0; float _tTimer = 0f;
p.SetUpdateCallback(dt => {
    _tTimer += dt;
    if (_tTimer >= 0.5f) { _tTimer=0f; _tFrame=1-_tFrame; lblTicker.SetText(tickF[_tFrame]); }
    _rTimer += dt; if (_rTimer < 1.0f) return; _rTimer=0f; RefreshSlots(GetLoaders());
});

// ════════════════════════════════════════════════════════════════════════
//  HELPERS — CarDebug via reflection
// ════════════════════════════════════════════════════════════════════════
System.Func<Il2Cpp.CarDebug> GetDbg = () => {
    var lds = GetLoaders();
    if (_inspSlot >= lds.Length || string.IsNullOrWhiteSpace(lds[_inspSlot].CarID)) return null;
    return lds[_inspSlot].gameObject.GetComponent<Il2Cpp.CarDebug>();
};
System.Func<Il2CppCMS.Core.Car.CarLoader> GetCar = () => {
    var lds = GetLoaders();
    return _inspSlot < lds.Length ? lds[_inspSlot] : null;
};

System.Action<string> DbgVoid = name => {
    var d = GetDbg(); if (d==null) { SetStatus("No CarDebug", C_WARN); return; }
    try {
        var mi = d.GetType().GetMethod(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        if (mi==null) { SetStatus(name+" not found", C_WARN); return; }
        mi.Invoke(d, null); SetStatus(name+" OK", C_OK);
    } catch (System.Exception ex) { SetStatus(name+": "+ex.Message, C_BAD); }
};

System.Action<string, float> DbgFloat = (name, v) => {
    var d = GetDbg(); if (d==null) return;
    try {
        var mi = d.GetType().GetMethod(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        mi?.Invoke(d, new object[]{ v });
    } catch (System.Exception ex) { SetStatus(name+": "+ex.Message, C_BAD); }
};

System.Action<string, bool> DbgBool = (name, v) => {
    var d = GetDbg(); if (d==null) return;
    try {
        var mi = d.GetType().GetMethod(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        mi?.Invoke(d, new object[]{ v });
    } catch {}
};

// ════════════════════════════════════════════════════════════════════════
//  INSPECTOR — rebuilt on every open
// ════════════════════════════════════════════════════════════════════════
const int IW = 800, IH = 900;

OpenInspector = slotIdx => {
    _inspSlot = slotIdx;
    var lds = GetLoaders();
    if (slotIdx >= lds.Length || string.IsNullOrWhiteSpace(lds[slotIdx].CarID)) {
        SetStatus("No car in slot "+(slotIdx+1), C_WARN); return; }

    var cl  = lds[slotIdx];
    var dbg = cl.gameObject.GetComponent<Il2Cpp.CarDebug>();

    CMS2026UITKFramework.FrameworkAPI.DestroyPanel("Car Inspector");
    var ip = CMS2026UITKFramework.UIPanel.Create("Car Inspector", 670, 18, IW, IH);
    ip.AddTitleButton("✕", () => CMS2026UITKFramework.FrameworkAPI.DestroyPanel("Car Inspector"), C_CLOSE);
    ip.Build(9998);

    // ── FIX: odblokuj kursor po zbudowaniu panelu ──
    try {
        UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;
    } catch {}



    ip.SetScrollbarVisible(true);
    ip.SetDragWhenScrollable(true);
    { var ve = CMS2026UITKFramework.UIRuntime.WrapVE(ip.GetPanelRawPtr());
      var st = CMS2026UITKFramework.UIRuntime.GetStyle(ve);
      CMS2026UITKFramework.S.BgColor(st, C_BG);
      CMS2026UITKFramework.S.BorderRadius(st, 14f);
      CMS2026UITKFramework.S.BorderColor(st, C_BORD);
      CMS2026UITKFramework.S.BorderWidth(st, 1.5f); }

    // ── Header ──────────────────────────────────────────────────────
    ip.AddSpace(4f);
    { var l = ip.AddLabel("  Slot "+(slotIdx+1)+"   "+cl.CarID+"   model loaded: "+cl.modelLoaded);
      l.SetFontSize(13); l.SetColor(new UnityEngine.Color(0.80f,0.86f,0.95f,1f)); }

    // Calculate averages
    float mechAvg=0f, bodyAvg=0f; int mechN=0, bodyN=0;
    try { var ipl=cl.indexedParts; if(ipl!=null) for(int j=0;j<ipl.Count;j++) if(ipl[j]!=null){mechAvg+=ipl[j].condition;mechN++;} } catch {}
    try { var cpl=cl.carParts;     if(cpl!=null) for(int j=0;j<cpl.Count;j++) if(cpl[j]!=null){bodyAvg+=cpl[j].Condition;bodyN++;} } catch {}
    if (mechN>0) mechAvg/=mechN; if (bodyN>0) bodyAvg/=bodyN;

    { var l = ip.AddLabel($"  Mech avg: {mechAvg*100f:F0}%   Body avg: {bodyAvg*100f:F0}%   Parts: {mechN} mech / {bodyN} body");
      l.SetFontSize(11); l.SetColor(C_DIM); }
    ip.AddSeparator();

    // ── SECTION: Quick Actions ─────────────────────────────────────────
    { var l = ip.AddLabel("  QUICK ACTIONS"); l.SetFontSize(12); l.SetColor(C_HEAD); }
    ip.AddSpace(3f);

    // Row 1 — Repair / Examine / Tune / Fluids
    var rQ1 = ip.AddRow(26f, 4f);
    rQ1.AddButton("Repair All", 115f, () => { DbgVoid("RepairAll"); OpenInspector(_inspSlot); }, C_GREEN);
    rQ1.AddButton("Examine All", 115f, () => { DbgVoid("ExamineAll"); }, C_BLUE);
    rQ1.AddButton("Tune Parts", 115f, () => { DbgVoid("TuneAllParts"); OpenInspector(_inspSlot); }, C_BLUE);
    rQ1.AddButton("Refill Fluids", 115f, () => { DbgVoid("RefillFluids"); SetStatus("Fluids refilled", C_OK); }, C_BLUE);
    rQ1.AddButton("Max Quality", 115f, () => { DbgVoid("SetMaxQuality"); OpenInspector(_inspSlot); }, C_BLUE);

    // Row 2 — Randomization
    var rQ2 = ip.AddRow(26f, 4f);
    rQ2.AddButton("Rnd Parts Cond", 130f, () => { DbgVoid("SetRandomPartsCondition"); OpenInspector(_inspSlot); }, C_CLOSE);
    rQ2.AddButton("Rnd Wheel Align", 130f, () => { DbgVoid("SetRandomWheelAlignment"); SetStatus("Wheel align randomized", C_WARN); }, C_CLOSE);
    rQ2.AddButton("Rnd Headlamp", 120f, () => { DbgVoid("SetRandomHeadlampAlignment"); SetStatus("Headlamps rnd", C_WARN); }, C_CLOSE);
    rQ2.AddButton("Rnd Dirtness", 120f, () => { DbgVoid("SetRandomDirtness"); SetStatus("Dirtness rnd", C_WARN); }, C_CLOSE);
    rQ2.AddButton("Rnd Color", 100f, () => { DbgVoid("SetRandomAllowedColor"); SetStatus("Color rnd", C_OK); }, C_BLUE);

    // Row 3 — Toggle / OEM / Unmount
    var rQ3 = ip.AddRow(26f, 4f);
    rQ3.AddButton("Toggle Dust", 108f, () => { DbgVoid("ToggleDust"); }, C_DUST);
    rQ3.AddButton("Toggle Dent", 108f, () => { DbgVoid("ToggleDent"); }, C_DUST);
    rQ3.AddButton("Toggle Rust", 108f, () => { DbgVoid("ToggleRust"); }, C_RUST);
    rQ3.AddButton("Toggle Wash", 108f, () => { DbgVoid("ToggleWashFactor"); }, C_DUST);
    rQ3.AddButton("Drain Fluids", 108f, () => { DbgBool("DrainFluids",false); SetStatus("Fluids drained", C_WARN); }, C_CLOSE);
    rQ3.AddButton("Drain+Fuel", 108f, () => { DbgBool("DrainFluids",true); SetStatus("All fluids drained", C_WARN); }, C_CLOSE);

    // Row 4 — OEM / Livery / Unmount
    var rQ4 = ip.AddRow(26f, 4f);
    rQ4.AddButton("OEM Parts", 108f, () => { DbgVoid("SetAllPartsOEM"); SetStatus("All OEM", C_OK); }, C_BLUE);
    rQ4.AddButton("Non-OEM", 108f, () => { DbgVoid("SetAllPartsNotOEM"); SetStatus("All non-OEM", C_WARN); }, C_BLUE);
    rQ4.AddButton("Factory Color", 108f, () => { DbgVoid("SetFactoryColor"); SetStatus("Factory color set", C_OK); }, C_BLUE);
    rQ4.AddButton("Next Livery", 108f, () => { DbgVoid("SetNextLivery"); SetStatus("Livery changed", C_OK); }, C_DBG);
    rQ4.AddButton("Default Livery", 108f, () => { DbgVoid("SetDefaultLivery"); SetStatus("Default livery", C_OK); }, C_DBG);
    rQ4.AddButton("Unmount All", 108f, () => { DbgVoid("UnmountAllParts"); OpenInspector(_inspSlot); }, C_CLOSE);

    // Row 5 — Auction / Salvage / Spawn
    var rQ5 = ip.AddRow(26f, 4f);
    rQ5.AddButton("Auction Car", 130f, () => { DbgVoid("AuctionCar"); SetStatus("Auctioned", C_WARN); }, C_PURP);
    rQ5.AddButton("Salvage Car", 130f, () => { DbgVoid("SalvageCar"); SetStatus("Salvaged", C_BAD); }, C_PURP);
    rQ5.AddButton("Spawn Random", 130f, () => { DbgVoid("SpawnRandomCar"); SetStatus("Random car spawned", C_OK); }, C_GREEN);
    rQ5.AddButton("Refresh Inspector", 175f, () => { OpenInspector(_inspSlot); }, C_BLUE);

    ip.AddSeparator();

    // ── SECTION: Live Sliders — Environment ──────────────────────────────
    { var l = ip.AddLabel("  LIVE SLIDERS  (immediate effect on drag)");
      l.SetFontSize(12); l.SetColor(C_HEAD); }
    ip.AddSpace(3f);

    ip.AddSlider("Fuel (0-1):", 0f, 1f, 0.5f, v => {
        DbgFloat("SetFuelLevel", v); }, step: 0.01f);

    ip.AddSlider("Dust (0-1):", 0f, 1f, 0f, v => {
        DbgFloat("SetDustLevel", v); }, step: 0.01f);

    ip.AddSlider("Dirtness (0-1):", 0f, 1f, 0f, v => {
        DbgFloat("SetDirtness", v); }, step: 0.01f);

    ip.AddSlider("Frame Condition (0-1):", 0f, 1f, 1f, v => {
        try { GetDbg()?.SetFrameCondition(v); } catch {} }, step: 0.01f);

    ip.AddSlider("Details Condition (0-1):", 0f, 1f, 1f, v => {
        try { GetDbg()?.SetDetailsCondtition(v); } catch {} }, step: 0.01f);

    ip.AddSlider("All MECH parts (global):", 0.02f, 1f, mechAvg, v => {
        var c2 = GetCar(); if (c2==null) return;
        try { var ipl=c2.indexedParts; if(ipl!=null)
            for(int j=0;j<ipl.Count;j++) ipl[j]?.SetCondition(v,false);
            c2.ClearEnginePartsConditionCache(); } catch {}
    }, step: 0.01f);

    ip.AddSlider("All BODY parts (global):", 0.02f, 1f, bodyAvg, v => {
        var c2 = GetCar(); if (c2==null) return;
        try { var cpl=c2.carParts; if(cpl!=null)
            for(int j=0;j<cpl.Count;j++) if(cpl[j]!=null) c2.SetCondition(cpl[j],v);
            c2.SetConditionOnBody(v); c2.SetConditionOnDetails(v); } catch {}
    }, step: 0.01f);

    ip.AddSeparator();

   // ── SECTION: Car Data ──────────────────────────────────────────────
{ var l = ip.AddLabel("  CAR DATA"); l.SetFontSize(12); l.SetColor(C_DATA); }
ip.AddSpace(3f);

var cid = cl.CarInfoData;
string mileInit = cid != null ? cid.Mileage.ToString() : "0";
string yearInit  = cid != null ? cid.Year.ToString() : "1990";

{ var l = ip.AddLabel("  Car ID: " + cl.CarID + "   Year: " + yearInit + "   Mileage: " + mileInit + " km");
  l.SetFontSize(11); l.SetColor(C_DIM); }
ip.AddSpace(3f);

// Mileage
{ var l = ip.AddLabel("  Mileage (km):"); l.SetFontSize(11); l.SetColor(C_DIM); }
var mileInput = ip.AddTextInput(mileInit, v => {});
try { mileInput.SetValue(mileInit); } catch {}
{ var r = ip.AddRow(26f, 5f);
  r.AddButton("Apply mileage", 165f, () => {
    string raw = (mileInput.GetValue() ?? "").Trim();
    if (string.IsNullOrEmpty(raw)) { SetStatus("Type a value first", C_WARN); return; }
    uint km;
    if (!uint.TryParse(raw, out km)) { SetStatus("Bad mileage: " + raw, C_BAD); return; }
    var c2 = GetCar(); if (c2 == null) { SetStatus("No car", C_BAD); return; }
    try {
        var cid2 = c2.CarInfoData;
        if (cid2 == null) { SetStatus("CarInfoData null", C_BAD); return; }
        cid2.Mileage = km;
        SetStatus("Mileage → " + km + " km", C_OK);
    } catch (System.Exception ex) { SetStatus("Mileage err: " + ex.Message, C_BAD); }
  }, C_BLUE); }

// Year
ip.AddSpace(3f);
{ var l = ip.AddLabel("  Production year:"); l.SetFontSize(11); l.SetColor(C_DIM); }
var yearInput = ip.AddTextInput(yearInit, v => {});
try { yearInput.SetValue(yearInit); } catch {}
{ var r = ip.AddRow(26f, 5f);
  r.AddButton("Apply year", 165f, () => {
    string raw = (yearInput.GetValue() ?? "").Trim();
    if (string.IsNullOrEmpty(raw)) { SetStatus("Type a value first", C_WARN); return; }
    ushort yr;
    if (!ushort.TryParse(raw, out yr) || yr < 1950 || yr > 2030) {
        SetStatus("Bad year: " + raw + " (1950-2030)", C_BAD); return; }
    var c2 = GetCar(); if (c2 == null) { SetStatus("No car", C_BAD); return; }
    try {
        var cid2 = c2.CarInfoData;
        if (cid2 == null) { SetStatus("CarInfoData null", C_BAD); return; }
        cid2.Year = yr;
        SetStatus("Year → " + yr, C_OK);
    } catch (System.Exception ex) { SetStatus("Year err: " + ex.Message, C_BAD); }
  }, C_BLUE); }

// License plates
ip.AddSpace(3f);
{ var l = ip.AddLabel("  License plate:"); l.SetFontSize(11); l.SetColor(C_DIM); }
var plateInput = ip.AddTextInput("e.g. AB12345", v => {});
{ var r = ip.AddRow(26f, 5f);
  r.AddButton("Set plates", 165f, () => {
    string plate = (plateInput.GetValue() ?? "").Trim();
    if (string.IsNullOrEmpty(plate)) { SetStatus("Type a plate number first", C_WARN); return; }
    var c2 = GetCar(); if (c2 == null) { SetStatus("No car", C_BAD); return; }
    try {
        c2.SetNewLicensePlateNumber(plate, true);
        c2.SetNewLicensePlateNumber(plate, false);
        SetStatus("Plates → " + plate, C_OK);
    } catch (System.Exception ex) { SetStatus("Plates err: " + ex.Message, C_BAD); }
  }, C_BLUE);
  r.AddButton("Remove plates", 140f, () => {
    var c2 = GetCar(); if (c2 == null) return;
    try {
        c2.SetNewLicensePlateNumber("-----", true);
        c2.SetNewLicensePlateNumber("-----", false);
        SetStatus("Plates cleared", C_OK);
    } catch {}
  }, C_CLOSE); }

   
// ── Color swatches ────────────────────────────────────────────────
ip.AddSpace(3f);
{ var l = ip.AddLabel("  Color  (click to apply):"); l.SetFontSize(11); l.SetColor(C_DIM); }
ip.AddSpace(3f);
var lblColStatus = ip.AddLabel("— select a color swatch —");
lblColStatus.SetFontSize(11); lblColStatus.SetColor(C_DIM);
ip.AddSpace(4f);

try {
    var allColors = cl.AllowedColors;
    if (allColors == null || allColors.Count == 0) {
        var lb = ip.AddLabel("  No colors available");
        lb.SetColor(C_DIM); lb.SetFontSize(11);
    } else {
        int total = allColors.Count;
        const float CBW = 36f;
        const float CBH = 26f;
        const int perRow = 15;
        int rowCount = (total + perRow - 1) / perRow;

        for (int ri = 0; ri < rowCount; ri++) {
            var cbRow = ip.AddRow(CBH + 4f, 3f);
            for (int ci3 = 0; ci3 < perRow; ci3++) {
                int globalIdx = ri * perRow + ci3;
                if (globalIdx >= total) break;

                // Bezpośredni dostęp do Color — z auto-normalizacją 0-255 → 0-1
float cr = 0.5f, cg = 0.5f, cb2 = 0.5f;
string hexStr = "#888888";
try {
    var rawCol = allColors[globalIdx].Color;
    cr = rawCol.r; cg = rawCol.g; cb2 = rawCol.b;

    // Jeśli którakolwiek wartość > 1 — jesteśmy w zakresie 0-255, normalizuj
    if (cr > 1f || cg > 1f || cb2 > 1f) {
        cr /= 255f; cg /= 255f; cb2 /= 255f;
    }
    cr  = UnityEngine.Mathf.Clamp01(cr);
    cg  = UnityEngine.Mathf.Clamp01(cg);
    cb2 = UnityEngine.Mathf.Clamp01(cb2);

    hexStr = string.Format("#{0:X2}{1:X2}{2:X2}",
        UnityEngine.Mathf.RoundToInt(cr  * 255f),
        UnityEngine.Mathf.RoundToInt(cg  * 255f),
        UnityEngine.Mathf.RoundToInt(cb2 * 255f));
} catch {}
                int   capIdx  = globalIdx;
                var   capBtnC = new UnityEngine.Color(cr, cg, cb2, 1f);
                string capHex = hexStr;

                cbRow.AddButton("", CBW, () => {
                    var c2 = GetCar(); if (c2 == null) return;
                    try {
                        var ac2 = c2.AllowedColors;
                        if (ac2 == null || capIdx >= ac2.Count) {
                            SetStatus("Color index out of range", C_WARN); return; }
                        Il2CppCMS.Core.Car.CarLoaderExtension.SetRandomCarColor(
                            c2, ac2[capIdx], false);
                        lblColStatus.SetText("Color #" + capIdx + "  " + capHex);
                        lblColStatus.SetColor(capBtnC);
                        SetStatus("Color #" + capIdx + " applied", C_OK);
                    } catch (System.Exception ex) {
                        SetStatus("Color err: " + ex.Message, C_BAD); }
                }, capBtnC);
            }
        }
    }
} catch (System.Exception ex) {
    var lb2 = ip.AddLabel("  Color error: " + ex.Message);
    lb2.SetColor(C_BAD); lb2.SetFontSize(11);
}



   

   // ── SECTION: Tires ─────────────────────────────────────────────────

ip.AddSeparator(); ip.AddSpace(4f);
{ var l = ip.AddLabel("  TIRES"); l.SetFontSize(12); l.SetColor(C_DATA); }
ip.AddSpace(3f);

// Dane do dropdownów
var tireTypes    = new[] { "standard", "sport", "allseason", "offroad", "winter", "mud" };
var tireWidths   = new[] { "165","175","185","195","205","215","225","235","245","255","265","275" };
var tireProfiles = new[] { "35","40","45","50","55","60","65","70","75" };
var tireDiams    = new[] { "13","14","15","16","17","18","19","20","21","22" };

int _tireTypeIdx = 0, _tireWidthIdx = 4, _tireProfileIdx = 2, _tireDiamIdx = 4;

// Podgląd ID
var lblTirePreview = ip.AddLabel("  → tire_standard_205_45_17");
lblTirePreview.SetFontSize(11); lblTirePreview.SetColor(C_OK);

System.Action UpdateTirePreview = () => {
    string id = "tire_" + tireTypes[_tireTypeIdx] + "_" +
                tireWidths[_tireWidthIdx] + "_" +
                tireProfiles[_tireProfileIdx] + "_" +
                tireDiams[_tireDiamIdx];
    lblTirePreview.SetText("  → " + id);
};

ip.AddDropdown("Type:", tireTypes, 0, idx => { _tireTypeIdx = idx; UpdateTirePreview(); });
ip.AddDropdown("Width (mm):", tireWidths, 4, idx => { _tireWidthIdx = idx; UpdateTirePreview(); });
ip.AddDropdown("Profile (%):", tireProfiles, 2, idx => { _tireProfileIdx = idx; UpdateTirePreview(); });
ip.AddDropdown("Diameter (\"):", tireDiams, 4, idx => { _tireDiamIdx = idx; UpdateTirePreview(); });

ip.AddSpace(4f);
{ var r = ip.AddRow(26f, 4f);
  System.Func<string> GetTireId = () =>
    "tire_" + tireTypes[_tireTypeIdx] + "_" +
    tireWidths[_tireWidthIdx] + "_" +
    tireProfiles[_tireProfileIdx] + "_" +
    tireDiams[_tireDiamIdx];

  r.AddButton("All 4", 90f, () => {
    string tid = GetTireId();
    if (dbg != null) {
        try { dbg.ChangeAllTires(tid); SetStatus("All tires → " + tid, C_OK); }
        catch (System.Exception ex) { SetStatus("Tire err: " + ex.Message, C_BAD); }
    }
  }, C_GREEN);
  r.AddButton("FL", 60f, () => {
    string tid = GetTireId();
    if (dbg != null) { try { dbg.ChangeFrontLeftTire(tid);  SetStatus("FL → "+tid, C_OK); } catch {} }
  }, C_BLUE);
  r.AddButton("FR", 60f, () => {
    string tid = GetTireId();
    if (dbg != null) { try { dbg.ChangeFrontRightTire(tid); SetStatus("FR → "+tid, C_OK); } catch {} }
  }, C_BLUE);
  r.AddButton("RL", 60f, () => {
    string tid = GetTireId();
    if (dbg != null) { try { dbg.ChangeRearLeftTire(tid);   SetStatus("RL → "+tid, C_OK); } catch {} }
  }, C_BLUE);
  r.AddButton("RR", 60f, () => {
    string tid = GetTireId();
    if (dbg != null) { try { dbg.ChangeRearRightTire(tid);  SetStatus("RR → "+tid, C_OK); } catch {} }
  }, C_BLUE);
  r.AddButton("Front", 75f, () => {
    string tid = GetTireId();
    if (dbg != null) {
        try {
            dbg.ChangeFrontLeftTire(tid);
            dbg.ChangeFrontRightTire(tid);
            SetStatus("Front → "+tid, C_OK);
        } catch {}
    }
  }, C_BLUE);
  r.AddButton("Rear", 75f, () => {
    string tid = GetTireId();
    if (dbg != null) {
        try {
            dbg.ChangeRearLeftTire(tid);
            dbg.ChangeRearRightTire(tid);
            SetStatus("Rear → "+tid, C_OK);
        } catch {}
    }
  }, C_BLUE); }

// Ręczne wpisanie ID jako fallback
ip.AddSpace(4f);
{ var l = ip.AddLabel("  Manual ID (fallback — wklej ID z gry):"); l.SetFontSize(10); l.SetColor(C_DIM); }
var tireManualInput = ip.AddTextInput("tire_sport_225_45_17", v => {});
{ var r2 = ip.AddRow(26f, 4f);
  r2.AddButton("Apply manual ID — All", 200f, () => {
    string tid = (tireManualInput.GetValue() ?? "").Trim();
    if (string.IsNullOrEmpty(tid)) { SetStatus("Enter tire ID first", C_WARN); return; }
    if (dbg != null) {
        try { dbg.ChangeAllTires(tid); SetStatus("All tires → " + tid, C_OK); }
        catch (System.Exception ex) { SetStatus("Tire err: " + ex.Message, C_BAD); }
    }
  }, C_BLUE); }

    // ── SECTION: Body parts — slider per part ────────────────────
    { var l = ip.AddLabel("  BODY PARTS  —  slider = live set condition");
      l.SetFontSize(12); l.SetColor(C_INSP); }
    ip.AddSpace(2f);

    var bpList = new System.Collections.Generic.List<(int idx, string name, float cond)>();
    try { var cpl=cl.carParts; if(cpl!=null)
        for(int j=0;j<cpl.Count;j++)
            if(cpl[j]!=null && !string.IsNullOrWhiteSpace(cpl[j].name))
                bpList.Add((j, cpl[j].name, cpl[j].Condition)); } catch {}
    bpList.Sort((a,b) => a.cond.CompareTo(b.cond));

    foreach (var entry in bpList) {
        int   capIdx  = entry.idx;
        float curCond = UnityEngine.Mathf.Clamp01(entry.cond);
        string tnm    = entry.name.Length > 40 ? entry.name.Substring(0,40) : entry.name;
        string cat    = ClassifyBody(entry.name.ToLower());

        var rp  = ip.AddRow(18f, 1f);
        var lNm = rp.AddLabel(tnm, 320f, CC(curCond)); lNm.SetFontSize(10);
        var lCt = rp.AddLabel(cat, 90f, C_DIM);        lCt.SetFontSize(9);
        var lPc = rp.AddLabel((curCond*100f).ToString("F0")+"%", 55f, CC(curCond)); lPc.SetFontSize(10);

        ip.AddSlider("", 0.02f, 1f, curCond, v => {
            lPc.SetText((v*100f).ToString("F0")+"%");
            lPc.SetColor(CC(v));
            lNm.SetColor(CC(v));
            var c2=GetCar(); if(c2==null) return;
            try { var cpl2=c2.carParts;
                if(cpl2!=null && capIdx<cpl2.Count && cpl2[capIdx]!=null)
                    c2.SetCondition(cpl2[capIdx],v); } catch {}
        }, step: 0.01f);
        ip.AddSpace(1f);
    }

    if (bpList.Count == 0) {
        var l = ip.AddLabel("  No body parts"); l.SetColor(C_DIM); l.SetFontSize(11); }

    ip.AddSpace(120f);
};

// ── RefreshSlots ──────────────────────────────────────────────────────────
RefreshSlots = loaders => {
    for (int i = 0; i < slotCnt && i < loaders.Length; i++) {
        var cl = loaders[i]; bool occ = !string.IsNullOrWhiteSpace(cl.CarID);
        float avgC = 0f;
        if (occ) { try { var ip2=cl.indexedParts; if(ip2!=null) {
            float s=0f; int n=0;
            for(int j=0;j<ip2.Count;j++) if(ip2[j]!=null){s+=ip2[j].condition;n++;}
            if(n>0) avgC=s/n; } } catch {} }
        sCarLbl[i].SetText(occ ? cl.CarID : "— empty —"); sCarLbl[i].SetColor(occ ? C_OK : C_DIM);
        sStatLbl[i].SetText(occ ? "occupied" : "free"); sStatLbl[i].SetColor(occ ? C_WARN : C_DIM);
        sCondLbl[i].SetText(occ ? (avgC*100f).ToString("F0")+"%" : "—"); sCondLbl[i].SetColor(occ ? CC(avgC) : C_DIM);
        sRemBtn[i].SetVisible(occ); sInspBtn[i].SetVisible(occ);
    }
};

// ── Coroutines ─────────────────────────────────────────────────────────────
System.Collections.IEnumerator DoSpawn(string carID, int targetSlot) {
    var loaders = GetLoaders();
    if (targetSlot < 0 || targetSlot >= loaders.Length) { SetStatus("Invalid slot", C_BAD); yield break; }
    var free = loaders[targetSlot];
    if (!string.IsNullOrWhiteSpace(free.CarID)) { SetStatus("Slot "+(targetSlot+1)+" occupied!", C_BAD); yield break; }
    var dbgS = free.gameObject.GetComponent<Il2Cpp.CarDebug>();
    if (dbgS == null) { SetStatus("CarDebug missing!", C_BAD); yield break; }
    dbgS.LoadCar(carID, carID == "car_mayenm5" ? 1 : 0);
    float timeout = 10f;
    while (!free.done && timeout > 0f) { timeout -= 0.1f; yield return new UnityEngine.WaitForSeconds(0.1f); }
    yield return new UnityEngine.WaitForFixedUpdate();
    yield return new UnityEngine.WaitForEndOfFrame();
    if (!string.IsNullOrWhiteSpace(free.CarID)) {
        SetStatus(carID+" spawned in slot "+(targetSlot+1)+"!", C_OK);
        RefreshSlots(GetLoaders()); }
    else SetStatus("Spawn failed", C_BAD);
}

System.Collections.IEnumerator DoUnload(Il2CppCMS.Core.Car.CarLoader loader) {
    if (loader == null || string.IsNullOrWhiteSpace(loader.CarID)) {
        SetStatus("Slot already empty", C_DIM); yield break; }
    string removed = loader.CarID;
    try {
        // DeleteCar requires DeleteCarReason enum — getting type via reflection
        var mi = loader.GetType().GetMethod("DeleteCar",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (mi == null) { SetStatus("DeleteCar not found!", C_BAD); yield break; }
        var reasonType = mi.GetParameters()[0].ParameterType;
        var reasonVal  = System.Enum.ToObject(reasonType, 1);   // 1 = Player
        mi.Invoke(loader, new object[]{ reasonVal, true });
    } catch (System.Exception ex) { SetStatus("Delete error: "+ex.Message, C_BAD); yield break; }
    float timeout = 3f;
    while (!string.IsNullOrWhiteSpace(loader.CarID) && timeout > 0f) {
        timeout -= 0.1f; yield return new UnityEngine.WaitForSeconds(0.1f); }
    yield return new UnityEngine.WaitForFixedUpdate();
    yield return new UnityEngine.WaitForEndOfFrame();
    SetStatus(removed+" removed", C_WARN);
    RefreshSlots(GetLoaders());
}

FnSpawn  = DoSpawn;
FnUnload = DoUnload;

Print("=== Car Spawner ready  —  "+slotCnt+" slots found ===");