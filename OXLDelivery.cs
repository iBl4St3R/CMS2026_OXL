// OXLDelivery.cs
// Dostawa kupionych części, silników, narzędzi i dekoracji przez system przesyłek gry (DeliveryManager), site.md 9.5.
// Gracz dostaje paczkę jak ze sklepu gry: o najbliższej godzinie dostaw albo od razu (OXLSettings.InstantDelivery).
// Wiedza z testów G14 i G16: Docs/Research/Market.md "Przesyłki gry".

using System;
using System.Collections.Generic;
using System.Linq;
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
            public ulong DeliveryId;    // ID dostawy gry = ID paczki w garażu (DeliveryManager.TakeFromStorage dostaje to samo ID)
            public string Error;
            public bool Instant => Hour == InstantHour;
        }

        /// <summary>
        /// Nadaje jedną paczkę z jednym przedmiotem. condition01: rzeczywisty stan 0..1 (dla Customizable ignorowany).
        /// Stan części jest ukryty (IsExamined = false), jak w grze: gracz poznaje go dopiero po sprawdzeniu.
        /// </summary>
        public static Result Send(OXLItemKind kind, string itemId, float condition01, bool instant) => Send(kind, itemId, condition01, instant, 0, null);

        /// <summary>hour > 0: konkretna godzina dostaw gry tego dnia (kurier OXL z opóźnieniem, ItemListingSystem); 0 = najbliższa runda. wheelSeed: ID ogłoszenia, z którego opona albo felga dostaje rozmiar (ten sam co na stronie, MarketCatalog.WheelFor).</summary>
        public static Result Send(OXLItemKind kind, string itemId, float condition01, bool instant, byte hour, string wheelSeed = null)
        {
            var r = new Result { Ok = false, Hour = 0, Error = "" };
            try
            {
                var dm = UnityEngine.Object.FindObjectOfType<DeliveryManager>();
                if (dm == null) { r.Error = "DeliveryManager not found"; OXLPlugin.Log.Warning($"[OXL:DELIVERY] {r.Error}"); return r; }

                var items = BuildItems(kind, itemId, condition01, wheelSeed);
                if (items == null || items.Count == 0) { r.Error = $"could not build item '{itemId}'"; OXLPlugin.Log.Warning($"[OXL:DELIVERY] {r.Error}"); return r; }

                if (instant) hour = InstantHour; else if (hour == 0) hour = dm.GetNextDeliveryTime();
                string target = "";
                try { target = Il2CppCMS.Multiplayer.NetworkHelpers.GetPlayerName() ?? ""; } catch (Exception ex) { OXLLog.Msg($"[OXL:DELIVERY] player name failed: {ex.Message}"); }

                var shop = kind == OXLItemKind.Customizable ? Il2CppCMS.UI.Logic.ShopType.DecorationsTools : Il2CppCMS.UI.Logic.ShopType.UsedParts;
                var delivery = new Delivery(hour, shop, target);
                // Jedna paczka z przedmiotami z BuildItems (dziś zawsze jeden).
                foreach (var item in items)
                {
                    var di = new DeliveryItem();
                    di.Data = Il2CppCMS.Multiplayer.Containers.NetworkSerializableIBaseItem.FromIBaseItem(item);
                    di.Amount = 1;
                    delivery.AddItem(di);
                }
                dm.OrderDelivery(delivery);
                if (instant)
                {
                    try { dm.RPC_DeliverWaitingInstantDeliveries(); }
                    catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:DELIVERY] instant delivery call failed (parcel stays queued): {ex.Message}"); }
                }

                r.Ok = true;
                r.Hour = hour;
                r.DeliveryId = delivery.ID;
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

        private static List<IBaseItem> BuildItems(OXLItemKind kind, string itemId, float condition01, string wheelSeed)
        {
            switch (kind)
            {
                case OXLItemKind.Part:
                    return new List<IBaseItem> { WithWheelSize(MakePart(itemId, condition01), itemId, wheelSeed ?? itemId).TryCast<IBaseItem>() };
                case OXLItemKind.Engine:
                    return BuildEngine(itemId, condition01);
                case OXLItemKind.Customizable:
                    return new List<IBaseItem> { new CustomizableItem(itemId).TryCast<IBaseItem>() };
            }
            return null;
        }

        /// <summary>
        /// Cały silnik, który siada na stojaku (2026-11-28, Agent Bridge; wzorzec = silnik wyjęty z auta, Docs/Research/Market.md):
        /// części w EngineGroupItem mają ID = nazwa gniazda z EnginePartsPaths ("część(n)"), NormalID = część i MountObjectData (ścieżka, śruby puste);
        /// bez tego stojak nie montuje żadnej części (model niewidoczny), a bez MountObjectData wywraca się z czarnym ekranem.
        /// Jedna całość, jak silnik wyjęty z auta (użytkownik 2026-11-28: bez części luzem). Części montowane tylko przy aucie (skrzynia,
        /// rozrusznik, początek wydechu) gra przy wyjmowaniu zostawia w aucie, a stojak je odrzuca: OXL ich nie wysyła, gdy zna je z modelu
        /// (OXLEngineParts); jeszcze nieznane idą w silniku i stojak pominie je tak samo jak gra.
        /// </summary>
        private static List<IBaseItem> BuildEngine(string engineId, float condition01)
        {
            var look = Il2CppCMS.Core.GameInventory.Instance?.EngineDataProvider?.EnginePartsLookup;
            if (look == null || !look.ContainsKey(engineId)) return null;
            var cfg = look[engineId];
            var parts = cfg?.EngineParts;
            if (parts == null || parts.Count == 0) return null;
            // Ścieżki gniazd w modelu (EnginePartsConfig.EnginePartsPaths, równoległe do EngineParts), np. ".../!v8_350_intake_NA/v8_przepustnica(1)":
            // numer w nawiasie to nazwa gniazda, nie numer wystąpienia (2026-11-28: z "(0)" przepustnica i napinacz V8 nie siadały).
            var paths = cfg.EnginePartsPaths;
            OXLEngineParts.ScanScene();

            var eng = new EngineGroupItem(engineId);
            var skipped = new List<string>();
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < parts.Count; i++)
            {
                string id = parts[i];
                float c = UnityEngine.Mathf.Clamp01(condition01 + UnityEngine.Random.Range(-0.12f, 0.12f));
                if (OXLEngineParts.IsCarOnly(id)) { skipped.Add(id); continue; }
                string path = paths != null && i < paths.Count ? paths[i] : null;
                string slot;
                if (!string.IsNullOrEmpty(path)) slot = path.Substring(path.LastIndexOf('/') + 1);
                else { seen.TryGetValue(id, out int k); seen[id] = k + 1; slot = id + "(" + k + ")"; }   // bez ścieżki: numer wystąpienia (zgadza się u większości części)
                var item = MakePart(slot, c);
                item.NormalID = id;
                item.MountObjectData = EmptyMount(path);
                eng.ItemList.Add(item);
            }
            OXLLog.Msg($"[OXL:DELIVERY] engine '{engineId}': {eng.ItemList.Count} parts, left out (car only, like an engine pulled from a car): {string.Join(", ", skipped)}");
            return new List<IBaseItem> { eng.TryCast<IBaseItem>() };
        }

        // MountObjectData ze ścieżką gniazda, bez stanu śrub (silnik wyjęty z auta ma też śruby; pusta lista wystarcza, sprawdzone 2026-11-28).
        private static MountObjectData EmptyMount(string path)
        {
            var m = new MountObjectData();
            m.ParentPath = path ?? "";
            m.Condition = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ushort>(0);
            m.IsStuck = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<bool>(0);
            m.Dirtness = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<ushort>(0);
            return m;
        }

        private static Item MakePart(string id, float condition01)
        {
            var item = new Item(id);
            // Skala gry 0..255 (G14: 100 = 39%). Minimum 1, żeby część nie była zerowa.
            item.Condition = (byte)UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(condition01 * 255f), 1, 255);
            item.IsExamined = false;
            return item;
        }

        // Opona albo felga bez WheelData przychodzi jako "0/0R0" warta 0 (użytkownik 2026-11-28). Rozmiar z configów aut gry (MarketCatalog.WheelFor).
        private static Item WithWheelSize(Item item, string id, string seed)
        {
            try
            {
                if (!MarketCatalog.TryGet(id, out var e) || (!e.IsTire && !e.IsRim)) return item;
                if (!MarketCatalog.WheelFor(seed, out int rim, out int width, out int profile, out int et)) { OXLPlugin.Log.Warning($"[OXL:DELIVERY] no wheel sizes in game data, {id} goes without size"); return item; }
                var w = new Il2Cpp.WheelData { Size = rim, Width = width, Profile = profile, ET = e.IsRim ? et : 0 };
                item.WheelData = w;
                OXLLog.Msg($"[OXL:DELIVERY] {id} wheel size={rim} width={width} profile={profile} ET={w.ET}");
            }
            catch (Exception ex) { OXLPlugin.Log.Warning($"[OXL:DELIVERY] wheel size for {id} failed: {ex.Message}"); }
            return item;
        }

        /// <summary>Tekst dla gracza: kiedy przyjdzie paczka.</summary>
        public static string EtaText(Result r) => r.Instant ? "Delivered now" : $"Next delivery round at {r.Hour}:00";
    }
}
