# OXL — Archetype System
## Technical Reference v0.3.0
### Checkpoint

---

## Przegląd

System archetypów kontroluje jak generowane jest ogłoszenie i
w jakim stanie trafia auto do gracza po zakupie.
Każde ogłoszenie ma przypisany `SellerArchetype` i `ArchetypeLevel` (1–3).
Te dwie wartości determinują: apparent, actual, honesty, body,
cenę, rating, TTL, opis i stan mechaniczny po spawnie.

---

## Enum SellerArchetype

```
public enum SellerArchetype { Honest, Neglected, Dealer, Wrecker }
```

### Prawdopodobieństwo losowania (RollArchetype)
| Archetype | Szansa |
|---|---|
| Honest | 20% |
| Neglected | 35% |
| Dealer | 30% |
| Wrecker | 15% |

---

## Kluczowe zmienne per listing

| Zmienna | Opis |
|---|---|
| `ApparentCondition` | Co gracz widzi w ogłoszeniu (0–1) |
| `ActualCondition` | Prawdziwy stan mechaniki (0–1) |
| `BodyCondition` | Stan karoserii po spawnie (0–1) |
| `Honesty` | Stosunek actual/apparent — nie przechowywany, używany przy generowaniu |
| `FairValue` | Uczciwa cena rynkowa dla danego actual (z JSON) |
| `Price` | Cena ogłoszenia (może być > lub < FairValue) |
| `Faults` | FaultFlags — ukryte usterki |

---

## HONEST

### Filozofia
Nie kłamie. L1 nie wie wszystkiego, L3 wie wszystko.
Cena = stan faktyczny. Usterki ujawniane w opisie.

### Apparent (RollApparent)
| Level | Rozkład | Zakres |
|---|---|---|
| L1 Novice | Beta(1.5, 3.5) | 0.08–0.80 |
| L2 Experienced | Beta(2.0, 3.0) | 0.20–0.90 |
| L3 Veteran | Beta(2.5, 2.0) | 0.40–0.95 |

### Honesty (RollHonesty)
| Level | Zakres | Sens |
|---|---|---|
| L1 | 0.88–0.98 | uczciwy ale może nie wiedzieć wszystkiego |
| L2 | 0.92–0.99 | dobrze zna auto |
| L3 | 0.96–1.00 | zna auto w 100% |

### Body (RollBodyCondition)
actual × (0.92–1.04) — karoseria odzwierciedla stan faktyczny

### Cena
base = CalcFairValue(actual)
mult = L1: 0.75–0.90×  L2: 0.90–1.05×  L3: 1.00–1.15×
ceiling = honestL3.Price × 0.75 (hard cap)
rabat za FaultFlags: -6–11% per usterka

### Rating / TTL
| Level | Rating | TTL |
|---|---|---|
| L1 | 3★ lub 4★ | 200–500s |
| L2 | 4★ lub 5★ | 250–600s |
| L3 | 5★ (80%) | 300–700s |

### ApplyWear
Normalny ApplyWear() — kondycja parts zależna od actual + FaultFlags.
StartFloor = 0.20 (Dealer i Honest — auto musi odpalaé).

---

## NEGLECTED

### Filozofia
Nie kłamie celowo — po prostu nie wie co się dzieje z autem.
Sprzedaje tanio bo chce się pozbyć. Im wyższy level tym bardziej
zaniedbane i tańsze.

### Apparent (RollApparent)
| Level | Rozkład | Zakres |
|---|---|---|
| L1 Casual | Beta(1.5, 3.0) | 0.10–0.75 |
| L2 Busy | Beta(1.2, 3.5) | 0.08–0.70 |
| L3 Hoarder | Beta(1.0, 4.0) | 0.05–0.65 |

### Honesty (RollHonesty)
| Level | Zakres | Sens |
|---|---|---|
| L1 | 0.75–0.95 | nie kłamie, nie sprawdzał |
| L2 | 0.65–0.90 | nie serwisował od lat |
| L3 | 0.50–0.80 | zgaduje stan |

### Body (RollBodyCondition)
| Level | Wzór |
|---|---|
| L1 | actual × 0.78–1.00 |
| L2 | actual × 0.68–0.93 |
| L3 | actual × 0.55–0.85 |

