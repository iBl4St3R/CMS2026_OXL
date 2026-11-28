# Nowa strona OXL (układ jak serwis ogłoszeń OLX): projekt

> Stan: **ZAAKCEPTOWANY 2026-11-25** z odpowiedziami z sekcji 9 (wygrywają nad wcześniejszymi punktami). Wygląd: wolna ręka agenta. Stary wygląd (ciemny, zielony akcent): `Docs/screens/ss2.png`. Zastępuje etap 4 z `rebuild.md` i rozszerza etap 3 (generator) o nowe kategorie.
> Wzorujemy się na **układzie** serwisu ogłoszeń (pasek, wyszukiwarka, kafle kategorii, karty ogłoszeń, strona ogłoszenia z kartą sprzedawcy). Bez logo, nazwy, kolorystyki 1:1 i grafik OLX: nazwa i marka zostają "OXL".

## 1. Cel

1. OXL wygląda i działa jak typowy serwis ogłoszeń z używanymi rzeczami, tylko w świecie mechanika: auta, silniki i drogie części, narzędzia i dekoracje.
2. Każda kategoria ma te same elementy: lista kart, filtry po lewej, sortowanie, strona ogłoszenia, sprzedawca z archetypem (kłamie w opisie, nie na zdjęciu).
3. Strona mieści się w oknie przeglądarki BlastCoreOS przy każdej rozdzielczości: wysokość treści przez `ctx.SetContentHeight`, przewijanie zamiast ucinania (dziś dolny wiersz wystaje poza ekran).

## 2. Kategorie (zakładki)

| Zakładka | Co sprzedajemy | Dostawa |
|---|---|---|
| **Cars** | Używane auta (jak dziś): 4 archetypy × 3 poziomy, zdjęcia z paczek (etap 2). | Spawn na parkingu (jest). |
| **Engines & Parts** | Całe silniki (`EngineGroupItem`, warianty z `EnginePartsLookup`) i drogie części z katalogu gry (np. skrzynie, wały, turbo, felgi; próg ceny do ustalenia, 8.3). | Do ekwipunku albo magazynu (do zbadania, 6.2). |
| **Tools & Decor** | Narzędzia garażu i dekoracje (`CustomizableItem`, `DummyEngineItem`: banery, pojemniki, narzędzia). | Do ekwipunku (gra trzyma je zawsze przy graczu, BlastCoreOS `decisions.md`). |

## 3. Układ stron (szkic)

```
+--------------------------------------------------------------------------+
| OXL      [ Search cars, engines, tools...      ] [All categories v] [Go] |  pasek górny
|          Cars | Engines & Parts | Tools & Decor | My purchases           |  zakładki
+--------------------------------------------------------------------------+
| Filters        |  142 listings          Sort: [Newest v]                 |
|  Make [v]      |  +-----------+ +-----------+ +-----------+ +-----------+|
|  Price from/to |  | [photo]   | | [photo]   | | [photo]   | | [photo]   ||  karty 4 w rzędzie
|  Year from/to  |  | Mayen M5  | | V8 AXK5.. | | ...       | | ...       ||
|  Condition     |  | $10 850   | | $4 200    | |           | |           ||
|  Seller rating |  | Aldenmoor | | Wexmoor   | |           | |           ||
|  [Apply]       |  | 3h left   | | 1d left   | |           | |           ||
|                |  +-----------+ +-----------+ +-----------+ +-----------+|
|                |  < 1 2 3 ... >                                           |
+--------------------------------------------------------------------------+
```

Strona ogłoszenia:

```
+--------------------------------------------------------------------------+
| < Back to results                                                        |
| +-------------------------------------+  +-----------------------------+ |
| |                                     |  | $10 850                     | |
| |          [ duże zdjęcie ]           |  | Mayen M5 T3, 2009           | |
| |                                     |  | 151 141 mi  |  Aldenmoor    | |
| +-------------------------------------+  | [ Buy now ]                 | |
| [mini][mini][mini][mini][mini]           +-----------------------------+ |
|                                          | [avatar] Seller nick  3/5   | |
| Description                              | Member since 2019           | |
| "Selling my 2009 Mayen..."               | 12 listings                 | |
|                                          +-----------------------------+ |
| Details: Year, Mileage, Color, Engine, Drive, Condition (as stated)      |
+--------------------------------------------------------------------------+
```

