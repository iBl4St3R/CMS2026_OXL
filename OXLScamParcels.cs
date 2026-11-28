using CMS2026UITKFramework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CMS2026_OXL
{
    /// <summary>
    /// Paczki od scammera z podmienioną zawartością (G73, użytkownik 2026-11-28): gracz musi wiedzieć, że go oszukano.
    /// Przy nadaniu zapamiętujemy ID dostawy gry (= ID paczki w garażu), a gdy gracz otwiera tę paczkę (DeliveryManager.TakeFromStorage),
    /// framework pokazuje komunikat: co było kupione, co przyszło. Zapis w items.dat (linie "S"), żeby przetrwało wyjście z gry.
    /// </summary>
    public static class OXLScamParcels
    {
        private sealed class Trap { public ulong DeliveryId; public string Bought = "", Got = "", Seller = ""; }

        private static readonly List<Trap> _traps = new List<Trap>();
        private static bool _patched;

        /// <summary>Paczka nadana do gry: od teraz jej otwarcie pokaże komunikat. Zapis robi ItemListingSystem (Save po kupnie/nadaniu).</summary>
        public static void Add(ulong deliveryId, string bought, string got, string seller)
        {
            if (deliveryId == 0) { OXLPlugin.Log.Warning($"[OXL:SCAM] No delivery id for '{bought}', the player will not get the scam message."); return; }
            _traps.RemoveAll(t => t.DeliveryId == deliveryId);
            _traps.Add(new Trap { DeliveryId = deliveryId, Bought = bought ?? "", Got = got ?? "", Seller = seller ?? "" });
            OXLLog.Msg($"[OXL:SCAM] parcel {deliveryId}: '{bought}' -> '{got}' armed");
        }

        // ── Harmony: DeliveryManager.TakeFromStorage(ulong packageID) woła gracz, który bierze paczkę (sprawdzone 2026-11-28 przez Agent Bridge:
        // kolejno TakeItems, RPC_RemovePackage, SendDeliveryCollectEvent, TakeItemsFromPackage, RPC_TakePackage, TakeFromStorage, z ID = ID dostawy).
        public static void Install(HarmonyLib.Harmony harmony)
        {
            if (_patched || harmony == null) return;
            try
            {
                var m = typeof(Il2CppCMS.Managers.DeliveryManager).GetMethod("TakeFromStorage", new[] { typeof(ulong) });
                if (m == null) { OXLPlugin.Log.Warning("[OXL:SCAM] DeliveryManager.TakeFromStorage(ulong) not found, no scam message on opening parcels."); return; }
                harmony.Patch(m, postfix: new HarmonyLib.HarmonyMethod(typeof(OXLScamParcels).GetMethod(nameof(TakeFromStoragePostfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)));
                _patched = true;
                OXLLog.Msg("[OXL:SCAM] TakeFromStorage patched");
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:SCAM] Harmony patch failed: {ex.Message}"); }
        }

        private static void TakeFromStoragePostfix(ulong packageID)
        {
            try
            {
                var t = _traps.FirstOrDefault(x => x.DeliveryId == packageID);
                if (t == null) return;
                _traps.Remove(t);
                ItemListingSystem.SaveNow();
                string who = string.IsNullOrEmpty(t.Seller) ? "The seller" : t.Seller;
                // Tekst dla gracza: angielski, bez emoji i bez myślników (CLAUDE.md).
                // Czerwony prompt (Error), nie pomarańczowy: użytkownik 2026-11-28, oszustwo ma wyglądać jak zła wiadomość.
                UIPrompt.Error($"You got scammed! {who} took your money for {t.Bought}, but the parcel only had {t.Got}.", PromptPriority.Top, 8f);
                OXLPlugin.Log.Msg($"[OXL:SCAM] parcel {packageID} opened: '{t.Bought}' was '{t.Got}', message shown");
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:SCAM] parcel {packageID} message failed: {ex.Message}"); }
        }

        // ── Zapis w items.dat: "S\t{id}\t{kupione}\t{przyszło}\t{sprzedawca}" ──
        public static void AppendLines(StringBuilder sb, Func<string, string> esc)
        {
            foreach (var t in _traps) sb.Append(string.Join("\t", new[] { "S", t.DeliveryId.ToString(CultureInfo.InvariantCulture), esc(t.Bought), esc(t.Got), esc(t.Seller) })).Append('\n');
        }

        /// <summary>Linia "S" z items.dat; false = to nie ta linia. Uszkodzona linia przepada bez wywracania wczytania.</summary>
        public static bool TryLoadLine(string line, Func<string, string> unesc)
        {
            if (!line.StartsWith("S\t")) return false;
            var f = line.Split('\t');
            if (f.Length >= 5 && ulong.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong id)) _traps.Add(new Trap { DeliveryId = id, Bought = unesc(f[2]), Got = unesc(f[3]), Seller = unesc(f[4]) });
            return true;
        }

        public static void Clear() => _traps.Clear();
    }
}
