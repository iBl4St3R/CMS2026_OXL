# Backlog

> Otwarte zadania, znaleziska i ryzyka. Zrobione przenoś do `Docs/History/backlog-done.md` (z datą). Źródło początkowe: przegląd kodu, opis Nexusa 0.6.0 i kompilacja względem 2027 (2026-11-20).

## Port na CMS 2027

- Etap 1 (2026-11-25): `GameBridge` na API 2027, `MelonGame` 2027, płatności przez bank, tryb samodzielny i przycisk w sklepie usunięte. Czeka na G6–G11; nieudane zamienniki z `Port2027.md` pkt 2 i 6 poprawić po teście.
- Powiadomienia przez `BlastCoreMailAPI` (`BlastCoreOS/Docs/Design/mail.md`): np. potwierdzenie kupna, koniec aukcji. Do ustalenia przy etapie 4.
- `Docs/PlayerInputFix.md` dotyczy Simple Console (`DevConsoleComponent`): przekazać agentowi konsoli. OXL już nie rusza `PlayerInput`.
- Fotograf (`totalMaker.cs` w folderze konsoli, `Docs/Research/ConsoleScripts.md`) i ekstraktor `car_spec.json` (`jsonCarExtractor.cs`) na API 2027, przenieść do `Scripts/` (etap 2).
- Nowe auta 2027 (Bolt Nevada, Lacroix Modique R): `PartCatalog`, `car_spec.json`, `color_map.txt`, paczki zdjęć (LOWRES w paczce moda, MEDRES osobno). Sprawdzić ID i configi 5 starych aut.
- Stare `listings.dat` z 2026: niepotrzebne, wydanie 2027 = czysta instalacja i nowa gra (`rebuild.md` 6).
- **Błąd G12 (wyjaśniony 2026-11-25):** brakujące przy wczytaniu silniki to fabryczne silniki DNB Censor cfg 1 (LIQD-X4) i 2 (5000), które stare OXL sprzedawało. Filtr wersji z danych gry + samoucząca blokada po spawnie (`site.md` 9.8), do zrobienia w etapie generatora.
- **README do przepisania** (`readme.md`): opisuje 2026, klawisz HOME, `oxl_open`, płatność z portfela, emoji. Przy wydaniu: tylko przeglądarka BlastCoreOS, bank Redline, zależności (Framework 0.4.0+, Konsola 1.3.2+, BlastCoreOS 0.2.0+), czysta reinstalacja, nowa paczka zdjęć. To samo w opisie Nexusa.
- Plik `Mods/CMS2026_OXL/Resources/buttons/OXLbutton.png` nie jest już używany (przycisk w sklepie usunięty): nie dołączać do paczki wydania.
- BlastCoreOS (do przekazania, nie edytujemy): przy wyjściu z garażu 3× `[CursorManager] Release() called with count already 0` w trakcie `Left the garage: 3 desktop(s) torn down` (log 2026-11-25 13:31:48). OXL nie woła `Request/Release`.

## Znaleziska z kodu

1. **Bardzo duże pliki** (`SellerNoteBuilder.cs` 261 KB, `OXLPanel.cs` 182 KB, `ListingSystem.cs` 86 KB): kandydaci na `partial class` bez zmiany API, osobno od portu.
2. **Teksty sprzedawców w kodzie** (`SellerNoteBuilder`): zawierają myślniki i są generowane; zmiany tylko na prośbę (to treść, nie błąd).
3. **Ścieżka danych przez `Application.dataPath/../Mods/CMS2026_OXL`** (`ListingPersistence.cs:13`): działa, ale zależy od nazwy folderu moda.
4. **Opis Nexusa 0.5.0 mówi o "MEDRES v3" i zdjęciach z artefaktami**: przy nowych autach regenerować paczki fotografem.

## Rejestr ryzyk

1. **Pieniądze i spawn:** błąd w nowym `GameBridge` = gracz płaci za auto, które się nie pojawiło, albo auto bez zużycia. Kontrakt "zwrot przy błędzie dostawy" ma goal przy każdym wydaniu.
2. **Fusion / multiplayer:** `CarInfoData` jest w 2027 strukturą sieciową; spawn i ustawianie stanu mogą wymagać autorytetu sieciowego.
4. **Wersje aut z elementami spoza dema** (2026-11-25): auto spawnuje się, ale zapis z nim się nie wczytuje (silniki DNB G12, felgi G48). Każda nowa część wersji (silnik, felgi, opony) musi być sprawdzona filtrem z danych gry; przy pełnej grze sprawdzić ponownie.
3. **Koniec wsparcia dema** po wydaniu pełnej gry (zapowiedź w opisie moda).
5. **ImageSharp 3.1.12** (2026-11-26): znane luki (TIFF, ICC, filtry; nie WebP) poprawione dopiero w 3.2.0, które wymaga klucza licencyjnego Six Labors przy buildzie Release. VS pokaże ostrzeżenia NU1902/NU1903. Paczka od osób trzecich z celowo spreparowanym profilem ICC mogłaby wywrócić dekodowanie (łapane w try/catch, zdjęcie z gry). Przejście na 3.2+ = darmowy klucz od Six Labors, jeśli go wydają (do sprawdzenia przez użytkownika).

## Prośby do BlastCoreOS (nie edytujemy tamtego repo)

