Przeczytaj Docs/ZADANIE.md i wykonaj opisane tam zadania zgodnie z CLAUDE.md.

- Pierwsza linia pliku w nawiasach kwadratowych `[ … ]` to wiadomość ode mnie (moimi słowami, co robimy). Czytaj ją pierwszą i nigdy jej nie zmieniaj ani nie usuwaj; resztę pliku tworzysz i aktualizujesz Ty.
- Najpierw krótko wypisz plan: co zrobisz i które pliki ruszysz. Jeśli coś jest niejasne albo wymaga mojej decyzji, zapytaj, zanim zaczniesz zmieniać kod.
- Jeśli zadanie odwołuje się do testu, przeczytaj MelonLoader/Latest.log (ścieżka w CLAUDE.md) grepem po słowach z opisu, po `[OXL` (prefiksy `[OXL]`, `[OXL:TAG]`), `[probe]`, `[CMS2027_Simple__Console]` i numerach goali oraz odpowiedzi UA: / OUA: w Docs/playtestGoals.md. Logi OXL są domyślnie ciche: przed testem poproś o `oxl_logs on`.
- Gdy zachowania gry nie da się ustalić z sygnatur, najpierw sprawdź archiwum skryptów konsoli (Docs/Research/ConsoleScripts.md), potem napisz skrypt REPL w Scripts/ (C:\Users\usmat\source\repos\GlideRail\Docs\Research\Repl.md), sprawdź go `bash Scripts/_check.sh Scripts/<nazwa>.cs` i daj mi pełną komendę `runfile C:\Users\usmat\source\repos\CMS2026_OXL\Scripts\<nazwa>.cs`.
- Przed oddaniem zmian w kodzie uruchom `bash Scripts/_build_check.sh`.
- Do każdej zmiany zachowania dopisz goal w Docs/playtestGoals.md (zasady na górze pliku). Zmiana w kupnie / `GameBridge` = goal na kontrakt "pieniądze tylko po udanym spawnie".
- Jeśli zadanie wymaga zmiany w BlastCoreOS (API, przeglądarka), nie edytuj tamtego repo: opisz potrzebę w Docs/BACKLOG.md i w raporcie.
- Na koniec dopisz w Docs/ZADANIE.md sekcję "## Wynik": zmienione pliki (1 linia na plik), nowe komendy konsoli, nowe skrypty REPL i numery nowych goali.
- Zaktualizuj Docs/BACKLOG.md: odhacz zrobione, dopisz nowe znaleziska.
- Po etapie czekaj na "dalej".
