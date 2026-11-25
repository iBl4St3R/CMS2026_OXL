# CMS2026_OXL: instrukcja dla agenta

Mod do gry **Car Mechanic Simulator 2027** (Demo; Unity 6000.3, **IL2CPP**, MelonLoader 0.7.2, net6). **OXL (Online eX-Owner Lies)**: serwis aukcji aut w grze. Generowane ogłoszenia (4 archetypy sprzedawców × 3 poziomy, opisy z osobowością, awatary, zdjęcia z paczek), filtry, kupno ze spawnem auta na parkingu ze zużyciem i ukrytymi usterkami wg archetypu. Od 2026-11-25 **tylko strona w przeglądarce BlastCoreOS** (klawisz T i przycisk w menu sklepu usunięte), płatności przez bank Redline. OXL to pierwszy zewnętrzny moduł BlastCoreOS. Wydany na Nexusie jako 0.6.0 dla CMS 2026 (alpha, strona ukryta od 2026-10-21), **w trakcie portu na 2027**. Licencja własna (nie do kopiowania ani rebrandu).

- **Z użytkownikiem rozmawiaj po polsku.** Krótko i konkretnie.
- **Teksty widoczne dla gracza i logi moda po angielsku. Komentarze w kodzie po polsku.** Dokumenty w `Docs/` po polsku.
- **Nie zgaduj.** To IL2CPP: zrzut gry ma tylko sygnatury, bez ciał metod. Nazwy bierz ze zrzutu albo z kompilacji (`Scripts/_build_check.sh`); zachowanie sprawdzaj skryptem REPL, nie z pamięci.
- **Nazwa "CMS2026" to nie błąd do poprawiania.** Gra miała wyjść w 2026, zmieniła nazwę na 2027. Dotyczy zależności (`CMS2026SimpleConsole.ConsoleAPI`, `_CMS2026_UITK_Framework`, `CMS2026UITKFramework`) **i samego moda**: DLL, namespace `CMS2026_OXL` i folder `Mods/CMS2026_OXL/` (dane graczy, paczki zdjęć) zostają, dopóki użytkownik nie zdecyduje inaczej (wtedy z migracją). Zmienić trzeba `MelonGame` i opisy dla graczy.

## Gdzie co jest

| Ścieżka | Co tam jest |
|---|---|
| `*.cs` (root) | Kod moda, płasko, 20 plików (~950 KB, w tym `SellerNoteBuilder.cs` 261 KB i `OXLPanel.cs` 182 KB). Mapa i przepływ: `Docs/Design/architecture.md`. |
| `archetypes.md` | Techniczny opis systemu archetypów (v0.3.0). Źródło prawdy dla logiki cen, uczciwości i zużycia. |
| `resources/` | Grafiki kategorii strony głównej. Nie zmieniaj bez prośby. |
| `Scripts/` | `_build_check.sh` (kompilacja bez gry), `_check.sh` (sprawdzenie skryptu REPL), skrypty REPL `probe_*.cs` / `try_*.cs`. **Wyłączone z kompilacji w csproj.** |
| `Docs/Research/Port2027.md` | **Co się zmieniło w API gry 2027** (42 błędy, zamienniki, nowe auta, stan BlastCoreOS). Zacznij od tego. |
| `Docs/PlayerInputFix.md` | Zgłoszenie od agenta BlastCoreOS (2026-11-22): postać idzie dalej po otwarciu konsoli F7. Dotyczy kodu **Simple Console** (`DevConsoleComponent`), nie OXL; OXL miał ten sam wzorzec w `SetPlayerInput`, usunięty 2026-11-25 z trybem samodzielnym. |
| `Docs/Research/Market.md` | Dane gry pod nową stronę: filtr wersji aut (`HaveDLC` silnika), katalog części i ceny, narzędzia i dekoracje, przesyłki gry. |
| `Docs/Research/ConsoleScripts.md` | Opis ~100 starych skryptów REPL użytkownika z folderu konsoli w grze (fotograf, ekstraktor `car_spec.json`, spawn i zużycie, ceny, czas). Sprawdź przed pisaniem nowego skryptu. |
| `Docs/Design/` | Jak działa mod i dlaczego (`architecture.md`). **`rebuild.md`: decyzje i etapy przebudowy na 2027 (aktualny plan).** |
| `Docs/BACKLOG.md` | Otwarte zadania, znaleziska, ryzyka. Zrobione idą do `Docs/History/backlog-done.md`. |
| `Docs/playtestGoals.md` | Goale do sprawdzenia w grze (`G<n>`). Zasady na górze pliku. |
| `Docs/Release.md` | Checklista publikacji, historia wydań, lista tymczasowych rzeczy w kodzie. |
| `Docs/ZADANIE.md` | Bieżące zadanie (komenda `/zadanie`). |
| `Docs/ForOtherRepos/` | Dokumenty napisane tutaj dla innych repo użytkownika (agent nie ma tam prawa zapisu): do przeniesienia ręcznie. |
| `Docs/History/` | Archiwum (dziennik odkryć `log.md`, zrobione zadania). Tylko grepem. |

