using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace CMS2026_OXL
{
    public class ListingSystem
    {
        // ── Car definitions — nowe widełki skalibrowane do cen skupu ─────────
        private class CarDef
        {
            public string Make, Model, ImageFolder, InternalId;
            public int MinYear, MaxYear, MinPrice, MaxPrice;
        }

        private static readonly CarDef[] CarDefs =
 {
    new CarDef { Make = "DNB",      Model = "Censor",         ImageFolder = "DNB Censor",
                 InternalId = "car_dnb_censor",
                 MinYear = 1991, MaxYear = 1992, MinPrice = 3500,  MaxPrice = 12000 },
    new CarDef { Make = "Katagiri", Model = "Tamago BP",       ImageFolder = "Katagiri Tamago BP",
                 InternalId = "car_katagiri_tamago",
                 MinYear = 2000, MaxYear = 2005, MinPrice = 3500,  MaxPrice = 12000 },
    new CarDef { Make = "Luxor",    Model = "Streamliner Mk3", ImageFolder = "Luxor Streamliner Mk3",
                 InternalId = "car_luxor_streamliner",
                 MinYear = 1992, MaxYear = 1996, MinPrice = 4000,  MaxPrice = 14000 },
    new CarDef { Make = "Mayen",    Model = "M5",              ImageFolder = "Mayen M5",
                 InternalId = "car_mayen_m5",
                 MinYear = 2008, MaxYear = 2015, MinPrice = 10000, MaxPrice = 26000 },
    new CarDef { Make = "Salem",    Model = "Aries MK3",       ImageFolder = "Salem Aries MK3",
                 InternalId = "car_salem_aries",
                 MinYear = 1994, MaxYear = 1999, MinPrice = 3000,  MaxPrice = 11000 },
};


        // ══════════════════════════════════════════════════════════════════════
        //  KOLORY — 4 aktywne per auto, reszta w komentarzu do przyszłego użycia
        // ══════════════════════════════════════════════════════════════════════

        // GetColorRegistry przekazuje AllColors — loader używa ich jako fallback
        public static Dictionary<string, (string carId, string[] colors)> GetColorRegistry()
        {
            var result = new Dictionary<string, (string, string[])>();
            foreach (var def in CarDefs)
            {
                if (AllColors.TryGetValue(def.InternalId, out var colors))
                    result[def.ImageFolder] = (def.InternalId, colors);
            }
            return result;
        }


        // ── Pełna paleta — kolejność = AllowedColors w grze = color_map.txt ──────
        private static readonly Dictionary<string, string[]> AllColors =
            new Dictionary<string, string[]>
        {
    { "car_dnb_censor", new[] {
        "black","darkred","gray","white","darkgreen","cyan","lightblue","blue","purple","pink","red" } },
    { "car_katagiri_tamago", new[] {
        "black","white","beige","gold","darkgreen","gray","gray2","silver","teal","navy","blue","red","darkred" } },
    { "car_luxor_streamliner", new[] {
        "darkgray","beige","cream","offwhite","gray","teal","darkblue","lightblue","navy","nearblack","charcoal","silver","darkmaroon","maroon" } },
    { "car_mayen_m5", new[] {
        "black","white","green","darkgray","darkteal","silver","darkblue","navy","maroon","darkmaroon","red" } },
    { "car_salem_aries", new[] {
        "red","darkred","rust","gold","green","white","lightblue","lightblue2","silver","darkpurple" } },
        };





        // ── Aktywne w listingach — nazwy muszą być z AllColors ───────────────────
        private static readonly Dictionary<string, string[]> ActiveColors =
            new Dictionary<string, string[]>
        {
    { "car_dnb_censor",        new[] { "black", "white", "cyan", "gray" } },
    { "car_katagiri_tamago",   new[] { "white", "silver", "black", "red" } },
    { "car_luxor_streamliner", new[] { "silver", "cream", "navy", "gray" } },
    { "car_mayen_m5",          new[] { "black", "white", "darkblue", "silver" } },
    { "car_salem_aries",       new[] { "white", "red", "silver", "lightblue" } },
        };


        // ══════════════════════════════════════════════════════════════════════
        //  PULE OPISÓW — pogrupowane per archetyp
        //  Kilka jest "awaryjnych" — nadpisują losowanie gdy wystąpi konkretna flaga
        // ══════════════════════════════════════════════════════════════════════

        // ── Archetype A — Uczciwy ─────────────────────────────────────────────
        private static readonly string[] NotesHonest =
{
    
    "Selling because I'm upgrading. Condition as shown in photos, nothing to hide.",
    "Good runner, price is lower because it needs some work. I've described everything honestly.",
    "Garage kept, single owner. Minor faults accounted for in the price.",
    "No time to fix it myself — selling with known issues, hence the below-market price.",
    "Solid car, just need the cash. Condition matches the photos perfectly.",
    "Not going to pretend it's perfect. What you see is what you get.",
    "Priced honestly. I'd rather sell it cheap than waste your time with nonsense.",
    "Everything I know about this car is in the description. No surprises.",
    "Bought it as a daily, never got around to fixing the small stuff. Price reflects that.",
    "I'm a mechanic myself — I know what this car needs. Hence the fair price.",

    // ── Nowe ────────────────────────────────────────────────────────

    // Rzadka uczciowść
    "I could lie but I'm tired. It needs work. The price reflects this. That's the whole listing.",

    // Mechanik uczciwy
    "Just had it on the ramp. Here's what it needs: rear bushings, front pads, coolant flush. Price adjusted accordingly.",

    // Filosof
    "Every used car is a project. This one more than most. At least I'm telling you up front.",

    // Spokojny tata-sprzedawca
    "Bought it for my son. He didn't want it. I don't need it. You might. It runs, it stops, it turns. Priced fair.",

    // Bez BS
    "No 'one careful owner', no 'drives like new'. It's used. It shows. Price is honest.",

    // Czas na emeryturę
    "She's served me well for twelve years. Nothing left to give her. Time to let someone else love her back to health.",

    // Mechanik V2
    "I itemised every fault and priced the parts on the big site. Subtracted that from market value. Here we are.",

    // Zbyt uczciwy na swoje dobro
    "The car has three things wrong with it. I wrote all three in the title. Yes, all three. Read the title.",

    // Filozofia używanego auta
    "There is no such thing as a perfect used car. This one is honest about being imperfect. That's worth something.",

    // Sprzedaje z ciężkim sercem
    "Hate selling it. Good memories. But the gearbox isn't getting better on its own and I don't have the time.",

    // Suchy humor
    "Running, driving, stopping. Three for three. Anything beyond that is your problem now. Price is fair for what it is.",

    // Certyfikat uczciwości
    "I have described this car accurately because I am an adult and this is a transaction between adults. No drama.",

    // Emocje kontrolowane
    "Will not pretend there is no rust. There is rust. It is visible in photos 4, 7, and 9. Price accounts for rust.",

    // Tata mechanik
    "My father taught me: price it honest or don't sell it. Following his advice. Car has issues. Price shows that.",

    // Na walizkach
    "Moving country in three weeks. No time to fix it, no point shipping it. Honest price for a quick sale.",

    // Odpowiedzialny sprzedawca
    "Test drive before you buy. Bring your own mechanic if you want. I have nothing to hide and nowhere to be.",

    // Prosto z serca
    "It's not pretty. It runs fine. Priced for what it is, not what I wish it was.",

    // Dokumenty w porządku
    "All paperwork present. Service history has some gaps but the gaps are honest — I just forgot to stamp it.",

    // Naprawdę uczciwy
    "You will find worse cars at higher prices. You will find better cars at higher prices. This is the honest middle.",

    // Klasyczny uczciwy zakończenie
    "If I was keeping it I would fix those three things and drive it another ten years. I am not keeping it. You do the maths.",
};

        // Notki awaryjne dla Honest — aktywowane przez FaultFlags
        private static readonly Dictionary<FaultFlags, string> NotesHonestFault =
            new Dictionary<FaultFlags, string>
        {
            { FaultFlags.TimingBelt,
        "Full disclosure: timing belt needs replacement soon. Price adjusted accordingly." },
    { FaultFlags.HeadGasket,
        "Engine needs a rebuild — head gasket is gone. Selling as a project car, priced as such." },
    { FaultFlags.SuspensionWorn,
        "Suspension needs attention, slight knocking from the front over bumps. Price reflects this." },
    { FaultFlags.BrakesGone,
        "Pads and discs need immediate replacement — braking is not where it should be." },
    { FaultFlags.ElectricalFault,
        "Electrical gremlin somewhere, possibly alternator. Haven't chased it — price accounts for the risk." },
        };

        // ── Archetype B — Zaniedbany właściciel ──────────────────────────────
        private static readonly string[] NotesNeglected =
        {
    "Drives fine. Did the services myself when I remembered to.",
    "Solid car, knocks a bit when cold but settles after a minute or two.",
    "Selling because I have no time to look after it. Starts every time.",
    "Used it daily, no major drama. Might smoke a little on cold starts.",
    "Cheap to run, never asked for much over the years. Moving on to something newer.",
    "Drove it to work every day for years. It's a tool, not a show car.",
    "Nothing catastrophic ever went wrong with it. Small things here and there.",
    "Honestly can't remember the last full service but it never let me down.",
    "Ran it hard for a few years. Tyres are newer, rest is as-is.",
    "It's not pretty but it always got me where I needed to go.",
    "Previous owner kept receipts, I did not. Still runs well though.",
    "A few warning lights but the mechanic I asked said to just ignore them.",

    // ── Nowe ────────────────────────────────────────────────────────

    // Ran when parked klasyk
    "Ran when parked. Has not been started since 2021 but I'm sure it's fine.",

    // Olej z dyskontu
    "Ran it on supermarket own-brand oil for six years. Still runs. Probably fine.",

    // Check engine vibe
    "Service light has been on so long I think it's just the vibe now. Aesthetic.",

    // Skręt w lewo
    "Makes a noise when turning left. I just don't turn left anymore. Problem solved.",

    // MOT advisory
    "MOT man said 'advisory'. I chose not to look up what that word means.",

    // Okno
    "Been meaning to fix the window seal for two years. Great opportunity for someone more motivated than me.",

    // Sprzęgło
    "Clutch feels a bit chewy. You get used to it after about a week.",

    // Trzeci bieg
    "Third gear is more of a suggestion than a requirement. Second and fourth are perfect.",

    // Klima motywuje
    "Air con doesn't work but that's just motivation to drive faster. Free performance upgrade.",

    // Gary
    "Engine makes a ticking noise but my mate Gary says that's just character. I trust Gary.",

    // Poprzedni przegląd
    "Technically passed its last inspection. The one before that is none of your business.",

    // Zimny rozruch
    "Cold starts can be dramatic but once it warms up it forgets everything. Like me on a Monday.",

    // Olej
    "I top up the oil every few weeks. Might be burning it, might be leaking it. Either way it's topped up.",

    // Zderzak
    "Small cosmetic damage to the front bumper. The tree came out of nowhere. Fully its fault.",

    // Serwis ustny
    "Service history is verbal. I remember most of it.",

    // Cały czas jeździło
    "It's been my daily for four years. Never once left me stranded. The three times it left me stranded don't count.",

    // Telefon z grillem
    "The stereo sometimes changes station by itself. I've started to think of it as a feature. Radio Russian Roulette.",

    // Hamulce
    "Brakes work but they're more of an 'eventually' situation. Plan your stops in advance. Good life skill.",

    // Filtr
    "Air filter probably needs doing. Or maybe it doesn't. We are both finding out together.",

    // Serwis roczny
    "I service it every year whether it needs it or not. Last service was 2019.",

    // Dym
    "Bit of smoke on startup but it clears after thirty seconds. Perfect time to check your phone.",

    // Bateria
    "Battery is the original from the factory. I think that counts as a feature at this point.",

    // Rezonans
    "There is a vibration above 90 km/h. I just don't go above 90. Simple lifestyle adjustment.",

    // Szczerość przez przypadek
    "I was going to say it's in great condition but then I looked at it again. It's in fair condition. Maybe fair-to-good.",

    // Kierunek sprzedaży
    "Selling as-is. What 'as-is' means specifically I would prefer to discuss in person after you've already driven here.",
};

        // ── Archetype C — Handlarz ────────────────────────────────────────────
        private static readonly string[] NotesDealer =
       {
    // ── Oryginalne ──────────────────────────────────────────────────
    "Freshly serviced, everything 100% functional, ready for the road today.",
    "One owner from new, garage kept, drives like it just rolled off the line.",
    "Just finished a full inspection — everything checked and passed without issue.",
    "Professional detailing done. Looks and drives like new.",
    "Selling due to family expansion. Viewing will not disappoint, serious buyers only.",
    "Drives like it's on rails, handling is perfect. First to see will buy.",
    "Regularly maintained by specialists. Nothing to worry about here.",
    "Immaculate example. Price is firm because the car speaks for itself.",
    "Bought it, didn't end up needing it. Stored dry, runs perfectly.",
    "Full history, all stamps, nothing to hide. A genuinely clean example.",
    "Turn key ready. Not a project — everything works as it should.",
    "Private sale from a careful, experienced driver. Condition is exceptional.",

    // ── Nowe ────────────────────────────────────────────────────────

    // I know what I have
    "I know what I have. Price is firm. Timewasters and lowballers will be blocked.",

    // Staranna pani właściciel
    "One careful lady owner from new. She never checked the oil but she drove very gently.",

    // Historia serwisowa mokra
    "Full service history in a dedicated folder. Some pages are slightly water damaged but the intention is there.",

    // Mój rynek
    "Priced to reflect current market value. The market I'm referring to is my own independent research.",

    // Rysy
    "Not a scratch on the bodywork that I am prepared to acknowledge in writing.",

    // Nie projekt
    "This is not a project car. Do not buy this if you want a project. This is finished. Move on.",

    // Tylko poważni
    "Serious buyers only. If your opening message is 'is it still available' I will not respond.",

    // Detailing
    "Just had a full professional valet. Smells like new car. What was underneath is now sealed away.",

    // Zdjęcia kłamią
    "Condition is honestly better than the photos suggest. The photos were taken in poor lighting. In the rain.",

    // Cena mówi sama
    "Price reflects the quality. If it seems high, that means you haven't seen it in person yet.",

    // Wyjazd
    "Selling only because I'm upgrading. If I wasn't upgrading, this car would never leave my possession.",

    // Pieczątki
    "All stamps present and correct. I have not counted them personally but they are all there, I'm fairly sure.",

    // Mechanicznie idealne
    "Mechanically sound in every way that I was able to test without specialist equipment.",

    // Czas nagli
    "Price has been reduced for quick sale. Quick meaning within the next three weeks. No rush but do rush.",

    // Silnik
    "Engine runs beautifully. Checked it myself with my ears, which are experienced ears.",

    // Klasyk dealerski
    "This car will sell today. If not today, tomorrow. If not tomorrow, I will lower the price slightly. But today.",

    // Bardzo poważne ogłoszenie
    "I don't normally sell cars. That's why this one is in such exceptional condition. No trader miles, genuine sale.",

    // Odbiór osobisty
    "Cash on collection only. I don't do bank transfers for security reasons. My security, specifically.",

    // Wypolerowane
    "Freshly polished to a mirror shine. I would encourage you to focus on the shine during the viewing.",

    // Historycznie pewny
    "I can trace the full ownership history of this vehicle. Two previous owners: myself and the one before me, probably.",
};

        // ── Archetype D — Złomiarz ────────────────────────────────────────────
        private static readonly string[] NotesWrecker =
        {
    "Sir go to Farget and reedem the code please and i send you the car rocket fast. God bless.",
    "Buy, buy, I happy me happy. Car very good. Happy day :) Send money now.",
    "Hemu very good speed delivery through desert. Pay for gas and I ship car now. Very fast!",
    "My mother car, very little use. Like new. Please pay with fruit cards or Farget codes.",
    "Hemu shipping 24h. Very fast delivery from overseas. Best price for you friend.",
    "I am currently out of country but my brother ship car if you send code from Farget.",
    "Very good car. I cry when I sell. Please send gift card and I cry less.",
    "Car run like cheetah. I am honest man. My cousin verify. Send deposit, we talk.",
    "Sir this is not scam. I am engineer. The smoke is normal for this model. Buy now.",
    "Me and car very close friend. You buy, you also become close friend. PalPal only.",
    "I ship from overseas. You pay shipping + gas + small fee for my time. Very reasonable.",
    "First person to send Farget code get special price. Very limited offer. God is watching.",
    "Engine very quiet because it is calm. This is feature not problem. Trust the process.",
    "I not respond to lowball. I respond to Farget, Amaz, or Steem card. God bless you.",
    "My uncle was mechanic before the incident. He says car is fine. I trust him.",

    "Dear beloved buyer. I am Prince Adebayo, currently overseas. My late father left me this vehicle. Send small processing fee and car is yours. God of Abraham bless you.",
    "I am currently deployed with the military. Car is in safe storage. Send gift cards and my trusted agent delivers. I have medal. Very legitimate.",
    "Accepting Bitcoin, Ethereum, or Farget codes ONLY. No banks — banks are the real scam. This car will moon. Do your own research. WAGMI.",
    "WARNING: This is NOT a scam. I say this so you know. Scammers never say it is not a scam. Therefore I am not scammer. Simple logic. Please send code.",
    "My cousin Dmitri look at car and say everything perfect. Dmitri is not mechanic by profession but he has seen many cars. I trust Dmitri. You trust Dmitri.",
    "VERY URGENT SALE. I am relocating to another country next week. No time for test drive. Price already too low. First to transfer wins car. Act now.",
    "Car is currently held in customs. You only need to pay small release fee and car is shipped same day. Very easy.",
    "Hello dear. I saw you were looking for car. I think we have connection. Please buy car and maybe we talk more. I am real person. I am not cat.",
    "Several lights on dashboard but they make car look like spaceship cockpit at night. Very cool. Engine light means engine is working.",
    "I am certified mechanic from University of Car. I personally inspect this vehicle. Diploma is on wall. You cannot see wall but trust me.",
    "Elon Musk himself would buy this car. He has not, but he would. Probably. Price going up soon like Bitcoin. Buy now before I change mind.",
    "I am legitimate seller from good family. My father was king of regional car dealership. He has passed. The car is his legacy. Please send deposit to claim.",
    "God bless you for viewing this listing. God bless this car. God bless the engine. God bless the gearbox. God bless the tyres. Price is firm. God bless.",
    "I already sent the car before you paid. Please send payment now to confirm delivery. Car is on its way. This is how trust works. Very normal process.",
    "My other uncle was also mechanic before different incident. Both say car is fine. I have two uncle opinions. Very confident.",
    "I know price seems too good. That is because I am too good. I just want car to go to nice home. Send half now, half when car arrives.",
    "Some rust but rust is just car's skin doing extra work. Shows character. Like wrinkles but for metal. This is premium patina.",
    "The smoke from exhaust is white which means it is clean smoke. Black smoke bad, white smoke good. I am not chemist but this is my understanding.",
    "Car has full MOT until last year. I know what this means but choose not to explain. Price is price. God willing, she run.",

    // ── Nowe ────────────────────────────────────────────────────────

    // Klasyczny 419 z duszą
    "CONFIDENTIAL: I am contacting you privately regarding vehicle inheritance. Government trying to seize. Must sell quickly and quietly. Please be my trusted partner in this matter.",

    // Jeff Bezos energy
    "Jeff Bezos has similar car. His has more features but spiritually this is same car. You feel powerful driving this. Very CEO energy.",

    // Zaufany mechanik który jest nim
    "I inspect car myself this morning. I am not mechanic but I have YouTube. Watched three videos. Very thorough inspection. Everything pass.",

    // Ogłoszenie pisane przez bota
    "Excellent vehicle in perfect condition. Runs smoothly and efficiently. All features functional. Great investment opportunity. Contact now for more informations.",

    // Logika odwrócona
    "If car was bad I would not sell it. I would keep bad car. I am selling this car. Therefore car is good. This is logic. Please send deposit.",

    // Modlitwa przed zakupem
    "Before you buy please say short prayer for safe delivery. God rewards those who pray and also those who send Steem card. Both options available.",

    // Tłumaczenie Google
    "Is very good automobile with four wheel and one engine inside. Move forward also backward. Has chair for sit. Mirror for look. Price fair.",

    // Świadek
    "My neighbour has seen this car. He say it looks fine from window. He is retired and has good eyes. Third party verification complete.",

    // Nieświadomy ARG
    "DO NOT LOOK UNDER THE PASSENGER SEAT. Everything else is fine. Great car. Please do not ask about the passenger seat.",

    // Escort mission
    "Car located in village two hours from city. I cannot bring to you but my associate will meet you at highway rest stop at night. Very normal.",

    // Elon V2
    "This car was once parked near a Tesla. Some of the energy transferred. You can feel it in the throttle response. Very electric vibe. Price reflects this.",

    // Kosmiczne przeznaczenie
    "Stars aligned when this car was manufactured. Astrologist friend confirm: this vehicle destined for great owner. Are you the great owner? Send deposit to find out.",

    // Auto blockchain
    "This car is now also NFT. You are buying both physical car and digital soul of car on blockchain. Very exclusive. Only one exist. Farget code for discount.",

    // Support hotline
    "If any problem with car after purchase please call my cousin. He will not answer but the intention is there. Five star seller.",

    // Záhadná pojízdka
    "Car sometimes starts by itself at 3am. I think it just misses me. This is not electrical fault, this is emotional bond. Very rare feature.",

    // Delivery company
    "I use my own delivery company: Hemu Express. Very fast. Five star rating on my personal website which I made yesterday. Very established company.",

    // Warranty void
    "Car comes with full warranty. Warranty provided by me personally. Does not cover engine, gearbox, bodywork, or other parts. Covers good vibes only.",

    // Deepest lore
    "Previous owner was scientist. He drove only to work and back. His work was four hundred kilometres away. He drove every day. But very carefully.",

    // Filozofia scammera
    "In this world there are givers and takers. I am giver. I give you this car for small price. You give me Farget code. This is balance. This is harmony.",

    // Masterpiece zakończenia
    "I will not be reachable after purchase. Not because scam. I am just very busy man. Extremely busy. Please do not try to reach me. God bless and good luck.",
};

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVE LISTINGS
        // ══════════════════════════════════════════════════════════════════════

        public List<CarListing> ActiveListings { get; private set; } = new();

        private float _gameTime = 0f;
        public float GameTime => _gameTime;

        public void Tick(float deltaTime)
        {
            _gameTime += deltaTime;
            ActiveListings.RemoveAll(l => l.ExpiresAt <= _gameTime);

            while (ActiveListings.Count < 4)
                ActiveListings.Add(GenerateListing());

            if (ActiveListings.Count < 10 && UnityEngine.Random.value < 0.002f)
                ActiveListings.Add(GenerateListing());
        }

        // ══════════════════════════════════════════════════════════════════════
        //  GENEROWANIE LISTINGU
        // ══════════════════════════════════════════════════════════════════════

        private CarListing GenerateListing()
        {
            var rng = new Random();
            var def = CarDefs[rng.Next(CarDefs.Length)];
            var ttl = UnityEngine.Random.Range(120f, 600f);
            int year = rng.Next(def.MinYear, def.MaxYear + 1);

            // Kolor — losowany z aktywnej puli dla danego auta
            string[] colorPool = ActiveColors.TryGetValue(def.InternalId.Split('_')[0] + "_" +
                string.Join("_", def.InternalId.Split('_').Skip(1).Take(2)), out var cp)
                ? cp : new[] { "white" };

            // prostsze podejście — wyciągamy base ID bez numeru losowego
            string baseId = def.InternalId; // np. "car_mayen_m5"
            string[] pool = ActiveColors.ContainsKey(baseId) ? ActiveColors[baseId] : new[] { "white" };
            string color = pool[rng.Next(pool.Length)];
            



            // 1. Archetyp sprzedawcy
            var archetype = RollArchetype(rng);

            int rating = RollSellerRating(archetype, rng);

            // 2. ApparentCondition — co gracz widzi (dyktuje cenę)
            //    Dealer wystawia auto wypolerowane (wysoka pozorna kondycja)
            //    Wrecker może wystawić auto w dowolnym stanie z dobrym opisem
            float apparent = archetype switch
            {
                SellerArchetype.Dealer => Mathf.Clamp((float)BetaSample(rng, 2.5, 1.5), 0.45f, 0.95f),
                SellerArchetype.Wrecker => Mathf.Clamp((float)BetaSample(rng, 1.5, 2.5), 0.15f, 0.75f),
                _ => Mathf.Clamp((float)BetaSample(rng, 1.8, 3.5), 0.08f, 0.95f),
            };

            // 3. ActualCondition — rzeczywisty stan mechaniki
            float honesty = RollHonesty(archetype, rng);
            float noise = (float)(rng.NextDouble() * 0.06 - 0.03);
            float actual = Mathf.Clamp(apparent * honesty + noise, 0.02f, 0.97f);

            // 4. Ukryte usterki — losowane z puli właściwej dla archetypu i actual
            FaultFlags faults = RollFaults(archetype, actual, rng);

            // 5. Cena — bazuje na ApparentCondition
            float basePricef = Mathf.Lerp(def.MinPrice, def.MaxPrice, apparent);
            float priceNoise = 1f + (float)(rng.NextDouble() * 0.16 - 0.08);
            int price = Mathf.RoundToInt(basePricef * priceNoise * OXLSettings.PriceMultiplier / 50f) * 50;

            // Clamp PRZED rabatem — trudność wyznacza widełki, rabat może zejść poniżej i to OK
            int scaledMin = Mathf.RoundToInt(def.MinPrice * OXLSettings.PriceMultiplier);
            int scaledMax = Mathf.RoundToInt(def.MaxPrice * OXLSettings.PriceMultiplier);
            price = Mathf.Clamp(price, scaledMin, scaledMax);

            // Uczciwy sprzedawca z usterkami daje rabat — celowo może zejść poniżej scaledMin
            if (archetype == SellerArchetype.Honest && faults != FaultFlags.None)
                price = Mathf.RoundToInt(price * 0.78f / 50f) * 50;

            // 6. Przebieg — z ActualCondition (bardziej prawdziwy)
            int mileage = Mathf.RoundToInt(
                Mathf.Lerp(180000, 4000, actual) * (1f + (float)(rng.NextDouble() * 0.30 - 0.15)));
            mileage = Mathf.Max(500, mileage);

            // 7. Nota sprzedawcy
            string note = SelectNote(archetype, faults, rng);

            return new CarListing
            {
                Registration = GenReg(rng),
                Make = def.Make,
                Model = def.Model,
                ImageFolder = def.ImageFolder,
                Year = year,
                Price = price,
                ApparentCondition = apparent,
                ActualCondition = actual,
                Archetype = archetype,
                Faults = faults,
                SellerNote = note,
                ExpiresAt = _gameTime + ttl,
                InternalId = def.InternalId + "_" + rng.Next(1000, 9999),
                Mileage = mileage,
                Location = Locations[rng.Next(Locations.Length)],
                DeliveryHours = rng.Next(1, 37),
                SellerRating = rating,
                Color = color,
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ARCHETYP — rozkład i uczciwość
        // ══════════════════════════════════════════════════════════════════════

        private static SellerArchetype RollArchetype(Random rng)
        {
            double r = rng.NextDouble();
            if (r < 0.20) return SellerArchetype.Honest;    // 20%
            if (r < 0.55) return SellerArchetype.Neglected; // 35%
            if (r < 0.85) return SellerArchetype.Dealer;    // 30%
            return SellerArchetype.Wrecker;                  // 15%
        }

        private static float RollHonesty(SellerArchetype arch, Random rng)
        {
            return arch switch
            {
                SellerArchetype.Honest => (float)(0.90 + rng.NextDouble() * 0.10), // 0.90–1.00
                SellerArchetype.Neglected => (float)(0.78 + rng.NextDouble() * 0.17), // 0.78–0.95
                SellerArchetype.Dealer => (float)(0.42 + rng.NextDouble() * 0.33), // 0.42–0.75
                SellerArchetype.Wrecker => (float)(0.07 + rng.NextDouble() * 0.28), // 0.07–0.35
                _ => 1.0f
            };
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FAULT ROLLING
        // ══════════════════════════════════════════════════════════════════════

        private static FaultFlags RollFaults(SellerArchetype arch, float actual, Random rng)
        {
            var f = FaultFlags.None;

            // Pasek rozrządu — pułapka nr 1, najważniejsza
            double timingChance = arch switch
            {
                SellerArchetype.Wrecker => 0.55,
                SellerArchetype.Neglected => 0.35,
                SellerArchetype.Dealer => 0.12,
                _ => 0.04, // Honest rzadko zatai
            };
            if (rng.NextDouble() < timingChance) f |= FaultFlags.TimingBelt;

            // Uszczelka głowicy — tylko zły stan + wrecker
            if (actual < 0.30 && arch == SellerArchetype.Wrecker && rng.NextDouble() < 0.30)
                f |= FaultFlags.HeadGasket;

            // Zawieszenie — dealer ukrywa, neglected ignoruje
            double suspChance = arch switch
            {
                SellerArchetype.Dealer => 0.62,
                SellerArchetype.Wrecker => 0.50,
                SellerArchetype.Neglected => 0.38,
                _ => 0.12,
            };
            if (rng.NextDouble() < suspChance) f |= FaultFlags.SuspensionWorn;

            // Hamulce — powszechne u neglected/wrecker
            double brakeChance = arch switch
            {
                SellerArchetype.Honest => 0.18,
                SellerArchetype.Neglected => 0.48,
                SellerArchetype.Dealer => 0.30, // zatuszowane
                SellerArchetype.Wrecker => 0.55,
                _ => 0.20
            };
            if (rng.NextDouble() < brakeChance) f |= FaultFlags.BrakesGone;

            // Tłumik — korozja, zależna od actual
            if (actual < 0.50 && rng.NextDouble() < 0.45) f |= FaultFlags.ExhaustRusted;

            // Elektryka — losowe, każdy archetyp
            if (rng.NextDouble() < 0.14) f |= FaultFlags.ElectricalFault;

            // Szyby / reflektory — zależne od actual
            double glassChance = actual < 0.35 ? 0.35 : 0.07;
            if (rng.NextDouble() < glassChance) f |= FaultFlags.GlassDamage;

            return f;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SELEKCJA OPISU
        // ══════════════════════════════════════════════════════════════════════

        private static string SelectNote(SellerArchetype arch, FaultFlags faults, Random rng)
        {
            // Uczciwy sprzedawca z krytyczną usterką — nota musi to odzwierciedlać
            if (arch == SellerArchetype.Honest)
            {
                // Priorytet: HeadGasket > TimingBelt > Suspension > Brakes > Electrical
                foreach (var flag in new[] {
                    FaultFlags.HeadGasket,
                    FaultFlags.TimingBelt,
                    FaultFlags.SuspensionWorn,
                    FaultFlags.BrakesGone,
                    FaultFlags.ElectricalFault })
                {
                    if (faults.HasFlag(flag) && NotesHonestFault.TryGetValue(flag, out var specific))
                        return specific;
                }
            }

            // Dla pozostałych — losuj z puli archetypu
            string[] pool = arch switch
            {
                SellerArchetype.Honest => NotesHonest,
                SellerArchetype.Neglected => NotesNeglected,
                SellerArchetype.Dealer => NotesDealer,
                SellerArchetype.Wrecker => NotesWrecker,
                _ => NotesNeglected,
            };

            return pool[rng.Next(pool.Length)];
        }

        // ══════════════════════════════════════════════════════════════════════
        //  PURCHASE
        // ══════════════════════════════════════════════════════════════════════

        public bool TryPurchase(CarListing listing, Action<string> spawnCar, Action<int> deductMoney)
        {
            if (!ActiveListings.Contains(listing)) return false;
            spawnCar(listing.InternalId);
            deductMoney(listing.Price);
            ActiveListings.Remove(listing);
            OXLPlugin.Log.Msg(
                $"[OXL] Purchased: {listing.Make} {listing.Model} " +
                $"| Arch={listing.Archetype} " +
                $"| Apparent={listing.ApparentCondition:P0} Actual={listing.ActualCondition:P0} " +
                $"| Faults={listing.Faults}");
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════


        // ── Helper: gwiazdki sprzedawcy ──────────────────────────────────────────
        private static string FormatStars(int rating)
        {
            // Wypełnione ★ + puste ☆ żeby zawsze było 5 znaków — czytelna skala
            return new string('★', rating) + new string('☆', 5 - rating);
        }

        private static Color StarColor(int rating) => rating switch
        {
            5 => new Color(0.22f, 0.75f, 0.40f, 1f), // zielony  — godny zaufania
            4 => new Color(0.55f, 0.80f, 0.30f, 1f), // żółtozielony
            3 => new Color(0.85f, 0.72f, 0.20f, 1f), // żółty    — uważaj
            2 => new Color(0.90f, 0.45f, 0.15f, 1f), // pomarańcz
            _ => new Color(0.90f, 0.20f, 0.20f, 1f), // czerwony — uciekaj
        };



        private static int RollSellerRating(SellerArchetype arch, Random rng)
        {
            // Losujemy z przedziału właściwego dla archetypu
            // Dealer celowo może dostać wysoką ocenę — to jest pułapka
            return arch switch
            {
                SellerArchetype.Honest => rng.Next(0, 10) < 8 ? 5 : 4,        // 80% = 5★, 20% = 4★
                SellerArchetype.Neglected => rng.Next(0, 10) < 6 ? 4 : 3,        // 60% = 4★, 40% = 3★
                SellerArchetype.Dealer => rng.Next(0, 10) switch               // pułapka
                {
                    < 3 => 5,   // 30% = 5★ — bardzo przekonujący
                    < 7 => 4,   // 40% = 4★
                    _ => 3    // 30% = 3★
                },
                SellerArchetype.Wrecker => rng.Next(0, 10) < 7 ? 1 : 2,        // 70% = 1★, 30% = 2★
                _ => 3
            };
        }
        private static double BetaSample(Random rng, double alpha, double beta)
        {
            double x = GammaSample(rng, alpha);
            double y = GammaSample(rng, beta);
            return x / (x + y);
        }

        private static double GammaSample(Random rng, double shape)
        {
            if (shape < 1.0)
                return GammaSample(rng, shape + 1.0) * Math.Pow(rng.NextDouble(), 1.0 / shape);
            double d = shape - 1.0 / 3.0, c = 1.0 / Math.Sqrt(9.0 * d);
            while (true)
            {
                double x, v;
                do { x = NextGaussian(rng); v = 1.0 + c * x; } while (v <= 0);
                v = v * v * v;
                double u = rng.NextDouble();
                if (u < 1.0 - 0.0331 * (x * x) * (x * x)) return d * v;
                if (Math.Log(u) < 0.5 * x * x + d * (1.0 - v + Math.Log(v))) return d * v;
            }
        }

        private static double NextGaussian(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private static string GenReg(Random rng)
        {
            const string L = "ABCDEFGHJKLMNPRSTVWXYZ";
            return $"{L[rng.Next(L.Length)]}{L[rng.Next(L.Length)]}" +
                   $"{rng.Next(100, 999)}" +
                   $"{L[rng.Next(L.Length)]}{rng.Next(10, 99)}";
        }

        private static readonly string[] Locations =
        {
            "Ashford Creek", "Dunmore Hill", "Crestwick", "Barlow Falls",
            "Tyndall Cross", "Greystone Bay", "Portwick", "Aldenmoor",
            "Fenwick Hollow", "Clarendon Rise", "Saltbury", "Wexmoor",
            "Hadleigh Point", "Thorngate", "Ivybridge End", "Coldwater Bluff",
            "Elmshire", "Brackenford", "Southmere", "Galloway Reach"
        };
    }
}