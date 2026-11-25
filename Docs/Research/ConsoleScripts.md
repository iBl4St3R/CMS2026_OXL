# Archiwum skryptów REPL w folderze konsoli

> Stan 2026-11-22. Źródło: przegląd nagłówków plików w `C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2026 Demo\Mods\CMS2026SimpleConsole\` (106 plików `.cs`, daty 2026-04 do 2026-10). Legenda: ✔ potwierdzone w pliku, ? do sprawdzenia.

## Co to jest

- Skrypty, które użytkownik pisał i odpalał w konsoli F7 **przed** pracą z agentem (głównie kwiecień 2026, przy budowie OXL 0.1–0.5; część z lipca i sierpnia dotyczy BlastCoreOS i innych modów).
- Uruchamiane jako `runfile <nazwa>.cs`: ścieżka względna liczy się od `Mods/CMS2026SimpleConsole/`. Format jak nasze skrypty: ciało `Execute()`, `Print`/`Log`.
- **Folder gry: tylko odczyt.** Nie dopisuj tam skryptów. Nowe skrypty idą do `Scripts/` w repo; gdy coś z archiwum przydaje się przy porcie, skopiuj fragment do `Scripts/` i popraw tam.
- **To API gry 2026.** Prawie wszystko, co dotyka `CarLoader` (`carParts`, `indexedParts`, `SetCondition`, `SetConditionOnBody`, `SwitchRusted(float)`, `EnableDust`, `modelLoaded`), w 2027 się nie kompiluje (`Docs/Research/Port2027.md` pkt 2). Archiwum mówi, **co mod robił i jaką sekwencją**, nie jak to zrobić w 2027.
- Czytaj grepem i zakresami. Pliki mają mieszane kodowanie (część nie jest czystym UTF-8), więc polskie znaki i ramki `═` mogą się wyświetlać jako krzaki.
- Wersje `… — kopia.cs`, `*OLD*.cs` to starsze iteracje; bierz plik bez sufiksu albo najnowszy wg daty.

## Najważniejsze dla OXL

| Plik | Data | Co robi |
|---|---|---|
| `totalMaker.cs` (v4) | 04-23 | **Fotograf automatyczny OXL.** Coroutine: auto → kolor → przedział kondycji (`030` 0.02–0.30, `3060`, `60100`) → losowe pozycje kamery → zdjęcia przez `RenderTexture`. Zapis wprost do `Mods/CMS2026_OXL/Resources/CarImages_LOWRES` (512x288 JPG) i `CarImages_MEDRES` (820x462 + mini 512x288), `color_map.txt` per config, walidacja `car_spec.json`. Lista 5 aut na sztywno, `configIndex` i kolory ustawiane w sekcji konfiguracji. Zużycie: `indexedParts[i].SetCondition`, `SetCondition(carPart)`, `EnableDust`, `SetConditionOnBody/Details`, `SwitchRusted(<0.5)`. **Do portu przy nowych autach** (Bolt Nevada, Lacroix Modique R). Starsze: `totalMakerOLD.cs`, `totalMakerOLDORIGINAL.cs` (opis struktury folderów `{Car}/{Color}/{030|3060|60100}/{uid8}/001.png`). |
| `PhotoMaker.cs`, `PhotoPosTool.cs`, `PhotoPosTool1.cs` | 04-11 | Panel testowy fotografa i narzędzie do zbierania pozycji/obrotów auta do zdjęć (tablica `carPositions`). |
| `watermark.cs`, `postProces1.cs` | 04-12, 07-27 | Znak wodny z `logoCMS.png` (nakładany przez fotografa), diagnostyka post-processingu. |
| `jsonCarExtractor.cs` (+ `OLD`, `OLD2`, `OLDOLD`) | 04-23 | **Źródło `car_spec.json`.** Wymaga otwartej karty auta (I) i odwiedzonej zakładki Sell; czyta `CarInfoWindow`, `SellTabSaleSummary`, `MainTab` (refleksja), zapisuje `car_spec_<CarID>.json` do folderu konsoli. Przykłady wyniku leżą obok: `car_spec_car_mayenm5.json`, `car_spec_car_salemariesmk3.json`. Starsza wersja: `readAllCarData.cs`. ? nazwy UI 2027. |
| `carspawnerADV.cs` | 04-09 | **"OXL Research Base": nagłówek z odkryciami API 2026** (nie usuwać): `carParts` 30–33 części karoserii, `indexedParts` 214 `PartScript` (`condition` małą literą, `id` np. `engine_block`), `SetConditionAll`, `CarInfoData.Mileage` = `UInt32`, `Year` = `UInt16`, auta poolowane w 10 `CarLoader` (`#CarLoader1..10`, pusty `CarID` = wolny slot), `CarDebug.LoadCar(carID, variant)` (Mayen M5 = 1, reszta 0), prefiksy `PartScript.id` wg kategorii, sekwencja coroutine spawnu. Najlepszy punkt wyjścia do porównania z 2027. |
| `carspawner.cs` | 10-21 | Najnowszy spawner z panelem (wybór auta i configu). Iteracje: `carspawnerOLD2026.cs`, `carspawnerOLDSTABLE.cs` (+ inspektor), `carspawneradv2/3.cs`, `PARTSSET.cs` (spawn + zużycie z konfiguracji w zmiennych), `carSpawner — kopia.cs`, `carSpawnerOLD.cs` (opis wariantów Mayen M5: T2/T3/T4Q/T6Q), `carspawner_load.cs`. |
| `ScammerScript.cs` | 04-18 | Zużycie archetypu "Scammer": `CarDebug.SalvageCar()`, wszystkie części 0.02, rama i detale 0.02, spuszczenie płynów. |
| `removeParts.cs`, `removePartsEngine.cs` | 04-18 | **Brakujące części ("BAZA DO MODA"):** `indexedParts` → `IsUnmounted = true` + `SetActive(false)`, `carParts` → `TakeOffCarPart(name)`, bez dotykania ekwipunku. Test zdjęcia silnika V6 z DNB Censor. |
| `PRICEEXTRACTOR.cs` | 04-17 | "Auction Mod DB Builder": panel, który zbiera ceny części z 1 auta w scenie (baza cen OXL). Pomocnicze: `pricesEX.cs`, `PriceCatalog.cs` (`GameInventory.PartPropertyList`), `missingParts.cs` (części bez wpisu w katalogu), `PARTSCAT.cs` (zrzut katalogu części), `dawajHelpera.cs`, `jakatwojacenaSkarbie.cs`, `missingP1.cs`, `mistrzKierownicy.cs` (szukanie ceny bazowej auta). |
| `setCarData_test.cs`, `InspectPartScript.cs`, `carloader.cs`, `carloaderInspector.cs`, `debugcarREPL.cs`, `testGPU.cs` | 04 | Ustawianie przebiegu/roku, inspekcja `PartScript`, `EngineData`, `CarDebug`. |
| `find_instance_and_time.cs`, `find_timemanager.cs`, `inspect_type.cs`, `test_settime.cs`, `timeexplorer.cs` (47 KB), `timeexplorer2.cs` | 04-13..19 | Badanie czasu gry (`Il2CppCMS.Core.TimeManagement.TimeManager`, `TimeControl`, `GameTimeClock`): źródło `GameTimeProvider.cs`. |
| `UITEST1/2/3.cs` | 04-13 | Wstrzyknięcie przycisku w menu sklepu (`ShopsList`, klon `ShopAvatar25`): źródło przycisku OXL w `OXLPlugin.cs`. ? nazwy 2027 (`Port2027.md` pkt 4). |
| `oxlTestPanel.cs`, `destroyPanel.cs` | 04-09 | Podgląd panelu OXL (1456x800, paleta), zamknięcie panelu "OXL". |
| `save.cs` | 04-08 | Test zapisu gry przez `GarageLoader`. |

