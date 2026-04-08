namespace CMS2026_OXL
{
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
    }
}