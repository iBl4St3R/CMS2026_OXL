# Przebudowa OXL na 2027: decyzje i etapy

> Stan: **zaakceptowane 2026-11-25** (rozmowa z użytkownikiem). Punkty numerowane, odpowiadaj "2.3 ok", "2.3 inaczej: …". Pytania otwarte w sekcji 5.

## 1. Czym jest OXL po przebudowie

1. **Tylko strona w przeglądarce BlastCoreOS.** Panel na klawisz T i przycisk w menu sklepu gry znikają (razem z trybem samodzielnym `OXLPanel`). Powód: jedno UI do utrzymania, spójny wygląd z BlastCoreOS, OXL jako wzorcowy moduł platformy.
2. Twarda zależność od `BlastCoreOS.dll` zostaje (wcześniej pytanie z `Port2027.md` pkt 1).
3. Płatności przez `BlastCoreBankAPI` (konto w banku Redline, nie saldo gry), powiadomienia przez `BlastCoreMailAPI`. Kontrakt kupna bez zmian: pieniądze tylko po udanym spawnie.
4. Ustawienia OXL (trudność, generacja) przenoszą się poza stronę OXL. Gdzie dokładnie (ustawienia BlastCoreOS czy podstrona), ustalimy w etapie 4 (sekcja 5).

## 2. Zdjęcia aut

1. **Sesję zdjęciową robi użytkownik w grze:** jeździ po świecie i zapisuje miejsca (pozycja i obrót auta oraz kamery) do pliku. Fotograf (następca `totalMaker.cs`) przechodzi po miejscach × kolorach × stanach i robi zdjęcia.
2. **Zdjęć 2-3 razy więcej niż potrzeba:** część odpadnie przez przenikanie auta z otoczeniem (overclipping). Użytkownik ogląda każde zdjęcie i kasuje odpady ręcznie.
3. Wniosek dla kodu: **loader i generator nie zakładają ciągłej numeracji ani pełnego kompletu.** Skanują to, co jest w folderze; brak zdjęcia danego koloru/stanu = ta kombinacja nie jest generowana.
4. **Konwersja formatu poza grą** (ImageMagick, `magick`, jest na komputerze użytkownika): gra zapisuje PNG/JPG, skrypt w `Scripts/` konwertuje gotową paczkę. Paczka leży obok plików gry (`Mods/CMS2026_OXL/Resources/…`), nie jest budowana przez Unity.
5. **Format: WebP q80, jedna rozdzielczość 1280x720, bez wariantów low/high** (decyzja użytkownika 2026-11-26). Skrypt `Scripts/convert_photos.sh` (ImageMagick, `-strip -quality 80 -define webp:method=6`). Pomiar na 20 zdjęciach paczki 2027: JPG 4,8 MB, WebP q75 1,4 MB, **q80 1,8 MB (o 62% mniej)**, q85 2,4 MB. Unity (`ImageConversion.LoadImage`) czyta tylko PNG/JPG, więc loader potrzebuje dekodera WebP w modzie (następny krok). Paczka jest opcjonalna: bez niej ogłoszenia mają miniatury z gry ("Stock photo"), które są też podstawą (7.3).

## 3. Etapy

1. **Port techniczny:** `MelonGame` 2027, 42 błędy w `GameBridge.cs` (najpierw skrypty REPL), przebudowa na `BlastCoreOS.dll` 0.2.0, płatności przez bank, usunięcie panelu T i przycisku w sklepie.
2. **Zdjęcia:** zapis miejsc w grze, fotograf na API 2027 (kolory, miejsca, stany, nowe auta Bolt Nevada i Lacroix Modique R), skrypt konwersji, loader odporny na luki (2.3).
3. **Generator ogłoszeń** na nowych paczkach.
4. **Strona OXL:** nowy wygląd z układem serwisu ogłoszeń i 3 kategoriami (Cars, Engines & Parts, Tools & Decor): **projekt `site.md`** (2026-11-25, czeka na akceptację), ustawienia poza OXL, dolny wiersz mieszczący się na ekranie.

## 4. Co znika

- ✔ Zrobione 2026-11-25: klawisz T (w kodzie 0.6.0 już nieczynny), przycisk w menu sklepu, tryb samodzielny `OXLPanel` (zakomentowany `Build`, `Open`/`Close`/`Toggle`, `SetPlayerInput`), haki `CursorManager` w `OXLPlugin`, komenda `oxl_open`. `architecture.md` zaktualizowany; README przy wydaniu (`BACKLOG.md`).

## 5. Pytania otwarte