1. Strona główna: wyszukiwarka, 3 duże kafle kategorii, "Recently added" (8 kart).
2. Lista: filtry po lewej (zależne od kategorii), karty z miniaturą, ceną, lokalizacją i czasem do końca.
3. Ogłoszenie: galeria, cena i przycisk kupna po prawej, karta sprzedawcy, opis, tabela szczegółów.
4. "My purchases": historia kupionych rzeczy (data gry, cena, sprzedawca, stan rzeczywisty po zakupie). Opcjonalne, 8.5.
5. Adresy: `oxl.com`, `oxl.com/cars`, `oxl.com/parts`, `oxl.com/tools`, `oxl.com/listing/{id}`, `oxl.com/settings` (jedna rejestracja z `HandlesSubpaths`).
6. Wygląd: jasne tło, białe karty, jeden kolor akcentu OXL, odstępy i czcionki z zasad `BlastCoreOS/Docs/Design/ui.md`. Tylko ASCII.

## 4. Źródło danych

1. **Auta:** tylko wersje, które da się zespawnić (biała lista). Wiki BlastCoreOS pokazuje wszystkie `CarConfigs`, ale część nie wczytuje się poprawnie (carspawner, błąd silników G12). Listę ustalamy skryptem (spawn każdej wersji i sprawdzenie silnika), zapisujemy w pliku moda.
2. **Silniki i części:** katalog gry (`GameInventory.PartPropertyList`: ID, nazwa, cena, grupa sklepu) i `EnginePartsLookup`. Te same dane czyta wiki BlastCoreOS (`BlastCoreWikiData`), ale to klasa `internal`. Dwie drogi (8.1):
   - A. **Publiczne API w BlastCoreOS** (np. `BlastCoreWikiAPI`: części, silniki, auta tylko do odczytu, typy proste). Jedno źródło prawdy, OXL nie dubluje kodu. Zmiana w BlastCoreOS = wpis w `BACKLOG.md`, robi ją agent BlastCoreOS.
   - B. OXL czyta katalog gry sam (mniej danych niż wiki: ID, nazwa, cena, grupa). Niezależne od BlastCoreOS, ale drugi kod do utrzymania.
3. **Zdjęcia:** auta z paczek (etap 2). Silniki, części, narzędzia i dekoracje z miniatur gry (namespace `Thumbnails` w zrzucie, do zbadania), bez sesji zdjęciowej.
4. **Link do wiki:** na stronie części przycisk "Specs on AutoFreaks" (`BlastCoreWebAPI.OpenInBrowser`), zamiast przepisywać dane techniczne.

## 5. Sprzedawcy dla nowych kategorii

1. Te same 4 archetypy. Dla części i silników kłamstwo dotyczy stanu ("like new" przy 45%), dla narzędzi i dekoracji ceny (dealer drożej, scammer "bargain" w złym stanie albo uszkodzone).
2. Stan rzeczywisty ustawiany przy dostawie (`Condition` przedmiotu), tak jak dziś zużycie auta.

## 6. Do zbadania przed implementacją (skrypty REPL)

1. G12: skąd silniki, których gra nie wczytuje (biała lista aut, 4.1).
2. Jak dodać przedmiot do ekwipunku/magazynu z kodu: `Item`, `EngineGroupItem` (89 części w środku), `CustomizableItem`, z ustawionym stanem. BlastCoreOS przenosi przedmioty między kontenerami, więc wzór może tam być.
3. Lista narzędzi i dekoracji do kupienia w demie 2027 i ich ceny (sklep gry).
4. Miniatury przedmiotów (`Thumbnails`).

## 7. Etapy (propozycja)

