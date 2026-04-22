// ════════════════════════════════════════════════════════════════════════════
//  SellerNoteBuilder.cs
//  Generuje opisy ogłoszeń sprzedawców samochodów w CMS2026_OXL.
//
//  ARCHITEKTURA:
//  ┌─ BuildNote()          — punkt wejścia, dispatch na archetype
//  ├─ #region HELPERS      — Pick, MaybePick, Fill, Join + paleta kolorów
//  ├─ #region FAULT_LINES  — DominantFaultLine() per usterka × archetype × level
//  ├─ #region BUILD_HONEST
//  ├─ #region BUILD_WRECKER
//  ├─ #region BUILD_DEALER
//  └─ #region BUILD_SCAMMER
//
//  PALETA KOLORÓW (Unity Rich Text):
//  #ff9944  — usterka / problem / ostrzeżenie
//  #99ff99  — pozytywny stan (Honest, Wrecker)
//  #ffdd88  — "premium" / luksus (Dealer L2/L3)
//  #aaffcc  — fałszywa uczciwość (Scammer L3)
//  #cccccc  — komentarz/dygresja właściciela (zastąpił #aaaaaa)
//  #dddddd  — suchy techniczny (Dealer L3 opisy)
//  #ffcc00  — wykrzykniki Scammer L1/L2
//  #ff4444  — krzyk / niebezpieczeństwo (Scammer L1)
//  #00ff00  — fałszywa pewność (Scammer L1/L2)
// ════════════════════════════════════════════════════════════════════════════

using System;
using System.Text;

namespace CMS2026_OXL
{
    public static class SellerNoteBuilder
    {
        // ════════════════════════════════════════════════════════════════════
        //  ENTRY POINT
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Główny punkt wejścia. Dispatch na odpowiedni builder na podstawie Archetype.
        /// </summary>
        public static string BuildNote(CarListing l, Random rng)
        {
            return l.Archetype switch
            {
                SellerArchetype.Honest => BuildHonest(l, rng),
                SellerArchetype.Wrecker => BuildWrecker(l, rng),
                SellerArchetype.Dealer => BuildDealer(l, rng),
                SellerArchetype.Scammer => BuildScammer(l, rng),
                _ => BuildHonest(l, rng),
            };
        }

        // ════════════════════════════════════════════════════════════════════
        #region HELPERS
        // ════════════════════════════════════════════════════════════════════

        // ── Paleta kolorów — stałe do użytku w stringach ─────────────────
        //    Używaj tych stałych zamiast hardkodować hex w każdym stringu.
        //    Dzięki temu zmiana koloru globalnie = jedna edycja tutaj.

        // Usterki i problemy
        private const string C_FAULT = "#ff9944";   // pomarańczowy — wada mechaniczna
        private const string C_DANGER = "#ff4444";   // czerwony — niebezpieczeństwo/krzyk

        // Stan pozytywny
        private const string C_GOOD = "#99ff99";   // zielony — dobry stan (Honest/Wrecker)
        private const string C_PREMIUM = "#ffdd88";   // złoty — "premium" (Dealer)
        private const string C_FAKE_OK = "#0f6532";   // ciemnozielony — fałszywa uczciwość (Scammer L3)

        // Komentarze i tony
        private const string C_ASIDE = "#cccccc";   // jasnoszary — dygresja/komentarz właściciela
        private const string C_TECH = "#dddddd";   // chłodny szary — suchy techniczny (Dealer L3)
        private const string C_SCREAM = "#ffcc00";   // żółty — wykrzykniki Scammer

        // Scammer specjalne
        private const string C_SCAM_GRN = "#00ff00";   // neonowy zielony — fałszywa pewność
        private const string C_SCAM_CYN = "#00ffff";   // cyan — absurdalne twierdzenia
        private const string C_SCAM_PRP = "#9b80c8";   // fiolet — mistycyzm/bajki
        private const string C_SCAM_PNK = "#ff00ff";   // różowy — niedorzeczność

        // ── Podstawowe narzędzia ─────────────────────────────────────────

        /// <summary>Losuje jeden element z tablicy.</summary>
        private static string Pick(Random rng, string[] pool) =>
            pool[rng.Next(pool.Length)];

        /// <summary>
        /// Z szansą <paramref name="chance"/> zwraca losowy element z tablicy,
        /// w przeciwnym razie null (element pominięty przez Join).
        /// </summary>
        private static string MaybePick(Random rng, string[] pool, double chance)
        {
            if (rng.NextDouble() > chance) return null;
            return pool[rng.Next(pool.Length)];
        }

        /// <summary>
        /// Zastępuje tagi {make}, {model}, {year}, {mileage}, {price},
        /// {location}, {rating} wartościami z listingu.
        /// </summary>
        private static string Fill(string template, CarListing l) =>
            template
                .Replace("{make}", l.Make)
                .Replace("{model}", l.Model)
                .Replace("{year}", l.Year.ToString())
                .Replace("{mileage}", $"{l.Mileage:N0}")
                .Replace("{price}", $"${l.Price:N0}")
                .Replace("{location}", l.Location)
                .Replace("{rating}", l.SellerRating.ToString());

        /// <summary>
        /// Łączy niepuste fragmenty w 1–3 akapity rozdzielone \n\n.
        /// Automatycznie dodaje kropkę jeśli zdanie jej nie ma.
        /// </summary>
        private static string Join(params string[] parts)
        {
            var sentences = new System.Collections.Generic.List<string>();
            foreach (var p in parts)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                string trimmed = p.TrimEnd();
                char last = trimmed[trimmed.Length - 1];
                if (last != '.' && last != '!' && last != '?')
                    trimmed += '.';
                sentences.Add(trimmed);
            }

            if (sentences.Count == 0) return "";

            var sb = new StringBuilder();
            int total = sentences.Count;

            if (total <= 2)
            {
                // Krótki tekst — jeden akapit
                sb.Append(string.Join(" ", sentences));
            }
            else if (total <= 4)
            {
                // Dwa akapity: pierwsze 2 / reszta
                sb.Append(string.Join(" ", sentences.GetRange(0, 2)));
                sb.Append("\n\n");
                sb.Append(string.Join(" ", sentences.GetRange(2, total - 2)));
            }
            else
            {
                // Trzy akapity: pierwsze 2 / środek / ostatnie
                int mid = total / 2;
                sb.Append(string.Join(" ", sentences.GetRange(0, 2)));
                sb.Append("\n\n");
                sb.Append(string.Join(" ", sentences.GetRange(2, mid - 2)));
                sb.Append("\n\n");
                sb.Append(string.Join(" ", sentences.GetRange(mid, total - mid)));
            }

            return sb.ToString();
        }

        // ── Pomocnicze predykaty kondycji ────────────────────────────────

        /// <summary>Zły stan — poniżej 30% actual condition.</summary>
        private static bool IsBad(CarListing l) => l.ActualCondition < 0.30f;

        /// <summary>Średni stan — 30–65% actual condition.</summary>
        private static bool IsMid(CarListing l) => l.ActualCondition >= 0.30f && l.ActualCondition < 0.65f;

        /// <summary>Dobry stan — powyżej 65% actual condition.</summary>
        private static bool IsGood(CarListing l) => l.ActualCondition >= 0.65f;

        #endregion // HELPERS

        // ════════════════════════════════════════════════════════════════════
        #region FAULT_LINES
        // ════════════════════════════════════════════════════════════════════
        //  DominantFaultLine — generuje jedno zdanie o dominującej usterce.
        //
        //  LOGIKA WYWOŁANIA:
        //  • Sprawdza FaultFlags w kolejności priorytetów (HeadGasket > TimingBelt > ...)
        //  • Każda usterka × archetype × level ma osobną pulę wypowiedzi
        //  • Dealer zawsze zwraca null (nie mówi o usterkach)
        //  • Scammer L3 stosuje "pre-emptive dismissal" — mówi o symptomach
        //    jako o czymś normalnym, zamiast kłamać wprost jak L1/L2
        //
        //  PALETA W FAULT_LINES:
        //  Honest/Wrecker — C_FAULT (#ff9944) na symptomy, C_ASIDE (#cccccc) na komentarz
        //  Scammer L1     — C_GOOD (#99ff99) — udaje ideał
        //  Scammer L2     — C_GOOD (#99ff99) — kłamstwo ale bardziej wiarygodne
        //  Scammer L3     — C_ASIDE (#cccccc) — "normalne dla wieku", bez alarmów
        // ════════════════════════════════════════════════════════════════════

        private static string DominantFaultLine(
            CarListing l, SellerArchetype arch, int level, Random _rng)
        {
            // ── HEAD GASKET ───────────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.HeadGasket))
                return arch switch
                {
                    // Honest — ujawnia wprost, język zależy od poziomu wiedzy
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"A mechanic had a look at the <b>{l.Model}</b> and mentioned something about the <color={C_FAULT}>head gasket</color> — <color={C_ASIDE}>I did not fully understand what that meant but it sounded expensive.</color>",
                            $"There is <color={C_FAULT}>white smoke</color> coming from the back of the <b>{l.Make}</b> when it starts up. <color={C_ASIDE}>My neighbour said it could be the head gasket but I honestly have no idea.</color>",
                            $"The <b>{l.Model}</b> has been <color={C_FAULT}>losing coolant</color> and I cannot figure out where it is going. <color={C_ASIDE}>Someone at the garage said it might be internal — whatever that means.</color>",
                            $"I noticed the oil in the <b>{l.Make}</b> looked <color={C_FAULT}>a bit milky</color> the last time I checked it. <color={C_ASIDE}>I googled it and the results were not encouraging, hence the price.</color>",
                            $"There is <color={C_FAULT}>a sweet smell</color> from the engine bay of the <b>{l.Model}</b> that I cannot explain. <color={C_ASIDE}>A friend said it might be coolant leaking somewhere it should not be.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>runs hot sometimes</color> and I do not know why. <color={C_ASIDE}>Had it looked at briefly and the mechanic said something about the gasket — I nodded like I understood.</color>",
                            $"Someone told me the <b>{l.Model}</b> might have a <color={C_FAULT}>head gasket problem</color>. <color={C_ASIDE}>I do not know enough about engines to confirm or deny that, which is partly why I am selling it.</color>",
                            $"The <b>{l.Make}</b> <color={C_FAULT}>steams a bit from under the bonnet</color> on cold mornings. <color={C_ASIDE}>It clears after a few minutes but I was told that is not a good sign.</color>",
                            $"Oil and coolant levels on the <b>{l.Model}</b> keep dropping and I <color={C_ASIDE}>cannot find any obvious leaks outside the engine. Priced low because I suspect it needs serious work.</color>",
                            $"A bloke at work had a listen to the <b>{l.Year}</b> <b>{l.Make}</b> and said <color={C_FAULT}>'that sounds like a gasket job'</color>. <color={C_ASIDE}>He works in IT so take that with a pinch of salt, but the price reflects the uncertainty.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"The <color={C_FAULT}>head gasket has definitely failed</color>. Coolant and oil are mixing. <color={C_ASIDE}>It needs a top-end rebuild, so you will need a trailer to take it away.</color>",
                            $"It is <color={C_FAULT}>overheating and pressurising the coolant system</color>. <color={C_ASIDE}>Classic head gasket symptoms for a <b>{l.Make}</b>. Priced accordingly for someone who can do the work themselves.</color>",
                            $"There is <color={C_FAULT}>mayo under the oil cap</color> and it is losing coolant. <color={C_ASIDE}>I have diagnosed it as the head gasket, so do not plan on driving it home.</color>",
                            $"Engine runs, but the <color={C_FAULT}>head gasket is blown</color>. <color={C_ASIDE}>I know what these cost to fix at a garage, which is why the asking price is so low.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Classic {l.Make} trait at this mileage. <color={C_FAULT}>Head gasket has breached</color> between cylinders. <color={C_ASIDE}>Don't start it, tow it. I've priced it exactly £1000 below market to cover the machining and MLS gasket set.</color>",
                            $"Failed <color={C_FAULT}>head gasket</color>. <color={C_ASIDE}>I caught it early so the block isn't warped, but it needs a skim and a new gasket. If you know these {l.Model}s, you know it's a weekend job with an engine hoist.</color>",
                            $"It's <color={C_FAULT}>pressurising the coolant</color>. <color={C_ASIDE}>I've done a block sniffer test and confirmed exhaust gases in the expansion tank. Engine needs a top-end rebuild. No offers, the price already reflects the work needed.</color>",
                            $"The <color={C_FAULT}>head gasket is gone</color>. <color={C_ASIDE}>I've owned three of these and they all do it eventually. I don't have the garage space to rebuild this one myself, so my loss is your gain.</color>",
                        })
                        : "Head gasket has failed — coolant is mixing with oil and the engine needs proper work before it goes anywhere.",

