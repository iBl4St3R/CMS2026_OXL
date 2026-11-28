# Scammer: przebudowa (projekt)

> Stan: **zaakceptowane i wdrożone 2026-11-26** (auta: `ScammerLies.cs`, przedmioty: `ItemListingSystem.cs`; goale G62–G66). Źródło logiki cen, uczciwości i zużycia dalej `archetypes.md`; ten plik opisuje, jak scammer ma **wyglądać** dla gracza na każdym poziomie. Punkty numerowane, odpowiadaj "3.2 ok", "3.2 inaczej: …".

## 1. Co się nie zmienia

1. Kupione auto to zawsze wrak (`ApplyWreckerWear`: `SalvageCar`, części 0,02, spuszczone płyny). Oszustwo jest w ogłoszeniu, nie w dostawie.
2. **Zdjęcia: ładne (folder 60100 z paczki)**, nie zdjęcie z gry (decyzja użytkownika 2026-11-26, zrobione w `CarPhotoPack.ConditionFolder`). Zdjęcie z gry ze znaczkiem "Stock photo" wyglądało dziwnie. Wyjątek od zasady 5 ("kłamią w opisach, nie na zdjęciach") tylko dla scammera; bez paczki zdjęcie z gry jak u wszystkich.
3. Tytuły w stylu "LIKE NEW!!! must go today" (`site.md` 11.1) i maile, w których pogania i każe płacić z góry (`site.md` 11.4).

## 2. Poziomy

1. **L1: kompletny "low IQ" oszust, widać od razu.** Tak jak teraz: opis pełen czerwonych flag, ocena 1 albo 2 gwiazdki, cena podejrzana. Gracz, który przeczyta ogłoszenie, nie powinien się nabrać.
2. **L2: dalej dość prosto, ale nie tak oczywiście jak L1.** Mniej czerwonych flag w opisie, cena bliżej rynkowej, ocena 2–3 gwiazdki. **Jedna niezgodność: moc 5x** (decyzja agenta, użytkownik zostawił wybór 2026-11-26): łatwo ją zauważyć, jeśli gracz zna auto. Trzeba przeczytać uważnie.
3. **L3: ogłoszenie wygląda porządnie, ale niektóre informacje się nie zgadzają.** Ocena **3 albo 3,5 gwiazdki** (wcześniej 4–5). Gracz łapie oszusta tylko wtedy, gdy porówna dane ogłoszenia ze zdjęciami i z danymi auta.

## 3. Niezgodności L3 (co się nie zgadza)

Każda niezgodność musi być **sprawdzalna przez gracza** (zdjęcie, dane wersji w grze, wiki AutoFreaks), inaczej to nie trop, tylko losowa kara.

1. **Kolor:** w opisie i parametrze "Color" inny kolor niż na zdjęciach (np. "red", a na zdjęciach srebrny).
2. **Silnik:** opis podaje silnik, którego ta wersja mieć nie może (np. "V6", a auto ma R4). Nazwa z danych gry: fabryczny silnik innej wersji, mocniejszy od prawdziwego (sprzedawca się chwali), nigdy lista w kodzie. Układu cylindrów dane nie podają wprost, więc "inny" znaczy "inna nazwa silnika".
3. **Wersja:** wymyślona nazwa zamiast prawdziwej, nie zmieniona literka. 16 nazw w `ScammerLies.FakeVersions` (np. DEMON, Super XL, GT Ultimate, RS Turbo Plus, Black Edition X, Hyper Sport, Track Monster, Twin Turbo Beast). Lista w kodzie, bo to tekst sprzedawcy, nie dane gry.
4. **Moc:** 5x większa niż w danych wersji (np. 105 HP w grze, w ogłoszeniu 525 HP).
5. Niezgodności pokazane w parametrach na stronie auta (pigułki Version, Power, Engine, Color), na liście (moc) i w opisie: zdanie "Full specification: …" (L3) / "Specs: …" (L2) miesza prawdziwe i fałszywe dane; kolor w tekście opisu też fałszywy. Prawdziwe dane zostają w zapisie ogłoszenia (dostawa, zdjęcia, lakier). Log `[OXL:SCAM]`.

## 4. Wdrożenie (2026-11-26)

1. `CarListing.SellerRating` jest `float` (połówki), zapis `seller_rating=3.5` w `listings.dat` (stare liczby całkowite czytają się bez zmian), w tekstach `RatingText` z kropką.
2. Nowe pola w `listings.dat`: `fake_color`, `fake_engine`, `fake_version`, `fake_hp_mult` (puste / 0 = prawda).
3. Liczba niezgodności L3: **1 albo 2 losowe** z sekcji 3 (decyzja użytkownika 2026-11-26).

## 5. Scammer w przedmiotach (decyzja użytkownika 2026-11-26)

1. Scammerzy sprzedają też silniki, części, narzędzia i dekoracje, ale **łatwiej ich rozpoznać: tylko L1 i L2** (bez L3).
2. Zawsze **niska ocena (1–2 gwiazdki)**, **świeże konto** ("Joined OXL this week" zamiast "On OXL since") i **krótko na OXL** (ogłoszenie wisi 2–6 h gry zamiast 10–48 h).
3. **Przysyłają najtańszy przedmiot gry zamiast kupionego** (`MarketCatalog.Cheapest()`: najtańsze narzędzie albo dekoracja z ceną > 0, z danych gry, np. wiadro albo karton). Mail z potwierdzeniem i kalendarz pokazują kupioną rzecz; prawda wychodzi dopiero przy odbiorze paczki.
4. L1: wielkie litery, "!!!", "nówka" za 15–30% ceny nowego. L2: spokojniejszy tytuł i opis, "używane kilka razy", 40–60% ceny, stan 82–95%.
5. Narzędzia i dekoracje mają scammerów od 2026-11-26 (wcześniej nie, bo bez stanu nie było w czym kłamać; teraz kłamstwem jest sama paczka).
6. **Gracz dowiaduje się przy otwarciu paczki** (decyzja użytkownika 2026-11-28, G73): czerwony komunikat frameworka (`UIPrompt.Error`, 8 s) "You got scammed! {sprzedawca} took your money for {kupione}, but the parcel only had {przyszło}." OXL pamięta ID dostawy gry podmienionej paczki (`OXLScamParcels`, linie "S" w `items.dat`), komunikat pokazuje postfix na `DeliveryManager.TakeFromStorage`.
