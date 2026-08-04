using CMS2026UITKFramework;
using UnityEngine;

namespace CMS2026_OXL
{
    /// <summary>
    /// Registers OXL as a set of BlastCoreOS browser pages ("oxl.com", "oxl.com/listings",
    /// and dynamically "oxl.com/listing/{id}" per active auction) instead of a single
    /// standalone UIPanel window. Each page rebuilds itself fresh on every Navigate/refresh —
    /// this is what makes a listing URL copy-pasteable between computers: the browser resolves
    /// the URL, calls Build(), and OXLPanel looks the listing up by id at that moment.
    /// </summary>
    public static class OXLWebPage
    {
        public const string HomeUrl = "oxl.com";
        public const string ListingsUrl = "oxl.com/listings";

        private static bool _registered;
        private static OXLPanel _panel;

        public static OXLPanel Instance => _panel;

        /// <summary>Registers the static oxl.com pages exactly once per process — safe to call every time a browser window is built.</summary>
        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;

            _panel = new OXLPanel();
            _panel.BuildBackend();

            BlastCoreOS.BlastCoreWebAPI.RegisterPage(new BlastCoreOS.BlastCoreWebPage
            {
                Url = HomeUrl,
                DisplayName = "OXL Auctions",
                Build = ctx => { ctx.SetTitle?.Invoke("OXL \u2014 Online eX-Owner Lies"); _panel.BuildHomeInto(ctx.ContentContainer, ctx); },
                ShowInDirectory = true,
            });

            BlastCoreOS.BlastCoreWebAPI.RegisterPage(new BlastCoreOS.BlastCoreWebPage
            {
                Url = ListingsUrl,
                DisplayName = "OXL \u2014 Active Listings",
                Build = ctx => { ctx.SetTitle?.Invoke("OXL \u2014 Active Listings"); _panel.BuildListingsInto(ctx.ContentContainer, ctx); },
                ShowInDirectory = false,
            });
        }

        /// <summary>Ticked every frame regardless of which page is active in any browser — listing timers and dynamic URL registration must keep running in the background.</summary>
        public static void Tick(float dt) => _panel?.TickSystem(dt);
    }
}