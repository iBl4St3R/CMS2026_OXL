// PartCatalog.cs
// Universal part classification table derived from live game dumps. 
// All 5 CMS2026 Demo cars: DNB Censor, Katagiri Tamago BP,
// Luxor Streamliner Mk3, Mayen M5, Salem Aries MK3.
//
// Use PartCatalog.Classify(partId) — returns WearCat.
// Handles camelCase variants, trailing _N suffixes, and engine-type prefixes
// via normalization + progressive suffix stripping.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CMS2026_OXL
{
    // ══════════════════════════════════════════════════════════════════════════
    //  WEAR CATEGORIES
    //  Gameplay-relevant groupings. Each maps to a specific wear curve
    //  in WearForCategory() and may respond to FaultFlags.
    // ══════════════════════════════════════════════════════════════════════════
    public enum WearCat
    {
        // ── Consumables (always low regardless of condition) ──────────────────
        SparkPlug,          // swieca_* — świece zapłonowe
        FilterOil,          // filtr_oleju — filtr oleju
        FilterFuel,         // filtr_paliwa — filtr paliwa
        FilterAir,          // filtr_srodek, obudowa_filtra — filtr powietrza

        // ── Timing system (TRAP — responds to FaultFlags.TimingBelt) ─────────
        TimingChain,        // łańcuch/pasek rozrządu — will kill the engine
        TimingTensioner,    // napinacz — worn together with chain
        TimingRoller,       // rolki — worn together with chain

        // ── Brakes (responds to FaultFlags.BrakesGone) ────────────────────────
        BrakeFriction,      // klocki, szczęki — pads/shoes
        BrakeDisc,          // tarcze — discs/drums
        BrakeCaliper,       // zaciski, jarzma, tłoczki — calipers
        BrakeBooster,       // serwoHamulca — brake servo
        AbsSystem,          // absModul, absPompa

        // ── Suspension (responds to FaultFlags.SuspensionWorn) ────────────────
        Shock,              // amortyzator — shocks/struts
        Spring,             // sprezyna — coil springs + mounts
        Bushing,            // tuleja — all bushings
        Wishbone,           // wahacz — control arms
        Stabilizer,         // stabilizator + łączniki — sway bar + links
        Knuckle,            // zwrotnica — steering/wheel knuckle
        Hub,                // piasta — wheel hub
        Bearing,            // lozysko — wheel/knuckle bearings
        Steering,           // drazek, koncowka, przekladnia — tie rods + rack
        Subframe,           // sanki — front/rear subframe cradle

        // ── Clutch ────────────────────────────────────────────────────────────
        Clutch,             // docisk, tarcza_sprzegla, lozysko dociskowe
        Flywheel,           // kolo_zamachowe

        // ── Engine internals ──────────────────────────────────────────────────
        EngineBlock,        // blok silnika
        Crankshaft,         // wal korbowy
        Piston,             // tlok + pierscienie + pokrywa stopy
        CylinderHead,       // glowica (responds to FaultFlags.HeadGasket)
        CamValve,           // walek rozrzadu + dzwignie + popychacze

        // ── Exhaust (responds to FaultFlags.ExhaustRusted) ───────────────────
        Muffler,            // tlumik koncowy/srodkowy
        Catalyst,           // katalizator
        ExhaustPipe,        // rury laczace + rury poczatkowe
        ExhaustManifold,    // kolektor wydechowy

        // ── Cooling ───────────────────────────────────────────────────────────
        Radiator,           // chlodnica
        CoolingFan,         // wentylator chlodnicy
        WaterPump,          // pompa wody
        CoolantSystem,      // zbiorniczek + korki plynow

        // ── Electrical (responds to FaultFlags.ElectricalFault) ──────────────
        Alternator,         // alternator
        Battery,            // akumulator
        Starter,            // rozrusznik
        Ecu,                // ecu
        Relay,              // relay
        Fuse,               // fusemedium, fusebox
        IgnitionCoil,       // cewka + kable zapłonowe
        Distributor,        // rozdzielacz zaplonu + moduly

        // ── Drivetrain ────────────────────────────────────────────────────────
        Gearbox,            // skrzynia biegów
        DriveShaft,         // Drive_Shaft + polos napedowa
        Differential,       // dyfer
        TransferCase,       // skrzYnia rozdzielcza

        // ── Intake / Fuel ─────────────────────────────────────────────────────
        Intake,             // kolektor dolotowy
        Throttle,           // przepustnica
        Injector,           // listwa wtryskowa + wtryskiwacze
        FuelPump,           // pompa paliwa
        FuelTank,           // bak
        Turbo,              // turbo + intercooler

        // ── Ancillary ─────────────────────────────────────────────────────────
        PowerSteering,      // pompa wspomagania + zbiorniczek

        // ── Non-wear ──────────────────────────────────────────────────────────
        Hardware,           // klipsy, korki, bagnety — fasteners, caps, plugs
        Structural,         // miska olejowa, osłony rozrzadu, pokrywy, wszystko inne
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PART CATALOG
    //  Main public API: PartCatalog.Classify(partId) → WearCat
    // ══════════════════════════════════════════════════════════════════════════
    public static class PartCatalog
    {
        /// <summary>
        /// Classifies a raw game part ID to a WearCat.
        /// Handles camelCase (via ToLower), trailing _N variants (via regex strip),
        /// engine-type suffixes like _sohc/_stara (via config strip),
        /// and unknown suffixes (via progressive underscore stripping).
        /// Returns WearCat.Structural as safe fallback for unknown parts.
        /// </summary>
        public static WearCat Classify(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return WearCat.Structural;

            string norm = Normalize(partId);

            // Direct lookup
            if (_table.TryGetValue(norm, out var cat)) return cat;

            // Progressive: strip last _segment until match or exhausted
            string s = norm;
            while (s.Contains("_"))
            {
                s = s.Substring(0, s.LastIndexOf('_'));
                if (_table.TryGetValue(s, out cat)) return cat;
            }

            return WearCat.Structural;
        }

        // ── Normalization pipeline ─────────────────────────────────────────────
        // 1. ToLower
        // 2. Iteratively strip: trailing _N / _double, then known config suffixes
        // 3. Strip bare trailing digits (e.g. r4_pasek1 → r4_pasek)
        // Result is the canonical lookup key used in _table.

        private static readonly Regex _trailNumRx =
            new Regex(@"(_\d+|_double)+$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Suffixes that describe engine config variants, not functional parts
        private static readonly Regex _configSuffixRx =
            new Regex(@"_(sohc|stara)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static string Normalize(string id)
        {
            id = id.ToLower().Trim();
            string prev;
            do
            {
                prev = id;
                id = _trailNumRx.Replace(id, "");
                id = _configSuffixRx.Replace(id, "");
            }
            while (id != prev);
            // Strip bare trailing digits: r4_pasek1 → r4_pasek, w_rurax12 → w_rurax
            id = Regex.Replace(id, @"\d+$", "");
            return id;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CLASSIFICATION TABLE
        //  Keys: normalized (lowercase, trailing _N stripped).
        //  Unknown parts not listed here are caught by progressive stripping
        //  or fall back to WearCat.Structural.
        //
        //  Derived from live dumps of all 5 CMS2026 Demo cars.
        // ══════════════════════════════════════════════════════════════════════
        private static readonly Dictionary<string, WearCat> _table =
            new Dictionary<string, WearCat>(StringComparer.OrdinalIgnoreCase)
        {
            // ── TIMING SYSTEM ─────────────────────────────────────────────────
            // Chain / belt variants across all 5 engines
            { "v8_zz_lancuch",              WearCat.TimingChain },  // DNB V6/231 (chain-based engine, named v8_zz)
            { "v8_350_lancuch",             WearCat.TimingChain },  // Luxor V8/350
            { "i4_lancuch",                 WearCat.TimingChain },  // Mayen I4
            { "v6_231_pasek",               WearCat.TimingChain },  // Salem V6/231 (belt)
            { "r4_pasek",                   WearCat.TimingChain },  // Tamago R4 (r4_pasek1/2/3_sohc → strip digit + _sohc)
            { "i4_pasek",                   WearCat.TimingChain },  // Mayen I4 (i4_pasek_1/2)
            { "v8_350_pasek",               WearCat.TimingChain },  // Luxor V8 (v8_350_pasek_1/2)
            // Tensioners
            { "v6_231_napinacz",            WearCat.TimingTensioner },
            { "v8_350_napinacz",            WearCat.TimingTensioner },
            { "v8_napinacz",                WearCat.TimingTensioner },  // v8_napinacz_2
            // Rollers / idler pulleys
            { "v8_350_rolkawalka",          WearCat.TimingRoller },
            { "v8_rolkawalka",              WearCat.TimingRoller },  // v8_rolkaWalka_stara → strip _stara
            { "i4_rolkawalka",              WearCat.TimingRoller },
            { "rolka_walka",                WearCat.TimingRoller },  // rolka_walka_sohc → strip _sohc
            { "rolka_pompy_wody",           WearCat.TimingRoller },  // water pump idler
            { "rolka_gladka",               WearCat.TimingRoller },  // smooth idler



            // ── SPARK PLUGS ───────────────────────────────────────────────────
            { "swieca",                     WearCat.SparkPlug },     // swieca_1/2/4

            // ── FILTERS ───────────────────────────────────────────────────────
            { "v8_filtr_oleju",             WearCat.FilterOil },
            { "r4_filtr_oleju",             WearCat.FilterOil },
            { "filtr_paliwa",               WearCat.FilterFuel },    // filtr_paliwa_1
            { "filtr",                      WearCat.FilterAir },     // filtr_1 (generic)
            { "filtr_srodek",               WearCat.FilterAir },     // filtr_srodek_2/3/5
            { "obudowa_filtra",             WearCat.FilterAir },     // obudowa_filtra_N_gora/dol → progressive
            { "v10_3_filtr_dol",            WearCat.FilterAir },     // DNB specific
            { "v10_3_filtr_gora",           WearCat.FilterAir },

            // ── BRAKES ────────────────────────────────────────────────────────
            { "klockihamulcowe",            WearCat.BrakeFriction }, // klockiHamulcowe_1/3/4
            { "szczeki",                    WearCat.BrakeFriction }, // drum brake shoes (Luxor)
            { "tarczawentylowana",          WearCat.BrakeDisc },     // tarczaWentylowana_1
            { "tarczahamulcowa",            WearCat.BrakeDisc },     // tarczaHamulcowa_1
            { "pokrywabeben",               WearCat.BrakeDisc },     // drum cover (Luxor)
            { "zaciskhamulcowy",            WearCat.BrakeCaliper },  // zaciskHamulcowy_1/3/4
            { "zaciskhamulcowy_tloczek",    WearCat.BrakeCaliper },  // caliper piston
            { "jarzmozaciskuhamulcowego",   WearCat.BrakeCaliper },  // caliper bracket
            { "szczekicylinder",            WearCat.BrakeCaliper },  // drum wheel cylinder
            { "serwohamulca",               WearCat.BrakeBooster },  // serwoHamulca_1 + _cap via progressive
            { "absmodul",                   WearCat.AbsSystem },
            { "abspompa",                   WearCat.AbsSystem },

            // ── SUSPENSION — Shocks ───────────────────────────────────────────
            { "amortyzator",                WearCat.Shock },         // amortyzator_double
            { "amortyzatorprzod",           WearCat.Shock },         // amortyzatorPrzod_1
            { "amortyzatortyl",             WearCat.Shock },         // amortyzatorTyl_1/2

            // ── SUSPENSION — Springs ──────────────────────────────────────────
            { "sprezynnaprzod",             WearCat.Spring },        // sprezynnaPrzod_1 (double-n)
            { "sprezyna",                   WearCat.Spring },        // generic spring
            { "sprezynatyl",                WearCat.Spring },        // sprezynaTyl_1
            { "czapkaamorprzod",            WearCat.Spring },        // strut top mount
            { "czapkasprezynytyl",          WearCat.Spring },
            { "podstawkasprezynytyl",       WearCat.Spring },

            // ── SUSPENSION — Bushings ─────────────────────────────────────────
            { "tuleja",                     WearCat.Bushing },       // tuleja_1 (large)
            { "tulejamala",                 WearCat.Bushing },       // tulejaMala_1 (small)
            { "pioratyl",                   WearCat.Bushing },       // leaf spring plates (DNB)

            // ── SUSPENSION — Wishbones ────────────────────────────────────────
            { "wahaczdol",                  WearCat.Wishbone },      // wahaczDol_double
            { "wahaczgora",                 WearCat.Wishbone },      // wahaczGora_double
            { "wahaczdolny",                WearCat.Wishbone },      // wahaczDolny_1
            { "wahacztyl",                  WearCat.Wishbone },      // wahaczTyl_1/2/3
            { "wahaczkrotkityl",            WearCat.Wishbone },
            { "wahaczdlugityl",             WearCat.Wishbone },
            { "wahaczmost",                 WearCat.Wishbone },      // solid axle links (DNB)
            { "wahaczprostytyl",            WearCat.Wishbone },

            // ── SUSPENSION — Stabilizers ──────────────────────────────────────
            { "stabilizatorprzod",          WearCat.Stabilizer },   // stabilizatorPrzod_1/2/4
            { "stabilizatortyl",            WearCat.Stabilizer },
            { "lacznikstabilizatora",       WearCat.Stabilizer },   // lacznikStabilizatora_double
            { "lacznikstabilizatoraprzod",  WearCat.Stabilizer },
            { "lacznikstabtyl",             WearCat.Stabilizer },

            // ── SUSPENSION — Knuckles ─────────────────────────────────────────
            { "zwrotnicaprzod",             WearCat.Knuckle },      // zwrotnicaPrzod_1, _double3 → progressive
            { "zwrotnicatyl",               WearCat.Knuckle },      // zwrotnicaTyl_1/2
            { "zwrotnicatylsztywna",        WearCat.Knuckle },      // zwrotnicaTylSztywna_1 (DNB)
            { "zwrotnicatylbeben",          WearCat.Knuckle },      // drum brake knuckle (Luxor)
            { "zaslepkazwrotnicatyl",       WearCat.Hardware },     // just a rubber plug

            // ── SUSPENSION — Bearings / Hubs ──────────────────────────────────
            { "lozyskozwrotnicyprzod",      WearCat.Bearing },
                 
            { "piastaprzod",                WearCat.Hub },
            { "piastatyl",                  WearCat.Hub },          // piastaTyl_1/3

            // ── SUSPENSION — Steering ─────────────────────────────────────────
            { "drazekkierowniczy",          WearCat.Steering },
            { "koncowkadrazkakier",         WearCat.Steering },
            { "przekladnia",                WearCat.Steering },     // steering rack

            // ── SUSPENSION — Subframe ─────────────────────────────────────────
            { "sankiprzod",                 WearCat.Subframe },     // sankiPrzod_1/5/7
            { "sankityl",                   WearCat.Subframe },

            // ── SUSPENSION — Solid axle / misc ───────────────────────────────
            { "mosttylny",                  WearCat.Structural },   // solid rear axle housing
            { "mosttylnisztywny",           WearCat.Structural },   // mostTylnySztywny → .ToLower()
            { "obejmapodstawka",            WearCat.Hardware },     // leaf spring clamps
            { "obejmapret",                 WearCat.Hardware },
            { "dyferprzod",                 WearCat.Differential }, // dyferPrzod_3

            // ── CLUTCH ────────────────────────────────────────────────────────
            { "docisk_sprzegla",            WearCat.Clutch },
            { "tarcza_sprzegla",            WearCat.Clutch },
            { "lozyskodociskowe",           WearCat.Clutch },
            { "v8_kolo_zamachowe",          WearCat.Flywheel },

            // ── ENGINE BLOCKS ─────────────────────────────────────────────────
            { "v6_231_blok",                WearCat.EngineBlock },
            { "v8_350_blok",                WearCat.EngineBlock },
            { "r4_blok",                    WearCat.EngineBlock },
            { "i4_blok",                    WearCat.EngineBlock },
            { "pokrywa_lozyska", WearCat.EngineBlock },

            // Oil pans — structural, not a wear item
            { "v6_231_miska_olejowa",       WearCat.Structural },   // _stara stripped by normalize
            { "v8_350_miska_olejowa",       WearCat.Structural },
            { "r4_miska_olejowa",           WearCat.Structural },
            { "i4_miska_olejowa",           WearCat.Structural },

            // ── CRANKSHAFT ────────────────────────────────────────────────────
            { "v6_231_walkorbowy",          WearCat.Crankshaft },   // v6_231_walKorbowy
            { "v8_350_walkorbowy",          WearCat.Crankshaft },
            { "walkorbowy",                 WearCat.Crankshaft },   // walKorbowy_2
            { "i4_walkorbowy",              WearCat.Crankshaft },   // i4_walKorbowy_b → progressive strips _b

            // ── PISTONS (child parts hit via progressive: tlok_1_pierscienie → tlok) ──
            { "tlok",                       WearCat.Piston },

            // ── CYLINDER HEADS ────────────────────────────────────────────────
            { "v6_231_glowica",             WearCat.CylinderHead }, // _1 / _2 stripped
            { "v8_350_glowica",             WearCat.CylinderHead },
            { "r4_glowica",                 WearCat.CylinderHead }, // r4_glowica_sohc_1 → strip _1, _sohc
            { "i4_glowica",                 WearCat.CylinderHead },
            // Valve covers — structural
            { "v6_231_pokrywa_glowicy",     WearCat.Structural },
            { "v8_350_pokrywa_glowicy",     WearCat.Structural },
            { "r4_pokrywa_glowicy",         WearCat.Structural },   // _sohc stripped
            { "i4_pokrywa_glowicy",         WearCat.Structural },
            // Timing covers — structural
            { "v6_231_oslona_rozrzadu",     WearCat.Structural },
            { "v8_350_oslona_rozrzadu",     WearCat.Structural },
            { "i4_oslona_rozrzadu",         WearCat.Structural },   // _tyl variant via progressive

            // ── CAMSHAFT / VALVETRAIN ─────────────────────────────────────────
            { "v6_231_walek_popychaczy",    WearCat.CamValve },
            { "v8_350_walek_popychaczy",    WearCat.CamValve },
            { "r4_walek_rozrzadu",          WearCat.CamValve },     // _sohc_1 stripped
            { "i4_walek_rozrzadu",          WearCat.CamValve },
            { "v8_dzwigniazaworu",          WearCat.CamValve },
            { "v6_231_popychacz",           WearCat.CamValve },
            { "v8_350_popychacz",           WearCat.CamValve },
            // Ignition wires (spark plug cables)
            { "v6_231_kable",               WearCat.IgnitionCoil },
            { "v8_350_kable",               WearCat.IgnitionCoil },
            { "r4_kable",                   WearCat.IgnitionCoil }, // r4_kable_sohc → strip _sohc
            // Engine misc hardware
            { "korek_spustowy",             WearCat.Hardware },     // drain plug
            { "korekoleju",                 WearCat.Hardware },     // oil filler cap (korekOleju_1/3)
            { "bagnet",                     WearCat.Hardware },     // dipstick
            { "v8_350_raczka",              WearCat.Hardware },     // crank handle (Luxor)
            { "v8_kolo_pasowe_walu",        WearCat.Structural },   // harmonic balancer
            { "r4_kolo_pasowe_walu",        WearCat.Structural },
            { "v8_350_kolo_pasowe_walu",    WearCat.Structural },
            { "v8_350_kolo_pasowe_pompy_wspomagania", WearCat.Structural },

            // ── EXHAUST ───────────────────────────────────────────────────────
            { "w_tlumik_koncowy",           WearCat.Muffler },      // _10/11 stripped
            { "w_tlumik_srodkowy_b",        WearCat.Muffler },      // _b stays, _srodkowy via progressive if needed
            { "w_katalizator",              WearCat.Catalyst },
            { "w_ruralaczaca",              WearCat.ExhaustPipe },  // w_ruraLaczaca6/12 → strip digit
            { "v6_231_kolektor_wydechowy",  WearCat.ExhaustManifold }, // _5/6 stripped
            { "v8_350_kolektor_wydechowy",  WearCat.ExhaustManifold },
            { "i4_kolektor_wydechowy",      WearCat.ExhaustManifold },
            { "r4_kolektor_wydechowy",      WearCat.ExhaustManifold }, // _oslona via progressive
            // Engine-specific initial downpipes
            { "w_v6_231_rwd_poczatkowy",    WearCat.ExhaustPipe },  // DNB (w_v6_231_rwd_poczatkowy_B_h1)
            { "w_v6_231_poczatkowy",        WearCat.ExhaustPipe },  // Salem (w_v6_231_poczatkowy_h0)
            { "w_v8_350_poczatkowy",        WearCat.ExhaustPipe },  // Luxor/Mayen (_1 / _h3 stripped)
            { "w_i4_poczatkowy",            WearCat.ExhaustPipe },  // Mayen
            { "w_r4_poczatkowy",            WearCat.ExhaustPipe },  // Tamago

            // ── COOLING ───────────────────────────────────────────────────────
            { "chlodnica",                  WearCat.Radiator },
            { "wentylatorchlodnicy",        WearCat.CoolingFan },   // wentylatorChlodnicy_1_fan_1 → progressive
            { "v6_231_pompa_wody",          WearCat.WaterPump },
            { "v8_350_pompa_wody",          WearCat.WaterPump },
            { "r4_pompa_wody",              WearCat.WaterPump },
            { "coolant_reservoir",          WearCat.CoolantSystem }, // _4_body → progressive
            { "coolant_cap",                WearCat.CoolantSystem },

            // ── ELECTRICAL ────────────────────────────────────────────────────
            { "alternator",                 WearCat.Alternator },
            { "i6_old_alternator",          WearCat.Alternator },   // Luxor (oldschool)
            { "akumulator",                 WearCat.Battery },      // akumulator_4/5
            { "v8_rozrusznik",              WearCat.Starter },
            { "r4_rozrusznik",              WearCat.Starter },
            { "ecu",                        WearCat.Ecu },          // ecu_1/3/4
            { "relay",                      WearCat.Relay },        // relay_1/2/3
            { "fusemedium",                 WearCat.Fuse },         // fuseMedium_1/2/3
            { "fusebox",                    WearCat.Fuse },         // fuseBox_1/2 + _top/_bottom via progressive
            { "cewka",                      WearCat.IgnitionCoil }, // cewka_1 (Mayen coil-on-plug)
            { "v6_231_rozdzielaczzaplonu",  WearCat.Distributor },  // v6_231_rozdzielaczZaplonu
            { "v8_350_rozdzielaczzaplonu",  WearCat.Distributor },
            { "r4_modul_zaplonowy",         WearCat.Distributor },  // r4_modul_zaplonowy_sohc → strip _sohc

            // ── DRIVETRAIN ────────────────────────────────────────────────────
            { "v6_231_gearbox",             WearCat.Gearbox },      // _rwd / _short via progressive
            { "v6_231_gearbox_rwd",         WearCat.Gearbox },      // explicit for safety
            { "r4_gearbox",                 WearCat.Gearbox },      // r4_gearbox_short → progressive
            { "v8_350_gearbox",             WearCat.Gearbox },
            { "skrzyniarozdzielcza",        WearCat.TransferCase },
            { "drive_shaft",                WearCat.DriveShaft },   // Drive_Shaft_1/3/4
            { "polosnapedowatylna",         WearCat.DriveShaft },

            // ── INTAKE / FUEL ─────────────────────────────────────────────────
            { "v6_231_kolektor_dolotowy",   WearCat.Intake },
            { "v8_350_kolektor_dolotowy",   WearCat.Intake },
            { "r4_kolektor_dolotowy",       WearCat.Intake },       // r4_kolektor_dolotowy4 → strip digit
            { "i4_kolektor_dolotowy",       WearCat.Intake },
            { "v6_231_przepustnica",        WearCat.Throttle },
            { "v8_przepustnica",            WearCat.Throttle },
            { "r4_przepustnica",            WearCat.Throttle },
            { "v6_30_przepustnica",         WearCat.Throttle },
            { "v6_231_listwa_wtryskowa",    WearCat.Injector },
            { "v8_350_listwa_wtryskowa",    WearCat.Injector },
            { "listwa_wtryskowa",           WearCat.Injector },
            { "i4_wtrysk",                  WearCat.Injector },
            { "pompa",                      WearCat.FuelPump },     // pompa_1 (fuel pump)
            { "bak",                        WearCat.FuelTank },     // bak_1
            { "i4_turbo",                   WearCat.Turbo },
            { "intercooler_small",          WearCat.Turbo },

            // ── POWER STEERING ────────────────────────────────────────────────
            { "v8_zz_pompa_wspomagania",    WearCat.PowerSteering },
            { "v8_350_pompawspomagania",    WearCat.PowerSteering }, // v8_350_pompaWspomagania → lowercase
            { "r4_pompa_wspomagania",       WearCat.PowerSteering },
            { "i4_pompa_wspomagania",       WearCat.PowerSteering },
            { "power_steering_reservoir",   WearCat.PowerSteering }, // _1_body → progressive
            { "power_steering_cap",         WearCat.Hardware },
            { "windscreen_washer_reservoir",WearCat.Hardware },
            { "windscreen_washer_cap",      WearCat.Hardware },

            // ── HARDWARE / FASTENERS ──────────────────────────────────────────
            { "klips",                      WearCat.Hardware },     // klips_1/2
        };
    }
}