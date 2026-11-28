# Dziennik odkryć

> Krótkie wpisy z datą: co ustalono, skąd (log, skrypt, zrzut, kompilacja), gdzie zapisano wniosek. Nowe wpisy na końcu.

## 2026-11-20: setup pod port na CMS 2027

- Struktura dokumentacji na wzór GlideRail (`CLAUDE.md`, `Docs/{Design,Research,History}`, `BACKLOG`, `playtestGoals`, `Release`, `ZADANIE`, `/zadanie`), folder `Scripts/` (wyłączony z kompilacji w csproj) z `_build_check.sh` i `_check.sh`.
- Mod odrzucony przez MelonLoader w 2027 ("incompatible", `MelonGame` 2026), log 2026-11-20.
- Kompilacja względem interopu 2027: wynik w `Docs/Research/Port2027.md`.

## 2026-11-22: przegląd warsztatu agenta

- `Scripts/_check.sh`: dodane `Il2CppFusion.Runtime` (bez tego każdy skrypt z `CarLoader` dawał CS0012, sprawdzone), też `Il2CppSystem` i `ImageConversionModule`.
- BlastCoreOS działa w 2027 (jego `Port2027.md`, 2026-11-21): zaktualizowane `CLAUDE.md`, `Port2027.md` pkt 1, `BACKLOG`, `ZADANIE`, `architecture.md`.
- Opisane archiwum ~100 starych skryptów REPL z `Mods/CMS2026SimpleConsole/`: `Docs/Research/ConsoleScripts.md` (fotograf `totalMaker.cs`, ekstraktor `car_spec.json`, odkrycia API 2026 w `carspawnerADV.cs`).
- `/zadanie`: prefiksy logów OXL, `oxl_logs on`, `_build_check` przed oddaniem.
- 2026-11-25: port OXL na 2027 (etap 1). Interop zwraca `ref` struktury sieciowej (`CarInfoData`) jako kopię w pudełku: odczyt daje nagłówek obiektu, zapis ginie; obejście natywnym getterem. `CarBodyPart` nie leżą pod `CarLoader`. Płatności przez bank Redline, tryb samodzielny i przycisk w sklepie usunięte (`Docs/Research/Port2027.md` pkt 6).
- 2026-11-25: rynek OXL na 2027: `MarketCatalog` (dane gry + `BlastCoreGameDataAPI`), kurier OXL przez `DeliveryManager` (godzina 255 = od razu, silnik trzeba wypełnić częściami, `IsExamined=false`), strona `OXLSite`. Wersje aut psujące zapis: silnik (G12) i felgi (G48, Luxor Cruiser `rim_274`); przy zawieszeniu wczytywania czytać `Player.log` gry. Kalendarz BlastCoreOS dostał API dla modów.
- 2026-11-28: testy przez Agent Bridge (Simple Console 1.4.0, `game.sh`): agent sam wysyła komendy i skrypty REPL do gry. Zamknięte G11, G15, G25, G41, G55, G64, G69 (`Scripts/probe_photos.cs`).

## Zaliczone goale (przeniesione z `playtestGoals.md` 2026-11-28)