### Cena
base = CalcFairValue(actual)
disc = L1: 0.20–0.35×  L2: 0.15–0.28×  L3: 0.08–0.20×
floor = fair × (L1: 0.62  L2: 0.48  L3: 0.32)  ← instant-flip guard

**Uwaga:** floor per-level zapobiega sytuacji gdzie gracz
rozbiera auto na części i od razu zarabia bez naprawy.

### Rating / TTL
| Level | Rating | TTL |
|---|---|---|
| L1 | 3★ lub 4★ | 90–300s |
| L2 | 2★ lub 3★ | 60–200s |
| L3 | 1★ lub 2★ | 40–150s |

### ApplyWear
Normalny ApplyWear() — brak StartFloor (auto może nie odpalać).

---

## DEALER

### Filozofia
Naprawia karoserię, ukrywa mechanikę. Cena bazuje na
apparent (wyglądzie), nie actual. Im wyższy level tym
lepiej ukryta katastrofa mechaniczna.

### Apparent (RollApparent)
| Level | Rozkład | Zakres |
|---|---|---|
| L1 Backyard | Beta(3.0, 1.5) | 0.50–0.88 |
| L2 Pro | Beta(3.5, 1.2) | 0.65–0.95 |
| L3 Criminal | Beta(5.0, 1.0) | 0.80–1.00 |

### Honesty (RollHonesty)
| Level | Zakres | Sens |
|---|---|---|
| L1 | 0.35–0.55 | polakierował i tyle |
| L2 | 0.20–0.40 | głęboki scam |
| L3 | 0.05–0.20 | auto to atrapa |

### Body (RollBodyCondition)
| Level | Zakres | Sens |
|---|---|---|
| L1 | 0.60–0.75 | umył, podmalował |
| L2 | 0.75–0.90 | profesjonalny detailing |
| L3 | 0.90–0.99 | perfekcyjne, nie do odróżnienia |

### Cena
base = CalcFairValue(apparent)  ← wycena od wyglądu, nie mechaniki!
mult = L1: 0.85–1.00×  L2: 0.90–1.05×  L3: 0.72–0.85× ("okazja")

### Rating / TTL
| Level | Rating | TTL |
|---|---|---|
| L1 | 3★ lub 4★ | 300–600s |
| L2 | 4★ lub 5★ | 400–700s |
| L3 | 5★ (80%, sfałszowany) | 500–900s |

### ApplyWear
Normalny ApplyWear() — StartFloor = 0.20 (auto odpala).

---

## WRECKER (SCAMER)

### Filozofia
Totalne kłamstwo. Zdjęcia i opis wiarygodne — auto które
gracz dostaje to złom. Wszystkie części na 0.02, fluidy
spuszczone, rama zniszczona, brak kluczowych elementow np maski, drzwi, pól silnika etc.

### Apparent (RollApparent)
| Level | Rozkład | Zakres |
|---|---|---|
| L1 Amateur | Beta(1.5, 2.5) | 0.15–0.65 |
| L2 Intermediate | Beta(2.5, 2.0) | 0.35–0.80 |
| L3 Expert | Beta(4.0, 1.5) | 0.65–0.98 |

### Honesty (RollHonesty)
| Level | Zakres | Sens |
|---|---|---|
| L1 | 0.25–0.50 | nie umie zbudować dobrego kłamstwa |
| L2 | 0.12–0.30 | lepszy storytelling |
| L3 | 0.03–0.13 | totalna fikcja |

### Body (RollBodyCondition) (TO NIE MA ZNACZENIA - SCAMER ZAWSZE WYBIERA ZDJECIA DO AUTA NAJLEPSZEJ JAKOSCI A ZAKUPIONE AUTO TO SMIEĆ)
| Level | Zakres | Sens |
|---|---|---|
| L1 | actual × 0.90–1.10 (cap 0.75) | łatwy do wykrycia |
| L2 | 0.40–0.65 | trudniejszy do wykrycia |
| L3 | 0.62–0.87 | posprzątany, odmalowany |

