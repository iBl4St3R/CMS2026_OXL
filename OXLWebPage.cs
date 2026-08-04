using CMS2026UITKFramework;
using UnityEngine;

namespace CMS2026_OXL
{
    /// <summary>
    /// Registers OXL as a BlastCoreOS browser page ("oxl.com") instead of its own standalone
    /// UIPanel window. OXLPanel no longer owns a UIPanel, title bar, or address bar — those are
    /// provided by whichever BlastCoreBrowserWindow currently has this page open. Backend data
    /// (ListingSystem, loaders) is initialized once via BuildBackend() and survives navigation
    /// away from and back to the page; only the visual tree is rebuilt on each Build() call.
    /// </summary>
    public static class OXLWebPage
    {
        public const string Url = "oxl.com";
        private static bool _registered;
        private static OXLPanel _panel;

        public static OXLPanel Instance => _panel;

        /// <summary>Registers the oxl.com page exactly once per process — safe to call every time a browser window is built.</summary>
        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;

            _panel = new OXLPanel();
            _panel.BuildBackend();

            BlastCoreOS.BlastCoreWebAPI.RegisterPage(new BlastCoreOS.BlastCoreWebPage
            {
                Url = Url,
                DisplayName = "OXL Auctions",
                Build = ctx => { ctx.SetTitle?.Invoke("OXL \u2014 Online eX-Owner Lies"); _panel.BuildEmbedded(ctx); },
                ShowInDirectory = true,
            });
        }

        /// <summary>Ticked every frame regardless of whether oxl.com is the currently active page in any browser — listing timers must keep running in the background.</summary>
        public static void Tick(float dt) => _panel?.TickSystem(dt);
    }
}