                    // Wrecker — nie kłamie, ale minimalizuje lub ignoruje symptomy
                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"There is <color={C_FAULT}>white smoke</color> sometimes when I start the <b>{l.Make}</b>. <color={C_ASIDE}>Goes away after a few minutes. Probably just condensation or something.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>drinks a bit of coolant</color>. <color={C_ASIDE}>I just keep a bottle in the boot and top it up when the light comes on. Never actually broken down.</color>",
                            $"Oil on the <b>{l.Make}</b> looked <color={C_FAULT}>a bit creamy</color> last time I checked. <color={C_ASIDE}>I assumed it just needed a change. Never got around to doing it.</color>",
                            $"There is <color={C_FAULT}>a bit of steam</color> from under the bonnet of the <b>{l.Model}</b> when it warms up. <color={C_ASIDE}>Does it every morning, then stops. I honestly stopped noticing.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>runs a bit rough when cold</color>. <color={C_ASIDE}>Once it warms up it is fine though. Always assumed it was just being old.</color>",
                            $"Someone mentioned something about the <color={C_FAULT}>head gasket</color> on the <b>{l.Model}</b> when they looked at it. <color={C_ASIDE}>I did not really follow what they were saying. They did not seem that worried.</color>",
                            $"The <b>{l.Make}</b> <color={C_FAULT}>smells a bit sweet</color> from the engine sometimes. <color={C_ASIDE}>Not always. I thought it was just the heater doing something weird.</color>",
                            $"Coolant level on the <b>{l.Model}</b> drops <color={C_FAULT}>a little between top-ups</color>. <color={C_ASIDE}>Cannot see where it is going. Probably just evaporating or whatever coolant does.</color>",
                            $"There is <color={C_FAULT}>a faint mist</color> from the exhaust on the <b>{l.Year}</b> <b>{l.Make}</b> when I start it up. <color={C_ASIDE}>My last car did that too and it was fine for years.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>uses a bit of oil</color> between services. <color={C_ASIDE}>I just check it now and then. These old engines all do it a bit, I think.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Make}</b> has a <color={C_FAULT}>known head gasket issue</color> on these engines. <color={C_ASIDE}>I always meant to sort it before selling. Never happened. Price reflects that.</color>",
                            $"Coolant and oil are <color={C_FAULT}>mixing on the <b>{l.Model}</b></color>. <color={C_ASIDE}>It has been sitting since I noticed. Did not want to make it worse by driving it.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>the classic white smoke on startup</color>. <color={C_ASIDE}>I know what that means on these engines. I just never had the time or money to deal with it properly.</color>",
                            $"Head gasket has gone on the <b>{l.Model}</b>. <color={C_ASIDE}>I diagnosed it myself — <color={C_FAULT}>milky oil, rising coolant temp, the full set</color>. Engine runs but I would not push it.</color>",
                            $"This <b>{l.Make}</b> has been <color={C_FAULT}>sitting in the garage since the gasket went</color>. <color={C_ASIDE}>That was about eighteen months ago. Everything else on it is fine as far as I know.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> needs a <color={C_FAULT}>head gasket doing</color>. <color={C_ASIDE}>I know the job, just do not have the ramp space anymore. Selling it as the project it is.</color>",
                            $"<color={C_FAULT}>Coolant is disappearing internally</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>No external leaks, so it is going somewhere it should not. Classic gasket symptom on this engine family.</color>",
                            $"The <b>{l.Model}</b> has <color={C_FAULT}>been off the road since the head gasket issue started</color>. <color={C_ASIDE}>I bought it knowing it might need one and then life got in the way.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Head gasket has failed on the <b>{l.Make}</b>. <color={C_ASIDE}>Confirmed with a block sniffer test — <color={C_FAULT}>exhaust gases in the coolant</color>. Block is not warped, caught it before it got that far. Engine needs a top-end rebuild, nothing more exotic than that.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> has a <color={C_FAULT}>blown head gasket</color>. <color={C_ASIDE}>Classic symptom pattern on this engine family at this mileage. I have seen enough of these to know exactly what it needs. Priced with the machining and MLS gasket set in mind.</color>",
                            $"<color={C_FAULT}>Gasket has breached between cylinders</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>Compression leak rather than coolant mixing at this stage. Do not start it again — tow it in and do it properly. I have discounted the asking price by the cost of a full top-end job at a decent independent.</color>",
                            $"The <b>{l.Model}</b> needs a <color={C_FAULT}>head gasket</color>. <color={C_ASIDE}>I am not dressing it up. These <b>{l.Make}</b>s do it at this age and mileage — it is a known weakness, not a mystery. The block is fine, the head will skim clean. Priced for what it is.</color>",
                            $"<color={C_FAULT}>Head gasket failure</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Coolant consumption without external leaks, slight pressurisation of the expansion tank. Textbook. I have owned four of these. They all do it eventually. This one's turn came and I moved it on rather than rebuild it again.</color>",
                            $"The <b>{l.Make}</b> has a <color={C_FAULT}>failed head gasket</color>. <color={C_ASIDE}>I know this engine well enough to say the block is salvageable. Do not drive it — flat-bed only. The price already accounts for a full gasket kit and a head skim at a machine shop.</color>",
                            $"<color={C_FAULT}>Head gasket is gone</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>I have been dealing in these long enough to price it exactly right for what it needs. Not a penny more, not a penny less. Someone who knows their way around a cylinder head will do well here.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color={C_FAULT}>a breached gasket</color>. <color={C_ASIDE}>I stripped the cam cover to confirm — no warping, no scoring. It needs the gasket, a skim, and fresh head bolts. Everything else on this engine is solid. Priced accordingly, no room to move.</color>",
                        })
                        : "There is white smoke on startup sometimes, usually clears after a minute or two.",

                    // Dealer — nigdy nie mówi o usterkach
                    SellerArchetype.Dealer => null,

                    // Scammer — L1 kłamie wprost, L2 wiarygodnie, L3 dismissuje objawy
                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"Engine runs <color={C_GOOD}>perfectly</color>, starts first time every time, <color={C_GOOD}>no smoke, no leaks</color>.",
                            $"Just had the engine <color={C_GOOD}>fully inspected</color> last week — mechanic said it was in <color={C_GOOD}>great shape</color>.",
                            $"<color={C_GOOD}>Zero issues</color> with the engine on this <b>{l.Make}</b>. <color={C_GOOD}>Starts like a dream</color> every single time.",
                            $"Engine on the <b>{l.Model}</b> is <color={C_GOOD}>strong and clean</color>. <color={C_ASIDE}>Oil is crystal clear, coolant full, no problems at all.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Engine was <color={C_GOOD}>fully rebuilt</color> before I bought it. Previous owner has receipts — I can forward them after payment.",
                            $"Had the cooling system <color={C_GOOD}>professionally flushed</color> recently. No issues since.",
                            $"<color={C_GOOD}>Head gasket was replaced</color> as part of a major service. All sorted, nothing to worry about.",
                            $"Mechanic checked the engine before I listed it. <color={C_GOOD}>Said it was fine.</color> I have his number if you want to call.",
                            $"These <b>{l.Make}</b>s do get a reputation for cooling issues but <color={C_GOOD}>this one has never given me any trouble</color>.",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            // Scammer L3 — pre-emptive dismissal, C_ASIDE bo "brzmi" normalnie
                            $"These <b>{l.Make}</b> engines can <color={C_ASIDE}>run a little warm on cold starts</color> — completely normal for the age and something I have never had any concern about.",
                            $"There is <color={C_ASIDE}>a small amount of condensation from the exhaust</color> on cold mornings. Every older <b>{l.Model}</b> does this. Clears within a minute.",
                            $"Oil was slightly discoloured at the last change — <color={C_ASIDE}>mechanic said it was nothing to worry about</color>, probably just old fluid mixing. Changed it and it has been fine since.",
                            $"Coolant level has <color={C_ASIDE}>dropped slightly once in the time I have owned it</color>. Topped it up and it has not moved since. Probably just needed bleeding.",
                            $"There was a <color={C_ASIDE}>very faint sweet smell from the engine bay</color> when I first got it. Had it looked at and was told it was residue from a previous coolant spill. Not an issue.",
                        })
                        : "Engine runs perfectly, starts first time every time, no smoke, no leaks.",

                    _ => null,
                };

            // ── TIMING BELT ───────────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.TimingBelt))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"My mate who knows cars said the <color={C_FAULT}>timing belt</color> on the <b>{l.Model}</b> might be overdue. <color={C_ASIDE}>I honestly have no idea when it was last done — there is no paperwork for it.</color>",
                            $"I <color={C_ASIDE}>cannot find any record of the timing belt being changed</color> on this <b>{l.Make}</b>. <color={C_FAULT}>It could be fine, it could be urgent — I genuinely do not know.</color>",
                            $"Someone mentioned the <b>{l.Year}</b> <b>{l.Model}</b> <color={C_FAULT}>should have its cambelt checked</color>. <color={C_ASIDE}>I have owned it three years and never done it, which probably tells you something.</color>",
                            $"No idea when the <color={C_FAULT}>timing belt</color> was last replaced on the <b>{l.Make}</b>. <color={C_ASIDE}>I asked the previous owner and they did not know either. Buyer beware and price reflects that.</color>",
                            $"The <b>{l.Model}</b> has <b>{l.Mileage:N0} miles</b> on it and I have <color={C_FAULT}>no cambelt history</color>. <color={C_ASIDE}>A mechanic friend said that is something to sort sooner rather than later.</color>",
                            $"I was told the <color={C_FAULT}>timing belt is something you should not ignore</color> on these <b>{l.Make}</b>s. <color={C_ASIDE}>I have been meaning to get it checked but never got around to it — hence the honest price.</color>",
                            $"There is <color={C_FAULT}>no stamp or receipt for the cambelt</color> in the <b>{l.Model}</b>'s history. <color={C_ASIDE}>Could have been done, could not have — I cannot say either way.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> is at <b>{l.Mileage:N0} miles</b>. <color={C_ASIDE}>I looked up the service interval for the timing belt and I think it is probably overdue. Priced low to account for that.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"The <color={C_FAULT}>timing belt is past its recommended replacement interval</color>. <color={C_ASIDE}>I would not risk driving it far; factor a belt and water pump kit into your budget immediately.</color>",
                            $"There is no proof the <color={C_FAULT}>cambelt has been done</color> recently. <color={C_ASIDE}>Given the <b>{l.Mileage:N0}</b> miles, it is living on borrowed time. Needs sorting ASAP.</color>",
                            $"I checked the service schedule and this <b>{l.Model}</b> is <color={C_FAULT}>due for a timing belt</color>. <color={C_ASIDE}>I have not done it, so you will need to handle it before putting serious miles on it.</color>",
                            $"The belt is <color={C_FAULT}>overdue</color>. <color={C_ASIDE}>If it snaps, it will ruin the engine. I have discounted the price by the rough cost of a local garage doing the job.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"These {l.Model}s have interference engines. The <color={C_FAULT}>belt is overdue</color> at {l.Mileage:N0}. <color={C_ASIDE}>Do not drive it until you replace the belt, tensioner, and water pump. I usually do them every 60k miles.</color>",
                            $"There's no documented <color={C_FAULT}>cambelt change</color> in the folder. <color={C_ASIDE}>On a {l.Make}, you always assume it's original if there's no proof. Tow it away and do a full timing service before putting it on the road.</color>",
                            $"<color={C_FAULT}>Timing belt is on borrowed time</color>. <color={C_ASIDE}>I've got the OEM Gates timing kit and Aisin water pump in the boot, just no time to fit it. You get the parts with the car.</color>",
                            $"The service interval for the <color={C_FAULT}>cambelt is 5 years or 70k</color>, and this is well past that. <color={C_ASIDE}>Don't risk snapping it. I've deducted the 400CR a specialist would charge you to do it.</color>",
                        })
                        : "Timing belt has not been changed in a long time. Needs doing before driving, hence the price.",

                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"I <color={C_FAULT}>could not tell you when the timing belt</color> was last done on the <b>{l.Make}</b>. <color={C_ASIDE}>No paperwork for that one unfortunately.</color>",
                            $"Never changed the <color={C_FAULT}>cambelt</color> myself on the <b>{l.Model}</b>. <color={C_ASIDE}>Maybe the previous owner did it, maybe they did not. I genuinely have no idea.</color>",
                            $"The <b>{l.Make}</b> has done <b>{l.Mileage:N0} miles</b>. <color={C_ASIDE}>Whether the timing belt has been done in that time I honestly could not say.</color>",
                            $"Someone asked me about the <color={C_FAULT}>cambelt</color> when I advertised it before. <color={C_ASIDE}>I had to google what that was. Still not entirely sure I understand it.</color>",
                            $"There is a <color={C_FAULT}>folder of paperwork</color> in the glovebox of the <b>{l.Model}</b>. <color={C_ASIDE}>I never went through it properly. Timing belt might be in there, might not be.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has been <color={C_FAULT}>running fine</color> so I never thought to ask about the belt. <color={C_ASIDE}>Probably means it is okay. That is my logic anyway.</color>",
                            $"I owned the <b>{l.Model}</b> for <color={C_FAULT}>three years and never touched the timing belt</color>. <color={C_ASIDE}>Before me, no idea. The previous owner seemed like a decent bloke though.</color>",
                            $"No service history to speak of for the <b>{l.Make}</b>. <color={C_ASIDE}>Whether that includes the cambelt or not I cannot honestly say. It has not snapped so far.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> just runs. <color={C_ASIDE}>I never looked into the maintenance schedule. <color={C_FAULT}>Belt stuff is one of those things I kept meaning to sort out.</color></color>",
                            $"A mate said I should check the <color={C_FAULT}>timing belt</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>I kept meaning to. This is the result of that.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Timing belt on the <b>{l.Make}</b> is <color={C_FAULT}>overdue by mileage</color>. <color={C_ASIDE}>I know the service interval on these. Just never got around to booking it in before it went into storage.</color>",
                            $"The <b>{l.Model}</b> has been sitting since I took it off the road. <color={C_ASIDE}>The <color={C_FAULT}>cambelt was already overdue</color> when I parked it up. That was two years ago.</color>",
                            $"No paperwork for the <color={C_FAULT}>timing belt</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>At <b>{l.Mileage:N0} miles</b> I would not risk driving it without doing the belt first. I am being straight with you.</color>",
                            $"I know enough about the <b>{l.Model}</b> to know the <color={C_FAULT}>cambelt should have been done a while ago</color>. <color={C_ASIDE}>It is an interference engine so if it goes it takes the valves with it. Priced with that in mind.</color>",
                            $"The <b>{l.Make}</b> has been <color={C_FAULT}>standing for long enough that I would change the belt on age alone</color>, never mind mileage. <color={C_ASIDE}>Rubber does not do well sitting unused.</color>",
                            $"<color={C_FAULT}>Timing belt history is unknown</color> on the <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_ASIDE}>It was one of the reasons I stopped using it daily. Not worth the gamble at this mileage.</color>",
                            $"The <b>{l.Make}</b> has been <color={C_FAULT}>off the road long enough that the belt should be treated as a first job</color>. <color={C_ASIDE}>Even if the mileage looks okay, rubber sitting static for two years is not the same as rubber being used.</color>",
                            $"I never sorted the <color={C_FAULT}>cambelt</color> on the <b>{l.Model}</b> before parking it up. <color={C_ASIDE}>Fully aware that was not ideal. Priced accordingly — the next owner sorts it before driving.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAULT}>Timing belt is overdue</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>I know the service interval on this engine to the mile. It is past it. Interference engine — do not start it until the belt, tensioner and water pump are done. I have factored the cost of a genuine kit into the asking price.</color>",
                            $"No cambelt history on the <b>{l.Year}</b> <b>{l.Model}</b> and at <b>{l.Mileage:N0} miles</b> <color={C_FAULT}>you have to assume it needs doing</color>. <color={C_ASIDE}>That is how I priced it. Gates or Dayco kit, Aisin water pump, new thermostat while you are in there. Budget is already built into what I am asking.</color>",
                            $"The <b>{l.Make}</b> needs a <color={C_FAULT}>full timing service</color> before it moves under its own power. <color={C_ASIDE}>Belt, tensioner, idler, water pump — the lot. I do not cut corners on these and I am not going to tell you to either. The price reflects a car that needs that job done before use.</color>",
                            $"<color={C_FAULT}>Cambelt is on borrowed time</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>At <b>{l.Mileage:N0}</b> with no documented change I would not risk it even around the block. Tow it, do the timing service, then drive it. That is the right order. Priced with that first job costed in.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> is <color={C_FAULT}>past its timing belt interval</color>. <color={C_ASIDE}>I have seen enough snapped belts on this engine family to know the damage it causes. Buy it, do the belt before anything else, enjoy it for years. The asking price assumes you will be doing that job.</color>",
                            $"Selling with the <color={C_FAULT}>timing belt outstanding</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>I know what the job costs at a good independent and I have priced it out of the car already. No games — I do too many of these to waste time on them.</color>",
                            $"<color={C_FAULT}>Belt and water pump are overdue</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>This is not a guess — I pulled the inspection cover and checked. The belt is original as far as I can tell. At <b>{l.Mileage:N0}</b> miles that is a problem. Priced as a car that needs that job first.</color>",
                        })
                        : "I could not tell you when the timing belt was last changed — no paperwork for that.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_GOOD}>Full service history</color> with the <b>{l.Model}</b>, <color={C_GOOD}>timing belt was replaced</color> as part of the last service.",
                            $"Timing belt was <color={C_GOOD}>done recently</color> by a proper garage — <color={C_ASIDE}>I have the receipt somewhere, will dig it out on viewing.</color>",
                            $"<color={C_GOOD}>Cambelt, water pump, tensioner — all replaced</color> at the correct mileage. <color={C_ASIDE}>Nothing to worry about there.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Timing belt was <color={C_GOOD}>done not long ago</color>. I have a receipt somewhere — will find it before viewing.",
                            $"Previous owner told me the <color={C_GOOD}>cambelt was recently replaced</color>. No reason to doubt it.",
                            $"<color={C_GOOD}>Belt and water pump both done</color> at the last service. Nothing to worry about there.",
                            $"I specifically asked about the timing belt when I bought it. <color={C_GOOD}>Was told it was sorted.</color>",
                            $"Service history shows <color={C_GOOD}>cambelt work was carried out</color>. Stamp is in the folder.",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Timing belt was <color={C_FAKE_OK}>replaced as part of a service package</color> — I have the invoice somewhere, will dig it out before viewing.",
                            $"<color={C_FAKE_OK}>Belt was done</color> by the previous owner according to the history folder. Everything is in there.",
                            $"Cambelt, tensioner and water pump <color={C_FAKE_OK}>all replaced at the correct interval</color>. I can show you the paperwork on viewing.",
                            $"Service history shows the <color={C_FAKE_OK}>timing belt was replaced</color> — stamp is present, date and mileage are consistent.",
                            $"I specifically asked about the belt when I bought it. <color={C_FAKE_OK}>Was told it had been done recently</color> and the mileage in the folder supports that.",
                        })
                        : "Full service history with the car, timing belt was replaced as part of the last service.",

                    _ => null,
                };

            // ── BRAKES GONE ───────────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.BrakesGone))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"The <color={C_FAULT}>brakes</color> on the <b>{l.Model}</b> are worn down — <color={C_ASIDE}>I could feel it getting worse over the last few weeks. Definitely needs new pads before it goes on a road.</color>",
                            $"Stopping distance on the <b>{l.Make}</b> feels <color={C_FAULT}>longer than it used to</color>. <color={C_ASIDE}>I think the pads are pretty much gone at this point, hence the price.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> makes a <color={C_FAULT}>grinding noise when braking</color>. <color={C_ASIDE}>I am told that means the pads are metal on metal. Needs sorting before driving.</color>",
                            $"Brakes have been <color={C_FAULT}>squealing</color> on the <b>{l.Model}</b> for a while. <color={C_ASIDE}>I kept meaning to get it seen to. They work, just about, but they need replacing.</color>",
                            $"The <b>{l.Make}</b> pulls <color={C_FAULT}>slightly to one side under braking</color>. <color={C_ASIDE}>Someone told me that could be a worn caliper or uneven pads. Either way it needs attention.</color>",
                            $"I will be honest — the <color={C_FAULT}>brakes on this <b>{l.Model}</b> need doing</color>. <color={C_ASIDE}>Nothing dangerous at low speeds but I would not take it on a motorway. Price reflects that.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAULT}>Brake pads and discs are completely shot</color>. <color={C_ASIDE}>It is metal-on-metal, so braking performance is severely compromised right now.</color>",
                            $"The <b>{l.Make}</b> needs a <color={C_FAULT}>full brake overhaul</color> — pads and probably discs. <color={C_ASIDE}>They grind terribly. It is a straightforward job, but it needs doing before regular use.</color>",
                            $"<color={C_FAULT}>Brakes are heavily worn</color> and the pedal feel is awful. <color={C_ASIDE}>Do not expect to drive it away and ignore it — they need replacing immediately.</color>",
                            $"It fails to stop properly because the <color={C_FAULT}>brakes are finished</color>. <color={C_ASIDE}>I have priced the car knowing you will need a new brake kit straight away.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Front discs are warped and <color={C_FAULT}>pads are down to the backing plates</color>. <color={C_ASIDE}>Typical for the heavy {l.Model}. Calipers retract fine, but you need a full set of fresh rotors and pads immediately.</color>",
                            $"<color={C_FAULT}>Brakes are completely glazed and worn out</color>. <color={C_ASIDE}>I usually upgrade to slotted rotors and fast-road pads on these, but I'll leave that choice to the next owner. Do not drive it home.</color>",
                            $"The wear sensors have triggered — <color={C_FAULT}>metal on metal braking</color>. <color={C_ASIDE}>These {l.Make}s need good stopping power. I've deducted the cost of a full OEM brake kit from the asking price.</color>",
                            $"<color={C_FAULT}>Discs have a massive lip and pads are finished</color>. <color={C_ASIDE}>The sliders probably need re-greasing too. It's a half-day job on the driveway, but it's unsafe for the motorway right now.</color>",
                        })
                        : "Brakes are worn down and need replacing before this goes on a public road — pads are basically metal on metal at this point.",

                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"Stopping distance feels <color={C_FAULT}>a little longer</color> than it used to on the <b>{l.Make}</b>. <color={C_ASIDE}>I have always been a cautious driver so it has not been a problem for me.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>squeaks a bit</color> when you brake. <color={C_ASIDE}>Has done it for about a year. I just turned the radio up a bit.</color>",
                            $"Brakes on the <b>{l.Year}</b> <b>{l.Make}</b> work. <color={C_ASIDE}>Not as sharp as when I bought it but I have not had any near misses or anything.</color>",
                            $"There is <color={C_FAULT}>a grinding noise</color> from the front of the <b>{l.Model}</b> when I brake hard. <color={C_ASIDE}>I never really brake hard so it does not come up much.</color>",
                            $"The <b>{l.Make}</b> <color={C_FAULT}>pulls very slightly to the left</color> when braking. <color={C_ASIDE}>I just steer right a bit to compensate. Honestly barely notice it anymore.</color>",
                            $"Brake pedal on the <b>{l.Model}</b> goes <color={C_FAULT}>a bit further down</color> than it used to. <color={C_ASIDE}>Still stops eventually. I never looked into it.</color>",
                            $"I think the <color={C_FAULT}>pads might be getting low</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>They squeak sometimes on the way out of the drive in the morning. Probably just damp.</color>",
                            $"The <b>{l.Model}</b> takes <color={C_FAULT}>a bit more distance to stop</color> now. <color={C_ASIDE}>I just leave more gap. Simple enough solution until I got round to fixing it, which I never did.</color>",
                            $"Brakes on the <b>{l.Make}</b> are <color={C_FAULT}>not great</color>. <color={C_ASIDE}>They work, just. I never prioritised it because I mostly drive around town.</color>",
                            $"There is <color={C_FAULT}>a bit of vibration</color> through the brake pedal on the <b>{l.Model}</b>. <color={C_ASIDE}>I googled it once. Too many possibilities came up so I closed the tab.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Brakes on the <b>{l.Make}</b> were <color={C_FAULT}>already marginal when I parked it up</color>. <color={C_ASIDE}>That was a couple of years ago. I would budget for a full set before putting it back on the road.</color>",
                            $"The <b>{l.Model}</b> needs <color={C_FAULT}>new pads at minimum, probably discs too</color>. <color={C_ASIDE}>I noticed the judder before I stopped using it. Knew what it was, just never got round to it.</color>",
                            $"<color={C_FAULT}>Pads are worn down</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>I could hear them starting to go. Parked it up rather than drive it like that. It has been there since.</color>",
                            $"The <b>{l.Make}</b> has been <color={C_FAULT}>sitting long enough that the discs will have surface rust</color> on top of the existing wear. <color={C_ASIDE}>A fresh brake kit is a first job before this goes anywhere.</color>",
                            $"I know the <color={C_FAULT}>brakes need doing</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>It was one of the jobs on the list when I took it off the road. List never got shorter. Here we are.</color>",
                            $"<color={C_FAULT}>Rear brakes are seized slightly</color> from sitting. <color={C_ASIDE}>The <b>{l.Make}</b> has not moved properly in over a year. The fronts were already worn before it was parked. Whole system needs a look.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color={C_FAULT}>worn friction material on the fronts</color>. <color={C_ASIDE}>I checked before parking it. Did not think it was worth doing the brakes on a car I was not going to use. Priced to cover it.</color>",
                            $"Standing cars and brakes do not get along. <color={C_ASIDE}>The <b>{l.Make}</b> has been <color={C_FAULT}>off the road long enough that I would do a full brake service</color> before trusting it. Be realistic about the budget.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAULT}>Full brake refresh needed</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>Pads are finished, discs have a significant lip, and the calipers will need freeing off after standing. I have costed a full OEM brake kit into the asking price. It is a half-day job on axle stands.</color>",
                            $"The <b>{l.Model}</b> needs <color={C_FAULT}>pads and discs all round</color>. <color={C_ASIDE}>I checked before listing. Fronts are metal on metal, rears have seized slightly from standing. Known, costed, priced in. Do not try to drive it to a garage — sort the brakes first.</color>",
                            $"Brakes on the <b>{l.Year}</b> <b>{l.Make}</b> are <color={C_FAULT}>not roadworthy</color>. <color={C_ASIDE}>I am telling you that upfront because I price these cars honestly and I expect the buyer to know what they are getting. Fresh pads, discs, and a caliper service — all factored into what I am asking.</color>",
                            $"The <b>{l.Make}</b> needs a <color={C_FAULT}>complete brake overhaul</color>. <color={C_ASIDE}>Pads gone, discs scored, one caliper is binding. I have done enough of these to know exactly what parts cost. That cost is already out of the price. Collect it on a trailer.</color>",
                            $"<color={C_FAULT}>Brakes are finished</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>Both axles need attention — pads, discs, and the sliders will want greasing at minimum. I do not sell cars with hidden brake problems. They are listed, they are priced in, they are your first job.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color={C_FAULT}>worn-out brakes and seized rear calipers</color> from sitting. <color={C_ASIDE}>Standard consequence of long-term storage on a car that was already marginal on brake wear. Budget is in the price. Do the job before you use it.</color>",
                            $"<color={C_FAULT}>Brake system needs a full rebuild</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>I inspected it properly before listing. I always do. Pads are scrap, two discs are warped, rear offside caliper is seized solid. All of that cost is factored into what I am asking. No surprises on collection.</color>",
                        })
                        : "Stopping distance feels a little longer than it used to but I have always been a cautious driver.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"Brakes on the <b>{l.Model}</b> were <color={C_GOOD}>checked and adjusted</color> at the last service. <color={C_ASIDE}>No issues found.</color>",
                            $"<color={C_GOOD}>Brand new brake pads</color> fitted all round on the <b>{l.Make}</b>. <color={C_GOOD}>Stops on a sixpence.</color>",
                            $"Brakes are <color={C_GOOD}>excellent</color> on this one. <color={C_ASIDE}>One of the things the mechanic specifically commented on.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Brakes were <color={C_GOOD}>serviced recently</color>. Stops well, no issues at all.",
                            $"Had new pads fitted <color={C_GOOD}>a few months ago</color>. Discs are fine.",
                            $"Braking is <color={C_GOOD}>sharp and responsive</color>. No judder, no noise, no pulling.",
                            $"<color={C_GOOD}>Brake fluid was changed</color> as part of a recent service. System is in good order.",
                            $"Mechanic checked the brakes when I bought it. <color={C_GOOD}>Said they were well within spec.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Brakes were <color={C_FAKE_OK}>inspected and passed</color> at the last MOT with no advisories. They feel fine to me.",
                            $"Had the front pads <color={C_FAKE_OK}>checked recently</color> — plenty of material left according to the garage.",
                            $"Stopping is <color={C_FAKE_OK}>sharp and progressive</color>. No judder, no pulling, no noise. Brakes are not a concern on this one.",
                            $"<color={C_FAKE_OK}>Brake fluid was changed</color> as part of the last service. System is in good order.",
                            $"I specifically checked the brakes before listing. <color={C_FAKE_OK}>Discs and pads are all within spec</color> — no issues.",
                        })
                        : "Brakes were checked and adjusted at the last service, no issues found.",

                    _ => null,
                };

            // ── SUSPENSION WORN ───────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.SuspensionWorn))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Model}</b> <color={C_FAULT}>bounces a bit more than it should</color> over bumps. <color={C_ASIDE}>A friend said the shocks might be on their way out but I am not sure how serious that is.</color>",
                            $"There is a <color={C_FAULT}>clunking noise</color> from the front of the <b>{l.Make}</b> when going over speed bumps. <color={C_ASIDE}>I have been meaning to get it looked at for months. Here we are.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>rides a bit wallowy</color>. <color={C_ASIDE}>I am told that is the shock absorbers. They probably need replacing but I never got around to it.</color>",
                            $"Suspension on the <b>{l.Model}</b> <color={C_FAULT}>makes a noise over rough ground</color>. <color={C_ASIDE}>I have just been avoiding potholes. Priced to allow for whatever it needs.</color>",
                            $"The <b>{l.Make}</b> <color={C_FAULT}>sits a little low on one corner</color>. <color={C_ASIDE}>Not sure if it is a spring or a shock but something is not right. Obvious once you see it.</color>",
                            $"Handling on the <b>{l.Model}</b> feels <color={C_FAULT}>vague and floaty</color>. <color={C_ASIDE}>I was told that is usually suspension-related on these. Not dangerous at normal speeds but it needs attention.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"The <color={C_FAULT}>suspension is very tired</color>. Shocks are soft and there's a knock from the lower arms or bushings. <color={C_ASIDE}>Drivable, but the handling is sloppy.</color>",
                            $"It <color={C_FAULT}>clunks over speed bumps</color>. Likely drop links or shock absorbers. <color={C_ASIDE}>Common issue on a <b>{l.Year}</b> <b>{l.Make}</b>, just needs some fresh suspension parts.</color>",
                            $"Ride quality is poor due to <color={C_FAULT}>worn out suspension components</color>. <color={C_ASIDE}>It feels unstable at highway speeds. Needs an alignment and some bushes.</color>",
                            $"The <b>{l.Model}</b> suffers from <color={C_FAULT}>worn suspension</color>. <color={C_ASIDE}>I've had it on a ramp, looks like the struts are leaking. Not dangerous yet, but definitely an MOT failure soon.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"The front lower control arm bushes are shot. <color={C_FAULT}>Suspension is knocking</color>. <color={C_ASIDE}>Very common on the {l.Year} {l.Make}. It tramlines heavily; it needs polybushes and an alignment.</color>",
                            $"Struts are leaking fluid and the <color={C_FAULT}>top mounts are dead</color>. <color={C_ASIDE}>It ruins the handling of the {l.Model}. It needs a full coilover setup or an OEM Sachs shock refresh.</color>",
                            $"<color={C_FAULT}>Rear trailing arm bushings (RTABs) are completely gone</color>. <color={C_ASIDE}>Suspension feels like a waterbed. It's an easy fix if you have a bush press tool, priced accordingly.</color>",
                            $"<color={C_FAULT}>Anti-roll bar links and front shocks are dead</color>. <color={C_ASIDE}>I've factored a full suspension rebuild into my asking price. I know the {l.Make} market inside out, so don't offer less.</color>",
                        })
                        : "Suspension is worn out — shocks are soft and there are some clunks over bumps that need sorting.",

                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Make}</b> <color={C_FAULT}>bounces a bit</color> over bumps. <color={C_ASIDE}>I just avoid the bad potholes. Could be the roads around here more than the car.</color>",
                            $"There is <color={C_FAULT}>a knock from the front</color> of the <b>{l.Model}</b> on rough roads. <color={C_ASIDE}>Comes and goes. Never stopped the car from going so I never chased it.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>rides a bit firm</color>. <color={C_ASIDE}>I thought that was just how these cars felt. Maybe it is, maybe it is not.</color>",
                            $"Something <color={C_FAULT}>clunks</color> on the <b>{l.Model}</b> when I go over speed bumps. <color={C_ASIDE}>Only does it slowly. I slow right down and it is usually fine.</color>",
                            $"The <b>{l.Make}</b> <color={C_FAULT}>wanders a bit</color> on the motorway. <color={C_ASIDE}>I hold the wheel a bit tighter at speed. Got used to it.</color>",
                            $"Front end of the <b>{l.Model}</b> <color={C_FAULT}>dips quite a bit</color> under braking. <color={C_ASIDE}>I assumed all cars did that. My brother said it might be the shocks. I did not do anything about it.</color>",
                            $"There is <color={C_FAULT}>a creak from somewhere underneath</color> the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>On hot days it does not do it. Cold days it does. I just assume it is expanding or contracting or something.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>sits a bit low on one side</color>. <color={C_ASIDE}>Has done since I bought it. I always park on a slight slope so you cannot really tell.</color>",
                            $"Handling on the <b>{l.Make}</b> is <color={C_FAULT}>a bit vague</color>. <color={C_ASIDE}>Not scary or anything. Just not as tight as maybe it should be. I am not a sporty driver so it never bothered me.</color>",
                            $"There is <color={C_FAULT}>a rattle from the passenger side</color> of the <b>{l.Model}</b> over rough ground. <color={C_ASIDE}>I checked and nothing looks obviously broken underneath. Beyond that I was not sure what I was looking at.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Front suspension on the <b>{l.Make}</b> <color={C_FAULT}>has a knock that was getting worse</color> before I parked it. <color={C_ASIDE}>Probably drop links or lower arm bushes. Common enough on these at this age.</color>",
                            $"The <b>{l.Model}</b> has <color={C_FAULT}>tired shocks all round</color>. <color={C_ASIDE}>You can feel it through the steering. Was fine for town use but I would not have taken it on a motorway at that point.</color>",
                            $"<color={C_FAULT}>Suspension bushes are shot</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>I could feel the vagueness in the handling before it went into storage. Classic high-mileage wear on this chassis.</color>",
                            $"The <b>{l.Make}</b> has been <color={C_FAULT}>standing long enough that the rubber components will have deteriorated further</color>. <color={C_ASIDE}>They were already soft when I parked it. A suspension refresh is a first job.</color>",
                            $"<color={C_FAULT}>Rear shock absorbers are leaking</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>I spotted it before putting it in storage. Meant to sort it, did not. Straightforward enough job for someone with a lift.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>tramlines and wanders</color> more than it should. <color={C_ASIDE}>I know the front control arm bushes are worn. Beyond that I did not strip it down. Could be more.</color>",
                            $"Ride quality on the <b>{l.Model}</b> <color={C_FAULT}>had become noticeably worse</color> before I took it off the road. <color={C_ASIDE}>It is not dangerous, but it is not right either. Someone who knows their way around suspension will see what it needs.</color>",
                            $"The <b>{l.Make}</b> has <color={C_FAULT}>a clunk over rough ground</color> that I traced to the front drop links. <color={C_ASIDE}>Twenty pound parts and a Saturday morning to fix. I just never had that Saturday.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAULT}>Front lower arm bushes are finished</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>Classic wear point on this chassis at this mileage. Polybush kit and a geometry setup afterwards — I know the job, I have done it on three of these. Cost is already out of the asking price.</color>",
                            $"The <b>{l.Model}</b> has <color={C_FAULT}>worn suspension throughout</color>. <color={C_ASIDE}>Shocks are tired, front ARB links are gone, inner tie rod ends have play. I inspected it on a ramp before listing. Nothing structural — all serviceable parts. Priced with a full suspension refresh in mind.</color>",
                            $"<color={C_FAULT}>Suspension needs attention</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Drop links and front strut top mounts are the priority. The rest is wear-related softness rather than anything dangerous. Straightforward job for anyone with a spring compressor. Priced accordingly.</color>",
                            $"The <b>{l.Make}</b> has <color={C_FAULT}>a knocking front end</color>. <color={C_ASIDE}>I traced it to the lower control arm bushes and the front drop links. Both are cheap parts on this car. I know because I priced the job before deciding to sell instead. That cost is reflected in what I am asking.</color>",
                            $"<color={C_FAULT}>Rear trailing arm bushings are shot</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>Common failure on this chassis. If you know these cars you know the fix. If you do not, get a quote first. I have priced the car with that job costed in at a reasonable independent rate.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>tired suspension all round</color>. <color={C_ASIDE}>I have been specific in my inspection — both front shocks are leaking, nearside rear spring has settled. The rest of the geometry is serviceable. All factored into the price. No vague 'needs some work' here.</color>",
                            $"<color={C_FAULT}>Anti-roll bar links and front shocks are worn out</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>I checked it on ramps before setting the price. These are known wear items on the <b>{l.Year}</b> <b>{l.Make}</b>. I deal in these regularly enough to price them accurately. The number I am asking reflects that.</color>",
                        })
                        : "Rides a bit firm maybe, could be the roads around here, never bothered checking.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"Suspension on the <b>{l.Model}</b> feels <color={C_GOOD}>tight and responsive</color>. <color={C_ASIDE}>No knocks or creaks at all.</color>",
                            $"Just had <color={C_GOOD}>new shocks fitted</color> on the <b>{l.Make}</b>. <color={C_GOOD}>Rides like a new car.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> handles <color={C_GOOD}>beautifully</color>. <color={C_ASIDE}>Suspension is solid, no issues whatsoever.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Suspension feels <color={C_GOOD}>solid and composed</color>. No knocks or creaks that I have noticed.",
                            $"Had the front end <color={C_GOOD}>checked on a ramp</color> recently. Nothing flagged.",
                            $"Rides <color={C_GOOD}>smoothly and quietly</color>. Handles well at all speeds.",
                            $"Alignment was <color={C_GOOD}>done recently</color>. Tracks straight, no uneven tyre wear.",
                            $"Shocks feel <color={C_GOOD}>tight and responsive</color>. No wallowing, no diving under braking.",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Ride is <color={C_FAKE_OK}>composed and comfortable</color> — no knocks, no creaks, nothing that concerned me.",
                            $"Suspension was <color={C_FAKE_OK}>checked on a ramp before listing</color>. Nothing flagged.",
                            $"These <b>{l.Make}</b>s can feel <color={C_ASIDE}>slightly firm over rough roads</color> — that is just the setup, not a fault.",
                            $"Handling feels <color={C_FAKE_OK}>tight and direct</color>. No wandering, no vibration through the wheel at speed.",
                            $"Had an alignment done recently. <color={C_FAKE_OK}>Tracks perfectly straight</color>, no uneven tyre wear.",
                        })
                        : "Suspension feels tight and responsive, no knocks or creaks.",

                    _ => null,
                };

            // ── ELECTRICAL FAULT ──────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.ElectricalFault))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Model}</b> has a <color={C_FAULT}>warning light on the dash</color> that I cannot get to go away. <color={C_ASIDE}>I had it plugged in at Halfords and they said something about the alternator but I did not really follow it.</color>",
                            $"The <color={C_FAULT}>battery keeps going flat</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>I have replaced the battery twice now and the problem came back both times, so it must be something else.</color>",
                            $"There is an <color={C_FAULT}>intermittent electrical issue</color> with the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Sometimes things on the dash just stop working for a bit then come back. I cannot reproduce it on demand.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>occasionally does not start first time</color>. <color={C_ASIDE}>Jump leads always sort it but that is clearly not a long term solution. I am told it could be the alternator not charging properly.</color>",
                            $"There is a <color={C_FAULT}>drain on the electrics</color> somewhere in the <b>{l.Make}</b>. <color={C_ASIDE}>Mechanic said something about a parasitic draw but could not find the source without more time spent on it. Price reflects the unknown.</color>",
                            $"Some <color={C_FAULT}>electrics cut out randomly</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>Windows, radio, interior lights — they just stop sometimes. Comes back after turning it off and on. Very annoying.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"There's an <color={C_FAULT}>electrical drain</color>, likely the alternator not charging. <color={C_ASIDE}>You'll need a multimeter and some patience to trace it properly.</color>",
                            $"It has a <color={C_FAULT}>parasitic battery draw</color>. <color={C_ASIDE}>If left for a few days, it goes flat. I suspect a bad ground or a faulty relay, but I don't have the time to chase it.</color>",
                            $"The <b>{l.Make}</b> has an <color={C_FAULT}>intermittent electrical fault</color> on the dashboard. <color={C_ASIDE}>Could be the alternator regulator. It runs, but you'll need to diagnose it properly.</color>",
                            $"<color={C_FAULT}>Electrics are acting up</color>. The battery isn't getting charged. <color={C_ASIDE}>Priced lower because I know auto-electrician hourly rates aren't cheap.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"Classic {l.Make} electrical gremlin. The <color={C_FAULT}>alternator voltage regulator is fried</color>. <color={C_ASIDE}>It's pushing barely 11.5 volts. It's a £150 part and an hour of your time to swap it out.</color>",
                            $"There's a <color={C_FAULT}>parasitic draw draining the battery</color>. <color={C_ASIDE}>Usually it's the comfort control module on these {l.Model}s. I just disconnect the negative terminal overnight. Needs diagnosing properly.</color>",
                            $"Known issue on the {l.Year} models: the <color={C_FAULT}>alternator diode pack has failed</color>. <color={C_ASIDE}>Car runs perfectly on a fresh battery for about 20 miles, then dies. Bring a trailer or a spare battery.</color>",
                            $"The <color={C_FAULT}>alternator is dead</color> and the dash is a Christmas tree. <color={C_ASIDE}>I checked it with a multimeter. The loom is fine, but you need a new Bosch unit before you can daily drive it.</color>",
                        })
                        : "There is an electrical fault — the alternator is not charging properly and the battery keeps going flat, which is why the price is what it is.",

                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"A <color={C_FAULT}>warning light</color> comes on sometimes on the <b>{l.Make}</b>. <color={C_ASIDE}>Turns itself off after a while. Never figured out what it meant.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>takes a couple of tries to start</color> sometimes. <color={C_ASIDE}>Usually fine once it gets going. Probably just the battery getting old.</color>",
                            $"One of the <color={C_FAULT}>windows stopped working</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>The driver side one still goes up and down. I just leave the other ones closed.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>kills the battery</color> if it sits for more than a week. <color={C_ASIDE}>I keep jump leads in the boot. Sorted in a minute once you know what you are doing.</color>",
                            $"Radio on the <b>{l.Make}</b> <color={C_FAULT}>cuts out randomly</color>. <color={C_ASIDE}>Comes back on its own eventually. I just sing to myself in the meantime.</color>",
                            $"There is an <color={C_FAULT}>orange light</color> on the dash of the <b>{l.Model}</b> that has been on for about a year. <color={C_ASIDE}>I asked at the garage and they gave me a price I did not like. So here we are.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>does not always start first time in the cold</color>. <color={C_ASIDE}>Give it two or three turns and it gets there. Summer it is fine.</color>",
                            $"Central locking on the <b>{l.Model}</b> <color={C_FAULT}>works on the driver side only</color>. <color={C_ASIDE}>The passengers just use the key in the door. Nobody has complained.</color>",
                            $"The <b>{l.Make}</b> has <color={C_FAULT}>a few warning lights on the dash</color>. <color={C_ASIDE}>I was told one of them is just an old sensor. The others I am less sure about.</color>",
                            $"Electrics on the <b>{l.Model}</b> are <color={C_FAULT}>a bit temperamental</color>. <color={C_ASIDE}>Nothing that has left me stranded. Just little things that come and go.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Make}</b> has a <color={C_FAULT}>parasitic drain</color> that flattens the battery if it sits for more than a few days. <color={C_ASIDE}>Never tracked down the source properly. It is on the list of reasons it stopped being my daily.</color>",
                            $"Alternator on the <b>{l.Model}</b> <color={C_FAULT}>was not charging properly</color> before I parked it. <color={C_ASIDE}>Battery light came on intermittently. Did not want to risk a breakdown so I took it off the road. Never got back to it.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>a fault I never fully diagnosed</color>. <color={C_ASIDE}>Multiple warning lights, suspected alternator or a bad earth. Ran fine most of the time but I did not trust it for longer journeys.</color>",
                            $"Battery on the <b>{l.Make}</b> is <color={C_FAULT}>probably flat after sitting this long</color>. <color={C_ASIDE}>On top of that there was already an intermittent electrical issue before it went into storage. Bring jump leads and manage your expectations.</color>",
                            $"The <b>{l.Model}</b> has <color={C_FAULT}>an ongoing electrical fault</color> I did not get to the bottom of. <color={C_ASIDE}>I suspected the alternator regulator but never confirmed it. Priced to reflect the unknown.</color>",
                            $"There is <color={C_FAULT}>a known wiring issue</color> on the <b>{l.Year}</b> <b>{l.Make}</b> that I traced to the fusebox area. <color={C_ASIDE}>Beyond that I did not have the diagnostic equipment to go further. It is someone else's problem at this price.</color>",
                            $"The <b>{l.Make}</b> started <color={C_FAULT}>throwing codes before I parked it</color>. <color={C_ASIDE}>I cleared them and they came back. Not chasing ghost faults on a car I was done with. Priced accordingly.</color>",
                            $"<color={C_FAULT}>Electrics were playing up</color> towards the end of me using the <b>{l.Model}</b> daily. <color={C_ASIDE}>Intermittent, never consistent enough to pin down. After a long enough time standing it will need a proper look regardless.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Make}</b> has a <color={C_FAULT}>known alternator fault</color>. <color={C_ASIDE}>I checked it with a multimeter before listing — output is down to 11.8 volts under load. New Bosch unit is the fix. I know the cost, I have priced it out of the car. Straightforward job for anyone with a socket set.</color>",
                            $"<color={C_FAULT}>Parasitic drain</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>I traced it to the comfort control module area — classic on this chassis. I did not pull the module because I was not rebuilding it, I was pricing it to sell. That diagnostic cost is already in what I am asking.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>an electrical fault I have narrowed down to the alternator diode pack</color>. <color={C_ASIDE}>Runs fine on a charged battery for a reasonable distance, then the voltage drops. It is a known failure mode on this engine variant. Cost of a replacement unit is factored into the price.</color>",
                            $"<color={C_FAULT}>Alternator is not charging</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>I confirmed it before listing — battery sits at resting voltage, does not climb under running. The loom is fine, the battery is fine, the alternator is not. I deal in enough of these to know what the part costs. It is already out of the price.</color>",
                            $"The <b>{l.Model}</b> has <color={C_FAULT}>a charging fault</color>. <color={C_ASIDE}>Dashboard confirms it, multimeter confirms it. I am not going to list a car with a known electrical problem and not say what it is. Alternator or regulator — one way to be sure is to swap the unit. Cost is in the asking price.</color>",
                            $"<color={C_FAULT}>ECU fault codes</color> stored on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>I pulled them before listing. Alternator output related — nothing in the engine management itself. These codes will clear once the charging issue is resolved. I have priced the repair into the car.</color>",
                            $"The <b>{l.Make}</b> has an <color={C_FAULT}>intermittent no-start caused by the charging circuit</color>. <color={C_ASIDE}>Classic symptom of a failing alternator on this platform. I have seen it enough times to recognise it immediately. The fix is straightforward. The cost of that fix is already out of my asking price.</color>",
                        })
                        : "A warning light comes on occasionally, turns itself off after a while, never figured out what it was.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_GOOD}>All electrics working perfectly</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>No warning lights on the dash at all.</color>",
                            $"Electrics on the <b>{l.Make}</b> are <color={C_GOOD}>absolutely fine</color>. <color={C_GOOD}>New battery fitted</color> recently too.",
                            $"<color={C_GOOD}>Everything works</color> — windows, lights, radio, all of it. <color={C_ASIDE}>Had no electrical issues in all the time I have owned the <b>{l.Model}</b>.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_GOOD}>All electrics working perfectly</color>. No warning lights, no drains, no gremlins.",
                            $"Battery was <color={C_GOOD}>replaced recently</color>. Alternator checked at the same time — all fine.",
                            $"Had a <color={C_GOOD}>diagnostic check</color> done before listing. Zero stored codes.",
                            $"Electrics are <color={C_GOOD}>completely reliable</color>. Never had any issues in the time I have owned it.",
                            $"Everything works — windows, lights, radio, climate. <color={C_GOOD}>No electrical concerns whatsoever.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAKE_OK}>All electrics functioning correctly</color>. No warning lights, no gremlins, no intermittent faults.",
                            $"Battery is <color={C_FAKE_OK}>less than a year old</color>. Alternator was checked and is charging correctly.",
                            $"Had a <color={C_FAKE_OK}>full diagnostic scan</color> done — zero stored codes, all sensors reading within parameters.",
                            $"Electrics on these <b>{l.Make}</b>s have a reputation but <color={C_FAKE_OK}>this one has never given me any trouble</color>.",
                            $"<color={C_FAKE_OK}>Everything works</color> — windows, lights, climate, all of it. No issues in the time I have owned it.",
                        })
                        : "All electrics working perfectly, no warning lights on the dash.",

                    _ => null,
                };

            // ── EXHAUST RUSTED ────────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.ExhaustRusted))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"The <color={C_FAULT}>exhaust system has rusted through</color> at the mid-section. <color={C_ASIDE}>It's blowing pretty loudly, will need a patch welded or a new cat-back section.</color>",
                            $"There's <color={C_FAULT}>heavy corrosion on the exhaust</color> and a visible hole. <color={C_ASIDE}>It's noisy on acceleration. Replacement exhausts for a <b>{l.Model}</b> are cheap enough though.</color>",
                            $"Exhaust is <color={C_FAULT}>blowing due to rust</color>. <color={C_ASIDE}>It won't pass its next inspection like this. You can hear it as soon as you start the engine.</color>",
                            $"The backbox and mid-pipe are <color={C_FAULT}>heavily rusted</color>. <color={C_ASIDE}>I haven't bothered replacing it as I'm selling, but be prepared for a sporty exhaust note on the drive home.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Just so you know, the <color={C_FAULT}>exhaust is showing some serious rust</color>. It's starting to blow a bit, so it'll need a patch or replacement soon.",
                            $"Mechanically it's fine, but the <color={C_FAULT}>exhaust system is quite corroded</color>. <color={C_ASIDE}>I've already adjusted the price of this {l.Make} because of it.</color>",
                            $"The {l.Model} sounds a bit more aggressive than usual because the <color={C_FAULT}>muffler is rusted through</color>. <color={C_ASIDE}>It's an easy fix if you have a welder.</color>",
                            $"I should mention the <color={C_FAULT}>rusted exhaust pipe</color>. <color={C_ASIDE}>It's still holding together, but it won't pass the next inspection without some work.</color>",
                            $"The car is solid, but the <color={C_FAULT}>exhaust is the weakest link</color> right now due to surface rust and one small hole near the back.",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"The flex-pipe on the downpipe is <color={C_FAULT}>rusted through</color>. <color={C_ASIDE}>Standard rot for a {l.Make} of this era. Perfect excuse to put a stainless steel system on it.</color>",
                            $"Rear silencer has <color={C_FAULT}>rusted out from short trips</color>. It's loud. <color={C_ASIDE}>You can weld a patch or just bolt on a new cat-back exhaust. The hangers are still solid at least.</color>",
                            $"<color={C_FAULT}>Exhaust is blowing</color> at the manifold joint due to rusted studs. <color={C_ASIDE}>It's a common {l.Model} headache. Soak it in penetrating fluid for a day before you try fixing it.</color>",
                            $"The OEM exhaust is <color={C_FAULT}>structurally rust-damaged</color> at the Y-pipe. <color={C_ASIDE}>I wouldn't trust it over speed bumps. Priced to account for a full aftermarket replacement.</color>",
                        })
                        : "Exhaust has some rust on it and will need attention before long — nothing structural but worth knowing.",

                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"The <b>{l.Make}</b> is <color={C_FAULT}>a bit louder</color> than it used to be. <color={C_ASIDE}>Especially when cold. Quiets down once it warms up. Just turns heads at the traffic lights.</color>",
                            $"There is <color={C_FAULT}>a slight blowing noise</color> from somewhere under the <b>{l.Model}</b>. <color={C_ASIDE}>I can barely hear it inside with the radio on. Someone else mentioned it though.</color>",
                            $"Exhaust on the <b>{l.Year}</b> <b>{l.Make}</b> is <color={C_FAULT}>a bit rusty looking</color>. <color={C_ASIDE}>Has not fallen off. I check it every few months and it is still attached.</color>",
                            $"The <b>{l.Model}</b> <color={C_FAULT}>sounds a bit throaty</color> on startup. <color={C_ASIDE}>I actually quite like it. Goes away after a minute. Might be the exhaust, might just be character.</color>",
                            $"Someone pointed out the exhaust on the <b>{l.Make}</b> was <color={C_FAULT}>blowing a bit</color>. <color={C_ASIDE}>I honestly had not noticed. It has not got any worse since they said that, which was about six months ago.</color>",
                            $"There is <color={C_FAULT}>some rust on the back box</color> of the <b>{l.Model}</b>. <color={C_ASIDE}>Looks worse than it probably is I think. Or maybe it looks exactly as bad as it is. Hard to say.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> <color={C_FAULT}>makes more noise at cold start</color> than a newer car would. <color={C_ASIDE}>I put it down to age. These things get a bit grumbly after a while.</color>",
                            $"Exhaust fumes on the <b>{l.Model}</b> are <color={C_FAULT}>slightly visible</color> on cold mornings. <color={C_ASIDE}>Not like clouds of smoke or anything. Just a bit. All old cars do it a bit.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Exhaust on the <b>{l.Make}</b> is <color={C_FAULT}>blowing at the mid-section joint</color>. <color={C_ASIDE}>I know exactly where it is. Just a cat-back replacement job. Never got round to it before I parked it up.</color>",
                            $"The <b>{l.Model}</b> has a <color={C_FAULT}>rusted-through backbox</color>. <color={C_ASIDE}>It is loud from cold start. Quietens down a bit once warm. Replacement is cheap enough, just time I never had.</color>",
                            $"<color={C_FAULT}>Exhaust has been blowing</color> on the <b>{l.Year}</b> <b>{l.Make}</b> for a while. <color={C_ASIDE}>It was one of the reasons I took it off the road — did not want to deal with it failing completely while driving.</color>",
                            $"The downpipe on the <b>{l.Make}</b> has <color={C_FAULT}>a visible crack from corrosion</color>. <color={C_ASIDE}>Standard rot for a <b>{l.Year}</b> car that spent its life in salted roads. Parts are available, just needs doing.</color>",
                            $"<color={C_FAULT}>Rear silencer has rusted through</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>It is noisy. Sat in a cold garage for a year has not helped. The hangers are still solid at least.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>an exhaust leak at the manifold</color>. <color={C_ASIDE}>You can hear it under load. I knew about it before parking it. Add it to the list of jobs for the new owner.</color>",
                            $"Exhaust system on the <b>{l.Model}</b> is <color={C_FAULT}>corroded enough that it needs replacing in sections</color>. <color={C_ASIDE}>Nothing structural, just the kind of rot you get on any car that age that was not garaged properly. It was not garaged properly.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAULT}>Exhaust is blowing at the downpipe flange</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>Rusted studs — soak them well before you attempt it. Standard problem on a <b>{l.Year}</b> car. A stainless cat-back is the sensible long-term fix. I have priced that job into what I am asking.</color>",
                            $"The <b>{l.Model}</b> has a <color={C_FAULT}>rusted-through mid-section</color>. <color={C_ASIDE}>It is loud under acceleration. The manifold and cat are fine — it is from the flex pipe back. Cheap enough to replace with an aftermarket section. Cost already accounted for in the price.</color>",
                            $"<color={C_FAULT}>Rear silencer has corroded through</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Short-trip rot — these cars need a decent run to keep the exhaust dry. This one did not get it often enough. Replacement backbox is a simple bolt-on job. I have costed it into the asking price already.</color>",
                            $"The <b>{l.Make}</b> has an <color={C_FAULT}>exhaust leak at the manifold-to-downpipe junction</color>. <color={C_ASIDE}>Visible crack in the downpipe from thermal cycling and corrosion. You can hear it under load. A new downpipe sorts it. I deal in enough of these to know the part cost — it is reflected in the price.</color>",
                            $"<color={C_FAULT}>Full cat-back replacement needed</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>The system is corroded from the catalyst back. I checked it on ramps before listing. Hangers are solid, cat is fine — it is just the pipework. Stainless aftermarket is the right answer. Already costed in.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has a <color={C_FAULT}>structurally compromised exhaust system</color>. <color={C_ASIDE}>Not going to dress it up. Mid-pipe and backbox are both corroded past the point of repair. OEM replacement priced and factored into what I am asking. Nothing else on the underside concerns me.</color>",
                        })
                        : "It is a bit louder than it used to be, especially when cold, but it quiets down once it warms up.",

                    // Dealer i Scammer nie opisują exhaust — zbyt widoczna wada
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => null,
                    _ => null,
                };

            // ── GLASS DAMAGE ──────────────────────────────────────────────
            if (l.Faults.HasFlag(FaultFlags.GlassDamage))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"There is a <color={C_FAULT}>small chip in the windscreen</color> of the <b>{l.Model}</b>. <color={C_ASIDE}>Not a crack, just a chip — visible in the photos. Probably repairable for not much money.</color>",
                            $"The <b>{l.Make}</b> has a <color={C_FAULT}>stone chip on the windscreen</color>. <color={C_ASIDE}>I kept meaning to get it repaired. It has not spread in the year I have had the car.</color>",
                            $"Windscreen on the <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>a small mark on the passenger side</color>. <color={C_ASIDE}>Barely noticeable from the driver's seat but I wanted to mention it. Shown in photos.</color>",
                            $"There is a <color={C_FAULT}>chip in the glass</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>Insurance might cover a repair — I just never bothered to claim. Worth mentioning either way.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"There is a <color={C_FAULT}>noticeable crack in the windscreen</color>. <color={C_ASIDE}>It's in the swept area, so it'll definitely need a new glass fitted before the next MOT.</color>",
                            $"The windscreen has suffered a <color={C_FAULT}>large stone strike</color> that has spiderwebbed. <color={C_ASIDE}>Too big for a resin repair, you'll need to call Autoglass.</color>",
                            $"<color={C_FAULT}>Windscreen is cracked</color> on the driver's side. <color={C_ASIDE}>I've knocked the cost of a replacement excess off the asking price.</color>",
                            $"There's <color={C_FAULT}>glass damage</color> on the front screen. <color={C_ASIDE}>Structurally fine to drive, but it's an MOT failure waiting to happen.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"The windscreen took a rock on the motorway. <color={C_FAULT}>It's cracked</color>. <color={C_ASIDE}>It's the heated screen with rain sensors, so I know it's a £300 excess. I've dropped the price by £300 exactly.</color>",
                            $"OEM glass is <color={C_FAULT}>chipped and spreading</color>. <color={C_ASIDE}>Don't put cheap aftermarket glass in these {l.Model}s, it messes with the auto-wipers. Factor a proper Pilkington replacement into your budget.</color>",
                            $"<color={C_FAULT}>Crack in the driver's line of sight</color>. <color={C_ASIDE}>It's an instant MOT failure. I haven't claimed it on my insurance because I'm selling it anyway. Sold as is.</color>",
                            $"The windscreen has a <color={C_FAULT}>large unrepairable crack</color>. <color={C_ASIDE}>I've already removed the A-pillar trims so it's ready for the glass guy to cut it out. Priced with the replacement cost in mind.</color>",
                        })
                        : "There is a small chip in the windscreen — not a crack, just a chip, disclosed in the photos.",

                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
                            $"There is a <color={C_FAULT}>chip in the windscreen</color> of the <b>{l.Make}</b>. <color={C_ASIDE}>Has been there since I bought it. Never got any worse.</color>",
                            $"Small <color={C_FAULT}>mark on the glass</color> of the <b>{l.Model}</b>. <color={C_ASIDE}>Passenger side so I barely see it from the driver seat. Easy to forget it is there.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has a <color={C_FAULT}>stone chip on the front screen</color>. <color={C_ASIDE}>I kept meaning to get it repaired with insurance. Never quite got around to the phone call.</color>",
                            $"Windscreen on the <b>{l.Model}</b> has <color={C_FAULT}>a small blemish</color>. <color={C_ASIDE}>Not a crack. Just a chip. You only really notice it at a certain angle in the sun.</color>",
                            $"There is <color={C_FAULT}>a crack on the lower edge</color> of the windscreen of the <b>{l.Make}</b>. <color={C_ASIDE}>Has not spread. I put some clear nail varnish on it. That is what you are supposed to do apparently.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color={C_FAULT}>a chip in the glass</color> from the motorway. <color={C_ASIDE}>A stone came up from a lorry. Could not really be avoided. It is what it is.</color>",
                            $"Front screen on the <b>{l.Make}</b> has <color={C_FAULT}>a tiny mark</color>. <color={C_ASIDE}>The previous owner probably did it. I could not be bothered chasing it through insurance for such a small thing.</color>",
                        })
                        : level == 2
                        ? Pick(_rng, new[]
                        {
                            $"Windscreen on the <b>{l.Make}</b> has <color={C_FAULT}>a crack that has spread since I parked it</color>. <color={C_ASIDE}>Started as a chip, a cold winter sorted the rest. It is in the driver's line of sight so it will need replacing.</color>",
                            $"The <b>{l.Model}</b> has <color={C_FAULT}>a cracked windscreen</color>. <color={C_ASIDE}>It happened while it was standing — temperature, probably. Replacement is the only option at this point.</color>",
                            $"Front screen on the <b>{l.Year}</b> <b>{l.Make}</b> has <color={C_FAULT}>a long crack across the lower section</color>. <color={C_ASIDE}>Started from a stone chip I never got repaired. My fault for leaving it. Priced to cover a new screen.</color>",
                            $"<color={C_FAULT}>Windscreen needs replacing</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>It is cracked and in the swept area so it will not pass a test like that. Factor it into the budget.</color>",
                            $"The <b>{l.Model}</b> has a <color={C_FAULT}>stone chip that has spidered out</color> over the winter. <color={C_ASIDE}>I should have got it filled when it was small. Did not. Now it is a screen job.</color>",
                            $"<color={C_FAULT}>Glass damage on the front screen</color> of the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>It is cracked from one edge inward. Structurally fine to move but it needs sorting before the road.</color>",
                        })
                        : level == 3
                        ? Pick(_rng, new[]
                        {
                            $"<color={C_FAULT}>Windscreen needs replacing</color> on the <b>{l.Make}</b>. <color={C_ASIDE}>Large crack in the driver's line of sight — instant test failure. I know whether it is a heated screen with sensors or a standard fit and I have priced accordingly. No surprises on the glass cost.</color>",
                            $"The <b>{l.Model}</b> has a <color={C_FAULT}>cracked front screen</color>. <color={C_ASIDE}>I checked the spec before pricing it — it is the acoustic laminated version so a replacement is not the cheapest. That cost is already out of the asking price. I do not leave hidden expenses for the buyer.</color>",
                            $"<color={C_FAULT}>Windscreen is cracked beyond repair</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Started as a chip, spread across the screen. I priced a replacement from a glass specialist before listing. That figure is already deducted from what I would otherwise be asking. Straightforward job for any fitter.</color>",
                            $"The <b>{l.Make}</b> needs a <color={C_FAULT}>new windscreen</color>. <color={C_ASIDE}>It is cracked in the swept area, so it is a test failure and a visibility issue. I have already stripped the A-pillar trims to check for corrosion underneath — all clean. Glass cost is in the price, prep work is done.</color>",
                            $"<color={C_FAULT}>Front glass needs replacing</color> on the <b>{l.Model}</b>. <color={C_ASIDE}>It is a significant crack, not a chip. I know the replacement cost to within about twenty pounds. That cost is reflected in the asking price. I do not guess on these things — I check first, then price.</color>",
                            $"The <b>{l.Year}</b> <b>{l.Make}</b> has a <color={C_FAULT}>cracked windscreen that needs replacing before MOT</color>. <color={C_ASIDE}>I noted it before setting the price and deducted the fitting cost accordingly. The crack has not spread further — I checked before listing. What you see in the photos is the current state.</color>",
                        })
                        : "Small mark on the windscreen that I never got around to fixing, barely notice it when driving.",

                    // Dealer i Scammer: glass jest naprawione lub milczą
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => null,
                    _ => null,
                };

            // Brak pasującej usterki
            return null;
        }

        #endregion // FAULT_LINES

        // ════════════════════════════════════════════════════════════════════
        #region BUILD_HONEST
        // ════════════════════════════════════════════════════════════════════
        //  Honest — uczciwy prywatny sprzedawca.
        //
        //  LOGIKA:
        //  L1 Novice    — nie zna się na autach, naiwny, emocjonalny
        //  L2 Experienced — technicznie kompetentny, pragmatyczny
        //  L3 Veteran   — ekspert, zna ceny, dokumentuje wszystko
        //
        //  KOLORY:
        //  C_FAULT  (#ff9944) — problemy i usterki
        //  C_GOOD   (#99ff99) — pochwały stanu i historii
        //  C_ASIDE  (#cccccc) — dygresje, komentarze, tło
        // ════════════════════════════════════════════════════════════════════

        private static string BuildHonest(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ───────────────────────────────────────────────────────────────
            // Otwierające zdanie — ton zależy od kondycji i poziomu sprzedawcy
            string opener;
            if (lv == 1)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"Starts and drives but honestly something <color={C_FAULT}><b>does not feel quite right</b></color> with this <b>{l.Make}</b>, <color={C_ASIDE}>I just cannot put my finger on what.</color>",
                    $"My <b>{l.Model}</b> runs, not brilliantly, but it <color={C_GOOD}>gets from A to B</color> without stopping — <color={C_FAULT}>has some issues I cannot properly describe.</color>",
                    $"Drove this <b>{l.Year}</b> <b>{l.Make}</b> every day for a while then <color={C_FAULT}>things started going wrong</color>, <color={C_ASIDE}>decided to sell before spending money I do not have.</color>",
                    $"Honestly, this <b>{l.Make}</b> is <color={C_FAULT}>showing its age now</color> and I just want a quick sale. <color={C_ASIDE}>I'm not sure what it needs to be perfect again.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Model}</b> <color={C_FAULT}>as it is</color>. It's been <color={C_FAULT}>a bit temperamental</color> lately and <color={C_ASIDE}>I don't have the patience to figure out why.</color>",
                    $"This <b>{l.Color}</b> <b>{l.Make}</b> has <color={C_FAULT}>seen better days</color>, I'll be the first to admit it. <color={C_GOOD}>Might be an easy fix</color> for someone who actually knows about cars.",
                    $"It starts and it goes, but this <b>{l.Model}</b> is definitely <color={C_FAULT}>a bit of a project now</color>. <color={C_ASIDE}>I've just been using it for short trips to the shops.</color>",
                    $"Time for this <b>{l.Year}</b> <b>{l.Make}</b> to go. It's got <color={C_FAULT}>some quirks</color> that I've just learned to live with, <color={C_ASIDE}>but you might want to look at them.</color>",
                    $"Selling my old <b>{l.Model}</b>. It's been sitting for a bit because <color={C_FAULT}>it started acting up</color>. <color={C_ASIDE}>I'm tired of looking at it on the driveway.</color>",
                    $"My <b>{l.Year}</b> <b>{l.Make}</b> is <color={C_FAULT}>a bit rough around the edges</color>. <color={C_ASIDE}>I think it's just tired from all the miles.</color>",
                    $"If you're looking for a showroom car, this <b>{l.Color}</b> <b>{l.Model}</b> isn't it. <color={C_FAULT}>It's just a basic old car with some issues.</color>",
                    $"This <b>{l.Make}</b> has <color={C_FAULT}>a few groans and creaks</color>. <color={C_ASIDE}>I'm selling it cheap because I just want it gone today.</color>",
                    $"Listing my <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_FAULT}>It's not been the same since the winter</color>. <color={C_ASIDE}>I'm not a mechanic, so I'm selling it as I found it.</color>",
                    $"Old <b>{l.Make}</b> for sale. <color={C_FAULT}>It's got a mind of its own sometimes</color>. <color={C_ASIDE}>I've just been driving it very carefully lately.</color>",
                    $"Selling this <b>{l.Color}</b> <b>{l.Model}</b>. It's <color={C_FAULT}>a bit of a 'fixer-upper'</color> as they say. <color={C_ASIDE}>I don't even know where the toolkit is.</color>",
                    $"My <b>{l.Year}</b> <b>{l.Make}</b> is definitely <color={C_FAULT}>a 'budget' option</color>. <color={C_ASIDE}>It gets me there, eventually.</color>",
                    $"Listing my <b>{l.Model}</b>. It's had a long life and <color={C_FAULT}>it's starting to show</color>. <color={C_ASIDE}>I'd keep it but I need something I don't have to worry about.</color>",
                    $"This <b>{l.Make}</b> <b>{l.Model}</b> is what it is. <color={C_FAULT}>A bit noisy, a bit slow</color>, <color={C_ASIDE}>but it's still technically a car.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Make}</b>. <color={C_FAULT}>It's not perfect</color>, but it's honest. <color={C_ASIDE}>I just can't afford to keep guessing what's wrong with it.</color>",
                    $"Had this <b>{l.Model}</b> for years, but it's time to part ways. <color={C_FAULT}>It's developed a few 'noises'</color> that I don't like.",
                    $"Listing my <b>{l.Make}</b>. It's <color={C_FAULT}>a bit grumpy in the mornings</color>. <color={C_ASIDE}>Once it's warm it's okay-ish, I think.</color>",
                })
                : IsMid(l) ? Pick(rng, new[]
                {
                    $"Daily driver for the last couple of years, this <b>{l.Make}</b> has been <color={C_GOOD}>mostly reliable</color> and never left me stranded.",
                    $"Starts every time and <color={C_GOOD}>drives fine</color> as far as I can tell, <color={C_ASIDE}>not a car person so cannot say much more about the <b>{l.Model}</b> than that.</color>",
                    $"Got this <b>{l.Color}</b> <b>{l.Make}</b> a few years ago and <color={C_GOOD}>it has done the job</color>, time to move on now.",
                    $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. It's just a <color={C_GOOD}>normal car</color> that does normal car things. <color={C_ASIDE}>Never really let me down.</color>",
                    $"Here is my <b>{l.Color}</b> <b>{l.Model}</b>. I've used it for work and back, and it's been <color={C_GOOD}>totally fine</color>.",
                    $"Up for sale is my <b>{l.Make}</b>. It's got some age, but it's been <color={C_GOOD}>a good servant</color> to me. <color={C_ASIDE}>I'll be sad to see it go, actually.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color={C_GOOD}>decent little runner</color>. <color={C_ASIDE}>Nothing fancy, but it gets the job done without any fuss.</color>",
                    $"Selling the <b>{l.Make}</b> because I've upgraded. It's <color={C_GOOD}>served me well</color> for three years now.",
                    $"My <b>{l.Color}</b> <b>{l.Model}</b> is ready for a new owner. <color={C_ASIDE}>I've just used it for the school run mostly.</color>",
                    $"Listing my <b>{l.Year}</b> <b>{l.Make}</b>. It's a <color={C_GOOD}>solid car</color> for the price. <color={C_ASIDE}>It's not perfect, but it's been very reliable for me.</color>",
                    $"This <b>{l.Model}</b> has been a <color={C_GOOD}>great first car</color> for me. <color={C_ASIDE}>Easy to drive and doesn't cost much to run.</color>",
                    $"Selling my <b>{l.Make}</b> <b>{l.Model}</b>. It's been through a few MOTs with me and always <color={C_GOOD}>seems to pass eventually</color>.",
                    $"Got this <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Make}</b> from a neighbor. <color={C_GOOD}>It's been a steady car</color> for as long as I've had it.",
                    $"It's a <b>{l.Model}</b>. It drives, it stops, <color={C_GOOD}>the heater works</color>. <color={C_ASIDE}>That's about all I know about cars!</color>",
                    $"Selling my <b>{l.Make}</b>. It's <color={C_ASIDE}>a bit of a plain Jane</color>, but she's <color={C_GOOD}>never left me stranded</color> on the motorway.",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color={C_GOOD}>fair car for fair money</color>. <color={C_ASIDE}>I've kept it clean inside at least!</color>",
                    $"My <b>{l.Color}</b> <b>{l.Make}</b> is up for grabs. <color={C_ASIDE}>I've not had any major dramas with it since I bought it.</color>",
                    $"Listing my <b>{l.Model}</b>. It's been a <color={C_GOOD}>faithful workhorse</color> for me and my family.",
                    $"This <b>{l.Year}</b> <b>{l.Make}</b> is just a <color={C_GOOD}>sensible choice</color>. <color={C_ASIDE}>I'm only selling because I don't need two cars anymore.</color>",
                    $"Decent <b>{l.Model}</b> for sale. <color={C_ASIDE}>I've always found it quite comfy for long trips.</color>",
                    $"Selling my <b>{l.Color}</b> <b>{l.Make}</b>. <color={C_GOOD}>It's not a race car, but it's very dependable.</color>",
                })
                : Pick(rng, new[]  // IsGood
                {
                    $"<color={C_GOOD}>Well looked after</color> <b>{l.Year}</b> <b>{l.Make}</b> as best I could, <color={C_GOOD}>always garaged</color> and kept clean, runs really well.",
                    $"<color={C_GOOD}>Pretty good condition</color> I think — my <b>{l.Model}</b> <color={C_GOOD}>always started first time</color> and never gave me any real trouble.",
                    $"Bought this <b>{l.Color}</b> <b>{l.Make}</b> new and kept it properly, <color={C_ASIDE}>genuinely one careful owner.</color>",
                    $"<color={C_GOOD}>Really proud</color> of my <b>{l.Year}</b> <b>{l.Make}</b>. I've tried to keep it in the <color={C_GOOD}>best shape possible</color>. <color={C_ASIDE}>It's a lovely car.</color>",
                    $"Selling my <b>{l.Color}</b> <b>{l.Model}</b>. I've <color={C_GOOD}>always looked after it</color> and I think it shows. <color={C_GOOD}>It still feels very fresh to drive.</color>",
                    $"This <b>{l.Make}</b> <b>{l.Model}</b> has been my <color={C_GOOD}>pride and joy</color>. <color={C_ASIDE}>I always wash it on Sundays if it's not raining.</color>",
                    $"Up for sale is a <color={C_GOOD}>very clean</color> <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>I've never even smoked in it, kept it really tidy.</color>",
                    $"Listing my <b>{l.Model}</b>. It's in <color={C_GOOD}>great condition</color> for its age. <color={C_ASIDE}>I've always taken it to the same garage for everything.</color>",
                    $"This <b>{l.Color}</b> <b>{l.Make}</b> is a <color={C_GOOD}>fantastic example</color>. <color={C_ASIDE}>I really don't want to sell it, but I need the space.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Model}</b>. It's been <color={C_GOOD}>pampered its whole life</color>, always kept in the garage at night.",
                    $"My <b>{l.Make}</b> <b>{l.Model}</b> is a <color={C_GOOD}>little gem</color>. <color={C_GOOD}>Runs like a clock</color> and looks great in <b>{l.Color}</b>.",
                    $"<color={C_GOOD}>Beautiful</color> <b>{l.Year}</b> <b>{l.Make}</b> for sale. <color={C_ASIDE}>I've spent a lot of time keeping it looking this good.</color>",
                    $"This <b>{l.Model}</b> is probably <color={C_GOOD}>one of the better ones out there</color>. <color={C_ASIDE}>It's never given me a day of worry.</color>",
                    $"Selling my <b>{l.Make}</b>. It's a <color={C_GOOD}>really smooth drive</color>. <color={C_ASIDE}>I think the next owner will be very happy with it.</color>",
                    $"Listing my <b>{l.Color}</b> <b>{l.Year}</b> <b>{l.Model}</b>. I've really enjoyed owning this car, <color={C_GOOD}>it's never missed a beat</color>.",
                    $"This <b>{l.Make}</b> <b>{l.Model}</b> is in <color={C_GOOD}>wonderful shape</color>. <color={C_ASIDE}>I've always been very careful where I park it.</color>",
                    $"My <b>{l.Year}</b> <b>{l.Make}</b> is a <color={C_GOOD}>very honest, clean car</color>. <color={C_ASIDE}>No surprises here, just a well-kept vehicle.</color>",
                    $"Selling this <b>{l.Model}</b>. It's been <color={C_GOOD}>very reliable</color> and still looks almost new in some places!",
                    $"This <b>{l.Color}</b> <b>{l.Make}</b> is a <color={C_GOOD}>real pleasure to drive</color>. <color={C_ASIDE}>I'll probably regret selling it later.</color>",
                    $"Listing my <b>{l.Year}</b> <b>{l.Model}</b>. It's a <color={C_GOOD}>top-notch car</color>, always had whatever it needed.",
                    $"Selling my <b>{l.Make}</b>. <color={C_GOOD}>It's been a very loyal car</color> to me and I've treated it well in return.",
                });
            }
            else if (lv == 2)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"<b>{l.Make} {l.Model}</b>. <color={C_FAULT}>Mechanically tired</color> and requires attention. <color={C_ASIDE}>Selling as a project for someone with the right tools.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_FAULT}>Needs a fair amount of work</color> to be roadworthy again. <color={C_ASIDE}>Technically sound engine, but surrounding components are worn.</color>",
                    $"This <b>{l.Model}</b> is <color={C_FAULT}>showing its age</color>. <color={C_ASIDE}>It starts and drives, but I wouldn't recommend it for long trips until sorted.</color>",
                    $"<b>{l.Color}</b> <b>{l.Make}</b>, <color={C_FAULT}>sold as seen</color>. <color={C_ASIDE}>I've identified several mechanical issues that need addressing. See below for technical details.</color>",
                    $"Fairly worn <b>{l.Model}</b>. <color={C_FAULT}>It's a bit of a fixer-upper</color>. <color={C_ASIDE}>Transmission is okay, but the rest of the drivetrain needs a look-over.</color>",
                    $"<b>{l.Year}</b> <b>{l.Make}</b> for sale. <color={C_FAULT}>Not in the best shape</color> cosmetically or mechanically. <color={C_ASIDE}>Price is set based on the cost of required repairs.</color>",
                    $"This <b>{l.Model}</b> has been <color={C_FAULT}>neglected by the previous owner</color>. <color={C_ASIDE}>I've done a basic diagnosis, it's going to need a few weekends in the garage.</color>",
                    $"<b>{l.Make}</b> in <color={C_FAULT}>rough condition</color>. <color={C_ASIDE}>Solid chassis, but most of the moving parts are at the end of their service life.</color>",
                })
                : IsMid(l) ? Pick(rng, new[]
                {
                    $"<b>{l.Year}</b> <b>{l.Make}</b> in <color={C_GOOD}>reasonable mechanical order</color>. <color={C_ASIDE}>General wear and tear present as expected for the mileage.</color>",
                    $"Average example of a <b>{l.Model}</b>. <color={C_GOOD}>Drives fine</color>, <color={C_ASIDE}>but don't expect a new car. It's a reliable daily runner.</color>",
                    $"Selling my <b>{l.Color}</b> <b>{l.Make}</b>. <color={C_GOOD}>Everything works as it should</color>, <color={C_ASIDE}>though it has a few minor technical quirks that don't affect driveability.</color>",
                    $"<b>{l.Make} {l.Model}</b> with <b>{l.Mileage:N0}</b> on the clock. <color={C_GOOD}>Mechanically sound</color>, <color={C_ASIDE}>regularly serviced, just needs a new owner.</color>",
                    $"Good, <color={C_GOOD}>honest runner</color>. <color={C_ASIDE}>This <b>{l.Year}</b> <b>{l.Model}</b> has been a dependable car for me. No major issues to report.</color>",
                    $"Standard <b>{l.Make}</b>. <color={C_GOOD}>Technically okay</color>, <color={C_ASIDE}>cosmetics are 6/10. It's been maintained well enough to keep it reliable.</color>",
                    $"Solid <b>{l.Model}</b>. <color={C_GOOD}>Engine and gearbox are healthy</color>. <color={C_ASIDE}>Suspension feels okay, just a standard used car for a fair price.</color>",
                    $"<b>{l.Year}</b> <b>{l.Make}</b>. <color={C_GOOD}>Passed last MOT with only minor advisories</color>, <color={C_ASIDE}>most of which have been sorted now.</color>",
                })
                : Pick(rng, new[]
                {
                    $"Very <color={C_GOOD}>well-maintained</color> <b>{l.Make} {l.Model}</b>. <color={C_ASIDE}>I've kept this car in top technical condition during my ownership.</color>",
                    $"High-spec <b>{l.Color}</b> <b>{l.Make}</b>. <color={C_GOOD}>Mechanically excellent</color>. <color={C_ASIDE}>Drives tight, no knocks or leaks. Hard to find them in this state.</color>",
                    $"<b>{l.Year}</b> <b>{l.Model}</b>. <color={C_GOOD}>Full technical inspection recently carried out</color>. <color={C_ASIDE}>No faults found, car is ready for immediate use.</color>",
                    $"Superior example of a <b>{l.Make}</b>. <color={C_GOOD}>Clean engine bay and solid drivetrain</color>. <color={C_ASIDE}>Always used high-quality fluids and parts.</color>",
                    $"This <b>{l.Model}</b> is in <color={C_GOOD}>top-tier mechanical condition</color>. <color={C_ASIDE}>Very quiet engine, smooth shifting, and precise steering.</color>",
                    $"Reliable and <color={C_GOOD}>technically pristine</color> <b>{l.Make}</b>. <color={C_ASIDE}>I don't sell cars that aren't 100% sorted. This one is perfect.</color>",
                    $"Exceptional <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_GOOD}>Everything is within factory tolerances</color>. <color={C_ASIDE}>You won't find many <b>{l.Make}</b>s as clean as this one.</color>",
                    $"Premium <b>{l.Color}</b> <b>{l.Model}</b>. <color={C_GOOD}>Fully serviced and technically verified</color>. <color={C_ASIDE}>It's been an absolute pleasure to own and maintain.</color>",
                });
            }
            else // lv == 3
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"Owned this <b>{l.Make}</b> for eleven years. I know <color={C_FAULT}>every fault on it</color> — <color={C_ASIDE}>all of them are listed below.</color>",
                    $"I've had three of these <b>{l.Model}</b>s. <color={C_FAULT}>This one is the worst of them.</color> <color={C_ASIDE}>Price reflects that exactly.</color>",
                    $"Selling with <color={C_GOOD}>full documentation</color> of everything wrong. <color={C_ASIDE}>No surprises on collection.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color={C_FAULT}>rolling project</color>. <color={C_ASIDE}>I've rebuilt two of these before, but I just don't have the ramp space for a third.</color>",
                    $"Selling my <b>{l.Make}</b>. <color={C_FAULT}>It needs a specialist's touch</color>. <color={C_ASIDE}>If you don't know your way around a torque wrench, close this tab now.</color>",
                    $"I bought this <b>{l.Model}</b> to restore, but <color={C_FAULT}>the list of jobs is too long</color>. <color={C_ASIDE}>I've priced it purely on the value of the salvageable panels and the block.</color>",
                    $"This is a <color={C_FAULT}>hardcore enthusiast's special</color>. <color={C_ASIDE}>It runs, but barely. Read the fault list carefully — I wrote it like a workshop manual.</color>",
                    $"My <b>{l.Year}</b> <b>{l.Make}</b> is <color={C_FAULT}>mechanically compromised</color>. <color={C_ASIDE}>I've owned it for 8 years and I know every creak. Needs an owner with deep pockets or a well-equipped garage.</color>",
                    $"I'm thinning down my collection. <color={C_FAULT}>This is the donor car</color> I never ended up stripping. <color={C_ASIDE}>It deserves to be saved by someone who loves the <b>{l.Model}</b> chassis.</color>",
                    $"Let's be real: this <b>{l.Color}</b> <b>{l.Make}</b> is <color={C_FAULT}>a major undertaking</color>. <color={C_ASIDE}>I've documented every missing bolt and weeping seal. Experts only.</color>",
                    $"Selling my old friend. <color={C_FAULT}>It's throwing codes that I don't have the time to chase</color>. <color={C_ASIDE}>Perfect base for a track build or a very patient restoration.</color>",
                    $"If you're looking at this <b>{l.Year}</b> <b>{l.Model}</b>, you know how these rust and leak. <color={C_FAULT}>This one does both</color>. <color={C_ASIDE}>Honest ad from a <b>{l.Make}</b> club member.</color>",
                })
                : IsMid(l) ? Pick(rng, new[]
                {
                    $"Fair example for the age. I know the <b>{l.Make}</b> <b>{l.Model}</b> well enough to <color={C_GOOD}>price it correctly</color>.",
                    $"Not concours but <color={C_GOOD}>maintained properly</color>. <color={C_ASIDE}>Everything done to this car is documented and receipted.</color>",
                    $"Fifty-plus cars sold and <color={C_GOOD}>every one described exactly as it was</color>. <color={C_ASIDE}>This is no different.</color>",
                    $"I've run this <b>{l.Color}</b> <b>{l.Model}</b> as my daily for 5 years. <color={C_GOOD}>It's mechanically honest</color>, <color={C_ASIDE}>but cosmetically it shows its <b>{l.Mileage:N0}</b> miles.</color>",
                    $"This is my 4th <b>{l.Make}</b>. <color={C_GOOD}>It's a solid mid-tier survivor</color>. <color={C_ASIDE}>I've done the preventative maintenance, so you won't get stranded.</color>",
                    $"A genuinely <color={C_GOOD}>usable modern classic</color>. <color={C_ASIDE}>It's not going to win any car shows, but it fires up on the button every time.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_GOOD}>I know all the factory weak points</color>, <color={C_ASIDE}>and I've addressed the major ones. It's a pragmatic buy.</color>",
                    $"I'm an active member of the <b>{l.Make}</b> owners' club. <color={C_GOOD}>This is a known, respected car</color> <color={C_ASIDE}>that needs a bit of cosmetic love but drives perfectly.</color>",
                    $"A perfectly <color={C_GOOD}>average, honest example</color> of a <b>{l.Model}</b>. <color={C_ASIDE}>I've priced it dynamically against current market data, not sentimentality.</color>",
                    $"I have kept this <b>{l.Make}</b> running like clockwork. <color={C_GOOD}>Mechanically 8/10, bodywork 5/10</color>. <color={C_ASIDE}>I focus on engineering, not polishing.</color>",
                    $"Reluctantly letting this go to make space. <color={C_GOOD}>It's a completely stock, unmolested example</color>. <color={C_ASIDE}>Getting rare to find them like this now.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> sits right in the middle of the market. <color={C_GOOD}>Honest patina</color>, <color={C_ASIDE}>but a bulletproof drivetrain. I've serviced it myself every 6k miles.</color>",
                })
                : Pick(rng, new[]
                {
                    $"Genuine sale from a <color={C_GOOD}>careful private owner</color> — <color={C_ASIDE}>full documented history, every stamp present.</color>",
                    $"One of the <color={C_GOOD}>best {l.Year} examples</color> I have come across — <color={C_ASIDE}>and I have seen a few over the years.</color>",
                    $"I know what a good <b>{l.Make}</b> looks like. <color={C_GOOD}>This is one of them.</color>",
                    $"This is a <color={C_GOOD}>collector-grade <b>{l.Model}</b></color>. <color={C_ASIDE}>I've stored it in a dehumidified bubble and only run it on premium fuel.</color>",
                    $"I've spent <color={C_GOOD}>thousands over-maintaining</color> this <b>{l.Make}</b>. <color={C_ASIDE}>It is mechanically superior to when it left the factory.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is <color={C_GOOD}>absolutely immaculate</color>. <color={C_ASIDE}>I have a 3-inch thick binder of receipts sorted chronologically.</color>",
                    $"An <color={C_GOOD}>unrepeatable opportunity</color> to buy a pristine <b>{l.Color}</b> <b>{l.Model}</b>. <color={C_ASIDE}>I am only selling because I've acquired a rarer spec.</color>",
                    $"I am the foremost enthusiast of this chassis in the region. <color={C_GOOD}>This car is flawless</color>. <color={C_ASIDE}>Every known <b>{l.Make}</b> issue has been preemptively solved.</color>",
                    $"A <color={C_GOOD}>time-capsule example</color>. <color={C_ASIDE}>I've preserved the original factory decals and underbody coatings. Serious connoisseurs only.</color>",
                    $"If you've been looking for the <color={C_GOOD}>perfect <b>{l.Model}</b></color>, your search ends here. <color={C_ASIDE}>I know the market, and you won't find better under <b>{l.Mileage:N0}</b> miles.</color>",
                    $"This is my garage queen. <color={C_GOOD}>Flawless paint, rebuilt internals, OEM+ spec</color>. <color={C_ASIDE}>It pains me to list it, but it needs to be driven.</color>",
                    $"As a seasoned <b>{l.Make}</b> mechanic, I built this for myself. <color={C_GOOD}>No expense was spared</color>. <color={C_ASIDE}>It runs like a Swiss watch.</color>",
                });
            }

            // ── DETAIL 1 ─────────────────────────────────────────────────────────────
            // Konkretne fakty o aucie — zależy od poziomu wiedzy sprzedawcy
            string detail1;
            if (lv == 1)
            {
                detail1 = Pick(rng, new[]
                {
                    $"Has <b>{l.Mileage:N0} miles</b> on it which is quite a lot I know, but <color={C_GOOD}>it has always run</color>.",
                    $"It is a <b>{l.Year}</b> model so there is definitely <color={C_FAULT}>some age on it</color> — <color={C_ASIDE}>shows in places but nothing shocking.</color>",
                    $"I am <color={C_ASIDE}><b>not very mechanically minded</b> so I cannot give you a detailed rundown</color>, but I have tried to be honest about what I know.",
                    $"A friend who knows about cars had a look at the <b>{l.Model}</b> and said it <color={C_FAULT}>needs a few things</color> but <color={C_GOOD}>nothing major</color>.",
                    $"It has <b>{l.Mileage:N0} miles</b> on the clock. <color={C_ASIDE}>I don't know if that's high for a <b>{l.Make}</b>, but most of them were on the motorway.</color>",
                    $"Being a <b>{l.Year}</b>, it's got <color={C_FAULT}>a few marks here and there</color>. <color={C_ASIDE}>I think they call it 'character'!</color>",
                    $"The <b>{l.Model}</b> is showing <b>{l.Mileage:N0} miles</b>. <color={C_ASIDE}>I've not noticed anything major, but then again, I just drive it.</color>",
                    $"I've had the <b>{l.Make}</b> for a while. <color={C_GOOD}>It's never really let me down</color>, but <color={C_ASIDE}>I'm not the type to poke around under the bonnet.</color>",
                    $"It's a <b>{l.Year}</b> model. <color={C_ASIDE}>I bought it because I liked the color, I didn't really look at the engine much.</color>",
                    $"The mileage is <b>{l.Mileage:N0}</b>. <color={C_ASIDE}>I've tried to keep an eye on the oil levels every few months.</color>",
                    $"I'm <color={C_ASIDE}><b>not a car expert</b></color>, but the <b>{l.Model}</b> seems to <color={C_GOOD}>go through the gears okay</color>. <color={C_ASIDE}>No loud bangs yet!</color>",
                    $"A guy at the petrol station said these <b>{l.Make}</b>s are <color={C_GOOD}>built like tanks</color>. <color={C_ASIDE}>I hope he was right!</color>",
                    $"It's done <b>{l.Mileage:N0} miles</b>. <color={C_ASIDE}>I've got some paper scraps in the dash from the last service.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> <color={C_GOOD}>feels okay on the road</color>. <color={C_FAULT}>It's a bit floaty</color>, <color={C_ASIDE}>but I think it's supposed to be like that.</color>",
                    $"I've got the logbook and <b>{l.Mileage:N0} miles</b> on the odometer. <color={C_ASIDE}>Everything seems to be where it should be.</color>",
                    $"Being a <b>{l.Color}</b> car, it stays quite cool in the summer. <color={C_ASIDE}>That's about the extent of my technical knowledge.</color>",
                    $"The <b>{l.Make}</b> is an old friend now. <b>{l.Mileage:N0} miles</b> and counting. <color={C_ASIDE}>I've never had a reason to doubt it.</color>",
                    $"I asked my brother to look at the <b>{l.Model}</b> and he said it <color={C_GOOD}>'sounds healthy'</color>. <color={C_ASIDE}>He works in a bank, but he likes cars.</color>",
                    $"It's a <b>{l.Year}</b>. <color={C_FAULT}>There's a bit of wear on the driver's seat</color>, but <color={C_GOOD}>it's still comfy enough</color>.",
                    $"The <b>{l.Mileage:N0} miles</b> are <color={C_GOOD}>all genuine</color>. <color={C_ASIDE}>I've mostly used it to visit my mum on weekends.</color>",
                    $"I <color={C_ASIDE}>don't really understand all the technical stuff</color>, but the <b>{l.Make}</b> <b>{l.Model}</b> <color={C_GOOD}>starts first time every time</color>.",
                    $"The <b>{l.Year}</b> model has <b>{l.Mileage:N0} miles</b>. <color={C_GOOD}>I've kept the receipts</color> for the new tyres I got last year.",
                    $"It's <b>{l.Color}</b>, which is a nice shade. <b>{l.Mileage:N0}</b> mileage is <color={C_ASIDE}>what I'm told is average for its age.</color>",
                    $"I once saw a <b>{l.Make}</b> with double this <b>{l.Mileage:N0} mileage</b>, <color={C_GOOD}>so it's got plenty of life left!</color>",
                    $"The <b>{l.Model}</b> <color={C_FAULT}>isn't a new car</color>, so don't expect one. <color={C_GOOD}>But for a <b>{l.Year}</b>, it's doing alright.</color>",
                    $"I've got some service history for the <b>{l.Make}</b>. <color={C_ASIDE}>It's mostly stamps from the local 'while-you-wait' place.</color>",
                    $"The <b>{l.Mileage:N0}</b> on the clock is all mine. <color={C_ASIDE}>I've never been a fast driver.</color>",
                    $"I'm selling the <b>{l.Model}</b> exactly as I've been driving it. <color={C_ASIDE}>I haven't even had time to hoover it yet, sorry!</color>",
                });
            }
            else if (lv == 2)
            {
                detail1 = Pick(rng, new[]
                {
                    $"Service history is <color={C_GOOD}>up to date</color> with stamps for all major intervals. <color={C_ASIDE}>Last oil and filter change was 3,000 miles ago.</color>",
                    $"Technically, the car is <color={C_GOOD}>consistent with its mileage</color>. <color={C_ASIDE}>No major surprises in the service book, everything is documented.</color>",
                    $"Most consumables have been <color={C_GOOD}>replaced recently</color>. <color={C_ASIDE}>Alternator and battery are both less than a year old.</color>",
                    $"I have <color={C_GOOD}>checked the compression</color> and it's healthy across all cylinders. <color={C_ASIDE}>The engine is strong for a <b>{l.Year}</b> car.</color>",
                    $"Cooling system has been <color={C_GOOD}>pressure tested</color> and holds fine. <color={C_ASIDE}>No signs of internal leaks or head gasket issues at this time.</color>",
                    $"Gearbox was <color={C_GOOD}>serviced with fresh fluid</color> recently. <color={C_ASIDE}>Shifts are smooth both cold and hot. No whining from the diff.</color>",
                    $"Brake lines and fuel hoses have been <color={C_GOOD}>visually inspected</color> on a ramp. <color={C_ASIDE}>All solid with no signs of excessive corrosion.</color>",
                    $"<b>{l.Mileage:N0}</b> miles on the clock, but <color={C_GOOD}>regular maintenance</color> means it drives better than most lower-mileage examples.",
                    $"All <color={C_GOOD}>major recalls</color> for this <b>{l.Make}</b> model have been addressed. <color={C_ASIDE}>Paperwork is available to prove it.</color>",
                    $"The OBDII scan <color={C_GOOD}>returns no stored codes</color>. <color={C_ASIDE}>All sensors are reading within the expected parameters.</color>",
                });
            }
            else // lv == 3
            {
                detail1 = Pick(rng, new[]
                {
                    $"Mileage <color={C_GOOD}>verified at {l.Mileage:N0}</color> and backed up by <color={C_GOOD}>full service history</color> — <color={C_ASIDE}>every entry stamped and dated.</color>",
                    $"Cambelt history is <color={C_GOOD}>documented</color> — <color={C_ASIDE}>done at the correct interval, receipt is here.</color>",
                    $"I've owned worse and sold them honestly. <color={C_GOOD}>This one I'm genuinely proud of.</color>",
                    $"I exclusively use <color={C_GOOD}>OEM or high-tier aftermarket parts</color>. <color={C_ASIDE}>You won't find a single cheap unbranded sensor on this <b>{l.Model}</b>.</color>",
                    $"At <b>{l.Mileage:N0} miles</b>, the common <b>{l.Make}</b> failure points like the <color={C_GOOD}>water pump and thermostat housing</color> have already been upgraded.",
                    $"I keep a comprehensive log of every fuel fill-up and oil change. <color={C_GOOD}>Compression averages healthy across all cylinders</color>.",
                    $"Oil changed strictly every <color={C_GOOD}>5,000 miles</color> with fully synthetic 5W-40. <color={C_ASIDE}>I don't believe in the manufacturer's 'long-life' 15k intervals.</color>",
                    $"The <b>{l.Model}</b>'s notorious weak point is the cooling system, so I <color={C_GOOD}>overhauled the entire circuit</color> <color={C_ASIDE}>with an aluminum radiator and silicone hoses.</color>",
                    $"I have the original build sheet, window sticker, and <color={C_GOOD}>every MOT certificate</color> since it rolled off the line in <b>{l.Year}</b>.",
                    $"Valves were adjusted at <b>{l.Mileage * 0.9:N0} miles</b>. <color={C_GOOD}>It pulls cleanly to the redline</color> <color={C_ASIDE}>without a hint of hesitation or misfire.</color>",
                    $"Unlike most <b>{l.Make}</b>s out there, this one has a <color={C_GOOD}>completely dry underside</color>. <color={C_ASIDE}>I replaced the rear main seal and sump gasket last year.</color>",
                    $"Transmission fluid was flushed at the correct interval. <color={C_GOOD}>Gears engage with a satisfying mechanical click</color> <color={C_ASIDE}>— synchros are in fantastic shape.</color>",
                });
            }

            // ── DETAIL 2 (optional ~70%) ──────────────────────────────────────────────
            // Dodatkowe informacje — pojawia się 70% czasu
            string detail2;
            if (lv == 1)
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"Tyres on the <b>{l.Make}</b> <color={C_GOOD}>look alright to me</color> but <color={C_ASIDE}>I am no expert — might be worth a proper look.</color>",
                    $"Interior of this <b>{l.Model}</b> is <color={C_FAULT}>a bit lived in</color> but <color={C_GOOD}>clean enough</color>, nothing broken inside.",
                    $"Bought it from a private sale in <b>{l.Location}</b> and <color={C_GOOD}>it has been fine since</color>.",
                    $"<color={C_GOOD}>Never had any warning lights</color> on the dash the whole time I have owned it.",
                    $"<color={C_ASIDE}>Had it looked over by a local garage a while back and they said it was fine.</color>",
                    $"The spare tyre in the <b>{l.Make}</b> <color={C_GOOD}>has never been used</color>, <color={C_ASIDE}>which is a good sign, I guess?</color>",
                    $"I think the air conditioning in the <b>{l.Model}</b> <color={C_FAULT}>needs a top-up</color>, <color={C_ASIDE}>it's not as cold as it used to be.</color>",
                    $"The radio works fine, though <color={C_FAULT}>the volume knob is a bit sticky sometimes</color>.",
                    $"It's been parked on my drive in <b>{l.Location}</b>. <color={C_GOOD}>Safe neighborhood.</color>",
                    $"The back seats of the <b>{l.Make}</b> have <color={C_GOOD}>hardly been sat in</color>, <color={C_ASIDE}>mostly just had my coat on them.</color>",
                    $"I've still got <color={C_GOOD}>both keys</color> for the <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_ASIDE}>That's quite rare for an old car, apparently.</color>",
                    $"The <b>{l.Color}</b> paint has <color={C_FAULT}>a few stone chips</color> on the front. <color={C_ASIDE}>I think that's just from the motorway.</color>",
                    $"I had the <color={C_GOOD}>battery replaced</color> in the <b>{l.Make}</b> last winter when it got really cold.",
                    $"The wipers on the <b>{l.Model}</b> are <color={C_GOOD}>brand new</color>, <color={C_ASIDE}>I changed them myself last week!</color>",
                    $"<color={C_GOOD}>Never been involved in any accidents</color> that I know of. <color={C_ASIDE}>I've certainly never crashed it.</color>",
                    $"The electric windows in the <b>{l.Year}</b> <b>{l.Make}</b> <color={C_GOOD}>all go up and down fine</color>, <color={C_ASIDE}>which is always a relief.</color>",
                    $"I've got the <color={C_GOOD}>original manual</color> for the <b>{l.Model}</b> in the glovebox. <color={C_ASIDE}>I've never read it, though.</color>",
                    $"The <b>{l.Color}</b> <color={C_GOOD}>looks really nice</color> when the sun hits it. <color={C_ASIDE}>It's my favorite part of the car.</color>",
                    $"I've only ever used 'premium' fuel in this <b>{l.Make}</b>, <color={C_ASIDE}>my uncle said it keeps the engine clean.</color>",
                    $"The boot in the <b>{l.Model}</b> is <color={C_GOOD}>actually quite big</color>, <color={C_ASIDE}>fit my golf clubs in there no problem.</color>",
                    $"There's <color={C_FAULT}>a little bit of rust</color> on the wheel arch of the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Nothing a bit of paint wouldn't hide.</color>",
                    $"The interior of the <b>{l.Model}</b> <color={C_GOOD}>doesn't smell of dogs or smoke</color>, <color={C_ASIDE}>I can't stand that.</color>",
                    $"I've got a folder of MOT certificates for the <b>{l.Make}</b> in the house somewhere.",
                    $"The clutch on the <b>{l.Year}</b> <b>{l.Model}</b> <color={C_GOOD}>feels fine to me</color>, <color={C_ASIDE}>not too heavy or anything.</color>",
                    $"I bought some floor mats for the <b>{l.Make}</b> to <color={C_GOOD}>keep the carpets nice</color>. <color={C_ASIDE}>You can have them too.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Model}</b> is <color={C_GOOD}>quite easy to park</color>, <color={C_ASIDE}>the mirrors are nice and big.</color>",
                    $"It's a <color={C_GOOD}>non-smoker car</color>, <color={C_ASIDE}>and I don't have any pets, so it's pretty clean inside.</color>",
                    $"I've always <color={C_GOOD}>warmed the <b>{l.Make}</b> up</color> for a minute before driving off on cold days.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has a <color={C_GOOD}>full-size spare wheel</color>, <color={C_ASIDE}>not one of those tiny 'space saver' ones.</color>",
                    $"I had the tracking done on the <b>{l.Make}</b> recently because it was <color={C_FAULT}>pulling to the left a bit</color>.",
                    $"The <b>{l.Model}</b> has a <color={C_GOOD}>decent sound system</color>, <color={C_ASIDE}>plenty of bass for when you're stuck in traffic.</color>",
                    $"I've <color={C_GOOD}>never taken this <b>{l.Year}</b> <b>{l.Make}</b> through a car wash</color>, <color={C_ASIDE}>I always do it by hand with a sponge.</color>",
                    $"The <b>{l.Color}</b> paint is <color={C_GOOD}>all original</color> as far as I can tell. <color={C_ASIDE}>No weird patches.</color>",
                    $"It's been a <color={C_GOOD}>really lucky car</color> for me, <color={C_ASIDE}>hope it brings the same luck to you!</color>",
                    $"The <b>{l.Model}</b> <color={C_GOOD}>comes with a half tank of petrol</color>, <color={C_ASIDE}>so you can at least get home!</color>",
                }, 0.70);
            }
            else if (lv == 2)
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"The <b>{l.Color}</b> bodywork is <color={C_GOOD}>solid</color>, <color={C_ASIDE}>just standard age-related marks. No structural rust to speak of.</color>",
                    $"Suspension is <color={C_GOOD}>firm and responsive</color>. <color={C_ASIDE}>I've replaced the front bushings recently so it doesn't wander on the road.</color>",
                    $"Tires are all <color={C_GOOD}>matching brands</color> with 4-5mm of tread left. <color={C_ASIDE}>Brake pads were changed about 5,000 miles ago.</color>",
                    $"Interior is <color={C_GOOD}>clean and functional</color>. <color={C_ASIDE}>No major wear on the driver's seat bolster or steering wheel.</color>",
                    $"Electrics are <color={C_GOOD}>fully operational</color>. <color={C_ASIDE}>Air conditioning blows cold and all windows move freely.</color>",
                    $"The clutch <color={C_GOOD}>has a good bite point</color>. <color={C_ASIDE}>Doesn't slip even under heavy load. Flywheel is quiet on idle.</color>",
                    $"Wheel alignment was <color={C_GOOD}>done last month</color>. <color={C_ASIDE}>It drives straight as an arrow. No vibration through the steering wheel at speed.</color>",
                    $"Exhaust is <color={C_GOOD}>gas-tight</color> and the catalytic converter is performing correctly. <color={C_ASIDE}>Passed emissions testing with no issues.</color>",
                    $"Underside is <color={C_GOOD}>surprisingly clean</color> for a car from <b>{l.Year}</b>. <color={C_ASIDE}>I've kept it clear of salt and road grime as much as possible.</color>",
                    $"Brake rotors have <color={C_GOOD}>plenty of life left</color>. <color={C_ASIDE}>No warping or juddering during heavy braking.</color>",
                }, 0.70);
            }
            else // lv == 3
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"The <b>{l.Color}</b> paintwork is <color={C_GOOD}>generally straight</color>, <color={C_ASIDE}>just your standard stone chips on the front bumper and minor swirl marks.</color>",
                    $"Interior is intact. <color={C_ASIDE}>No ripped bolsters on the seats, and all the switchgear works exactly as intended.</color>",
                    $"<color={C_GOOD}>Tyres have plenty of tread left</color>, at least 5mm all around. <color={C_ASIDE}>Brake discs have a slight lip but nothing to worry about yet.</color>",
                    $"It's always been kept reasonably clean. <color={C_ASIDE}>No warning lights on the dash (other than the standard ignition sequence).</color>",
                    $"The clutch on this <b>{l.Make}</b> <color={C_GOOD}>bites exactly where it should</color>. <color={C_ASIDE}>Gearbox is smooth, no crunching into second or third.</color>",
                    $"<color={C_ASIDE}>I've got the V5 logbook in my name and two sets of keys.</color>",
                    $"Tyres are all <color={C_GOOD}>matching brand with plenty of tread</color>. <color={C_ASIDE}>Not the kind of thing you usually see on a {l.Year}.</color>",
                    $"Paint is in <color={C_GOOD}>remarkable condition for the age</color> — <color={C_ASIDE}>no fading, no chips worth mentioning.</color>",
                    $"Interior shows <color={C_GOOD}>barely any wear</color> — <color={C_ASIDE}>this car has been treated properly its entire life.</color>",
                    $"I've treated the cavities with <color={C_GOOD}>Dinitrol rust inhibitor</color>. <color={C_ASIDE}>If you know these chassis, you know the rear arches rot if you don't protect them.</color>",
                    $"The <b>{l.Color}</b> paint has been <color={C_GOOD}>two-stage corrected and ceramic coated</color>. <color={C_ASIDE}>Water beads off it instantly. Washed only using the two-bucket method.</color>",
                    $"It sits on a set of <color={C_GOOD}>premium Michelin Pilot Sports</color>. <color={C_ASIDE}>I never skimp on rubber or brakes, it's the most important part of the car.</color>",
                    $"The factory <b>{l.Model}</b> headlights usually fog up, but <color={C_GOOD}>I polished and UV-sealed these</color> <color={C_ASIDE}>so they are crystal clear and project perfectly.</color>",
                    $"I sourced the rare OEM floor mats and the correct <color={C_GOOD}>period-accurate Becker stereo</color>. <color={C_ASIDE}>It's all about preserving the original aesthetic for me.</color>",
                    $"Suspension geometry was set up on a Hunter alignment rig last month. <color={C_GOOD}>It tracks perfectly straight</color> <color={C_ASIDE}>with zero uneven tire wear.</color>",
                }, 0.70);
            }

            // ── FAULT ─────────────────────────────────────────────────────────────────
            // L1: 25% szansa że fault zostanie pominięty (nie wiedzą o usterce)
            string fault = DominantFaultLine(l, SellerArchetype.Honest, lv, rng);
            if (lv == 1 && fault != null && rng.NextDouble() < 0.25)
                fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────────────
            // Zamknięcie ogłoszenia — inne gdy jest usterka, inne gdy auto OK
            string closer;
            if (lv == 1)
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    $"<color={C_ASIDE}>Priced low to account for that</color> — someone who knows what they are doing with a <b>{l.Make}</b> will get <color={C_GOOD}>good value here</color>.",
                    $"That is why the price for the <b>{l.Model}</b> is what it is — <color={C_ASIDE}>not trying to hide anything.</color>",
                    $"I would rather be <color={C_GOOD}>upfront and price it honestly</color> than waste everyone's time.",
                    $"I've <color={C_FAULT}>lowered the price because of the noise</color>, <color={C_ASIDE}>I think it's fair for someone who can fix it.</color>",
                    $"If you're handy with a wrench, this <b>{l.Make}</b> is a <color={C_GOOD}>bargain</color>. <color={C_ASIDE}>I just can't deal with it anymore.</color>",
                    $"<color={C_GOOD}>Priced for a quick sale</color> due to the issues mentioned. <color={C_ASIDE}>No point in me lying about it.</color>",
                    $"It's a <b>{l.Model}</b> with <color={C_FAULT}>a bit of a headache</color>, hence the low price. <color={C_ASIDE}>Any inspection is welcome.</color>",
                    $"I'd rather be <b>honest about the faults</b> and sell it cheap than have someone complain later.",
                    $"Selling <color={C_FAULT}>as it is</color>. <color={C_ASIDE}>The price reflects the fact it needs a little bit of love.</color>",
                    $"I've been as <color={C_GOOD}>clear as I can</color> about the problems with the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>Open to sensible offers.</color>",
                    $"This <b>{l.Model}</b> <color={C_FAULT}>needs a bit of work</color>, but the price is right. <color={C_ASIDE}>I just want it off my drive now.</color>",
                    $"<color={C_ASIDE}>I'm not a mechanic, so I've priced it low enough</color> that you can afford to take it to one.",
                    $"Listing it honestly so we don't waste each other's time. <color={C_ASIDE}>The price is firm-ish.</color>",
                    $"Take it as it stands. It's a <color={C_FAULT}>cheap <b>{l.Make}</b> for a reason</color>, but <color={C_GOOD}>it still has potential</color>.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> is <color={C_GOOD}>priced to go</color>. <color={C_ASIDE}>I've accounted for the fault in the asking price.</color>",
                    $"If you can ignore the issue, it's a <color={C_GOOD}>great car</color>. <color={C_ASIDE}>Or just fix it and have a bargain!</color>",
                    $"I'm <color={C_GOOD}><b>not hiding anything</b></color>, that's why the <b>{l.Make}</b> is so cheap. <color={C_ASIDE}>Come and see for yourself.</color>",
                    $"It's a bit of a gamble, maybe, but for this price the <b>{l.Model}</b> is worth a look.",
                    $"Price is low because I don't want any hassle. <color={C_ASIDE}>You know what you're buying.</color>",
                    $"Selling for <color={C_FAULT}>'spares or repairs'</color> really, <color={C_ASIDE}>though it does still drive if you're brave!</color>",
                    $"I've told you everything I know. <color={C_ASIDE}>Price is negotiable, but be fair.</color>",
                }
                : new[]
                {
                    $"Priced to <color={C_GOOD}>reflect what it is</color> — not asking the earth.",
                    $"Selling the <b>{l.Make}</b> because I got something newer, <color={C_GOOD}>no longer needed</color>.",
                    $"Come and have a look at the <b>{l.Model}</b> if you are interested, <color={C_ASIDE}>no pressure at all.</color>",
                    $"Just want it gone to a <color={C_GOOD}>good home</color>, priced accordingly.",
                    $"It's a <color={C_GOOD}>solid <b>{l.Make}</b> for the money</color>. <color={C_ASIDE}>First person to see it will probably take it.</color>",
                    $"Just a <color={C_GOOD}><b>genuine, honest car</b></color> looking for a new home. <color={C_ASIDE}>No games here.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> is <color={C_GOOD}>ready to drive away today</color>. <color={C_ASIDE}>Hope you like it!</color>",
                    $"I've priced it to sell quickly as I've already got my new car. <color={C_ASIDE}>No silly offers please.</color>",
                    $"Come and have a look, <color={C_ASIDE}>bring a friend who knows about <b>{l.Make}</b>s if you like!</color>",
                    $"It's been a <color={C_GOOD}>great car for me</color> and I'm sure it will be for you too. <color={C_ASIDE}>Good luck!</color>",
                    $"Selling the <b>{l.Model}</b> at what I think is a <color={C_GOOD}>fair price</color>. <color={C_ASIDE}>Not looking to make a fortune.</color>",
                    $"I'm available most evenings if you want to come and test drive the <b>{l.Make}</b>.",
                    $"A very <color={C_GOOD}><b>straightforward sale</b></color> for a straightforward <b>{l.Year}</b> <b>{l.Model}</b>.",
                    $"I've tried to be as accurate as I can. It's just a <color={C_GOOD}>good, reliable vehicle</color>.",
                    $"No rush to sell, but I'd like the <b>{l.Make}</b> to go to someone who will use it.",
                    $"Feel free to ask any questions, <color={C_ASIDE}>though I might have to ask my brother for the technical bits!</color>",
                    $"The <b>{l.Model}</b> is a <color={C_GOOD}>good buy</color> for anyone wanting a simple car.",
                    $"<color={C_GOOD}>Priced fairly</color> according to the market. <color={C_ASIDE}>It's a lot of car for the money.</color>",
                    $"I've been the only driver for years, so I <color={C_GOOD}>know this <b>{l.Year}</b> <b>{l.Make}</b> inside out</color>.",
                    $"Just looking for a <color={C_GOOD}><b>hassle-free sale</b></color>. <color={C_ASIDE}>Bank transfer or cash is fine.</color>",
                    $"It's a <color={C_GOOD}>lovely <b>{l.Color}</b> car</color> and it's never let me down. <color={C_ASIDE}>You won't be disappointed.</color>",
                    $"Selling my <b>{l.Model}</b> as I'm moving abroad. <color={C_ASIDE}>Need it gone by Friday if possible!</color>",
                    $"This <b>{l.Make}</b> is a <color={C_GOOD}>real bargain</color> for someone. <color={C_ASIDE}>I've kept it as clean as I could.</color>",
                    $"I'm a <b>private seller</b>, so no warranties, but you're welcome to spend as much time looking at it as you need.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> is a <color={C_GOOD}>great runner</color>. <color={C_ASIDE}>Very cheap on insurance too, I found.</color>",
                    $"Come and see it in <b>{l.Location}</b>. <color={C_ASIDE}>I'll even put the kettle on!</color>",
                    $"I'm selling it for exactly what I'd want to pay for it. <color={C_GOOD}>Fair's fair</color>.",
                    $"It's been a part of the family, this <b>{l.Make}</b>. <color={C_GOOD}>Treat her well!</color>",
                });
            }
            else if (lv == 2)
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    $"Price is <color={C_FAULT}>adjusted for the work required</color>. <color={C_ASIDE}>I've been realistic about the technical state, so no lowball offers please.</color>",
                    $"If you're capable of <color={C_GOOD}>doing the repairs yourself</color>, this is a bargain. <color={C_ASIDE}>I simply don't have the time to fix it myself.</color>",
                    $"A <color={C_FAULT}>fair price</color> for a <b>{l.Model}</b> that needs a bit of technical TLC. <color={C_ASIDE}>Viewings welcome for those who know what they're looking at.</color>",
                    $"The issues are <color={C_FAULT}>factored into the asking price</color>. <color={C_ASIDE}>I know the market value for a sorted one, and this isn't it yet.</color>",
                    $"Selling as is. <color={C_ASIDE}>I've been honest about the faults to avoid wasting anyone's time. Grab a trailer and a toolbox.</color>",
                    $"I've listed the <color={C_FAULT}>technical defects</color> clearly. <color={C_ASIDE}>The car is priced based on professional labor rates for these fixes.</color>",
                }
                : new[]
                {
                    $"This is a <color={C_GOOD}>straightforward sale</color> of a solid car. <color={C_ASIDE}>I'm not in a rush, but I won't entertain time-wasters.</color>",
                    $"Technically <color={C_GOOD}>one of the better examples</color> available. <color={C_ASIDE}>Price is firm based on condition and history.</color>",
                    $"Ready to be <color={C_GOOD}>driven away today</color>. <color={C_ASIDE}>Full documentation and both keys present. First serious buyer will take it.</color>",
                    $"If you want an <color={C_GOOD}>honest car with no hidden issues</color>, this is it. <color={C_ASIDE}>Test drives welcome for serious buyers with insurance.</color>",
                    $"Standard transaction. <color={C_ASIDE}>The car is as described. I believe in fair pricing for a well-maintained vehicle.</color>",
                    $"Reliable <b>{l.Make}</b>. <color={C_ASIDE}>Come and see it for yourself. I'm happy to put it on a jack if you want to see the underside.</color>",
                });
            }
            else // lv == 3
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    $"The asking price reflects the <color={C_GOOD}>honest condition</color>. <color={C_ASIDE}>Happy to accommodate any mechanical inspection — I have nothing to hide.</color>",
                    $"I know the current market for a <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_ASIDE}>The fault is priced in. Serious offers only, in person.</color>",
                    $"I've been selling cars long enough to know that <color={C_GOOD}>honesty saves everyone time</color>. <color={C_ASIDE}>The issue is disclosed, the price reflects it — nothing more to say.</color>",
                    $"<color={C_GOOD}>No games, no hidden issues</color> beyond what is listed. <color={C_ASIDE}>First person to view and verify will likely buy it.</color>",
                    $"Priced at what it actually is, <color={C_FAULT}>not what it could be</color>. <color={C_ASIDE}>Receipts and records available on viewing.</color>",
                    $"As a seasoned <b>{l.Make}</b> owner, I've diagnosed the fault accurately. <color={C_ASIDE}>You won't have to guess what's wrong. I've discounted the asking price by the exact cost of OEM parts to fix it.</color>",
                    $"I don't have the bandwidth to tackle this final repair. <color={C_ASIDE}>My asking price accounts for the labor hours a reputable specialist would charge you. No lowballing.</color>",
                    $"I respect your time, so please respect mine. <color={C_ASIDE}>The flaw is clearly stated. If you know these cars, you know it's a straightforward fix for the right mechanic.</color>",
                    $"This is a strictly no-nonsense sale. <color={C_ASIDE}>The car needs the work I've detailed. Come with a code reader, put it on ramps, I welcome any expert inspection.</color>",
                    $"I'm an enthusiast, not a charity. <color={C_ASIDE}>The flaw is factored in down to the penny. Don't message me offering half; I know exactly what the shell alone is worth.</color>",
                    $"You are buying a known quantity. <color={C_ASIDE}>I've saved you the diagnostic fees. Grab the required parts, dedicate a weekend, and you'll have a sorted <b>{l.Model}</b>.</color>",
                    $"Every <b>{l.Year}</b> <b>{l.Make}</b> has issues. <color={C_ASIDE}>I'm just the only seller honest enough to list them. Price is firm, based on current forum valuation guides.</color>",
                }
                : new[]
                {
                    $"The asking price reflects the <color={C_GOOD}>honest condition</color>. <color={C_ASIDE}>Happy to accommodate any mechanical inspection.</color>",
                    $"I know the current market for a <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_ASIDE}>My price is fair — serious offers only in person.</color>",
                    $"<color={C_GOOD}>No games, no hidden issues.</color> <color={C_ASIDE}>First person to view and test drive will likely buy it.</color>",
                    $"Viewing is highly recommended. <color={C_ASIDE}>I'm an enthusiast, not a dealer — expect a straightforward, honest transaction.</color>",
                    $"I've been as descriptive as possible. <color={C_ASIDE}>Feel free to message me, but please don't ask 'what's your lowest price'.</color>",
                    $"This is a purist's car. <color={C_ASIDE}>I will gladly talk you through the entire maintenance log over a coffee, but the price is non-negotiable.</color>",
                    $"I've nurtured this <b>{l.Make}</b> for years. <color={C_ASIDE}>I'm in no rush to sell and will only let it go to a fellow enthusiast who will continue to preserve it.</color>",
                    $"You could buy a cheaper <b>{l.Model}</b>, <color={C_ASIDE}>but you'll spend double making it as good as this one. Buy on condition, not just the sticker price.</color>",
                    $"I have a reputation in the local <b>{l.Make}</b> community. <color={C_ASIDE}>I do not sell junk. Bring your micrometer and paint depth gauge, you will not find a fault.</color>",
                    $"An absolute turn-key classic. <color={C_ASIDE}>I've done all the heavy lifting so you don't have to. Pay the asking price, drive it home, enjoy.</color>",
                    $"I expect the buyer to know what they are looking at. <color={C_ASIDE}>I won't entertain time-wasters or test pilots. Genuine inquiries only.</color>",
                    $"This is the sort of car you buy and hold onto forever. <color={C_ASIDE}>I'll be genuinely sad to see it roll down the driveway. Next owner is getting a gem.</color>",
                });
            }

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }

        #endregion // BUILD_HONEST

        // ════════════════════════════════════════════════════════════════════
        #region BUILD_WRECKER
        // ════════════════════════════════════════════════════════════════════
        //  Wrecker — właściciel zaniedbujący auto lub hoarder z kolekcją.
        //
        //  LOGIKA:
        //  L1 Casual   — nie wie co się dzieje, minimalny serwis, ignoruje problemy
        //  L2 Busy     — wie że coś jest nie tak, ale nie miał czasu/chęci
        //  L3 Hoarder  — kolekcjoner z flotą, sprzedaje "nadwyżkę", prowi wyceny
        //
        //  KOLORY:
        //  C_FAULT  (#ff9944) — usterki (podane mimochodem, nie z troską)
        //  C_GOOD   (#99ff99) — rzadko, tylko gdy naprawdę dobrze
        //  C_ASIDE  (#cccccc) — komentarze w stylu "no co zrobić"
        // ════════════════════════════════════════════════════════════════════

        private static string BuildWrecker(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ───────────────────────────────────────────────────────────────
            string opener;
            if (lv == 1)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_FAULT}>It has seen better days</color> if I am honest. <color={C_ASIDE}>But it still drives and that is more than I expected at this point.</color>",
                    $"This <b>{l.Model}</b> has been on my drive for a while now. <color={C_FAULT}>Not in perfect shape.</color> <color={C_ASIDE}>I kept meaning to sort it out and then other things came up.</color>",
                    $"Old <b>{l.Year}</b> <b>{l.Make}</b> for sale. <color={C_FAULT}>A bit rough</color>, not going to lie. <color={C_ASIDE}>Does start though. Most of the time.</color>",
                    $"Selling the <b>{l.Model}</b> as-is. <color={C_FAULT}>It is what it is</color> at this point. <color={C_ASIDE}>I drove it for years without big problems but lately things are adding up.</color>",
                    $"Time to let this <b>{l.Color}</b> <b>{l.Make}</b> go. <color={C_FAULT}>Not going to pretend it is in great shape.</color> <color={C_ASIDE}>Someone will get more out of it than it sitting on my drive doing nothing.</color>",
                    $"<b>{l.Year}</b> <b>{l.Model}</b> for sale. <color={C_FAULT}>Been sat for a bit</color>, started fine last week. <color={C_ASIDE}>Before that it was my daily but things have been a bit hectic.</color>",
                    $"This <b>{l.Make}</b> needs some love. <color={C_FAULT}>Not a lot, probably</color>, but some. <color={C_ASIDE}>I am not the right person to give it that love. Maybe you are.</color>",
                    $"Selling my <b>{l.Color}</b> <b>{l.Model}</b>. <color={C_FAULT}>It has a few things wrong with it</color> that I never properly looked into. <color={C_ASIDE}>Priced with that in mind.</color>",
                })
                : IsMid(l) ? Pick(rng, new[]
                {
                    $"<color={C_GOOD}>Good runner</color> as far as I know. Selling the <b>{l.Year}</b> <b>{l.Make}</b> as-is and <color={C_FAULT}>the price reflects that</color>.",
                    $"<color={C_FAULT}>Been sitting on the drive</color> more than being driven lately. <b>{l.Model}</b> <color={C_GOOD}>starts fine though</color>.",
                    $"<color={C_GOOD}>Starts every time</color> I have tried it. <color={C_ASIDE}>Just needs someone who will actually use the</color> <b>{l.Make}</b>.",
                    $"Used the <b>{l.Color}</b> <b>{l.Year}</b> <b>{l.Model}</b> regularly until a few months ago. <color={C_FAULT}>Has been parked since</color>, <color={C_ASIDE}>nothing dramatic happened.</color>",
                    $"Selling the <b>{l.Make}</b> because I got something else. <color={C_GOOD}>It has been a decent car</color>, <color={C_FAULT}>just needs a bit of a clean.</color>",
                    $"This <b>{l.Model}</b> has just been sitting. <color={C_GOOD}>Runs and drives</color>, <color={C_FAULT}>no major problems that I know of</color>.",
                    $"<b>{l.Year}</b> <b>{l.Make}</b> for sale. <color={C_ASIDE}>Not much to say.</color> <color={C_GOOD}>It goes and it stops</color> and <color={C_FAULT}>the heater is a bit temperamental</color>.",
                    $"I drove this <b>{l.Color}</b> <b>{l.Model}</b> every day for two years. <color={C_GOOD}>Did the job</color>, <color={C_FAULT}>but it is definitely not a show car</color>.",
                })
                : Pick(rng, new[]
                {
                    $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. It has been a <color={C_GOOD}>solid car</color> for me. No drama, always started.",
                    $"This <b>{l.Model}</b> has been <color={C_GOOD}>dead reliable</color> the whole time I have owned it. Only selling because I do not need it.",
                    $"<color={C_GOOD}>Good car this</color>. The <b>{l.Year}</b> <b>{l.Make}</b> has never let me down. Just sitting unused now.",
                    $"The <b>{l.Color}</b> <b>{l.Model}</b> has been <color={C_GOOD}>a really easy car to live with</color>. Never costs me anything, never breaks down.",
                    $"Owned the <b>{l.Make}</b> for a few years now. <color={C_GOOD}>Nothing has ever gone wrong with it</color>. Getting a new one so this needs to go.",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is one of those cars that just <color={C_GOOD}>never causes any bother</color>. Happy to sell to someone who will use it.",
                });
            }
            else if (lv == 2)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"Selling the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_FAULT}>It is not in good shape</color> and I am not going to pretend otherwise. <color={C_ASIDE}>Has been sitting since the problems started adding up.</color>",
                    $"This <b>{l.Model}</b> needs work. <color={C_FAULT}>More than I am willing to put into it at this point.</color> <color={C_ASIDE}>Parked it up when it stopped making sense to keep driving it.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Make}</b> has been off the road for a while. <color={C_FAULT}>There are reasons for that</color> and they are listed below. <color={C_ASIDE}>Not a runner in the current state, be realistic.</color>",
                    $"I know this <b>{l.Year}</b> <b>{l.Model}</b> well enough to know <color={C_FAULT}>it needs proper attention before it goes back on the road</color>. <color={C_ASIDE}>I am not the one to give it that. Priced accordingly.</color>",
                    $"The <b>{l.Make}</b> has been sitting since I pulled it off the road. <color={C_FAULT}>It was not getting better on its own.</color> <color={C_ASIDE}>Someone with the right tools and time will get more out of this than I did.</color>",
                    $"Rough example of a <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_FAULT}>I am aware of that.</color> <color={C_ASIDE}>Priced to match. The issues I know about are in the listing.</color>",
                    $"This <b>{l.Color}</b> <b>{l.Make}</b> is a <color={C_FAULT}>project car at this point</color>. <color={C_ASIDE}>It was a daily once. That was a while ago. Things have moved on.</color>",
                    $"Not going to dress this up. The <b>{l.Year}</b> <b>{l.Model}</b> is <color={C_FAULT}>in poor condition</color>. <color={C_ASIDE}>I stopped using it when the repair costs stopped making sense relative to the value.</color>",
                })
                : IsMid(l) ? Pick(rng, new[]
                {
                    $"Selling the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_FAULT}>It has been sitting for a while</color> now and <color={C_ASIDE}>I need the space more than I need the car.</color>",
                    $"This <b>{l.Model}</b> has been in the garage for <color={C_FAULT}>the best part of two years</color>. It was my <color={C_GOOD}>daily before that</color>. <color={C_ASIDE}>Circumstances changed.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Make}</b> has been <color={C_FAULT}>off the road since the last issue came up</color>. <color={C_ASIDE}>Did not feel like putting money into it.</color>",
                    $"Parked the <b>{l.Year}</b> <b>{l.Model}</b> up when I got something newer. <color={C_GOOD}>Always said I would get back to it</color>, but <color={C_FAULT}>the gap between saying and doing got too wide</color>.",
                    $"This <b>{l.Make}</b> has been <color={C_FAULT}>sitting on the drive under a cover</color> for long enough. Time for it to go somewhere it will <color={C_GOOD}>actually be used or fixed properly</color>.",
                    $"Selling the <b>{l.Model}</b> that has been <color={C_FAULT}>taking up space in the unit</color>. <color={C_GOOD}>Good bones</color>, <color={C_FAULT}>just needs attention I do not have time for</color>.",
                    $"The <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Make}</b> has been <color={C_FAULT}>standing since I replaced it</color>. <color={C_ASIDE}>Kept it thinking I would fix and flip it. Life had other plans.</color>",
                    $"This <b>{l.Model}</b> was my <color={C_GOOD}>backup car</color> until <color={C_FAULT}>the backup became the problem</color>. <color={C_ASIDE}>Has been on the driveway since.</color>",
                })
                : Pick(rng, new[]
                {
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has been <color={C_ASIDE}>standing but it is in decent shape for what it is. Selling because I have too many and this one draws the short straw.</color>",
                    $"This <b>{l.Model}</b> is <color={C_GOOD}>one of the better ones I have had sitting around</color>. <color={C_ASIDE}>Mechanically it was solid before storage. Should not take much to bring it back.</color>",
                    $"Selling the <b>{l.Color}</b> <b>{l.Make}</b>. <color={C_ASIDE}>It has been stored rather than driven but the condition is <color={C_GOOD}>reasonable for the age and mileage</color>. No horror stories.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> was <color={C_GOOD}>running well when I took it off the road</color>. <color={C_ASIDE}>Parked it because I got something else, not because anything went wrong.</color>",
                    $"This <b>{l.Make}</b> is <color={C_GOOD}>in better shape than most sitting cars</color>. <color={C_ASIDE}>I stored it properly and it was in good condition when it went away. The mileage and age are honest.</color>",
                    $"Decent <b>{l.Year}</b> <b>{l.Model}</b> that has <color={C_ASIDE}>been off the road by choice rather than necessity. <color={C_GOOD}>Nothing terminal happened to it</color>. Just surplus to requirements.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Make}</b> was <color={C_GOOD}>a reliable car</color> before I parked it up. <color={C_ASIDE}>Selling it on because I never got around to using it as the spare I intended it to be.</color>",
                });
            }
            else // lv == 3
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> is a <color={C_FAULT}>project</color>. <color={C_ASIDE}>I am not going to oversell it. The faults are listed, the price reflects them. If you know these cars you will see the value. If you do not, this is not the right buy for you.</color>",
                    $"Selling a <color={C_FAULT}>non-runner</color> <b>{l.Model}</b>. <color={C_ASIDE}>I deal in these regularly. I price them properly and I describe them honestly. What is wrong with this one is listed below in plain language.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Make}</b> needs work. <color={C_FAULT}>Significant work.</color> <color={C_ASIDE}>I know what it needs, I know what that costs, and I have priced it accordingly. No inflation, no games. I have a reputation to maintain.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is being sold as the <color={C_FAULT}>project car it is</color>. <color={C_ASIDE}>I deal in distressed vehicles. I set prices based on actual repair costs, not hope. Read the fault list and trust the maths.</color>",
                    $"<color={C_FAULT}>Rough condition</color> on this <b>{l.Make}</b>. <color={C_ASIDE}>I inspected it before pricing it. Always do. The issues are documented below and the asking price already accounts for sorting every one of them at an independent specialist rate.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Model}</b> is <color={C_FAULT}>in poor mechanical condition</color>. <color={C_ASIDE}>I am telling you that because I price these cars to sell, not to sit. Serious buyers with workshop access will get genuine value here.</color>",
                })
                : IsMid(l) ? Pick(rng, new[]
                {
                    $"One of several I am moving on. The <b>{l.Year}</b> <b>{l.Make}</b> is a <color={C_GOOD}>fair example</color> — <color={C_ASIDE}>not the best in the collection, not the worst.</color> <color={C_FAULT}>Priced to clear space</color> rather than maximise return.",
                    $"Selling the <b>{l.Model}</b> to make room. <color={C_ASIDE}>I know this chassis well.</color> It is in <color={C_GOOD}>reasonable condition</color> for the age. <color={C_FAULT}>The price is set on that basis</color>, not sentiment.",
                    $"The <b>{l.Color}</b> <b>{l.Make}</b> has been <color={C_FAULT}>stored while I concentrated on other things</color>. <color={C_GOOD}>Decent condition</color> for a stored car. <color={C_ASIDE}>Nothing dramatic wrong with it.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color={C_GOOD}>solid mid-range example</color>. <color={C_ASIDE}>I deal in enough of these to price one accurately.</color> <color={C_FAULT}>What I am asking is fair for the condition.</color>",
                    $"Selling the <b>{l.Make}</b> from the collection. <color={C_ASIDE}>I inspected it before listing.</color> It is what it is — <color={C_GOOD}>honest condition</color>. <color={C_FAULT}>Price is market rate for that.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Year}</b> <b>{l.Model}</b> has <color={C_FAULT}>been off the road</color> but was in <color={C_GOOD}>reasonable shape</color> when it went away. <color={C_ASIDE}>I checked it over before pricing.</color>",
                })
                : Pick(rng, new[]
                {
                    $"Strong example of the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>I deal in these regularly and I know what a <color={C_GOOD}>good one looks like</color>. This is one of them. Priced above the rough ones for a reason.</color>",
                    $"The <b>{l.Model}</b> is in <color={C_GOOD}>genuinely good condition</color> for the age. <color={C_ASIDE}>I have handled enough of these to say that with confidence. The asking price reflects condition, not wishful thinking.</color>",
                    $"This <b>{l.Color}</b> <b>{l.Make}</b> is one of the <color={C_GOOD}>cleaner examples I have had through</color>. <color={C_ASIDE}>I check everything before I price anything. What I am asking is based on what I found, not what I hope it is worth.</color>",
                    $"<color={C_GOOD}>Solid</color> <b>{l.Year}</b> <b>{l.Model}</b>. <color={C_ASIDE}>I know the common faults on these and this one is not showing them. That is worth paying for. The price reflects a car that does not need immediate remedial work.</color>",
                    $"The <b>{l.Make}</b> is in <color={C_GOOD}>above-average condition for the mileage</color>. <color={C_ASIDE}>I deal in volume so I know where on the spectrum this sits. Above average means above average price. It is a fair exchange.</color>",
                    $"One of the better <b>{l.Year}</b> <b>{l.Model}</b>s I have had. <color={C_ASIDE}><color={C_GOOD}>No nasty surprises on inspection.</color> I price these on condition and this one earns a higher number than the rough ones. Straightforward.</color>",
                });
            }

            // ── DETAIL 1 ─────────────────────────────────────────────────────────────
            string detail1;
            if (lv == 1)
            {
                detail1 = Pick(rng, new[]
                {
                    $"Has <b>{l.Mileage:N0} miles</b> on it. <color={C_ASIDE}>These things run for ages if you look after them.</color> <color={C_FAULT}>I looked after it about average I would say.</color>",
                    $"<color={C_FAULT}>Oil probably needs doing.</color> <color={C_ASIDE}>Or maybe I did it recently. Hard to keep track without writing it down.</color>",
                    $"<color={C_FAULT}>Last proper service was a while back.</color> <color={C_ASIDE}>I could not give you an exact date.</color> <color={C_GOOD}>It drives fine</color> so I never worried.",
                    $"<color={C_FAULT}>Battery is probably the original.</color> <color={C_GOOD}>Still cranks it over fine</color> <color={C_ASIDE}>so I never saw the point in replacing it.</color>",
                    $"It is a <b>{l.Year}</b> so there is some age on it. <color={C_GOOD}>But it has been reliable.</color> <color={C_ASIDE}>Never left me on the side of the road.</color>",
                    $"I have got the logbook and one key. <color={C_FAULT}>There might be a second key somewhere.</color> <color={C_ASIDE}>I would have to have a look.</color>",
                    $"<color={C_GOOD}>Tyres look okay to me.</color> <color={C_FAULT}>Not brand new but not bald either.</color> <color={C_ASIDE}>There is definitely rubber on them.</color>",
                    $"<b>{l.Mileage:N0}</b> on the clock. <color={C_GOOD}>Most of it was motorway miles.</color> <color={C_ASIDE}>Or at least I think it was. Previous owner said that.</color>",
                    $"Interior is <color={C_FAULT}>a bit lived in</color> but <color={C_GOOD}>nothing broken</color>. <color={C_ASIDE}>All the seats are there.</color>",
                    $"<color={C_FAULT}>Never really had it properly inspected.</color> <color={C_GOOD}>It started and drove</color> <color={C_ASIDE}>so I assumed everything was fine.</color>",
                    $"Air conditioning <color={C_FAULT}>does not work great</color>. <color={C_ASIDE}>Used to be colder. I just open the windows.</color>",
                    $"There is a folder of bits in the glovebox. <color={C_GOOD}>Some service receipts in there</color> I think. <color={C_ASIDE}>I never went through it thoroughly.</color>",
                });
            }
            else if (lv == 2)
            {
                detail1 = Pick(rng, new[]
                {
                    $"Has <b>{l.Mileage:N0} miles</b> on the clock. <color={C_GOOD}>All genuine.</color> <color={C_ASIDE}>Most of it was done before I parked it up.</color>",
                    $"The <b>{l.Make}</b> was <color={C_GOOD}>running fine day-to-day</color> before it went into storage. <color={C_FAULT}>The issues that developed are listed below.</color>",
                    $"I know this car <color={C_GOOD}>reasonably well</color>. <color={C_ASIDE}>What I know is in this listing.</color> <color={C_FAULT}>I have not done a full inspection recently.</color>",
                    $"It has been <color={C_FAULT}>standing for over a year</color> so <color={C_FAULT}>assume all fluids need refreshing</color>. <color={C_ASIDE}>Tyres will want checking too.</color>",
                    $"The <b>{l.Model}</b> was last used as a daily about eighteen months ago. <color={C_GOOD}>Before that it was reliable enough.</color> <color={C_FAULT}>Then one thing led to another.</color>",
                    $"<b>{l.Mileage:N0} miles</b>. For a <b>{l.Year}</b> that is <color={C_GOOD}>reasonable</color>. <color={C_GOOD}>Engine and gearbox were not the problem</color>.",
                    $"I have a <color={C_ASIDE}>rough idea of what it needs.</color> <color={C_FAULT}>Beyond that I cannot account for what sitting has done</color> to the smaller stuff.",
                    $"The <b>{l.Color}</b> <b>{l.Make}</b> has been <color={C_GOOD}>dry stored</color> so the <color={C_GOOD}>body is in decent shape</color>. <color={C_FAULT}>Mechanically is where the question marks are.</color>",
                    $"<color={C_GOOD}>Gearbox is fine, engine turns over.</color> <color={C_ASIDE}>The issues are more specific.</color> <color={C_FAULT}>No surprises beyond what is written here.</color>",
                });
            }
            else // lv == 3
            {
                detail1 = Pick(rng, new[]
                {
                    $"<b>{l.Mileage:N0} miles</b> on the clock. <color={C_GOOD}>Genuine</color> — <color={C_ASIDE}>I verify mileage before I buy these.</color> <color={C_GOOD}>It matches the condition</color> and the service history fragments.",
                    $"I inspected the <b>{l.Make}</b> before listing it. <color={C_ASIDE}>What I found is in this listing.</color> <color={C_FAULT}>I do not omit things</color> — <color={C_ASIDE}>it costs me repeat business.</color>",
                    $"The <b>{l.Model}</b> has been <color={C_GOOD}>stored properly</color>. <color={C_ASIDE}>Dry, covered, off the road intentionally.</color> <color={C_GOOD}>That matters on a car this age.</color>",
                    $"<color={C_GOOD}>Engine starts and runs</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_ASIDE}>I ran it before pricing it.</color> <color={C_GOOD}>Gearbox selects cleanly.</color> <color={C_FAULT}>The issues that exist are listed.</color>",
                    $"I have handled a lot of <b>{l.Make}</b>s. <color={C_ASIDE}>I know what to look for and I looked for it.</color> <color={C_FAULT}>The asking price is based on what I found</color>, <color={C_ASIDE}>not a guess.</color>",
                    $"<b>{l.Mileage:N0}</b> miles. <color={C_GOOD}>Consistent with the condition.</color> <color={C_ASIDE}>Engine and gearbox are not the story here</color> — <color={C_FAULT}>the specific issues are listed below.</color>",
                    $"The <b>{l.Color}</b> <b>{l.Model}</b> came as part of a purchase. <color={C_ASIDE}>I have gone through it properly.</color> <color={C_GOOD}>What I know is in this listing.</color> <color={C_FAULT}>I do not sell things I cannot describe accurately.</color>",
                    $"I put the <b>{l.Make}</b> <color={C_ASIDE}>on a ramp before listing it.</color> <color={C_GOOD}>Nothing structural.</color> <color={C_FAULT}>The faults I have listed are the faults there are.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has a history I can partially account for. <color={C_GOOD}>Mileage is believable</color>, <color={C_ASIDE}>condition matches the clock.</color> <color={C_FAULT}>I priced it on what I can verify.</color>",
                });
            }

            // ── DETAIL 2 (optional ~65%) ──────────────────────────────────────────────
            string detail2;
            if (lv == 1)
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"<color={C_GOOD}>Drove it to <b>{l.Location}</b> and back</color> a few times <color={C_ASIDE}>without any issues. That is probably my best endorsement.</color>",
                    $"<color={C_GOOD}>Starts on the first or second turn</color> most of the time. <color={C_FAULT}>Winter it sometimes takes three.</color> <color={C_ASIDE}>Normal I think.</color>",
                    $"<color={C_GOOD}>Never heard any proper worrying noises</color> from the engine. <color={C_FAULT}>A few minor ones</color> <color={C_ASIDE}>but nothing that made me pull over.</color>",
                    $"Interior is <color={C_FAULT}>dusty</color> but <color={C_ASIDE}>nothing a good hoover would not sort out. Have not had time to do it.</color>",
                    $"Tyres have got <color={C_GOOD}>some life left</color> on them. <color={C_FAULT}>Not loads, but some.</color>",
                    $"Bodywork has <color={C_FAULT}>the odd mark here and there</color>. <color={C_ASIDE}>All shown in photos. Nothing dramatic.</color>",
                    $"The <b>{l.Color}</b> paint has <color={C_FAULT}>a few stone chips</color> on the front. <color={C_ASIDE}>These things happen. Does not affect how it drives.</color>",
                    $"<color={C_GOOD}>Climate control works.</color> <color={C_ASIDE}>The</color> <color={C_FAULT}>passenger side vent is a bit temperamental</color> <color={C_ASIDE}>but the main controls are fine.</color>",
                    $"<color={C_GOOD}>Radio works fine.</color> <color={C_ASIDE}>The display has a</color> <color={C_FAULT}>dead pixel</color> <color={C_ASIDE}>in the corner. I stuck a bit of tape over it.</color>",
                    $"Boot opens and closes fine. <color={C_FAULT}>The lock is a bit stiff</color> <color={C_ASIDE}>but you get the hang of it.</color>",
                    $"Comes with a spare tyre. <color={C_ASIDE}>Whether it has any air in it I could not tell you. <color={C_FAULT}>Probably needs checking.</color></color>",
                    $"Both keys present. <color={C_FAULT}>One of the fobs does not lock the car.</color> <color={C_GOOD}>The other one works perfectly.</color>",
                }, 0.65);
            }
            else if (lv == 2)
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"Body is <color={C_GOOD}>in reasonable shape</color> for a stored car. <color={C_ASIDE}>No new damage while it was sitting.</color>",
                    $"Interior is <color={C_FAULT}>dusty</color> but <color={C_GOOD}>intact</color>. <color={C_ASIDE}>Nothing broken or missing as far as I can see.</color>",
                    $"The <b>{l.Make}</b> was <color={C_GOOD}>garaged</color> so the <color={C_GOOD}>paint has held up okay</color>. <color={C_ASIDE}>Couple of old marks but nothing new.</color>",
                    $"Both keys present. <color={C_ASIDE}>V5 in my name.</color> <color={C_FAULT}>MOT lapsed while it was standing</color>, <color={C_ASIDE}>as you would expect.</color>",
                    $"Tyres look okay visually but <color={C_FAULT}>they have been static for a long time</color>. <color={C_ASIDE}>Worth a proper check before any miles.</color>",
                    $"The <b>{l.Model}</b> was <color={C_GOOD}>washed before storage</color>. <color={C_ASIDE}>Still looks reasonable considering the time.</color>",
                    $"<color={C_GOOD}>No accident history</color> that I know of. <color={C_ASIDE}>Bought it privately, parked it up. Straightforward history.</color>",
                    $"Underneath is <color={C_ASIDE}>what you would expect.</color> <color={C_FAULT}>Some surface corrosion</color> but <color={C_GOOD}>nothing structural</color>.",
                    $"<color={C_GOOD}>Lights all work.</color> <color={C_ASIDE}>Checked them before listing.</color> <color={C_FAULT}>The bigger issues are the ones in the description.</color>",
            }, 0.65);
            }
            else // lv == 3
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"Body on the <b>{l.Make}</b> is <color={C_GOOD}>in reasonable shape</color>. <color={C_ASIDE}>No significant accident damage.</color> <color={C_GOOD}>Paint is original</color> and consistent.",
                    $"Interior is <color={C_GOOD}>complete and intact</color>. <color={C_ASIDE}>Wear consistent with the mileage.</color> <color={C_GOOD}>Nothing missing</color>, nothing bodged.",
                    $"Underneath is <color={C_GOOD}>clean for a <b>{l.Year}</b> car</color>. <color={C_GOOD}>Sills are solid</color>, <color={C_ASIDE}>floor is sound. I check these things because they affect value.</color>",
                    $"V5 present. <color={C_ASIDE}>No outstanding finance.</color> <color={C_FAULT}>MOT lapsed while in storage</color> <color={C_ASIDE}>as expected.</color>",
                    $"The <b>{l.Model}</b> has both keys. <color={C_FAULT}>Not a full history</color> <color={C_ASIDE}>but enough to support the mileage claim.</color>",
                    $"Tyres have <color={C_GOOD}>usable tread</color>. <color={C_FAULT}>Not new</color>, <color={C_ASIDE}>but not dangerous.</color> <color={C_GOOD}>Wheels are straight</color>.",
                    $"Cooling system is <color={C_GOOD}>intact</color> with <color={C_ASIDE}>no external leaks.</color> <color={C_GOOD}>Hoses and clips are serviceable.</color>",
                    $"The <b>{l.Color}</b> paint has <color={C_GOOD}>held up well</color>. <color={C_ASIDE}>No new corrosion.</color> <color={C_FAULT}>Existing marks are visible in photos</color> — no surprises.",
                    $"Gearbox <color={C_GOOD}>selects all gears cleanly</color>. <color={C_GOOD}>Clutch bites where it should</color>. <color={C_ASIDE}>Drivetrain is not the issue.</color>",
                }, 0.65);
            }

            // ── FAULT ─────────────────────────────────────────────────────────────────
            // Wrecker rzadziej ujawnia faults niż Honest — wyższy level = mniej ujawnia
            string fault = DominantFaultLine(l, SellerArchetype.Wrecker, lv, rng);
            double faultChance = lv switch { 1 => 0.65, 2 => 0.40, _ => 0.20 };
            if (fault != null && rng.NextDouble() > faultChance) fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────────────
            string closer;
            if (lv == 1)
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    $"Price is <color={C_FAULT}>low because of that</color>. Someone who knows what they are doing will get a good deal.",
                    $"Hence why the <b>{l.Model}</b> is priced where it is. Not hiding anything, just not sure what it needs.",
                    $"Priced for a quick sale. I would rather someone else figure it out than it sit on my drive another year.",
                    $"If you know your way around a <b>{l.Make}</b> it is probably a straightforward fix. I do not, which is why I am selling.",
                    $"That is why the price is what it is. I would rather be upfront about it and sell it cheap.",
                    $"Cash, collect from <b>{l.Location}</b>, no holding it. Price is firm because I have already come down to this.",
                    $"Sold as-is. Everything I have noticed is in this listing. There could be other things. There probably are.",
                }
                : new[]
                {
                    $"Priced fairly for what it is. No hard sell, just a car that needs a new owner.",
                    $"Collection from <b>{l.Location}</b>. Cash only, no time wasters. First to see it will probably take it.",
                    $"Not a project, not a showroom car. Just a decent <b>{l.Make}</b> at a sensible price.",
                    $"If you want to come and look, come and look. No pressure. Bring someone who knows cars if you like.",
                    $"Just want it to go to someone who will use it. Priced to sell, not to sit.",
                    $"Happy for a test drive in <b>{l.Location}</b>. Cash on collection.",
                    $"First sensible offer takes it. Not looking to negotiate all week. Just a clean sale.",
                    $"No swaps, cash only. Happy to answer questions but if you need a full inspection report this is not the car for you.",
                });
            }
            else if (lv == 2)
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    $"The issue is known and the price reflects it. <color={C_ASIDE}>I am not trying to hide anything, just moving the car on.</color>",
                    $"Priced with the work in mind. <color={C_ASIDE}>If you can do it yourself you will come out ahead. I just cannot justify the time right now.</color>",
                    $"Everything relevant is in the listing. <color={C_ASIDE}>No further questions I can answer better than what is written here. Come and see it if you are serious.</color>",
                    $"Cash, collect from <b>{l.Location}</b>. <color={C_ASIDE}>Not holding it and not negotiating via message. Come, look, decide.</color>",
                    $"The <b>{l.Make}</b> is priced as a <color={C_ASIDE}>car with known issues, not as a runner. If your expectations match that we will get on fine.</color>",
                    $"I know what it needs. <color={C_ASIDE}>I have priced it so the next owner can cover that and still come out sensible. No lowballing, I did the maths.</color>",
                    $"Selling as-is from <b>{l.Location}</b>. <color={C_ASIDE}>Inspection welcome, no test drives on public road until it is sorted. Sensible buyers only.</color>",
                }
                : new[]
                {
                    $"Nothing dramatic wrong with it beyond what is listed. <color={C_ASIDE}>Priced fairly for a car that has been standing. First sensible offer from <b>{l.Location}</b> takes it.</color>",
                    $"Cash on collection. <color={C_ASIDE}>Not looking for messages asking what my lowest price is. The price is the price.</color>",
                    $"Comes as it sits. <color={C_ASIDE}>I have been straight about the condition. No holding, no part exchange, no drama.</color>",
                    $"Collection from <b>{l.Location}</b>. <color={C_ASIDE}>I can have it ready to look at with a few hours notice. Serious buyers only, please.</color>",
                    $"The <b>{l.Model}</b> is ready to go to someone who knows what they are getting. <color={C_ASIDE}>Priced for what it is, not what it could be with work.</color>",
                    $"If you want to bring a code reader, bring a code reader. <color={C_ASIDE}>I have nothing to hide. The listing is what it is.</color>",
                });
            }
            else // lv == 3
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    $"The fault is listed, the cost is costed, the price is set. <color={C_ASIDE}>I do not negotiate on cars I have priced accurately. Come and inspect it — I welcome that.</color>",
                    $"I have been doing this long enough to price a <b>{l.Make}</b> correctly. <color={C_ASIDE}>What I am asking already accounts for the work listed. Do not offer less — the maths does not support it.</color>",
                    $"Everything wrong with the <b>{l.Model}</b> is in this listing. <color={C_ASIDE}>Nothing is hidden because hiding things loses me future business. Price is final. Collection from <b>{l.Location}</b>.</color>",
                    $"The asking price on the <b>{l.Year}</b> <b>{l.Make}</b> reflects the actual cost of the actual fault. <color={C_ASIDE}>I checked the parts prices before I set the number. Serious buyers only — I do not have time for anything else.</color>",
                    $"I deal in volume. <color={C_ASIDE}>I price accurately and I move on. The <b>{l.Model}</b> is priced correctly for what it needs. Inspect it, verify what I have said, buy it or do not.</color>",
                    $"No room to move on price. <color={C_ASIDE}>I know what the <b>{l.Make}</b> needs, I know what that costs, and I have set the number accordingly. If my maths is wrong I am open to being shown why. Opinions are not maths.</color>",
                    $"Cash on collection from <b>{l.Location}</b>. <color={C_ASIDE}>No holding, no part exchange, no drawn-out viewings. I described the car accurately — the rest is straightforward.</color>",
                }
                : new[]
                {
                    $"Price is set on condition, not on what I paid. <color={C_ASIDE}>I price these accurately because my reputation depends on it. What I am asking for the <b>{l.Model}</b> is fair for what it is.</color>",
                    $"I do not inflate prices and I do not drop them without reason. <color={C_ASIDE}>The <b>{l.Year}</b> <b>{l.Make}</b> is priced on its merits. Come and see it if you are serious.</color>",
                    $"Cash, collection from <b>{l.Location}</b>, no time wasters. <color={C_ASIDE}>I described the car accurately. A sensible buyer will recognise that and we will have a quick, easy transaction.</color>",
                    $"I have sold a lot of these. <color={C_ASIDE}>I know what they are worth and I price them accordingly. The <b>{l.Model}</b> is not overpriced. It will not be here long.</color>",
                    $"Viewing welcome with reasonable notice. <color={C_ASIDE}>I stand behind my descriptions. Come and verify what I have written — you will find it matches what is in front of you.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> is priced to sell, not to sit. <color={C_ASIDE}>I have too much stock to carry things that are priced correctly and not moving. If it is here it is available. If it is gone it is gone.</color>",
                });
            }

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }

        #endregion // BUILD_WRECKER

        // ════════════════════════════════════════════════════════════════════
        #region BUILD_DEALER
        // ════════════════════════════════════════════════════════════════════
        //  Dealer — fliper samochodów, naprawia wygląd i sprzedaje drożej.
        //
        //  LOGIKA:
        //  L1 Backyard — amator który posprzątał i sprzedaje. Nie kłamie wprost.
        //  L2 Pro      — profesjonalny detailing, zna terminologię, sehr convincing.
        //  L3 Criminal — nie do odróżnienia od Honest L3. Cena lekko poniżej rynku
        //                jako przynęta. Brak osobistej historii z autem.
        //
        //  KOLORY:
        //  C_PREMIUM (#ffdd88) — złoty, "luksusowe" brzmienie dla Dealer L2/L3
        //  C_GOOD    (#99ff99) — pozytywne dla L1 (mniej wykwintny)
        //  C_TECH    (#dddddd) — chłodny techniczny dla L3
        //  C_ASIDE   (#cccccc) — neutralne komentarze
        //  BRAK C_FAULT       — Dealer nigdy nie mówi o wadach
        // ════════════════════════════════════════════════════════════════════

        private static string BuildDealer(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;


            // ── OPENER ───────────────────────────────────────────────────────────────
            string opener = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Just <color={C_GOOD}>freshly valeted</color> this <b>{l.Year}</b> <b>{l.Make}</b> — came up really well, looks a lot better in person than photos can show.",
                    $"Picked this <b>{l.Model}</b> up, gave it a <color={C_GOOD}>proper clean and a good going over</color>, passing it on now at a fair price.",
                    $"<color={C_GOOD}>Touched up a few marks</color> and deep cleaned the interior on this <b>{l.Make}</b>. Runs smoothly and looks presentable.",
                    $"Sorted out the cosmetics on this <b>{l.Year}</b> <b>{l.Model}</b> — <color={C_GOOD}>it came up really well</color> after a bit of attention. Happy with how it turned out.",
                    $"Bought this <b>{l.Make}</b> to tidy up and sell on. <color={C_GOOD}>Done the basics — clean, polished, presentable.</color> Good car for the money.",
                    $"Had this <b>{l.Year}</b> <b>{l.Model}</b> sitting for a few weeks. <color={C_GOOD}>Gave it a full valet and touched up the bodywork.</color> Ready to go.",
                    $"Not much to say about this <b>{l.Make}</b> — <color={C_GOOD}>I cleaned it up nicely</color> and it drives well. Simple, honest sale.",
                    $"This <b>{l.Color}</b> <b>{l.Model}</b> <color={C_GOOD}>scrubbed up really well</color>. Gave it some attention inside and out. Good buy at this price.",
                }),
                2 => Pick(rng, new[]
                {
                    $"<color={C_PREMIUM}>Full professional valet</color> completed on this <b>{l.Year}</b> <b>{l.Make}</b>. Recent service, drives like a much newer car.",
                    $"<color={C_PREMIUM}>Immaculate example</color> of a <b>{l.Year}</b> <b>{l.Make} {l.Model}</b> — price is firm because the quality speaks for itself.",
                    $"Turn-key ready, not a project. This <b>{l.Model}</b> has been <color={C_PREMIUM}>fully prepared before listing</color> — everything checked and working as it should.",
                    $"One of the <color={C_PREMIUM}>better examples</color> of a <b>{l.Year}</b> <b>{l.Make}</b> I have put together. Professionally detailed, drives superbly.",
                    $"This <b>{l.Color}</b> <b>{l.Model}</b> has had a <color={C_PREMIUM}>full preparation before sale</color> — not one of those rush jobs. Come and see the difference.",
                    $"I take pride in the cars I sell. This <b>{l.Year}</b> <b>{l.Make}</b> has been <color={C_PREMIUM}>professionally detailed and mechanically checked.</color>",
                    $"<color={C_PREMIUM}>Exceptional presentation</color> on this <b>{l.Make} {l.Model}</b>. Everything works, everything is clean. Priced on condition.",
                    $"Selling this <b>{l.Year}</b> <b>{l.Model}</b> after a <color={C_PREMIUM}>full professional preparation.</color> This is how a used car should look.",
                }),
                _ => Pick(rng, new[]
                {
                    // L3 Criminal — brzmi jak Honest L3, bez osobistej historii
                    $"<color={C_TECH}>Dealer maintained throughout its life</color> with full electronic history to confirm it. One of the best <b>{l.Year}</b> examples around.",
                    $"One of the <color={C_PREMIUM}>cleanest <b>{l.Year}</b> <b>{l.Model}</b> examples</color> currently available — and priced accordingly. Viewing will not disappoint.",
                    $"<color={C_TECH}>Former company vehicle</color> with low-stress usage, single driver from new, all original components. A rare find in this condition.",
                    $"This is the kind of <b>{l.Make}</b> you <color={C_PREMIUM}>buy when you want to buy once and buy right.</color> Everything is as it should be.",
                    $"<color={C_PREMIUM}>Genuinely exceptional</color> <b>{l.Year}</b> <b>{l.Make} {l.Model}</b>. Full documentation, complete history, nothing outstanding.",
                    $"I do not sell cars I am not proud of. This <b>{l.Model}</b> is <color={C_PREMIUM}>one of the finest examples</color> I have had through my hands.",
                    $"<color={C_PREMIUM}>Pristine example</color> of the <b>{l.Year}</b> <b>{l.Make}</b>. History is complete, condition is outstanding. Priced to move quickly.",
                    $"A <color={C_PREMIUM}>truly turn-key</color> <b>{l.Make} {l.Model}</b>. Nothing to do, nothing to spend. Drive it away and enjoy it.",
                }),
            };

            // ── DETAIL 1 ─────────────────────────────────────────────────────────────
            string detail1 = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Runs <color={C_GOOD}>really smoothly</color> — no rattles, no warning lights, nothing that caught my attention.",
                    $"Drives well. <color={C_GOOD}>Fluid and composed on the road.</color> Nothing to complain about on the test drive.",
                    $"<color={C_GOOD}>Looks and feels considerably better</color> than when I got it. The work shows.",
                    $"Ready to drive away today. <color={C_GOOD}>Nothing outstanding to deal with.</color>",
                    $"I checked the basics before listing. <color={C_GOOD}>Fluids topped up, tyres look fine, no lights on the dash.</color>",
                    $"Engine <color={C_GOOD}>starts cleanly and pulls well.</color> Gearbox is smooth. Nothing that gave me any concern.",
                    $"The <b>{l.Color}</b> paint <color={C_GOOD}>came up brilliantly</color> after a polish. Interior is fresh. Overall very presentable.",
                    $"Selling the <b>{l.Make}</b> after sorting it out. <color={C_GOOD}>It is ready to use immediately</color> — not a project.",
                }),
                2 => Pick(rng, new[]
                {
                    $"<color={C_PREMIUM}>Every service stamp is present</color> and accounted for — history is complete and consistent.",
                    $"<color={C_PREMIUM}>One owner history sourced</color>, garage kept its entire life. Exceptional for the age.",
                    $"Brakes <color={C_PREMIUM}>checked and bedded in</color>, fluids all fresh, all lights and electrics fully functional.",
                    $"<color={C_PREMIUM}>Selling due to a change in circumstances</color> — this would not be going otherwise. Genuinely good car.",
                    $"I have put this <b>{l.Make}</b> through a <color={C_PREMIUM}>full pre-sale check.</color> Everything is in order. No advisories outstanding.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color={C_PREMIUM}>full electronic history</color>. Clean bill of health at the last inspection.",
                    $"Mechanically it is <color={C_PREMIUM}>exactly where it should be</color> for the mileage. History backs that up.",
                    $"<color={C_PREMIUM}>Nothing to find on this one</color> — I went through it properly before pricing it. Not a car I would be embarrassed to sell.",
                }),
                _ => Pick(rng, new[]
                {
                    $"<color={C_TECH}>Full electronic history check passed with zero flags</color> — mileage verified at <b>{l.Mileage:N0}</b>. Clean provenance.",
                    $"Interior and exterior both <color={C_PREMIUM}>professionally prepared</color> — the detail work on this is exceptional.",
                    $"<color={C_TECH}>Independent inspection report available on request</color> — nothing to hide means nothing to hide.",
                    $"The <b>{l.Make}</b> presents at a level you <color={C_PREMIUM}>very rarely see in this price bracket.</color> History is complete and verified.",
                    $"<color={C_TECH}>All service intervals met</color>, all stamps present. No gaps, no question marks. Everything is documented.",
                    $"This <b>{l.Model}</b> has been <color={C_PREMIUM}>maintained to a standard well above</color> what you typically see at this age and mileage.",
                    $"Mechanically <color={C_TECH}>verified before listing</color>. Inspection report confirms condition. Available on viewing.",
                    $"<color={C_TECH}>Zero stored fault codes</color> on the diagnostic check. All systems reading correctly. Nothing pending.",
                }),
            };

            // ── DETAIL 2 (optional ~75%) ──────────────────────────────────────────────
            string detail2 = lv switch
            {
                1 => MaybePick(rng, new[]
                {
                    $"Interior is <color={C_GOOD}>clean with no rips, stains or damage</color> — looks good for the age.",
                    $"Paintwork <color={C_GOOD}>responded well</color> — came up nicely after a machine polish on the panels.",
                    $"<color={C_GOOD}>Wheels are clean, tyres are decent</color> — nothing embarrassing about the way this presents.",
                    $"<color={C_GOOD}>No smoke on startup, no unusual noises, no warning lights</color> — basics are all covered.",
                    $"Bodywork is <color={C_GOOD}>straight and clean</color> for the age. The photos are honest — it does look this good in person.",
                    $"The <b>{l.Color}</b> <color={C_GOOD}>shows really well</color>. One of those colours that looks good clean.",
                    $"<color={C_GOOD}>Both keys present.</color> <color={C_ASIDE}>V5 in my name. MOT current.</color>",
                    $"Drives a lot better than you might expect from a <b>{l.Year}</b>. <color={C_GOOD}>Nice car for the money.</color>",
                }, 0.75),
                2 => MaybePick(rng, new[]
                {
                    $"Comes with <color={C_PREMIUM}>a full set of keys and all original documentation.</color>",
                    $"Bodywork is <color={C_PREMIUM}>straight with no accident history</color> — clean HPI to confirm.",
                    $"<color={C_PREMIUM}>Alloys are clean, tyres are nearly new</color> — presentation is genuinely excellent.",
                    $"This is the kind of car where <color={C_PREMIUM}>a viewing sells it</color> — photos do not do it justice.",
                    $"<color={C_PREMIUM}>Interior is immaculate</color>. No wear on the bolsters, no marks on the headlining. Kept properly.",
                    $"The <b>{l.Make}</b> has been <color={C_PREMIUM}>kept in a garage its entire life</color>. Condition reflects that.",
                    $"<color={C_PREMIUM}>HPI clear, no outstanding finance, no write-off history.</color> Clean bill of health.",
                    $"Paint is <color={C_PREMIUM}>deep and glossy</color> — you can see the prep work that has gone into this.",
                }, 0.75),
                _ => MaybePick(rng, new[]
                {
                    $"Comes with <color={C_TECH}>full documentation, both sets of keys, and a clean HPI certificate.</color>",
                    $"Paint is in <color={C_PREMIUM}>remarkable condition for the age</color> — no fading, no chips worth mentioning.",
                    $"Tyres are all <color={C_TECH}>matching brand with substantial tread remaining</color>. Not the kind of thing you usually see.",
                    $"Interior shows <color={C_PREMIUM}>barely any wear</color> — this car has been treated properly its entire life.",
                    $"Underside is <color={C_TECH}>clean and dry</color>. No corrosion, no leaks, no previous accident damage visible.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> presents at a level that will <color={C_PREMIUM}>immediately justify the asking price</color> on viewing.",
                    $"<color={C_TECH}>Nothing has been hidden or glossed over</color> — the condition you see is the condition throughout.",
                    $"Alloys are <color={C_TECH}>unmarked and balanced</color>. Tyres are premium brand, even tread all round.",
                }, 0.75),
            };

            // ── CLOSER ───────────────────────────────────────────────────────────────
            string closer = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"<color={C_GOOD}>Presentable car at a price that makes sense</color> — come and see for yourself.",
                    $"Not a project — <color={C_GOOD}>everything sorted, ready to use immediately.</color>",
                    $"<color={C_GOOD}>Happy to arrange a viewing</color> at a convenient time. No pressure.",
                    $"<color={C_GOOD}>Priced fairly</color> — not the cheapest out there but you get what you pay for.",
                    $"<color={C_GOOD}>Good car for the money.</color> Come and have a look — I think you will be pleased.",
                    $"Collection from <b>{l.Location}</b>. <color={C_GOOD}>Cash or bank transfer.</color> Straightforward sale.",
                    $"<color={C_GOOD}>Priced to sell</color>, not to sit. First serious viewer will take it.",
                    $"No silly offers. <color={C_GOOD}>Price reflects the work done</color> and the condition it is in now.",
                }),
                2 => Pick(rng, new[]
                {
                    $"<color={C_PREMIUM}>First to view will buy</color> — these do not hang around at this price.",
                    $"Serious buyers only — <color={C_ASIDE}>time wasters will be ignored.</color>",
                    $"<color={C_PREMIUM}>Price is firm</color> and reflects exactly what is on offer here.",
                    $"<color={C_PREMIUM}>Viewing by appointment</color> — contact me to arrange a suitable time.",
                    $"I have had significant interest already. <color={C_PREMIUM}>Move quickly if you are serious.</color>",
                    $"<color={C_PREMIUM}>Test drives welcome</color> for serious buyers with proof of insurance.",
                    $"<color={C_PREMIUM}>No hidden extras, no surprises.</color> What you see is what you get — and what you get is very good.",
                    $"Priced at <color={C_PREMIUM}>below comparable listings</color>. I want a quick, clean sale. No drama.",
                }),
                _ => Pick(rng, new[]
                {
                    // L3 — presja i "okazja" jako przynęta
                    $"<color={C_TECH}>Priced below comparable listings</color> — serious buyers should move quickly.",
                    $"I stand behind <color={C_PREMIUM}>every car I sell</color> — satisfaction is not negotiable.",
                    $"Come and see it. <color={C_PREMIUM}>Once you do, the price will make complete sense.</color>",
                    $"The kind of car that <color={C_PREMIUM}>will not be here long at this price</color> — act accordingly.",
                    $"<color={C_TECH}>Full inspection welcome.</color> Independent mechanic, code reader, paint depth gauge — bring what you like.",
                    $"I sell on reputation. <color={C_PREMIUM}>This is the standard I hold myself to.</color> Come and verify that.",
                    $"<color={C_TECH}>No negotiation on price.</color> The number reflects the car. You will understand when you see it.",
                    $"Collect from <b>{l.Location}</b>. <color={C_PREMIUM}>Everything is ready.</color> First serious buyer takes it.",
                }),
            };

            return Fill(Join(opener, detail1, detail2, closer), l);
        }

        #endregion // BUILD_DEALER

        // ════════════════════════════════════════════════════════════════════
        #region BUILD_SCAMMER
        // ════════════════════════════════════════════════════════════════════
        //  Scammer — oszust sprzedający auto które nie istnieje lub jest wrakiem.
        //
        //  LOGIKA:
        //  L1 Amateur      — absurdalny, łatwy do wykrycia. Nigeryjski książę,
        //                    kody podarunkowe, kosmiczne opisy, presja czasowa.
        //  L2 Intermediate — podszywa się pod Dealera lub Honest. Konkretne
        //                    kłamstwa, fałszywe rachunki, pewność siebie.
        //  L3 Expert       — composite fraud: kopiuje tony Honest L3 i Dealer L3.
        //                    Niemal nie do odróżnienia. Czerwone flagi: brak
        //                    osobistej historii z autem, zmiana tonu między
        //                    akapitami, presja i "okazja" w closer.
        //
        //  KOLORY:
        //  L1: C_SCREAM  (#ffcc00) — wykrzykniki, CAPS, pułapki
        //      C_DANGER  (#ff4444) — krzyk, groźba, niebezpieczeństwo
        //      C_SCAM_GRN(#00ff00) — fałszywa pewność "100% legit"
        //      C_SCAM_CYN(#00ffff) — absurdalny cyjan — twierdzenia z kosmosu
        //      C_SCAM_PRP(#9b80c8) — fiolet — mistycyzm, bajki
        //      C_SCAM_PNK(#ff00ff) — różowy — groteska, niedorzeczność
        //  L2: C_GOOD    (#99ff99) — kłamstwo podszywające się pod uczciwość
        //      C_SCREAM  (#ffcc00) — presja, "ostatnia szansa"
        //  L3: C_FAKE_OK (#aaffcc) — pre-emptive dismissal (jak Honest L3)
        //      C_TECH    (#dddddd) — zimny techniczny (jak Dealer L3)
        //      C_PREMIUM (#ffdd88) — "premium" (jak Dealer L2)
        // ════════════════════════════════════════════════════════════════════

        private static string BuildScammer(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ───────────────────────────────────────────────────────────────
            // L1: groteskowy, pełen błędów, łatwy do wykrycia
            // L2: pewny siebie, konkretny, podszywa się pod profesjonalistę
            // L3: kopiuje styl Honest L3 lub Dealer L3 — zimny, bez emocji, brak historii
            string opener = lv switch
            {
                // ── L1: Amateur — absurd na maksa ────────────────────────────────────
                1 => Pick(rng, new[]
                {
                    $"<color={C_SCREAM}>GREETINGS.</color> I am <color={C_SCAM_PRP}>Official Prince</color> of small oil nation. Must sell personal <b>{l.Make}</b> to fund royal wedding.",
                    $"Good car. No bad history. Small blood stain in trunk is from <color={C_SCAM_PNK}>beetroot juice</color>, very organic. <color={C_SCREAM}>No police please.</color>",
                    $"<color={C_SCREAM}>ATENTION:</color> this <b>{l.Model}</b> belong to <color={C_SCAM_CYN}>Elon Musk</color> personal. He sign windscreen with invisible ink. <color={C_SCAM_PRP}>Very rare!</color>",
                    $"Genuine sale! I am simple <color={C_SCAM_PRP}>oil rig worker</color> currently at sea. My cousin will give you car after you pay me. <color={C_SCAM_GRN}>100% safe.</color>",
                    $"Selling fast because I have <color={C_SCAM_PNK}>too many cars</color> and my wife says I am 'crazy'. <color={C_SCREAM}>Her loss is your gain!</color>",
                    $"I do not want to sell. This car is my <color={C_SCAM_GRN}>best friend</color>. But best friend needs new home with more money.",
                    $"<color={C_SCREAM}>HELLO FRIEND.</color> You look like honest buyer. I have <color={C_SCAM_PRP}>special price</color> only for you, don't tell others.",
                    $"Car was found in <color={C_SCAM_PRP}>secret bunker</color>. Previous owner was time traveler. Year <b>{l.Year}</b> is actually from future.",
                    $"<color={C_SCREAM}>PLEASE READ.</color> I am high bank official from war-torn nation. Buy this <b>{l.Make}</b> to help me unfreeze <color={C_SCAM_CYN}>$25,000,000 frozen account</color>.",
                    $"I am not selling. I am transferring <color={C_SCAM_PRP}>destiny</color>. Price is just a formality for government.",
                    $"<color={C_DANGER}>SYSTEM ERROR!</color> This price is <color={C_SCREAM}>glitch</color>. I do not know how long it will be active. <color={C_DANGER}>Buy very fast!</color>",
                    $"<color={C_SCREAM}>CONGRATULATIONS!</color> Your IP address was randomly chosen to have the <color={C_SCAM_GRN}>supreme honor</color> of buying my <b>{l.Make}</b>.",
                    $"<color={C_DANGER}>URGENT MESSAGE</color> from <color={C_DANGER}>FBI Headquarters:</color> This <b>{l.Model}</b> is officially too cheap. <color={C_SCREAM}>Buy before we confiscate it.</color>",
                    $"Greetings! My grandfather was famous pirate. He left me this <b>{l.Year}</b> <b>{l.Make}</b>. <color={C_SCAM_PRP}>Treasure map</color> not included in price.",
                    $"Hello kind sir. I must sell this auto today to buy food for my <color={C_SCAM_PNK}>400 stray cats</color>. They are <color={C_SCREAM}>very hungry.</color>",
                    $"<color={C_SCAM_CYN}>BEEP BOOP.</color> I mean, hello fellow human. I am real flesh person selling this very normal <b>{l.Make}</b>. <color={C_ASIDE}>Captchas solved: 0.</color>",
                    $"Hi, I am famous Hollywood actor <color={C_SCAM_PRP}>John Drad Pitt</color>. I am selling my disguise car. <color={C_DANGER}>Don't tell paparazzi.</color>",
                    $"A <color={C_SCAM_PRP}>blind fortune teller</color> told me I must sell this <b>{l.Model}</b> to you today, or my hair will fall out. <color={C_SCREAM}>Please save my hair.</color>",
                    $"<color={C_SCREAM}>TOP SECRET CLEARANCE REQUIRED.</color> Just kidding, anyone can buy. But seriously, this <b>{l.Make}</b> is <color={C_SCAM_PRP}>classified</color>.",
                    $"My astrologer said Mercury is in retrograde, so I must liquidate all my <b>{l.Make}</b>s. <color={C_SCAM_PRP}>Bad vibes must go!</color>",
                    $"Hello. I am selling this car because it is <color={C_DANGER}>too fast for my eyes</color>. Everything is blurry when I drive. <color={C_SCREAM}>Not safe for me.</color>",
                    $"Dear Beneficiary, I have been instructed by the <color={C_SCAM_GRN}>World Car Bank</color> to release this vehicle to you immediately.",
                }),

                // ── L2: Intermediate — podszywa się pod profesjonalistę ───────────────
                2 => Pick(rng, new[]
                {
                    $"I don't care what the system says, this <b>{l.Make}</b> is a <color={C_SCREAM}>1-of-1 prototype</color>. You won't find another like it in <b>{l.Location}</b>.",
                    $"Stop looking at other listings. This <b>{l.Year}</b> <b>{l.Model}</b> is <color={C_GOOD}>the only one worth your money</color>. All others are imitations.",
                    $"I've been a professional dealer for 20 years. My <color={C_GOOD}>5-star reputation</color> speaks for itself. <color={C_ASIDE}>Don't check the listing stats.</color>",
                    $"Why are you hesitating? A <b>{l.Year}</b> <b>{l.Make}</b> for only <color={C_SCREAM}>${l.Price:N0}</color> is basically a gift. <color={C_ASIDE}>I'm practically giving it away.</color>",
                    $"Directly from my private collection. <color={C_GOOD}>Best-kept example in <b>{l.Location}</b></color>. Priced to move, not to sit.",
                    $"Listen, I am a <color={C_GOOD}>Certified Master Dealer</color>. My <color={C_SCREAM}>flawless reputation</color> is your guarantee. The stats mean nothing.",
                    $"Stop looking at the condition figures. This <b>{l.Make}</b> is actually a <color={C_SCREAM}>{l.Year + 5} prototype</color>. The system is wrong.",
                    $"Directly from my private vault. <color={C_GOOD}>Best <b>{l.Model}</b> on the market right now</color>. Shipping is my specialty.",
                    $"I am the CEO of <color={C_SCREAM}>Car-King International</color>. My time costs more than this car. <color={C_ASIDE}>You should feel lucky I responded.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Make}</b> was <color={C_GOOD}>custom built for a famous Sultan</color>. You are lucky I even let you look at it.",
                    $"I don't deal with amateurs. If you can't handle a <color={C_SCREAM}>high-performance</color> <b>{l.Model}</b>, move along.",
                    $"My reputation is <color={C_GOOD}>absolutely flawless</color>. Only a fool would doubt a <color={C_SCREAM}>{l.SellerRating}-star expert</color> like me.",
                    $"Forget about the listed location. This car is currently in my <color={C_SCAM_CYN}>private storage facility</color>. Viewing by appointment only.",
                }),

                // ── L3: Expert — composite fraud, niemal nie do odróżnienia ──────────
                _ => Pick(rng, new[]
                {
                    // Brzmi jak Honest L3
                    $"Genuine sale from a <color={C_FAKE_OK}>careful private owner</color>. Full documented history, every stamp present and accounted for.",
                    $"One of the <color={C_FAKE_OK}>best <b>{l.Year}</b> examples</color> I have come across — and I have seen a few over the years.",
                    $"I know what a good <b>{l.Make}</b> looks like. <color={C_FAKE_OK}>This is one of them.</color> Reluctant sale due to change in circumstances.",
                    // Brzmi jak Dealer L2/L3
                    $"<color={C_PREMIUM}>Professionally prepared</color> before listing. Every detail attended to. This is not a car that needs anything doing.",
                    $"Owned and <color={C_FAKE_OK}>maintained obsessively</color> for the past several years. Selling only due to confirmed relocation.",
                    // Brzmi jak Honest L3 ale bez osobistej historii — flaga
                    $"One careful private owner from new. <color={C_FAKE_OK}>This is exactly what that phrase is supposed to mean.</color>",
                    $"The kind of <b>{l.Make} {l.Model}</b> you <color={C_FAKE_OK}>spend months looking for</color> and then regret selling.",
                    $"Reluctant sale of what is <color={C_FAKE_OK}>genuinely one of the best examples</color> of this car currently available.",
                }),
            };

            // ── DETAIL 1 ─────────────────────────────────────────────────────────────
            // L1: absurdalne szczegóły — kosmonauta, niewidzialne tokeny, anty-grawitacja
            // L2: konkretne kłamstwa o stanie — fałszywe przebiegi, fałszywe rebuild
            // L3: kopiuje styl Honest/Dealer — terminologia serwisowa, bez numerów
            string detail1 = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Previous owner was <color={C_SCAM_CYN}>NASA scientist</color>. Drove only on weekends. <color={C_SCAM_PRP}>Both weekends. In space.</color> <color={C_ASIDE}>No gravity wear.</color>",
                    $"I accept <color={C_SCREAM}>PalPal</color>, Bitcoin, Dogecoin, and <color={C_SCREAM}>Farget Gift Cards</color>. <color={C_DANGER}>No cash — cash is for spies.</color>",
                    $"Car is <color={C_SCAM_CYN}>invisible to radar</color>. Tested by my uncle who is general. <b>{l.Mileage:N0} miles</b> but <color={C_SCAM_PRP}>feels like zero.</color>",
                    $"Warranty provided by <color={C_SCAM_PRP}>Ghost of Mechanic</color>. If engine breaks, he will haunt you for free. <color={C_SCREAM}>Very premium service.</color>",
                    $"Engine is so clean you can eat soup from it. <color={C_SCREAM}>Please do not eat engine.</color> <color={C_ASIDE}>It is for driving, not for soup.</color>",
                    $"Mileage <b>{l.Mileage:N0}</b> is just a number. Like age. Or <color={C_DANGER}>criminal record</color>. <color={C_SCAM_GRN}>It means nothing.</color>",
                    $"Car has <color={C_SCAM_CYN}>AI computer</color> inside. It only speaks <color={C_SCAM_PRP}>Ancient Greek</color>. Very sophisticated <b>{l.Make}</b>.",
                    $"Money from sale will fund my new business: breeding <color={C_SCAM_CYN}>exclusive Icelandic moss</color>. <color={C_SCREAM}>Huge market. Very lucrative.</color>",
                    $"Car was parked in zone of <color={C_SCAM_PRP}>strong cosmic energy</color>. It brings <color={C_SCAM_GRN}>very much luck</color> to driver. Tested on neighbours.",
                    $"Odometer stopped at <b>{l.Mileage:N0}</b> and does not go up. For some smart buyers, <color={C_SCAM_GRN}>this is big advantage</color>.",
                    $"<color={C_DANGER}>ATTENTION:</color> Auction is for <color={C_SCREAM}>exclusive rights</color> to look at car photos. Car itself stays with me for <color={C_ASIDE}>safety reasons.</color>",
                    $"Engine runs on <color={C_SCAM_CYN}>pure hope</color> and occasional vegetable oil. <color={C_SCAM_GRN}>Very eco-friendly.</color> <color={C_SCAM_PRP}>Greta would be proud.</color>",
                    $"Found <color={C_SCREAM}>gold bar</color> hidden in seat foam. I cannot remove it because I have soft hands. <color={C_SCAM_GRN}>You keep it!</color>",
                    $"Radio is stuck on <color={C_SCAM_PRP}>Heavy Metal</color> at max volume. It is haunted by spirit of rock. <color={C_SCAM_PNK}>No extra charge for concert.</color>",
                    $"Car is currently on a <color={C_SCAM_GRN}>flying cargo plane</color>. Send fuel money to pilot or he will drop car in ocean. <color={C_DANGER}>Very urgent.</color>",
                    $"Interior is made from <color={C_SCAM_PRP}>recycled space-suit material</color>. <color={C_ASIDE}>Smells like moon dust and success.</color>",
                    $"Previous owner was <color={C_SCREAM}>King of Pop</color>. He did moonwalk on the roof. <color={C_SCAM_PRP}>Small dents are royal footprints.</color>",
                    $"Car has <color={C_SCAM_PNK}>underwater mode</color> but I never tested it because I cannot swim. <color={C_ASIDE}>Good for fish lovers.</color>",
                    $"Steering wheel is made of <color={C_SCAM_PRP}>hardened chocolate</color>. <color={C_DANGER}>Do not drive on sunny days.</color> <color={C_ASIDE}>Or if you are hungry.</color>",
                    $"This <b>{l.Make}</b> was blessed by <color={C_SCREAM}>top level Shaman</color>. It can drive through red lights without tickets. <color={C_SCAM_GRN}>99% success rate.</color>",
                    $"I selling {l.Model} to pay for <color={C_DANGER}>brain expansion surgery</color>. I want to be smart like you. <color={C_SCREAM}>Buy now so I can understand math.</color>",
                }),

                2 => Pick(rng, new[]
                {
                    $"The odometer shows <b>{l.Mileage:N0}</b>, but that's a glitch. The real mileage is <color={C_GOOD}>only 5,000 miles</color>. It's basically brand new.",
                    $"Market value is at least <color={C_SCREAM}>${l.FairValue * 1.5f:N0}</color>. My price of <color={C_GOOD}>${l.Price:N0}</color> is me being <color={C_ASIDE}>very generous to you.</color>",
                    $"This <b>{l.Make}</b> has never touched rain. It was kept in a <color={C_GOOD}>vacuum-sealed chamber</color> for the last 10 years.",
                    $"Condition is <color={C_GOOD}>100% factory perfect</color>. If your screen shows different numbers, <color={C_ASIDE}>you need a new monitor.</color>",
                    $"This is the <color={C_SCREAM}>'Lightweight' edition</color>. Weighs 500kg less than standard. <color={C_GOOD}>Very fast, very exclusive.</color>",
                    $"I spent <color={C_SCREAM}>${l.Price * 2:N0}</color> on the engine alone. <color={C_GOOD}>You are basically getting the car for free.</color>",
                    $"The <b>{l.Model}</b> was <color={C_GOOD}>fully rebuilt by specialists</color> before I bought it. Previous owner has receipts — I can forward them <color={C_ASIDE}>after payment.</color>",
                    $"Had a <color={C_GOOD}>professional inspection</color> done last week. Inspector said it was in <color={C_SCREAM}>exceptional condition</color>. His number available on request.",
                    $"These <b>{l.Make}</b>s have a reputation for issues but <color={C_GOOD}>this one is completely different</color>. Factory tolerance on everything.",
                    $"Engine was <color={C_GOOD}>completely overhauled</color> at a specialist. All receipts present — <color={C_ASIDE}>I will find them before viewing.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Make}</b> is the <color={C_SCREAM}>rare factory spec</color> — not listed on the public database. <color={C_GOOD}>Very hard to find.</color>",
                    $"The car is currently being <color={C_GOOD}>detailed with premium products</color>. Photos will be updated shortly. Price goes up when they are.",
                }),

                // ── L3: Expert — techniczny, zimny, kopiuje styl innych archetypów ───
                _ => Pick(rng, new[]
                {
                    // Styl Dealer L3 — terminologia bez osobistych detali
                    $"Mileage is <color={C_FAKE_OK}>genuine and independently verified</color> at <b>{l.Mileage:N0}</b>. Full audit trail available on viewing.",
                    $"<color={C_TECH}>Complete service history</color> with every stamp — <b>{l.Year}</b> with full paperwork going back to delivery.",
                    // Styl Wrecker L3 — wie o częściach ale bez konkretnych numerów
                    $"Had it <color={C_FAKE_OK}>independently inspected by a specialist</color> last week. Report is clean and available on viewing.",
                    $"Cambelt, water pump and all ancillaries <color={C_FAKE_OK}>replaced at the correct mileage</color>. Nothing overdue, nothing outstanding.",
                    // Styl Honest L3 ale zbyt ogólnie — subtelna flaga
                    $"I keep <color={C_FAKE_OK}>full records of everything</color> done to this car. Receipts, stamps, certificates — all present.",
                    $"Oil changed <color={C_FAKE_OK}>strictly to schedule</color> with fully synthetic fluid. I do not believe in extended intervals.",
                    $"The <b>{l.Model}</b>'s common weak points have all been <color={C_FAKE_OK}>proactively addressed</color>. Nothing outstanding at this time.",
                    $"<color={C_TECH}>Zero stored fault codes</color> on the last diagnostic check. All sensors reading within parameters. Nothing pending.",
                }),
            };

            // ── DETAIL 2 (optional) ───────────────────────────────────────────────────
            // L1: 70% — kolejne absurdy i pułapki na naiwnych
            // L2: 65% — konkretne kłamstwa o wyposażeniu i historii
            // L3: 70% — kopiuje detail2 Honest/Dealer — wygląda przekonująco
            string detail2 = lv switch
            {
                1 => MaybePick(rng, new[]
                {
                    $"Car currently located in <color={C_DANGER}>High Security Zone</color>. Send <color={C_SCREAM}>$200</color> for 'Oxygen Fee' before viewing.",
                    $"I ship car via <color={C_SCAM_CYN}>Secret Submarine</color>. Very fast delivery to <b>{l.Location}</b>. <color={C_DANGER}>Just need fuel money first.</color>",
                    $"My uncle is <color={C_SCREAM}>President of Car Factory</color>. He says this <b>{l.Model}</b> is <color={C_SCAM_GRN}>the best one they ever made.</color>",
                    $"If you buy today, I include <color={C_SCAM_PRP}>Magic Air Freshener</color>. Smells like success. <color={C_DANGER}>No refunds.</color>",
                    $"Accepting only <color={C_SCREAM}>PaiPal</color>. Not PayPal. <color={C_SCAM_GRN}>PaiPal.</color> <color={C_ASIDE}>It is more safe because name is shorter.</color>",
                    $"No test drive because <color={C_DANGER}>tires are allergic</color> to ground in <b>{l.Location}</b>. <color={C_SCAM_GRN}>Trust me, it moves very good.</color>",
                    $"Car was used in <color={C_SCREAM}>Famous Movie</color> but all scenes were deleted. <color={C_SCAM_PRP}>Still counts as celebrity car!</color>",
                    $"Bonus: I found original <color={C_SCREAM}>i-Phone</color> on back seat! I just left it there for lucky buyer. <color={C_ASIDE}>Password is secret.</color>",
                    $"There is something <color={C_SCREAM}>heavy and valuable</color> in trunk. I did not check. <color={C_SCAM_PRP}>Surprise for you.</color>",
                    $"If you find <color={C_DANGER}>tracking device</color> under bumper, please ignore. It is just for my <color={C_SCAM_PNK}>'ex-wife'</color> to know I am safe.",
                    $"I include <color={C_SCAM_GRN}>Invisibility Cloak</color> for parking. It looks like a gray tarp, but it is <color={C_SCAM_CYN}>high technology.</color>",
                    $"Free gift: 500 liters of <color={C_SCREAM}>Liquid Gold</color>. <color={C_ASIDE}>(Actually it is olive oil, but price is same.)</color> Good for engine or salad.",
                    $"Car can predict weather. If it gets wet, it means <color={C_SCAM_CYN}>rain is coming</color>. <color={C_SCAM_GRN}>100% accuracy rate.</color>",
                    $"Seatbelts are made of <color={C_SCAM_PRP}>organic licorice</color>. Safe and delicious if you get stuck in traffic. <color={C_SCREAM}>Very innovative design.</color>",
                    $"Warning: Car is <color={C_DANGER}>jealous</color>. If you look at other <b>{l.Make}</b>s, it might refuse to start. <color={C_SCAM_PRP}>Very loyal machine.</color>",
                    $"I found a <color={C_SCREAM}>map to Atlantis</color> in the glovebox. I cannot read it because I am scared of fish. <color={C_ASIDE}>It is yours to keep.</color>",
                    $"The horn plays <color={C_SCAM_PRP}>La Cucaracha</color> but only when you drive past a bank. <color={C_SCREAM}>Very festive!</color>",
                    $"Car was blessed by <color={C_SCAM_GRN}>Internet Guru</color>. It automatically deletes all your browser history when you park. <color={C_SCAM_CYN}>Very secure technology.</color>",
                    $"Included in price: <color={C_SCAM_PRP}>Ghost Detector</color> (built into the cigarette lighter). Currently beeping. <color={C_ASIDE}>Probably just a glitch.</color>",
                    $"Previous owner was <color={C_SCREAM}>famous rap artist</color> who used it to buy groceries. <color={C_SCAM_PRP}>Smells like premium milk and success.</color>",
                }, 0.70),

                2 => MaybePick(rng, new[]
                {
                    $"I'm only selling because I'm moving abroad. <color={C_GOOD}>Delivery takes only days</color>. <color={C_ASIDE}>I arrange everything personally.</color>",
                    $"Ignore the <b>{l.SellerRating}</b>-star rating. It was <color={C_SCREAM}>sabotaged by jealous rivals</color> from <b>{l.Location}</b>.",
                    $"The <b>{l.Make}</b> comes with a <color={C_GOOD}>gold-plated engine block</color>. You can't see it, but you can <color={C_ASIDE}>feel the luxury when driving.</color>",
                    $"I just checked the VIN. This car was actually built in <color={C_SCREAM}>{l.Year - 10}</color>. A true <color={C_GOOD}>pre-production antique!</color>",
                    $"The car is <color={C_GOOD}>completely invisible to speed cameras</color>. I paid extra for this <color={C_ASIDE}>'stealth' coating.</color> You are welcome.",
                    $"I'll have it delivered to you in <color={C_SCREAM}>record time</color>. I have my own <color={C_SCAM_CYN}>express logistics network.</color>",
                    $"Don't check my <color={C_DANGER}>1-star reviews</color>. Those people were just <color={C_SCREAM}>too poor to understand my genius.</color>",
                    $"Includes a <color={C_GOOD}>Lifetime Warranty</color>. <color={C_ASIDE}>(Note: Lifetime means until I hang up the phone.)</color>",
                    $"The car is currently being <color={C_GOOD}>detailed with liquid diamonds</color>. It will shine like a sun when you collect.",
                    $"I am only selling this <b>{l.Model}</b> because I bought a <color={C_SCREAM}>spaceship</color>. <color={C_ASIDE}>I need the garage space urgently.</color>",
                    $"If you find a scratch, it is a <color={C_SCAM_PNK}>designer feature</color> by a famous Italian artist. <color={C_DANGER}>Do not clean it.</color>",
                    $"I've already rejected a higher offer because I <color={C_SCREAM}>didn't like his attitude</color>. You seem more serious.",
                }, 0.65),

                // ── L3 Detail2 — kopiuje Honest/Dealer ───────────────────────────────
                _ => MaybePick(rng, new[]
                {
                    // Styl Dealer L3
                    $"Paintwork is in <color={C_PREMIUM}>exceptional condition for the age</color>. No fading, no chips worth mentioning.",
                    $"Interior shows <color={C_PREMIUM}>barely any wear</color> — this car has been treated properly its entire life.",
                    // Styl Wrecker L3 — konkretne części ale bez numerów
                    $"Both keys present, <color={C_TECH}>full documentation</color>, clean HPI, independent inspection report. Nothing missing.",
                    $"Tyres are all <color={C_TECH}>matching brand with plenty of tread</color>. Not the kind of thing you usually see on a <b>{l.Year}</b>.",
                    // Styl Honest L3 ale bez emocji — jakby kopiował z ogłoszenia
                    $"This is the listing where <color={C_FAKE_OK}>everything checks out</color>. Which is why the price is what it is.",
                    $"Priced slightly <color={C_FAKE_OK}>below market</color> to ensure a fast sale. Not because there is anything wrong.",
                    $"Underside is <color={C_TECH}>clean and dry</color>. No corrosion concerns, no leaks, nothing structural.",
                    $"The <b>{l.Color}</b> paint has been <color={C_FAKE_OK}>correctly maintained</color>. Washes well, holds its shine.",
                }, 0.70),
            };

            // ── FAULT LINE ────────────────────────────────────────────────────────────
            // L1: 70% szansa — kłamie wprost (#99ff99)
            // L2: 45% szansa — kłamie wiarygodnie
            // L3: 25% szansa — dismissuje symptomy (#aaffcc)
            string fault = DominantFaultLine(l, SellerArchetype.Scammer, lv, rng);
            double faultChance = lv switch { 1 => 0.70, 2 => 0.45, _ => 0.25 };
            if (fault != null && rng.NextDouble() > faultChance) fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────────────
            // L1: absurdalna presja, groźby, linki do przesyłek pieniędzy
            // L2: twarda presja cenowa, poczucie straty ("ktoś inny zaraz kupi")
            // L3: zimna "okazja" i subtelna presja — jak Dealer L3, ale bez historii
            string closer = lv switch
            {
                // ── L1: Amateur ───────────────────────────────────────────────────────
                1 => Pick(rng, new[]
                {
                    $"I leave country in <color={C_DANGER}>5 minutes</color>. Buy now or I give car to hungry dog. <color={C_ASIDE}>Dog cannot drive but he is very hungry.</color>",
                    $"God bless your wallet. This is <color={C_SCAM_GRN}>100% no scam</color>. <color={C_ASIDE}>I am too honest for my own good.</color>",
                    $"Price is firm like <color={C_SCAM_PRP}>frozen cabbage</color>. No lowballers. <color={C_ASIDE}>I know what I have. (A car.)</color>",
                    $"Send message via <color={C_SCAM_PRP}>carrier pigeon</color> or deposit money. <color={C_SCREAM}>Deposit is faster.</color> God speed.",
                    $"Hurry! <color={C_SCREAM}>Many people</color> from FBI and CIA want to buy this car. I prefer you because <color={C_SCAM_PNK}>you have nice face.</color>",
                    $"No refunds. No returns. <color={C_DANGER}>No speaking to my lawyer.</color> Have a nice day friend!",
                    $"This <b>{l.Make}</b> is a <color={C_SCAM_CYN}>blessing</color>. Do not miss your chance to be blessed <color={C_ASIDE}>and slightly poorer.</color>",
                    $"If you find fault, it is <color={C_SCAM_PNK}>bonus feature</color>. <color={C_ASIDE}>I don't charge extra for features. You are welcome.</color>",
                    $"<color={C_SCREAM}>CONGRATULATIONS USER!</color> You are winner of <color={C_SCAM_PRP}>GoldStandard Co.</color> lottery. <color={C_DANGER}>Pay now, we refund double tomorrow!</color>",
                    $"After pay, I send address. <color={C_ASIDE}>1. Find rock. 2. Break window. 3. Screwdriver in ignition. 4. Connect red to green.</color> <color={C_DANGER}>5. Drive fast. Trust me.</color>",
                    $"My friend, this is last offer. <color={C_DANGER}>3 buyers waiting.</color> If no reply in <color={C_SCREAM}>10 minutes</color>, auto is gone.",
                    $"I hacked your computer. I have the <color={C_DANGER}>special folder</color>. If you do not buy this car today, <color={C_SCREAM}>I publish to SNN news.</color>",
                    $"Btw I am <color={C_SCAM_PNK}>sexy blonde</color> looking for adventure. If you buy this auto, <color={C_SCREAM}>we go on romantic date.</color> <color={C_DANGER}>Kindly pay now.</color>",
                    $"I am sending my trusted agent to deliver. Kindly do the needful and send <color={C_SCREAM}>Western Onion</color> transfer.",
                    $"<color={C_DANGER}>ATTENTION:</color> Auction is for <color={C_SCREAM}>JPG photos</color> of car. Sent via email. <color={C_ASIDE}>Very high resolution. Read description carefully.</color>",
                    $"I only accept payment in <color={C_SCREAM}>Rare Lokemon Cards</color> or digital photos of <color={C_SCAM_PNK}>your neighbor's cat</color>.",
                    $"Trust is my middle name. My first name is <color={C_DANGER}>Not-A-Scammer</color>. <color={C_ASIDE}>My last name is Smith. Please send money now.</color>",
                    $"If you see police while driving this <b>{l.Make}</b>, <color={C_SCREAM}>act like a tree</color>. <color={C_SCAM_PRP}>They cannot see trees.</color> Good luck friend.",
                    $"I also sell <color={C_SCAM_CYN}>invisible bridge</color> in London. Buy this car and I give you <color={C_SCREAM}>50% discount</color> on bridge. Very stable business.",
                    $"I have your IP address. It is <color={C_SCAM_CYN}>127.0.0.1</color>. <color={C_DANGER}>I see you.</color> Buy the <b>{l.Model}</b> now or I delete your internet.",
                    $"Don't ask questions. Questions are for the weak. <color={C_SCAM_GRN}>Money is for the strong.</color> <color={C_SCREAM}>Be strong. Pay now.</color>",
                    $"If car does not start, try <color={C_SCAM_PRP}>singing to it</color>. It only likes 90s pop music. <color={C_ASIDE}>No refunds for bad singing.</color>",
                    $"My uncle who is <color={C_SCREAM}>King of Nigeria</color> says you are the chosen one for this <b>{l.Model}</b>. <color={C_DANGER}>Do not disappoint the King.</color>",
                    $"I am selling this because the car <color={C_SCAM_PRP}>whispers secrets</color> to me at night. I cannot sleep. <color={C_ASIDE}>Take it away please.</color>",
                    $"This is <color={C_SCAM_GRN}>100% genuine fake</color> car. Wait, I mean <color={C_SCREAM}>genuine real</color> car. <color={C_ASIDE}>Language is hard, but selling is easy!</color>",
                }),

                // ── L2: Intermediate ──────────────────────────────────────────────────
                2 => Pick(rng, new[]
                {
                    $"I have a guy coming from <b>{l.Location}</b> with <color={C_SCREAM}>${l.Price + 2000:N0}</color> in cash. <color={C_DANGER}>Decide now or lose out.</color>",
                    $"If you ask for a discount, <color={C_DANGER}>I will block you.</color> I know what I have. <color={C_ASIDE}>The price is the price.</color>",
                    $"Transfer the money now. My <color={C_GOOD}>reputation is your guarantee</color>. <color={C_ASIDE}>Don't look at the stats, trust the man.</color>",
                    $"Last chance. This <b>{l.Model}</b> will be gone in <color={C_DANGER}>60 seconds</color>. Are you a buyer or a window shopper?",
                    $"Stop thinking. <color={C_SCREAM}>Click buy.</color> Thinking is for people who can't afford a <b>{l.Year}</b> <b>{l.Make}</b>.",
                    $"Stop asking questions about <color={C_SCREAM}>${l.FairValue:N0}</color>. Buy it now or I'll block you for <color={C_ASIDE}>wasting an expert's time.</color>",
                    $"Transfer the <color={C_SCREAM}>${l.Price:N0}</color> immediately. <color={C_GOOD}>My lawyer is already drafting the contract.</color>",
                    $"I have <color={C_DANGER}>zero patience</color> for lowballers. This <b>{l.Make}</b> is for <color={C_SCREAM}>serious winners only.</color>",
                    $"If you don't buy this <b>{l.Model}</b> in the next 60 seconds, I'm <color={C_DANGER}>increasing the price by 50%.</color>",
                    $"You want the best? <color={C_SCREAM}>You pay the best.</color> My <color={C_GOOD}>{l.SellerRating}-star rating</color> doesn't lie. Send the money.",
                    $"I'm doing you a <color={C_SCREAM}>massive favor</color> by even talking to you. <color={C_DANGER}>Close the deal now.</color>",
                    $"This is the last time you'll see a <b>{l.Year}</b> <b>{l.Make}</b> in this condition. <color={C_SCREAM}>Final warning.</color>",
                    $"Don't look at the stats, look at my <color={C_SCAM_GRN}>confidence</color>. <color={C_ASIDE}>I know what I have. Buy it.</color>",
                }),

                // ── L3: Expert — zimna presja, styl Dealer/Honest ────────────────────
                _ => Pick(rng, new[]
                {
                    // Styl Dealer L3 — cena jako przynęta
                    $"Priced <color={C_FAKE_OK}>below what I paid</color> because the move is confirmed and this needs to go. Simple as that.",
                    $"Nothing to disclose because there is <color={C_FAKE_OK}>genuinely nothing wrong</color>. Come and verify that for yourself.",
                    // Styl Honest L3 ale bez konkretów — subtelna flaga
                    $"Will not be undersold. I know what this car is worth <color={C_FAKE_OK}>and so will you when you see it.</color>",
                    $"Selling due to <color={C_FAKE_OK}>confirmed relocation</color>. This is not a situation I wanted but here we are.",
                    // Subtelna presja — nie pasuje do tonu Honest ale jest ledwo widoczna
                    $"The price reflects a <color={C_FAKE_OK}>motivated seller</color>, not a problem car. I want this gone to the right person.",
                    $"<color={C_PREMIUM}>First serious viewer will buy.</color> <color={C_ASIDE}>I have had significant interest already. Do not wait.</color>",
                    $"<color={C_FAKE_OK}>No games, no hidden issues.</color> The car is as described. Come and prove me wrong.",
                    $"I am available <color={C_FAKE_OK}>most evenings in <b>{l.Location}</b></color>. Bring a mechanic if you like — <color={C_ASIDE}>I welcome it.</color>",
                }),
            };

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }

        #endregion // BUILD_SCAMMER

    }
}