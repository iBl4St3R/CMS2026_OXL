# Goale do sprawdzenia w grze

## Zasady

- Jeden goal = jedno lub dwa zdania: co zrobić w grze i co ma się stać.
- `UA:` na końcu = potrzebna odpowiedź użytkownika (wpisuje ją po dwukropku). `OUA:` = wystarczy `MelonLoader/Latest.log` (agent sprawdza grepem; użytkownik może dopisać uwagi).
- Numeracja ciągła od G1, numerów nie używamy ponownie. Nowy goal dostaje "Następny numer" i podbija go.
- Goal działa: usuń go, wniosek do `Docs/Design/` albo `Docs/Research/`. Nie działa: poprawka z nowym goalem albo wpis w `Docs/BACKLOG.md`. Brak danych: zostaje.
- Przed testem: zamknij grę, zbuduj w VS (DLL kopiuje się sam), uruchom grę.

Następny numer: **G67**

## Etap 1: port na 2027, bank, tylko przeglądarka (2026-11-25)

(G2 zaliczony 2026-11-25: OXL ładuje się w 2027, strona działa, 3 kupna `SpawnResult: Success`. G1 nie przeszedł: `bodyParts=0` pod obiektem auta, a `CarInfoData` z interopu to kopia (dla każdego auta te same "dane" = nagłówek obiektu), więc zużycie blach i przebieg się nie nakładały. Poprawka: `GameCarAccess.cs`, test G6–G8. G3–G5 zastąpione przez G7–G10.)

(G6–G9 zaliczone 2026-11-25 z logu: 21–27 `CarBodyPart` na auto ze sceny, przebieg i rok zapisane i odczytane zgodnie z ogłoszeniem w 7 kupnach, Wrecker/Scammer `27 carParts → 0.02`, bank: wpłata 200 000, każde kupno jedno `Charge oxl`. Wnioski w `Docs/Research/Port2027.md` pkt 6.)

(G12 wyjaśniony 2026-11-25: brakujące silniki to fabryczne silniki DNB Censor cfg 1 i 2; zapis już nie istnieje. Decyzja w `site.md` 9.8.)

Przed testem: zamknij grę, zbuduj OXL w VS, w konsoli `oxl_logs on`. Zapisz grę przed kupnem.

- **G10** Kontrakt kupna: przy pełnym parkingu kupno jest blokowane, saldo konta bez zmian, ogłoszenie zostaje (także po wyjściu i powrocie do garażu). UA:
- **G11** W menu sklepu gry nie ma już przycisku OXL, komenda `oxl_open` nie istnieje, inne `oxl_*` (np. `oxl_status`, `oxl_generate 3`) działają, strona OXL w przeglądarce działa jak wcześniej. UA:

## Nowa strona: badanie danych gry (2026-11-25)

(G13, G14, G16 zaliczone 2026-11-25: wyniki w `Docs/Research/Market.md`. Dostawa natychmiastowa i pełny silnik działają; części silnika miały odkryty stan, poprawka w G19.)

- **G15** Po zbudowaniu OXL: `oxl_end_all`, potem `oxl_generate 20`. Na stronie OXL nie ma DNB Censor LIQD-X4 ani 5000 (ani innych wersji z silnikiem spoza dema, lista w `Docs/Research/Market.md`); w logu brak `No spawnable car version`. UA:

## Rynek: dane z gry, dostawa, mail (2026-11-25)

(G17–G20 zaliczone 2026-11-25: katalog 11 aut, 17 silników, 59 części >= $480, 51 narzędzi, 860 dekoracji, wszystkie z miniaturą; dostawa po czasie (18:00) i od razu, mail od OXL, stan części silnika ukryty, podnośnik do postawienia od razu.)


## Rynek E2: ogłoszenia przedmiotów (2026-11-25)

(G21, G22, G24 zaliczone 2026-11-25: ogłoszenia, opisy, wygasanie i przybywanie z czasem gry, odmowa przy pustym koncie. Znalezione: ceny powyżej nowego i groszowe dekoracje, poprawka G25.)

Przed testem: zamknij grę, zbuduj OXL w VS, wejdź do garażu, `oxl_logs on`. Zapisz grę przed `oxl_buyitem`.


