// MarketCatalog.cs
// Co OXL może sprzedawać, w całości z danych gry (site.md 9.3): bez list aut, części i ID w kodzie, więc po aktualizacji gry
// nowe auta, części i dekoracje pojawiają się same. Źródła: BlastCoreGameDataAPI (auta, części, silniki) i GarageObjectsProvider gry
// (narzędzia i dekoracje, których API BlastCoreOS nie ma). Wiedza: Docs/Research/Market.md.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CMS2026_OXL
{
    public enum MarketCategory { Cars, EnginesParts, ToolsDecor }

    /// <summary>Jedna rzecz, którą sprzedawca może wystawić (wzorzec ogłoszenia, nie samo ogłoszenie).</summary>
    public sealed class MarketEntry
    {
        public MarketCategory Category;
        public OXLItemKind Kind;            // dla aut nieużywane
        public string Id = "", Name = "";
        public int Price;                    // cena nowego w sklepie gry (auta: carValue wersji)
        public string CarId = "";            // auta
        public int Version = -1;             // auta: configVersion
        public string Group = "";            // grupa do filtrów: marka auta, grupa sklepu części, Tools/Decorations
        public string[] Colors = new string[0];
        public bool IsTool;                  // ToolsDecor: narzędzie (true) czy dekoracja
        // Auta (z danych gry): do tytułów, parametrów i filtrów
        public string Make = "", Model = "", VersionName = "", Drive = "", EngineId = "", EngineName = "";
        public int YearMin, YearMax, Hp, HpRpm, TorqueNm, TorqueRpm, Weight, Gears;
    }

    public static class MarketCatalog
    {
        // Próg "droga część": percentyl cen katalogu liczony w czasie gry (site.md 9.3). 0.90 = 10% najdroższych części.
        private const float ExpensivePercentile = 0.90f;

        private static List<MarketEntry> _entries = new List<MarketEntry>();
        private static int _builtDataVersion = -1;
        private static int _expensiveThreshold;

        public static int ExpensiveThreshold { get { EnsureBuilt(); return _expensiveThreshold; } }

        public static IReadOnlyList<MarketEntry> All { get { EnsureBuilt(); return _entries; } }

        public static IEnumerable<MarketEntry> In(MarketCategory c) => All.Where(e => e.Category == c);

        public static bool TryGet(string id, out MarketEntry entry)
        {
            entry = All.FirstOrDefault(e => string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
            return entry != null;
        }

        /// <summary>
        /// Najtańszy przedmiot gry, który kurier umie dowieźć (narzędzia i dekoracje, cena > 0): to przysyła scammer zamiast kupionej rzeczy
        /// (scammer.md 5). Z danych gry, bez ID w kodzie; przy równej cenie pierwszy po nazwie, żeby był stały. null = katalog pusty.
        /// </summary>
        public static MarketEntry Cheapest() => In(MarketCategory.ToolsDecor).Where(e => e.Price > 0).OrderBy(e => e.Price).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault();

        /// <summary>Wymusza przebudowę przy następnym użyciu (wejście do garażu).</summary>
        public static void Invalidate() { _builtDataVersion = -1; }

        /// <summary>Buduje katalog, gdy dane gry są gotowe i zmieniły się od ostatniego razu. False = dane jeszcze niegotowe.</summary>
        public static bool EnsureBuilt()
        {
            try
            {
                if (!BlastCoreOS.BlastCoreGameDataAPI.IsReady) return false;
                int ver = BlastCoreOS.BlastCoreGameDataAPI.DataVersion;
                if (ver == _builtDataVersion && _entries.Count > 0) return true;
                var list = new List<MarketEntry>();
                AddCars(list);
                AddEnginesAndParts(list);
                AddToolsDecor(list);
                _entries = list;
                _builtDataVersion = ver;
                OXLPlugin.Log.Msg($"[OXL:MARKET] Catalog built: cars={list.Count(e => e.Category == MarketCategory.Cars)} engines={list.Count(e => e.Kind == OXLItemKind.Engine)} parts={list.Count(e => e.Category == MarketCategory.EnginesParts && e.Kind == OXLItemKind.Part)} (>= ${_expensiveThreshold}) tools={list.Count(e => e.Category == MarketCategory.ToolsDecor && e.IsTool)} decor={list.Count(e => e.Category == MarketCategory.ToolsDecor && !e.IsTool)} dataVersion={ver}");
                return true;
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:MARKET] Catalog build failed: {ex.Message}"); return false; }
        }

        // Auta: tylko wersje, których cały fabryczny osprzęt jest w tej wersji gry: silnik (G12) oraz felgi i opony przód i tył (G48:
        // Luxor Cruiser z felgą rim_274 spoza dema spawnował się, ale zapis z nim wieszał wczytywanie). Inne psują zapis gracza.
        // Marka i model z nazwy auta w grze (pierwsze słowo = marka), roczniki, napęd, silnik, moc i moment z danych gry (filtry, site.md 12).
        private static void AddCars(List<MarketEntry> list)
        {
            var edp = Il2CppCMS.Core.GameInventory.Instance?.EngineDataProvider;
            var badWheels = VersionsWithMissingWheels();
            foreach (var car in BlastCoreOS.BlastCoreGameDataAPI.GetCars())
            {
                if (car?.Versions == null) continue;
                string full = string.IsNullOrWhiteSpace(car.Name) ? car.Id : car.Name.Trim();
                int sp = full.IndexOf(' ');
                string make = sp > 0 ? full.Substring(0, sp) : full, model = sp > 0 ? full.Substring(sp + 1) : "";
                foreach (var v in car.Versions)
                {
                    if (v == null || !v.StockEngineAvailable) continue;
                    if (badWheels.TryGetValue($"{car.Id}#{v.Index}", out string missing)) { OXLLog.Msg($"[OXL:MARKET] {car.Id}#{v.Index} skipped: {missing} not in this game build"); continue; }
                    string name = string.IsNullOrWhiteSpace(v.VersionName) ? full : $"{full} {v.VersionName}";
                    var e = new MarketEntry { Category = MarketCategory.Cars, Id = $"{car.Id}#{v.Index}", Name = name, Price = v.Value, CarId = car.Id, Version = v.Index, Group = make, Colors = v.Colors ?? new string[0], Make = make, Model = model, VersionName = v.VersionName ?? "", Drive = v.Drive ?? "", EngineId = v.StockEngine ?? "", Weight = v.Weight, Gears = v.Gears };
                    ParseYears(v.Years, out e.YearMin, out e.YearMax);
                    if (BlastCoreOS.BlastCoreGameDataAPI.TryGetEngine(e.EngineId, out var eng) && eng != null) e.EngineName = eng.Name ?? "";
                    try
                    {
                        if (edp != null && edp.TryGet(e.EngineId, out var pd) && pd != null)
                        {
                            e.Hp = (int)Math.Round(pd.GetHP(out float hpRpm)); e.HpRpm = (int)hpRpm;
                            e.TorqueNm = (int)Math.Round(pd.GetMaxTorque(out float tqRpm)); e.TorqueRpm = (int)tqRpm;
                            // G36 (2026-11-25): GetHP i GetMaxTorque dają 0 bez symulacji silnika. Zapas: krzywa momentu GetTorque(rpm)
                            // od biegu jałowego do maks. obrotów, moc z CalculateHP gry (te same liczby co w karcie silnika).
                            if (e.Hp <= 0 || e.TorqueNm <= 0)
                            {
                                float idle = Math.Max(500f, pd.IdleRPM), max = Math.Max(idle + 500f, pd.MaxRPM);
                                float bestHp = 0f, bestTq = 0f, hpAt = 0f, tqAt = 0f;
                                for (float rpm = idle; rpm <= max; rpm += 100f)
                                {
                                    float tq = pd.GetTorque(rpm, 0f, 1f, 1f);
                                    float hp = Il2CppCMS.VehiclePhysics.ScriptableObjects.PhysicsEngineData.CalculateHP(tq, rpm);
                                    if (tq > bestTq) { bestTq = tq; tqAt = rpm; }
                                    if (hp > bestHp) { bestHp = hp; hpAt = rpm; }
                                }
                                if (e.Hp <= 0) { e.Hp = (int)Math.Round(bestHp); e.HpRpm = (int)hpAt; }
                                if (e.TorqueNm <= 0) { e.TorqueNm = (int)Math.Round(bestTq); e.TorqueRpm = (int)tqAt; }
                            }
                        }
                    }
                    catch (Exception ex) { OXLLog.Msg($"[OXL:MARKET] engine data '{e.EngineId}' failed: {ex.Message}"); }
                    list.Add(e);
                }
            }
        }

        // Felgi i opony fabryczne z configu wersji ([3_wheels] i [4_wheels_rear], klucze rim i tire), sprawdzone przez GameInventory.HaveDLC
        // (G48: felgi spoza dema mają HaveDLC = false). Klucz "carId#cfg" -> pierwsza brakująca część. API BlastCoreOS nie podaje kół wersji.
        private static Dictionary<string, string> VersionsWithMissingWheels()
        {
            var bad = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var inv = Il2CppCMS.Core.GameInventory.Instance;
                var cars = UnityEngine.Object.FindObjectOfType<Il2Cpp.CarBundleLoader>(true)?.CarNamesData;
                for (int ci = 0; inv != null && cars != null && ci < cars.Count; ci++)
                {
                    var data = cars[ci];
                    if (data?.CarConfigs == null) continue;
                    for (int i = 0; i < data.CarConfigs.Count; i++)
                    {
                        var ini = data.CarConfigs[i];
                        for (int k = 0; ini?.Entries != null && k < ini.Entries.Count; k++)
                        {
                            var en = ini.Entries[k];
                            string sec = en.Section ?? "", id = en.ID ?? "", val = (en.Value ?? "").Trim();
                            if ((sec != "3_wheels" && sec != "4_wheels_rear") || (id != "rim" && id != "tire") || val.Length == 0) continue;
                            bool ok = false;
                            try { ok = inv.HaveDLC(val); } catch { }
                            if (!ok) { bad[$"{data.CarID}#{i}"] = val; break; }
                        }
                    }
                }
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:MARKET] wheel check failed: {ex.Message}"); }
            return bad;
        }

        // "1985-1992", "1990", "1990 - 1995": pierwsza i ostatnia liczba czterocyfrowa; brak = 1990..2005.
        private static void ParseYears(string s, out int min, out int max)
        {
            var years = System.Text.RegularExpressions.Regex.Matches(s ?? "", @"\d{4}").Cast<System.Text.RegularExpressions.Match>().Select(m => int.Parse(m.Value)).ToList();
            min = years.Count > 0 ? years.Min() : 1990;
            max = years.Count > 0 ? years.Max() : 2005;
        }

        // Silniki: kompletne, dostępne w tej wersji gry i sprzedawane w sklepie gry (cena z katalogu).
        // Części: droższe niż próg z rozkładu cen, bez całych silników (są wyżej) i bez części z ceną 0.
        private static void AddEnginesAndParts(List<MarketEntry> list)
        {
            var engineIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in BlastCoreOS.BlastCoreGameDataAPI.GetEngines())
            {
                if (e == null || !e.Available || !e.BuyableComplete || e.Price <= 0) continue;
                if (!engineIds.Add(e.Id)) continue;
                list.Add(new MarketEntry { Category = MarketCategory.EnginesParts, Kind = OXLItemKind.Engine, Id = e.Id, Name = string.IsNullOrWhiteSpace(e.Name) ? e.Id : e.Name, Price = e.Price, Group = "Engine" });
            }

            var parts = BlastCoreOS.BlastCoreGameDataAPI.GetParts().Where(p => p != null && p.Price > 0 && !engineIds.Contains(p.Id)).ToList();
            if (parts.Count == 0) { _expensiveThreshold = 0; return; }
            var prices = parts.Select(p => p.Price).OrderBy(x => x).ToList();
            _expensiveThreshold = prices[Math.Min(prices.Count - 1, (int)(prices.Count * ExpensivePercentile))];
            foreach (var p in parts.Where(p => p.Price >= _expensiveThreshold))
                list.Add(new MarketEntry { Category = MarketCategory.EnginesParts, Kind = OXLItemKind.Part, Id = p.Id, Name = string.IsNullOrWhiteSpace(p.Name) ? p.Id : p.Name, Price = p.Price, Group = p.ShopGroup ?? "" });
        }

        // Narzędzia i dekoracje z gry (911 w demie), bez obiektów modów i bez darmowych.
        private static void AddToolsDecor(List<MarketEntry> list)
        {
            var inv = Il2CppCMS.Core.GameInventory.Instance;
            var objs = inv?.GarageObjectsProvider?.GetAllCustomizableObjects();
            if (objs == null) { OXLPlugin.Log.Warning("[OXL:MARKET] GarageObjectsProvider not ready, no tools and decorations."); return; }
            for (int i = 0; i < objs.Count; i++)
            {
                var o = objs[i];
                if (o == null || o.IsMod || o.Price == 0 || string.IsNullOrEmpty(o.ObjectID)) continue;
                string name = o.ObjectID, cat = "";
                try { name = inv.GetCustomizableItemLocalizeName(o.ObjectID) ?? o.ObjectID; } catch { }
                try { cat = inv.GetCustomizableItemCategory(o.ObjectID).ToString(); } catch { }
                bool tool = string.Equals(cat, "Tools", StringComparison.OrdinalIgnoreCase);
                list.Add(new MarketEntry { Category = MarketCategory.ToolsDecor, Kind = OXLItemKind.Customizable, Id = o.ObjectID, Name = name, Price = o.Price, Group = tool ? "Tools" : "Decorations", IsTool = tool });
            }
        }

        // ── Zdjęcia z gry (site.md 10): wersja podstawowa bez paczek ─────────────────
        /// <summary>Sprite miniatury z gry: auto (wersja) albo przedmiot; null, gdy gra go nie ma.</summary>
        public static UnityEngine.Sprite GetThumbnail(MarketEntry e)
        {
            try
            {
                var tp = Il2CppCMS.Core.GameInventory.Instance?.ThumbnailsProvider;
                if (tp == null || e == null) return null;
                if (e.Category == MarketCategory.Cars)
                {
                    var s = tp.GetCarThumb(e.CarId, e.Version);
                    return s != null && s != tp.GetNoImageCar() ? s : null;
                }
                return tp.TryGetThumbnail(e.Id, out var sp) ? sp : null;
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:MARKET] thumbnail '{e?.Id}' failed: {ex.Message}"); return null; }
        }
    }
}
