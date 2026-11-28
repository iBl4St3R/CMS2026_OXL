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
- ✔ Grupy `BlastCoreGameDataAPI.GetParts` (2026-11-28, `Scripts/probe_partgroups.cs`, ShopGroup | ShopName): Engine|MainShop 224 ($5–3000), Noinshop|BodyShop 155 (karoseria, `IsBody`, $70–550), Suspension 65, Engine|ElectronicsShop 37 (ECU, akumulatory), Exhaust 21, Brakes|MainShop 19, engine|EngineShop 17 (całe silniki, $5005), Gearbox 14, Noinshop|MainShop 13 (cena 0: bagnety, korki), Rims 8, Seats 7, Benches 6, SteeringWheels 6, Noinshop|BodyTuningShop 4 (karoseria B), Tires 3, Brakes|ElectronicsShop 2 (ABS Pump, ABS Module). Ceny: p10 $26, p25 $70, p50 $150.
- ✔ Kurier OXL (`Item(id)`) przyjmuje każdą grupę: maska, drzwi, felga, opona, moduł ABS, tłumik, fotel (2026-11-28, `try_partkinds.cs`; ? wygląd w ekwipunku G76).
- ✔ Opona i felga z `new Item(id)` mają `WheelData` = 0 (gracz widzi "0/0R0", wartość 0). `Item.WheelData` (struktura `Il2Cpp.WheelData`: `Size`, `Width`, `Profile`, `ET`, `IsBalanced`) da się ustawić i odczytać; `UIHelper.GetTireDataString` daje wtedy "(225/75R15)", `GetRimDataString` "(15\") ET: 30" (2026-11-28, `probe_wheelset.cs`). `PartScript.CreateItem()` kół auta z parkingu zwraca WheelData = 0 (nie źródło rozmiarów). Rozmiary: config auta `[3_wheels]` `rimSize=15`, `wheelWidth=225`, `tireSize=75` (Bolt Nevada), bez ET (`probe_wheelcfg.cs`). `Helper.GetRandomWheelData()` daje losowe (rim, width, profile) do 24", nie pasujące do aut dema. ? ET felg ze sklepu gry.

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
- ✔ Dostawa po czasie działa (2026-11-28, Agent Bridge: paczka nadana o północy na 16:00, po 16:00 `WaitingDelivery` puste).
- ✔ **Otwarcie paczki przez gracza** (2026-11-28, logujące postfixy Harmony z REPL, `Scripts/probe_takepackage.cs`): kolejno `TakeItems(Delivery, Inventory, ...)`, `RPC_RemovePackage(id)`, `SendDeliveryCollectEvent(added, inventoryFull)`, `TakeItemsFromPackage(TakePackagePacket)`, `RPC_TakePackage(id, player)`, `TakeFromStorage(id)`. **ID paczki = `Delivery.ID`** z nadania (1380 = 1380). Hak OXL: postfix na `TakeFromStorage(ulong packageID)`.
- ✔ Harmony w REPL konsoli: brak referencji do `0Harmony`, działa przez refleksję (`AppDomain` → `HarmonyLib.Harmony`, `Patch` z 5 parametrami), postfix jako statyczna funkcja lokalna skryptu.

## Zawieszenie wczytywania zapisu (2026-11-25)

- ✔ Zapis z autami kupionymi w OXL (sesja 17:31) nie wczytuje się: ekran ładowania zostaje, gra działa dalej (konsola odpowiada). W `%USERPROFILE%\AppData\LocalLow\Red Dot Games\Car Mechanic Simulator 2027 Demo\Player.log` (log samej gry, nie MelonLoadera): `Fusion ... AcquireInstance ... The Object you want to instantiate is null` w `CMS.Core.Car.Components.Wheels.SpawnRim` ← `CarLoader.LoadCarFromFile` ← `GarageLoader.LoadCarsState`. Wyjątek przerywa wczytywanie aut.
- ✔ Ten sam schemat co G12 (silniki DNB): wersja spawnuje się przez `CarDebug.LoadCar`, a psuje dopiero przy wczytaniu zapisu, bo felgi nie ma w demie. **Winny: Luxor Streamliner Mk3 Cruiser (cfg 1)**, silnik z dema, felga `rim_274` `HaveDLC=false` (G48, `probe_versions.cs`). Felgi i opony są w configu wersji: `[3_wheels]` i `[4_wheels_rear]`, klucze `rim` i `tire`. Spoza dema m.in. `rim_108`, `rim_107`, `rim_109`, `rim_17`, `rim_110`, `rim_113`, `rim_46`, `rim_46C`, `rim_8`, `rim_274`, `tire_sport`; prawie wszystkie te wersje mają też silnik spoza dema.
- ✔ Filtr OXL (2026-11-25): wersja auta tylko gdy silnik fabryczny **oraz felgi i opony przód i tył** mają `HaveDLC = true` (`MarketCatalog.VersionsWithMissingWheels`). W demie zostaje 10 wersji (bez Cruisera).
- Wniosek na przyszłość: **przy kłopotach z wczytywaniem zawsze czytać też `Player.log` gry**, nie tylko `MelonLoader/Latest.log`.

## Cały silnik w paczce i stojak (2026-11-28, Agent Bridge)