- G2, G6–G9 (2026-11-25): OXL ładuje się w 2027, 3 kupna `SpawnResult: Success`; 21–27 `CarBodyPart` na auto, przebieg i rok zgodne w 7 kupnach, Wrecker/Scammer `27 carParts → 0.02`, każde kupno jedno `Charge oxl`. G1 nie przeszedł (`CarInfoData` z interopu to kopia), poprawka `GameCarAccess.cs`. Wnioski: `Docs/Research/Port2027.md` pkt 6.
- G12 (2026-11-25): brakujące silniki to fabryczne silniki DNB Censor cfg 1 i 2 (`site.md` 9.8).
- G13, G14, G16–G20 (2026-11-25): katalog rynku, dostawa po czasie i od razu, pełny silnik, mail OXL, ukryty stan części (`Docs/Research/Market.md`).
- G21, G22, G24 (2026-11-25): ogłoszenia przedmiotów, wygasanie, odmowa przy pustym koncie. Pierwsza wersja strony odrzucona (wyglądała jak sklep), G26–G28 i G30 zastąpione przez G31–G35.
- G48 (2026-11-25): zawieszenie wczytywania przez Luxor Cruiser z felgą `rim_274` spoza dema; filtr felg i opon.
- G52–G54 (2026-11-26): paczka 7 aut demo, 4289 zdjęć, układ `CarPhotos/{auto}/{wersja}/{NN_RRGGBB}/{stan}/spotNNN_MM` (`rebuild.md` 2 i 7).
- 2026-11-28 przez Agent Bridge: G11, G15, G25, G41, G55, G64, G69 (opis wyżej). Część techniczna G40, G51, G66: kupno przez `oxl_buyitem`, `Charge oxl` raz, mail, kalendarz `providers=1 (oxl)`, o północy `handed to the game courier for 16:00`, po 16:00 `WaitingDelivery=0` (`Scripts/probe_waiting.cs`); scammer: zderzak Mayen zamieniony na `Bottle_1` ($1). Uwaga: `addtime` w konsoli przesuwa czas z animacją (słońce, 5–10 s), więc odczyt zaraz po komendzie pokazuje czas w trakcie przesuwania; po `addtime` czekać ok. 10 s.
- 2026-11-28 (odpowiedzi użytkownika): zaliczone G10, G29, G31–G35, G37–G40, G42, G43, G45–G47, G49–G51, G56–G63, G65, G67, G68, G70. G44 działa, ale zdjęcie w mailu ma być o 20% mniejsze (G72). G66: butelka przychodzi, ale gracz nie wie, że go oszukano: komunikat przy otwarciu paczki (G73). Auta zawsze dostawa od razu (kurier OXL tylko dla przedmiotów), potwierdzone przez użytkownika jako wymóg.
- 2026-11-28: kupno auta przy pełnym parkingu (zapełnionym `carspawner.cs`) kończyło się `SpawnFailed`: gra zwraca w `GetPlaceForLoadCar` pusty CarLoader z `IsCarLoaded()=true` i `done=true`, który ignoruje `CarDebug.LoadCar` (także po `DeleteCar`), a OXL czekał tylko na `done`. Poprawka w `GameBridge.DoSpawn`: czekanie na `CarID`, kolejne puste miejsca, na końcu "parking full" (G71). Skrypty `probe_loaders.cs`, `try_loadtiming.cs`.
- 2026-11-28: komunikat przy otwarciu paczki od scammera (G73): `OXLScamParcels.cs`, postfix Harmony na `DeliveryManager.TakeFromStorage` (ID paczki = `Delivery.ID`), referencja `0Harmony` w csproj. Zdjęcie w mailu 288 px (G72).
- 2026-11-28: G73 zaliczony (komunikat przy paczce scammera: kto, co kupione, co przyszło; zwykła paczka bez komunikatu; pułapka przetrwała kuriera i zapis w `items.dat`). Na prośbę użytkownika komunikat czerwony (`UIPrompt.Error`), G74.
- 2026-11-28: G72 i G74 zaliczone (G74 bez testu, decyzja użytkownika). G71: przy pełnym parkingu strona nie reagowała, drugi klik dawał 'no listing', potem przejście na Home; okno 'Delivering your car' i zostanie na stronie auta przy porażce (`site.md` 15, G75).
- 2026-11-28: G71 i G75 zaliczone (okno 'Delivering', komunikat przy pełnym parkingu, kupno po zwolnieniu miejsca). Zakładka Parts ze wszystkimi częściami auta, silnik 5% (`site.md` 16, G76, G77); `Scripts/probe_partgroups.cs`, `try_partkinds.cs`.
- 2026-11-28: G76: części z karoserii, ABS, tłumik, fotel OK; opona i felga bez rozmiaru (WheelData 0) -> rozmiar z configów aut (G78); silniki prawie zawsze V6 -> losowanie po rodzinie (G79). G77 na danych OK (30 ogłoszeń: karoseria, wnętrze, opona, zawieszenie, skrzynie, I4).
- 2026-11-28: G79 zaliczony (5 rodzin silników w katalogu: i4 1, r4 2, v8_350 2, v6_231 10, v8_d62 2; losowanie po rodzinie). G78: paczki z rozmiarem wysłane (log `wheel size=...`).
- 2026-11-28: G78 zaliczony (opona i felgi z rozmiarem i wartością). `oxl_end_all [cars|parts|tools]` (G80); w tej sesji przedmioty wyczyszczone przez REPL `ItemListingSystem.Clear()`.
- 2026-11-28: silnik z OXL na stojaku był pustym szkieletem (części z `new Item(id)` bez `NormalID`, numeru i `MountObjectData`). Wzorzec z silnika wyjętego z auta, test w grze: R4 z pustym `MountObjectData` siada cały. Części tylko do auta luzem (`OXLEngineParts`). G81. Uwaga: `EngineStand.CreateEngine` z REPL = czarny ekran (`try_unblack.cs`).
- 2026-11-28: silnik na stojaku: numer gniazda z `EnginePartsConfig.EnginePartsPaths` (nie numer wystąpienia); V8 ze ścieżkami siada cały (`try_enginepaths.cs`). Silnik w paczce jako jedna rzecz, bez części luzem (decyzja użytkownika).
- 2026-11-28: G80 zaliczony (`oxl_end_all parts|cars`, zły argument = instrukcja). G81 zaliczony: 5 rodzin (I4 B4FM, I4 AQ4C, V6 322BZ turbo, V8 820MAL, V8 D.vik) siada na stojakach w całości, stan ok. 70% jak w wysyłce, brak tylko części do auta.
- 2026-11-28: G77 zaliczony (zakładka Parts wygląda dobrze, użytkownik).
- 2026-11-28: ogłoszenia per zapis gry (`OXLSaveSlot`, `site.md` 17, G82); prośba do BlastCoreOS o oficjalne API katalogu zapisu (`Docs/ForOtherRepos/BlastCoreOS_profileStorageAPI.md`).
- 2026-11-28: G82 zaliczony (slot 0: zapis gry zatwierdza OXL w Saved, wyjście bez zapisu cofa kupno; nowa gra w slocie 1 ma własne, inne aukcje). Znalezione: aukcje aut nowej kariery nie trafiały na dysk do pierwszej zmiany; zapis po pierwszym wygenerowaniu (G83). BlastCoreOS dodał kontrakt i zdarzenie `Ready`.
- 2026-11-28: G83 zaliczony (nowa gra: listings.dat od razu na dysku, po zapisie i wczytaniu te same 20 aut).
- 2026-11-28 (przerwane limitem): pkt 2 etap 1 rozpoczęty: `OXLCarMarket.cs` (backend aut bez UI) gotowy obok `OXLPanel`; do zrobienia: OXLWebPage na OXLCarMarket, OXLPlugin (typ _panel, usunąć oxl_detail i oxl_end_current), OXLPanel.HexColor -> OXLCarMarket.HexColor, usunąć OXLPanel.cs, OXLFilterPanel.cs, FilterOptionsBuilder.cs, resources/, stare strony oxl.com/listings i listing/{id}; goal 'działa jak przed zmianą'.
- 2026-11-28: pkt 2: stary panel aut usunięty (~220 KB kodu), backend w `OXLCarMarket`; osierocone `ListingSystem.TryPurchase` (kupno bez kontraktu) i `OXLLog.Warn` usunięte. G84.
- 2026-11-28: ceny silników z sumy części (katalog 5005 to placeholder), widełki, częściej silniki, rzadziej scammer, realny stan (`site.md` 16.9, G85).
- 2026-11-28: G84 zaliczony (strona i kupno auta po usunięciu starego UI), G85 zaliczony (ceny silników). Stopka z logo Redline, tańsze silniki częściej (G86).
- 2026-11-28: pkt 4: PartCatalog rozpoznaje części silników spoza 2026 (V8 D.vik w Bolcie Nevadzie: 72 części trafiały do Structural). G87.
- 2026-11-28: dane aut tylko z gry: usunięte `car_spec.json` (CarSpecLoader), stare paczki 2026 (CarPhotoLoader, PhotoCondition), `CarDefs`, mapa `ToGameId`, `ImageFolder`, `oxl_memory`; ceny aut z `carValue` (`site.md` 19, G88). G86, G87 zaliczone (stopka do oceny).
- 2026-11-28: G86 zaliczony (stopka, użytkownik). G88 zaliczony: po usunięciu car_spec.json 20 nowych aut z 7 modeli, wszystkie ze zdjęciami z paczki 2027, ceny z wartości auta i stanu, brak [PhotoLoader] i błędów; kupno Lacroixa (stan 20%): części 22–29%, wydech 8–12%. Użytkownik usunął stare foldery MEDRES/LOWRES z gry.
- 2026-11-28: pkt 6: wersja 0.7.0, README.md, `Docs/NexusOpis.md`, komendy testowe oznaczone [CHEAT]/[DEV] (zostają), przycisk "Check on wiki" (BlastCoreWebAPI.OpenInBrowser, AutoFreaks), paczka zdjęć jako CarPhotos BASE v2 osobno; wymaga BlastCoreOS 0.3.0 (G89).
- 2026-11-28: G89 zaliczony (Check on wiki). Opis Nexusa przepisany od nowa (revamp, `Docs/NexusOpis.md`, zdjęcia z `Docs/zdjeciaPodRelease/`). 0.7.0 pakuje użytkownik, publikacja po TheBucket.
- 2026-11-28: CHANGELOG.md (root) z pełnym 0.7.0 i skrótem 0.1.0–0.6.0; stary `Docs/changelog.md` usunięty. Koniec dnia: wszystko zapisane przed /clear.
