# Publikacja (Nexus i GitHub)

> Strona Nexusa: Car Mechanic Simulator 2027 → CMS2026 OXL (ukryta od 2026-10-21, powód: "not supported"; 1 zgłoszony bug, 10 postów, artykuł). Paczka zdjęć MEDRES osobno (v3). Licencja własna.

## Historia wydań

**0.6.0** (Nexus, ostatnia aktualizacja 2026-08-24, CMS 2026, alpha). Integracja z przeglądarką BlastCoreOS, ciche logi (`oxl_logs`), poprawki wysokości (wg commitów).

**0.5.0**: dynamiczne opisy sprzedawców wg osobowości, poziomu i usterki (kolorowane słowa); 115 awatarów z buforem 30 ostatnich; rozbudowane filtry i sortowanie; przebudowa zdjęć (configi, losowe zdjęcia dla tego samego configu i koloru, kolory w plikach); poprawki: znikające ogłoszenia (`listings.dat`), zapis ustawień, zbyt czyste auta uczciwych sprzedawców; balans cen. Paczka MEDRES v3 wymagana.

## Checklista wydania

1. Wersja w `MelonInfo` (`OXLPlugin.cs`) = README = Nexus.
2. `MelonGame` ma obie nazwy 2027.
3. `bash Scripts/_build_check.sh` bez błędów, build Release; paczka: `CMS2026_OXL.dll` + `UserLibs/SixLabors.ImageSharp.dll` (z plikiem `LICENSE` ImageSharp, Apache 2.0 wg Split License) + folder `CMS2026_OXL/Resources` (awatary, ikony, przyciski). Paczka zdjęć `Resources/CarPhotos` (WebP, ok. 60 MB na auto) osobnym plikiem na Nexusie jako opcjonalna.
4. Goale wydania zamknięte, w tym kupno, zużycie wg archetypu i zwrot przy błędzie dostawy.
5. Brak tymczasowych flag (lista niżej).
6. README i opis: 2027, zależności (BlastCoreOS wg decyzji), czysta reinstalacja, wersja paczki MEDRES, bez myślników w nowych zdaniach.
7. Stary `listings.dat` i cfg wczytują się albo opis wydania jasno każe je usunąć.
8. "Set to visible" po wrzuceniu pliku 2027 (i odpowiedź na otwarty bug).

## Sprawdzić przed wydaniem (tymczasowe rzeczy w kodzie)

- Komenda `oxl_deliver` (`OXLPlugin.cs`): test dostawy bez zakupu, za darmo. Przed wydaniem usunąć albo zostawić jako komendę deweloperską z jasnym opisem (2026-11-25).
- Komendy `oxl_items_generate` i `oxl_buyitem` (2026-11-25): narzędzia testowe przed nowymi stronami. `oxl_buyitem` płaci prawdziwie; zdecydować, czy zostają.
