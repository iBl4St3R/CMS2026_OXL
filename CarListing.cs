namespace CMS2026_OXL
{
    public enum SellerArchetype
    {
        Honest,    // A — uczciwy, cena = stan
        Neglected, // B — zaniedbany, serwis ignorowany
        Dealer,    // C — handlarz, karoseria OK mechanika ukryta
        Wrecker    // D — złomiarz, opis oderwany od rzeczywistości
    }

    [System.Flags]
    public enum FaultFlags
    {
        None = 0,
        TimingBelt = 1 << 0, // pasek/łańcuch rozrządu — pułapka
        HeadGasket = 1 << 1, // uszczelka głowicy — katastrofa
        SuspensionWorn = 1 << 2, // amortyzatory + tuleje
        BrakesGone = 1 << 3, // klocki + tarcze na 0
        ExhaustRusted = 1 << 4, // tłumik
        ElectricalFault = 1 << 5, // alternator / akumulator
        GlassDamage = 1 << 6, // szyby / reflektory / lusterka
    }

    public class CarListing
    {
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";
        public int Year { get; set; }
        public int Price { get; set; }
        public string SellerNote { get; set; } = "";
        public float ExpiresAt { get; set; }
        public string InternalId { get; set; } = "";
        public string ImageFolder { get; set; } = "";
        public string Registration { get; set; } = "";
        public int Mileage { get; set; }
        public string Location { get; set; } = "";
        public int DeliveryHours { get; set; }

        // ── Kondycja ──────────────────────────────────────────────────────────
        /// <summary>Co widać w ogłoszeniu — dyktuje cenę.</summary>
        public float ApparentCondition { get; set; } = 1.0f;

        /// <summary>Stan faktyczny mechaniki — używany przez ApplyWear.</summary>
        public float ActualCondition { get; set; } = 1.0f;

        // ── Sprzedawca ────────────────────────────────────────────────────────
        public SellerArchetype Archetype { get; set; } = SellerArchetype.Honest;

        /// <summary>Ocena sprzedawcy 1–5. Generowana z archetypu + szum.</summary>
        public int SellerRating { get; set; } = 3;

        // ── Ukryte usterki ────────────────────────────────────────────────────
        public FaultFlags Faults { get; set; } = FaultFlags.None;

        // ── Alias wstecznej kompatybilności ───────────────────────────────────
        public float Condition
        {
            get => ActualCondition;
            set => ActualCondition = value;
        }
    }
}