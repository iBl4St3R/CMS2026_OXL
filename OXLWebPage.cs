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
        public const string HomeUrl = "oxl.com";
        public const string ListingsUrl = "oxl.com/listings";

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

            BlastCoreWebAPI.RegisterPage(new BlastCoreWebPage { Url = HomeUrl, DisplayName = "OXL Auctions", Build = ctx => { ctx.SetTitle?.Invoke("OXL \u2014 Online eX-Owner Lies"); GetInstance(ctx).BuildHomeInto(ctx.ContentContainer, ctx); }, ShowInDirectory = true });
            BlastCoreWebAPI.RegisterPage(new BlastCoreWebPage { Url = ListingsUrl, DisplayName = "OXL \u2014 Active Listings", Build = ctx => { ctx.SetTitle?.Invoke("OXL \u2014 Active Listings"); GetInstance(ctx).BuildListingsInto(ctx.ContentContainer, ctx); }, ShowInDirectory = false });
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