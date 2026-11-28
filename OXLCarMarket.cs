using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CMS2026_OXL
{
    /// <summary>
    /// Rynek aut OXL bez UI (ZADANIE pkt 2, 2026-11-28): jeden wspólny dla wszystkich komputerów. Strona (OXLSite) i komendy oxl_* czytają stąd
    /// ogłoszenia i kupują auta. Zastępuje backend starego panelu OXLPanel (stara lista oxl.com/listings i strony oxl.com/listing/{id} usunięte).
    /// </summary>
    public sealed class OXLCarMarket
    {
        private ListingSystem _listings;

        /// <summary>Generator aut (dane gry) i profile sprzedawców; raz przy starcie moda (OXLWebPage.EnsureRegistered). Ogłoszenia zapisu gry dopiero po ReloadListingsForSlot.</summary>
        public void Build()
        {
            string modsRoot = Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources");
            _listings = new ListingSystem(new SellerProfile(modsRoot));
            _listings.ApplyConfig(OXLSettings.SavedGenConfig);
            _listings.LoadSaved();
        }

        public List<CarListing> GetActiveListings() => _listings?.ActiveListings;
        public float GetGameTime() => _listings?.GameTime ?? 0f;
        public void GenerateListings(int count) { for (int i = 0; i < count; i++) _listings?.ForceGenerate(); }
        public void ForceGenCheck() => _listings?.ForceCheckNow();
        public void SaveListings() => _listings?.Save();

        /// <summary>Slot zapisu znany: ogłoszenia aut tego zapisu (OXLSaveSlot, site.md 17).</summary>
        public void ReloadListingsForSlot() => _listings?.ReloadForSlot();

        /// <summary>Co klatkę: wygasanie i dokładanie ogłoszeń. Dopóki zapis gry nieznany, nic (inaczej ogłoszenia trafiłyby do poprzedniego zapisu).</summary>
        public void Tick(float dt)
        {
            if (_listings == null || !OXLSaveSlot.Ready) return;
            _listings.Tick(dt);
        }

        /// <summary>
        /// Kupno auta ze strony (OXLSite). Kontrakt kupna (CLAUDE.md zasada 3): najpierw parking i saldo konta,
        /// ogłoszenie znika na czas dostawy, pieniądze schodzą dopiero po udanym spawnie, przy błędzie ogłoszenie wraca (z zapisem).
        /// done(ok, tekst dla gracza) przychodzi po spawnie (kilka klatek później). false = odmowa od razu (message), nic się nie zmieniło.
        /// </summary>
        public bool TryBuyCar(CarListing listing, Action<bool, string> done, out string message)
        {
            message = "";
            if (_listings == null || listing == null || !_listings.ActiveListings.Contains(listing)) { message = "This listing is no longer available."; return false; }
            if (!GameBridge.IsVersionSpawnable(listing.InternalId, listing.CarConfig)) { message = "This car is not available in this version of the game. The listing will disappear."; return false; }
            if (!GameBridge.HasFreeSlot()) { message = "Your parking lot is full. Make room before buying a car."; return false; }
            long balance = GameBridge.GetBankAvailable();
            if (balance < 0) { message = "Payment service unavailable. Open redline.bank and try again."; return false; }
            if (balance < listing.Price) { message = $"Not enough funds on your Redline account. Required ${listing.Price:N0}, available ${balance:N0}. Deposit cash at redline.bank/atm."; return false; }

            _listings.ActiveListings.Remove(listing);
            _listings.Save();
            OXLLog.Msg($"[OXL:BUY] {listing.Make} {listing.Model} {listing.Year} cfg={listing.CarConfig} ${listing.Price} {listing.Archetype} L{listing.ArchetypeLevel} actual={listing.ActualCondition:P0}");
            GameBridge.SpawnCar(listing, result =>
            {
                if (result == GameBridge.SpawnResult.Success)
                {
                    GameBridge.ChargeForCar(listing, out _);
                    OXLMail.SendPurchase($"{listing.Make} {listing.Model} {listing.Year}", listing.SellerNick, listing.Price, "Delivered to your parking lot", listing.InternalId, MarketCatalog.TryGet(GameBridge.ToGameId(listing.InternalId) + "#" + listing.CarConfig, out var ce) ? OXLUi.ExportSprite(MarketCatalog.GetThumbnail(ce), "car_" + listing.InternalId) : null);
                    done?.Invoke(true, $"Bought {listing.Make} {listing.Model} {listing.Year} for ${listing.Price:N0}. It is waiting in your parking lot.");
                }
                else
                {
                    _listings.ActiveListings.Add(listing);
                    _listings.Save();
                    OXLPlugin.Log.Warning($"[OXL:BUY] Car delivery failed ({result}), listing restored, nothing charged.");
                    // NoFreeSlot po spawnie = gra pokazała wolne miejsce, ale żadne auta nie przyjęło: dla gracza to pełny parking.
                    done?.Invoke(false, result == GameBridge.SpawnResult.NoFreeSlot ? "Your parking lot is full. Make room before buying a car. Nothing was charged." : $"Delivery failed ({result}). The listing is back and nothing was charged.");
                }
            });
            return true;
        }

        // ── Kolor lakieru ogłoszenia (dostawa dobiera najbliższy lakier gry, GameBridge.TrySetCarColor) ──
        // "#RRGGBB" z danych gry albo paczki zdjęć 2027, na końcu nazwa koloru.
        public static Color HexColor(CarListing listing)
        {
            // Auta z danych gry (bez paczki zdjęć): kolor fabryczny wersji zapisany jako "#RRGGBB" (BlastCoreGameCarVersion.Colors).
            if (listing?.Color != null && listing.Color.StartsWith("#") && ColorUtility.TryParseHtmlString(listing.Color, out Color hex))
                return hex;
            return HexColor(listing?.Color);
        }

        public static Color HexColor(string colorName) => colorName?.ToLower() switch
    {
        "white" => new Color(0.97f, 0.97f, 0.97f),
        "nearblack" => new Color(0.08f, 0.08f, 0.08f),
        "nearblack2" => new Color(0.12f, 0.13f, 0.14f),
        "black" => new Color(0.06f, 0.06f, 0.06f),
        "red" => new Color(0.80f, 0.10f, 0.10f),
        "darkred" => new Color(0.50f, 0.05f, 0.08f),
        "maroon" => new Color(0.40f, 0.02f, 0.09f),
        "darkmaroon" => new Color(0.28f, 0.01f, 0.06f),
        "silver" => new Color(0.70f, 0.72f, 0.74f),
        "gray" => new Color(0.45f, 0.45f, 0.45f),
        "gray2" => new Color(0.59f, 0.60f, 0.63f),  // 969DA0
        "darkgray" => new Color(0.24f, 0.28f, 0.30f),  // 3D484C
        "charcoal" => new Color(0.16f, 0.18f, 0.18f),  // 292F2F
        "cyan" => new Color(0.11f, 0.83f, 0.81f),
        "lightblue" => new Color(0.03f, 0.73f, 0.85f),  // 08BBDA
        "lightblue2" => new Color(0.62f, 0.79f, 0.88f),
        "blue" => new Color(0.05f, 0.27f, 0.55f),
        "darkblue" => new Color(0.00f, 0.20f, 0.33f),  // 003253
        "navy" => new Color(0.07f, 0.17f, 0.31f),  // 132B4F
        "green" => new Color(0.10f, 0.60f, 0.20f),
        "darkgreen" => new Color(0.22f, 0.38f, 0.18f),  // 38602E
        "teal" => new Color(0.10f, 0.55f, 0.52f),
        "darkteal" => new Color(0.05f, 0.35f, 0.32f),
        "gold" => new Color(0.85f, 0.68f, 0.10f),
        "beige" => new Color(0.90f, 0.85f, 0.72f),
        "cream" => new Color(0.95f, 0.92f, 0.80f),
        "offwhite" => new Color(0.92f, 0.90f, 0.86f),
        "rust" => new Color(0.70f, 0.28f, 0.05f),
        "purple" => new Color(0.27f, 0.21f, 0.75f),  // 4536C0
        "pink" => new Color(0.90f, 0.45f, 0.65f),
        _ => new Color(0.55f, 0.55f, 0.55f),
    };

    }
}
