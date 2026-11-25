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
