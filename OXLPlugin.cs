using CMS2026UITKFramework;
using MelonLoader;
using System;
using System.Linq;

[assembly: MelonInfo(typeof(CMS2026_OXL.OXLPlugin),
    "CMS2026_OXL", "0.7.0", "iBlaster")]
[assembly: MelonGame("Red Dot Games", "Car Mechanic Simulator 2027 Demo")]
[assembly: MelonGame("Red Dot Games", "Car Mechanic Simulator 2027")]

namespace CMS2026_OXL
{
    public class OXLPlugin : MelonMod
    {
        internal static MelonLogger.Instance Log => Melon<OXLPlugin>.Logger;

        private OXLCarMarket _panel;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!sceneName.ToLower().Contains("garage")) return;
            if (!FrameworkAPI.IsReady) return;

            GameTimeProvider.Reset();// reset przy każdym załadowaniu sceny

            // Nazwa w historii banku Redline zamiast samego "Oxl" (kolumna "From").
            try { BlastCoreOS.BlastCoreBankAPI.RegisterMod(GameBridge.BankModId, "OXL Marketplace"); }
            catch (Exception ex) { Log.Warning($"[OXL] Bank RegisterMod failed: {ex.Message}"); }

            OXLSettings.Load();

            // Rynek z danych gry przebudowuje się po każdym wejściu do garażu; nadawca maili "OXL Marketplace".
            MarketCatalog.Invalidate();
            CarPhotoPack.Invalidate();
            OXLMail.Register();

            OXLWebPage.EnsureRegistered();
            _panel = OXLWebPage.Backend;

            // Ogłoszenia są osobno dla każdego zapisu gry (site.md 17): stoją, aż BlastCoreOS poda slot (OnUpdate, OXLSaveSlot.TryActivate).
            OXLSaveSlot.Reset();
            // Ogłoszenia przedmiotów (silniki, części, narzędzia, dekoracje) żyją tylko w garażu (zasada 7).
            ItemListingSystem.OnGarageLoaded();
            _inGarage = true;

            // Komunikat przy otwarciu paczki od scammera (G73); patch raz na sesję, metoda gry żyje przez zmiany scen.
            OXLScamParcels.Install(HarmonyInstance);

            // Części silnika tylko do montażu przy aucie: nauka z modeli w scenie (silniki aut na parkingu), OXLEngineParts.
            OXLEngineParts.ScanScene();

            TryRegisterConsole();
        }

        private bool _inGarage;

        // Wyjście ze sceny garażu (menu, inna lokacja): ogłoszenia przedmiotów przestają tykać. Inne doładowywane sceny nie wyłączają.
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName != null && sceneName.ToLower().Contains("garage")) { _inGarage = false; OXLSaveSlot.Reset(); }
        }

        public override void OnUpdate()
        {
            OXLWebPage.Tick(UnityEngine.Time.deltaTime);
            try { CarPhotoPack.Tick(); }
            catch (Exception ex) { Log.Warning($"[OXL:PHOTO] Tick failed: {ex.Message}"); }
            if (_inGarage)
            {
                // Slot zapisu właśnie znany: ogłoszenia aut i przedmiotów tego zapisu (nowa kariera = puste, generator dokłada).
                if (OXLSaveSlot.TryActivate())
                {
                    try { _panel?.ReloadListingsForSlot(); ItemListingSystem.ReloadForSlot(); }
                    catch (Exception ex) { Log.Warning($"[OXL:SAVE] reload for slot {OXLSaveSlot.Slot} failed: {ex.Message}"); }
                }
                try { ItemListingSystem.Tick(); }
                catch (Exception ex) { Log.Warning($"[OXL:ITEMS] Tick failed: {ex.Message}"); }
            }
        }

        private void TryRegisterConsole()
        {
            try
            {
                var apiType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.FullName == "CMS2026SimpleConsole.ConsoleAPI");

                if (apiType == null) return;

                var print = apiType.GetMethod("Print", new[] { typeof(string), typeof(string) });
                var register = apiType.GetMethod("RegisterCommand");

                void Print(string msg) => print?.Invoke(null, new object[] { msg, "OXL" });

                // ── oxl_status — podsumowanie aktywnych listingów ─────────────────
                register?.Invoke(null, new object[]
                {
            "oxl_status",
            "Show active listing count and game time",
            (Action<string[]>)(_ =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                var listings = _panel.GetActiveListings();
                Print($"Active listings: {listings.Count}");
                Print($"Game time: {_panel.GetGameTime():F0}s");
                foreach (var l in listings)
                {
                    float rem = l.ExpiresAt - _panel.GetGameTime();
                    Print($"  [{l.Archetype,-9} L{l.ArchetypeLevel}] {l.Make} {l.Model} {l.Year} | ${l.Price:N0} | " +
                          $"{l.SellerRating}★ | Cond={l.ApparentCondition:P0}/{l.ActualCondition:P0} Body={l.BodyCondition:P0} | " +
                          $"Expires in {(int)(rem/60f)}:{(int)(rem%60f):D2}");
                }
            })
                });


                register?.Invoke(null, new object[]
{
    "oxl_check_now",
    "Force a generation check on next tick",
    (Action<string[]>)(_ =>
    {
        _panel?.ForceGenCheck();
        Print("Generation check forced — will trigger on next tick.");
    })
});


                register?.Invoke(null, new object[]
{
    "oxl_logs",
    "oxl_logs <on|off> — toggle verbose OXL logging",
    (Action<string[]>)(args =>
    {
        if (args.Length < 2)
        {
            Print($"OXL logs: {(OXLLog.Enabled ? "ON" : "OFF")}");
            return;
        }
        OXLLog.Enabled = args[1].Equals("on", StringComparison.OrdinalIgnoreCase);
        Print($"OXL logs: {(OXLLog.Enabled ? "ON" : "OFF")}");
    })
});

                // ── oxl_end_all — kończy wszystkie aukcje ─────────────────────────
                register?.Invoke(null, new object[]
                {
            "oxl_end_all",
            "oxl_end_all [cars|parts|tools] - end all OXL listings now (no argument = every tab); new ones come right away",
            (Action<string[]>)(args =>
{
    // Użytkownik 2026-11-28: kończyło tylko auta, części zostawały te same. Bez argumentu wszystkie zakładki, z argumentem jedna.
    string what = args.Length > 1 ? args[1].ToLowerInvariant() : "all";
    if (what != "all" && what != "cars" && what != "parts" && what != "tools") { Print("Usage: oxl_end_all [cars|parts|tools]"); return; }
    if (what == "all" || what == "cars")
    {
        if (_panel == null) { Print("OXL panel not initialized."); return; }
        int count = _panel.GetActiveListings().Count;
        _panel.GetActiveListings().Clear();
        _panel.SaveListings();  // zapis pustej listy
        Print($"Ended {count} car listing(s).");
    }
    if (what != "cars")
    {
        int n = ItemListingSystem.EndListings(what == "parts" ? MarketCategory.EnginesParts : what == "tools" ? MarketCategory.ToolsDecor : (MarketCategory?)null);
        Print($"Ended {n} item listing(s). New ones appear in a moment.");
    }
})
                });

                // ── oxl_generate — generuje N nowych listingów ───────────────────
                register?.Invoke(null, new object[]
                {
            "oxl_generate",
            "[DEV] oxl_generate <count> - add up to N car listings now (up to the limit from OXL settings)",
            (Action<string[]>)(args =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                int n = 1;
                if (args.Length > 1) int.TryParse(args[1], out n);
                n = Math.Max(1, Math.Min(50, n));
                // Liczymy faktycznie dodane: przy limicie z ustawień generator nic nie dokłada (komunikat mówił wcześniej "Generated n").
                int before = _panel.GetActiveListings()?.Count ?? 0;
                _panel.GenerateListings(n);
                int after = _panel.GetActiveListings()?.Count ?? 0;
                Print($"Generated {after - before} new listing(s). Total: {after}" + (after - before < n ? " (limit from OXL settings reached)" : ""));
            })
                });

                // ── oxl_reveal — ujawnia prawdziwy stan wszystkich listingów ──────
                register?.Invoke(null, new object[]
                {
            "oxl_reveal",
            "[CHEAT] Reveal actual condition and hidden faults of all car listings",
            (Action<string[]>)(_ =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                var listings = _panel.GetActiveListings();
                if (listings.Count == 0) { Print("No active listings."); return; }
                Print($"══ REVEAL ALL ({listings.Count} listings) ════════════");
                foreach (var l in listings)
                {
                    float gap = l.ApparentCondition - l.ActualCondition;
                    string warning = gap > 0.25f ? " ⚠ SCAM" : gap > 0.10f ? " ! suspicious" : "";
                    Print($"  {l.Make} {l.Model} | {l.Archetype,-9} | " +
                          $"App={l.ApparentCondition:P0} Act={l.ActualCondition:P0}{warning}");
                    if (l.Faults != FaultFlags.None)
                        Print($"    Faults: {l.Faults}");
                }
                Print($"══════════════════════════════════════════");
            })
                });


                                // ── oxl_set_difficulty — zmienia trudność z konsoli ───────────────
                                register?.Invoke(null, new object[]
                {
            "oxl_difficulty",
            "oxl_difficulty <easy|normal|hard> — set economy difficulty",
            (Action<string[]>)(args =>
            {
                if (args.Length < 2) { Print($"Current difficulty: {OXLSettings.CurrentDifficulty}"); return; }
                if (Enum.TryParse(args[1], ignoreCase: true, out Difficulty d))
                {
                    OXLSettings.Set(d);
                    Print($"Difficulty set to: {d}  (multiplier: {OXLSettings.PriceMultiplier:P0})");
                }
                else
                    Print($"Unknown difficulty '{args[1]}'. Use: easy | normal | hard");
            })
                });

                // ── oxl_items — ogłoszenia przedmiotów (przed nowymi stronami: podgląd i kupno z konsoli) ──
                register?.Invoke(null, new object[] { "oxl_items", "oxl_items [parts|tools] - list active OXL listings of engines, parts, tools and decorations", (Action<string[]>)(args =>
                {
                    var list = ItemListingSystem.Active.ToList();
                    if (args.Length > 1) { var cat = args[1].StartsWith("tool", StringComparison.OrdinalIgnoreCase) ? MarketCategory.ToolsDecor : MarketCategory.EnginesParts; list = list.Where(l => l.Category == cat).ToList(); }
                    double now = GameTimeProvider.TotalGameSeconds;
                    Print($"Item listings: {list.Count}");
                    for (int i = 0; i < ItemListingSystem.Active.Count; i++)
                    {
                        var l = ItemListingSystem.Active[i];
                        if (!list.Contains(l)) continue;
                        string cond = l.HasCondition ? $" stated {l.ApparentCondition:P0} real {l.ActualCondition:P0}" : "";
                        Print($"  [{i}] {l.Name} | ${l.Price:N0} (new ${l.NewPrice:N0}) | {l.Archetype}{(l.IsGem ? " GEM" : "")}{cond} | {l.SellerNick} | {(l.ExpiresAt - now) / 3600.0:F0}h left");
                        Print($"      \"{l.Story}\"");
                    }
                }) });

                register?.Invoke(null, new object[] { "oxl_items_generate", "[DEV] oxl_items_generate <count> [parts|tools] - add new item listings now (above the limit from OXL settings)", (Action<string[]>)(args =>
                {
                    if (!MarketCatalog.EnsureBuilt()) { Print("Game data not ready yet."); return; }
                    int n = 3; if (args.Length > 1) int.TryParse(args[1], out n);
                    var cat = args.Length > 2 && args[2].StartsWith("tool", StringComparison.OrdinalIgnoreCase) ? MarketCategory.ToolsDecor : MarketCategory.EnginesParts;
                    int made = 0;
                    for (int i = 0; i < Math.Max(1, Math.Min(30, n)); i++) if (ItemListingSystem.Generate(cat, GameTimeProvider.TotalGameSeconds) != null) made++;
                    Print($"Generated {made} {cat} listing(s). Total: {ItemListingSystem.Active.Count}");
                }) });

                register?.Invoke(null, new object[] { "oxl_buyitem", "oxl_buyitem <index> - buy an item listing from oxl_items (pays from your Redline account)", (Action<string[]>)(args =>
                {
                    if (args.Length < 2 || !int.TryParse(args[1], out int idx) || idx < 0 || idx >= ItemListingSystem.Active.Count) { Print("Usage: oxl_buyitem <index from oxl_items>"); return; }
                    ItemListingSystem.Buy(ItemListingSystem.Active[idx], out string msg);
                    Print(msg);
                }) });

                // ── oxl_parcels — paczki u kuriera OXL (jeszcze nie w systemie przesyłek gry, więc kalendarz ich nie widzi) ──
                register?.Invoke(null, new object[] { "oxl_parcels", "oxl_parcels - list OXL parcels on the way that the game courier has not received yet", (Action<string[]>)(_ =>
                {
                    var list = ItemListingSystem.GetPendingParcels();
                    Print(list.Count == 0 ? "No OXL parcels waiting at the OXL courier." : $"OXL parcels at the OXL courier: {list.Count}");
                    foreach (var (_, name, kind, secs) in list) Print($"  {name} ({kind}) arrives in {Math.Max(0, (int)(secs / 3600))}h {Math.Max(0, (int)(secs % 3600 / 60))}m");
                }) });

                // ── oxl_market — co OXL może sprzedawać (dane z gry, MarketCatalog) ──
                register?.Invoke(null, new object[] { "oxl_market", "oxl_market [cars|parts|tools] - show what OXL can sell (read from game data)", (Action<string[]>)(args =>
                {
                    if (!MarketCatalog.EnsureBuilt()) { Print("Game data not ready yet."); return; }
                    var all = MarketCatalog.All;
                    Print($"Cars (safe versions): {all.Count(e => e.Category == MarketCategory.Cars)}, engines: {all.Count(e => e.Kind == OXLItemKind.Engine)}, parts >= ${MarketCatalog.MinPartPrice}: {all.Count(e => e.Category == MarketCategory.EnginesParts && e.Kind == OXLItemKind.Part)} in {all.Where(e => e.Category == MarketCategory.EnginesParts && e.Kind == OXLItemKind.Part).Select(e => e.Group).Distinct().Count()} groups ({string.Join(", ", all.Where(e => e.Category == MarketCategory.EnginesParts && e.Kind == OXLItemKind.Part).GroupBy(e => e.Group).Select(g => g.Key + " " + g.Count()))}), tools: {all.Count(e => e.Category == MarketCategory.ToolsDecor && e.IsTool)}, decorations: {all.Count(e => e.Category == MarketCategory.ToolsDecor && !e.IsTool)}");
                    if (args.Length < 2) return;
                    MarketCategory cat = args[1].StartsWith("car", StringComparison.OrdinalIgnoreCase) ? MarketCategory.Cars : args[1].StartsWith("tool", StringComparison.OrdinalIgnoreCase) ? MarketCategory.ToolsDecor : MarketCategory.EnginesParts;
                    foreach (var e in MarketCatalog.In(cat).Take(40)) Print($"  {e.Id} | {e.Name} | ${e.Price:N0} | {e.Group} | photo={(MarketCatalog.GetThumbnail(e) != null ? "yes" : "no")}{(e.Category == MarketCategory.Cars ? $" | {e.Make} / {e.Model} / {e.VersionName} | {e.YearMin}-{e.YearMax} | {e.Drive} | {e.EngineName} | {e.Hp} HP @{e.HpRpm} | {e.TorqueNm} Nm @{e.TorqueRpm} | {e.Weight} kg | {e.Gears} gears" : "")}");
                }) });

                // ── oxl_deliver — test dostawy bez zakupu (bez pobrania z banku): paczka i mail jak po kupnie ──
                register?.Invoke(null, new object[] { "oxl_deliver", "[CHEAT] oxl_deliver <id> [condition 1-100] [now] - send yourself an OXL parcel with this part, engine, tool or decoration for free (testing)", (Action<string[]>)(args =>
                {
                    if (args.Length < 2) { Print("Usage: oxl_deliver <id> [condition 1-100] [now]"); return; }
                    if (!MarketCatalog.EnsureBuilt()) { Print("Game data not ready yet."); return; }
                    if (!MarketCatalog.TryGet(args[1], out var e) || e.Category == MarketCategory.Cars) { Print($"'{args[1]}' is not a part, engine, tool or decoration OXL can sell (see oxl_market parts / tools)."); return; }
                    int cond = 60; if (args.Length > 2) int.TryParse(args[2], out cond);
                    bool now = args.Length > 3 ? args[3].Equals("now", StringComparison.OrdinalIgnoreCase) : OXLSettings.InstantDelivery;
                    var r = OXLDelivery.Send(e.Kind, e.Id, Math.Max(1, Math.Min(100, cond)) / 100f, now);
                    if (!r.Ok) { Print($"Delivery failed: {r.Error}"); return; }
                    OXLMail.SendPurchase(e.Name, "OXL test", 0, OXLDelivery.EtaText(r), "test-" + DateTime.Now.Ticks);
                    Print($"Sent {e.Name}: {OXLDelivery.EtaText(r)}.");
                }) });

                Log.Msg("[OXL] Registered in SimpleConsole.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[OXL] Console registration failed: {ex.Message}");
            }
        }
    }




    /// <summary>
    /// Routes log messages to SimpleConsole if available, otherwise MelonLoader.
    /// </summary>
    internal static class OXLLog
    {
        private static readonly MelonLogger.Instance _log =
            new MelonLogger.Instance("CMS2026_OXL");

        private static System.Reflection.MethodInfo _consolePrint;
        private static bool _resolved;

        public static bool Enabled { get; set; } = false;  // domyślnie cicho

        private static void TryResolve()
        {
            if (_resolved) return;
            try
            {
                var t = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try { return a.GetTypes(); }
                        catch { return System.Type.EmptyTypes; }
                    })
                    .FirstOrDefault(x => x.FullName == "CMS2026SimpleConsole.ConsoleAPI");

                _consolePrint = t?.GetMethod("Print", new[] { typeof(string), typeof(string) });
                if (_consolePrint == null)
                    _consolePrint = t?.GetMethod("Print", new[] { typeof(string) });
                if (_consolePrint != null) _resolved = true;
            }
            catch { }
        }

        public static void Msg(string msg)
        {
            if (!Enabled) return;
            try
            {
                TryResolve();
                if (_consolePrint != null)
                {
                    var pars = _consolePrint.GetParameters();
                    if (pars.Length == 2)
                        _consolePrint.Invoke(null, new object[] { msg, "OXL" });
                    else
                        _consolePrint.Invoke(null, new object[] { msg });
                    return;
                }
            }
            catch { }
            _log.Msg(msg);
        }

        public static void Error(string msg) => _log.Error(msg);
        public static void Always(string msg) => _log.Msg(msg);
    }
}
