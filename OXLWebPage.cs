using BlastCoreOS;
using CMS2026UITKFramework;
using System.Collections.Generic;

namespace CMS2026_OXL
{
    /// <summary>
    /// Registers OXL as BlastCoreOS browser pages. Backend data (ListingSystem, loaders, icon
    /// cache) lives once on a shared "backend" OXLPanel and is copied by reference into a
    /// separate, isolated OXLPanel instance per computer (keyed by ctx.Panel identity) — this
    /// is what prevents two computers with the browser open simultaneously from corrupting
    /// each other's pagination state, gallery state, and click handlers.
    /// </summary>
    public static class OXLWebPage
    {
        public const string ListingsUrl = "oxl.com/listings";
        // Zakładka Cars nowej strony (OXLSite); stare strony aut wracają tutaj zamiast do starej listy.
        public const string CarsUrl = "oxl.com/cars";

        private static bool _registered;
        private static OXLPanel _backend;
        private static readonly Dictionary<UIPanel, OXLPanel> _instances = new();

        /// <summary>Shared backend — data queries only (GetActiveListings, GetGameTime, console commands). Never build UI against this instance.</summary>
        public static OXLPanel Backend => _backend;

        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;
            _backend = new OXLPanel();
            _backend.BuildBackend();

            // Nowa strona (site.md): oxl.com i wszystkie podstrony bez dok\u0142adnej rejestracji. Stara lista aut (oxl.com/listings)
            // i stare strony og\u0142osze\u0144 aut (oxl.com/listing/{id}) zostaj\u0105 do przeniesienia aut na now\u0105 stron\u0119 (BACKLOG).
            OXLSite.Register();
            BlastCoreWebAPI.RegisterPage(new BlastCoreWebPage { Url = ListingsUrl, DisplayName = "OXL Cars \u2014 Active Listings", Build = ctx => { ctx.SetTitle?.Invoke("OXL Cars \u2014 Active Listings"); GetInstance(ctx).BuildListingsInto(ctx.ContentContainer, ctx); }, ShowInDirectory = false });
        }

        /// <summary>Returns (creating if necessary) the OXLPanel dedicated to this specific computer's browser. Each computer's UIPanel is a distinct object, so this dictionary naturally isolates state per computer.</summary>
        public static OXLPanel GetInstance(BlastCoreWebPageContext ctx)
        {
            if (!_instances.TryGetValue(ctx.Panel, out var inst) || inst == null)
            {
                inst = new OXLPanel();
                inst.AttachBackend(_backend);
                _instances[ctx.Panel] = inst;
            }
            return inst;
        }

        /// <summary>Called once per frame from OXLPlugin.OnUpdate. Ticks the shared market exactly once, then refreshes live countdown timers for every currently-known per-computer instance.</summary>
        public static void Tick(float dt)
        {
            _backend?.TickBackend(dt);
            foreach (var inst in _instances.Values) inst?.TickUI(dt);
        }
    }
}