1. ✔ Badanie 6.1–6.4 (G12–G16, `Docs/Research/Market.md`), decyzja 8.1 = A (`BlastCoreGameDataAPI`).
   - ✔ E1 (2026-11-25): `MarketCatalog`, `OXLDelivery`, `OXLMail`, ustawienie `instant_delivery`; G17–G20 zaliczone.
   - E3 (2026-11-25): nowa strona `OXLSite` pod `oxl.com` (home, zakładki, filtry w adresie, ogłoszenie przedmiotu z kupnem, ustawienia, wyszukiwarka), stara strona główna usunięta; auta: nowa siatka, szczegóły jeszcze na starej stronie; test G26–G30.
   - E2 (2026-11-25): generator ogłoszeń przedmiotów `ItemListingSystem` (archetypy: Honest, Wrecker, Dealer, Scammer; Scammera nie ma w narzędziach i dekoracjach, bo nie mają stanu; perełki 4%; do 14 ogłoszeń Engines & Parts, 30% to silniki; do 18 Tools & Decor; życie 10–48 h gry), kupno z konsoli do czasu nowych stron; test G21–G24.
2. Zdjęcia aut (etap 2 z `rebuild.md`, bez zmian) i biała lista wersji.
3. Generator: kategorie Engines & Parts, Tools & Decor (dane, ceny, archetypy, dostawa z kontraktem "pieniądze po dostawie").
4. Nowa strona: układ z sekcji 3, ustawienia w `oxl.com/settings`, wysokość treści.

## 8. Pytania do użytkownika

1. Dane części i silników: A (publiczne API BlastCoreOS) czy B (OXL czyta grę sam)? Zalecam **A**.
2. Strona z układem OLX ma **zastąpić** obecny wygląd w całości (stary `OXLPanel` do kosza po przeniesieniu logiki), czy przerabiamy obecny kod krok po kroku? Zalecam **nowy kod stron**, logika ogłoszeń i kupna zostaje.
3. Które części liczą się jako "drogie": próg ceny (np. > 500), konkretne grupy (silnik kompletny, skrzynia, turbo, felgi), czy obie?
4. Dostawa części i narzędzi: od razu do ekwipunku, czy z czasem dostawy (godziny gry) i mailem "Your parcel has arrived" (`BlastCoreMailAPI`)?
5. "My purchases" (3.4) potrzebne?
6. Ustawienia OXL: podstrona `oxl.com/settings` (zalecam) czy okno ustawień BlastCoreOS?

## 9. Decyzje użytkownika (2026-11-25)

1. **Dane: A.** Części, silniki i auta z publicznego API BlastCoreOS (tylko odczyt, typy proste). Prośba w `BACKLOG.md`, zmianę robi agent BlastCoreOS.
2. **Nowy kod stron**, wygląd według uznania agenta ("zrób to naprawdę fajnie"). Logika ogłoszeń, sprzedawców i kupna zostaje.
3. **Mod czyta dane z gry, nie trzyma ich w sobie.** "Droga część" = decyduje cena (próg liczony z rozkładu cen katalogu w czasie gry, nie stała lista). Zero list ID, aut i wersji w kodzie: po aktualizacji gry nowe auta, części i dekoracje pojawiają się w OXL same. Dotyczy też istniejących statycznych danych (`PartCatalog.cs` z 5 autami, `car_spec.json`, `color_map.txt`): do zastąpienia danymi z gry w etapie generatora.
4. **Ogłoszenia jak w życiu:** ktoś sprzedaje cały silnik, bo wpadł w spiralę kredytową, robi upgrade, rozbił tył auta i zostało mu sprawne serce. Perełki trafiają się rzadko. Historia sprzedawcy w opisie (archetyp + powód sprzedaży).
5. **Dostawa przez system przesyłek gry** (`DeliveryManager.OrderDelivery`, paczka jak ze sklepu). W `oxl.com/settings`: domyślnie dostawa po czasie (złudzenie paczkomatu: kilka godzin gry), opcja "Instant delivery".
6. **Bez "My purchases".** Po każdym kupnie mail (`BlastCoreMailAPI`) jak z serwisu aukcyjnego: co, od kogo, za ile, kiedy przyjdzie. Poczta BlastCoreOS sama trzyma historię.
7. **Ustawienia w `oxl.com/settings`.** Zasada: ustawienia portali są na ich stronach.
8. **Błąd G12 (silniki przy wczytaniu):** wersje DNB Censor 1 (LIQD-X4, `engine25_v8_head1_intake3_NA_4x4`) i 2 (5000, `engine25_v6_231_scharged_4x4`) psują zapis; obie są w `EnginePartsLookup`. **Filtr z danych gry (G13): wersja auta tylko gdy `GameInventory.HaveDLC(silnik fabryczny)` = True** (`Docs/Research/Market.md`). Zgodność ze starymi zapisami niepotrzebna (`rebuild.md` 6). Dodatkowo po spawnie OXL sprawdza auto i wersję, która nie przejdzie, zapamiętuje w pliku moda jako zablokowaną (samouczenie, bez listy w kodzie).

