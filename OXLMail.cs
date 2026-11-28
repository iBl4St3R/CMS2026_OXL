// OXLMail.cs
// Mail po każdym kupnie (site.md 9.6): co, od kogo, za ile, kiedy dotrze. Poczta BlastCoreOS trzyma historię, więc OXL nie ma "My purchases".

using System;

namespace CMS2026_OXL
{
    public static class OXLMail
    {
        public const string SenderId = "oxl";
        private static bool _registered;

        /// <summary>Rejestruje nadawcę "OXL Marketplace" (logo z Mods/CMS2026_OXL/Resources/Images/logo.png). Wołane przy wejściu do garażu.</summary>
        public static void Register()
        {
            try
            {
                string assets = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Mods", "CMS2026_OXL", "Resources", "Images");
                // Grafiki z Docs/Design/geminiPromptOXL.md (oxlLogo.png, oxlMailBanner.png); bez nich stare logo i mail bez banera.
                string full = System.IO.Path.GetFullPath(assets);
                string logo = System.IO.File.Exists(System.IO.Path.Combine(full, "oxlLogo.png")) ? "oxlLogo.png" : "logo.png";
                string banner = System.IO.File.Exists(System.IO.Path.Combine(full, "oxlMailBanner.png")) ? "oxlMailBanner.png" : "";
                BlastCoreOS.BlastCoreMailAPI.RegisterSender(SenderId, "OXL Marketplace", "noreply@oxl.com", "#0E3338", logo, banner, full);
                _registered = true;
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL] Mail RegisterSender failed: {ex.Message}"); }
        }

        /// <summary>Odpowiedź sprzedawcy na "Message seller" (site.md 11.4): jedna na ogłoszenie (dedup), w stylu archetypu.</summary>
        public static bool SendSellerReply(string sellerNick, string listingTitle, string reply, string listingId)
        {
            try
            {
                if (!_registered) Register();
                if (!BlastCoreOS.BlastCoreMailAPI.IsReady) return false;
                string body = $"[b]{sellerNick}[/b] replied to your question about:\n{listingTitle}\n\n\"{reply}\"\n\nOXL never asks you to pay outside the site. Messages are not checked by OXL.";
                return BlastCoreOS.BlastCoreMailAPI.Send(SenderId, $"{sellerNick} replied: {listingTitle}", body, false, "oxl-msg-" + listingId, "") != null;
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL] Seller reply mail failed: {ex.Message}"); return false; }
        }

        /// <summary>Potwierdzenie zakupu. Błąd poczty nie cofa zakupu (pieniądze i paczka już są), tylko trafia do logu.</summary>
        public static void SendPurchase(string itemName, string sellerNick, int price, string deliveryText, string listingId, string imageFile = null)
        {
            try
            {
                if (!_registered) Register();
                if (!BlastCoreOS.BlastCoreMailAPI.IsReady) { OXLPlugin.Log.Warning("[OXL] Mail not ready, purchase mail skipped."); return; }
                string body =
                    "Thank you for your purchase on OXL.\n\n" +
                    $"[b]Item:[/b] {itemName}\n" +
                    $"[b]Seller:[/b] {sellerNick}\n" +
                    $"[b]Price:[/b] ${price:N0} (paid from your Redline account)\n" +
                    $"[b]Delivery:[/b] {deliveryText}\n\n" +
                    (deliveryText.Contains("parking lot") ? "" : "Your parcel will wait in the delivery area of your garage.\n") +
                    "Remember: on OXL you buy used goods as described by the seller. Check the condition before you use anything." +
                    // Zdjęcie na samym dole (użytkownik 2026-11-25), mniejsze (OXLUi.ExportSprite, 288 px).
                    (string.IsNullOrEmpty(imageFile) ? "" : $"\n\n[img={imageFile}]");
                BlastCoreOS.BlastCoreMailAPI.Send(SenderId, $"Order confirmed: {itemName}", body, false, "oxl-buy-" + listingId, "");
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL] Purchase mail failed: {ex.Message}"); }
        }
    }
}
