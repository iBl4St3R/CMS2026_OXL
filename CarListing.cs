// CarListing.cs
using System.Collections.Generic;

namespace CMS2026_OXL
{
    public enum SellerArchetype
    {
        Honest,    // A — uczciwy, cena = stan
        Wrecker, // B — zaniedbany, serwis ignorowany
        Dealer,    // C — handlarz, karoseria OK mechanika ukryta
        Scammer    // D — złomiarz, opis oderwany od rzeczywistości
    }

    [System.Flags]
    public enum FaultFlags
    {
        None = 0,
        TimingBelt = 1 << 0,
        HeadGasket = 1 << 1,
        SuspensionWorn = 1 << 2,
        BrakesGone = 1 << 3,
        ExhaustRusted = 1 << 4,
        ElectricalFault = 1 << 5,
        GlassDamage = 1 << 6,
    }

    public class CarListing
    {
        public string SellerNick { get; set; } = "";
        public string AvatarPath { get; set; } = "";

        public int ColorIndex { get; set; } = 0;
        public List<string> PhotoFiles { get; set; } = new();
        public string Make { get; set; } = "";
        public string Model { get; set; } = "";

        /// <summary>
        /// Config index for the car model (0 = default, 1+ = variant engine/body).
        /// Used for both spawning (GameBridge) and photo lookup (CarPhotoLoader).
        /// </summary>
        public int CarConfig { get; set; } = 0;

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

        public float ApparentCondition { get; set; } = 1.0f;
        public float ActualCondition { get; set; } = 1.0f;

        public SellerArchetype Archetype { get; set; } = SellerArchetype.Honest;

        /// <summary>1 = Novice/Casual/Backyard/Amateur, 2 = Experienced/Busy/Pro/Intermediate, 3 = Veteran/Hoarder/Criminal/Expert</summary>
        public int ArchetypeLevel { get; set; } = 1;

        public int SellerRating { get; set; } = 3;
        public FaultFlags Faults { get; set; } = FaultFlags.None;

        /// <summary>Uczciwa wartość rynkowa na podstawie actual condition + roku. Niezależna od archetype.</summary>
        public int FairValue { get; set; }

        /// <summary>Faktyczna kondycja karoserii/nadwozia/wnętrza po spawnowaniu. Dealer ustawia ją wysoko niezależnie od ActualCondition.</summary>
        public float BodyCondition { get; set; }

        public float Condition { get => ActualCondition; set => ActualCondition = value; }
        public string Color { get; set; } = "white";
    }
}