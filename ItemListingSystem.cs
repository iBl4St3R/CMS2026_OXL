// ItemListingSystem.cs
// Generator, cykl życia, zapis i kupno ogłoszeń przedmiotów (Parts, Tools & Decor), site.md 9.3–9.6.
// Dane z MarketCatalog (gra), czas z GameTimeProvider (sekundy gry), dostawa przez OXLDelivery, płatność przez bank Redline,
// potwierdzenie mailem. Kontrakt kupna: pieniądze schodzą dopiero, gdy gra przyjęła paczkę.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CMS2026_OXL
{
    public static class ItemListingSystem
    {
        // Ile ogłoszeń naraz w każdej zakładce i jak często przybywają (godziny gry). Perełki rzadko (site.md 9.4).
        // Cały silnik to rzadkość (użytkownik 2026-11-28), ale ma się pojawiać: ok. co 8. ogłoszenie zakładki Parts (było 5%, wcześniej 30%).
        private const float EngineShareOfEnginesParts = 0.12f;
        // Silniki sprzedają głównie zwykli ludzie (użytkownik 2026-11-28): scammer ma przy silnikach 1/5 swojej wagi z ustawień.
        private const float EngineScammerKeep = 0.20f;
        private const float ToolShareOfToolsDecor = 0.50f;
        private const float GemChance = 0.04f;
        private const double CheckEveryHours = 1.0;
        private const double MinLifeHours = 10.0, MaxLifeHours = 48.0;
        private const double ScamMinLifeHours = 2.0, ScamMaxLifeHours = 6.0;   // scammer znika, zanim ktoś go zgłosi

        private static readonly List<ItemListing> _listings = new List<ItemListing>();
        private static readonly Random _rng = new Random();
        private static SellerProfile _sellers;
        private static double _lastCheck = double.NaN;
        private static bool _loaded;

        public static IReadOnlyList<ItemListing> Active => _listings;

        // Plik bieżącego zapisu gry (OXLSaveSlot, site.md 17); null, gdy slot jeszcze nieznany.
        private static string DataPath => OXLSaveSlot.PathOf("items.dat");

        /// <summary>Wejście do garażu: ogłoszenia stoją, dopóki OXLSaveSlot nie zna zapisu gry (potem ReloadForSlot).</summary>
        public static void OnGarageLoaded()
        {
            _lastCheck = double.NaN;
            _loaded = false;
            if (_sellers != null) return;
            try { _sellers = new SellerProfile(Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources")); }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ITEMS] SellerProfile failed: {ex.Message}"); }
        }

        /// <summary>Slot zapisu znany (OXLSaveSlot): ogłoszenia, paczki u kuriera i pułapki scammera tego zapisu; brak pliku = nowa kariera, pusto.</summary>
        public static void ReloadForSlot()
        {
            Load();
            _loaded = true;
            _lastCheck = double.NaN;
        }

        /// <summary>Co klatkę w garażu: wygasanie i dokładanie ogłoszeń co godzinę gry. Czeka na prawdziwy czas gry i dane rynku.</summary>
        public static void Tick()
        {
            if (!_loaded || !GameTimeProvider.IsReadingFromTM) return;
            double now = GameTimeProvider.TotalGameSeconds;
            int removed = _listings.RemoveAll(l => l.ExpiresAt <= now);
            DispatchParcels(now);
            if (removed > 0) Save();

            if (!double.IsNaN(_lastCheck) && now - _lastCheck < CheckEveryHours * 3600.0) return;
            if (!MarketCatalog.EnsureBuilt()) return;
            bool first = double.IsNaN(_lastCheck);
            _lastCheck = now;
            if (first) DropUnknown();

            int added = 0;
            // Co godzinę gry dokładamy brakujące do docelowej liczby z ustawień OXL (Settings: listings per category).
            foreach (var cat in new[] { MarketCategory.EnginesParts, MarketCategory.ToolsDecor })
            {
                var gc = OXLSettings.SavedGenConfig;
                int max = cat == MarketCategory.EnginesParts ? gc.TargetParts : gc.TargetDecor;
                int have = _listings.Count(l => l.Category == cat);
                int want = max - have;   // docelowa liczba z ustawień OXL, brakujące od razu
                for (int i = 0; i < want && have < max; i++, have++) if (Generate(cat, now) != null) added++;
            }
            if (added > 0) { Save(); OXLLog.Msg($"[OXL:ITEMS] +{added} listings, active={_listings.Count}"); }
        }

        /// <summary>Nowe ogłoszenie w kategorii (też dla komendy oxl_items_generate). null, gdy rynek pusty.</summary>
        public static ItemListing Generate(MarketCategory cat, double now)
        {
            var pool = MarketCatalog.In(cat).ToList();
            if (cat == MarketCategory.EnginesParts)
            {
                bool engine = _rng.NextDouble() < EngineShareOfEnginesParts;
                var sub = pool.Where(e => (e.Kind == OXLItemKind.Engine) == engine).ToList();
                if (sub.Count > 0) pool = sub;
                // Silnik: najpierw rodzina z gry (V6 322BZ ma 11 wariantów z 17, bez tego prawie zawsze V6), potem wariant. Tańsze częściej
                // (użytkownik 2026-11-28: lepsze silniki rzadko): waga = (najtańszy / cena)^2 dla rodziny (po jej najtańszym) i dla wariantu w rodzinie,
                // np. I4 AQ4C ok. 40%, V8 po ok. 10%, mocniejsze odmiany (stroker, turbo) rzadziej niż podstawowe.
                if (engine && pool.Count > 0)
                {
                    var fams = pool.GroupBy(e => e.EngineFamily ?? "").Select(g => g.ToList()).ToList();
                    float cheapest = Math.Max(1, pool.Min(e => e.Price));
                    var fam = PickWeighted(fams, f => Math.Pow(cheapest / Math.Max(1f, f.Min(e => e.Price)), 2));
                    float famMin = Math.Max(1, fam.Min(e => e.Price));
                    pool = new List<MarketEntry> { PickWeighted(fam, e => Math.Pow(famMin / Math.Max(1f, e.Price), 2)) };
                }
                // Części: najpierw grupa (karoseria, zawieszenie, felgi...), waga = pierwiastek z liczby części w grupie, potem część z grupy.
                // Bez tego 155 elementów karoserii i 224 części silnika zasłaniałyby 3 opony i moduł ABS (użytkownik 2026-11-28: szeroko pojęte części).
                if (!engine && pool.Count > 0)
                {
                    var groups = pool.GroupBy(e => e.Group ?? "").ToList();
                    double total = groups.Sum(g => Math.Sqrt(g.Count())), pick = _rng.NextDouble() * total;
                    foreach (var g in groups) { pick -= Math.Sqrt(g.Count()); if (pick <= 0) { pool = g.ToList(); break; } }
                }
            }
            else
            {
                // G21: 860 dekoracji przy 51 narzędziach zalewało zakładkę groszowymi drobiazgami (karton za $1). Na OXL nikt nie wystawia
                // rzeczy za grosze: tylko przedmioty od mediany cen tej zakładki w górę (próg z danych gry), a narzędzia w połowie ogłoszeń.
                var prices = pool.Select(e => e.Price).OrderBy(p => p).ToList();
                int minWorth = prices.Count > 0 ? prices[prices.Count / 2] : 0;
                bool tool = _rng.NextDouble() < ToolShareOfToolsDecor;
                var sub = pool.Where(e => e.IsTool == tool && e.Price >= minWorth).ToList();
                if (sub.Count == 0) sub = pool.Where(e => e.Price >= minWorth).ToList();
                if (sub.Count > 0) pool = sub;
            }
            if (pool.Count == 0) return null;
            var entry = pool[_rng.Next(pool.Count)];

            var l = new ItemListing
            {
                Id = NewId(),
                EntryId = entry.Id,
                Category = entry.Category,
                Kind = entry.Kind,
                Name = entry.Name,
                NewPrice = entry.Price,
                Location = ListingSystem.Locations[_rng.Next(ListingSystem.Locations.Length)],
                PostedAt = now,
                ExpiresAt = now + (MinLifeHours + _rng.NextDouble() * (MaxLifeHours - MinLifeHours)) * 3600.0,
                IsGem = _rng.NextDouble() < GemChance,
            };
            l.Archetype = PickArchetype(l.Kind);
            if (l.Archetype == SellerArchetype.Scammer)
            {
                // Scammer przedmiotów (scammer.md 5): tylko L1 i L2, łatwy do rozpoznania: nowe konto, niska ocena, ogłoszenie wisi krótko.
                l.IsGem = false;
                // Poziom z wag scammera (L1/L2 z ustawień; L3 w przedmiotach nie ma).
                var lw = OXLSettings.SavedGenConfig.LvlWeights[3];
                l.Level = _rng.Next(Math.Max(1, lw[0] + lw[1])) < lw[0] ? 1 : 2;
                l.ExpiresAt = now + (ScamMinLifeHours + _rng.NextDouble() * (ScamMaxLifeHours - ScamMinLifeHours)) * 3600.0;
            }
            Price(l);
            l.SellerRating = l.Archetype switch { SellerArchetype.Honest => _rng.Next(4, 6), SellerArchetype.Dealer => _rng.Next(4, 6), SellerArchetype.Wrecker => _rng.Next(2, 5), _ => _rng.Next(1, 3) };
            try
            {
                var seller = _sellers?.Generate(new CarListing { Archetype = l.Archetype }, _rng);
                if (seller.HasValue) { l.SellerNick = seller.Value.nick; l.AvatarPath = seller.Value.path; }
            }
            catch (Exception ex) { OXLLog.Msg($"[OXL:ITEMS] seller profile failed: {ex.Message}"); }
            if (string.IsNullOrEmpty(l.SellerNick)) l.SellerNick = "user" + _rng.Next(1000, 9999);
            l.Story = ItemStoryBuilder.Build(l, _rng);
            l.Title = ItemStoryBuilder.Title(l.Name, l.Archetype, l.IsGem, l.HasCondition, _rng.Next(), l.Level);
            _listings.Add(l);
            OXLLog.Msg($"[OXL:ITEMS] new {l.Kind} '{l.EntryId}' {l.Archetype} L{l.Level}{(l.IsGem ? " GEM" : "")} price={l.Price} new={l.NewPrice} apparent={l.ApparentCondition:F2} actual={l.ActualCondition:F2}");
            return l;
        }

        // Scammerzy są we wszystkich kategoriach (scammer.md 5): w narzędziach i dekoracjach nie kłamią o stanie, tylko przysyłają co innego.
        private static SellerArchetype PickArchetype(OXLItemKind kind)
        {
            // Te same wagi archetypów co auta (ustawienia OXL, "Seller mix"), żeby gracz sterował liczbą scammerów wszędzie.
            var w = OXLSettings.SavedGenConfig.ArchWeights;
            if (kind == OXLItemKind.Engine) { w = (int[])w.Clone(); w[3] = (int)Math.Round(w[3] * EngineScammerKeep); }
            int total = Math.Max(1, w.Sum()), r = _rng.Next(total);
            for (int i = 0; i < 4; i++) { r -= w[i]; if (r < 0) return (SellerArchetype)i; }
            return SellerArchetype.Honest;
        }

        // Stan i cena wg archetypu (archetypes.md w skrócie dla przedmiotów). Cena względem nowego ze sklepu gry, potem mnożnik trudności.
        private static void Price(ItemListing l)
        {
            float R(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
            float factor;
            if (l.Archetype == SellerArchetype.Scammer)
            {
                // Scammer i tak przysyła najtańszy przedmiot gry (Buy), więc stan jest tylko na pokaz. L1: "nówka" za ułamek ceny, aż za pięknie;
                // L2: cena i stan bardziej wiarygodne.
                l.ActualCondition = l.HasCondition ? 0.05f : 1f;
                l.ApparentCondition = l.Level == 1 ? 1f : R(0.82f, 0.95f);
                factor = l.Level == 1 ? R(0.15f, 0.30f) : R(0.40f, 0.60f);
            }
            else if (!l.HasCondition)
            {
                l.ApparentCondition = l.ActualCondition = 1f;
                factor = l.IsGem ? R(0.20f, 0.32f) : l.Archetype switch { SellerArchetype.Honest => R(0.50f, 0.75f), SellerArchetype.Wrecker => R(0.35f, 0.55f), _ => R(0.85f, 1.15f) };
            }
            else if (l.IsGem)
            {
                l.ActualCondition = R(0.85f, 1.00f);
                l.ApparentCondition = Math.Min(1f, l.ActualCondition + R(-0.05f, 0.02f));
                factor = R(0.28f, 0.42f);
            }
            else
            {
                switch (l.Archetype)
                {
                    // Silnik: zwykle mocno używany, rzadko prawie nowy (użytkownik 2026-11-28).
                    case SellerArchetype.Honest when l.Kind == OXLItemKind.Engine: l.ActualCondition = _rng.NextDouble() < 0.10 ? R(0.88f, 0.98f) : R(0.35f, 0.85f); l.ApparentCondition = Clamp(l.ActualCondition + R(-0.03f, 0.03f)); factor = 0.30f + 0.50f * l.ActualCondition; break;
                    case SellerArchetype.Honest: l.ActualCondition = R(0.45f, 0.95f); l.ApparentCondition = Clamp(l.ActualCondition + R(-0.03f, 0.03f)); factor = 0.30f + 0.50f * l.ActualCondition; break;
                    case SellerArchetype.Wrecker: l.ActualCondition = R(0.12f, 0.50f); l.ApparentCondition = Clamp(l.ActualCondition + R(0.05f, 0.18f)); factor = 0.18f + 0.40f * l.ApparentCondition; break;
                    case SellerArchetype.Dealer: l.ActualCondition = R(0.30f, 0.70f); l.ApparentCondition = R(0.80f, 0.95f); factor = 0.55f + 0.40f * l.ApparentCondition; break;
                    default: l.ActualCondition = R(0.05f, 0.30f); l.ApparentCondition = R(0.88f, 1.00f); factor = R(0.30f, 0.45f); break;
                }
            }
            // Używane nigdy nie jest droższe niż nowe w sklepie gry (G21: banner $19 przy nowym $17): najwyżej 95% ceny nowego.
            int price = (int)Math.Min(l.NewPrice * factor * OXLSettings.PriceMultiplier, l.NewPrice * 0.95f);
            // Silnik: nie taniej niż 55% wartości w prawdziwym stanie (gra wycenia silnik na sumę części x stan i odkupuje za połowę);
            // inaczej gracz kupowałby na OXL i od razu sprzedawał grze z zyskiem. Scammer nie (i tak przysyła co innego).
            if (l.Kind == OXLItemKind.Engine && l.Archetype != SellerArchetype.Scammer) price = (int)Math.Min(Math.Max(price, l.NewPrice * l.ActualCondition * 0.55f), l.NewPrice * 0.95f);
            l.Price = Math.Max(1, price < 100 ? price : price / 5 * 5);
        }

        // Losowanie z wagami (wagi > 0); ostatni element jako zapas na błąd zaokrąglenia.
        private static T PickWeighted<T>(List<T> items, Func<T, double> weight)
        {
            double total = items.Sum(weight), r = _rng.NextDouble() * total;
            foreach (var it in items) { r -= weight(it); if (r <= 0) return it; }
            return items[items.Count - 1];
        }

        private static float Clamp(float v) => Math.Max(0.02f, Math.Min(1f, v));

        private static string NewId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        // ── Kupno ────────────────────────────────────────────────────────────────
        /// <summary>
        /// Kupno: saldo konta, paczka (od razu przez grę albo do kuriera OXL na jutro / pojutrze), pobranie z banku, mail ze zdjęciem, usunięcie ogłoszenia.
        /// Zwraca tekst dla gracza (po angielsku). ok = false: nic się nie zmieniło (ogłoszenie i pieniądze zostają).
        /// Kurier OXL nie gubi paczek: nadanie przez grę ponawia co godzinę gry, aż się uda (zapis w items.dat).
        /// </summary>
        public static bool Buy(ItemListing l, out string message)
        {
            message = "";
            if (l == null || !_listings.Contains(l)) { message = "This listing is no longer available."; return false; }
            long balance = GameBridge.GetBankAvailable();
            if (balance < 0) { message = "Payment service unavailable. Open redline.bank and try again."; return false; }
            if (balance < l.Price) { message = $"Not enough funds on your Redline account. Required ${l.Price:N0}, available ${balance:N0}. Deposit cash at redline.bank/atm."; return false; }

            // Scammer przysyła zamiast kupionej rzeczy najtańszy przedmiot gry (scammer.md 5). Gracz dowiaduje się dopiero, gdy paczka przyjdzie.
            var (sendKind, sendId, sendCond) = (l.Kind, l.EntryId, l.ActualCondition);
            string scamGot = "";   // niepuste = podmiana; nazwa tego, co naprawdę jest w paczce (komunikat przy otwarciu, G73)
            if (l.Archetype == SellerArchetype.Scammer)
            {
                var junk = MarketCatalog.Cheapest();
                if (junk != null) { (sendKind, sendId, sendCond) = (junk.Kind, junk.Id, 1f); scamGot = string.IsNullOrEmpty(junk.Name) ? junk.Id : junk.Name; OXLLog.Msg($"[OXL:SCAM] item {l.Id} '{l.EntryId}' swapped for cheapest '{junk.Id}' (${junk.Price})"); }
                else OXLPlugin.Log.Warning($"[OXL:SCAM] No cheapest item in catalog, scammer {l.Id} sends the real item.");
            }

            string eta;
            if (OXLSettings.InstantDelivery)
            {
                var r = OXLDelivery.Send(sendKind, sendId, sendCond, true, 0, l.Id);
                if (!r.Ok) { message = "The courier could not take the parcel. Nothing was charged, try again later."; OXLPlugin.Log.Warning($"[OXL:ITEMS] Buy {l.Id} '{l.EntryId}' delivery failed: {r.Error}"); return false; }
                if (scamGot.Length > 0) OXLScamParcels.Add(r.DeliveryId, l.Name, scamGot, l.SellerNick);
                eta = OXLDelivery.EtaText(r);
            }
            else
            {
                if (UnityEngine.Object.FindObjectOfType<Il2CppCMS.Managers.DeliveryManager>() == null) { message = "The courier is not available here. Nothing was charged, try again in your garage."; return false; }
                var p = ScheduleParcel(l, sendKind, sendId, sendCond);
                p.ScamGot = scamGot;
                p.Seller = l.SellerNick ?? "";
                eta = ParcelEta(p);
            }

            // Paczka przyjęta: teraz pobranie. Odmowa banku tutaj (saldo zmieniło się w ułamku sekundy) zostaje w logu, paczka już jedzie.
            string title = $"OXL: {l.Name}";
            bool paid = false;
            try { paid = BlastCoreOS.BlastCoreBankAPI.TryCharge(GameBridge.BankModId, l.Price, title, out var reason); if (!paid) OXLPlugin.Log.Warning($"[OXL:ITEMS] Bank refused {l.Price} for {l.Id} after the parcel was sent: {reason}"); }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ITEMS] Bank charge for {l.Id} failed after the parcel was sent: {ex.Message}"); }

            _listings.Remove(l);
            Save();
            string img = MarketCatalog.TryGet(l.EntryId, out var entry) ? OXLUi.ExportSprite(MarketCatalog.GetThumbnail(entry), "item_" + l.Id) : null;
            OXLMail.SendPurchase(l.Name, l.SellerNick, l.Price, eta, l.Id, img);
            OXLPlugin.Log.Msg($"[OXL:ITEMS] Bought {l.Kind} '{l.EntryId}' for {l.Price} from {l.SellerNick} ({l.Archetype}{(l.IsGem ? " GEM" : "")}), actual={l.ActualCondition:F2}, paid={paid}, {eta}");
            message = $"Bought {l.Name} for ${l.Price:N0}. {eta}. Confirmation sent to your mail.";
            return true;
        }

        // ── Kurier OXL: dostawa jutro albo pojutrze (site.md 13) ──────────────────────
        private sealed class Parcel
        {
            public OXLItemKind Kind;
            public string Id = "", EntryId = "", Name = "";   // Id = ID ogłoszenia (stały klucz paczki w kalendarzu BlastCoreOS)
            public float Condition;
            public double DueAt;        // sekundy gry: dzień i godzina dostawy
            public double NextTry;      // po nieudanym nadaniu: następna próba (co godzinę gry)
            public string ScamGot = "", Seller = "";   // podmiana od scammera: co jest w paczce naprawdę (pusta = uczciwa paczka) i kto sprzedał
        }

        private static readonly List<Parcel> _parcels = new List<Parcel>();

        /// <summary>
        /// Termin dostawy ogłoszenia (dni od zakupu 1 albo 2, godzina dostaw gry), stały dla ogłoszenia (z jego ID): strona pokazuje go przed
        /// kupnem ("tomorrow after 16:00"), a kurier dowozi dokładnie wtedy (site.md 13).
        /// </summary>
        public static void PlanDelivery(ItemListing l, out int days, out byte hour)
        {
            byte[] hours = { 8, 10, 12, 14, 16, 18 };
            try { var gh = Il2CppCMS.Managers.DeliveryManager.GetDeliveryHours(); if (gh != null && gh.Length > 0) { hours = new byte[gh.Length]; for (int i = 0; i < gh.Length; i++) hours[i] = gh[i]; } } catch { }
            int h = OXLUi.StableHash(l?.Id ?? "");
            days = 1 + h % 2;
            hour = hours[(h / 2) % hours.Length];
        }

        /// <summary>Krótki termin do ramki DELIVERY na stronie ogłoszenia: "Tomorrow after 16:00".</summary>
        public static string DeliveryWhen(ItemListing l)
        {
            if (OXLSettings.InstantDelivery) return "Right after purchase";
            PlanDelivery(l, out int days, out byte hour);
            return $"{(days == 1 ? "Tomorrow" : "The day after tomorrow")} after {hour}:00";
        }

        private static Parcel ScheduleParcel(ItemListing l, OXLItemKind kind, string entryId, float condition)
        {
            double now = GameTimeProvider.TotalGameSeconds;
            PlanDelivery(l, out int days, out byte hour);
            var p = new Parcel { Id = l.Id, Kind = kind, EntryId = entryId, Name = l.Name, Condition = condition, DueAt = (Math.Floor(now / 86400.0) + days) * 86400.0 + hour * 3600.0 };
            _parcels.Add(p);
            OXLLog.Msg($"[OXL:COURIER] '{p.EntryId}' scheduled for day +{days} at {hour}:00");
            return p;
        }

        private static string ParcelEta(Parcel p)
        {
            double now = GameTimeProvider.TotalGameSeconds;
            long days = (long)Math.Floor(p.DueAt / 86400.0) - (long)Math.Floor(now / 86400.0);
            int hour = (int)((p.DueAt % 86400.0) / 3600.0);
            string day = days <= 0 ? "today" : days == 1 ? "tomorrow" : "the day after tomorrow";
            return $"Expected {day} at {hour}:00";
        }

        // Od północy dnia dostawy paczka idzie do systemu przesyłek gry na swoją godzinę; po przespanej godzinie na najbliższą rundę.
        private static void DispatchParcels(double now)
        {
            if (_parcels.Count == 0) return;
            bool changed = false;
            foreach (var p in _parcels.ToList())
            {
                double dayStart = Math.Floor(p.DueAt / 86400.0) * 86400.0;
                if (now < dayStart || now < p.NextTry) continue;
                byte hour = now < p.DueAt ? (byte)((p.DueAt % 86400.0) / 3600.0) : (byte)0;
                var r = OXLDelivery.Send(p.Kind, p.EntryId, p.Condition, false, hour, p.Id);
                if (r.Ok) { _parcels.Remove(p); changed = true; if (p.ScamGot.Length > 0) OXLScamParcels.Add(r.DeliveryId, p.Name, p.ScamGot, p.Seller); OXLLog.Msg($"[OXL:COURIER] '{p.EntryId}' handed to the game courier for {r.Hour}:00"); }
                else { p.NextTry = now + 3600.0; changed = true; OXLPlugin.Log.Warning($"[OXL:COURIER] '{p.EntryId}' dispatch failed, retry in 1 game hour: {r.Error}"); }
            }
            if (changed) Save();
        }

        /// <summary>
        /// Paczki u kuriera OXL (kupione, jeszcze nie przekazane grze): nazwa, rodzaj i za ile sekund gry przyjdą (dzień i godzina dostawy).
        /// Dla kalendarza BlastCoreOS (prośba o API w BACKLOG) i komendy oxl_parcels. Po przekazaniu grze paczkę pokazuje już kalendarz z DeliveryManager.
        /// </summary>
        public static List<(string id, string name, OXLItemKind kind, double secondsUntil)> GetPendingParcels()
        {
            double now = GameTimeProvider.TotalGameSeconds;
            return _parcels.Select(p => (p.Id, p.Name, p.Kind, p.DueAt - now)).OrderBy(x => x.Item4).ToList();
        }

        public static void Clear() { _listings.Clear(); Save(); }

        /// <summary>
        /// Komenda oxl_end_all: kończy ogłoszenia przedmiotów (cat null = obie zakładki). Następne sprawdzenie (w kolejnej klatce) dokłada
        /// świeże do celu z ustawień, więc gracz od razu widzi nowe ogłoszenia. Paczki już kupione zostają.
        /// </summary>
        public static int EndListings(MarketCategory? cat)
        {
            int n = _listings.RemoveAll(l => cat == null || l.Category == cat);
            _lastCheck = double.NaN;
            Save();
            return n;
        }

        /// <summary>Zapis items.dat z zewnątrz (OXLScamParcels po otwarciu paczki).</summary>
        public static void SaveNow() => Save();

        public static ItemListing Find(string id) => _listings.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));

        /// <summary>Awatar sprzedawcy (cache SellerProfile, jak przy autach); null, gdy brak pliku.</summary>
        public static UnityEngine.Texture2D LoadAvatar(string path) { try { return _sellers?.GetCachedOrLoad(path); } catch { return null; } }

        // ── Zapis: Mods/CMS2026_OXL/items.dat, linia na ogłoszenie, pola rozdzielone tabulatorem, liczby InvariantCulture ──
        // Nowy format w wydaniu 2027 (czysta instalacja, rebuild.md 6). Nieznane przedmioty (po aktualizacji gry) wypadają przy wczytaniu.
        private static void Save()
        {
            if (DataPath == null) return;   // slot zapisu jeszcze nieznany: nie pisać do cudzego zapisu
            try
            {
                var sb = new StringBuilder("# OXL item listings v1\n");
                foreach (var l in _listings)
                    sb.Append(string.Join("\t", new[] { l.Id, l.EntryId, ((int)l.Category).ToString(CultureInfo.InvariantCulture), ((int)l.Kind).ToString(CultureInfo.InvariantCulture), Esc(l.Name), l.NewPrice.ToString(CultureInfo.InvariantCulture), l.Price.ToString(CultureInfo.InvariantCulture), l.ApparentCondition.ToString("R", CultureInfo.InvariantCulture), l.ActualCondition.ToString("R", CultureInfo.InvariantCulture), ((int)l.Archetype).ToString(CultureInfo.InvariantCulture), l.IsGem ? "1" : "0", Esc(l.SellerNick), Esc(l.AvatarPath), l.SellerRating.ToString(CultureInfo.InvariantCulture), Esc(l.Story), Esc(l.Location), l.PostedAt.ToString("R", CultureInfo.InvariantCulture), l.ExpiresAt.ToString("R", CultureInfo.InvariantCulture), Esc(l.Title), l.Level.ToString(CultureInfo.InvariantCulture) })).Append('\n');
                // Paczki u kuriera OXL: linie "P", żeby kupione, a jeszcze nie nadane rzeczy przetrwały wyjście z gry.
                foreach (var p in _parcels) sb.Append(string.Join("\t", new[] { "P", ((int)p.Kind).ToString(CultureInfo.InvariantCulture), p.EntryId, Esc(p.Name), p.Condition.ToString("R", CultureInfo.InvariantCulture), p.DueAt.ToString("R", CultureInfo.InvariantCulture), p.NextTry.ToString("R", CultureInfo.InvariantCulture), p.Id, Esc(p.ScamGot), Esc(p.Seller) })).Append('\n');
                // Paczki od scammera już u kuriera gry: linie "S" (komunikat przy otwarciu, G73).
                OXLScamParcels.AppendLines(sb, Esc);
                File.WriteAllText(DataPath, sb.ToString());
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ITEMS] Save failed: {ex.Message}"); }
        }

        private static void Load()
        {
            _listings.Clear();
            _parcels.Clear();
            OXLScamParcels.Clear();
            try
            {
                if (!File.Exists(DataPath)) return;
                int skipped = 0;
                foreach (var line in File.ReadAllLines(DataPath))
                {
                    if (line.Length == 0 || line[0] == '#') continue;
                    if (OXLScamParcels.TryLoadLine(line, Unesc)) continue;
                    if (line.StartsWith("P\t"))
                    {
                        // Paczka u kuriera OXL (kupiona, jeszcze nie nadana przez grę)
                        var pf = line.Split('\t');
                        try { if (pf.Length >= 7) _parcels.Add(new Parcel { Kind = (OXLItemKind)int.Parse(pf[1], CultureInfo.InvariantCulture), EntryId = pf[2], Name = Unesc(pf[3]), Condition = float.Parse(pf[4], CultureInfo.InvariantCulture), DueAt = double.Parse(pf[5], CultureInfo.InvariantCulture), NextTry = double.Parse(pf[6], CultureInfo.InvariantCulture), Id = pf.Length > 7 ? pf[7] : pf[2] + "_" + pf[5], ScamGot = pf.Length > 8 ? Unesc(pf[8]) : "", Seller = pf.Length > 9 ? Unesc(pf[9]) : "" }); }
                        catch { skipped++; }
                        continue;
                    }
                    var f = line.Split('\t');
                    if (f.Length < 18) { skipped++; continue; }
                    try
                    {
                        var inv = CultureInfo.InvariantCulture;
                        _listings.Add(new ItemListing
                        {
                            Id = f[0], EntryId = f[1], Category = (MarketCategory)int.Parse(f[2], inv), Kind = (OXLItemKind)int.Parse(f[3], inv), Name = Unesc(f[4]),
                            NewPrice = int.Parse(f[5], inv), Price = int.Parse(f[6], inv), ApparentCondition = float.Parse(f[7], inv), ActualCondition = float.Parse(f[8], inv),
                            Archetype = (SellerArchetype)int.Parse(f[9], inv), IsGem = f[10] == "1", SellerNick = Unesc(f[11]), AvatarPath = Unesc(f[12]), SellerRating = int.Parse(f[13], inv),
                            Story = Unesc(f[14]), Location = Unesc(f[15]), PostedAt = double.Parse(f[16], inv), ExpiresAt = double.Parse(f[17], inv),
                            Title = f.Length > 18 ? Unesc(f[18]) : "",
                            Level = f.Length > 19 && int.TryParse(f[19], NumberStyles.Integer, inv, out int lvl) ? lvl : 1,
                        });
                    }
                    catch { skipped++; }
                }
                OXLLog.Msg($"[OXL:ITEMS] Loaded {_listings.Count} item listings (skipped {skipped}).");
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:ITEMS] Load failed: {ex.Message}"); }
        }

        /// <summary>Po zbudowaniu katalogu: usuwa ogłoszenia przedmiotów, których gra już nie zna (np. po aktualizacji).</summary>
        public static void DropUnknown()
        {
            if (!MarketCatalog.EnsureBuilt()) return;
            int n = _listings.RemoveAll(l => !MarketCatalog.TryGet(l.EntryId, out _));
            if (n > 0) { OXLPlugin.Log.Msg($"[OXL:ITEMS] Removed {n} listings of items the game no longer has."); Save(); }
        }

        private static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "");
        private static string Unesc(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length) { char n = s[++i]; sb.Append(n == 't' ? '\t' : n == 'n' ? '\n' : n); }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }
    }
}
