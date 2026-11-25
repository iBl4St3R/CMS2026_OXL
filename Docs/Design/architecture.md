# Architektura CMS2026_OXL

> Odtworzone z kodu i opisu Nexusa 2026-11-20 (wersja 0.6.0). Szczegóły systemu archetypów: `archetypes.md` w roocie repo (v0.3.0, po polsku). Aktualizuj przy zmianach.

## Pliki (wg roli)

| Plik | KB | Rola |
|---|---|---|
| `OXLPlugin.cs` | 6 | `MelonMod`: scena "garage", rejestracja stron i nazwy w banku Redline, komendy konsoli `oxl_*`. Klawisz T i przycisk w menu sklepu usunięte 2026-11-25 (`rebuild.md` 1.1). |
| `OXLPanel.cs` | **182** | Całe UI: lista ogłoszeń, szczegóły, kupno, galeria, ustawienia. Tylko tryb osadzony w przeglądarce BlastCoreOS (`Build…Into(container, ctx)`); tryb samodzielny usunięty 2026-11-25. Czytaj tylko zakresami. |
| `OXLSite.cs`, `OXLUi.cs` | 25, 7 | **Nowa strona** `oxl.com` (`site.md`): routing po ścieżce (home, cars, parts, tools, search, item/{id}, settings), stan w adresie, karty ze zdjęciami z gry i znaczkiem STOCK PHOTO, kupno przedmiotów, ustawienia. `OXLUi`: klocki UI (pudełko, tekst, przycisk, sprite contain, pole tekstowe). |
| `OXLFilterPanel.cs` | 41 | Panel filtrów (dropdowny, opcje z `FilterOptionsBuilder`). |
| `OXLWebPage.cs` | 3 | Rejestracja stron: nowa strona (`OXLSite`) + stare strony aut `oxl.com/listings` i `oxl.com/listing/{id}` (do przeniesienia). `OXLHomePage.cs` usunięty 2026-11-25. |
| `ListingSystem.cs` | 86 | Generator i cykl życia ogłoszeń (czas gry, TTL, archetypy, ceny). |
| `SellerNoteBuilder.cs` | **261** | Generator opisów sprzedawców (osobowość, poziom, główna usterka, kolorowane słowa). Tekst dla gracza. Tylko grep. |
| `SellerProfile.cs` | 17 | Awatary (115, LRU 8, historia 30) i nicki. |
| `GameBridge.cs` | 60 | **Most do gry:** spawn kupionego auta na parkingu i nałożenie zużycia wg archetypu (`PartCatalog.Classify`). Płatność przez `BlastCoreBankAPI` (`GetBankAvailable`, `ChargeForCar`). Przepisany na API 2027 (2026-11-25). `IsVersionSpawnable`: filtr wersji aut z `BlastCoreGameDataAPI` (G12). |
| `GameCarAccess.cs` | 5 | Stan auta 2027, którego interop nie daje wprost: części karoserii (`CarBodyPart` z filtrem po właścicielu) i przebieg w `CarInfoData` (natywny getter). Używany też przez `Scripts/probe_carstate.cs`. |
| `MarketCatalog.cs` | 7 | Co OXL może sprzedawać, w całości z danych gry: auta (bezpieczne wersje), kompletne silniki i drogie części (próg = percentyl cen) z `BlastCoreGameDataAPI`, narzędzia i dekoracje z `GarageObjectsProvider`; miniatury z gry (`site.md` 10). Komenda `oxl_market`. |
| `OXLDelivery.cs` | 6 | Paczka przez system przesyłek gry (`DeliveryManager`): część, kompletny silnik (wypełniany częściami), narzędzie/dekoracja; po czasie albo od razu (godzina 255), stan ukryty. Komenda testowa `oxl_deliver`. |
| `OXLMail.cs` | 2 | Nadawca "OXL Marketplace" i mail po kupnie (`BlastCoreMailAPI`). |
| (w `OXLSite.cs`) | | Dostawcy BlastCoreOS: wyniki Geargle (`RegisterSearchProvider`) i paczki kuriera OXL w kalendarzu (`BlastCoreCalendarAPI.RegisterDeliveryProvider`, `ItemListingSystem.GetPendingParcels`). |
| `OXLPanel.TryBuyCar` | | Kupno auta z nowej strony `oxl.com/car/{id}` (bez starego UI), kontrakt "pieniądze po spawnie", mail ze zdjęciem. |
| `ItemListing.cs`, `ItemStoryBuilder.cs` | 1, 4 | Ogłoszenie przedmiotu (silnik, część, narzędzie, dekoracja) i opisy sprzedawców: powód sprzedaży jak w życiu + styl archetypu, po angielsku bez myślników. |
| `ItemListingSystem.cs` | 12 | Generator i cykl życia ogłoszeń przedmiotów (czas gry, limity, perełki 4%), ceny i stan wg archetypu, zapis `Mods/CMS2026_OXL/items.dat`, kupno: saldo → paczka → pobranie z banku → mail. Komendy `oxl_items`, `oxl_items_generate`, `oxl_buyitem`. |
| `PartCatalog.cs` | 32 | Klasyfikacja części 5 aut demo 2026. |
| `ScammerLies.cs` | 6 | Kłamstwa scammera L2/L3 w ogłoszeniu auta (kolor, silnik, wersja, moc 5x) i to, co pokazuje strona (`scammer.md`). |
| `CarPhotoPack.cs` | 12 | **Paczka zdjęć 2027** (`Resources/CarPhotos`, WebP przez ImageSharp w `UserLibs`): skan, wybór koloru i miejsca przy generowaniu, dekodowanie w tle, cache tekstur (`rebuild.md` 8). |
| `CarPhotoLoader.cs`, `CarSpecLoader.cs`, `PhotoCondition.cs` | 30, 17, 1.5 | Stare paczki 2026 (MEDRES / LOWRES, kolor, config, przedział kondycji), `car_spec.json`; do usunięcia z `CarDefs` (ZADANIE pkt 4). |
| `ListingPersistence.cs` | 13 | Zapis ogłoszeń `Mods/CMS2026_OXL/listings.dat`. |
| `OXLSettings.cs`, `ListingGenConfig.cs` | 13, 2 | Ustawienia (trudność, generacja) w `CMS2026_OXL.cfg`. |
| `GameTimeProvider.cs` | 6 | Czas gry (`TimeManager` przez refleksję, interpolacja sekund). |
| `CarListing.cs`, `FilterOptionsBuilder.cs`, `NullableShim.cs` | | Model ogłoszenia, opcje filtrów, szim `NullableAttribute`. |

