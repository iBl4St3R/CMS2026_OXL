# Bug: po otwarciu konsoli (F7) postać dalej idzie i obraca się

> Od agenta BlastCoreOS, 2026-11-22. Ten sam bug był w BlastCoreOS i jest tam naprawiony (test w grze ✔, G90 w repo BlastCoreOS). Szczegóły i log: `C:\Users\usmat\source\repos\BlastCoreOS\Docs\Research\SimpleKCC.md`.

## Objaw (zgłoszenie użytkownika)

Gracz idzie do przodu i obraca myszą w lewo, wciska F7: konsola się otwiera, a postać dalej idzie przed siebie i kamera dalej obraca się w lewo z tą samą prędkością, mimo że mysz już steruje kursorem. Po zamknięciu konsoli postać jest kilka metrów dalej i patrzy w inną stronę.

## Przyczyna

- `Il2CppCMS.Player.Controller.PlayerInput` to `NetworkBehaviour` Fusion. Jego `currentInput` (`PlayerInputData`: `MoveDirection`, `LookRotationDelta`, `State`, ...) to **stan sieciowy** (stąd `CopyBackingFieldsToState`; pole `_currentInput` to tylko kopia sprzed `Spawned`).
- `currentInput` odświeża co tick metoda `PlayerInput.BeforeTick`, wołana przez Fusion.
- `DevConsoleComponent.SetGameInputEnabled` robi `_playerInput.enabled = enabled`. **Wyłączony komponent = Fusion nie woła `BeforeTick`**, więc ostatni input zostaje zamrożony. `PlayerMovement` (ruch, `SimpleKCC`) i obrót kamery dalej go wykonują co tick.
- `InputManager.EnableGameplay(false)` tego nie zatrzymuje, bo input już siedzi w stanie sieciowym.

Dowód z logu BlastCoreOS: przy samym `enabled = false` delta obrotu przy wejściu i przy wyjściu była identyczna (np. `-0.12, -0.87`), a postać przeszła 2–8 m w trakcie, gdy input był "wyłączony".

## Jak naprawić (sprawdzone w BlastCoreOS)

W `DevConsoleComponent.SetGameInputEnabled(bool enabled)`:

1. **Nie wyłączaj komponentu.** Usuń `_playerInput.enabled = enabled;`. Jeśli komponent jest wyłączony (np. przez starą wersję innego moda), włącz go: `if (!pi.enabled) pi.enabled = true;`.
2. Zablokuj input API gry: `pi.EnableInput(enabled);` (ustawia flagę `disableInput`), potem `pi.ResetInputData();`.
3. Wyzeruj input, który już jest w stanie sieciowym:
   ```csharp
   var empty = default(Il2CppCMS.Player.Controller.PlayerInputData);
   pi.currentInput = empty;      // stan sieciowy: to czyta PlayerMovement
   pi._currentInput = empty;
   pi.accumulatedInput = empty;  // zebrany ruch myszy między tickami
   ```
   Wszystkie trzy właściwości mają settery w interopie (`Il2CppCMS/Player/Controller/PlayerInput.cs` w zrzucie `cms2027assembly`). Każde wywołanie w `try/catch`.
4. Zrób krok 3 przy otwarciu i przy zamknięciu konsoli. BlastCoreOS robi to dodatkowo co klatkę, póki komputer jest otwarty (zapas); log nie pokazał, żeby input wracał w trakcie, więc wystarczy `EnableInput(false)` + jednorazowe zerowanie. Zerowanie co klatkę w `Update`, gdy konsola jest widoczna, jest tanie i bezpieczne.
5. `_playerInput` jest cache'owany w polu komponentu. Po zmianie sceny może wskazywać zniszczony obiekt, więc szukaj `PlayerInput` od nowa, gdy `_playerInput == null` albo przy każdym wywołaniu (tak robi BlastCoreOS).
6. `InputManager.EnableGameplay` i przełączanie mapy "UI Common" mogą zostać bez zmian.

Wzór kodu: `C:\Users\usmat\source\repos\BlastCoreOS\00 ComputerSystem\BlastCoreScreenViewpoint.cs`, metody `SetPlayerInput` i `ZeroPlayerInput` (tylko odczyt).

## Test w grze

Idź do przodu, obracając myszą, i w trakcie wciśnij F7. Przy otwartej konsoli spróbuj WASD i poruszaj myszą. Postać ma stać w miejscu i się nie obracać. Po zamknięciu konsoli chodzenie i mysz działają normalnie, postać stoi tam, gdzie była przy otwarciu.