**Nie czytaj i nie zmieniaj:** `bin/`, `obj/`, `.vs/`, `.git/`, `*.sln`, `logo.png`, `archetypes.png`. `CMS2026_OXL.csproj` tylko, gdy zadanie tego wymaga.

**Duże pliki czytaj wyłącznie grepem i zakresami:** `SellerNoteBuilder.cs`, `OXLPanel.cs`, `ListingSystem.cs`, `GameBridge.cs`, `OXLFilterPanel.cs`.

### Wiedza wspólna (tylko odczyt, w innych repo użytkownika)

| Plik | Co tam jest |
|---|---|
| `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Research\Ecosystem.md` | Gra, loader, mody użytkownika, wersje Frameworka (0.4.0+) i Konsoli. **Najnowsza** (2026-11-23). Starsza, pełniejsza o inne mody: `…\GlideRail\Docs\Research\Ecosystem.md`. |
| `C:\Users\usmat\source\repos\GlideRail\Docs\Research\Repl.md` | Skrypty REPL: szablon, pułapki, co działa w Roslynie konsoli (pełniejsza niż kopia w BlastCoreOS). |
| `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Research\GameInternals.md` | Wiedza o grze 2027 (kamera, gracz, wejście, UI, magazyny, dane aut i części). **Najnowsza i największa** (26 KB, 2026-11-23), tylko grepem. Stara wersja w GlideRail pomijaj. |
| `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Research\Port2027.md`, `Parking.md`, `SimpleKCC.md` | Port BlastCoreOS na 2027 (kamera, `SimpleKCC`, Fusion), parking gry (gdzie stawiać kupione auto), blokada ruchu gracza. |
| `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Design\modules.md` | Publiczne API BlastCoreOS dla modułów (używane przez OXL) i historia jego zmian. **Czytaj przed każdą zmianą integracji.** |
| `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Design\web.md`, `ui.md` | Przeglądarka (adresy, podstrony, `Refresh`, wysokość treści) i zasady UI BlastCoreOS (motyw `01 Core/BlastCoreTheme.cs`, GarageOS jako wzór paneli). Punkt odniesienia dla nowego wyglądu OXL. |
| `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Design\bank.md`, `mail.md` | Bank Redline (`BlastCoreBankAPI`: strony i mody płacą przez konto, nie saldo gry) i poczta (`BlastCoreMailAPI`: powiadomienia, np. o aukcjach). OXL ma z nich korzystać. |
| `C:\Users\usmat\source\repos\GlideRail\Docs\Design\decisions.md` | Sekcja o `CursorManager` (bilansowanie paneli frameworka). |

### Poza repo (tylko odczyt)

