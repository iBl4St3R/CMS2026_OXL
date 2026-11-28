# OXL (Online eX-Owner Lies) for Car Mechanic Simulator 2027

A used market inside the game. OXL is a website in the BlastCoreOS browser (`oxl.com`) where private sellers offer used cars, engines, car parts, tools and decorations. Every seller has a personality: some are honest, some sell wrecks, some are dealers who polish what they sell, and a few are scammers. Read the descriptions, check the photos, compare with the wiki and decide who to trust.

Version **0.7.0**, for Car Mechanic Simulator 2027 (demo and full game).

## What you get

* **Cars** from the game itself: every car version that is safe to spawn in your game build, with year, mileage, power, drive and price based on the car value in the game. A new car added by a game update shows up on OXL without a mod update.
* **Parts tab**: engines (rare, cheaper engines more often), body parts, suspension, exhaust, brakes, gearboxes, electronics, interior, rims and tires with real sizes. Complete engines arrive as one piece and sit on the engine stand like an engine pulled out of a car.
* **Tools and decorations** for your workshop.
* **Sellers who lie in their descriptions.** Honest sellers tell you what is wrong, dealers hide it, wreckers sell scrap, scammers take your money and send you something else. When you open a scam parcel, you find out.
* **Check on wiki** on every car and part listing opens the AutoFreaks page in a new browser window.
* **Payments through the Redline Bank** of BlastCoreOS. A car is paid only after it appears in your parking lot; if the delivery fails, the listing comes back and nothing is charged.
* **OXL courier**: parts arrive tomorrow or the day after (or instantly, see Settings), the parcel is visible in the BlastCoreOS calendar.
* **Listings per game save**: every career has its own market. Leaving without saving also undoes OXL purchases.
* **Settings** on `oxl.com/settings`: delivery, units, prices, number of listings and the mix of sellers.

## Requirements

* MelonLoader 0.7.2
* CMS2026 UITK Framework 0.4.0 or newer (required)
* BlastCoreOS 0.3.0 or newer (required: browser, Redline Bank, mail, calendar, game data)
* CMS2026 Simple Console (optional, for `oxl_*` commands)

The "CMS2026" in the names is not a mistake: the game was announced as 2026 and renamed to 2027.

## Installation

1. Install the requirements.
2. Copy `CMS2026_OXL.dll` to `Mods/`, `SixLabors.ImageSharp.dll` to `UserLibs/` and the folder `CMS2026_OXL` to `Mods/`.
3. Optional: download **CarPhotos BASE** and unpack it into `Mods/CMS2026_OXL/Resources/CarPhotos/`. Without it every car shows the game photo with a STOCK PHOTO badge.
4. Start the game, load a save, sit at a BlastCoreOS computer and open `oxl.com`.

**Coming from 0.6.0 (CMS 2026): clean install.** Delete the old `Mods/CMS2026_OXL` folder first. Old listings, the old photo packs (MEDRES, LOWRES) and `car_spec.json` files are not used anymore.

## Console commands (Simple Console)

* `oxl_status`, `oxl_items [parts|tools]`, `oxl_market`, `oxl_parcels`: show what is going on.
* `oxl_end_all [cars|parts|tools]`: end listings now, new ones come right away.
* `oxl_logs on|off`: detailed log for bug reports (off by default).
* `oxl_difficulty easy|normal|hard`: price level.
* Development and cheats, marked in the command list: `[DEV] oxl_generate`, `[DEV] oxl_items_generate`, `oxl_buyitem`, `oxl_check_now`, `[CHEAT] oxl_deliver` (free parcel), `[CHEAT] oxl_reveal` (real condition of all cars).

## Reporting a bug

Type `oxl_logs on` in the F7 console, repeat the problem and send `MelonLoader/Latest.log`.

## License

Own license: you may use the mod, you may not copy, repackage or rebrand it.