- **G25** `oxl_items_generate 15 tools`, potem `oxl_items tools`: mniej więcej połowa to narzędzia, brak przedmiotów za kilka dolarów (karton, śmieci, magazyn w ramce), żadna cena nie przekracza ceny nowego (`$X (new $Y)`, X < Y). OUA:

## Rynek E3: nowa strona OXL (2026-11-25)

(Pierwsza wersja odrzucona 2026-11-25: wyglądała jak sklep z nowymi rzeczami, `Docs/screens/ss3.png`, `ss4.png`. Przebudowa wg OLX, `site.md` 11; G26–G28 i G30 zastąpione przez G31–G35.)

Przed testem: zamknij grę, zbuduj OXL w VS, wejdź do garażu, `oxl_logs on`. Zrzuty ekranu strony wrzuć do `Docs/screens/` (ss3.png, ss4.png...).

- **G29** `oxl.com/settings`: dostawa (At the next delivery round / Instant delivery), jednostki, ceny; kliknięcie podświetla wybór, zostaje po wyjściu z gry (cfg), a kupno po zmianie dostawy działa wg nowego ustawienia. UA:
- **G31** `oxl.com`: ciemny morski pasek z zielonym logo OXL, linkami Cars / Engines & Parts / Tools & Decor, "Your Redline: $..." i białym przyciskiem Settings; pod nim szeroka wyszukiwarka z ciemnym "Search"; "Main categories" (trzy koła ze zdjęciem z gry i liczbą ogłoszeń); "Featured and fresh listings" w kartach (zdjęcie, STOCK PHOTO, tytuł sprzedawcy, cena, miasto, data). Przewijanie do stopki. Czy wygląda jak serwis z używanymi rzeczami? UA:
- **G32** Lista (Cars / Engines & Parts / Tools & Decor): ścieżka "Home / ...", pasek grup z liczbami (marki aut, Engines/Parts, Tools/Decorations), "We found N listings", linki Sort i Price, **wiersze** (zdjęcie z lewej, tytuł pisany przez sprzedawcę: dealer WIELKIMI LITERAMI z gwiazdkami, scammer "LIKE NEW!!!"; "miasto | Today at 06:12"; parametry; cena z prawej), część ogłoszeń dealerów z "FEATURED" na górze; strony po 15. UA:
- **G33** Ogłoszenie przedmiotu: "< Back" i ścieżka, duże zdjęcie STOCK PHOTO, pigułki (Private/Business, kategoria, przedmiot, stan, cena nowego), "Description"; po prawej "Posted ...", tytuł, cena, ciemny "Buy now" i obramowany "Message seller", karta PRIVATE PERSON / BUSINESS ("On OXL since", "Last online"), LOCATION z mapką, CONSUMER RIGHTS; na dole "See also". UA:
- **G34** "Message seller": zielony komunikat "X replied. Check your mail.", w poczcie mail od OXL z odpowiedzią w stylu sprzedawcy (scammer pogania i każe płacić z góry); drugi klik na tym samym ogłoszeniu nie tworzy drugiego maila. UA:
- **G35** "Buy now" (zapisz grę przed): przy saldzie zielony komunikat na liście, paczka i mail "Order confirmed"; przy pustym koncie czerwony komunikat, nic nie zeszło. Stan kupionej części po sprawdzeniu = rzeczywisty, nie deklarowany. UA:

## Rynek E4: filtry, wszystkie auta gry, strona auta, kurier, zdjęcie w mailu (2026-11-25)

Przed testem: zamknij grę, zbuduj OXL w VS, `oxl_logs on`. Stare ogłoszenia aut wyczyść `oxl_end_all` i dodaj nowe `oxl_generate 20`. Zapisz grę przed kupnem.