- ✔ Gra nie sprzedaje całych silników (sklep ma tylko części silnika); pełny silnik powstaje z auta (dźwig, `NotificationCenter.UnmountEngine`) albo z części na stojaku. "Create engine" na stojaku tworzy **pusty szkielet** (3 korki zamontowane, reszta `IsUnmounted`).
- ✔ Silnik z OXL sprzed poprawki (`EngineGroupItem` z `new Item(id)`) siadał na stojaku jako szkielet: model niewidoczny, menu "No engine on stand", 71/74 części `IsUnmounted`, części z paczki przepadały.
- ✔ **Wzorzec gry (silnik V6 wyjęty z auta, 89 części):** `Item.ID` = `część(k)` (k = numer wystąpienia, np. `v6_231_popychacz(0)`..`(11)`), `NormalID` = `część`, UID nadaje gra, **każda część ma `MountObjectData`** (`ParentPath`, `Condition[]`, `IsStuck[]`, `Dirtness[]` = stan śrub; zwykle pusty, przy kilku częściach lista śrub, np. docisk sprzęgła 5, miska 6, wartości 65535 = pełny stan; `ParentPath` np. `engine25_v6_231/!BASE/docisk_sprzegla(0)`). `new Item(id)` ma `NormalID` puste i `MountObjectData` null.
- ✔ **Numer w nawiasie to nazwa gniazda w modelu, nie numer wystąpienia**: `EnginePartsConfig.EnginePartsPaths` (równoległe do `EngineParts`, `EnginePartsLookup[id]` to `EnginePartsConfig`) podaje ścieżkę gniazda, np. `engine25_v8_350/!MODULES/intake_manifolds/!v8_350_intake_NA/v8_przepustnica(1)`; napinacz V8 jest dwa razy, w gniazdach `(2)` i `(0)`. ID części = ostatni człon ścieżki, `MountObjectData.ParentPath` = ścieżka (jak w silniku wyjętym z auta). Z numerem wystąpienia nie siadały przepustnice, napinacz V8 i rolka I4; ze ścieżkami V8 820MAL siada w całości (106 części, brak tylko części do auta).
- ✔ ID `część(k)` + `NormalID` bez `MountObjectData`: stojak rozpoznaje części, ale `PartScript.SetMountObjectData` rzuca NullReferenceException i zostaje czarny ekran (fader). **Z pustym `MountObjectData` (ścieżka '', tablice 0) silnik siada cały** (R4: 57/60 zamontowanych, model widoczny).
- ✔ Stojak nie montuje części z `PartScript.canMountUnmountOnlyOnCarLoader` (R4: rozrusznik, skrzynia `r4_gearbox_short`, początek wydechu); przepadają (nie wracają do ekwipunku). Przy wyjmowaniu z auta gra zostawia w aucie skrzynię, rozrusznik i 3 korki. Flaga jest tylko na modelu części; `BlastCoreGamePart.Fits` i grupy sklepu jej nie odróżniają (rozrusznik i alternator oba Engine|ElectronicsShop). OXL uczy się jej z modeli w scenie (`OXLEngineParts`, `engine_parts.txt`).
- ✔ `EngineGroupItem.ItemList` przesłania `GroupItem.ItemList`: rzut na `GroupItem` daje null, czytać przez `EngineGroupItem`. `Inventory.GetEngines()` zwraca silniki z ekwipunku.
- ⚠ REPL: nie wołać `EngineStand.CreateEngine` ani innych funkcji stojaka bez okna gry: `LockPlayer` + fader zostają, czarny ekran. Naprawa: `Scripts/try_unblack.cs` (`ScreenFader.SetClear`, `HideLoadingInstant`, `EngineStand.UnlockPlayer`).
- ✔ **Wartość całego silnika** (2026-11-28, `Scripts/probe_engineprices.cs`): katalog gry podaje 5005 dla każdego silnika (zastępcza). `Helper.GetPrice(grupa)` = suma cen nowych części × stan (30% → 30%), `Helper.GetItemSellPrice` = połowa. Nowe (suma części): I4 AQ4C 7430–7553, I4 B4FM 11403, V6 322BZ 11137–15584, V8 820MAL 14418–15793, V8 D.vik 14885–16657.

## Klasyfikacja zużycia części nowych aut (pkt 4, 2026-11-28, `Scripts/probe_wearclass.cs`, `probe_wearclass2.cs`)

- ✔ `PartCatalog.Classify` (tabela z 5 aut 2026) wrzucała nieznane części do `Structural` (krzywa jak blacha). Lacroix Modique R: tylko 13 części mechanicznych poza tabelą (felgi, opony, fotele, drobiazgi). **Bolt Nevada: 72**, w tym prawie cały silnik V8 D.vik (`v8_d62_*`: blok, głowice, wał, łańcuch i paski rozrządu, wtryski, turbo), zapłon z rozdzielaczem, gaźnik, wentylator.
- Poprawka: gdy ID nie pasuje, `Classify` próbuje bez 1–3 pierwszych członów (prefiks silnika, np. `v8_d62_`, `w_v8_d62_`) i porównuje z tabelą i z nazwami bazowymi jej kluczy (`v8_350_lancuch` -> `lancuch`, niejednoznaczne pominięte), plus kilka nazw ogólnych (cewka zapłonowa, rozdzielacz, wentylator, gaźnik, kompresor, intercooler). Po poprawce z części mechanicznych Bolta w `Structural` zostają tylko koła i fotele (zużycie liczone osobno).
