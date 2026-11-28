namespace CMS2026_OXL
{
    /// <summary>
    /// Rejestracja OXL w przeglądarce BlastCoreOS (strona OXLSite: oxl.com i podstrony) i wspólny rynek aut (OXLCarMarket),
    /// jeden dla wszystkich komputerów. Stary panel (oxl.com/listings, oxl.com/listing/{id}) usunięty 2026-11-28 (ZADANIE pkt 2).
    /// </summary>
    public static class OXLWebPage
    {
        // Zakładka Cars strony (OXLSite).
        public const string CarsUrl = "oxl.com/cars";

        private static bool _registered;
        private static OXLCarMarket _backend;

        /// <summary>Rynek aut: ogłoszenia, kupno, komendy konsoli.</summary>
        public static OXLCarMarket Backend => _backend;

        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;
            _backend = new OXLCarMarket();
            _backend.Build();
            OXLSite.Register();
        }

        /// <summary>Raz na klatkę z OXLPlugin.OnUpdate: wygasanie i dokładanie ogłoszeń aut.</summary>
        public static void Tick(float dt) => _backend?.Tick(dt);
    }
}