## Inne mody i framework (dla OXL drugorzędne)

- **BlastCoreOS (07-22..23):** `subm1–5.cs`, `subtest6.cs` (ekran PC, `Computer_new(Clone)`, anchory UI), `use1/2.cs` (`_ClickUse`), `testMODE.cs` (`GameMode`), `brain.cs`, `flicker_debug.cs`, `flicker_brain_debug.cs` (miganie `CinemachineBrain`).
- **GlideRail:** `TP.cs` (pierwowzór: fly mode + keyframe'y), `script2.cs` (CamDebug2), `dumpCamera.cs`, `TESTCURSOR.cs`.
- **NPCGarageHelper / magazyny:** `chaseplayer.cs`, `createNPCDDON.cs`, `storages_in_garage.cs`, `script — kopia.cs` (WarehouseObject).
- **Mapa (08-24):** `rep1–3.cs` (`MapMarkersManager`).
- **Teleport:** `script.cs` (kopia `PlayerAPI.Teleport`), `ttest444.cs` (TP auta do przodu, 10-21).
- **Sklep / ekwipunek:** `script444.cs` (`PartsPage.itemsProvider`), `scirpt11.cs` (`InventoryItem25`), `script4.cs` (zrzut instancji).
- **Framework UI (przykłady i testy):** `SampleScript.cs`, `sampleNOREF.cs`, `sample2–4NOREF.cs` (Garage OS, kalkulator, telefon), `script5–10.cs`, `test_scrollbug.cs`, `promtTester.cs`, `PerfMonitor.cs`.
- Puste: `drukujTokurwa.cs`.

## Inne pliki w folderze

`apiReadme.md` (API konsoli), `CMS2026SimpleConsole.cfg` (`repl_enabled = true`, F7), obrazki do przykładów (`app_*.png`, `phone_wallpaper.png`, `logoCMS.png`, `test.png`), `tps.txt`, zrzut tekstowy auta `[204545] ║ CAR car_katagiritamagobp slot #CarLoader10.txt`.