### Cena
base = CalcFairValue(apparent)  ← kłamstwo na poziomie wyceny
mult = L1: 1.10–1.40× (przepłata = czerwona flaga)
L2: 0.88–1.10× (rynkowa, brak sygnału)
L3: 0.75–0.90× ("lekko taniej od Honest")

### Rating / TTL
| Level | Rating | TTL |
|---|---|---|
| L1 | 1★ lub 2★ | 120–350s |
| L2 | 2★ lub 3★ | 200–500s |
| L3 | 4★ lub 5★ (sfałszowany) | 300–650s |

### ApplyWear — ApplyWreckerWear()
**Osobna ścieżka — nie używa normalnego ApplyWear().**

SalvageCar()          — bazowa destrukcja przez silnik gry
indexedParts → 0.02   — wszystkie części mechaniczne
carParts → 0.02       — karoseria
SetConditionOnBody(0.02) + SetConditionOnDetails(0.02)
SetFrameCondition(0.02)  + SetDetailsCondtition(0.02)
DrainFluids(true)     — olej, chłodnica, paliwo
Historia: mileage, year, tablica rejestracyjna


---

## CalcFairValue — interpolacja ceny rynkowej

Używana przez Honest (cena = fair) i jako punkt odniesienia
dla Dealer/Wrecker (cena = fairApparent).
actual < 0.38 → lerp(honestL1 × 0.25, honestL1, t)
actual < 0.70 → lerp(honestL1, honestL2, t)
actual ≥ 0.70 → lerp(honestL2, honestL3, t)

Dane cenowe z `car_spec_*.json` → `archetypePrices`.
Fallback do `RollPriceFallback()` jeśli brak pliku JSON.

---

## FaultFlags — ukryte usterki (TO NIE MA ZNACZENIA AUTO TO ZLOM)

```
[Flags] public enum FaultFlags
{
    TimingBelt     = 1 << 0,  // rozrząd — zabije silnik
    HeadGasket     = 1 << 1,  // głowica — najpoważniejsza
    SuspensionWorn = 1 << 2,  // zawieszenie
    BrakesGone     = 1 << 3,  // hamulce
    ExhaustRusted  = 1 << 4,  // wydech
    ElectricalFault= 1 << 5,  // elektryka
    GlassDamage    = 1 << 6,  // szyby
}
```

FaultFlags wpływają na:
- `WearForPart()` — odpowiednie kategorie dostają 0–0.12 zamiast normal
- `RollFaults()` — szansa per archetype+level+actual
- `SelectNote()` — Honest ujawnia usterki w opisie
- `RollPrice()` — Honest obniża cenę za każdą usterkę

**Wrecker ignoruje FaultFlags** — wszystko i tak idzie na 0.02.

---

## Difficulty — mnożnik cen

| Difficulty | PriceMultiplier | Efekt |
|---|---|---|
| Easy | 0.85 | ceny 15% niższe |
| Normal | 1.00 | ceny bazowe |
| Hard | 1.20 | ceny 20% wyższe |

Stosowany jako ostatni krok w RollPrice(), przed zaokrągleniem.
Dotyczy wszystkich archetypów. Nie zmienia FairValue.

---

## PhotoCondition — zdjęcia w ogłoszeniu

| Archetype | Folder zdjęć | Sens |
|---|---|---|
| Dealer | `60100` (Good) | wypolerowane niezależnie od actual |
| Wrecker | `60100` (Good) | kłamliwe zdjęcia |
| Neglected | `030` (Bad) | zapuszczone, brudne |
| Honest | zależnie od actual | 030 / 3060 / 60100 |

---

## Pliki źródłowe

| Plik | Odpowiedzialność |
|---|---|
| `ListingSystem.cs` | generowanie listingów, RollArchetype/Level/Apparent/Honesty/Body/Price/TTL/Rating/Faults/Note |
| `GameBridge.cs` | ApplyWear(), ApplyWreckerWear(), WearForPart() |
| `PartCatalog.cs` | klasyfikacja części → WearCat |
| `CarListing.cs` | model danych listingu |
| `PhotoCondition.cs` | mapowanie archetype → folder zdjęć |
| `OXLSettings.cs` | Difficulty + PriceMultiplier |
| `CarSpecLoader.cs` | ładowanie car_spec_*.json (ceny, dane techniczne) |