| Ścieżka | Co to jest |
|---|---|
| `C:\Users\usmat\source\repos\cms2027assembly\` | Zrzut `Assembly-CSharp` gry 2027 (tylko sygnatury). `Il2CppCMS/Core/Car/CarLoader.cs` 900 KB, `Il2Cpp/CarDebug.cs`: tylko grep. Nigdy całych katalogów. |
| `C:\Users\usmat\source\repos\BlastCoreOS\` | Mod użytkownika (PC w grze z przeglądarką), twarda zależność OXL. **Nie edytuj z tego repo.** Potrzebna zmiana w BlastCoreOS = wpis w `Docs/BACKLOG.md` i informacja w raporcie. |
| `C:\Users\usmat\source\repos\GlideRail\` | Wzorzec warsztatu agenta (CLAUDE.md, Docs, Scripts), `GlideUi.cs` (wzór `CursorManager`). **Nie edytuj.** |
| `C:\Users\usmat\source\repos\_CMS2026_UITK_Framework\`, `…\CMS2026-Simple-Console\` | Framework UI (API: `BasicAPI.md`, `ProAPI.md`) i konsola (API: `apiReadme.md`). **Nie edytuj.** |
| `C:\Program Files (x86)\Steam\steamapps\common\Car Mechanic Simulator 2026 Demo\` | Gra (folder ma starą nazwę, exe i `_Data` są "2027"). `Mods/CMS2026_OXL/` (cfg, `listings.dat`, `Resources/`), `Mods/CMS2026SimpleConsole/` (stare skrypty REPL, opis w `Docs/Research/ConsoleScripts.md`), `…2027 Demo_Data\StreamingAssets\Cars` (lista aut), `MelonLoader/Latest.log`. **Nie zmieniaj niczego w folderze gry.** |

Zakazy edycji poza repo są też w `.claude/settings.json` (deny `Edit` dla BlastCoreOS, GlideRail, Frameworka, Konsoli, zrzutu i folderu gry). Nowe repo w tabelach = nowy wpis deny.

## Jak szukać (oszczędnie)

1. Po zimnym starcie: ten plik, **pierwsza linia `Docs/ZADANIE.md`** (`[ … ]`, wiadomość użytkownika, ma pierwszeństwo), to, co zadanie wskazuje.
2. Port: `Docs/Research/Port2027.md`, potem `bash Scripts/_build_check.sh`.
3. Gra: zrzut grepem; potem `Docs/Research/ConsoleScripts.md` (czy użytkownik już to badał w 2026); gdy sygnatura nie wystarcza, skrypt REPL w `Scripts/`.
4. Logika ogłoszeń: najpierw `archetypes.md`, potem grep w `ListingSystem.cs` / `SellerNoteBuilder.cs`.

## Twarde zasady moda (naruszenie = mod się nie włączy albo psuje grę)

1. **`MelonGame` musi wskazywać "Car Mechanic Simulator 2027 Demo" / "Car Mechanic Simulator 2027"** (`OXLPlugin.cs`). Z samym "2026" MelonLoader odrzuca mod jako niekompatybilny.
2. **Typy gry z interop** (`Il2CppCMS.*`, `Il2Cpp.*`). Nazwy po stringach (`GetIl2CppType().FullName == "CMS.UI…"`, refleksja) kompilator nie sprawdzi: potwierdź w zrzucie albo skryptem. Każde takie wywołanie w `try/catch` z logiem.
3. **Kupno jest transakcją:** pieniądze pobierane tylko, gdy auto faktycznie się pojawiło; przy błędzie dostawy ogłoszenie wraca, pieniądze nie znikają. Każda zmiana w `GameBridge` / kupnie zachowuje ten kontrakt i dostaje goal.
4. **Dane graczy:** `listings.dat`, `CMS2026_OXL.cfg`, paczki zdjęć. Format zmieniamy tylko ze zgodnością wstecz albo z jawną informacją "czysta reinstalacja" w opisie wydania. Nieznane auta/configi z pliku pomijać, nie wywracać wczytania. Liczby w zapisie `InvariantCulture`.
5. **"Sprzedawcy kłamią w opisach, nie na zdjęciach":** prawdziwe zdjęcie (z paczki) i stan po dostawie muszą odpowiadać rzeczywistej kondycji z ogłoszenia (`archetypes.md`). **Jedyny wyjątek: scammer** wrzuca ładne zdjęcia (60100), a przyjeżdża wrak (decyzja użytkownika 2026-11-26, `Docs/Design/scammer.md`). Zdjęcie z gry (bez paczki) zawsze ze znaczkiem "Stock photo" (`site.md` 10).
6. **Zależności:** Framework twardy (`FrameworkAPI.IsReady`), Konsola miękka (refleksja po `CMS2026SimpleConsole.ConsoleAPI`, mod działa bez niej), BlastCoreOS twardy w kodzie 0.6.0 (`BlastCoreWebAPI`, `BlastCoreWebPage`, `BlastCoreWebPageContext`, `BlastCoreSoundLibrary`); w 2027 dochodzą `BlastCoreBankAPI` (płatności) i `BlastCoreMailAPI` (poczta). Po każdej aktualizacji `BlastCoreOS.dll` OXL trzeba przebudować.
7. **Unity API tylko na głównym wątku. Sesja w scenie z "garage".** Nie trzymaj statycznych referencji do obiektów gry przez zmianę sceny. Panele frameworka bilansują `CursorManager` (wzór `GlideRail/GlideUi.cs`).
8. **UI: angielski, bez emoji i symboli Unicode** (font UI Toolkitu renderuje je jako kwadraty).
9. **`Scripts/**` nie trafia do DLL** (`<Compile Remove="Scripts/**" />` w csproj, nie usuwaj). Brak tymczasowych flag testowych; jeśli musisz dodać, wpisz do `Docs/Release.md` ("Sprawdzić przed wydaniem").

## Zasady kodu

- **C# bez łamania argumentów** w nowym kodzie (jedna linia, nawet długa); istniejącego formatowania nie przerabiaj. LangVersion 10, net6.0, `Nullable` wyłączone (repo ma własny `NullableShim.cs`).
- **Zmiany pokazywane w rozmowie:** zmieniona metoda w całości; kilka linii + 2 linie kontekstu; nowe składowe z 2 poprzedzającymi liniami; >2 miejsca w pliku = cały plik; nowe pliki w całości. Przy pracy agentowej edytuj pliki i wypisz zmiany.
- **Pełne ścieżki plików** w każdej odpowiedzi.
- **Komentarze po polsku**, mówią "dlaczego" i "co gracz widzi".
- **Logi:** `Log.Msg/Warning` z prefiksem `[OXL]` albo `[OXL:TAG]` (istniejące: `LGSYS`, `PERSIST`, `TIME`, `WRECKER`; `CarPhotoLoader` ma `[PhotoLoader]`), po angielsku, z konkretnymi wartościami. Mod ma przełącznik `oxl_logs on|off`, **domyślnie ciche**: ważne komunikaty błędów (kupno, zapis) muszą iść zawsze.
- **Bez myślników** (`—`, `–`, ` - ` jako myślnik) w nowych tekstach dla gracza (opisy sprzedawców, UI, README, Nexus); istniejących tekstów nie przepisuj masowo bez prośby. Nie dotyczy komentarzy ani `Docs/`.
- **Nie zostawiaj martwego kodu.** Usuwając coś, usuń też wpisy w dokumentach albo wpisz to do `BACKLOG.md`.

## Skrypty REPL (najważniejsze narzędzie badawcze)

Szczegóły i pułapki: `C:\Users\usmat\source\repos\GlideRail\Docs\Research\Repl.md`. Skrót:
- Plik `Scripts/<nazwa>.cs` = **ciało metody** `__Repl.Execute()` (bez `using`, bez klas; pomocniki jako delegaty `Func`/`Action`). Wynik przez `Print("…")`. Długie operacje przez `MelonLoader.MelonCoroutines.Start(...)` (wzór: `totalMaker.cs`, `ConsoleScripts.md`).
- **Przed oddaniem skryptu sprawdź kompilację:** `bash Scripts/_check.sh Scripts/<nazwa>.cs` (harness w `%TEMP%`, referencje m.in. `Il2CppFusion.Runtime`, `CMS2026_OXL.dll`, `BlastCoreOS.dll`, framework).
- **Prosząc o uruchomienie, zawsze podawaj pełną komendę w osobnej linii kodu:** `runfile C:\Users\usmat\source\repos\CMS2026_OXL\Scripts\<nazwa>.cs`. Nigdy sama nazwa pliku (względna ścieżka szuka w folderze konsoli w grze). Komenda to `runfile`, nie `run`.
- Wynik `Print` trafia do `MelonLoader/Latest.log` (prefiks `[CMS2027_Simple__Console]`): czytasz log grepem, nie prosisz o przepisywanie.
- Skrypt, który zmienia stan auta albo pieniądze: napisz w poleceniu "zapisz grę przed".
- Każdy nowy skrypt badawczy = goal z `OUA:` w `Docs/playtestGoals.md`.

## Przebieg pracy

1. Użytkownik opisuje zadanie w `Docs/ZADANIE.md` (komenda `/zadanie`) albo w rozmowie. Punkty są numerowane, odwołuj się do numerów. Wynik dopisuj w sekcji `## Wynik`.
2. **Każda zmiana zachowania w grze = implementacja + goal** w `Docs/playtestGoals.md`. Refaktor bez zmiany zachowania dostaje jeden goal "działa jak przed zmianą".
3. **Kompilacja:** przed oddaniem zmian w kodzie `bash Scripts/_build_check.sh` (kompiluje w `%TEMP%`, nie kopiuje DLL). Potem użytkownik buduje w VS; build kopiuje DLL do `Mods` (target `CopyToMods`), więc **gra musi być zamknięta**. Błędy z VS poprawiasz, zanim zaczniesz coś nowego.
4. **Po teście:** `…\Car Mechanic Simulator 2026 Demo\MelonLoader\Latest.log` (starsze: `MelonLoader\Logs\`) grepem po `[OXL`, słowach i numerach goali (najpierw `oxl_logs on`). Ostrzeżenie `[CursorManager] Release() called with count already 0` = błąd do zbadania. Goal działa → usuń, wniosek do `Docs/Design/` albo `Docs/Research/`. Nie działa → poprawka (nowy goal) albo `BACKLOG.md`. Brak danych → zostaje.
5. **Duże zadania w etapach.** Po etapie raport i czekaj na wynik kompilacji/testu; dalej dopiero po słowie "dalej". Zadanie zmienia decyzję z `Docs/Design/` = nowa decyzja wygrywa, zaktualizuj dokument i napisz o tym.
6. **Dokumenty:** decyzje i zasady działania w `Docs/Design/` (bez kodu), w tej samej partii co decyzja. Nowa wiedza o grze w `Docs/Research/` (z datą i legendą ✔/?), krótki wpis w `Docs/History/log.md`.
7. **Git:** nie commituj bez prośby. Użytkownik przegląda diff i commituje sam.
8. Nie używaj artefaktów, dokumentów Google ani innych dodatków. Wszystko w plikach projektu.
9. Brak dostępu do pliku (np. po kompresji kontekstu) = przerwij i poproś o niego, nie zgaduj zawartości.

## Stan projektu

Wersja w kodzie **0.6.0** (2026, alpha), `MelonGame` jeszcze 2026. **Zależności gotowe na 2027 i wydane:** Konsola, Framework (>= 0.4.0), **BlastCoreOS 0.2.0** (Nexus, 2026-11-23; API dla modułów rozszerzone: przeglądarka z podstronami i `Refresh`, bank `BlastCoreBankAPI`, poczta `BlastCoreMailAPI`). **Teraz (2026-11-25): gruntowna przebudowa OXL jako dedykowanej strony na API BlastCoreOS** (plan w `Docs/ZADANIE.md`): ogłoszenia z paczek zdjęć w folderze gry (kolory, miejsca, stany; sesja fotografa `totalMaker.cs`, zdjęcia w WebP), nowy wygląd panelu, ustawienia poza OXL, dolny wiersz panelu nie mieści się na ekranie. **Stan 2026-11-25 (koniec dnia):** OXL działa w 2027 jako strona `oxl.com` w przeglądarce BlastCoreOS (wygląd serwisu ogłoszeń, `site.md` 11–14): auta ze wszystkich bezpiecznych wersji gry (silnik, felgi, opony z dema), silniki, drogie części, narzędzia i dekoracje, filtry, kurier OXL (jutro/pojutrze) przez system przesyłek gry, bank Redline, maile, Geargle, kalendarz. Wiele goali czeka na test. **Plan na następny dzień: `Docs/ZADANIE.md`.** Archiwum dnia: `Docs/History/zadanie-2026-11-25.md`. Demo 2027 ma 2 nowe auta bez danych OXL. Szczegóły: `Docs/Research/Port2027.md`.
