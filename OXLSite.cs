// OXLSite.cs
// Strona OXL w przeglądarce BlastCoreOS, wygląd serwisu z ogłoszeniami rzeczy używanych (site.md 11, wzór: zrzuty OLX ss5–ss8).
// Jedna rejestracja "oxl.com" z HandlesSubpaths, routing po ctx.Path (home, cars, parts, tools, search, item/{id}, settings).
// Strona jest bezstanowa: grupa, sortowanie, cena i numer strony siedzą w adresie (wstecz/dalej działają, kilka komputerów nie miesza stanu).
// Wszystkie strony OXL (stary panel oxl.com/listings i oxl.com/listing/{id} usunięty 2026-11-28).

using System;
using System.Collections.Generic;
using System.Linq;
using BlastCoreOS;
using CMS2026UITKFramework;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class OXLSite
    {
        public const string Host = "oxl.com";

        // ── Kolory (site.md 11.5): ciemny morski pasek i tekst, jasnoszare tło, białe prostokąty, zieleń tylko w logo ──
        private static readonly Color PageBg = OXLUi.Hex("#F2F4F5");
        private static readonly Color Dark = OXLUi.Hex("#0E3338");
        private static readonly Color DarkHover = OXLUi.Hex("#1C4A50");
        private static readonly Color White = OXLUi.Hex("#FFFFFF");
        private static readonly Color RowHover = OXLUi.Hex("#F7FAFA");
        private static readonly Color Muted = OXLUi.Hex("#3E5458");
        private static readonly Color Line = OXLUi.Hex("#D8DFE0");
        private static readonly Color PhotoBg = OXLUi.Hex("#E9EEEF");
        private static readonly Color Logo = OXLUi.Hex("#3BD16F");
        private static readonly Color FeaturedBg = OXLUi.Hex("#2CE0C9");
        private static readonly Color Chip = OXLUi.Hex("#FFFFFF");
        private static readonly Color Ok = OXLUi.Hex("#E3F4EA");
        private static readonly Color OkInk = OXLUi.Hex("#1B7A42");
        private static readonly Color Bad = OXLUi.Hex("#FBE7E7");
        private static readonly Color BadInk = OXLUi.Hex("#B83232");
        private static readonly Color Badge = new Color(0.05f, 0.12f, 0.14f, 0.80f);
        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        private const float MaxContentW = 1240f, HeaderH = 64f, SearchH = 84f, Gap = 16f, RowH = 180f;
        private const int PerPage = 15;

        // Jednorazowy komunikat po akcji (kupno, wiadomość), osobno dla każdego komputera.
        private static readonly Dictionary<UIPanel, (string text, bool ok)> _flash = new Dictionary<UIPanel, (string, bool)>();

        // Okno z powodem odmowy kupna (pełny parking, brak środków), na wierzchu strony; money = przycisk do bankomatu Redline.
        private static readonly Dictionary<UIPanel, (string title, string text, bool money)> _modal = new Dictionary<UIPanel, (string, string, bool)>();

        // Które zdjęcie z paczki gracz ogląda w galerii ogłoszenia (InternalId -> indeks); przeżywa odświeżenie strony strzałką.
        private static readonly Dictionary<string, int> _photoIdx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Auto w trakcie dostawy (kupno trwa do kilku sekund, przy zajętym parkingu ok. 6 s): ogłoszenie jest wtedy zdjęte z listy, więc strona auta
        // bierze je stąd, a okno "Delivering" blokuje drugi klik (użytkownik 2026-11-28: strona nie reagowała, drugi klik dawał "no listing").
        private static readonly Dictionary<UIPanel, CarListing> _delivering = new Dictionary<UIPanel, CarListing>();
        private static readonly Dictionary<UIPanel, float> _deliveringSince = new Dictionary<UIPanel, float>();   // bezpiecznik: wynik nie przyszedł w 30 s (np. zmiana sceny) = okno znika

        private static void DrawModal(UIPanel panel, BlastCoreWebPageContext ctx, object root, float W, float pageH)
        {
            if (_delivering.ContainsKey(panel) && _deliveringSince.TryGetValue(panel, out float since) && Time.realtimeSinceStartup - since > 30f) { _delivering.Remove(panel); _deliveringSince.Remove(panel); }
            if (_delivering.ContainsKey(panel))
            {
                // Bez przycisków: zamyka je dopiero wynik dostawy (sukces albo okno z powodem odmowy). Klik w tło nic nie robi.
                var dimB = OXLUi.Box(root, 0f, 0f, W, pageH, new Color(0f, 0f, 0f, 0.5f));
                OXLUi.Clickable(panel, ctx, dimB, new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), () => { });
                float bw = Math.Min(600f, W - 40f), bx = (W - bw) / 2f;
                var bbox = OXLUi.Box(root, bx, HeaderH + 80f, bw, 150f, White, 6f);
                OXLUi.Text(panel, bbox, "Delivering your car...", 32f, 26f, bw - 64f, 36f, Dark, 22, true);
                OXLUi.Text(panel, bbox, "Please wait. The seller is bringing the car to your parking lot.", 32f, 76f, bw - 64f, 48f, Dark, 16, false, TextAnchor.UpperLeft, true);
                return;
            }
            if (!_modal.TryGetValue(panel, out var m)) return;
            _modal.Remove(panel);
            var dim = OXLUi.Box(root, 0f, 0f, W, pageH, new Color(0f, 0f, 0f, 0.5f));
            OXLUi.Clickable(panel, ctx, dim, new Color(0f, 0f, 0f, 0.5f), new Color(0f, 0f, 0f, 0.5f), () => ctx.Refresh?.Invoke());
            float mw = Math.Min(600f, W - 40f), mx = (W - mw) / 2f;
            int lines = OXLUi.Lines(m.text, mw - 64f, 16);
            float mh = 90f + lines * 24f + 100f;
            var box = OXLUi.Box(root, mx, HeaderH + 80f, mw, mh, White, 6f);
            OXLUi.Text(panel, box, m.title, 32f, 26f, mw - 64f, 36f, Dark, 22, true);
            OXLUi.Text(panel, box, m.text, 32f, 76f, mw - 64f, lines * 24f + 8f, Dark, 16, false, TextAnchor.UpperLeft, true);
            float by = mh - 72f;
            OXLUi.Button(panel, ctx, box, "OK", mw - 32f - 120f, by, 120f, 46f, Dark, DarkHover, White, 16, () => ctx.Refresh?.Invoke(), 4f);
            if (m.money) OXLUi.Button(panel, ctx, box, "Go to Redline ATM", mw - 32f - 120f - 12f - 210f, by, 210f, 46f, White, RowHover, Dark, 16, () => ctx.Navigate("redline.bank/atm"), 4f);
        }

        public static void Register()
        {
            BlastCoreWebAPI.RegisterPage(new BlastCoreWebPage
            {
                Url = Host,
                DisplayName = "OXL Marketplace",
                Description = "Used cars, engines, parts, tools and decorations from real people. Mostly honest.",
                Keywords = new[] { "cars", "used", "engine", "parts", "tools", "decorations", "auction", "marketplace" },
                HandlesSubpaths = true,
                ShowInDirectory = true,
                Icon = OXLUi.ModImage("oxlLogo.png"),
                Build = ctx => { try { Build(ctx); } catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:SITE] Build '{ctx?.Url}' failed: {ex}"); } },
            });

            // Geargle pokazuje aktywne ogłoszenia OXL w zakładce "OXL" (każde słowo zapytania musi pasować do tytułu albo parametrów).
            try { BlastCoreWebAPI.RegisterSearchProvider("oxl", SearchForGeargle); }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:SITE] Geargle provider failed: {ex.Message}"); }

            // Kalendarz BlastCoreOS pokazuje paczki u kuriera OXL (zgłoszenie użytkownika 2026-11-25). Osobna metoda: starszy BlastCoreOS
            // bez BlastCoreCalendarAPI rzuci przy jej kompilacji JIT, a wyjątek złapie try tutaj.
            try { RegisterCalendar(); }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:SITE] Calendar provider not available (BlastCoreOS too old?): {ex.Message}"); }
        }

        private static void RegisterCalendar() => BlastCoreCalendarAPI.RegisterDeliveryProvider("oxl", CalendarParcels);

        // Paczki u kuriera OXL dla kalendarza: dzień i godzina dostawy liczone od "teraz" kalendarza (ten sam zegar gry), zaokrąglone do pełnej godziny.
        // Po przekazaniu grze (północ dnia dostawy) paczka znika stąd, a kalendarz pokazuje ją z DeliveryManager.
        private static IEnumerable<BlastCoreCalendarExternalDelivery> CalendarParcels(DateTime now)
        {
            var list = new List<BlastCoreCalendarExternalDelivery>();
            foreach (var (id, name, kind, secs) in ItemListingSystem.GetPendingParcels())
            {
                DateTime at;
                try { var raw = now.AddSeconds(Math.Max(0, secs) + 1800); at = new DateTime(raw.Year, raw.Month, raw.Day, raw.Hour, 0, 0); }
                catch { continue; }
                list.Add(new BlastCoreCalendarExternalDelivery { Id = "oxl-" + id, At = at, Sender = "OXL Marketplace", Title = name, Items = new[] { name + " x1" }, Arrived = false });
            }
            return list;
        }

        private static IEnumerable<BlastCoreSearchResult> SearchForGeargle(string query)
        {
            var words = BlastCoreWebAPI.Fold(query ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return Enumerable.Empty<BlastCoreSearchResult>();
            MarketCatalog.EnsureBuilt();
            return CarRows().Concat(ItemRows(null))
                .Where(r => { string hay = BlastCoreWebAPI.Fold(r.Title + " " + r.Params + " " + r.Group + " " + r.Model); return words.All(w => hay.Contains(w)); })
                .OrderByDescending(r => r.Featured).ThenByDescending(r => r.Order).Take(30)
                .Select(r => new BlastCoreSearchResult { Title = r.Title, Url = r.Url, Snippet = $"${r.Price:N0}  |  {r.Location}  |  {r.Params}", Category = "OXL", Score = r.Featured ? 60f : 50f })
                .ToList();
        }

        // ── Router ───────────────────────────────────────────────────────────────
        private static void Build(BlastCoreWebPageContext ctx)
        {
            var panel = ctx.Panel;
            object root = ctx.ContentContainer;
            float W = ctx.Width;
            var bg = OXLUi.Box(root, 0f, 0f, W, Math.Max(ctx.Height, 2000f), PageBg);
            string path = (ctx.Path ?? "").Trim('/');
            string first = path.Split('/')[0].ToLowerInvariant();
            MarketCatalog.EnsureBuilt();

            float cw = Math.Min(MaxContentW, W - 2 * Gap);
            float cx = (W - cw) / 2f;
            Header(panel, ctx, root, W, cx, cw);
            SearchBar(panel, ctx, root, cx, cw, first == "search" ? ctx.GetQuery("q") : "");
            float y = HeaderH + SearchH;

            if (_flash.TryGetValue(panel, out var f))
            {
                _flash.Remove(panel);
                OXLUi.Box(root, cx, y, cw, 48f, f.ok ? Ok : Bad, 4f, f.ok ? OkInk : BadInk, 1f);
                OXLUi.Text(panel, root, f.text, cx + 16f, y, cw - 32f, 48f, f.ok ? OkInk : BadInk, 15, true);
                y += 48f + Gap;
            }

            switch (first)
            {
                case "cars": ctx.SetTitle?.Invoke("OXL - Cars"); y = ListPage(panel, ctx, root, cx, cw, y, "cars"); break;
                case "parts": ctx.SetTitle?.Invoke("OXL - Parts"); y = ListPage(panel, ctx, root, cx, cw, y, "parts"); break;
                case "tools": ctx.SetTitle?.Invoke("OXL - Tools & Decor"); y = ListPage(panel, ctx, root, cx, cw, y, "tools"); break;
                case "search": ctx.SetTitle?.Invoke("OXL - Search"); y = ListPage(panel, ctx, root, cx, cw, y, "search"); break;
                case "item": ctx.SetTitle?.Invoke("OXL - Listing"); y = ItemPage(panel, ctx, root, cx, cw, y, path.Length > 5 ? path.Substring(5) : ""); break;
                case "car": ctx.SetTitle?.Invoke("OXL - Car"); y = CarPage(panel, ctx, root, cx, cw, y, path.Length > 4 ? path.Substring(4) : ""); break;
                case "settings": ctx.SetTitle?.Invoke("OXL - Settings"); y = SettingsPage(panel, ctx, root, cx, cw, y); break;
                default: ctx.SetTitle?.Invoke("OXL - Online eX-Owner Lies"); y = HomePage(panel, ctx, root, cx, cw, y); break;
            }

            y = Footer(panel, ctx, root, W, cx, cw, y + 40f);
            DrawModal(panel, ctx, root, W, Math.Max(ctx.Height, y));
            S.Height(UIRuntime.GetStyle(bg), Math.Max(ctx.Height, y));
            ctx.SetContentHeight?.Invoke(y);
        }

        // ── Pasek górny i wyszukiwarka ─────────────────────────────────────────────
        private static void Header(UIPanel panel, BlastCoreWebPageContext ctx, object root, float W, float cx, float cw)
        {
            OXLUi.Box(root, 0f, 0f, W, HeaderH, Dark);
            var logo = OXLUi.Box(root, cx, 10f, 160f, 44f, Clear);
            var mark = OXLUi.ModImage("oxlWordmark.png");
            if (mark != null) OXLUi.Texture(logo, mark, 0f, 0f, 44f * mark.width / Math.Max(1, mark.height), 44f);
            else OXLUi.Text(panel, logo, "OXL", 0f, 0f, 90f, 44f, Logo, 36, true);
            OXLUi.Clickable(panel, ctx, logo, Clear, new Color(1, 1, 1, 0.06f), () => ctx.Navigate(Host));

            float x = cx + cw;
            x -= 150f; OXLUi.Button(panel, ctx, root, "Settings", x, 12f, 150f, 40f, White, OXLUi.Hex("#E6ECEC"), Dark, 15, () => ctx.Navigate($"{Host}/settings"), 4f);
            long bal = GameBridge.GetBankAvailable();
            x -= 220f; OXLUi.Text(panel, root, bal < 0 ? "Your Redline: ---" : $"Your Redline: ${bal:N0}", x, 12f, 204f, 40f, White, 14, true, TextAnchor.MiddleRight);
            foreach (var (key, label) in new[] { ("tools", "Tools & Decor"), ("parts", "Parts"), ("cars", "Cars") })
            {
                float w = 30f + label.Length * 8f;
                x -= w + 8f;
                var link = OXLUi.Box(root, x, 12f, w, 40f, Clear, 4f);
                OXLUi.Text(panel, link, label, 0f, 0f, w, 40f, White, 15, true, TextAnchor.MiddleCenter);
                string target = $"{Host}/{key}";
                OXLUi.Clickable(panel, ctx, link, Clear, new Color(1, 1, 1, 0.08f), () => ctx.Navigate(target));
            }
        }

        private static void SearchBar(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, string q)
        {
            float y = HeaderH + 20f, bw = 150f;
            var tf = OXLUi.TextField(root, cx, y, cw - bw, 48f, q, White, Dark, White);
            Action search = () => { string v = OXLUi.FieldValue(tf).Trim(); ctx.Navigate(v.Length == 0 ? Host : $"{Host}/search?q={Uri.EscapeDataString(v)}"); };
            OXLUi.OnEnter(tf, search);
            OXLUi.Button(panel, ctx, root, "Search", cx + cw - bw, y, bw, 48f, Dark, DarkHover, White, 16, search, 2f);
        }

        // ── "Check on wiki": strona AutoFreaks (wiki BlastCoreOS) w nowym oknie przeglądarki, jak przycisk w GarageOS (użytkownik 2026-11-28) ──
        private const string WikiHost = "autofreaks.org";

        /// <summary>Adres wiki dla przedmiotu: silnik (/engine), część (/part); narzędzia i dekoracje null (wiki ich nie opisuje).</summary>
        private static string WikiUrl(MarketEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.Id)) return null;
            if (e.Kind == OXLItemKind.Engine) return BlastCoreWebAPI.BuildUrl(WikiHost + "/engine", "id", e.Id);
            if (e.Kind == OXLItemKind.Part) return BlastCoreWebAPI.BuildUrl(WikiHost + "/part", "id", e.Id);
            return null;
        }

        private static void WikiButton(UIPanel panel, BlastCoreWebPageContext ctx, object box, string url, float x, float y, float w)
        {
            var ptr = OXLUi.Button(panel, ctx, box, "Check on wiki", x, y, w, 50f, White, RowHover, Dark, 17, () =>
            {
                // Nowe okno na komputerze gracza; przy limicie okien BlastCoreOS odmawia, wtedy czerwony komunikat zamiast cichej porażki.
                if (!BlastCoreWebAPI.OpenInBrowser(url)) { _flash[panel] = ("Could not open a new browser window. Close another app and try again.", false); ctx.Refresh?.Invoke(); }
            }, 4f);
            var st = UIRuntime.GetStyle(UIRuntime.WrapVE(ptr)); S.BorderWidth(st, 2f); S.BorderColor(st, Dark);
        }

        private static float Footer(UIPanel panel, BlastCoreWebPageContext ctx, object root, float W, float cx, float cw, float y)
        {
            OXLUi.Box(root, 0f, y, W, 1f, Line);
            y += 24f;
            var cols = new[] { new[] { ("Home", Host), ("Settings", $"{Host}/settings") }, new[] { ("Cars", $"{Host}/cars"), ("Parts", $"{Host}/parts"), ("Tools & Decor", $"{Host}/tools") } };
            for (int c = 0; c < cols.Length; c++)
                for (int i = 0; i < cols[c].Length; i++)
                {
                    var (label, url) = cols[c][i];
                    var link = OXLUi.Box(root, cx + c * 320f, y + i * 30f, 260f, 28f, Clear);
                    OXLUi.Text(panel, link, label, 0f, 0f, 260f, 28f, Dark, 15, true);
                    OXLUi.Clickable(panel, ctx, link, Clear, new Color(0, 0, 0, 0.04f), () => ctx.Navigate(url));
                }
            // Stopka jak w oficjalnym serwisie (użytkownik 2026-11-28: bez "porad" o kłamiących sprzedawcach): prawa i partner płatności.
            // Logo Redline z plików BlastCoreOS (bank to twarda zależność); bez pliku zostaje sam napis. Klik otwiera bank.
            float fx = cx + 680f;
            OXLUi.Text(panel, root, "Supported by", fx, y, 120f, 28f, Muted, 13);
            var logo = OXLUi.BlastCoreImage("redlineLogo.png");
            var bank = OXLUi.Box(root, fx + 108f, y - 4f, 112f, 36f, Clear, 3f);
            if (logo != null) OXLUi.Texture(bank, logo, 0f, 0f, 112f, 36f, 3f);
            else OXLUi.Text(panel, bank, "Redline Bank", 0f, 0f, 112f, 36f, Dark, 14, true);
            OXLUi.Clickable(panel, ctx, bank, Clear, new Color(0, 0, 0, 0.06f), () => ctx.Navigate("redline.bank"));
            OXLUi.Text(panel, root, "(c) OXL Marketplace. All rights reserved.", fx, y + 40f, cw - 680f, 28f, Muted, 13);
            return y + 110f;
        }

        // ── Wspólny model wiersza / karty (auto albo przedmiot) ───────────────────────
        private sealed class Row
        {
            public string Title = "", Location = "", When = "", Params = "", Url = "", Group = "";
            public int Price;
            public double Hours, Order;
            public Sprite Sprite;
            public string Photo;   // pierwsze zdjęcie z paczki (CarPhotoPack); null = zdjęcie z gry ze znaczkiem
            public bool Featured;
            public MarketCategory Category;
            // Do filtrów (site.md 12): auta: marka, model, rocznik, przebieg (mile jak w zapisie), moc, napęd; wszystko: sprzedawca, stan deklarowany
            public string Make = "", Model = "", Drive = "";
            public int Year, Miles, Hp;
            public float Stated = -1f;
        }

        // Godzina gry "06:12" i dzień: "Today at", "Yesterday at", "N days ago" (jak data dodania na OLX).
        private static string Posted(double postedSec)
        {
            double now = GameTimeProvider.TotalGameSeconds;
            long dNow = (long)Math.Floor(now / 86400.0), dPost = (long)Math.Floor(postedSec / 86400.0);
            int minOfDay = (int)((postedSec % 86400.0) / 60.0);
            string hhmm = $"{minOfDay / 60:D2}:{minOfDay % 60:D2}";
            long days = dNow - dPost;
            return days <= 0 ? $"Today at {hhmm}" : days == 1 ? $"Yesterday at {hhmm}" : $"{days} days ago";
        }

        private static string Ends(double h) => h < 1 ? "Ends within the hour" : h < 24 ? $"Ends in {(int)h}h" : $"Ends in {(int)(h / 24)}d {(int)(h % 24)}h";

        private static List<Row> CarRows()
        {
            var list = new List<Row>();
            var backend = OXLWebPage.Backend;
            var cars = backend?.GetActiveListings();
            if (cars == null) return list;
            float now = backend.GetGameTime();
            for (int i = 0; i < cars.Count; i++)
            {
                var l = cars[i];
                if (l == null) continue;
                Sprite sp = null;
                // Ogłoszenie wersji spoza katalogu (stary listings.dat, wersja psująca zapis, G48) nie trafia na stronę.
                if (!MarketCatalog.TryGet($"{GameBridge.ToGameId(l.InternalId)}#{l.CarConfig}", out var e)) continue;
                if (e != null) sp = MarketCatalog.GetThumbnail(e);
                double h = (l.ExpiresAt - now) / 3600.0;
                string ps = $"{l.Year}  |  {OXLSettings.FormatMileage(l.Mileage)}";
                int shownHp = ScammerLies.ShownHp(l, e);   // scammer może kłamać o mocy (scammer.md 3.4)
                if (shownHp > 0) ps += $"  |  {shownHp} HP";
                if (e != null && e.Drive.Length > 0) ps += $"  |  {e.Drive}";
                list.Add(new Row
                {
                    Category = MarketCategory.Cars,
                    Title = ItemStoryBuilder.CarTitle(l.Make, l.Model, l.Year, l.Archetype, OXLUi.StableHash(l.InternalId)),
                    Location = l.Location, When = Ends(h), Params = ps,
                    Price = l.Price, Hours = h, Order = i, Sprite = sp, Photo = CarPhotoPack.Photos(l).FirstOrDefault(), Url = $"{Host}/car/{l.InternalId}", Group = l.Make,
                    Featured = l.Archetype == SellerArchetype.Dealer && l.ArchetypeLevel >= 2,
                    Make = l.Make, Model = l.Model, Drive = e?.Drive ?? "", Year = l.Year, Miles = l.Mileage, Hp = shownHp, Stated = l.ApparentCondition,
                });
            }
            return list;
        }

        private static List<Row> ItemRows(MarketCategory? cat)
        {
            double now = GameTimeProvider.TotalGameSeconds;
            return ItemListingSystem.Active.Where(l => cat == null || l.Category == cat).Select(l =>
            {
                MarketCatalog.TryGet(l.EntryId, out var e);
                string group = GroupOf(l, e);
                return new Row
                {
                    Category = l.Category, Title = string.IsNullOrEmpty(l.Title) ? l.Name : l.Title, Location = l.Location, When = Posted(l.PostedAt),
                    Params = (l.HasCondition ? $"{l.Name}  |  Condition: {ItemStoryBuilder.ConditionWord(l.ApparentCondition)}" : l.Name) + (MarketCatalog.WheelText(e, l.Id) is string ws && ws.Length > 0 ? $"  |  {ws}" : ""),
                    Price = l.Price, Hours = (l.ExpiresAt - now) / 3600.0, Order = l.PostedAt, Sprite = e != null ? MarketCatalog.GetThumbnail(e) : null,
                    Url = $"{Host}/item/{l.Id}", Group = group, Featured = l.Archetype == SellerArchetype.Dealer && OXLUi.StableHash(l.Id) % 2 == 0,
                    Stated = l.HasCondition ? l.ApparentCondition : -1f,
                };
            }).ToList();
        }

        // Części: grupa z katalogu (Body, Suspension, Rims...), pasek filtrów zakładki Parts (site.md 16).
        private static string GroupOf(ItemListing l, MarketEntry e) => l.Kind == OXLItemKind.Engine ? "Engines" : l.Kind == OXLItemKind.Part ? (e != null && !string.IsNullOrEmpty(e.Group) ? e.Group : "Parts") : (e != null && e.IsTool ? "Tools" : "Decorations");

        // Wiersz listy jak na OLX: zdjęcie, tytuł sprzedawcy, miasto i data, parametry, cena po prawej.
        private static void DrawRow(UIPanel panel, BlastCoreWebPageContext ctx, object root, Row r, float x, float y, float w)
        {
            var box = OXLUi.Box(root, x, y, w, RowH, White, 4f);
            float pw = 250f, ph = RowH - 16f;
            OXLUi.Box(box, 8f, 8f, pw, ph, PhotoBg, 2f);
            if (r.Photo != null) CarPhotoPack.Draw(box, r.Photo, true, 8f, 8f, pw, ph);
            else
            {
                OXLUi.Sprite(panel, box, r.Sprite, 16f, 12f, pw - 16f, ph - 8f, Muted);
                OXLUi.Box(box, 14f, 14f, 84f, 18f, Badge, 2f);
                OXLUi.Text(panel, box, "STOCK PHOTO", 14f, 14f, 84f, 18f, White, 9, true, TextAnchor.MiddleCenter);
            }
            if (r.Featured) { OXLUi.Box(box, 8f, 8f + ph - 26f, 92f, 26f, FeaturedBg); OXLUi.Text(panel, box, "FEATURED", 8f, 8f + ph - 26f, 92f, 26f, Dark, 12, true, TextAnchor.MiddleCenter); }
            float tx = pw + 28f, tw = w - tx - 220f;
            OXLUi.Text(panel, box, r.Title, tx, 18f, tw, 62f, Dark, 20, false, TextAnchor.UpperLeft, true);
            OXLUi.Text(panel, box, $"{r.Location}  |  {r.When}", tx, 104f, tw, 22f, Muted, 14);
            OXLUi.Text(panel, box, r.Params, tx, 132f, tw, 22f, Muted, 14);
            OXLUi.Text(panel, box, $"${r.Price:N0}", w - 220f, 18f, 196f, 30f, Dark, 20, true, TextAnchor.MiddleRight);
            string url = r.Url;
            OXLUi.Clickable(panel, ctx, box, White, RowHover, () => ctx.Navigate(url));
        }

        // Karta jak "Zobacz też": zdjęcie u góry, tytuł, cena, miasto i data.
        private static void DrawCard(UIPanel panel, BlastCoreWebPageContext ctx, object root, Row r, float x, float y, float w, float h)
        {
            var box = OXLUi.Box(root, x, y, w, h, White, 4f);
            float ih = w * 0.70f;
            OXLUi.Box(box, 0f, 0f, w, ih, PhotoBg);
            if (r.Photo != null) CarPhotoPack.Draw(box, r.Photo, true, 0f, 0f, w, ih);
            else
            {
                OXLUi.Sprite(panel, box, r.Sprite, 8f, 8f, w - 16f, ih - 16f, Muted);
                OXLUi.Box(box, 8f, 8f, 84f, 18f, Badge, 2f);
                OXLUi.Text(panel, box, "STOCK PHOTO", 8f, 8f, 84f, 18f, White, 9, true, TextAnchor.MiddleCenter);
            }
            if (r.Featured) { OXLUi.Box(box, 0f, ih - 24f, 84f, 24f, FeaturedBg); OXLUi.Text(panel, box, "FEATURED", 0f, ih - 24f, 84f, 24f, Dark, 11, true, TextAnchor.MiddleCenter); }
            OXLUi.Text(panel, box, r.Title, 14f, ih + 10f, w - 28f, 44f, Dark, 15, false, TextAnchor.UpperLeft, true);
            OXLUi.Text(panel, box, $"${r.Price:N0}", 14f, ih + 58f, w - 28f, 26f, Dark, 18, true);
            OXLUi.Text(panel, box, r.Location, 14f, ih + 90f, w - 28f, 20f, Muted, 13);
            OXLUi.Text(panel, box, r.When, 14f, ih + 110f, w - 28f, 20f, Muted, 13);
            string url = r.Url;
            OXLUi.Clickable(panel, ctx, box, White, RowHover, () => ctx.Navigate(url));
        }

        private static float CardHeight(float w) => w * 0.70f + 140f;

        private static float Cards(UIPanel panel, BlastCoreWebPageContext ctx, object root, List<Row> rows, float x, float w, float y, int cols)
        {
            if (rows.Count == 0) return y;
            float cardW = (w - (cols - 1) * Gap) / cols, cardH = CardHeight(cardW);
            for (int i = 0; i < rows.Count; i++) DrawCard(panel, ctx, root, rows[i], x + (i % cols) * (cardW + Gap), y + (i / cols) * (cardH + Gap), cardW, cardH);
            return y + ((rows.Count + cols - 1) / cols) * (cardH + Gap) - Gap;
        }

        // ── Strona główna ──────────────────────────────────────────────────────────
        private static float HomePage(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, float y)
        {
            var cars = CarRows();
            var items = ItemRows(null);
            OXLUi.Text(panel, root, "Main categories", cx, y, cw, 40f, Dark, 26, true, TextAnchor.MiddleCenter);
            y += 56f;
            var cats = new[]
            {
                ("cars", "Cars", cars.Count, cars.FirstOrDefault(r => r.Sprite != null)?.Sprite),
                ("parts", "Parts", items.Count(r => r.Category == MarketCategory.EnginesParts), items.FirstOrDefault(r => r.Category == MarketCategory.EnginesParts && r.Sprite != null)?.Sprite),
                ("tools", "Tools & Decor", items.Count(r => r.Category == MarketCategory.ToolsDecor), items.FirstOrDefault(r => r.Group == "Tools" && r.Sprite != null)?.Sprite ?? items.FirstOrDefault(r => r.Category == MarketCategory.ToolsDecor && r.Sprite != null)?.Sprite),
            };
            float cellW = 220f, totalW = cats.Length * cellW, sx = cx + (cw - totalW) / 2f;
            for (int i = 0; i < cats.Length; i++)
            {
                var (key, label, count, sprite) = cats[i];
                var cell = OXLUi.Box(root, sx + i * cellW, y, cellW, 190f, Clear, 8f);
                OXLUi.Box(cell, cellW / 2f - 60f, 0f, 120f, 120f, White, 60f);
                OXLUi.Sprite(panel, cell, sprite, cellW / 2f - 44f, 16f, 88f, 88f, Muted);
                OXLUi.Text(panel, cell, label, 0f, 128f, cellW, 28f, Dark, 17, true, TextAnchor.MiddleCenter);
                OXLUi.Text(panel, cell, count == 1 ? "1 listing" : $"{count} listings", 0f, 156f, cellW, 22f, Muted, 13, false, TextAnchor.MiddleCenter);
                string target = $"{Host}/{key}";
                OXLUi.Clickable(panel, ctx, cell, Clear, new Color(0, 0, 0, 0.04f), () => ctx.Navigate(target));
            }
            y += 190f + 40f;

            var featured = items.Concat(cars).Where(r => r.Featured).OrderByDescending(r => r.Order).ToList();
            var fresh = items.OrderByDescending(r => r.Order).ToList();
            var show = featured.Concat(fresh.Where(r => !featured.Contains(r))).Take(8).ToList();
            OXLUi.Text(panel, root, "Featured and fresh listings", cx, y, cw, 36f, Dark, 24, true);
            y += 50f;
            return Cards(panel, ctx, root, show, cx, cw, y, 4);
        }

        // ── Lista (cars, parts, tools, search) z filtrami (site.md 12) ──────────────────
        // Wszystkie filtry w adresie: group (marka / typ), model, ymin, miles (maks. przebieg w milach jak w zapisie), hp (min. moc),
        // drive, cond (min. stan deklarowany w %), price, sort, page, more=1 (otwarte okno filtrów). Wszyscy sprzedawcy są prywatni (decyzja 2026-11-25).
        private static readonly string[] FilterKeys = { "group", "model", "ymin", "miles", "hp", "drive", "cond", "price" };

        private static List<Row> ApplyFilters(List<Row> rows, Dictionary<string, string> f, string kind)
        {
            IEnumerable<Row> r = rows;
            string v;
            if ((v = f["group"]).Length > 0) r = r.Where(x => string.Equals(x.Group, v, StringComparison.OrdinalIgnoreCase));
            string model = f["model"];
            if (model.Length > 0) r = r.Where(x => string.Equals(x.Model, model, StringComparison.OrdinalIgnoreCase));
            if (int.TryParse(f["ymin"], out int ymin)) r = r.Where(x => x.Year >= ymin);
            if (int.TryParse(f["miles"], out int miles)) r = r.Where(x => x.Miles <= miles);
            if (int.TryParse(f["hp"], out int hp)) r = r.Where(x => x.Hp >= hp);
            string drive = f["drive"];
            if (drive.Length > 0) r = r.Where(x => string.Equals(x.Drive, drive, StringComparison.OrdinalIgnoreCase));
            if (int.TryParse(f["cond"], out int cond)) r = r.Where(x => x.Stated >= cond / 100f);
            bool cars = kind == "cars";
            int lowMax = cars ? 8000 : 500, highMin = cars ? 20000 : 5000;
            switch (f["price"])
            {
                case "low": r = r.Where(x => x.Price < lowMax); break;
                case "mid": r = r.Where(x => x.Price >= lowMax && x.Price <= highMin); break;
                case "high": r = r.Where(x => x.Price > highMin); break;
            }
            return r.ToList();
        }

        private static (string val, string label)[] PriceOpts(string kind) => kind == "cars"
            ? new[] { ("any", "Any"), ("low", "Under $8,000"), ("mid", "$8,000 to $20,000"), ("high", "Over $20,000") }
            : new[] { ("any", "Any"), ("low", "Under $500"), ("mid", "$500 to $5,000"), ("high", "Over $5,000") };

        private static float ListPage(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, float y, string kind)
        {
            string sort = ctx.GetQuery("sort", "new"), q = ctx.GetQuery("q");
            bool more = ctx.GetQuery("more") == "1";
            int.TryParse(ctx.GetQuery("page", "1"), out int page);
            var f = FilterKeys.ToDictionary(k => k, k => ctx.GetQuery(k, k == "price" ? "any" : ""));

            List<Row> all = kind == "cars" ? CarRows() : kind == "parts" ? ItemRows(MarketCategory.EnginesParts) : kind == "tools" ? ItemRows(MarketCategory.ToolsDecor) : CarRows().Concat(ItemRows(null)).ToList();
            if (q.Length > 0) all = all.Where(r => r.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || r.Params.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            // Adres z podmienionym parametrem; zmiana filtra wraca na stronę 1. Marka kasuje model.
            Func<Dictionary<string, string>, string> build = p =>
            {
                var parts = p.Where(kv => kv.Value.Length > 0 && !(kv.Key == "sort" && kv.Value == "new") && !(kv.Key == "price" && kv.Value == "any") && !(kv.Key == "page" && kv.Value == "1")).Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value));
                string qs = string.Join("&", parts);
                return Host + "/" + kind + (qs.Length > 0 ? "?" + qs : "");
            };
            Func<string, string, bool, string> link = (key, val, keepMore) =>
            {
                var p = new Dictionary<string, string>(f);
                p["sort"] = sort; p["q"] = q; p["page"] = "1"; p["more"] = keepMore ? "1" : "";
                p[key] = val;
                if (key == "group") p["model"] = "";
                return build(p);
            };

            string catName = kind == "cars" ? "Cars" : kind == "parts" ? "Parts" : kind == "tools" ? "Tools & Decor" : "Search";
            string crumb = "Home  /  " + catName + (f["group"].Length > 0 ? "  /  " + f["group"] : "") + (f["model"].Length > 0 ? "  /  " + f["model"] : "");
            OXLUi.Text(panel, root, crumb, cx, y, cw, 24f, Muted, 13);
            y += 34f;

            // Pasek grup z liczbami (marki aut, grupy części z katalogu, Tools / Decorations), jak marki na OLX; przy wielu markach wiersze się zawijają.
            float gx = cx;
            foreach (var g in all.GroupBy(r => r.Group).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
            {
                string label = g.Key + " " + g.Count();
                float w = 20f + label.Length * 8.6f;
                if (gx + w > cx + cw) { gx = cx; y += 32f; }
                bool on = string.Equals(f["group"], g.Key, StringComparison.OrdinalIgnoreCase);
                var chip = OXLUi.Box(root, gx, y, w, 28f, Clear, 4f);
                OXLUi.Text(panel, chip, label, 0f, 0f, w, 28f, Dark, 15, on, TextAnchor.MiddleCenter);
                if (on) OXLUi.Box(chip, 6f, 26f, w - 12f, 2f, Dark);
                string target = link("group", on ? "" : g.Key, false);
                OXLUi.Clickable(panel, ctx, chip, Clear, new Color(0, 0, 0, 0.05f), () => ctx.Navigate(target));
                gx += w + 10f;
            }
            y += 48f;

            var rows = ApplyFilters(all, f, kind);
            switch (sort)
            {
                case "price_asc": rows = rows.OrderBy(r => r.Price).ToList(); break;
                case "price_desc": rows = rows.OrderByDescending(r => r.Price).ToList(); break;
                case "ending": rows = rows.OrderBy(r => r.Hours).ToList(); break;
                case "year_desc": rows = rows.OrderByDescending(r => r.Year).ToList(); break;
                case "miles_asc": rows = rows.OrderBy(r => r.Miles).ToList(); break;
                case "hp_desc": rows = rows.OrderByDescending(r => r.Hp).ToList(); break;
                default: rows = rows.OrderByDescending(r => r.Featured).ThenByDescending(r => r.Order).ToList(); break;
            }

            string found = kind == "search" ? "Results for \"" + q + "\": " + rows.Count : rows.Count == 0 ? "We found no listings" : rows.Count == 1 ? "We found 1 listing" : "We found " + rows.Count + " listings";
            OXLUi.Text(panel, root, found, cx, y, cw - 200f, 40f, Dark, 28, true);
            int active = FilterKeys.Count(k => k != "group" && f[k].Length > 0 && !(k == "price" && f[k] == "any"));
            string moreUrl = link("more", "1", true);
            OXLUi.Button(panel, ctx, root, active > 0 ? "More filters (" + active + ")" : "More filters", cx + cw - 190f, y, 190f, 40f, White, RowHover, Dark, 15, () => ctx.Navigate(moreUrl), 4f);
            y += 52f;

            // Aktywne filtry jako pigułki do usunięcia ("X").
            var activeChips = new List<(string label, string url)>();
            if (f["model"].Length > 0) activeChips.Add(("Model: " + f["model"], link("model", "", false)));
            if (f["ymin"].Length > 0) activeChips.Add(("From " + f["ymin"], link("ymin", "", false)));
            if (int.TryParse(f["miles"], out int mi)) activeChips.Add(("Up to " + OXLSettings.FormatMileage(mi), link("miles", "", false)));
            if (f["hp"].Length > 0) activeChips.Add((f["hp"] + "+ HP", link("hp", "", false)));
            if (f["drive"].Length > 0) activeChips.Add(("Drive: " + f["drive"], link("drive", "", false)));
            if (f["cond"].Length > 0) activeChips.Add(("Stated condition " + f["cond"] + "%+", link("cond", "", false)));
            if (activeChips.Count > 0)
            {
                float ax = cx;
                foreach (var (label, url) in activeChips)
                {
                    float w = 44f + label.Length * 7.8f;
                    if (ax + w > cx + cw) { ax = cx; y += 38f; }
                    var c = OXLUi.Box(root, ax, y, w, 30f, Dark, 15f);
                    OXLUi.Text(panel, c, label + "   X", 0f, 0f, w, 30f, White, 13, true, TextAnchor.MiddleCenter);
                    OXLUi.Clickable(panel, ctx, c, Dark, DarkHover, () => ctx.Navigate(url));
                    ax += w + 8f;
                }
                y += 42f;
            }

            var sortOpts = kind == "cars"
                ? new[] { ("new", "Newest"), ("price_asc", "Cheapest"), ("price_desc", "Most expensive"), ("year_desc", "Newest year"), ("miles_asc", "Lowest mileage"), ("hp_desc", "Most power"), ("ending", "Ending soon") }
                : new[] { ("new", "Newest"), ("price_asc", "Cheapest"), ("price_desc", "Most expensive"), ("ending", "Ending soon") };
            y = ChipLine(panel, ctx, root, cx, cw, y, "Sort:", sort, sortOpts, v => link("sort", v, false));
            y = ChipLine(panel, ctx, root, cx, cw, y, "Price:", f["price"], PriceOpts(kind), v => link("price", v, false));
            y += 8f;

            if (rows.Count == 0)
            {
                OXLUi.Box(root, cx, y, cw, 120f, White, 4f);
                OXLUi.Text(panel, root, "Nothing matches. Try fewer filters; new listings appear every game hour.", cx, y, cw, 120f, Muted, 16, false, TextAnchor.MiddleCenter);
                y += 120f;
            }
            else
            {
                int pages = Math.Max(1, (rows.Count + PerPage - 1) / PerPage);
                page = Math.Max(1, Math.Min(pages, page));
                foreach (var r in rows.Skip((page - 1) * PerPage).Take(PerPage)) { DrawRow(panel, ctx, root, r, cx, y, cw); y += RowH + 12f; }
                if (pages > 1)
                {
                    y += 8f;
                    float bx = cx + cw / 2f - pages * 23f;
                    for (int p = 1; p <= pages; p++)
                    {
                        var pp = new Dictionary<string, string>(f);
                        pp["sort"] = sort; pp["q"] = q; pp["page"] = p.ToString();
                        string target = build(pp);
                        bool on = p == page;
                        OXLUi.Button(panel, ctx, root, p.ToString(), bx, y, 40f, 40f, on ? Dark : White, on ? DarkHover : RowHover, on ? White : Dark, 15, () => ctx.Navigate(target), 20f);
                        bx += 46f;
                    }
                    y += 40f;
                }
            }

            if (more) y = Math.Max(y, FiltersModal(panel, ctx, root, cx, cw, kind, all, f, rows.Count, link, sort, q, build));
            return y;
        }

        // Okno "More filters" (site.md 12): na wierzchu strony, wybór zmienia adres i liczbę wyników od razu, "Show results" zamyka okno.
        private static float FiltersModal(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, string kind, List<Row> all, Dictionary<string, string> f, int count, Func<string, string, bool, string> link, string sort, string q, Func<Dictionary<string, string>, string> build)
        {
            float top = HeaderH + 20f;
            float mw = Math.Min(860f, cw), mx = cx + (cw - mw) / 2f;
            var sections = new List<(string head, string key, (string val, string label)[] opts)>();
            if (kind == "cars")
            {
                if (f["group"].Length > 0)
                {
                    string make = f["group"];
                    var models = all.Where(r => string.Equals(r.Group, make, StringComparison.OrdinalIgnoreCase)).GroupBy(r => r.Model).OrderBy(g => g.Key).Select(g => (g.Key, g.Key + " (" + g.Count() + ")")).ToList();
                    if (models.Count > 0) sections.Add(("Model", "model", new[] { ("", "Any") }.Concat(models).ToArray()));
                }
                var years = all.Where(r => r.Year > 0).Select(r => r.Year).ToList();
                if (years.Count > 0)
                {
                    var steps = new List<(string, string)> { ("", "Any") };
                    for (int yv = years.Min() / 5 * 5; yv <= years.Max(); yv += 5) steps.Add((yv.ToString(), yv + "+"));
                    sections.Add(("Year from", "ymin", steps.ToArray()));
                }
                // Przebieg w jednostkach gracza, w adresie zawsze mile (jak w zapisie ogłoszenia).
                bool km = !OXLSettings.UseMiles;
                var mileOpts = new List<(string, string)> { ("", "Any") };
                foreach (int d in new[] { 50000, 100000, 150000, 200000 }) mileOpts.Add(((km ? d * 1000 / 1609 : d).ToString(), "Up to " + (d / 1000) + "k " + (km ? "km" : "mi")));
                sections.Add(("Mileage", "miles", mileOpts.ToArray()));
                int maxHp = all.Count > 0 ? all.Max(r => r.Hp) : 0;
                if (maxHp > 0) sections.Add(("Power", "hp", new[] { ("", "Any") }.Concat(new[] { 100, 150, 200, 250, 300, 400 }.Where(h => h < maxHp).Select(h => (h.ToString(), h + "+ HP"))).ToArray()));
                var drives = all.Select(r => r.Drive).Where(d => d.Length > 0).Distinct().OrderBy(d => d).Select(d => (d, d)).ToArray();
                if (drives.Length > 1) sections.Add(("Drive", "drive", new[] { ("", "Any") }.Concat(drives).ToArray()));
            }
            if (kind != "tools") sections.Add(("Stated condition", "cond", new[] { ("", "Any"), ("50", "50%+"), ("70", "70%+"), ("85", "85%+") }));
            sections.Add(("Price", "price", PriceOpts(kind)));

            // Wysokość: nagłówek + sekcje (pigułki mogą się zawijać) + stopka.
            float inner = mw - 48f, h = 80f;
            foreach (var s in sections) h += 34f + ChipRows(s.opts, inner) * 40f + 12f;
            h += 80f;

            float pageH = Math.Max(top + h + 40f, 1400f);
            string closeUrl = link("more", "", false);
            var dim = OXLUi.Box(root, 0f, 0f, ctx.Width, pageH, new Color(0f, 0f, 0f, 0.45f));
            OXLUi.Clickable(panel, ctx, dim, new Color(0f, 0f, 0f, 0.45f), new Color(0f, 0f, 0f, 0.45f), () => ctx.Navigate(closeUrl));
            var box = OXLUi.Box(root, mx, top, mw, h, White, 6f);
            OXLUi.Text(panel, box, "Filters", 24f, 18f, inner - 60f, 36f, Dark, 24, true);
            OXLUi.Button(panel, ctx, box, "X", mw - 60f, 16f, 40f, 40f, White, RowHover, Dark, 18, () => ctx.Navigate(closeUrl), 20f);
            float yy = 80f;
            foreach (var s in sections)
            {
                OXLUi.Text(panel, box, s.head.ToUpperInvariant(), 24f, yy, inner, 26f, Muted, 12, true);
                yy += 34f;
                string key = s.key;
                yy = ChipWrap(panel, ctx, box, 24f, inner, yy, f[key], s.opts, v => link(key, v, true));
                yy += 12f;
            }
            var clear = FilterKeys.ToDictionary(k => k, k => "");
            clear["sort"] = sort; clear["q"] = q; clear["more"] = "1"; clear["group"] = f["group"];
            string clearUrl = build(clear);
            OXLUi.Button(panel, ctx, box, "Clear filters", 24f, h - 64f, 170f, 44f, White, RowHover, Dark, 15, () => ctx.Navigate(clearUrl), 4f);
            OXLUi.Button(panel, ctx, box, count == 1 ? "Show 1 result" : "Show " + count + " results", mw - 24f - 240f, h - 64f, 240f, 44f, Dark, DarkHover, White, 16, () => ctx.Navigate(closeUrl), 4f);
            return pageH;
        }

        private static int ChipRows((string val, string label)[] opts, float w)
        {
            int rows = 1; float x = 0f;
            foreach (var o in opts) { float cw = 24f + o.label.Length * 8f; if (x + cw > w) { rows++; x = 0f; } x += cw + 8f; }
            return rows;
        }

        // Pigułki wyboru w kontenerze, z zawijaniem; zwraca y pod ostatnim wierszem.
        private static float ChipWrap(UIPanel panel, BlastCoreWebPageContext ctx, object parent, float x, float w, float y, string current, (string val, string label)[] opts, Func<string, string> url)
        {
            float lx = x;
            foreach (var (val, label) in opts)
            {
                bool on = string.Equals(current ?? "", val, StringComparison.OrdinalIgnoreCase);
                float lw = 24f + label.Length * 8f;
                if (lx + lw > x + w) { lx = x; y += 40f; }
                var b = OXLUi.Box(parent, lx, y, lw, 32f, on ? Dark : White, 16f, on ? Dark : Line, 1f);
                OXLUi.Text(panel, b, label, 0f, 0f, lw, 32f, on ? White : Dark, 13, on, TextAnchor.MiddleCenter);
                string target = url(val);
                OXLUi.Clickable(panel, ctx, b, on ? Dark : White, on ? DarkHover : RowHover, () => ctx.Navigate(target));
                lx += lw + 8f;
            }
            return y + 40f;
        }

        private static float ChipLine(UIPanel panel, BlastCoreWebPageContext ctx, object root, float x, float w, float y, string head, string current, (string val, string label)[] opts, Func<string, string> url)
        {
            OXLUi.Text(panel, root, head, x, y, 60f, 32f, Muted, 14);
            return ChipWrap(panel, ctx, root, x + 64f, w - 64f, y, current, opts, url);
        }

        // ── Ogłoszenie przedmiotu ────────────────────────────────────────────────────
        private static float ItemPage(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, float y, string id)
        {
            var l = ItemListingSystem.Find(id);
            if (l == null)
            {
                OXLUi.Box(root, cx, y, cw, 160f, White, 4f);
                OXLUi.Text(panel, root, "This listing is no longer active.", cx, y + 30f, cw, 40f, Dark, 22, true, TextAnchor.MiddleCenter);
                OXLUi.Button(panel, ctx, root, "Back to OXL", cx + cw / 2f - 90f, y + 90f, 180f, 44f, Dark, DarkHover, White, 15, () => ctx.Navigate(Host), 4f);
                return y + 160f;
            }
            MarketCatalog.TryGet(l.EntryId, out var entry);
            string group = GroupOf(l, entry);
            string catKey = l.Category == MarketCategory.ToolsDecor ? "tools" : "parts";
            string catName = l.Category == MarketCategory.ToolsDecor ? "Tools & Decor" : "Parts";
            string back = $"{Host}/{catKey}";
            string title = string.IsNullOrEmpty(l.Title) ? l.Name : l.Title;
            int seed = OXLUi.StableHash(l.Id + l.SellerNick);

            OXLUi.Button(panel, ctx, root, "<  Back", cx, y, 90f, 30f, Clear, new Color(0, 0, 0, 0.05f), Dark, 16, () => ctx.Navigate(back), 4f);
            var crumbs = new[] { ("Home", Host), (catName, back), (group, $"{back}?group={group}") };
            float bx = cx + 110f;
            for (int i = 0; i < crumbs.Length; i++)
            {
                var (label, url) = crumbs[i];
                float w = 12f + label.Length * 7.6f;
                var c = OXLUi.Box(root, bx, y, w, 30f, Clear);
                OXLUi.Text(panel, c, label, 0f, 0f, w, 30f, Dark, 14, false, TextAnchor.MiddleLeft);
                OXLUi.Box(c, 0f, 23f, w - 10f, 1f, Dark);
                OXLUi.Clickable(panel, ctx, c, Clear, new Color(0, 0, 0, 0.05f), () => ctx.Navigate(url));
                bx += w + (i < crumbs.Length - 1 ? 4f : 0f);
                if (i < crumbs.Length - 1) { OXLUi.Text(panel, root, "/", bx, y, 14f, 30f, Muted, 14); bx += 16f; }
            }
            y += 46f;

            float lw = cw * 0.65f, rx = cx + lw + Gap, rw = cw - lw - Gap;

            // Lewa kolumna: zdjęcie, parametry, opis
            float ih = Math.Min(520f, lw * 0.70f);
            var photo = OXLUi.Box(root, cx, y, lw, ih + 40f, White, 4f);
            OXLUi.Box(photo, 30f, 20f, lw - 60f, ih, PhotoBg);
            OXLUi.Sprite(panel, photo, entry != null ? MarketCatalog.GetThumbnail(entry) : null, 60f, 40f, lw - 120f, ih - 40f, Muted);
            OXLUi.Box(photo, 40f, 30f, 110f, 24f, Badge, 2f);
            OXLUi.Text(panel, photo, "STOCK PHOTO", 40f, 30f, 110f, 24f, White, 11, true, TextAnchor.MiddleCenter);
            float ly = y + ih + 40f + Gap;

            var chips = new List<string> { "Private", $"Category: {group}", $"Item: {l.Name}" };
            string size = MarketCatalog.WheelText(entry, l.Id);   // opona albo felga: ten sam rozmiar przyjdzie w paczce
            if (size.Length > 0) chips.Add($"Size: {size}");
            if (l.HasCondition) chips.Add($"Condition: {ItemStoryBuilder.ConditionWord(l.ApparentCondition)} ({(int)Math.Round(l.ApparentCondition * 100f)}%)");
            chips.Add($"New in shop: ${l.NewPrice:N0}");
            int lines = OXLUi.Lines(l.Story, lw - 56f, 16);
            float chipRows = 1f;
            { float tx = 0f; foreach (var ch in chips) { float w = 30f + ch.Length * 8f; if (tx + w > lw - 56f) { chipRows++; tx = 0f; } tx += w + 10f; } }
            float dh = 28f + chipRows * 48f + 30f + 44f + lines * 24f + 28f;
            var desc = OXLUi.Box(root, cx, ly, lw, dh, White, 4f);
            float chx = 28f, chy = 28f;
            foreach (var ch in chips)
            {
                float w = 30f + ch.Length * 8f;
                if (chx + w > lw - 28f) { chx = 28f; chy += 48f; }
                OXLUi.Box(desc, chx, chy, w, 38f, Chip, 4f, Line, 1f);
                OXLUi.Text(panel, desc, ch, chx, chy, w, 38f, Dark, 15, false, TextAnchor.MiddleCenter);
                chx += w + 10f;
            }
            chy += 48f + 30f;
            OXLUi.Text(panel, desc, "Description", 28f, chy, lw - 56f, 36f, Dark, 24, true);
            OXLUi.Text(panel, desc, l.Story, 28f, chy + 44f, lw - 56f, lines * 24f + 8f, Dark, 16, false, TextAnchor.UpperLeft, true);
            ly += dh;

            // Prawa kolumna: tytuł, cena, kupno, sprzedawca, lokalizacja, prawa konsumenta
            float ry = y;
            int tlines = OXLUi.Lines(title, rw - 48f, 22);
            string wiki = WikiUrl(entry);
            float bh = 30f + 26f + tlines * 30f + 56f + 60f + 60f + 50f + (wiki != null ? 60f : 0f);
            var buyBox = OXLUi.Box(root, rx, ry, rw, bh, White, 4f);
            float by = 24f;
            OXLUi.Text(panel, buyBox, $"Posted {Posted(l.PostedAt)}", 24f, by, rw - 48f, 22f, Muted, 13); by += 30f;
            OXLUi.Text(panel, buyBox, title, 24f, by, rw - 48f, tlines * 30f, Dark, 22, false, TextAnchor.UpperLeft, true); by += tlines * 30f + 8f;
            OXLUi.Text(panel, buyBox, $"${l.Price:N0}", 24f, by, rw - 48f, 44f, Dark, 30, true); by += 56f;
            OXLUi.Button(panel, ctx, buyBox, "Buy now", 24f, by, rw - 48f, 50f, Dark, DarkHover, White, 17, () =>
            {
                bool ok = ItemListingSystem.Buy(l, out string msg);
                if (ok) { _flash[panel] = (msg, true); ctx.Navigate(back); }
                else { _modal[panel] = ("You cannot buy this item", msg, msg.Contains("Redline")); ctx.Refresh?.Invoke(); }
            }, 4f);
            by += 60f;
            var msgPtr = OXLUi.Button(panel, ctx, buyBox, "Message seller", 24f, by, rw - 48f, 50f, White, RowHover, Dark, 17, () =>
            {
                bool sent = OXLMail.SendSellerReply(l.SellerNick, title, ItemStoryBuilder.SellerReply(l.Archetype, l.IsGem, l.HasCondition, l.ApparentCondition, l.Level), l.Id);
                _flash[panel] = sent ? ($"{l.SellerNick} replied. Check your mail.", true) : ("Mail is not available right now.", false);
                ctx.Refresh?.Invoke();
            }, 4f);
            var ms = UIRuntime.GetStyle(UIRuntime.WrapVE(msgPtr)); S.BorderWidth(ms, 2f); S.BorderColor(ms, Dark);
            by += 60f;
            if (wiki != null) { WikiButton(panel, ctx, buyBox, wiki, 24f, by, rw - 48f); by += 60f; }
            OXLUi.Text(panel, buyBox, (OXLSettings.InstantDelivery ? "Instant delivery" : "Courier delivery tomorrow or the day after") + ", paid from your Redline account.", 24f, by, rw - 48f, 22f, Muted, 13);
            ry += bh + Gap;

            var seller = OXLUi.Box(root, rx, ry, rw, 170f, White, 4f);
            OXLUi.Text(panel, seller, "PRIVATE PERSON", 24f, 20f, rw - 48f, 22f, Dark, 14, true);
            var av = ItemListingSystem.LoadAvatar(l.AvatarPath);
            if (av != null) OXLUi.Texture(seller, av, 24f, 56f, 72f, 72f, 36f); else OXLUi.Box(seller, 24f, 56f, 72f, 72f, PhotoBg, 36f);
            int since = 2014 + seed % 12, lastMin = (int)((GameTimeProvider.TotalGameSeconds % 86400.0) / 60.0) - (seed % 90);
            if (lastMin < 0) lastMin = 0;
            OXLUi.Text(panel, seller, l.SellerNick, 112f, 54f, rw - 136f, 28f, Dark, 20);
            // Scammer przedmiotów ma świeże konto (scammer.md 5): kolejne zakłada po każdym zgłoszeniu.
            OXLUi.Text(panel, seller, l.Archetype == SellerArchetype.Scammer ? "Joined OXL this week" : $"On OXL since {since}", 112f, 84f, rw - 136f, 22f, Muted, 14);
            OXLUi.Text(panel, seller, $"Last online today at {lastMin / 60:D2}:{lastMin % 60:D2}", 112f, 108f, rw - 136f, 22f, Muted, 14);
            float sw = OXLUi.Stars(seller, l.SellerRating, 112f, 134f, 18f);
            OXLUi.Text(panel, seller, ((float)l.SellerRating).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " / 5", 112f + sw + 8f, 132f, rw - 136f - sw - 8f, 22f, Muted, 14);
            ry += 170f + Gap;

            // Ramka DELIVERY (użytkownik 2026-11-25): termin z planu kuriera, ten sam, z którym paczka przyjdzie po kupnie.
            var dlv = OXLUi.Box(root, rx, ry, rw, 112f, White, 4f);
            OXLUi.Text(panel, dlv, "DELIVERY", 24f, 18f, rw - 48f, 22f, Dark, 14, true);
            OXLUi.Text(panel, dlv, ItemListingSystem.DeliveryWhen(l), 24f, 44f, rw - 48f, 30f, Dark, 20, true);
            OXLUi.Text(panel, dlv, OXLSettings.InstantDelivery ? "Instant delivery is on in OXL settings." : "OXL courier, parcel to your garage delivery area.", 24f, 76f, rw - 48f, 22f, Muted, 14);
            ry += 112f + Gap;
            var loc = OXLUi.Box(root, rx, ry, rw, 170f, White, 4f);
            OXLUi.Text(panel, loc, "LOCATION", 24f, 20f, rw - 48f, 22f, Dark, 14, true);
            OXLUi.Text(panel, loc, l.Location, 24f, 60f, rw * 0.5f, 28f, Dark, 18, true);
            OXLUi.Text(panel, loc, "Parcel only, no pickup.", 24f, 90f, rw * 0.5f + 20f, 40f, Muted, 14, false, TextAnchor.UpperLeft, true);
            var map = OXLUi.ModImage("mapPH.png");
            if (map != null) OXLUi.Texture(loc, map, rw - 24f - 150f, 20f, 150f, 130f, 6f);
            ry += 170f + Gap;

            var rights = OXLUi.Box(root, rx, ry, rw, 110f, White, 4f);
            OXLUi.Text(panel, rights, "CONSUMER RIGHTS", 24f, 18f, rw - 48f, 22f, Dark, 14, true);
            OXLUi.Text(panel, rights, "Purchases from private sellers are not protected by consumer law. Read the description and ask questions before you buy.", 24f, 44f, rw - 48f, 56f, Muted, 13, false, TextAnchor.UpperLeft, true);
            ry += 110f;

            // "See also": inne ogłoszenia z tej kategorii
            float end = Math.Max(ly, ry) + 40f;
            var more = ItemRows(l.Category).Where(r => r.Url != $"{Host}/item/{l.Id}").OrderByDescending(r => r.Featured).ThenByDescending(r => r.Order).Take(4).ToList();
            if (more.Count > 0)
            {
                OXLUi.Text(panel, root, "See also", cx, end, cw, 36f, Dark, 24, true);
                end = Cards(panel, ctx, root, more, cx, cw, end + 50f, 4);
            }
            return end;
        }

        // ── Ogłoszenie auta (oxl.com/car/{InternalId}), site.md 12 ─────────────────────
        // Parametry z danych gry (wersja, silnik, moc, moment, napęd, skrzynia, waga), opis sprzedawcy z SellerNoteBuilder,
        // kupno przez OXLCarMarket.TryBuyCar (pieniądze po udanym spawnie), "Message seller" jak przy przedmiotach.
        private static float CarPage(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, float y, string id)
        {
            var backend = OXLWebPage.Backend;
            var l = backend?.GetActiveListings()?.FirstOrDefault(x => x != null && string.Equals(x.InternalId, id, StringComparison.OrdinalIgnoreCase));
            if (l == null && _delivering.TryGetValue(panel, out var dl) && string.Equals(dl.InternalId, id, StringComparison.OrdinalIgnoreCase)) l = dl;
            string back = Host + "/cars";
            if (l == null)
            {
                OXLUi.Box(root, cx, y, cw, 160f, White, 4f);
                OXLUi.Text(panel, root, "This listing is no longer active.", cx, y + 30f, cw, 40f, Dark, 22, true, TextAnchor.MiddleCenter);
                OXLUi.Button(panel, ctx, root, "Back to cars", cx + cw / 2f - 90f, y + 90f, 180f, 44f, Dark, DarkHover, White, 15, () => ctx.Navigate(back), 4f);
                return y + 160f;
            }
            MarketCatalog.TryGet(GameBridge.ToGameId(l.InternalId) + "#" + l.CarConfig, out var e);
            string title = ItemStoryBuilder.CarTitle(l.Make, l.Model, l.Year, l.Archetype, OXLUi.StableHash(l.InternalId));
            int seed = OXLUi.StableHash(l.InternalId + l.SellerNick);
            float hoursLeft = (l.ExpiresAt - backend.GetGameTime()) / 3600f;

            OXLUi.Button(panel, ctx, root, "<  Back", cx, y, 90f, 30f, Clear, new Color(0, 0, 0, 0.05f), Dark, 16, () => ctx.Navigate(back), 4f);
            var crumbs = new[] { ("Home", Host), ("Cars", back), (l.Make, back + "?group=" + Uri.EscapeDataString(l.Make)), (l.Model, back + "?group=" + Uri.EscapeDataString(l.Make) + "&model=" + Uri.EscapeDataString(l.Model)) };
            float bx = cx + 110f;
            for (int i = 0; i < crumbs.Length; i++)
            {
                var (label, url) = crumbs[i];
                float w = 12f + label.Length * 7.6f;
                var c = OXLUi.Box(root, bx, y, w, 30f, Clear);
                OXLUi.Text(panel, c, label, 0f, 0f, w, 30f, Dark, 14);
                OXLUi.Clickable(panel, ctx, c, Clear, new Color(0, 0, 0, 0.05f), () => ctx.Navigate(url));
                bx += w;
                if (i < crumbs.Length - 1) { OXLUi.Text(panel, root, "/", bx, y, 14f, 30f, Muted, 14); bx += 16f; }
            }
            y += 46f;

            float lw = cw * 0.65f, rx = cx + lw + Gap, rw = cw - lw - Gap;
            float ih = Math.Min(520f, lw * 0.70f);
            var pack = CarPhotoPack.Photos(l);
            // Pasek miniatur pod zdjęciem jak w serwisie ogłoszeń, tylko dla prawdziwych zdjęć z paczki (zdjęcie z gry jest jedno).
            const float ThumbW = 128f, ThumbH = 72f, ThumbGap = 10f;
            float stripH = pack.Count > 1 ? ThumbH + 6f + 16f : 0f;
            var photo = OXLUi.Box(root, cx, y, lw, ih + 40f + stripH, White, 4f);
            OXLUi.Box(photo, 30f, 20f, lw - 60f, ih, PhotoBg);
            if (pack.Count > 0)
            {
                // Prawdziwe zdjęcia z paczki (jedno miejsce, stan zdjęć = stan karoserii): galeria ze strzałkami, bez znaczka "Stock photo".
                int n = pack.Count;
                _photoIdx.TryGetValue(l.InternalId, out int pi);
                pi = ((pi % n) + n) % n;
                CarPhotoPack.Draw(photo, pack[pi], false, 30f, 20f, lw - 60f, ih);
                if (n > 1)
                {
                    CarPhotoPack.Prefetch(pack[(pi + 1) % n], false);
                    CarPhotoPack.Prefetch(pack[(pi - 1 + n) % n], false);
                    string lid = l.InternalId;
                    var arrowBg = new Color(1f, 1f, 1f, 0.85f);
                    OXLUi.Button(panel, ctx, photo, "<", 42f, 20f + ih / 2f - 28f, 44f, 56f, arrowBg, White, Dark, 22, () => { _photoIdx[lid] = (pi - 1 + n) % n; ctx.Refresh?.Invoke(); }, 4f);
                    OXLUi.Button(panel, ctx, photo, ">", lw - 86f, 20f + ih / 2f - 28f, 44f, 56f, arrowBg, White, Dark, 22, () => { _photoIdx[lid] = (pi + 1) % n; ctx.Refresh?.Invoke(); }, 4f);
                    OXLUi.Box(photo, lw - 122f, ih - 22f, 80f, 26f, Badge, 4f);
                    OXLUi.Text(panel, photo, $"{pi + 1} / {n}", lw - 122f, ih - 22f, 80f, 26f, White, 13, true, TextAnchor.MiddleCenter);

                    // Miniatury: kliknięcie pokazuje to zdjęcie; oglądane ma ciemną ramkę. Gdy wszystkie się nie mieszczą, okno wokół oglądanego.
                    int fit = Math.Max(1, (int)((lw - 60f + ThumbGap) / (ThumbW + 6f + ThumbGap)));
                    int first = n <= fit ? 0 : Math.Max(0, Math.Min(n - fit, pi - fit / 2));
                    float ty = 20f + ih + 12f;
                    for (int k = first; k < Math.Min(n, first + fit); k++)
                    {
                        int idx = k;
                        float tx = 30f + (k - first) * (ThumbW + 6f + ThumbGap);
                        var cell = OXLUi.Box(photo, tx, ty, ThumbW + 6f, ThumbH + 6f, Clear, 4f, k == pi ? Dark : Line, k == pi ? 3f : 1f);
                        OXLUi.Box(cell, 3f, 3f, ThumbW, ThumbH, PhotoBg);
                        CarPhotoPack.Draw(cell, pack[k], true, 3f, 3f, ThumbW, ThumbH);
                        OXLUi.Clickable(panel, ctx, cell, Clear, new Color(0f, 0f, 0f, 0.05f), () => { _photoIdx[lid] = idx; ctx.Refresh?.Invoke(); });
                    }
                }
            }
            else
            {
                OXLUi.Sprite(panel, photo, e != null ? MarketCatalog.GetThumbnail(e) : null, 60f, 40f, lw - 120f, ih - 40f, Muted);
                OXLUi.Box(photo, 40f, 30f, 110f, 24f, Badge, 2f);
                OXLUi.Text(panel, photo, "STOCK PHOTO", 40f, 30f, 110f, 24f, White, 11, true, TextAnchor.MiddleCenter);
            }
            float ly = y + ih + 40f + stripH + Gap;

            var chips = new List<string> { "Private", "Make: " + l.Make, "Model: " + l.Model };
            // Wersja, kolor, silnik i moc tak, jak napisał sprzedawca: scammer L2/L3 kłamie tu (scammer.md 3), reszta pokazuje dane gry.
            string shownVer = ScammerLies.ShownVersion(l, e), shownEng = ScammerLies.ShownEngine(l, e);
            int shownHp = ScammerLies.ShownHp(l, e);
            if (shownVer.Length > 0) chips.Add("Version: " + shownVer);
            chips.Add("Year: " + l.Year);
            chips.Add("Mileage: " + OXLSettings.FormatMileage(l.Mileage));
            chips.Add("Color: " + ScammerLies.ShownColor(l));
            if (shownEng.Length > 0) chips.Add("Engine: " + shownEng);
            if (shownHp > 0) chips.Add("Power: " + shownHp + " HP");
            if (e != null && e.TorqueNm > 0) chips.Add("Torque: " + e.TorqueNm + " Nm");
            if (e != null && e.Drive.Length > 0) chips.Add("Drive: " + e.Drive);
            if (e != null && e.Gears > 0) chips.Add("Gearbox: " + e.Gears + " gears");
            if (e != null && e.Weight > 0) chips.Add("Weight: " + e.Weight + " kg");
            chips.Add("Condition (as stated): " + ItemStoryBuilder.ConditionWord(l.ApparentCondition));
            if (!string.IsNullOrEmpty(l.Registration)) chips.Add("Plate: " + l.Registration);

            string note = System.Text.RegularExpressions.Regex.Replace(l.SellerNote ?? "", "<[^>]+>", "");
            int lines = OXLUi.Lines(note, lw - 56f, 16);
            float chipRows = 1f;
            { float tx = 0f; foreach (var ch in chips) { float w = 30f + ch.Length * 8f; if (tx + w > lw - 56f) { chipRows++; tx = 0f; } tx += w + 10f; } }
            float dh = 28f + chipRows * 48f + 30f + 44f + lines * 24f + 28f;
            var desc = OXLUi.Box(root, cx, ly, lw, dh, White, 4f);
            float chx = 28f, chy = 28f;
            foreach (var ch in chips)
            {
                float w = 30f + ch.Length * 8f;
                if (chx + w > lw - 28f) { chx = 28f; chy += 48f; }
                OXLUi.Box(desc, chx, chy, w, 38f, Chip, 4f, Line, 1f);
                OXLUi.Text(panel, desc, ch, chx, chy, w, 38f, Dark, 15, false, TextAnchor.MiddleCenter);
                chx += w + 10f;
            }
            chy += 48f + 30f;
            OXLUi.Text(panel, desc, "Description", 28f, chy, lw - 56f, 36f, Dark, 24, true);
            OXLUi.Text(panel, desc, OXLUi.ReadableOnWhite(l.SellerNote, Dark), 28f, chy + 44f, lw - 56f, lines * 24f + 8f, Dark, 16, false, TextAnchor.UpperLeft, true);
            ly += dh;

            float ry = y;
            int tlines = OXLUi.Lines(title, rw - 48f, 22);
            // Wiki AutoFreaks: prawdziwa wersja auta (gracz sprawdzi tam kłamstwa scammera o wersji i mocy).
            string wiki = BlastCoreWebAPI.BuildUrl(WikiHost + "/car", new Dictionary<string, string> { ["id"] = GameBridge.ToGameId(l.InternalId), ["v"] = l.CarConfig.ToString(System.Globalization.CultureInfo.InvariantCulture) });
            float bh = 30f + 26f + tlines * 30f + 56f + 60f + 60f + 50f + 60f;
            var buyBox = OXLUi.Box(root, rx, ry, rw, bh, White, 4f);
            float by = 24f;
            OXLUi.Text(panel, buyBox, Ends(hoursLeft), 24f, by, rw - 48f, 22f, hoursLeft < 3 ? BadInk : Muted, 13); by += 30f;
            OXLUi.Text(panel, buyBox, title, 24f, by, rw - 48f, tlines * 30f, Dark, 22, false, TextAnchor.UpperLeft, true); by += tlines * 30f + 8f;
            OXLUi.Text(panel, buyBox, "$" + l.Price.ToString("N0"), 24f, by, rw - 48f, 44f, Dark, 30, true); by += 56f;
            OXLUi.Button(panel, ctx, buyBox, "Buy now", 24f, by, rw - 48f, 50f, Dark, DarkHover, White, 17, () =>
            {
                if (_delivering.ContainsKey(panel)) return;
                bool finished = false;   // wynik przed końcem TryBuyCar (spawn bez czekania) = bez okna "Delivering"
                bool started = backend.TryBuyCar(l, (ok, msg) =>
                {
                    finished = true;
                    _delivering.Remove(panel);
                    // Sukces: lista z zielonym komunikatem. Porażka: ogłoszenie wróciło, więc zostajemy na stronie auta z oknem powodu.
                    if (ok) { _flash[panel] = (msg, true); ctx.Navigate(back); }
                    else { _modal[panel] = ("You cannot buy this car", msg, false); ctx.Refresh?.Invoke(); }
                }, out string now);
                if (!started) { _modal[panel] = ("You cannot buy this car", now, now.Contains("Redline")); ctx.Refresh?.Invoke(); }
                else if (!finished) { _delivering[panel] = l; _deliveringSince[panel] = Time.realtimeSinceStartup; ctx.Refresh?.Invoke(); }
            }, 4f);
            by += 60f;
            var msgPtr = OXLUi.Button(panel, ctx, buyBox, "Message seller", 24f, by, rw - 48f, 50f, White, RowHover, Dark, 17, () =>
            {
                bool sent = OXLMail.SendSellerReply(l.SellerNick, title, ItemStoryBuilder.SellerReply(l.Archetype, false, true, l.ApparentCondition), l.InternalId);
                _flash[panel] = sent ? (l.SellerNick + " replied. Check your mail.", true) : ("Mail is not available right now.", false);
                ctx.Refresh?.Invoke();
            }, 4f);
            var ms = UIRuntime.GetStyle(UIRuntime.WrapVE(msgPtr)); S.BorderWidth(ms, 2f); S.BorderColor(ms, Dark);
            by += 60f;
            WikiButton(panel, ctx, buyBox, wiki, 24f, by, rw - 48f); by += 60f;
            OXLUi.Text(panel, buyBox, "Delivered to your parking lot right away, paid from your Redline account.", 24f, by, rw - 48f, 40f, Muted, 13, false, TextAnchor.UpperLeft, true);
            ry += bh + Gap;

            var seller = OXLUi.Box(root, rx, ry, rw, 170f, White, 4f);
            OXLUi.Text(panel, seller, "PRIVATE PERSON", 24f, 20f, rw - 48f, 22f, Dark, 14, true);
            var av = ItemListingSystem.LoadAvatar(l.AvatarPath);
            if (av != null) OXLUi.Texture(seller, av, 24f, 56f, 72f, 72f, 36f); else OXLUi.Box(seller, 24f, 56f, 72f, 72f, PhotoBg, 36f);
            int since = 2014 + seed % 12, lastMin = (int)((GameTimeProvider.TotalGameSeconds % 86400.0) / 60.0) - (seed % 90);
            if (lastMin < 0) lastMin = 0;
            OXLUi.Text(panel, seller, l.SellerNick, 112f, 54f, rw - 136f, 28f, Dark, 20);
            OXLUi.Text(panel, seller, "On OXL since " + since, 112f, 84f, rw - 136f, 22f, Muted, 14);
            OXLUi.Text(panel, seller, "Last online today at " + (lastMin / 60).ToString("D2") + ":" + (lastMin % 60).ToString("D2"), 112f, 108f, rw - 136f, 22f, Muted, 14);
            float sw = OXLUi.Stars(seller, l.SellerRating, 112f, 134f, 18f);
            OXLUi.Text(panel, seller, ((float)l.SellerRating).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " / 5", 112f + sw + 8f, 132f, rw - 136f - sw - 8f, 22f, Muted, 14);
            ry += 170f + Gap;

            // Ramka DELIVERY (użytkownik 2026-11-25): kiedy auto będzie u gracza, osobno od lokalizacji.
            var dlv = OXLUi.Box(root, rx, ry, rw, 112f, White, 4f);
            OXLUi.Text(panel, dlv, "DELIVERY", 24f, 18f, rw - 48f, 22f, Dark, 14, true);
            OXLUi.Text(panel, dlv, "Right after purchase", 24f, 44f, rw - 48f, 30f, Dark, 20, true);
            OXLUi.Text(panel, dlv, "The seller brings it to your parking lot.", 24f, 76f, rw - 48f, 22f, Muted, 14);
            ry += 112f + Gap;
            var loc = OXLUi.Box(root, rx, ry, rw, 170f, White, 4f);
            OXLUi.Text(panel, loc, "LOCATION", 24f, 20f, rw - 48f, 22f, Dark, 14, true);
            OXLUi.Text(panel, loc, l.Location, 24f, 60f, rw * 0.5f, 28f, Dark, 18, true);
            OXLUi.Text(panel, loc, "No viewing, bought as described.", 24f, 90f, rw * 0.5f + 20f, 40f, Muted, 14, false, TextAnchor.UpperLeft, true);
            var map = OXLUi.ModImage("mapPH.png");
            if (map != null) OXLUi.Texture(loc, map, rw - 24f - 150f, 20f, 150f, 130f, 6f);
            ry += 170f + Gap;

            var rights = OXLUi.Box(root, rx, ry, rw, 110f, White, 4f);
            OXLUi.Text(panel, rights, "CONSUMER RIGHTS", 24f, 18f, rw - 48f, 22f, Dark, 14, true);
            OXLUi.Text(panel, rights, "Purchases from private sellers are not protected by consumer law. Bought as seen, no returns.", 24f, 44f, rw - 48f, 56f, Muted, 13, false, TextAnchor.UpperLeft, true);
            ry += 110f;

            float end = Math.Max(ly, ry) + 40f;
            var more = CarRows().Where(r => r.Url != Host + "/car/" + l.InternalId).OrderByDescending(r => r.Make == l.Make).ThenByDescending(r => r.Featured).Take(4).ToList();
            if (more.Count > 0)
            {
                OXLUi.Text(panel, root, "See also", cx, end, cw, 36f, Dark, 24, true);
                end = Cards(panel, ctx, root, more, cx, cw, end + 50f, 4);
            }
            return end;
        }

        // Nazwa koloru dla gracza z koloru fabrycznego ("#7A745F" nic nie mówi): odcień, nasycenie i jasność na kilka prostych nazw.
        internal static string ColorName(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            if (v < 0.18f) return "Black";
            if (s < 0.12f) return v > 0.85f ? "White" : v > 0.55f ? "Silver" : "Gray";
            if (s < 0.35f && v > 0.55f && (h < 0.17f)) return "Beige";
            float deg = h * 360f;
            if (deg < 15f || deg >= 340f) return v < 0.45f ? "Dark red" : "Red";
            if (deg < 40f) return v < 0.5f ? "Brown" : "Orange";
            if (deg < 65f) return "Yellow";
            if (deg < 165f) return v < 0.45f ? "Dark green" : "Green";
            if (deg < 200f) return "Teal";
            if (deg < 255f) return v < 0.45f ? "Dark blue" : "Blue";
            if (deg < 300f) return "Purple";
            return "Pink";
        }

        // ── Ustawienia (oxl.com/settings, site.md 9.7) ──────────────────────────────
        private static float SettingsPage(UIPanel panel, BlastCoreWebPageContext ctx, object root, float cx, float cw, float y)
        {
            OXLUi.Text(panel, root, "Settings", cx, y, cw, 40f, Dark, 28, true);
            y += 56f;
            float w = Math.Min(800f, cw);
            y = Option(panel, ctx, root, cx, w, y, "Delivery of parts, engines, tools and decorations", "Cars are always delivered to your parking lot right away.",
                new[] { ("Courier: tomorrow or the day after", !OXLSettings.InstantDelivery, (Action)(() => OXLSettings.SetInstantDelivery(false))), ("Instant delivery", OXLSettings.InstantDelivery, (Action)(() => OXLSettings.SetInstantDelivery(true))) });
            y = Option(panel, ctx, root, cx, w, y, "Distance units", "Used for mileage in car listings.",
                new[] { ("Miles", OXLSettings.UseMiles, (Action)(() => OXLSettings.SetUseMiles(true))), ("Kilometers", !OXLSettings.UseMiles, (Action)(() => OXLSettings.SetUseMiles(false))) });
            y = Option(panel, ctx, root, cx, w, y, "Prices", "Easy: listings 15% cheaper. Hard: 20% more expensive. Applies to new listings.",
                new[] { ("Easy", OXLSettings.CurrentDifficulty == Difficulty.Easy, (Action)(() => OXLSettings.Set(Difficulty.Easy))), ("Normal", OXLSettings.CurrentDifficulty == Difficulty.Normal, (Action)(() => OXLSettings.Set(Difficulty.Normal))), ("Hard", OXLSettings.CurrentDifficulty == Difficulty.Hard, (Action)(() => OXLSettings.Set(Difficulty.Hard))) });

            // Liczba aukcji i mieszanka sprzedawców (użytkownik 2026-11-26, jak w starym OXL): ten sam ListingGenConfig, który czyta generator,
            // więc zmiana działa od razu; zapis do CMS2026_OXL.cfg (lg_*).
            var gc = OXLSettings.SavedGenConfig;
            y = Steppers(panel, ctx, root, cx, w, y, "Listings on OXL", "The market keeps this many active listings in each category.", new[]
            {
                ("Cars", (Func<int>)(() => gc.MaxListings), (Action<int>)(v => gc.MaxListings = Math.Max(1, Math.Min(50, v))), 1),
                ("Parts", () => gc.TargetParts, v => gc.TargetParts = Math.Max(0, Math.Min(40, v)), 1),
                ("Tools and decorations", () => gc.TargetDecor, v => gc.TargetDecor = Math.Max(0, Math.Min(40, v)), 1),
            });
            string[] archNames = { "Honest", "Wrecker", "Dealer", "Scammer" };
            y = Shares(panel, ctx, root, cx, w, y, "mix", "Seller mix", "Percent of each kind of seller (cars and items). Always 100% in total; Lock keeps a value fixed.", archNames, gc.ArchWeights);
            for (int a = 0; a < 4; a++)
            {
                string hint = a == 3 ? "Level 1 is obvious, level 3 lies about specs. Items only have levels 1 and 2." : "Percent of seller experience levels. Always 100% in total.";
                y = Shares(panel, ctx, root, cx, w, y, "lvl" + a, archNames[a] + " levels", hint, new[] { "Level 1", "Level 2", "Level 3" }, gc.LvlWeights[a]);
            }
            return y;
        }

        // Zablokowane pozycje udziałów ("grupa:indeks"); tylko na czas sesji gry.
        private static readonly HashSet<string> _shareLocks = new HashSet<string>();

        /// <summary>
        /// Udziały procentowe sumujące się do 100 (użytkownik 2026-11-26): "+5" w jednej pozycji zabiera 5 z innych niezablokowanych, po 1 punkcie
        /// od największej, żeby nic nie zeszło poniżej 0. Kłódka ("Lock") wyłącza pozycję z oddawania i z własnej zmiany.
        /// </summary>
        private static float Shares(UIPanel panel, BlastCoreWebPageContext ctx, object root, float x, float w, float y, string group, string title, string hint, string[] labels, int[] values)
        {
            Normalize(values);
            float h = 80f + labels.Length * 48f + 10f;
            var box = OXLUi.Box(root, x, y, w, h, White, 4f);
            OXLUi.Text(panel, box, title, 24f, 16f, w - 48f, 28f, Dark, 18, true);
            OXLUi.Text(panel, box, hint, 24f, 44f, w - 48f, 22f, Muted, 14);
            float ry = 80f;
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                string key = group + ":" + i;
                bool locked = _shareLocks.Contains(key);
                OXLUi.Text(panel, box, labels[i], 24f, ry, 260f, 38f, locked ? Muted : Dark, 15);
                if (!locked)
                {
                    OXLUi.Button(panel, ctx, box, "-", 300f, ry, 44f, 38f, White, RowHover, Dark, 18, () => { Shift(values, idx, -5, group); OXLSettings.SaveGenConfig(OXLSettings.SavedGenConfig); ctx.Refresh?.Invoke(); }, 4f);
                    OXLUi.Button(panel, ctx, box, "+", 416f, ry, 44f, 38f, White, RowHover, Dark, 18, () => { Shift(values, idx, 5, group); OXLSettings.SaveGenConfig(OXLSettings.SavedGenConfig); ctx.Refresh?.Invoke(); }, 4f);
                }
                OXLUi.Text(panel, box, values[i] + "%", 350f, ry, 60f, 38f, Dark, 16, true, TextAnchor.MiddleCenter);
                OXLUi.Button(panel, ctx, box, locked ? "Locked" : "Lock", 476f, ry, 90f, 38f, locked ? Dark : White, locked ? DarkHover : RowHover, locked ? White : Dark, 14, () => { if (!_shareLocks.Remove(key)) _shareLocks.Add(key); ctx.Refresh?.Invoke(); }, 4f);
                ry += 48f;
            }
            return y + h + Gap;
        }

        // Zmiana o delta z oddaniem / zabraniem tej samej sumy z niezablokowanych pozycji; gdy nie ma skąd wziąć, zmienia się tyle, ile się da.
        private static void Shift(int[] v, int idx, int delta, string group)
        {
            var others = Enumerable.Range(0, v.Length).Where(i => i != idx && !_shareLocks.Contains(group + ":" + i)).ToList();
            int sign = Math.Sign(delta);
            for (int n = 0; n < Math.Abs(delta); n++)
            {
                if (v[idx] + sign < 0 || v[idx] + sign > 100) break;
                // +: zabieramy od największej (> 0); -: oddajemy najmniejszej
                var pick = sign > 0 ? others.Where(i => v[i] > 0).OrderByDescending(i => v[i]).FirstOrDefault(-1) : others.OrderBy(i => v[i]).FirstOrDefault(-1);
                if (pick < 0) break;
                v[idx] += sign;
                v[pick] -= sign;
            }
        }

        // Stare wartości z cfg (wagi względne, suma różna od 100): przeliczenie na procenty, reszta do największej pozycji.
        private static void Normalize(int[] v)
        {
            int sum = v.Sum();
            if (sum == 100) return;
            if (sum <= 0) { for (int i = 0; i < v.Length; i++) v[i] = i == 0 ? 100 : 0; return; }
            for (int i = 0; i < v.Length; i++) v[i] = v[i] * 100 / sum;
            int max = Array.IndexOf(v, v.Max());
            v[max] += 100 - v.Sum();
        }

        // Pole z liczbami: nazwa, "-", wartość, "+". Każda zmiana zapisuje ustawienia i odświeża stronę.
        private static float Steppers(UIPanel panel, BlastCoreWebPageContext ctx, object root, float x, float w, float y, string title, string hint, (string label, Func<int> get, Action<int> set, int step)[] rows)
        {
            float h = 80f + rows.Length * 48f + 10f;
            var box = OXLUi.Box(root, x, y, w, h, White, 4f);
            OXLUi.Text(panel, box, title, 24f, 16f, w - 48f, 28f, Dark, 18, true);
            OXLUi.Text(panel, box, hint, 24f, 44f, w - 48f, 22f, Muted, 14);
            float ry = 80f;
            foreach (var r in rows)
            {
                var row = r;
                OXLUi.Text(panel, box, row.label, 24f, ry, 260f, 38f, Dark, 15);
                OXLUi.Button(panel, ctx, box, "-", 300f, ry, 44f, 38f, White, RowHover, Dark, 18, () => { row.set(row.get() - row.step); OXLSettings.SaveGenConfig(OXLSettings.SavedGenConfig); ctx.Refresh?.Invoke(); }, 4f);
                OXLUi.Text(panel, box, row.get().ToString(System.Globalization.CultureInfo.InvariantCulture), 350f, ry, 60f, 38f, Dark, 16, true, TextAnchor.MiddleCenter);
                OXLUi.Button(panel, ctx, box, "+", 416f, ry, 44f, 38f, White, RowHover, Dark, 18, () => { row.set(row.get() + row.step); OXLSettings.SaveGenConfig(OXLSettings.SavedGenConfig); ctx.Refresh?.Invoke(); }, 4f);
                ry += 48f;
            }
            return y + h + Gap;
        }

        private static float Option(UIPanel panel, BlastCoreWebPageContext ctx, object root, float x, float w, float y, string title, string hint, (string label, bool on, Action set)[] opts)
        {
            var box = OXLUi.Box(root, x, y, w, 136f, White, 4f);
            OXLUi.Text(panel, box, title, 24f, 16f, w - 48f, 28f, Dark, 18, true);
            OXLUi.Text(panel, box, hint, 24f, 44f, w - 48f, 22f, Muted, 14);
            float bx = 24f;
            foreach (var o in opts)
            {
                float bw = 40f + o.label.Length * 8.4f;
                var set = o.set;
                var ptr = OXLUi.Button(panel, ctx, box, o.label, bx, 80f, bw, 40f, o.on ? Dark : White, o.on ? DarkHover : RowHover, o.on ? White : Dark, 14, () => { set(); ctx.Refresh?.Invoke(); }, 4f, o.on);
                if (!o.on) { var st = UIRuntime.GetStyle(UIRuntime.WrapVE(ptr)); S.BorderWidth(st, 1f); S.BorderColor(st, Dark); }
                bx += bw + 10f;
            }
            return y + 136f + Gap;
        }
    }
}
