// ListingGenConfig.cs
namespace CMS2026_OXL
{
    /// <summary>
    /// Konfiguracja generatora ogłoszeń — tworzona z wartości draft w OXLPanel
    /// i przekazywana do ListingSystem przez ApplyConfig().
    /// </summary>
    public class ListingGenConfig
    {
        // ── Parametry generacji ───────────────────────────────────────────────
        public int MaxListings { get; set; } = 20;
        public int GenChancePct { get; set; } = 50;   // % szansy per interwał
        public int GenMin { get; set; } = 1;
        public int GenMax { get; set; } = 4;

        // ── Czas trwania — w sekundach gry (konwersja: h × SecondsPerGameHour) ─
        public float DurMinSec { get; set; } = 12f * SecondsPerGameHour;  // 43200f
        public float DurMaxSec { get; set; } = 36f * SecondsPerGameHour;  // 129600f

        // ── Wagi archetypów [0]=Honest [1]=Wrecker [2]=Dealer [3]=Scammer ──
        public int[] ArchWeights { get; set; } = { 20, 35, 30, 15 };

        // ── Wagi poziomów [arch][L1/L2/L3] ────────────────────────────────────
        public int[][] LvlWeights { get; set; } =
        {
            new[] { 40, 35, 25 },  // Honest
            new[] { 35, 35, 30 },  // Wrecker
            new[] { 30, 40, 30 },  // Dealer
            new[] { 45, 30, 25 },  // Scammer
        };

        /// <summary>
        /// 1 godzina gry = ile sekund czasu Unity.
        /// Przy 60s: domyślne 12h–36h daje 720s–2160s.
        /// </summary>
        public const float SecondsPerGameHour = 3600f;
    }
}