## Przepływ

Generator (`ListingSystem`) tworzy ogłoszenia w czasie gry → UI (`OXLPanel`) je pokazuje (z `CarPhotoLoader`, `SellerNoteBuilder`, `SellerProfile`) → kupno: kontrola miejsca na parkingu i salda konta w banku Redline → `GameBridge` spawnuje auto i nakłada zużycie → dopiero po udanym spawnie pobranie z konta (`BlastCoreBankAPI.TryCharge`); przy błędzie dostawy ogłoszenie wraca (z zapisem), pieniądze nie są pobierane.

## Zasady (z opisu Nexusa 0.5/0.6)

- 4 archetypy sprzedawców (Honest, Neglected, Dealer, Scammer) × 3 poziomy: ceny, zużycie, usterki, styl opisu.
- "Sprzedawcy kłamią w opisach, nie na zdjęciach (zwykle)."
- Odliczanie aukcji w czasie gry; przyspieszenie czasu skraca okno.
- Trudność Easy/Normal/Hard zmienia ceny; zapis w cfg.
- Limit 10 aut w demie; kupno blokowane przy pełnym parkingu.
- Aktualizacja moda wymaga czystej reinstalacji (usunąć folder i DLL).

## Zależności

- `_CMS2026_UITK_Framework` (twarda), **`BlastCoreOS` (twarda, >= 0.2.0: strony w przeglądarce, bank Redline do płatności)**, Simple Console (miękka, komendy `oxl_*`).
