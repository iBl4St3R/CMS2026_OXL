namespace CMS2026_OXL
{
    public enum PhotoCondition { Bad, Mid, Good }

    public static class PhotoConditionHelper
    {
        /// <summary>
        /// Zwraca folder kondycji zdjęć dla danego listingu.
        /// Dealer: zawsze 60100 (auto wypolerowane na zewnątrz).
        /// Wrecker: zawsze 60100 ale cena okazyjna — "wygląda OK".
        /// Neglected: zawsze 030 — brudne, zapuszczone.
        /// Honest: mapuje ActualCondition na prawdziwy przedział.
        /// </summary>
        public static PhotoCondition Resolve(CarListing listing)
        {
            return listing.Archetype switch
            {
                SellerArchetype.Dealer => PhotoCondition.Good,   // 60100 — wypolerowane
                SellerArchetype.Wrecker => PhotoCondition.Good,   // 60100 — kłamliwe zdjęcia
                SellerArchetype.Neglected => PhotoCondition.Bad,   // 030   — zapuszczone
                SellerArchetype.Honest => listing.ActualCondition switch
                {
                    < 0.30f => PhotoCondition.Bad,
                    < 0.60f => PhotoCondition.Mid,
                    _ => PhotoCondition.Good,
                },
                _ => PhotoCondition.Mid,
            };
        }

        public static string ToFolderName(PhotoCondition c) => c switch
        {
            PhotoCondition.Bad => "030",
            PhotoCondition.Mid => "3060",
            _ => "60100",
        };
    }
}