1. ✔ **Zrobione 2026-11-25: `BlastCoreGameDataAPI`** (auta, wersje ze `StockEngineAvailable`, części, silniki; narzędzi i dekoracji nie ma, OXL czyta je z gry sam). Pierwotna prośba: **Publiczne API danych wiki** (2026-11-25, `site.md` 9.1, treść zadania dla agenta BlastCoreOS: `Docs/Research/Market.md` + raport z 2026-11-25). Klasa tylko do odczytu na danych `BlastCoreWikiData` (dziś `internal`), typy proste, wzór kontraktu `BlastCoreBankAPI`. Potrzebne: znacznik gotowości; auta z wersjami (indeks, nazwa wersji, rocznik, napęd, silnik fabryczny, **czy silnik jest w tej wersji gry (`HaveDLC`)**, waga, wartość, kolory); części (ID, nazwa, cena, grupa i nazwa sklepu, marka, karoseria, `Availability`, auta, do których pasuje); warianty silników (ID, rodzina, nazwa, auta, liczba części, czy kupowalny w demie).
2. **`BlastCoreGameCarVersion` bez kół** (2026-11-25, G48): `StockEngineAvailable` sprawdza tylko silnik, a zapis psują też felgi i opony spoza wersji gry. OXL sprawdza je sam (`MarketCatalog.VersionsWithMissingWheels`, czyta config z `CarBundleLoader`). Prośba: pole `Available` (silnik + felgi + opony) albo `Rim`/`Tire` z flagą dostępności w publicznym API; wtedy OXL usunie własne sprawdzenie.
3. ✔ **Kalendarz: dostawy z modów** (2026-11-25): BlastCoreOS dodał `BlastCoreCalendarAPI`, OXL rejestruje dostawcę (`OXLSite.CalendarParcels`), test G51. Pierwotnie: Kalendarz BlastCoreOS czyta tylko `DeliveryManager`, a kurier OXL trzyma paczkę do dnia dostawy. Treść zadania: `Docs/ForOtherRepos/BlastCoreOS_calendarProvider.md`. Po zrobieniu: OXL rejestruje dostawcę z `ItemListingSystem.GetPendingParcels`. Do tego czasu paczkę widać w kalendarzu od północy dnia dostawy, a wcześniej komendą `oxl_parcels`.

## Nowa strona: do zrobienia (2026-11-25)

1. **Sprzątanie starego UI aut** (strona auta na nowej stronie zrobiona 2026-11-25, `site.md` 12.5; stare `oxl.com/listing/{id}` i `oxl.com/listings` nieużywane przez nowe linki). Dawny opis: strona ogłoszenia auta w stylu `OXLSite` (galeria: paczka zdjęć albo zdjęcie z gry ze znaczkiem, szczegóły, sprzedawca, kupno). Potem usunąć starą listę `oxl.com/listings`, stare strony `oxl.com/listing/{id}` i nieużywane części `OXLPanel.cs` / `OXLFilterPanel.cs` / `FilterOptionsBuilder.cs`, grafiki kategorii z `resources/` (sprawdzić, czy jeszcze potrzebne).
2. ✔ (2026-11-25, `site.md` 12.4) Generator aut bierze wszystkie bezpieczne wersje z `MarketCatalog`. Zostało: `CarDefs`, `car_spec.json` i `ToGameId` z mapą 5 starych aut jako wzbogacenie; przy przejściu na pełną grę rozważyć ich usunięcie. Zdjęcia z paczek nie są jeszcze pokazywane na nowej stronie auta (tylko STOCK PHOTO).
3. **Zapisy per zapis gry:** `listings.dat` i `items.dat` są wspólne dla wszystkich zapisów; wzór BlastCoreOS `Profiles/Profile{slot}/`.
4. `oxl_end_all` czyści tylko auta; dodać czyszczenie ogłoszeń przedmiotów (albo osobną komendę).

## Przebudowa scammera (2026-11-26)

1. ✔ Wdrożone 2026-11-26 (`Docs/Design/scammer.md`, goale G62–G66). Zostaje: zestawienie przy teście, czy L3 nie jest za trudny albo za łatwy.

## TODO od użytkownika (2026-11-26, na koniec sesji)

1. **Silniki rzadko.** Szansa na ogłoszenie silnika bardzo mała: silnik to okazja, a dobry silnik (wysoki stan, niska cena) to "okazja z okazji", top z topów. Dziś `EngineShareOfEnginesParts = 0.30` w `ItemListingSystem.cs`.
2. **Części nie tylko silnikowe.** W "Engines & Parts" także inne drogie części, np. rura wydechowa, zawieszenie, karoseria (dziś próg "droga część" z `MarketCatalog.ExpensivePercentile`; sprawdzić, jakie grupy części faktycznie trafiają).
3. **Ekonomia cen aut.** Przyjrzeć się cenom kupna na OXL względem ceny sprzedaży auta w grze (po naprawie), tak żeby gracz realnie zarabiał, a Easy / Normal / Hard dawały odczuwalnie różny, ale realny zysk. Potrzebny pomiar w grze (skrypt REPL: wartość auta po naprawie vs cena ogłoszenia wg archetypu).
4. **Odpowiedź sprzedającym przez maila** (może później): gracz odpisuje na mail sprzedawcy (`OXLMail`, "Message seller"), np. negocjacja ceny. Sprawdzić, czy `BlastCoreMailAPI` pozwala odpowiadać; jeśli nie, prośba do BlastCoreOS (`Docs/ForOtherRepos/`).
5. **Pole wyszukiwania** na stronie OXL (`OXLUi.TextField`, `OXLSite.SearchBar`) do poprawy: wygląd i obsługa wpisywania (do ustalenia z użytkownikiem, co dokładnie nie gra; zrzut w `Docs/screens/`).