## 10. Wersja podstawowa bez paczek zdjęć (decyzja użytkownika 2026-11-25)

1. **OXL działa bez paczek MEDRES/LOWRES.** Zdjęcia z gry: auto `ThumbnailsProvider.GetCarThumb(carId, wersja)`, części, silniki, narzędzia i dekoracje `TryGetThumbnail(id)` (te same, które pokazuje wiki AutoFreaks). Sprite rysowany wprost przez framework (`UIRuntime.SetBackgroundSprite`, >= 0.4.0), bez kopiowania tekstur.
2. **Paczki zdjęć są opcjonalnym dodatkiem** (użytkownik je też wyda): gdy paczka ma zdjęcia danego auta, wersji, koloru i stanu, ogłoszenie pokazuje prawdziwe zdjęcia; gdy nie ma, zdjęcie z gry.
3. **Zasada "sprzedawcy kłamią w opisach, nie na zdjęciach" zostaje:** zdjęcie z gry pokazuje nowe auto/przedmiot, więc na stronie ma wyraźny znaczek **"Stock photo"** (jak w prawdziwych ogłoszeniach, gdzie sprzedawca wrzuca zdjęcie katalogowe). Gracz wie, że zdjęcie nie mówi nic o stanie; stan zna z opisu i szczegółów. Prawdziwe zdjęcia z paczki dalej muszą odpowiadać rzeczywistemu stanowi.
4. Kolor auta bez paczki: nazwa i próbka koloru w szczegółach (fabryczne kolory wersji z `BlastCoreGameCarVersion.Colors`).

## 11. Wygląd jak OLX, nie jak sklep (decyzja użytkownika 2026-11-25, zastępuje sekcję 3)

Pierwsza wersja E3 (`Docs/screens/ss3.png`, `ss4.png`) wyglądała jak sklep z nowymi rzeczami (kafelki, nazwy z katalogu, zaokrąglone karty). Wzór: zrzuty OLX `ss5.png` (lista), `ss6.png`, `ss7.png` (ogłoszenie), `ss8.png` (karty "Zobacz też", stopka).

1. **Tytuły pisze sprzedawca**, nie katalog: styl archetypu (Dealer: WIELKIE LITERY i gwiazdki, Scammer: "LIKE NEW!!! must go today", Wrecker: "sold as is", Honest: rzeczowo). Nazwa z gry tylko w parametrach.
2. **Lista = wiersze**: zdjęcie po lewej (znaczek STOCK PHOTO zostaje, zasada 5), tytuł, pod nim "miasto | Today at 06:12", parametry (rocznik i przebieg / stan deklarowany), cena po prawej. Nad listą "We found N listings", pasek grup z liczbami (marki aut, Engines / Parts, Tools / Decorations), sortowanie i przedział ceny jako linki.
3. **"FEATURED"**: część ogłoszeń dealerów jest wyróżniona (płacą za promocję) i stoi na górze przy sortowaniu "Newest".
4. **Ogłoszenie**: "Back" i ścieżka kategorii, duże zdjęcie; po prawej data dodania, tytuł sprzedawcy, cena, "Buy now" (ciemny przycisk) i "Message seller" (obramowany: sprzedawca odpisuje mailem w stylu archetypu, scammer pogania i każe płacić z góry); karta "PRIVATE PERSON" albo "BUSINESS" (dealer) z "On OXL since", "Last online"; parametry jako pigułki; opis; lokalizacja z mapką; ramka "Consumer rights: purchases from private sellers are not protected by consumer law."
5. **Kolory**: ciemny morski pasek górny, jasnoszare tło, białe prostokąty z małym zaokrągleniem, tekst ciemny morski, główny przycisk ciemny; zieleń OXL tylko w logo. Bez logo, nazw i grafik OLX.
6. Strona główna: wyszukiwarka, trzy kategorie, "Featured listings" w kartach jak "Zobacz też" (zdjęcie, tytuł, cena, miasto i data).

