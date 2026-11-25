// OXLDelivery.cs
// Dostawa kupionych części, silników, narzędzi i dekoracji przez system przesyłek gry (DeliveryManager), site.md 9.5.
// Gracz dostaje paczkę jak ze sklepu gry: o najbliższej godzinie dostaw albo od razu (OXLSettings.InstantDelivery).
// Wiedza z testów G14 i G16: Docs/Research/Market.md "Przesyłki gry".

using System;
using Il2CppCMS.Containers;
using Il2CppCMS.Managers;
using Il2CppCMS.Player.Containers;

namespace CMS2026_OXL
{
    public enum OXLItemKind { Part, Engine, Customizable }

    public static class OXLDelivery
    {
        // Godzina 255 = dostawa natychmiastowa (G16: tylko 255 daje Delivery.IsInstant() = true).
        private const byte InstantHour = 255;

        /// <summary>Wynik nadania paczki: czy gra ją przyjęła, o której przyjdzie (InstantHour = już jest u gracza).</summary>
        public struct Result
        {
            public bool Ok;
            public byte Hour;
            public string Error;
            public bool Instant => Hour == InstantHour;
        }

        /// <summary>
        /// Nadaje jedną paczkę z jednym przedmiotem. condition01: rzeczywisty stan 0..1 (dla Customizable ignorowany).
        /// Stan części jest ukryty (IsExamined = false), jak w grze: gracz poznaje go dopiero po sprawdzeniu.
        /// </summary>
        public static Result Send(OXLItemKind kind, string itemId, float condition01, bool instant) => Send(kind, itemId, condition01, instant, 0);

        /// <summary>hour > 0: konkretna godzina dostaw gry tego dnia (kurier OXL z opóźnieniem, ItemListingSystem); 0 = najbliższa runda.</summary>
        public static Result Send(OXLItemKind kind, string itemId, float condition01, bool instant, byte hour)
        {
            var r = new Result { Ok = false, Hour = 0, Error = "" };
            try
            {
                var dm = UnityEngine.Object.FindObjectOfType<DeliveryManager>();
                if (dm == null) { r.Error = "DeliveryManager not found"; OXLPlugin.Log.Warning($"[OXL:DELIVERY] {r.Error}"); return r; }

                IBaseItem item = BuildItem(kind, itemId, condition01);
                if (item == null) { r.Error = $"could not build item '{itemId}'"; OXLPlugin.Log.Warning($"[OXL:DELIVERY] {r.Error}"); return r; }

                if (instant) hour = InstantHour; else if (hour == 0) hour = dm.GetNextDeliveryTime();
                string target = "";
                try { target = Il2CppCMS.Multiplayer.NetworkHelpers.GetPlayerName() ?? ""; } catch (Exception ex) { OXLLog.Msg($"[OXL:DELIVERY] player name failed: {ex.Message}"); }

                var shop = kind == OXLItemKind.Customizable ? Il2CppCMS.UI.Logic.ShopType.DecorationsTools : Il2CppCMS.UI.Logic.ShopType.UsedParts;
                var delivery = new Delivery(hour, shop, target);
                var di = new DeliveryItem();
                di.Data = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.FromIBaseItem(item);
                di.Amount = 1;
                delivery.AddItem(di);
                dm.OrderDelivery(delivery);
                if (instant)
                {
                    try { dm.RPC_DeliverWaitingInstantDeliveries(); }
                    catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:DELIVERY] instant delivery call failed (parcel stays queued): {ex.Message}"); }
                }

                r.Ok = true;
                r.Hour = hour;
                OXLLog.Msg($"[OXL:DELIVERY] {kind} '{itemId}' cond={condition01:F2} shop={shop} hour={hour} target='{target}' id={delivery.ID}");
                return r;
            }
            catch (Exception ex)
            {
                r.Error = ex.Message;
                OXLPlugin.Log.Warning($"[OXL:DELIVERY] {kind} '{itemId}' failed: {ex.Message}");
                return r;
            }
        }

        private static IBaseItem BuildItem(OXLItemKind kind, string itemId, float condition01)
        {
            switch (kind)
            {
                case OXLItemKind.Part:
                    return MakePart(itemId, condition01).TryCast<IBaseItem>();

                case OXLItemKind.Engine:
                {
                    // G14: EngineGroupItem(id) jest pusty; części z EnginePartsLookup, każda z lekkim rozrzutem stanu wokół ogłoszenia.
                    var look = Il2CppCMS.Core.GameInventory.Instance?.EngineDataProvider?.EnginePartsLookup;
                    if (look == null || !look.ContainsKey(itemId)) return null;
                    var parts = look[itemId]?.EngineParts;
                    if (parts == null || parts.Count == 0) return null;
                    var eng = new EngineGroupItem(itemId);
                    for (int i = 0; i < parts.Count; i++)
                    {
                        float c = UnityEngine.Mathf.Clamp01(condition01 + UnityEngine.Random.Range(-0.12f, 0.12f));
                        eng.ItemList.Add(MakePart(parts[i], c));
                    }
                    return eng.TryCast<IBaseItem>();
                }

                case OXLItemKind.Customizable:
                    return new CustomizableItem(itemId).TryCast<IBaseItem>();
            }
            return null;
        }

        private static Item MakePart(string id, float condition01)
        {
            var item = new Item(id);
            // Skala gry 0..255 (G14: 100 = 39%). Minimum 1, żeby część nie była zerowa.
            item.Condition = (byte)UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(condition01 * 255f), 1, 255);
            item.IsExamined = false;
            return item;
        }

        /// <summary>Tekst dla gracza: kiedy przyjdzie paczka.</summary>
        public static string EtaText(Result r) => r.Instant ? "Delivered now" : $"Next delivery round at {r.Hour}:00";
    }
}
