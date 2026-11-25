// ItemListing.cs
// Ogłoszenie przedmiotu (zakładki Engines & Parts i Tools & Decor, site.md 2). Osobny model od CarListing: inne dane i inna dostawa (paczka zamiast spawnu).

namespace CMS2026_OXL
{
    public sealed class ItemListing
    {
        public string Id = "";              // krótkie ID ogłoszenia (adres oxl.com/item/{Id})
        public string EntryId = "";         // ID przedmiotu w grze (MarketEntry.Id)
        public MarketCategory Category;
        public OXLItemKind Kind;
        public string Name = "";                // nazwa przedmiotu z gry (parametry ogłoszenia)
        public string Title = "";               // tytuł pisany przez sprzedawcę (site.md 11.1)
        public int NewPrice;                // cena nowego w sklepie gry (dla porównania na stronie)
        public int Price;                   // cena w ogłoszeniu
        public float ApparentCondition;     // stan deklarowany przez sprzedawcę (0..1)
        public float ActualCondition;       // stan rzeczywisty po dostawie (0..1); narzędzia i dekoracje: 1
        public SellerArchetype Archetype;
        public int Level = 1;               // poziom sprzedawcy; przedmioty mają scammerów tylko L1 i L2 (scammer.md 5)
        public bool IsGem;                  // rzadka perełka: dobry stan, niska cena
        public string SellerNick = "";
        public string AvatarPath = "";
        public int SellerRating;            // 1..5
        public string Story = "";           // opis sprzedawcy (po angielsku, bez myślników)
        public string Location = "";
        public double PostedAt;             // sekundy gry (GameTimeProvider.TotalGameSeconds)
        public double ExpiresAt;

        /// <summary>Przedmioty bez stanu w grze (narzędzia, dekoracje): strona nie pokazuje procentu.</summary>
        public bool HasCondition => Kind != OXLItemKind.Customizable;
    }
}
