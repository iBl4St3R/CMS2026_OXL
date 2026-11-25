# Dane gry pod nową stronę OXL (rynek)

> Stan 2026-11-25. Źródło: `Scripts/probe_engines.cs` (G12), `Scripts/probe_market.cs` (G13), zrzut `cms2027assembly`. Legenda: ✔ potwierdzone w grze, ? do sprawdzenia.

## Wersje aut i silniki (filtr "da się sprzedać")

- ✔ 7 aut, 31 wersji (`CarBundleLoader.CarNamesData[i].CarConfigs[cfg]`, silnik fabryczny w `[6_engine] type`). Wszystkie silniki są w `EnginePartsLookup` (564) i mają `PhysicsEngineData` (`EngineDataProvider.TryGet`), więc te dwa sprawdzenia **nie** odróżniają złych wersji.
- ✔ **Odróżnia `GameInventory.HaveDLC(silnik)`** (to samo co `ExistsInPartProperty` i miniatura silnika): True dla 11 wersji, False dla 20. Silniki z błędu G12 (DNB Censor cfg 1 i 2) mają False; wszystkie wersje sprzedawane bez problemu przez stare OXL (Katagiri 0, Luxor 0, Mayen 1, Salem 0, DNB 0) mają True.
- Wniosek: **wersja auta do OXL tylko gdy `HaveDLC(silnik fabryczny)` = True.** Od 2026-11-25 BlastCoreOS podaje to jako `BlastCoreGameDataAPI` → `BlastCoreGameCarVersion.StockEngineAvailable`; OXL filtruje w `GameBridge.IsVersionSpawnable` (G15). Wersje z True: Bolt Nevada 0, 3, 4; DNB Censor 0, 3; Katagiri 0; Lacroix 2; Luxor 0, 1; Mayen 1; Salem 0. Dane z gry, bez listy w kodzie (po aktualizacji gry lista zmieni się sama). ? Potwierdzić kupnem i wczytaniem zapisu jednej wersji z True, której stare OXL nie sprzedawało (np. Bolt Nevada 3).
- ✔ Miniatura auta per wersja: `ThumbnailsProvider.GetCarThumb(carId, cfg)` (sprite `car_x-car_x-N`, jest dla wszystkich wersji).

## Katalog części (`GameInventory.PartPropertyList`, 601)

- ✔ Cena `ushort`. Rozkład: p50 = 150, p75 = 285, p90 = 550, p97 = 2700, max = 5005. Próg "droga część" liczony z tego rozkładu w czasie gry (np. >= p90).
- ✔ Całe silniki: grupa `engine` / `EngineShop`, 17 wariantów, każdy 5005, `HaveDLC` True, miniatury są. To są silniki, które gracz może kupić w demie.
- ✔ Najdroższe części: bloki silników (1850–3000), kompresor (2700), wały korbowe (1680–1950), skrzynia rozdzielcza (1700), felgi do 900, hamulce elektroniczne do 950.
- ✔ `Availability`: `OnlyNew` 514, `Always` 24, `OnlyUsed` 63 (`OnlyUsed`: części karoserii z `BodyShop`, 83 z 155 ma != 0). `HaveDLC` w katalogu = True dla wszystkich 601.
- ✔ Miniatury części: `ThumbnailsProvider.TryGetThumbnail(id)` działa dla części i silników z katalogu.

## Narzędzia i dekoracje (`GameInventory.GarageObjectsProvider`)

- ✔ `GetAllCustomizableObjects()`: 911 obiektów `CustomizableObjectConfig` (`ObjectID`, `Price` ushort, `ObjectType` Tool/Decoration, `Brand`: tool, decoration, storage...). Nazwa: `GetCustomizableItemLocalizeName(id)`, kategoria: `GetCustomizableItemCategory(id)` (Tools / Decorations). Miniatury są.
- ✔ Ceny narzędzi: od 175 (ściągacz sprężyn) do 7500 (podnośnik); dekoracje od 5 (krzesło).
- Przedmiot do ekwipunku: `CustomizableItem(id)` (gra trzyma je zawsze przy graczu, BlastCoreOS `decisions.md`).

## Przesyłki gry (`Il2CppCMS.Managers.DeliveryManager`)

- ✔ Godziny dostaw: 8, 10, 12, 14, 16, 18 (`GetDeliveryHours`), `GetNextDeliveryTime()`.
- ✔ `WaitingDelivery`: lista `Delivery` (`ID`, `Hour`, `ShopType`, `DeliveryTarget` = nazwa gracza, `DeliveryItems`: `DeliveryItem.Data.BaseItem` + `Amount`). Jedna paczka na sklep: Wheels, Body, Engine, Main, Interior, LicensePlates, **DecorationsTools**, **UsedParts**.
- ✔ Tworzenie (G14, G16 2026-11-25): `new Delivery(hour, ShopType, target)`, `new DeliveryItem { Data = NetworkSerializableIBaseItem.FromIBaseItem(item), Amount = 1 }`, `delivery.AddItem(...)`, `DeliveryManager.OrderDelivery(delivery)`. Natychmiastowa: `Delivery.IsInstant()` (zależne od godziny?), `DeliverAllOrdersInstant()`, `RPC_DeliverWaitingInstantDeliveries()`. Do sprawdzenia skryptem `try_`.
- ✔ Przedmioty (G14): `new Item(id)` ma `Condition` 255 = 100%, skala liniowa (100 → 0.39 w `GetConditionToShow`); `CustomizableItem(id)` działa; **`EngineGroupItem(id)` jest pusty (0 części, stan NaN)**: OXL musi dodać części z `EnginePartsLookup[id].EngineParts` (test G16, `try_delivery2.cs`).
- ✔ Adresat: `Il2CppCMS.Multiplayer.NetworkHelpers.GetPlayerName()` (np. "Blaster"), jak w paczkach gry.
- ✔ **Dostawa natychmiastowa = godzina 255** (`IsInstant()` true tylko dla 255) + `RPC_DeliverWaitingInstantDeliveries()`: przedmiot u gracza od razu, kolejka pusta (G16).
- ✔ Silnik wypełniony 89 częściami z `EnginePartsLookup` dociera kompletny (G16). Części miały **odkryty stan**; w grze części z paczki mają stan ukryty do sprawdzenia: `Item.IsExamined = false` (wdrożone w `OXLDelivery`, test G19).
- ? Dostawa po czasie (godzina z `GetNextDeliveryTime()`): paczki z G14 przyjęte do kolejki, dotarcie o 10:00 niepotwierdzone (test G18).

## Zawieszenie wczytywania zapisu (2026-11-25)

- ✔ Zapis z autami kupionymi w OXL (sesja 17:31) nie wczytuje się: ekran ładowania zostaje, gra działa dalej (konsola odpowiada). W `%USERPROFILE%\AppData\LocalLow\Red Dot Games\Car Mechanic Simulator 2027 Demo\Player.log` (log samej gry, nie MelonLoadera): `Fusion ... AcquireInstance ... The Object you want to instantiate is null` w `CMS.Core.Car.Components.Wheels.SpawnRim` ← `CarLoader.LoadCarFromFile` ← `GarageLoader.LoadCarsState`. Wyjątek przerywa wczytywanie aut.
- ✔ Ten sam schemat co G12 (silniki DNB): wersja spawnuje się przez `CarDebug.LoadCar`, a psuje dopiero przy wczytaniu zapisu, bo felgi nie ma w demie. **Winny: Luxor Streamliner Mk3 Cruiser (cfg 1)**, silnik z dema, felga `rim_274` `HaveDLC=false` (G48, `probe_versions.cs`). Felgi i opony są w configu wersji: `[3_wheels]` i `[4_wheels_rear]`, klucze `rim` i `tire`. Spoza dema m.in. `rim_108`, `rim_107`, `rim_109`, `rim_17`, `rim_110`, `rim_113`, `rim_46`, `rim_46C`, `rim_8`, `rim_274`, `tire_sport`; prawie wszystkie te wersje mają też silnik spoza dema.
- ✔ Filtr OXL (2026-11-25): wersja auta tylko gdy silnik fabryczny **oraz felgi i opony przód i tył** mają `HaveDLC = true` (`MarketCatalog.VersionsWithMissingWheels`). W demie zostaje 10 wersji (bez Cruisera).
- Wniosek na przyszłość: **przy kłopotach z wczytywaniem zawsze czytać też `Player.log` gry**, nie tylko `MelonLoader/Latest.log`.
