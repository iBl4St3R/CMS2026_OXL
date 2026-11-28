# Publikacja (Nexus i GitHub)

> Strona Nexusa: Car Mechanic Simulator 2027 → CMS2026 OXL (ukryta od 2026-10-21, powód: "not supported"; 1 zgłoszony bug, 10 postów, artykuł). Paczka zdjęć MEDRES osobno (v3). Licencja własna.

## Historia wydań

**0.7.0** (2026-11-28: spakowane przez użytkownika, strona na Nexusie niewidoczna do wydania TheBucket; port na CMS 2027): tylko strona `oxl.com` w przeglądarce BlastCoreOS (stary panel usunięty), auta i części wyłącznie z danych gry (bez `car_spec.json` i starych paczek), zakładka Parts z całymi silnikami na stojak, oponami i felgami z rozmiarem, ceny silników z części, scammer z komunikatem przy paczce, kurier OXL z kalendarzem, bank Redline, poczta, Geargle, "Check on wiki", ogłoszenia per zapis gry. Paczka zdjęć osobno: **CarPhotos BASE v2** (WebP, 7 aut demo, ok. 417 MB), rzadko aktualizowana; później dodatki (np. CarPhotos DLC1 v1). Wymaga BlastCoreOS 0.3.0 (API danych gry, kalendarza i katalogu zapisu), wydać razem. Opis: `Docs/NexusOpis.md`, README w repo.

**0.6.0** (Nexus, ostatnia aktualizacja 2026-08-24, CMS 2026, alpha). Integracja z przeglądarką BlastCoreOS, ciche logi (`oxl_logs`), poprawki wysokości (wg commitów).

**0.5.0**: dynamiczne opisy sprzedawców wg osobowości, poziomu i usterki (kolorowane słowa); 115 awatarów z buforem 30 ostatnich; rozbudowane filtry i sortowanie; przebudowa zdjęć (configi, losowe zdjęcia dla tego samego configu i koloru, kolory w plikach); poprawki: znikające ogłoszenia (`listings.dat`), zapis ustawień, zbyt czyste auta uczciwych sprzedawców; balans cen. Paczka MEDRES v3 wymagana.

## Checklista wydania

1. Wersja w `MelonInfo` (`OXLPlugin.cs`) = README = Nexus.
2. `MelonGame` ma obie nazwy 2027.
3. `bash Scripts/_build_check.sh` bez błędów, build Release; paczka: `CMS2026_OXL.dll` + `UserLibs/SixLabors.ImageSharp.dll` (z plikiem `LICENSE` ImageSharp, Apache 2.0 wg Split License) + folder `CMS2026_OXL/Resources` (`avatars/`, `Images/`). Paczka zdjęć `Resources/CarPhotos` (WebP, ok. 60 MB na auto) osobnym opcjonalnym plikiem na Nexusie: **CarPhotos BASE v2** (później dodatki, np. CarPhotos DLC1 v1).
4. Goale wydania zamknięte, w tym kupno, zużycie wg archetypu i zwrot przy błędzie dostawy.
5. Brak tymczasowych flag (lista niżej).
6. README (repo) i opis (`Docs/NexusOpis.md`): 2027, zależności (BlastCoreOS 0.3.0+), czysta reinstalacja, wersja paczki CarPhotos, bez myślników w nowych zdaniach. Linki do zależności wstawia użytkownik.
7. Stary `listings.dat` i cfg wczytują się albo opis wydania jasno każe je usunąć.
8. `CHANGELOG.md` (repo: 0.7.0 i skrót starszych wersji; stary `Docs/changelog.md` usunięty 2026-11-28) wklejony do opisu zmian na Nexusie i GitHubie.
9. "Set to visible" po wrzuceniu pliku 2027 (i odpowiedź na otwarty bug). Dla 0.7.0: dopiero po wydaniu TheBucket (decyzja użytkownika 2026-11-28).

## Sprawdzić przed wydaniem (tymczasowe rzeczy w kodzie)

- ✔ Komendy testowe (decyzja użytkownika 2026-11-28): zostają, oznaczone w opisie `[CHEAT]` (`oxl_deliver`, `oxl_reveal`) i `[DEV]` (`oxl_generate`, `oxl_items_generate`); opisane w README.
- Paczka moda (2026-11-28): bez `Resources/buttons/`, `Resources/icons/`, `Resources/CarImages_LOWRES/`, `CarImages_MEDRES/`, `CarPhotos_BACKUP_OLD/` i plików `car_spec*.json` (stare paczki 2026, kod ich nie czyta, `site.md` 19), tylko `CarPhotos/` (paczka 2027) i `Images/`, `avatars/`; bez `Resources/buttons/` i `Resources/icons/` (grafiki starego panelu, kod już ich nie czyta). Stare wspólne `Mods/CMS2026_OXL/listings.dat` i `items.dat` nie są używane (ogłoszenia per zapis gry, `site.md` 17): czysta instalacja.