- **G37** Po `oxl_generate 20` w zakładce Cars są też **Bolt** i **Lacroix** (ze zdjęciem z gry); wiersze mają rocznik, przebieg, moc i napęd. UA:
- **G38** "More filters" na Cars: okno z marką/modelem (po wybraniu marki), rocznikiem, przebiegiem, mocą, napędem, stanem, sprzedawcą, ceną; każdy wybór zmienia "Show N results", aktywne filtry widać nad listą i znikają po "X"; sortowanie po roczniku, przebiegu i mocy działa. UA:
- **G39** Klik w auto otwiera nową stronę `oxl.com/car/...` (jasna, jak przedmioty) z parametrami z gry, opisem, Buy now i Message seller. Buy now (z pieniędzmi): auto na parkingu, zielony komunikat, mail "Order confirmed" **ze zdjęciem auta**; bez pieniędzy: czerwony komunikat; przy pełnym parkingu: komunikat o parkingu. UA:
- **G40** Kupno części przy wyłączonym "Instant delivery": komunikat i mail "Expected tomorrow (albo the day after tomorrow) at H:00" **ze zdjęciem części**; paczki nie ma dziś, przychodzi w podanym dniu o tej godzinie (przewiń czas snem). Wyjście z gry przed dostawą nie gubi paczki. W logu `[OXL:COURIER] ... scheduled` i `handed to the game courier`. UA:
- **G41** `oxl_market cars` (po zbudowaniu): moc i moment różne od 0 i sensowne (np. V8 ok. 150 do 300 HP); na stronie auta pigułki Power i Torque, w "More filters" sekcja Power, sortowanie "Most power" działa. OUA:
- **G42** Kupno auta przy pełnym parkingu i przy pustym koncie: zamiast czerwonego paska okno "You cannot buy this car" z powodem; przy braku pieniędzy przycisk "Go to Redline ATM" otwiera bankomat. To samo dla przedmiotu ("You cannot buy this item"). UA:
- **G43** Nigdzie nie ma "Business" (pigułki, karta sprzedawcy, filtry); karta sprzedawcy zawsze "PRIVATE PERSON"; "Consumer rights" jest. W ramce LOCATION przedmiotu "Buy now and it is at your garage tomorrow (the day after tomorrow) after H:00"; po kupnie mail i paczka zgadzają się z tym terminem. UA:
- **G44** Mail po kupnie: zdjęcie na samym dole i wyraźnie mniejsze niż wcześniej. UA:
- **G45** Czytelność: zwykły tekst na białym tle (opisy, miasta, daty) wyraźny, nie rozmazany; kolorowe słowa w opisie auta czytelne. Zrzut porównawczy do `Docs/screens/`. UA:
- **G46** Geargle: wpisz np. "V8" albo "Mayen": w wynikach zakładka "OXL" z ogłoszeniami (tytuł, cena, miasto, parametry), klik otwiera ogłoszenie na oxl.com. UA:
- **G47** Po wrzuceniu grafik z `Docs/Design/geminiPromptOXL.md` do `Resources/Images` i restarcie gry: logo w pasku strony, awatar i baner w mailach OXL, ikona OXL w katalogu stron. UA:

## Zawieszenie wczytywania zapisu (2026-11-25)

(G48 zaliczony 2026-11-25: winny Luxor Cruiser z felgą rim_274 spoza dema; filtr rozszerzony o felgi i opony, `Docs/Research/Market.md`.)

- **G49** Nowa gra, `oxl_end_all`, `oxl_generate 20`, `oxl_market cars`: 10 wersji (bez Luxor Streamliner Mk3 Cruiser); w logu (`oxl_logs on`) `[OXL:MARKET] car_luxorstreamlinermk3#1 skipped: rim_274 not in this game build`. Kup 2–3 auta różnych marek, zapisz, wyjdź do menu, wczytaj: zapis się wczytuje. UA:
- **G50** Na stronie przedmiotu ramka **DELIVERY** nad lokalizacją z terminem ("Tomorrow after 16:00" / "Right after purchase" przy Instant delivery); na stronie auta "Right after purchase". Kupiona część przychodzi w tym terminie. UA:
- **G51** Kup część przy wyłączonym Instant delivery. Od razu w kalendarzu BlastCoreOS (okno i flyout z paska): paczka "OXL Marketplace" z nazwą części w "On the way", kropka w dniu dostawy i blok o godzinie dostawy, zgodnie z ramką DELIVERY i `oxl_parcels`. Po północy dnia dostawy paczka dalej w kalendarzu (już jako dostawa gry "Used parts") i przychodzi o tej godzinie. `bcos_calendar_mods` pokazuje dostawcę "oxl". UA:

## Paczki zdjęć: miejsca i fotograf w świecie (2026-11-25)

