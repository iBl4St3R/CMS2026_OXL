using CMS2026UITKFramework;
using MelonLoader;
using System;
using System.Linq;

[assembly: MelonInfo(typeof(CMS2026_OXL.OXLPlugin),
    "CMS2026_OXL", "0.3.0", "Blaster")]
[assembly: MelonGame("Red Dot Games", "Car Mechanic Simulator 2026 Demo")]
[assembly: MelonGame("Red Dot Games", "Car Mechanic Simulator 2026")]

namespace CMS2026_OXL
{
    public class OXLPlugin : MelonMod
    {
        internal static MelonLogger.Instance Log => Melon<OXLPlugin>.Logger;

        private OXLPanel _panel;

        private UnityEngine.GameObject _oxlShopButton;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!sceneName.ToLower().Contains("garage")) return;
            if (!FrameworkAPI.IsReady) return;

            CursorManager.OnCursorShow -= OnCursorShow;
            CursorManager.OnCursorHide -= OnCursorHide;
            CursorManager.OnCursorShow += OnCursorShow;
            CursorManager.OnCursorHide += OnCursorHide;

            OXLSettings.Load();

            _panel = new OXLPanel();
            _panel.Build();

            // Wstrzyknij przycisk do menu sklepu
            MelonCoroutines.Start(InjectShopButtonDelayed());

            TryRegisterConsole();
        }

        private static void OnCursorShow()
        {
            try
            {
                if (Il2CppCMS.Core.GameMode.Get().currentMode != Il2Cpp.gameMode.UI)
                    Il2CppCMS.Core.GameMode.Get().SetCurrentMode(Il2Cpp.gameMode.UI);
            }
            catch (Exception ex) { Log.Warning($"[OXL] OnCursorShow: {ex.Message}"); }
        }

        private void OnCursorHide()
        {
            try
            {
                if (Il2CppCMS.Core.GameMode.Get().currentMode == Il2Cpp.gameMode.UI)
                {
                    var wm = Il2CppCMS.UI.WindowManager.Instance;
                    if (wm == null || wm.activeWindows.Count <= 0)
                        Il2CppCMS.Core.GameMode.Get().SetCurrentMode(Il2Cpp.gameMode.Garage);
                }
            }
            catch (Exception ex) { Log.Warning($"[OXL] OnCursorHide: {ex.Message}"); }
        }

        public override void OnUpdate()
        {
            if (_panel != null && _panel.IsVisible
                && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
            {
                _panel.Close();
            }
        }

        // ── Wstrzyknięcie przycisku do menu sklepu ────────────────────────────────
        private void InjectShopButton()
        {
            try
            {
                UnityEngine.GameObject shopListGO = null;

                foreach (var go in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>())
                {
                    if (go.name == "ShopsList" && go.scene.name == "garage")
                    {
                        shopListGO = go;
                        break;
                    }
                }

                if (shopListGO == null)
                {
                    Log.Msg("[OXL] ShopsList not found in scene.");
                    return;
                }

                Log.Msg($"[OXL] ShopsList found, active={shopListGO.activeInHierarchy}, children={shopListGO.transform.childCount}");

                var existing = shopListGO.transform.Find("OXLShopButton");
                if (existing != null) UnityEngine.Object.Destroy(existing.gameObject);

                if (shopListGO.transform.childCount == 0)
                {
                    Log.Msg("[OXL] ShopsList has no children — cannot clone template.");
                    return;
                }

                var lastChild = shopListGO.transform.GetChild(shopListGO.transform.childCount - 1);
                var clone = UnityEngine.Object.Instantiate(lastChild.gameObject, shopListGO.transform);
                clone.name = "OXLShopButton";
                clone.SetActive(true);
                clone.transform.SetAsLastSibling();
                _oxlShopButton = clone;

                // ── Podmień obrazek ──────────────────────────────────────────────────
                string imgPath = System.IO.Path.Combine(
                    UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL",
                    "Resources", "buttons", "OXLbutton.png");

                var imageGO = clone.transform.Find("Image");
                if (imageGO != null && System.IO.File.Exists(imgPath))
                {
                    var img = imageGO.gameObject.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        var bytes = System.IO.File.ReadAllBytes(imgPath);
                        var tex = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);

                        var il2b = new Il2CppInterop.Runtime.InteropTypes
                                       .Arrays.Il2CppStructArray<byte>(bytes.Length);
                        for (int i = 0; i < bytes.Length; i++) il2b[i] = bytes[i];

                        var icType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                            .FirstOrDefault(t => t.FullName == "UnityEngine.ImageConversion");

                        var loadImg = icType?.GetMethods()
                            .FirstOrDefault(m => m.Name == "LoadImage" && m.GetParameters().Length == 2);

                        bool ok = loadImg != null && (bool)loadImg.Invoke(null, new object[] { tex, il2b });
                        if (!ok)
                            Log.Msg("[OXL] InjectShopButton: texture load failed");
                        else
                        {
                            tex.hideFlags = UnityEngine.HideFlags.DontUnloadUnusedAsset;
                            img.sprite = UnityEngine.Sprite.Create(
                                tex,
                                new UnityEngine.Rect(0, 0, tex.width, tex.height),
                                new UnityEngine.Vector2(0.5f, 0.5f));
                            img.color = UnityEngine.Color.white;
                        }
                    }
                }

                // ── Podmień tekst ────────────────────────────────────────────────────
                var textGO = clone.transform.Find("Text");
                if (textGO != null)
                {
                    foreach (var c in textGO.gameObject.GetComponents<UnityEngine.Component>())
                    {
                        if (c.GetIl2CppType().FullName == "CMS.Localization.TextLocalize")
                        {
                            UnityEngine.Object.Destroy(c);
                            break;
                        }
                    }
                    foreach (var c in textGO.gameObject.GetComponents<UnityEngine.Component>())
                    {
                        if (c.GetIl2CppType().FullName == "TMPro.TextMeshProUGUI")
                        {
                            c.GetIl2CppType().GetProperty("text")?.SetValue(c, "OXL Market Place v0.4.0");
                            c.GetIl2CppType().GetProperty("enabled")?.SetValue(c, true);
                            break;
                        }
                    }
                }

                // ── Ukryj DealsIcon ──────────────────────────────────────────────────
                var dealsGO = clone.transform.Find("DealsIcon");
                if (dealsGO != null) dealsGO.gameObject.SetActive(false);

                // ── Zachowaj ShopAvatar25 dla dźwięku, wyczyść jego akcję ───────────
                UnityEngine.Component shopAvatar = null;
                foreach (var c in clone.GetComponents<UnityEngine.Component>())
                {
                    if (c.GetIl2CppType().FullName == "CMS.UI.Logic.Shop.Controls.ShopAvatar25")
                    {
                        shopAvatar = c;
                        break;
                    }
                }

                if (shopAvatar != null)
                {
                    try
                    {
                        var onItemSubmit = shopAvatar.GetIl2CppType().GetField("OnItemSubmit");
                        onItemSubmit?.SetValue(shopAvatar, null);
                        Log.Msg("[OXL] ShopAvatar25.OnItemSubmit cleared.");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[OXL] Could not clear OnItemSubmit: {ex.Message}");
                    }
                }

                // ── Kliknięcie → dźwięk z ShopAvatar25 + otwórz panel ───────────────
                var trigger = clone.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                var entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                entry.callback.AddListener(
                    new System.Action<UnityEngine.EventSystems.BaseEventData>(eventData =>
                    {
                        if (shopAvatar != null)
                        {
                            try
                            {
                                var pointerData = eventData
                                    .TryCast<UnityEngine.EventSystems.PointerEventData>();
                                var onPointerClick = shopAvatar.GetIl2CppType()
                                    .GetMethod("OnPointerClick");
                                var args = new Il2CppInterop.Runtime.InteropTypes.Arrays
                                               .Il2CppReferenceArray<Il2CppSystem.Object>(1);
                                args[0] = pointerData?.TryCast<Il2CppSystem.Object>();
                                onPointerClick?.Invoke(shopAvatar, args);
                            }
                            catch (Exception ex)
                            {
                                Log.Warning($"[OXL] OnPointerClick invoke failed: {ex.Message}");
                            }
                        }

                        _panel?.Open();
                    }));
                trigger.triggers.Add(entry);

                Log.Msg("[OXL] Shop button injected successfully.");
            }
            catch (Exception ex)
            {
                Log.Warning($"[OXL] InjectShopButton failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator InjectShopButtonDelayed()
        {
            float timeout = 10f;
            while (timeout > 0f)
            {
                bool found = false;
                foreach (var go in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>())
                {
                    if (go.name == "ShopsList" && go.scene.name == "garage")
                    { found = true; break; }
                }
                if (found) break;

                timeout -= 0.5f;
                yield return new UnityEngine.WaitForSeconds(0.5f);
            }
            InjectShopButton();
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

                // ── Istniejąca komenda ────────────────────────────────────────────
                register?.Invoke(null, new object[]
                {
            "oxl_open",
            "Toggle the OXL auction panel",
            (Action<string[]>)(_ => { _panel?.Toggle(); })
                });

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

                // ── oxl_end_all — kończy wszystkie aukcje ─────────────────────────
                register?.Invoke(null, new object[]
                {
            "oxl_end_all",
            "End all active auctions immediately",
            (Action<string[]>)(_ =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                int count = _panel.GetActiveListings().Count;
                _panel.GetActiveListings().Clear();
                Print($"Ended {count} active auction(s).");
            })
                });

                // ── oxl_generate — generuje N nowych listingów ───────────────────
                register?.Invoke(null, new object[]
                {
            "oxl_generate",
            "oxl_generate <count> — generate N new listings (1–20)",
            (Action<string[]>)(args =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                int n = 1;
                if (args.Length > 1) int.TryParse(args[1], out n);
                n = Math.Max(1, Math.Min(50, n));
                _panel.GenerateListings(n);
                Print($"Generated {n} new listing(s). Total: {_panel.GetActiveListings().Count}");
            })
                });

                // ── oxl_detail — szczegóły aktualnie otwartego listingu ──────────
                register?.Invoke(null, new object[]
                {
            "oxl_detail",
            "Show full details of the currently open listing",
            (Action<string[]>)(_ =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                var l = _panel.GetCurrentDetailListing();
                if (l == null) { Print("No listing currently open. Open a detail view first."); return; }

                Print($"══ LISTING DETAIL ═══════════════════════");
                Print($"  Car:        {l.Make} {l.Model} {l.Year}");
                Print($"  Price:      ${l.Price:N0}");
                Print($"  Plate:      {l.Registration}");
                Print($"  Color:      {l.Color}");
                Print($"  Mileage:    {l.Mileage:N0} mi");
                Print($"  Location:   {l.Location}  (~{l.DeliveryHours}h delivery)");
                Print($"──────────────────────────────────────────");
                Print($"  Archetype:  {l.Archetype} L{l.ArchetypeLevel}");
                Print($"  Rating:     {l.SellerRating}★");
                Print($"  Apparent:   {l.ApparentCondition:P0}  (what buyer sees)");
                Print($"  Actual:     {l.ActualCondition:P0}  (real condition)");
                Print($"  Honesty:    {l.ActualCondition / Math.Max(l.ApparentCondition, 0.01f):P0}");
                Print($"  Faults:     {l.Faults}");
                Print($"──────────────────────────────────────────");
                Print($"  Note:       \"{l.SellerNote}\"");
                Print($"══════════════════════════════════════════");
            })
                });

                // ── oxl_end_current — kończy aktualnie otwarty listing ───────────
                register?.Invoke(null, new object[]
                {
            "oxl_end_current",
            "End the currently open listing auction",
            (Action<string[]>)(_ =>
            {
                if (_panel == null) { Print("OXL panel not initialized."); return; }
                var l = _panel.GetCurrentDetailListing();
                if (l == null) { Print("No listing currently open."); return; }
                string name = $"{l.Make} {l.Model} {l.Year}";
                _panel.GetActiveListings().Remove(l);
                _panel.CloseDetail();
                Print($"Auction ended: {name}");
            })
                });

                // ── oxl_reveal — ujawnia prawdziwy stan wszystkich listingów ──────
                register?.Invoke(null, new object[]
                {
            "oxl_reveal",
            "Reveal actual condition and hidden faults for all active listings",
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


                // ── oxl_memory — pomiar RAM tylko naszego systemu ─────────────────────
                register?.Invoke(null, new object[]
                {
    "oxl_memory",
    "Show OXL memory usage (textures, listings, cache)",
    (Action<string[]>)(_ =>
    {
        if (_panel == null) { Print("OXL panel not initialized."); return; }

        // ── Tekstury w cache ──────────────────────────────────────────────
        var cacheInfo = _panel.GetPhotoCacheInfo();

        // ── Listings ──────────────────────────────────────────────────────
        int listingCount = _panel.GetActiveListings()?.Count ?? 0;
        int photoFilesTotal = _panel.GetActiveListings()
            ?.Sum(l => l.PhotoFiles?.Count ?? 0) ?? 0;

        // ── Managed heap — nasz assembly ─────────────────────────────────
        long managedBytes = GC.GetTotalMemory(false);

        Print($"══ OXL MEMORY ════════════════════════════");
        Print($"  Texture cache:   {cacheInfo.Count} textures  (~{cacheInfo.EstimatedMB:F1} MB)");
        Print($"  Thumbnail cache: {cacheInfo.ThumbCount} thumbs    (~{cacheInfo.ThumbMB:F1} MB)");
        Print($"  LRU entries:     {cacheInfo.LruCount}");
        Print($"  Fallbacks:       {cacheInfo.FallbackCount} textures");
        Print($"──────────────────────────────────────────");
        Print($"  Active listings: {listingCount}");
        Print($"  PhotoFiles refs: {photoFilesTotal} path strings");
        Print($"──────────────────────────────────────────");
        Print($"  Managed heap:    {managedBytes / 1024f / 1024f:F2} MB  (whole process)");
        Print($"  (heap includes Unity, MelonLoader, all mods)");
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

        private static void TryResolve()
        {
            if (_resolved) return;
            _resolved = true;
            try
            {
                var t = System.AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => {
                        try { return a.GetTypes(); }
                        catch { return System.Type.EmptyTypes; }
                    })
                    .FirstOrDefault(x => x.FullName == "CMS2026SimpleConsole.ConsoleAPI");

                _consolePrint = t?.GetMethod("Print",
                    new[] { typeof(string), typeof(string) });

                if (_consolePrint == null)
                    _consolePrint = t?.GetMethod("Print",
                        new[] { typeof(string) });
            }
            catch { }
        }

        public static void Msg(string msg)
        {
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

        public static void Warn(string msg) => _log.Warning(msg);
        public static void Error(string msg) => _log.Error(msg);
    }
}