## 12. Filtry i auta z danych gry (prośba użytkownika 2026-11-25)

1. **Pełna gra doda ponad 100 modeli**, więc lista aut ma porządny podział: pasek marek z liczbami (zawija się), w "More filters" model wybranej marki, rocznik od (co 5 lat z danych), maks. przebieg, min. moc, napęd, typ sprzedawcy, stan deklarowany, cena. Sortowanie aut: najnowsze, cena, rocznik, przebieg, moc, koniec aukcji.
2. **"More filters" = okno na wierzchu strony** (przyciemnione tło), każdy wybór od razu zmienia adres i liczbę wyników ("Show N results"), "Clear filters". Aktywne filtry nad listą jako ciemne pigułki z "X". Stan okna w adresie (`more=1`), jak reszta filtrów.
3. **Moc i moment z gry**: `PhysicsEngineData.GetHP` / `GetMaxTorque` silnika fabrycznego wersji (`MarketCatalog`), napęd, skrzynia, waga, roczniki z `BlastCoreGameDataAPI`.
4. (Uzupełnione przez 19: bez paczek 2026 i `car_spec.json`.) **Generator aut bierze wszystkie bezpieczne wersje z gry** (`MarketCatalog`): Bolt Nevada i Lacroix Modique R są w OXL bez zmian w kodzie. Paczki zdjęć (MEDRES/LOWRES) i `car_spec.json` zostają tylko jako wzbogacenie 5 aut z 2026 (ceny, kolory, zdjęcia); auto bez nich: cena z wartości wersji w grze (10–34% `carValue`), kolor fabryczny wersji.
5. **Strona auta na nowej stronie** (`oxl.com/car/{id}`): parametry z gry jako pigułki (wersja, rocznik, przebieg, kolor po nazwie, silnik, moc, moment, napęd, skrzynia, waga, stan deklarowany, tablica), opis sprzedawcy, "Buy now" (`OXLPanel.TryBuyCar`, pieniądze po spawnie), "Message seller", sprzedawca, lokalizacja, prawa konsumenta, "See also".

## 13. Kurier OXL i zdjęcie w mailu (prośba użytkownika 2026-11-25, zmienia 9.5)

1. Dostawa domyślna: **jutro albo pojutrze** (losowo), o losowej godzinie dostaw gry. OXL trzyma paczkę u siebie (linie `P` w `items.dat`) i od północy dnia dostawy nadaje ją przez system przesyłek gry; po przespanej godzinie na najbliższą rundę; nieudane nadanie ponawia co godzinę gry. Opcja "Instant delivery" bez zmian.
2. Pieniądze schodzą przy zamówieniu (paczka jest u kuriera OXL i nie ginie), tak jak w prawdziwym sklepie.
3. Mail "Order confirmed" ma zdjęcie kupionej rzeczy (miniatura z gry zapisana jako PNG w `Mods/CMS2026_OXL/Resources/Images/mail/`, znacznik `[img=...]` poczty BlastCoreOS) i termin ("Expected tomorrow at 14:00").

## 14. Poprawki po teście E4 (użytkownik 2026-11-25)

1. **Wszyscy sprzedawcy są prywatni**: bez "Business" w pigułkach i karcie sprzedawcy, bez filtra sprzedawcy. Archetyp Dealer zostaje (styl tytułu, FEATURED), "Consumer rights" zostaje.
2. **Odmowa kupna = okno** na wierzchu strony (pełny parking, brak środków, kurier niedostępny); przy braku środków przycisk "Go to Redline ATM".
3. **Ramka DELIVERY nad lokalizacją** z terminem przed kupnem ("Tomorrow after 16:00"); termin liczony z ID ogłoszenia, kurier dowozi dokładnie wtedy. Auta: "Right after purchase".
4. **Mail**: zdjęcie na samym dole, najwyżej 360 px szerokości.
5. **Czytelność**: zwykły tekst LiberationSans (ostrzejszy niż cienki Rubik na monitorze w grze), pogrubiony Rubik; ciemniejszy kolor drugorzędny `#3E5458`; kolorowe słowa w opisach sprzedawców przyciemnione do czytelnych na białym tle, szarości jako zwykły tekst.
6. **Geargle** pokazuje ogłoszenia OXL (`BlastCoreWebAPI.RegisterSearchProvider("oxl")`, zakładka "OXL").
7. **Grafiki** z `geminiPromptOXL.md`: `oxlLogo.png`, `oxlWordmark.png`, `oxlMailBanner.png` w `Resources/Images`; bez nich stary wygląd.
8. Moc i moment: `GetHP`/`GetMaxTorque` dawały 0 (G36), zapas z krzywej `GetTorque` i `CalculateHP`.

