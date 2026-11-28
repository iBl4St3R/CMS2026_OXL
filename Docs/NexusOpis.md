# Opis Nexusa 0.7.0 (BBCode do wklejenia)

> Agent 2026-11-28, pełny revamp (0.7.0 to inny mod niż 0.6.0). Po angielsku, bez myślników. `ZDJECIE:` = wstaw `[img]adres[/img]` po wrzuceniu pliku z `Docs/zdjeciaPodRelease/` do galerii Nexusa. Linki do zależności wstawia użytkownik.
> Zrzuty `aukcja.png` i `aukcja2.png` mają jeszcze starą nazwę zakładki "Engines & Parts" i stronę auta bez "Check on wiki": przed publikacją lepiej zrobić nowe.

```
[center][size=6][b]OXL[/b][/size]
[size=4][i]Online eX-Owner Lies[/i][/size]
[size=3]The used market of Car Mechanic Simulator 2027. Great deals. Honest sellers. Mostly.[/size][/center]

ZDJECIE: aukcja.png

[size=4][b]What is OXL?[/b][/size]
OXL is a classifieds website you open in the [b]BlastCoreOS[/b] browser on your garage computer ([b]oxl.com[/b]). Private sellers put up used cars, engines, car parts, tools and decorations. You read the ad, look at the photos, maybe message the seller, and buy. The car rolls into your parking lot, the parts come with the courier.

The catch: every seller has a personality, and they lie in their descriptions.
[list]
[*][b]Honest people[/b] tell you what is broken and ask a fair price.
[*][b]Wreckers[/b] sell scrap and call it a project.
[*][b]Dealers[/b] polish the body, write "PERFECT * WARRANTY * FINANCE" and hide what is under it.
[*][b]Scammers[/b] write "LIKE NEW!!!", make up versions and horsepower, take your money and send you something else.
[/list]

ZDJECIE: aukcja2.png

[size=4][b]Cars[/b][/size]
Every car version that is safe to spawn in your copy of the game, read straight from the game data: year, mileage, engine, power, torque, drive, gearbox, weight, color. Prices follow the real value of the car and its condition. A car added in a game update shows up on OXL [b]without a mod update[/b]. Filters for make, model, year, mileage, power, drive, condition, seller and price, sorting, photo galleries and seller ratings.
A car is paid only after it appears in your parking lot. If the delivery fails, the listing comes back and nothing is charged.

ZDJECIE: scam3.png

[size=4][b]Parts[/b][/size]
Engines (rare, the cheaper ones more often), body panels, suspension, exhaust, brakes, gearboxes, electronics, interior, rims and tires with real sizes. Complete engines arrive in one piece and go straight onto your engine stand, like an engine pulled out of a car. Prices follow what the game pays for the parts, so there is no free money in reselling.

ZDJECIE: aukcja3.png

[size=4][b]Scams you only discover at home[/b][/size]
A scammer took your money for an engine? You will know when you open the parcel.

[size=4][b]Built into BlastCoreOS[/b][/size]
[list]
[*][b]Redline Bank[/b]: every purchase is paid from your bank account.
[*][b]Mail[/b]: order confirmations with a photo, replies from sellers in their own style.
[*][b]Calendar[/b]: OXL courier parcels on their delivery day.
[*][b]Geargle[/b]: search results include OXL listings.
[*][b]Check on wiki[/b]: every car and part opens its AutoFreaks page in a new browser window. Great for catching a scammer lying about the version.
[/list]

ZDJECIE: mail.png

[size=4][b]Your rules[/b][/size]
[b]oxl.com/settings[/b]: instant delivery or the OXL courier (tomorrow or the day after), units, prices, number of listings per tab and the mix of sellers (more scammers, anyone?). Every game save has its own market, and quitting without saving also undoes your OXL purchases.

[size=4][b]Requirements[/b][/size]
[list]
[*]MelonLoader 0.7.2
[*]CMS2026 UITK Framework 0.4.0 or newer (required)
[*]BlastCoreOS 0.3.0 or newer (required)
[*]CMS2026 Simple Console (optional, console commands)
[/list]
"CMS2026" in the names is not a mistake: the game was announced as 2026.

[size=4][b]Installation[/b][/size]
[list=1]
[*]Install the requirements.
[*]Copy [b]CMS2026_OXL.dll[/b] and the folder [b]CMS2026_OXL[/b] to Mods, and [b]SixLabors.ImageSharp.dll[/b] to UserLibs.
[*]Optional: download [b]CarPhotos BASE v2[/b] (real photos of the demo cars in every color and condition) and unpack it into Mods/CMS2026_OXL/Resources/CarPhotos. Without it, cars show the game photo with a STOCK PHOTO badge.
[*]Load your save, sit at a BlastCoreOS computer and open [b]oxl.com[/b].
[/list]

[b]Updating from 0.6.0 (CMS 2026): clean install.[/b] Delete the old Mods/CMS2026_OXL folder first. The old photo packs (MEDRES, LOWRES) and car_spec.json files are not used anymore.

[size=4][b]Console commands[/b][/size]
oxl_status, oxl_items, oxl_market, oxl_parcels, oxl_end_all [cars|parts|tools], oxl_logs on|off, oxl_difficulty easy|normal|hard. Development and cheats are marked [DEV] and [CHEAT] in the command list.

[size=4][b]Reporting a bug[/b][/size]
Type [b]oxl_logs on[/b] in the F7 console, repeat the problem and send MelonLoader/Latest.log.
```