(G52–G54 zaliczone 2026-11-26: użytkownik zrobił paczkę 7 aut demo, 4289 zdjęć po ręcznej selekcji, 1280x720, układ `CarPhotos/{auto}/{wersja}/{NN_RRGGBB}/{stan}/spotNNN_MM`. Wnioski w `rebuild.md` 2 i 7.)


## Paczka zdjęć na stronie (2026-11-26)

Przed testem: zbuduj w VS (kopiuje też `SixLabors.ImageSharp.dll` do `UserLibs` gry), w grze `oxl_logs on`, wygeneruj nowe ogłoszenia (stare nie mają zdjęć z paczki).

- **G55** Po starcie w logu nie ma `[OXL:PHOTO] SixLabors.ImageSharp.dll not found`; przy nowych ogłoszeniach linie `[OXL:PHOTO] {auto}#{wersja} color=NN_RRGGBB cond=... spot=... photos=N`. OUA:
- **G56** Lista aut na `oxl.com/cars`: ogłoszenia aut z paczki mają prawdziwe zdjęcie (bez znaczka STOCK PHOTO), także scammerów (ładne zdjęcia 60100); auta bez paczki mają zdjęcie z gry ze znaczkiem. Zdjęcia pojawiają się po chwili, gra się nie zacina przy przewijaniu. UA:
- **G57** Strona auta z paczki: duże zdjęcie, strzałki `<` `>` przełączają zdjęcia (to samo tło, inne ujęcia), licznik "2 / 5"; po kliknięciu strona się nie przewija do góry. UA:
- **G58** Kup auto z paczki: kolor lakieru po dostawie taki jak na zdjęciach, a wygląd karoserii pasuje do stanu na zdjęciach (030 zniszczone i brudne, 60100 ładne). Chip "Color" na stronie i kolor w opisie sprzedawcy to nazwa ("silver"), nie "#7A745F". UA:
- **G59** Usuń kilka zdjęć jednego ogłoszenia z folderu paczki (gra zamknięta), uruchom: ogłoszenie pokazuje pozostałe zdjęcia; gdy usuniesz wszystkie, zdjęcie z gry ze znaczkiem STOCK PHOTO, bez błędów w logu. UA:
- **G60** Strona auta z paczki: pod dużym zdjęciem pasek miniatur, oglądane w ciemnej ramce; kliknięcie miniatury pokazuje to zdjęcie (strzałki i licznik się zgadzają). Ogłoszenie ze zdjęciem z gry nie ma paska. UA:
- **G61** Karta sprzedawcy (auto i przedmiot): 5 gwiazdek (złote = ocena, reszta szare, bez kwadratów) i "4.0 / 5" obok, z kropką. UA:

## Scammer: przebudowa (2026-11-26, `Docs/Design/scammer.md`)

Wygeneruj nowe ogłoszenia (`oxl_logs on`). Scammera rozpoznasz w logu po `[OXL:SCAM]` i `[OXL:ITEMS] new ... Scammer L1/L2`.

- **G62** Auto scammera L3: karta sprzedawcy ma 3 albo 3,5 gwiazdki (połówka widoczna, "3.5 / 5"); w ogłoszeniu 1 albo 2 niezgodności z logu `[OXL:SCAM] ... L3 lies` (kolor inny niż na zdjęciach, wymyślona wersja jak DEMON, silnik innej wersji, moc 5x) w pigułkach i w zdaniu "Full specification" w opisie. UA:
- **G63** Auto scammera L2: moc w pigułce "Power" i na liście 5x większa niż u tego samego auta od innego sprzedawcy, zdanie "Specs: ..." w opisie; L1 bez zmian. UA:
- **G64** Ogłoszenia sprzed zmiany (stary `listings.dat`) dalej się wczytują, oceny całkowite pokazują się jak wcześniej ("4.0 / 5"). OUA:
- **G65** Przedmiot od scammera (także narzędzie albo dekoracja): ocena 1 albo 2 gwiazdki, "Joined OXL this week", ogłoszenie znika po kilku godzinach gry; L1 krzyczy wielkimi literami, L2 pisze spokojniej. UA:
- **G66** Kup przedmiot od scammera: w logu `[OXL:SCAM] item ... swapped for cheapest '...'`, mail potwierdza kupioną rzecz, a w paczce przychodzi najtańszy przedmiot gry (wpisz, co to jest). Pieniądze pobrane raz. UA:
