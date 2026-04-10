namespace CMS2026_OXL
{
    public enum Difficulty { Easy, Normal, Hard }

    public static class OXLSettings
    {
        public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;

        /// <summary>
        /// Multiplier applied to all generated listing prices.
        /// Easy  = 0.70 (cars 30% cheaper — easier profit margin)
        /// Normal = 1.00 (baseline)
        /// Hard  = 1.30 (cars 30% more expensive — tighter margins)
        /// </summary>
        public static float PriceMultiplier => CurrentDifficulty switch
        {
            Difficulty.Easy => 0.70f,
            Difficulty.Hard => 1.30f,
            _ => 1.00f,
        };

        public static void Set(Difficulty d) => CurrentDifficulty = d;
    }
}