1. ✔ Format zdjęć (2.5): WebP, rozstrzygnięte 2026-11-26. Dekoder do wyboru przy loaderze (`libwebp`, natywna DLL od Google, licencja BSD, ok. 0,3 MB, z folderu moda).
2. Gdzie ustawienia OXL (1.4): okno ustawień BlastCoreOS (wymaga API w BlastCoreOS = wpis w BACKLOG) czy podstrona `oxl…/settings` w przeglądarce?

## 6. Dane graczy (decyzja 2026-11-25)

Wydanie 2027 to **nowa gra i czysta instalacja OXL**: bez zgodności ze starymi zapisami, `listings.dat` i autami z 2026. Stare ogłoszenia gracz czyści komendą (`oxl_end_all`). Format zapisu ogłoszeń wolno zmienić; w opisie wydania informacja "clean install, new game".

## 7. Sesja zdjęciowa 2027 (decyzja użytkownika 2026-11-25)

1. Użytkownik sam wybiera auto i wjeżdża nim do świata (jak w 2026); miejsca zapisuje od nowa (`photo_spots.cs`, F9), bo w 2027 na ulicach stoją zaparkowane auta.
2. Fotograf (`photo_world.cs`) bierze auto, którym gracz jedzie, i jego wersję; tylko wersje sprzedawane przez OXL (`MarketCatalog`). Kolory fabryczne x 3 przedziały stanu (030, 3060, 60100) x losowe miejsca x ujęcia ze starego fotografa. JPG 1280x720 do `Resources/CarPhotos/{carId}/{cfg}/{kolor}/{stan}/`; konwersja do WebP poza grą.
3. Miniatury z gry ("Stock photo") działają bez zarzutu (użytkownik 2026-11-25), zostają jako podstawa; paczka jest dodatkiem.
4. **Kurz tylko w 030** (użytkownik 2026-11-25, po pierwszych zdjęciach): auta w stanie 30% i wyżej są umyte przed zdjęciami, brudne są tylko wraki. Pierwsze zdjęcia: kadr, tło, brak UI i kierowcy (chowany `Man 3(Clone)/Character`, opony `rim_…/tire` zostają) w porządku.

## 8. Paczka na stronie (decyzje 2026-11-26)

1. **Dekoder: ImageSharp 3.1.12** (czysty .NET, `UserLibs/SixLabors.ImageSharp.dll`, ok. 2 MB, PackageReference w csproj, build kopiuje do `UserLibs`). Natywny `libwebp` odpadł: Google wydaje dla Windows tylko statyczne `.lib`, a na komputerze nie ma kompilatora C. ImageSharp 3.2+ wymaga przy buildzie Release klucza licencyjnego Six Labors (`sixlabors.lic`), 3.1.12 nie. Licencja: Six Labors Split License, dla osoby prywatnej z przychodem < 1 mln USD to Apache 2.0 (przy wydaniu dołączyć `LICENSE` ImageSharp). Znane luki 3.1.x dotyczą TIFF, ICC i filtrów, nie dekodera WebP; nasze pliki mają usunięte metadane (`-strip`). Pomiar poza grą: 28 ms pełne zdjęcie, 15 ms miniatura 320 px.
2. Dekodowanie w wątku tła, tekstura Unity powstaje w `CarPhotoPack.Tick()` na głównym wątku (najwyżej 2 na klatkę). Cache: 12 pełnych zdjęć i 96 miniatur. Brak DLL = paczka wyłączona, wszystko na zdjęciach z gry.
3. **Folder stanu wg karoserii** (`BodyCondition`: < 0,30 → 030, < 0,60 → 3060, reszta 60100), bo karoserię widać na zdjęciu (zasada 5). **Scammer zawsze 60100** (decyzja użytkownika 2026-11-26, zastępuje pierwszą wersję z "Stock photo", która wyglądała dziwnie): ładne zdjęcia, przyjeżdża wrak (`scammer.md` 1.2).
4. Kolor ogłoszenia z paczki = kolor folderu (`#RRGGBB` z `AllowedColors` gry), więc dostawa trafia w ten sam lakier. Losowane tylko kolory, które mają zdjęcia w potrzebnym stanie; w opisie sprzedawcy nazwa koloru ("silver"), nie hex.
5. Jedno ogłoszenie = zdjęcia jednego miejsca (`spotNNN`); miejsca z co najmniej 2 ujęciami mają pierwszeństwo. Ścieżki w `listings.dat` względne do `Resources` (`CarPhotos/...`); usunięte pliki strona pomija, gdy nie zostanie żaden, pokazuje zdjęcie z gry.
6. Strona: miniatura na liście i kartach (bez znaczka), na stronie auta galeria ze strzałkami, licznikiem i paskiem miniatur pod zdjęciem (kliknięcie pokazuje zdjęcie, oglądane w ciemnej ramce; tylko zdjęcia z paczki), sąsiednie zdjęcia ładowane z wyprzedzeniem.
