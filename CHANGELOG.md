# OXL (Online eX-Owner Lies) Changelog

## 0.7.0 (Car Mechanic Simulator 2027)

A complete rebuild for Car Mechanic Simulator 2027. **Clean install required**: delete the old `Mods/CMS2026_OXL` folder before installing. Old listings, the MEDRES and LOWRES photo packs and `car_spec.json` files are not used anymore.

### Requirements
* BlastCoreOS 0.3.0 or newer (required: browser, Redline Bank, mail, calendar, game data, per save storage)
* CMS2026 UITK Framework 0.4.0 or newer (required)
* CMS2026 Simple Console (optional)
* MelonLoader 0.7.2
* `SixLabors.ImageSharp.dll` in `UserLibs` (included, reads the WebP car photos)

### The website
* OXL is now only a website in the BlastCoreOS browser: `oxl.com`. The old panel, the T key and the button in the game shop are gone.
* New look of a classifieds site: home page with categories and fresh listings, lists with groups, sorting and price filters, "More filters" for cars, search, listing pages with photos, parameters, seller card with star rating, location and delivery.
* Tabs: **Cars**, **Parts**, **Tools & Decor**.
* "Message seller": the seller replies by mail in their own style.
* **Check on wiki** on car, engine and part listings opens the AutoFreaks page in a new browser window.
* Geargle search shows OXL listings.
* Footer with the Redline Bank partner logo.

### Cars
* Everything comes from the game data: every car version that is safe to spawn in your game build, with year, mileage, engine, power, torque, drive, gearbox, weight and color. A car added by a game update appears without a mod update.
* Car prices follow the value of the car in the game, its real condition, year, faults and the seller.
* Optional photo pack **CarPhotos BASE v2** (WebP, 1280x720, every demo car in all colors and three body conditions). Photos match the real body condition of the car. Without the pack a car shows the game photo with a STOCK PHOTO badge.
* Photo gallery with arrows, counter and thumbnail strip.
* "Delivering your car" window while the car is brought to your parking lot. With a full parking lot you get a clear message and nothing is charged.
* Wear on delivery now recognizes the parts of engines the old version did not know (for example the V8 D.vik in the Bolt Nevada), including timing faults.

### Parts, tools and decorations
* Parts tab with all car parts from the game: body panels, suspension, exhaust, brakes, gearboxes, electronics, interior, rims and tires.
* Complete engines are rare (cheaper ones more often) and arrive as one piece that sits on the engine stand like an engine pulled out of a car.
* Engine prices are the sum of their parts, as the game values them, and never low enough to resell to the game for profit.
* Rims and tires come with real sizes taken from the cars in the game.
* Tools and decorations for the workshop, without penny junk.
* Used condition is hidden until you examine the part, like in the game.

### Sellers and scams
* Four seller types with levels: honest, wrecker, dealer, scammer.
* Scammers make up versions, colors and horsepower on car listings and send the cheapest item in the game instead of the part you paid for. Opening a scam parcel shows a red message telling you what happened.
* Item scammers have new accounts, low ratings and short lived listings. Engines are rarely sold by scammers.

### Money, delivery and saves
* All payments go through the **Redline Bank** of BlastCoreOS. A car is paid only after it appears in your parking lot; if the delivery fails, the listing comes back and nothing is charged.
* **OXL courier**: parts arrive tomorrow or the day after at a game delivery hour, or instantly (setting). Parcels are visible in the BlastCoreOS calendar and survive quitting the game.
* Order confirmation mails with a small photo.
* **Listings per game save**: every career has its own market. Leaving without saving also undoes OXL purchases.

### Settings (`oxl.com/settings`)
* Delivery (courier or instant), units, prices.
* Number of listings per tab.
* Seller mix with locks: share of each seller type and level.

### Console commands
* `oxl_end_all [cars|parts|tools]` ends listings in one tab or all, new ones appear right away.
* New: `oxl_items`, `oxl_market`, `oxl_parcels`, `oxl_buyitem`.
* Development and cheats are marked in the command list: `[DEV] oxl_generate`, `[DEV] oxl_items_generate`, `[CHEAT] oxl_deliver`, `[CHEAT] oxl_reveal`.
* Removed: `oxl_open`, `oxl_detail`, `oxl_end_current`, `oxl_memory`.
* Logs stay quiet by default; `oxl_logs on` for bug reports.

## Earlier versions (Car Mechanic Simulator 2026)

Full release notes of the 2026 versions: https://github.com/iBl4St3R/CMS2026_OXL/releases

### 0.6.0
* OXL moved into the monitor of the BlastCoreOS computer; BlastCoreOS became a requirement.

### 0.5.1
* Fixed severe FPS drops.
* The balance shown in the panel now updates after a purchase.
* Active filters no longer reset when listings expire or appear.

### 0.5.0
* Dynamic seller descriptions by personality, level and the car's main fault, with color coded keywords.
* Honest sellers no longer hide total wrecks.
* Rebuilt photo backend: car versions, random photo sets, colors in external files.
* 115 generated seller avatars, no repeats among the last 30.
* Expanded filters and sorting.
* Fixed: listings disappearing between sessions, settings not saving, wrecks of honest sellers spawning too clean.
* Price balance. Optional MEDRES photo pack v3.

### 0.4.1
* Fixed: vehicle data missing with the LOWRES pack only, listings not refreshing after sleeping, missing shop button texture.

### 0.4.0
* Seller archetypes (honest, neglected, dealer, scammer) with three levels each, affecting price, rating, photos, description and real condition.
* Pricing rebalance per archetype.
* Filter panel and listing generation settings.
* Auction timers run on game time. Optional MEDRES photo pack v2.

### 0.3.0
* Photo gallery from real in game photography (LOWRES bundled, MEDRES optional), photos by color and condition.
* Simple Console commands.
* Plates, mileage and year of the listing carried over to the spawned car.

### 0.2.1
* Fixed: car parts showing the right condition right after purchase; dents and dust matching the seller and condition.

### 0.2.0
* Category tabs, difficulty settings saved between sessions, seller star ratings, more seller descriptions.
* Purchased cars spawn with realistic wear; prices by archetype; correct production years.
* Sorting by ending time; failed delivery (full parking lot) restores the listing without charging; insufficient funds warning; redesigned details view.

### 0.1.0
* First release.