## 15. Kupno auta w toku (użytkownik 2026-11-28)

1. Dostawa auta trwa do kilku sekund (przy zajętym parkingu ok. 6 s, bo OXL próbuje kolejnych miejsc). W tym czasie strona auta pokazuje okno "Delivering your car... Please wait" bez przycisków, które blokuje drugi klik.
2. Sukces: lista aut z zielonym komunikatem. Porażka (np. parking pełny): gracz zostaje na stronie auta, okno "You cannot buy this car" z powodem; ogłoszenie wraca, nic nie pobrane.
3. Zdjęcie w mailu po kupnie: 288 px szerokości (było 360).

## 16. Zakładka Parts: wszystkie części auta (użytkownik 2026-11-28, zmienia 9.3)

1. Zakładka "Engines & Parts" nazywa się **"Parts"**: części do auta szeroko pojęte (karoseria, drzwi, zawieszenie, wydech, hamulce, skrzynie, elektronika z modułem ABS, wnętrze, felgi, opony). **Cały silnik to rzadkość**: ok. 5% ogłoszeń zakładki (było 30%).
2. Katalog z danych gry (`BlastCoreGameDataAPI.GetParts`), bez listy części w kodzie: nowe auta i części po aktualizacji gry wchodzą same. Pomijane: cena 0 i 25% najtańszych (próg liczony z cen w grze, w demie $70; było: tylko 10% najdroższych).
3. Grupy w pasku filtrów z danych sklepu gry: Body (`IsBody`), Electronics (`ElectronicsShop`), Interior (`InteriorShop`), Engine parts (grupa `Engine`), reszta z nazwy grupy sklepu (Suspension, Exhaust, Brakes, Gearbox, Rims, Tires). Nieznana grupa z aktualizacji dostaje swoją nazwę, nic się nie wywraca.
4. Losowanie: najpierw grupa z wagą pierwiastka z liczby części w grupie, potem część z grupy, żeby 155 elementów karoserii nie zasłoniło 3 opon.
5. Dostawa: każda część jako `Item(id)` kurierem gry (sprawdzone 2026-11-28: maska, drzwi, felga, opona, moduł ABS, tłumik, fotel przyjęte przez `DeliveryManager`).
6. **Opony i felgi mają rozmiar** (użytkownik 2026-11-28: opona "0/0R0" warta 0, felga 0"): rozmiary z configów aut gry (`[3_wheels]`, `[4_wheels_rear]`: `rimSize`, `wheelWidth`, `tireSize`), wybór stały dla ogłoszenia (ziarno = ID), widoczny na liście i w pigułce "Size", ten sam w paczce (`Item.WheelData`). ET felgi 20–40 (config go nie ma).
7. **Silnik losowany po rodzinie** (I4 B4FM, I4 AQ4C, V6 322BZ, V8 820MAL, V8 D.vik): demo ma 11 wariantów V6 na 17, bez tego prawie zawsze V6.
8. **Cały silnik siada na stojaku** (2026-11-28): paczka ma jeden przedmiot, silnik złożony jak silnik wyjęty z auta (ID = gniazdo z `EnginePartsPaths`, `NormalID`, `MountObjectData` ze ścieżką gniazda). **Bez części luzem** (użytkownik 2026-11-28: "nie podoba mi się"). Części montowane tylko przy aucie (skrzynia, rozrusznik, początek wydechu) nie wchodzą w skład silnika, tak jak w grze przy wyjmowaniu z auta; OXL zna je z modeli w scenie (`OXLEngineParts`), jeszcze nieznane idą w silniku i stojak je pomija.

## 17. Ogłoszenia osobno dla każdego zapisu gry (ZADANIE pkt 3, 2026-11-28)

1. `listings.dat` (auta) i `items.dat` (przedmioty, paczki u kuriera OXL, pułapki scammera) leżą w katalogu zapisu gry BlastCoreOS: `Mods/BlastCoreOS/Profiles/Profile{slot}/Session/OXL/` (`BlastCoreProfileStorage.DataRoot`). Zapis gry kopiuje `Session` do `Saved` (cały katalog, z OXL), wyjście bez zapisu porzuca zmiany, jak pulpity i bank BlastCoreOS.
2. Nowa kariera = brak plików = świeże ogłoszenia (generator od razu do celu z ustawień). Inny zapis = jego ogłoszenia.
3. Dopóki BlastCoreOS nie poda slotu (pierwsze klatki garażu, menu), OXL nie generuje, nie czyta i nie zapisuje ogłoszeń (`OXLSaveSlot`), żeby nic nie trafiło do poprzedniego zapisu. Po podaniu slotu: wczytanie ogłoszeń aut i przedmiotów tego zapisu, czas gry kotwiczony na nowo.
4. Wspólne dla wszystkich zapisów: ustawienia (`CMS2026_OXL.cfg`) i wiedza o grze (`engine_parts.txt`). Stare wspólne `Mods/CMS2026_OXL/listings.dat` i `items.dat` nie są już używane (wydanie 2027 = czysta instalacja, `rebuild.md` 6).
5. Ogłoszenia aut zapisują się od razu po pierwszym wygenerowaniu (gra zatwierdza tylko pliki już na dysku; test 2026-11-28: nowa kariera miała aukcje aut tylko w pamięci). BlastCoreOS potwierdził kontrakt (`modules.md`, "Dane modułów per zapis gry"): pisać od razu po zmianie, nie trzymać otwartych plików.
9. **Ceny silników** (użytkownik 2026-11-28: "5005 to placeholder"): cena nowego = suma cen nowych części (jak wycenia gra), widełki ogłoszenia: od 55% wartości w prawdziwym stanie (gra odkupuje za 50%, więc bez darmowego zysku) do 95% nowego. Silnik w ok. 12% ogłoszeń części, tańsze częściej (waga (najtańszy / cena)^2 dla rodziny i wariantu: I4 AQ4C ok. 42%, V8 po ok. 10%); scammer przy silnikach z 1/5 swojej wagi; uczciwy sprzedaje zwykle silnik w stanie 35–85%, co 10. prawie nowy (88–98%).

## 18. Stopka (użytkownik 2026-11-28)

Stopka jak w oficjalnym serwisie: linki, "Supported by" z logo banku Redline (plik BlastCoreOS `resources/image/redlineLogo.png`, klik otwiera `redline.bank`; bez pliku napis "Redline Bank") i "(c) OXL Marketplace. All rights reserved.". Bez tekstów w stylu porad o kłamiących sprzedawcach.

## 19. Tylko dane gry, bez plików o autach (użytkownik 2026-11-28, zmienia 12.4)

1. Mod buduje aukcje wyłącznie z danych gry: auto, wersja, rocznik, kolory, cena bazowa (`carValue` wersji), części, klasyfikacja zużycia. **Żadnych `car_spec.json`, list aut w kodzie (`CarDefs`), ręcznych map ID (`ToGameId`) ani metadanych starych paczek 2026.** Nowe auto z aktualizacji gry działa bez zmian w modzie.
2. Zdjęcia: tylko paczka 2027 (`CarPhotoPack`, `CarPhotos/{carId}/...`); auto bez paczki ma zdjęcie z gry ze znaczkiem STOCK PHOTO. Paczkę zdjęć użytkownik dostarczy później.
3. Usunięte 2026-11-28: `CarSpecLoader.cs` (z rejestrem kolorów), `CarPhotoLoader.cs` (paczki `CarImages_LOWRES`/`MEDRES`), `PhotoCondition.cs`, `CarDefs`, ścieżka cen z JSON w `ListingSystem.RollPrice`, pole `ImageFolder`, komenda `oxl_memory`.
