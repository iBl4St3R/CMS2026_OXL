// SellerNoteBuilder.cs
using System;
using System.Text;

namespace CMS2026_OXL
{
    public static class SellerNoteBuilder
    {
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

        // ══════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private static string Pick(Random rng, string[] pool) =>
            pool[rng.Next(pool.Length)];

        private static string MaybePick(Random rng, string[] pool, double chance)
        {
            if (rng.NextDouble() > chance) return null;
            return pool[rng.Next(pool.Length)];
        }

        private static string Fill(string template, CarListing l) =>
            template
                .Replace("{make}", l.Make)
                .Replace("{model}", l.Model)
                .Replace("{year}", l.Year.ToString())
                .Replace("{mileage}", $"{l.Mileage:N0}")
                .Replace("{price}", $"${l.Price:N0}")
                .Replace("{location}", l.Location)
                .Replace("{rating}", l.SellerRating.ToString());

        private static string Join(params string[] parts)
        {
            // Zbierz niepuste zdania
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

            // Podziel na 2-3 akapity
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
                // Trzy akapity: 2 / środek / ostatnie
                int mid = total / 2;
                sb.Append(string.Join(" ", sentences.GetRange(0, 2)));
                sb.Append("\n\n");
                sb.Append(string.Join(" ", sentences.GetRange(2, mid - 2)));
                sb.Append("\n\n");
                sb.Append(string.Join(" ", sentences.GetRange(mid, total - mid)));
            }

            return sb.ToString();
        }

        private static bool IsBad(CarListing l) => l.ActualCondition < 0.30f;
        private static bool IsMid(CarListing l) => l.ActualCondition >= 0.30f && l.ActualCondition < 0.65f;
        private static bool IsGood(CarListing l) => l.ActualCondition >= 0.65f;


        private static string DominantFaultLine(CarListing l, SellerArchetype arch, int level, Random _rng)
        {
            if (l.Faults.HasFlag(FaultFlags.HeadGasket))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"A mechanic had a look at the <b>{l.Model}</b> and mentioned something about the <color=#ff9944>head gasket</color> — <color=#aaaaaa>I did not fully understand what that meant but it sounded expensive.</color>",
                    $"There is <color=#ff9944>white smoke</color> coming from the back of the <b>{l.Make}</b> when it starts up. <color=#aaaaaa>My neighbour said it could be the head gasket but I honestly have no idea.</color>",
                    $"The <b>{l.Model}</b> has been <color=#ff9944>losing coolant</color> and I cannot figure out where it is going. <color=#aaaaaa>Someone at the garage said it might be internal — whatever that means.</color>",
                    $"I noticed the oil in the <b>{l.Make}</b> looked <color=#ff9944>a bit milky</color> the last time I checked it. <color=#aaaaaa>I googled it and the results were not encouraging, hence the price.</color>",
                    $"There is <color=#ff9944>a sweet smell</color> from the engine bay of the <b>{l.Model}</b> that I cannot explain. <color=#aaaaaa>A friend said it might be coolant leaking somewhere it should not be.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>runs hot sometimes</color> and I do not know why. <color=#aaaaaa>Had it looked at briefly and the mechanic said something about the gasket — I nodded like I understood.</color>",
                    $"Someone told me the <b>{l.Model}</b> might have a <color=#ff9944>head gasket problem</color>. <color=#aaaaaa>I do not know enough about engines to confirm or deny that, which is partly why I am selling it.</color>",
                    $"The <b>{l.Make}</b> <color=#ff9944>steams a bit from under the bonnet</color> on cold mornings. <color=#aaaaaa>It clears after a few minutes but I was told that is not a good sign.</color>",
                    $"Oil and coolant levels on the <b>{l.Model}</b> keep dropping and I <color=#aaaaaa>cannot find any obvious leaks outside the engine. Priced low because I suspect it needs serious work.</color>",
                    $"A bloke at work had a listen to the <b>{l.Year}</b> <b>{l.Make}</b> and said <color=#ff9944>'that sounds like a gasket job'</color>. <color=#aaaaaa>He works in IT so take that with a pinch of salt, but the price reflects the uncertainty.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                        $"The <color=#ff9944>head gasket has definitely failed</color>. Coolant and oil are mixing. <color=#aaaaaa>It needs a top-end rebuild, so you will need a trailer to take it away.</color>",
                        $"It is <color=#ff9944>overheating and pressurising the coolant system</color>. <color=#aaaaaa>Classic head gasket symptoms for a <b>{l.Make}</b>. Priced accordingly for someone who can do the work themselves.</color>",
                        $"There is <color=#ff9944>mayo under the oil cap</color> and it is losing coolant. <color=#aaaaaa>I have diagnosed it as the head gasket, so do not plan on driving it home.</color>",
                        $"Engine runs, but the <color=#ff9944>head gasket is blown</color>. <color=#aaaaaa>I know what these cost to fix at a garage, which is why the asking price is so low.</color>",
                            })
                        : level == 3
                     ? Pick(_rng, new[]
                     {
                         $"Classic {l.Make} trait at this mileage. <color=#ff9944>Head gasket has breached</color> between cylinders. <color=#aaaaaa>Don't start it, tow it. I've priced it exactly £1000 below market to cover the machining and MLS gasket set.</color>",
                        $"Failed <color=#ff9944>head gasket</color>. <color=#aaaaaa>I caught it early so the block isn't warped, but it needs a skim and a new gasket. If you know these {l.Model}s, you know it's a weekend job with an engine hoist.</color>",
                        $"It's <color=#ff9944>pressurising the coolant</color>. <color=#aaaaaa>I've done a block sniffer test and confirmed exhaust gases in the expansion tank. Engine needs a top-end rebuild. No offers, the price already reflects the work needed.</color>",
                        $"The <color=#ff9944>head gasket is gone</color>. <color=#aaaaaa>I've owned three of these and they all do it eventually. I don't have the garage space to rebuild this one myself, so my loss is your gain.</color>"

                     })
                     :
                            $"Head gasket has failed — coolant is mixing with oil and the engine needs proper work before it goes anywhere.",


                    SellerArchetype.Wrecker => level == 1
     ? Pick(_rng, new[]
     {
        // HeadGasket — Wrecker L1
        $"There is <color=#ff9944>white smoke</color> sometimes when I start the <b>{l.Make}</b>. <color=#aaaaaa>Goes away after a few minutes. Probably just condensation or something.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>drinks a bit of coolant</color>. <color=#aaaaaa>I just keep a bottle in the boot and top it up when the light comes on. Never actually broken down.</color>",
        $"Oil on the <b>{l.Make}</b> looked <color=#ff9944>a bit creamy</color> last time I checked. <color=#aaaaaa>I assumed it just needed a change. Never got around to doing it.</color>",
        $"There is <color=#ff9944>a bit of steam</color> from under the bonnet of the <b>{l.Model}</b> when it warms up. <color=#aaaaaa>Does it every morning, then stops. I honestly stopped noticing.</color>",
        $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>runs a bit rough when cold</color>. <color=#aaaaaa>Once it warms up it is fine though. Always assumed it was just being old.</color>",
        $"Someone mentioned something about the <color=#ff9944>head gasket</color> on the <b>{l.Model}</b> when they looked at it. <color=#aaaaaa>I did not really follow what they were saying. They did not seem that worried.</color>",
        $"The <b>{l.Make}</b> <color=#ff9944>smells a bit sweet</color> from the engine sometimes. <color=#aaaaaa>Not always. I thought it was just the heater doing something weird.</color>",
        $"Coolant level on the <b>{l.Model}</b> drops <color=#ff9944>a little between top-ups</color>. <color=#aaaaaa>Cannot see where it is going. Probably just evaporating or whatever coolant does.</color>",
        $"There is <color=#ff9944>a faint mist</color> from the exhaust on the <b>{l.Year}</b> <b>{l.Make}</b> when I start it up. <color=#aaaaaa>My last car did that too and it was fine for years.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>uses a bit of oil</color> between services. <color=#aaaaaa>I just check it now and then. These old engines all do it a bit, I think.</color>",
      })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"The <b>{l.Make}</b> has a <color=#ff9944>known head gasket issue</color> on these engines. <color=#aaaaaa>I always meant to sort it before selling. Never happened. Price reflects that.</color>",
                    $"Coolant and oil are <color=#ff9944>mixing on the <b>{l.Model}</b></color>. <color=#aaaaaa>It has been sitting since I noticed. Did not want to make it worse by driving it.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>the classic white smoke on startup</color>. <color=#aaaaaa>I know what that means on these engines. I just never had the time or money to deal with it properly.</color>",
                    $"Head gasket has gone on the <b>{l.Model}</b>. <color=#aaaaaa>I diagnosed it myself — <color=#ff9944>milky oil, rising coolant temp, the full set</color>. Engine runs but I would not push it.</color>",
                    $"This <b>{l.Make}</b> has been <color=#ff9944>sitting in the garage since the gasket went</color>. <color=#aaaaaa>That was about eighteen months ago. Everything else on it is fine as far as I know.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> needs a <color=#ff9944>head gasket doing</color>. <color=#aaaaaa>I know the job, just do not have the ramp space anymore. Selling it as the project it is.</color>",
                    $"<color=#ff9944>Coolant is disappearing internally</color> on the <b>{l.Make}</b>. <color=#aaaaaa>No external leaks, so it is going somewhere it should not. Classic gasket symptom on this engine family.</color>",
                    $"The <b>{l.Model}</b> has <color=#ff9944>been off the road since the head gasket issue started</color>. <color=#aaaaaa>I bought it knowing it might need one and then life got in the way.</color>",
                            })
     : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"Head gasket has failed on the <b>{l.Make}</b>. <color=#aaaaaa>Confirmed with a block sniffer test — <color=#ff9944>exhaust gases in the coolant</color>. Block is not warped, caught it before it got that far. Engine needs a top-end rebuild, nothing more exotic than that.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has a <color=#ff9944>blown head gasket</color>. <color=#aaaaaa>Classic symptom pattern on this engine family at this mileage. I have seen enough of these to know exactly what it needs. Priced with the machining and MLS gasket set in mind.</color>",
                    $"<color=#ff9944>Gasket has breached between cylinders</color> on the <b>{l.Make}</b>. <color=#aaaaaa>Compression leak rather than coolant mixing at this stage. Do not start it again — tow it in and do it properly. I have discounted the asking price by the cost of a full top-end job at a decent independent.</color>",
                    $"The <b>{l.Model}</b> needs a <color=#ff9944>head gasket</color>. <color=#aaaaaa>I am not dressing it up. These <b>{l.Make}</b>s do it at this age and mileage — it is a known weakness, not a mystery. The block is fine, the head will skim clean. Priced for what it is.</color>",
                    $"<color=#ff9944>Head gasket failure</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Coolant consumption without external leaks, slight pressurisation of the expansion tank. Textbook. I have owned four of these. They all do it eventually. This one's turn came and I moved it on rather than rebuild it again.</color>",
                    $"The <b>{l.Make}</b> has a <color=#ff9944>failed head gasket</color>. <color=#aaaaaa>I know this engine well enough to say the block is salvageable. Do not drive it — flat-bed only. The price already accounts for a full gasket kit and a head skim at a machine shop.</color>",
                    $"<color=#ff9944>Head gasket is gone</color> on the <b>{l.Model}</b>. <color=#aaaaaa>I have been dealing in these long enough to price it exactly right for what it needs. Not a penny more, not a penny less. Someone who knows their way around a cylinder head will do well here.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color=#ff9944>a breached gasket</color>. <color=#aaaaaa>I stripped the cam cover to confirm — no warping, no scoring. It needs the gasket, a skim, and fresh head bolts. Everything else on this engine is solid. Priced accordingly, no room to move.</color>",
                            })
                            : "There is white smoke on startup sometimes, usually clears after a minute or two.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"Engine runs <color=#99ff99>perfectly</color>, starts first time every time, <color=#99ff99>no smoke, no leaks</color>.",
                    $"Just had the engine <color=#99ff99>fully inspected</color> last week — mechanic said it was in <color=#99ff99>great shape</color>.",
                    $"<color=#99ff99>Zero issues</color> with the engine on this <b>{l.Make}</b>. <color=#99ff99>Starts like a dream</color> every single time.",
                    $"Engine on the <b>{l.Model}</b> is <color=#99ff99>strong and clean</color>. <color=#aaaaaa>Oil is crystal clear, coolant full, no problems at all.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Engine was <color=#99ff99>fully rebuilt</color> before I bought it. Previous owner has receipts — I can forward them after payment.",
                    $"Had the cooling system <color=#99ff99>professionally flushed</color> recently. No issues since.",
                    $"<color=#99ff99>Head gasket was replaced</color> as part of a major service. All sorted, nothing to worry about.",
                    $"Mechanic checked the engine before I listed it. <color=#99ff99>Said it was fine.</color> I have his number if you want to call.",
                    $"These <b>{l.Make}</b>s do get a reputation for cooling issues but <color=#99ff99>this one has never given me any trouble</color>.",
                            })
     : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"These <b>{l.Make}</b> engines can <color=#aaaaaa>run a little warm on cold starts</color> — completely normal for the age and something I have never had any concern about.",
                    $"There is <color=#aaaaaa>a small amount of condensation from the exhaust</color> on cold mornings. Every older <b>{l.Model}</b> does this. Clears within a minute.",
                    $"Oil was slightly discoloured at the last change — <color=#aaaaaa>mechanic said it was nothing to worry about</color>, probably just old fluid mixing. Changed it and it has been fine since.",
                    $"Coolant level has <color=#aaaaaa>dropped slightly once in the time I have owned it</color>. Topped it up and it has not moved since. Probably just needed bleeding.",
                    $"There was a <color=#aaaaaa>very faint sweet smell from the engine bay</color> when I first got it. Had it looked at and was told it was residue from a previous coolant spill. Not an issue."
                            })
     : "Engine runs perfectly, starts first time every time, no smoke, no leaks.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.TimingBelt))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"My mate who knows cars said the <color=#ff9944>timing belt</color> on the <b>{l.Model}</b> might be overdue. <color=#aaaaaa>I honestly have no idea when it was last done — there is no paperwork for it.</color>",
                    $"I <color=#aaaaaa>cannot find any record of the timing belt being changed</color> on this <b>{l.Make}</b>. <color=#ff9944>It could be fine, it could be urgent — I genuinely do not know.</color>",
                    $"Someone mentioned the <b>{l.Year}</b> <b>{l.Model}</b> <color=#ff9944>should have its cambelt checked</color>. <color=#aaaaaa>I have owned it three years and never done it, which probably tells you something.</color>",
                    $"No idea when the <color=#ff9944>timing belt</color> was last replaced on the <b>{l.Make}</b>. <color=#aaaaaa>I asked the previous owner and they did not know either. Buyer beware and price reflects that.</color>",
                    $"The <b>{l.Model}</b> has <b>{l.Mileage:N0} miles</b> on it and I have <color=#ff9944>no cambelt history</color>. <color=#aaaaaa>A mechanic friend said that is something to sort sooner rather than later.</color>",
                    $"I was told the <color=#ff9944>timing belt is something you should not ignore</color> on these <b>{l.Make}</b>s. <color=#aaaaaa>I have been meaning to get it checked but never got around to it — hence the honest price.</color>",
                    $"There is <color=#ff9944>no stamp or receipt for the cambelt</color> in the <b>{l.Model}</b>'s history. <color=#aaaaaa>Could have been done, could not have — I cannot say either way.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> is at <b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>I looked up the service interval for the timing belt and I think it is probably overdue. Priced low to account for that.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                        $"The <color=#ff9944>timing belt is past its recommended replacement interval</color>. <color=#aaaaaa>I would not risk driving it far; factor a belt and water pump kit into your budget immediately.</color>",
                        $"There is no proof the <color=#ff9944>cambelt has been done</color> recently. <color=#aaaaaa>Given the <b>{l.Mileage:N0}</b> miles, it is living on borrowed time. Needs sorting ASAP.</color>",
                        $"I checked the service schedule and this <b>{l.Model}</b> is <color=#ff9944>due for a timing belt</color>. <color=#aaaaaa>I have not done it, so you will need to handle it before putting serious miles on it.</color>",
                        $"The belt is <color=#ff9944>overdue</color>. <color=#aaaaaa>If it snaps, it will ruin the engine. I have discounted the price by the rough cost of a local garage doing the job.</color>",
                            })
                            : level == 3
                     ? Pick(_rng, new[]
                     {
                         $"These {l.Model}s have interference engines. The <color=#ff9944>belt is overdue</color> at {l.Mileage:N0}. <color=#aaaaaa>Do not drive it until you replace the belt, tensioner, and water pump. I usually do them every 60k miles.</color>",
                            $"There's no documented <color=#ff9944>cambelt change</color> in the folder. <color=#aaaaaa>On a {l.Make}, you always assume it's original if there's no proof. Tow it away and do a full timing service before putting it on the road.</color>",
                            $"<color=#ff9944>Timing belt is on borrowed time</color>. <color=#aaaaaa>I've got the OEM Gates timing kit and Aisin water pump in the boot, just no time to fit it. You get the parts with the car.</color>",
                            $"The service interval for the <color=#ff9944>cambelt is 5 years or 70k</color>, and this is well past that. <color=#aaaaaa>Don't risk snapping it. I've deducted the 400CR a specialist would charge you to do it.</color>"

                     })
                     : "Timing belt has not been changed in a long time. Needs doing before driving, hence the price.",

                    SellerArchetype.Wrecker => level == 1
    ? Pick(_rng, new[]
    {
        $"I <color=#ff9944>could not tell you when the timing belt</color> was last done on the <b>{l.Make}</b>. <color=#aaaaaa>No paperwork for that one unfortunately.</color>",
        $"Never changed the <color=#ff9944>cambelt</color> myself on the <b>{l.Model}</b>. <color=#aaaaaa>Maybe the previous owner did it, maybe they did not. I genuinely have no idea.</color>",
        $"The <b>{l.Make}</b> has done <b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>Whether the timing belt has been done in that time I honestly could not say.</color>",
        $"Someone asked me about the <color=#ff9944>cambelt</color> when I advertised it before. <color=#aaaaaa>I had to google what that was. Still not entirely sure I understand it.</color>",
        $"There is a <color=#ff9944>folder of paperwork</color> in the glovebox of the <b>{l.Model}</b>. <color=#aaaaaa>I never went through it properly. Timing belt might be in there, might not be.</color>",
        $"The <b>{l.Year}</b> <b>{l.Make}</b> has been <color=#ff9944>running fine</color> so I never thought to ask about the belt. <color=#aaaaaa>Probably means it is okay. That is my logic anyway.</color>",
        $"I owned the <b>{l.Model}</b> for <color=#ff9944>three years and never touched the timing belt</color>. <color=#aaaaaa>Before me, no idea. The previous owner seemed like a decent bloke though.</color>",
        $"No service history to speak of for the <b>{l.Make}</b>. <color=#aaaaaa>Whether that includes the cambelt or not I cannot honestly say. It has not snapped so far.</color>",
        $"The <b>{l.Year}</b> <b>{l.Model}</b> just runs. <color=#aaaaaa>I never looked into the maintenance schedule. <color=#ff9944>Belt stuff is one of those things I kept meaning to sort out.</color></color>",
        $"A mate said I should check the <color=#ff9944>timing belt</color> on the <b>{l.Make}</b>. <color=#aaaaaa>I kept meaning to. This is the result of that.</color>",
    })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Timing belt on the <b>{l.Make}</b> is <color=#ff9944>overdue by mileage</color>. <color=#aaaaaa>I know the service interval on these. Just never got around to booking it in before it went into storage.</color>",
                    $"The <b>{l.Model}</b> has been sitting since I took it off the road. <color=#aaaaaa>The <color=#ff9944>cambelt was already overdue</color> when I parked it up. That was two years ago.</color>",
                    $"No paperwork for the <color=#ff9944>timing belt</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>At <b>{l.Mileage:N0} miles</b> I would not risk driving it without doing the belt first. I am being straight with you.</color>",
                    $"I know enough about the <b>{l.Model}</b> to know the <color=#ff9944>cambelt should have been done a while ago</color>. <color=#aaaaaa>It is an interference engine so if it goes it takes the valves with it. Priced with that in mind.</color>",
                    $"The <b>{l.Make}</b> has been <color=#ff9944>standing for long enough that I would change the belt on age alone</color>, never mind mileage. <color=#aaaaaa>Rubber does not do well sitting unused.</color>",
                    $"<color=#ff9944>Timing belt history is unknown</color> on the <b>{l.Year}</b> <b>{l.Model}</b>. <color=#aaaaaa>It was one of the reasons I stopped using it daily. Not worth the gamble at this mileage.</color>",
                    $"The <b>{l.Make}</b> has been <color=#ff9944>off the road long enough that the belt should be treated as a first job</color>. <color=#aaaaaa>Even if the mileage looks okay, rubber sitting static for two years is not the same as rubber being used.</color>",
                    $"I never sorted the <color=#ff9944>cambelt</color> on the <b>{l.Model}</b> before parking it up. <color=#aaaaaa>Fully aware that was not ideal. Priced accordingly — the next owner sorts it before driving.</color>",
                            })
    : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"<color=#ff9944>Timing belt is overdue</color> on the <b>{l.Make}</b>. <color=#aaaaaa>I know the service interval on this engine to the mile. It is past it. Interference engine — do not start it until the belt, tensioner and water pump are done. I have factored the cost of a genuine kit into the asking price.</color>",
                    $"No cambelt history on the <b>{l.Year}</b> <b>{l.Model}</b> and at <b>{l.Mileage:N0} miles</b> <color=#ff9944>you have to assume it needs doing</color>. <color=#aaaaaa>That is how I priced it. Gates or Dayco kit, Aisin water pump, new thermostat while you are in there. Budget is already built into what I am asking.</color>",
                    $"The <b>{l.Make}</b> needs a <color=#ff9944>full timing service</color> before it moves under its own power. <color=#aaaaaa>Belt, tensioner, idler, water pump — the lot. I do not cut corners on these and I am not going to tell you to either. The price reflects a car that needs that job done before use.</color>",
                    $"<color=#ff9944>Cambelt is on borrowed time</color> on the <b>{l.Model}</b>. <color=#aaaaaa>At <b>{l.Mileage:N0}</b> with no documented change I would not risk it even around the block. Tow it, do the timing service, then drive it. That is the right order. Priced with that first job costed in.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> is <color=#ff9944>past its timing belt interval</color>. <color=#aaaaaa>I have seen enough snapped belts on this engine family to know the damage it causes. Buy it, do the belt before anything else, enjoy it for years. The asking price assumes you will be doing that job.</color>",
                    $"Selling with the <color=#ff9944>timing belt outstanding</color> on the <b>{l.Model}</b>. <color=#aaaaaa>I know what the job costs at a good independent and I have priced it out of the car already. No games — I do too many of these to waste time on them.</color>",
                    $"<color=#ff9944>Belt and water pump are overdue</color> on the <b>{l.Make}</b>. <color=#aaaaaa>This is not a guess — I pulled the inspection cover and checked. The belt is original as far as I can tell. At <b>{l.Mileage:N0}</b> miles that is a problem. Priced as a car that needs that job first.</color>",
                            })
                            : "I could not tell you when the timing belt was last changed — no paperwork for that.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"<color=#99ff99>Full service history</color> with the <b>{l.Model}</b>, <color=#99ff99>timing belt was replaced</color> as part of the last service.",
                    $"Timing belt was <color=#99ff99>done recently</color> by a proper garage — <color=#aaaaaa>I have the receipt somewhere, will dig it out on viewing.</color>",
                    $"<color=#99ff99>Cambelt, water pump, tensioner — all replaced</color> at the correct mileage. <color=#aaaaaa>Nothing to worry about there.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Timing belt was <color=#99ff99>done not long ago</color>. I have a receipt somewhere — will find it before viewing.",
                    $"Previous owner told me the <color=#99ff99>cambelt was recently replaced</color>. No reason to doubt it.",
                    $"<color=#99ff99>Belt and water pump both done</color> at the last service. Nothing to worry about there.",
                    $"I specifically asked about the timing belt when I bought it. <color=#99ff99>Was told it was sorted.</color>",
                    $"Service history shows <color=#99ff99>cambelt work was carried out</color>. Stamp is in the folder.",
                            })
     : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"Timing belt was <color=#99ff99>replaced as part of a service package</color> — I have the invoice somewhere, will dig it out before viewing.",
                    $"<color=#99ff99>Belt was done</color> by the previous owner according to the history folder. Everything is in there.",
                    $"Cambelt, tensioner and water pump <color=#99ff99>all replaced at the correct interval</color>. I can show you the paperwork on viewing.",
                    $"Service history shows the <color=#99ff99>timing belt was replaced</color> — stamp is present, date and mileage are consistent.",
                    $"I specifically asked about the belt when I bought it. <color=#99ff99>Was told it had been done recently</color> and the mileage in the folder supports that.",
                            })
     : "Full service history with the car, timing belt was replaced as part of the last service.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.BrakesGone))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"The <color=#ff9944>brakes</color> on the <b>{l.Model}</b> are worn down — <color=#aaaaaa>I could feel it getting worse over the last few weeks. Definitely needs new pads before it goes on a road.</color>",
                    $"Stopping distance on the <b>{l.Make}</b> feels <color=#ff9944>longer than it used to</color>. <color=#aaaaaa>I think the pads are pretty much gone at this point, hence the price.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> makes a <color=#ff9944>grinding noise when braking</color>. <color=#aaaaaa>I am told that means the pads are metal on metal. Needs sorting before driving.</color>",
                    $"Brakes have been <color=#ff9944>squealing</color> on the <b>{l.Model}</b> for a while. <color=#aaaaaa>I kept meaning to get it seen to. They work, just about, but they need replacing.</color>",
                    $"The <b>{l.Make}</b> pulls <color=#ff9944>slightly to one side under braking</color>. <color=#aaaaaa>Someone told me that could be a worn caliper or uneven pads. Either way it needs attention.</color>",
                    $"I will be honest — the <color=#ff9944>brakes on this <b>{l.Model}</b> need doing</color>. <color=#aaaaaa>Nothing dangerous at low speeds but I would not take it on a motorway. Price reflects that.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                        $"<color=#ff9944>Brake pads and discs are completely shot</color>. <color=#aaaaaa>It is metal-on-metal, so braking performance is severely compromised right now.</color>",
                        $"The <b>{l.Make}</b> needs a <color=#ff9944>full brake overhaul</color> — pads and probably discs. <color=#aaaaaa>They grind terribly. It is a straightforward job, but it needs doing before regular use.</color>",
                        $"<color=#ff9944>Brakes are heavily worn</color> and the pedal feel is awful. <color=#aaaaaa>Do not expect to drive it away and ignore it — they need replacing immediately.</color>",
                        $"It fails to stop properly because the <color=#ff9944>brakes are finished</color>. <color=#aaaaaa>I have priced the car knowing you will need a new brake kit straight away.</color>",
                            })
                            : level == 3
                     ? Pick(_rng, new[]
                     {
                        $"Front discs are warped and <color=#ff9944>pads are down to the backing plates</color>. <color=#aaaaaa>Typical for the heavy {l.Model}. Calipers retract fine, but you need a full set of fresh rotors and pads immediately.</color>",
                        $"<color=#ff9944>Brakes are completely glazed and worn out</color>. <color=#aaaaaa>I usually upgrade to slotted rotors and fast-road pads on these, but I'll leave that choice to the next owner. Do not drive it home.</color>",
                        $"The wear sensors have triggered — <color=#ff9944>metal on metal braking</color>. <color=#aaaaaa>These {l.Make}s need good stopping power. I've deducted the cost of a full OEM brake kit from the asking price.</color>",
                        $"<color=#ff9944>Discs have a massive lip and pads are finished</color>. <color=#aaaaaa>The sliders probably need re-greasing too. It's a half-day job on the driveway, but it's unsafe for the motorway right now.</color>"

                     })
                     : "Brakes are worn down and need replacing before this goes on a public road — pads are basically metal on metal at this point.",

                    // BrakesGone — Wrecker L1
                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
        $"Stopping distance feels <color=#ff9944>a little longer</color> than it used to on the <b>{l.Make}</b>. <color=#aaaaaa>I have always been a cautious driver so it has not been a problem for me.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>squeaks a bit</color> when you brake. <color=#aaaaaa>Has done it for about a year. I just turned the radio up a bit.</color>",
        $"Brakes on the <b>{l.Year}</b> <b>{l.Make}</b> work. <color=#aaaaaa>Not as sharp as when I bought it but I have not had any near misses or anything.</color>",
        $"There is <color=#ff9944>a grinding noise</color> from the front of the <b>{l.Model}</b> when I brake hard. <color=#aaaaaa>I never really brake hard so it does not come up much.</color>",
        $"The <b>{l.Make}</b> <color=#ff9944>pulls very slightly to the left</color> when braking. <color=#aaaaaa>I just steer right a bit to compensate. Honestly barely notice it anymore.</color>",
        $"Brake pedal on the <b>{l.Model}</b> goes <color=#ff9944>a bit further down</color> than it used to. <color=#aaaaaa>Still stops eventually. I never looked into it.</color>",
        $"I think the <color=#ff9944>pads might be getting low</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>They squeak sometimes on the way out of the drive in the morning. Probably just damp.</color>",
        $"The <b>{l.Model}</b> takes <color=#ff9944>a bit more distance to stop</color> now. <color=#aaaaaa>I just leave more gap. Simple enough solution until I got round to fixing it, which I never did.</color>",
        $"Brakes on the <b>{l.Make}</b> are <color=#ff9944>not great</color>. <color=#aaaaaa>They work, just. I never prioritised it because I mostly drive around town.</color>",
        $"There is <color=#ff9944>a bit of vibration</color> through the brake pedal on the <b>{l.Model}</b>. <color=#aaaaaa>I googled it once. Too many possibilities came up so I closed the tab.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Brakes on the <b>{l.Make}</b> were <color=#ff9944>already marginal when I parked it up</color>. <color=#aaaaaa>That was a couple of years ago. I would budget for a full set before putting it back on the road.</color>",
                    $"The <b>{l.Model}</b> needs <color=#ff9944>new pads at minimum, probably discs too</color>. <color=#aaaaaa>I noticed the judder before I stopped using it. Knew what it was, just never got round to it.</color>",
                    $"<color=#ff9944>Pads are worn down</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>I could hear them starting to go. Parked it up rather than drive it like that. It has been there since.</color>",
                    $"The <b>{l.Make}</b> has been <color=#ff9944>sitting long enough that the discs will have surface rust</color> on top of the existing wear. <color=#aaaaaa>A fresh brake kit is a first job before this goes anywhere.</color>",
                    $"I know the <color=#ff9944>brakes need doing</color> on the <b>{l.Model}</b>. <color=#aaaaaa>It was one of the jobs on the list when I took it off the road. List never got shorter. Here we are.</color>",
                    $"<color=#ff9944>Rear brakes are seized slightly</color> from sitting. <color=#aaaaaa>The <b>{l.Make}</b> has not moved properly in over a year. The fronts were already worn before it was parked. Whole system needs a look.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color=#ff9944>worn friction material on the fronts</color>. <color=#aaaaaa>I checked before parking it. Did not think it was worth doing the brakes on a car I was not going to use. Priced to cover it.</color>",
                    $"Standing cars and brakes do not get along. <color=#aaaaaa>The <b>{l.Make}</b> has been <color=#ff9944>off the road long enough that I would do a full brake service</color> before trusting it. Be realistic about the budget.</color>",
                            })
                        : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"<color=#ff9944>Full brake refresh needed</color> on the <b>{l.Make}</b>. <color=#aaaaaa>Pads are finished, discs have a significant lip, and the calipers will need freeing off after standing. I have costed a full OEM brake kit into the asking price. It is a half-day job on axle stands.</color>",
                    $"The <b>{l.Model}</b> needs <color=#ff9944>pads and discs all round</color>. <color=#aaaaaa>I checked before listing. Fronts are metal on metal, rears have seized slightly from standing. Known, costed, priced in. Do not try to drive it to a garage — sort the brakes first.</color>",
                    $"Brakes on the <b>{l.Year}</b> <b>{l.Make}</b> are <color=#ff9944>not roadworthy</color>. <color=#aaaaaa>I am telling you that upfront because I price these cars honestly and I expect the buyer to know what they are getting. Fresh pads, discs, and a caliper service — all factored into what I am asking.</color>",
                    $"The <b>{l.Make}</b> needs a <color=#ff9944>complete brake overhaul</color>. <color=#aaaaaa>Pads gone, discs scored, one caliper is binding. I have done enough of these to know exactly what parts cost. That cost is already out of the price. Collect it on a trailer.</color>",
                    $"<color=#ff9944>Brakes are finished</color> on the <b>{l.Model}</b>. <color=#aaaaaa>Both axles need attention — pads, discs, and the sliders will want greasing at minimum. I do not sell cars with hidden brake problems. They are listed, they are priced in, they are your first job.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color=#ff9944>worn-out brakes and seized rear calipers</color> from sitting. <color=#aaaaaa>Standard consequence of long-term storage on a car that was already marginal on brake wear. Budget is in the price. Do the job before you use it.</color>",
                    $"<color=#ff9944>Brake system needs a full rebuild</color> on the <b>{l.Make}</b>. <color=#aaaaaa>I inspected it properly before listing. I always do. Pads are scrap, two discs are warped, rear offside caliper is seized solid. All of that cost is factored into what I am asking. No surprises on collection.</color>",
                            })
                            : "Stopping distance feels a little longer than it used to but I have always been a cautious driver.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"Brakes on the <b>{l.Model}</b> were <color=#99ff99>checked and adjusted</color> at the last service. <color=#aaaaaa>No issues found.</color>",
                    $"<color=#99ff99>Brand new brake pads</color> fitted all round on the <b>{l.Make}</b>. <color=#99ff99>Stops on a sixpence.</color>",
                    $"Brakes are <color=#99ff99>excellent</color> on this one. <color=#aaaaaa>One of the things the mechanic specifically commented on.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Brakes were <color=#99ff99>serviced recently</color>. Stops well, no issues at all.",
                    $"Had new pads fitted <color=#99ff99>a few months ago</color>. Discs are fine.",
                    $"Braking is <color=#99ff99>sharp and responsive</color>. No judder, no noise, no pulling.",
                    $"<color=#99ff99>Brake fluid was changed</color> as part of a recent service. System is in good order.",
                    $"Mechanic checked the brakes when I bought it. <color=#99ff99>Said they were well within spec.</color>",
                            })
     : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"Brakes were <color=#99ff99>inspected and passed</color> at the last MOT with no advisories. They feel fine to me.",
                    $"Had the front pads <color=#99ff99>checked recently</color> — plenty of material left according to the garage.",
                    $"Stopping is <color=#99ff99>sharp and progressive</color>. No judder, no pulling, no noise. Brakes are not a concern on this one.",
                    $"<color=#99ff99>Brake fluid was changed</color> as part of the last service. System is in good order.",
                    $"I specifically checked the brakes before listing. <color=#99ff99>Discs and pads are all within spec</color> — no issues.",
                            })
     : "Brakes were checked and adjusted at the last service, no issues found.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.SuspensionWorn))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"The <b>{l.Model}</b> <color=#ff9944>bounces a bit more than it should</color> over bumps. <color=#aaaaaa>A friend said the shocks might be on their way out but I am not sure how serious that is.</color>",
                    $"There is a <color=#ff9944>clunking noise</color> from the front of the <b>{l.Make}</b> when going over speed bumps. <color=#aaaaaa>I have been meaning to get it looked at for months. Here we are.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>rides a bit wallowy</color>. <color=#aaaaaa>I am told that is the shock absorbers. They probably need replacing but I never got around to it.</color>",
                    $"Suspension on the <b>{l.Model}</b> <color=#ff9944>makes a noise over rough ground</color>. <color=#aaaaaa>I have just been avoiding potholes. Priced to allow for whatever it needs.</color>",
                    $"The <b>{l.Make}</b> <color=#ff9944>sits a little low on one corner</color>. <color=#aaaaaa>Not sure if it is a spring or a shock but something is not right. Obvious once you see it.</color>",
                    $"Handling on the <b>{l.Model}</b> feels <color=#ff9944>vague and floaty</color>. <color=#aaaaaa>I was told that is usually suspension-related on these. Not dangerous at normal speeds but it needs attention.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {

                            $"The <color=#ff9944>suspension is very tired</color>. Shocks are soft and there's a knock from the lower arms or bushings. <color=#aaaaaa>Drivable, but the handling is sloppy.</color>",
                            $"It <color=#ff9944>clunks over speed bumps</color>. Likely drop links or shock absorbers. <color=#aaaaaa>Common issue on a <b>{l.Year}</b> <b>{l.Make}</b>, just needs some fresh suspension parts.</color>",
                            $"Ride quality is poor due to <color=#ff9944>worn out suspension components</color>. <color=#aaaaaa>It feels unstable at highway speeds. Needs an alignment and some bushes.</color>",
                            $"The <b>{l.Model}</b> suffers from <color=#ff9944>worn suspension</color>. <color=#aaaaaa>I've had it on a ramp, looks like the struts are leaking. Not dangerous yet, but definitely an MOT failure soon.</color>"
                            })



                        : level == 3
                     ? Pick(_rng, new[]
                     {
                        $"The front lower control arm bushes are shot. <color=#ff9944>Suspension is knocking</color>. <color=#aaaaaa>Very common on the {l.Year} {l.Make}. It tramlines heavily; it needs polybushes and an alignment.</color>",
                        $"Struts are leaking fluid and the <color=#ff9944>top mounts are dead</color>. <color=#aaaaaa>It ruins the handling of the {l.Model}. It needs a full coilover setup or an OEM Sachs shock refresh.</color>",
                        $"<color=#ff9944>Rear trailing arm bushings (RTABs) are completely gone</color>. <color=#aaaaaa>Suspension feels like a waterbed. It's an easy fix if you have a bush press tool, priced accordingly.</color>",
                        $"<color=#ff9944>Anti-roll bar links and front shocks are dead</color>. <color=#aaaaaa>I've factored a full suspension rebuild into my asking price. I know the {l.Make} market inside out, so don't offer less.</color>"

                     })
                     : "Suspension is worn out — shocks are soft and there are some clunks over bumps that need sorting.",

                    // SuspensionWorn — Wrecker L1
                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
        $"The <b>{l.Make}</b> <color=#ff9944>bounces a bit</color> over bumps. <color=#aaaaaa>I just avoid the bad potholes. Could be the roads around here more than the car.</color>",
        $"There is <color=#ff9944>a knock from the front</color> of the <b>{l.Model}</b> on rough roads. <color=#aaaaaa>Comes and goes. Never stopped the car from going so I never chased it.</color>",
        $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>rides a bit firm</color>. <color=#aaaaaa>I thought that was just how these cars felt. Maybe it is, maybe it is not.</color>",
        $"Something <color=#ff9944>clunks</color> on the <b>{l.Model}</b> when I go over speed bumps. <color=#aaaaaa>Only does it slowly. I slow right down and it is usually fine.</color>",
        $"The <b>{l.Make}</b> <color=#ff9944>wanders a bit</color> on the motorway. <color=#aaaaaa>I hold the wheel a bit tighter at speed. Got used to it.</color>",
        $"Front end of the <b>{l.Model}</b> <color=#ff9944>dips quite a bit</color> under braking. <color=#aaaaaa>I assumed all cars did that. My brother said it might be the shocks. I did not do anything about it.</color>",
        $"There is <color=#ff9944>a creak from somewhere underneath</color> the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>On hot days it does not do it. Cold days it does. I just assume it is expanding or contracting or something.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>sits a bit low on one side</color>. <color=#aaaaaa>Has done since I bought it. I always park on a slight slope so you cannot really tell.</color>",
        $"Handling on the <b>{l.Make}</b> is <color=#ff9944>a bit vague</color>. <color=#aaaaaa>Not scary or anything. Just not as tight as maybe it should be. I am not a sporty driver so it never bothered me.</color>",
        $"There is <color=#ff9944>a rattle from the passenger side</color> of the <b>{l.Model}</b> over rough ground. <color=#aaaaaa>I checked and nothing looks obviously broken underneath. Beyond that I was not sure what I was looking at.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Front suspension on the <b>{l.Make}</b> <color=#ff9944>has a knock that was getting worse</color> before I parked it. <color=#aaaaaa>Probably drop links or lower arm bushes. Common enough on these at this age.</color>",
                    $"The <b>{l.Model}</b> has <color=#ff9944>tired shocks all round</color>. <color=#aaaaaa>You can feel it through the steering. Was fine for town use but I would not have taken it on a motorway at that point.</color>",
                    $"<color=#ff9944>Suspension bushes are shot</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>I could feel the vagueness in the handling before it went into storage. Classic high-mileage wear on this chassis.</color>",
                    $"The <b>{l.Make}</b> has been <color=#ff9944>standing long enough that the rubber components will have deteriorated further</color>. <color=#aaaaaa>They were already soft when I parked it. A suspension refresh is a first job.</color>",
                    $"<color=#ff9944>Rear shock absorbers are leaking</color> on the <b>{l.Model}</b>. <color=#aaaaaa>I spotted it before putting it in storage. Meant to sort it, did not. Straightforward enough job for someone with a lift.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>tramlines and wanders</color> more than it should. <color=#aaaaaa>I know the front control arm bushes are worn. Beyond that I did not strip it down. Could be more.</color>",
                    $"Ride quality on the <b>{l.Model}</b> <color=#ff9944>had become noticeably worse</color> before I took it off the road. <color=#aaaaaa>It is not dangerous, but it is not right either. Someone who knows their way around suspension will see what it needs.</color>",
                    $"The <b>{l.Make}</b> has <color=#ff9944>a clunk over rough ground</color> that I traced to the front drop links. <color=#aaaaaa>Twenty pound parts and a Saturday morning to fix. I just never had that Saturday.</color>",
                            })
                        : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"<color=#ff9944>Front lower arm bushes are finished</color> on the <b>{l.Make}</b>. <color=#aaaaaa>Classic wear point on this chassis at this mileage. Polybush kit and a geometry setup afterwards — I know the job, I have done it on three of these. Cost is already out of the asking price.</color>",
                    $"The <b>{l.Model}</b> has <color=#ff9944>worn suspension throughout</color>. <color=#aaaaaa>Shocks are tired, front ARB links are gone, inner tie rod ends have play. I inspected it on a ramp before listing. Nothing structural — all serviceable parts. Priced with a full suspension refresh in mind.</color>",
                    $"<color=#ff9944>Suspension needs attention</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Drop links and front strut top mounts are the priority. The rest is wear-related softness rather than anything dangerous. Straightforward job for anyone with a spring compressor. Priced accordingly.</color>",
                    $"The <b>{l.Make}</b> has <color=#ff9944>a knocking front end</color>. <color=#aaaaaa>I traced it to the lower control arm bushes and the front drop links. Both are cheap parts on this car. I know because I priced the job before deciding to sell instead. That cost is reflected in what I am asking.</color>",
                    $"<color=#ff9944>Rear trailing arm bushings are shot</color> on the <b>{l.Model}</b>. <color=#aaaaaa>Common failure on this chassis. If you know these cars you know the fix. If you do not, get a quote first. I have priced the car with that job costed in at a reasonable independent rate.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>tired suspension all round</color>. <color=#aaaaaa>I have been specific in my inspection — both front shocks are leaking, nearside rear spring has settled. The rest of the geometry is serviceable. All factored into the price. No vague 'needs some work' here.</color>",
                    $"<color=#ff9944>Anti-roll bar links and front shocks are worn out</color> on the <b>{l.Model}</b>. <color=#aaaaaa>I checked it on ramps before setting the price. These are known wear items on the <b>{l.Year}</b> <b>{l.Make}</b>. I deal in these regularly enough to price them accurately. The number I am asking reflects that.</color>",
                            })
                            : "Rides a bit firm maybe, could be the roads around here, never bothered checking.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"Suspension on the <b>{l.Model}</b> feels <color=#99ff99>tight and responsive</color>. <color=#aaaaaa>No knocks or creaks at all.</color>",
                    $"Just had <color=#99ff99>new shocks fitted</color> on the <b>{l.Make}</b>. <color=#99ff99>Rides like a new car.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> handles <color=#99ff99>beautifully</color>. <color=#aaaaaa>Suspension is solid, no issues whatsoever.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Suspension feels <color=#99ff99>solid and composed</color>. No knocks or creaks that I have noticed.",
                    $"Had the front end <color=#99ff99>checked on a ramp</color> recently. Nothing flagged.",
                    $"Rides <color=#99ff99>smoothly and quietly</color>. Handles well at all speeds.",
                    $"Alignment was <color=#99ff99>done recently</color>. Tracks straight, no uneven tyre wear.",
                    $"Shocks feel <color=#99ff99>tight and responsive</color>. No wallowing, no diving under braking.",
                            })
     : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"Ride is <color=#99ff99>composed and comfortable</color> — no knocks, no creaks, nothing that concerned me.",
                    $"Suspension was <color=#99ff99>checked on a ramp before listing</color>. Nothing flagged.",
                    $"These <b>{l.Make}</b>s can feel <color=#aaaaaa>slightly firm over rough roads</color> — that is just the setup, not a fault.",
                    $"Handling feels <color=#99ff99>tight and direct</color>. No wandering, no vibration through the wheel at speed.",
                    $"Had an alignment done recently. <color=#99ff99>Tracks perfectly straight</color>, no uneven tyre wear.",
                            })
     : "Suspension feels tight and responsive, no knocks or creaks.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.ElectricalFault))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"The <b>{l.Model}</b> has a <color=#ff9944>warning light on the dash</color> that I cannot get to go away. <color=#aaaaaa>I had it plugged in at Halfords and they said something about the alternator but I did not really follow it.</color>",
                    $"The <color=#ff9944>battery keeps going flat</color> on the <b>{l.Make}</b>. <color=#aaaaaa>I have replaced the battery twice now and the problem came back both times, so it must be something else.</color>",
                    $"There is an <color=#ff9944>intermittent electrical issue</color> with the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Sometimes things on the dash just stop working for a bit then come back. I cannot reproduce it on demand.</color>",
                    $"The <b>{l.Model}</b> <color=#ff9944>occasionally does not start first time</color>. <color=#aaaaaa>Jump leads always sort it but that is clearly not a long term solution. I am told it could be the alternator not charging properly.</color>",
                    $"There is a <color=#ff9944>drain on the electrics</color> somewhere in the <b>{l.Make}</b>. <color=#aaaaaa>Mechanic said something about a parasitic draw but could not find the source without more time spent on it. Price reflects the unknown.</color>",
                    $"Some <color=#ff9944>electrics cut out randomly</color> on the <b>{l.Model}</b>. <color=#aaaaaa>Windows, radio, interior lights — they just stop sometimes. Comes back after turning it off and on. Very annoying.</color>",
                        })

                        : level == 2
                            ? Pick(_rng, new[]
                            {

                        $"There's an <color=#ff9944>electrical drain</color>, likely the alternator not charging. <color=#aaaaaa>You'll need a multimeter and some patience to trace it properly.</color>",
                        $"It has a <color=#ff9944>parasitic battery draw</color>. <color=#aaaaaa>If left for a few days, it goes flat. I suspect a bad ground or a faulty relay, but I don't have the time to chase it.</color>",
                        $"The <b>{l.Make}</b> has an <color=#ff9944>intermittent electrical fault</color> on the dashboard. <color=#aaaaaa>Could be the alternator regulator. It runs, but you'll need to diagnose it properly.</color>",
                        $"<color=#ff9944>Electrics are acting up</color>. The battery isn't getting charged. <color=#aaaaaa>Priced lower because I know auto-electrician hourly rates aren't cheap.</color>"
                            })


                        : level == 3
                     ? Pick(_rng, new[]
                     {
                        $"Classic {l.Make} electrical gremlin. The <color=#ff9944>alternator voltage regulator is fried</color>. <color=#aaaaaa>It's pushing barely 11.5 volts. It's a £150 part and an hour of your time to swap it out.</color>",
                        $"There's a <color=#ff9944>parasitic draw draining the battery</color>. <color=#aaaaaa>Usually it's the comfort control module on these {l.Model}s. I just disconnect the negative terminal overnight. Needs diagnosing properly.</color>",
                        $"Known issue on the {l.Year} models: the <color=#ff9944>alternator diode pack has failed</color>. <color=#aaaaaa>Car runs perfectly on a fresh battery for about 20 miles, then dies. Bring a trailer or a spare battery.</color>",
                        $"The <color=#ff9944>alternator is dead</color> and the dash is a Christmas tree. <color=#aaaaaa>I checked it with a multimeter. The loom is fine, but you need a new Bosch unit before you can daily drive it.</color>"

                     })
                     : "There is an electrical fault — the alternator is not charging properly and the battery keeps going flat, which is why the price is what it is.",

                    // ElectricalFault — Wrecker L1
                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
        $"A <color=#ff9944>warning light</color> comes on sometimes on the <b>{l.Make}</b>. <color=#aaaaaa>Turns itself off after a while. Never figured out what it meant.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>takes a couple of tries to start</color> sometimes. <color=#aaaaaa>Usually fine once it gets going. Probably just the battery getting old.</color>",
        $"One of the <color=#ff9944>windows stopped working</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>The driver side one still goes up and down. I just leave the other ones closed.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>kills the battery</color> if it sits for more than a week. <color=#aaaaaa>I keep jump leads in the boot. Sorted in a minute once you know what you are doing.</color>",
        $"Radio on the <b>{l.Make}</b> <color=#ff9944>cuts out randomly</color>. <color=#aaaaaa>Comes back on its own eventually. I just sing to myself in the meantime.</color>",
        $"There is an <color=#ff9944>orange light</color> on the dash of the <b>{l.Model}</b> that has been on for about a year. <color=#aaaaaa>I asked at the garage and they gave me a price I did not like. So here we are.</color>",
        $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>does not always start first time in the cold</color>. <color=#aaaaaa>Give it two or three turns and it gets there. Summer it is fine.</color>",
        $"Central locking on the <b>{l.Model}</b> <color=#ff9944>works on the driver side only</color>. <color=#aaaaaa>The passengers just use the key in the door. Nobody has complained.</color>",
        $"The <b>{l.Make}</b> has <color=#ff9944>a few warning lights on the dash</color>. <color=#aaaaaa>I was told one of them is just an old sensor. The others I am less sure about.</color>",
        $"Electrics on the <b>{l.Model}</b> are <color=#ff9944>a bit temperamental</color>. <color=#aaaaaa>Nothing that has left me stranded. Just little things that come and go.</color>",
                       })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"The <b>{l.Make}</b> has a <color=#ff9944>parasitic drain</color> that flattens the battery if it sits for more than a few days. <color=#aaaaaa>Never tracked down the source properly. It is on the list of reasons it stopped being my daily.</color>",
                    $"Alternator on the <b>{l.Model}</b> <color=#ff9944>was not charging properly</color> before I parked it. <color=#aaaaaa>Battery light came on intermittently. Did not want to risk a breakdown so I took it off the road. Never got back to it.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>a fault I never fully diagnosed</color>. <color=#aaaaaa>Multiple warning lights, suspected alternator or a bad earth. Ran fine most of the time but I did not trust it for longer journeys.</color>",
                    $"Battery on the <b>{l.Make}</b> is <color=#ff9944>probably flat after sitting this long</color>. <color=#aaaaaa>On top of that there was already an intermittent electrical issue before it went into storage. Bring jump leads and manage your expectations.</color>",
                    $"The <b>{l.Model}</b> has <color=#ff9944>an ongoing electrical fault</color> I did not get to the bottom of. <color=#aaaaaa>I suspected the alternator regulator but never confirmed it. Priced to reflect the unknown.</color>",
                    $"There is <color=#ff9944>a known wiring issue</color> on the <b>{l.Year}</b> <b>{l.Make}</b> that I traced to the fusebox area. <color=#aaaaaa>Beyond that I did not have the diagnostic equipment to go further. It is someone else's problem at this price.</color>",
                    $"The <b>{l.Make}</b> started <color=#ff9944>throwing codes before I parked it</color>. <color=#aaaaaa>I cleared them and they came back. Not chasing ghost faults on a car I was done with. Priced accordingly.</color>",
                    $"<color=#ff9944>Electrics were playing up</color> towards the end of me using the <b>{l.Model}</b> daily. <color=#aaaaaa>Intermittent, never consistent enough to pin down. After a long enough time standing it will need a proper look regardless.</color>",
                            })
                        : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"The <b>{l.Make}</b> has a <color=#ff9944>known alternator fault</color>. <color=#aaaaaa>I checked it with a multimeter before listing — output is down to 11.8 volts under load. New Bosch unit is the fix. I know the cost, I have priced it out of the car. Straightforward job for anyone with a socket set.</color>",
                    $"<color=#ff9944>Parasitic drain</color> on the <b>{l.Model}</b>. <color=#aaaaaa>I traced it to the comfort control module area — classic on this chassis. I did not pull the module because I was not rebuilding it, I was pricing it to sell. That diagnostic cost is already in what I am asking.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>an electrical fault I have narrowed down to the alternator diode pack</color>. <color=#aaaaaa>Runs fine on a charged battery for a reasonable distance, then the voltage drops. It is a known failure mode on this engine variant. Cost of a replacement unit is factored into the price.</color>",
                    $"<color=#ff9944>Alternator is not charging</color> on the <b>{l.Make}</b>. <color=#aaaaaa>I confirmed it before listing — battery sits at resting voltage, does not climb under running. The loom is fine, the battery is fine, the alternator is not. I deal in enough of these to know what the part costs. It is already out of the price.</color>",
                    $"The <b>{l.Model}</b> has <color=#ff9944>a charging fault</color>. <color=#aaaaaa>Dashboard confirms it, multimeter confirms it. I am not going to list a car with a known electrical problem and not say what it is. Alternator or regulator — one way to be sure is to swap the unit. Cost is in the asking price.</color>",
                    $"<color=#ff9944>ECU fault codes</color> stored on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>I pulled them before listing. Alternator output related — nothing in the engine management itself. These codes will clear once the charging issue is resolved. I have priced the repair into the car.</color>",
                    $"The <b>{l.Make}</b> has an <color=#ff9944>intermittent no-start caused by the charging circuit</color>. <color=#aaaaaa>Classic symptom of a failing alternator on this platform. I have seen it enough times to recognise it immediately. The fix is straightforward. The cost of that fix is already out of my asking price.</color>",
                            })
                            : "A warning light comes on occasionally, turns itself off after a while, never figured out what it was.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"<color=#99ff99>All electrics working perfectly</color> on the <b>{l.Model}</b>. <color=#aaaaaa>No warning lights on the dash at all.</color>",
                    $"Electrics on the <b>{l.Make}</b> are <color=#99ff99>absolutely fine</color>. <color=#99ff99>New battery fitted</color> recently too.",
                    $"<color=#99ff99>Everything works</color> — windows, lights, radio, all of it. <color=#aaaaaa>Had no electrical issues in all the time I have owned the <b>{l.Model}</b>.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"<color=#99ff99>All electrics working perfectly</color>. No warning lights, no drains, no gremlins.",
                    $"Battery was <color=#99ff99>replaced recently</color>. Alternator checked at the same time — all fine.",
                    $"Had a <color=#99ff99>diagnostic check</color> done before listing. Zero stored codes.",
                    $"Electrics are <color=#99ff99>completely reliable</color>. Never had any issues in the time I have owned it.",
                    $"Everything works — windows, lights, radio, climate. <color=#99ff99>No electrical concerns whatsoever.</color>",
                            })
     : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"<color=#99ff99>All electrics functioning correctly</color>. No warning lights, no gremlins, no intermittent faults.",
                    $"Battery is <color=#99ff99>less than a year old</color>. Alternator was checked and is charging correctly.",
                    $"Had a <color=#99ff99>full diagnostic scan</color> done — zero stored codes, all sensors reading within parameters.",
                    $"Electrics on these <b>{l.Make}</b>s have a reputation but <color=#99ff99>this one has never given me any trouble</color>.",
                    $"<color=#99ff99>Everything works</color> — windows, lights, climate, all of it. No issues in the time I have owned it.",
                            })
     : "All electrics working perfectly, no warning lights on the dash.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.ExhaustRusted))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"The <color=#ff9944>exhaust system has rusted through</color> at the mid-section. <color=#aaaaaa>It's blowing pretty loudly, will need a patch welded or a new cat-back section.</color>",
                    $"There's <color=#ff9944>heavy corrosion on the exhaust</color> and a visible hole. <color=#aaaaaa>It's noisy on acceleration. Replacement exhausts for a <b>{l.Model}</b> are cheap enough though.</color>",
                    $"Exhaust is <color=#ff9944>blowing due to rust</color>. <color=#aaaaaa>It won't pass its next inspection like this. You can hear it as soon as you start the engine.</color>",
                    $"The backbox and mid-pipe are <color=#ff9944>heavily rusted</color>. <color=#aaaaaa>I haven't bothered replacing it as I'm selling, but be prepared for a sporty exhaust note on the drive home.</color>"
                        })

                        : level == 2
                            ? Pick(_rng, new[]
                            {

                       $"Just so you know, the <color=#777777>exhaust is showing some serious rust</color>. It's starting to blow a bit, so it'll need a patch or replacement soon.",
                        $"Mechanically it's fine, but the <color=#ffcc00>exhaust system is quite corroded</color>. I've already adjusted the price of this {l.Make} because of it.",
                     $"The {l.Model} sounds a bit more aggressive than usual because the <color=#777777>muffler is rusted through</color>. It’s an easy fix if you have a welder.",
                    $"I should mention the <color=#ff4444>rusted exhaust pipe</color>. It’s still holding together, but it won't pass the next inspection without some work.",
                    $"The car is solid, but the <color=#777777>exhaust is the weakest link</color> right now due to surface rust and one small hole near the back."
                            })

                        : level == 3
                     ? Pick(_rng, new[]
                     {
                        $"The flex-pipe on the downpipe is <color=#ff9944>rusted through</color>. <color=#aaaaaa>Standard rot for a {l.Make} of this era. Perfect excuse to put a stainless steel system on it.</color>",
                        $"Rear silencer has <color=#ff9944>rusted out from short trips</color>. It's loud. <color=#aaaaaa>You can weld a patch or just bolt on a new cat-back exhaust. The hangers are still solid at least.</color>",
                        $"<color=#ff9944>Exhaust is blowing</color> at the manifold joint due to rusted studs. <color=#aaaaaa>It's a common {l.Model} headache. Soak it in penetrating fluid for a day before you try fixing it.</color>",
                        $"The OEM exhaust is <color=#ff9944>structurally rust-damaged</color> at the Y-pipe. <color=#aaaaaa>I wouldn't trust it over speed bumps. Priced to account for a full aftermarket replacement.</color>"

                     })
                     : "Exhaust has some rust on it and will need attention before long — nothing structural but worth knowing.",

                    // ExhaustRusted — Wrecker L1
                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
        $"The <b>{l.Make}</b> is <color=#ff9944>a bit louder</color> than it used to be. <color=#aaaaaa>Especially when cold. Quiets down once it warms up. Just turns heads at the traffic lights.</color>",
        $"There is <color=#ff9944>a slight blowing noise</color> from somewhere under the <b>{l.Model}</b>. <color=#aaaaaa>I can barely hear it inside with the radio on. Someone else mentioned it though.</color>",
        $"Exhaust on the <b>{l.Year}</b> <b>{l.Make}</b> is <color=#ff9944>a bit rusty looking</color>. <color=#aaaaaa>Has not fallen off. I check it every few months and it is still attached.</color>",
        $"The <b>{l.Model}</b> <color=#ff9944>sounds a bit throaty</color> on startup. <color=#aaaaaa>I actually quite like it. Goes away after a minute. Might be the exhaust, might just be character.</color>",
        $"Someone pointed out the exhaust on the <b>{l.Make}</b> was <color=#ff9944>blowing a bit</color>. <color=#aaaaaa>I honestly had not noticed. It has not got any worse since they said that, which was about six months ago.</color>",
        $"There is <color=#ff9944>some rust on the back box</color> of the <b>{l.Model}</b>. <color=#aaaaaa>Looks worse than it probably is I think. Or maybe it looks exactly as bad as it is. Hard to say.</color>",
        $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>makes more noise at cold start</color> than a newer car would. <color=#aaaaaa>I put it down to age. These things get a bit grumbly after a while.</color>",
        $"Exhaust fumes on the <b>{l.Model}</b> are <color=#ff9944>slightly visible</color> on cold mornings. <color=#aaaaaa>Not like clouds of smoke or anything. Just a bit. All old cars do it a bit.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Exhaust on the <b>{l.Make}</b> is <color=#ff9944>blowing at the mid-section joint</color>. <color=#aaaaaa>I know exactly where it is. Just a cat-back replacement job. Never got round to it before I parked it up.</color>",
                    $"The <b>{l.Model}</b> has a <color=#ff9944>rusted-through backbox</color>. <color=#aaaaaa>It is loud from cold start. Quietens down a bit once warm. Replacement is cheap enough, just time I never had.</color>",
                    $"<color=#ff9944>Exhaust has been blowing</color> on the <b>{l.Year}</b> <b>{l.Make}</b> for a while. <color=#aaaaaa>It was one of the reasons I took it off the road — did not want to deal with it failing completely while driving.</color>",
                    $"The downpipe on the <b>{l.Make}</b> has <color=#ff9944>a visible crack from corrosion</color>. <color=#aaaaaa>Standard rot for a <b>{l.Year}</b> car that spent its life in salted roads. Parts are available, just needs doing.</color>",
                    $"<color=#ff9944>Rear silencer has rusted through</color> on the <b>{l.Model}</b>. <color=#aaaaaa>It is noisy. Sat in a cold garage for a year has not helped. The hangers are still solid at least.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>an exhaust leak at the manifold</color>. <color=#aaaaaa>You can hear it under load. I knew about it before parking it. Add it to the list of jobs for the new owner.</color>",
                    $"Exhaust system on the <b>{l.Model}</b> is <color=#ff9944>corroded enough that it needs replacing in sections</color>. <color=#aaaaaa>Nothing structural, just the kind of rot you get on any car that age that was not garaged properly. It was not garaged properly.</color>",
                            })
                        : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"<color=#ff9944>Exhaust is blowing at the downpipe flange</color> on the <b>{l.Make}</b>. <color=#aaaaaa>Rusted studs — soak them well before you attempt it. Standard problem on a <b>{l.Year}</b> car. A stainless cat-back is the sensible long-term fix. I have priced that job into what I am asking.</color>",
                    $"The <b>{l.Model}</b> has a <color=#ff9944>rusted-through mid-section</color>. <color=#aaaaaa>It is loud under acceleration. The manifold and cat are fine — it is from the flex pipe back. Cheap enough to replace with an aftermarket section. Cost already accounted for in the price.</color>",
                    $"<color=#ff9944>Rear silencer has corroded through</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Short-trip rot — these cars need a decent run to keep the exhaust dry. This one did not get it often enough. Replacement backbox is a simple bolt-on job. I have costed it into the asking price already.</color>",
                    $"The <b>{l.Make}</b> has an <color=#ff9944>exhaust leak at the manifold-to-downpipe junction</color>. <color=#aaaaaa>Visible crack in the downpipe from thermal cycling and corrosion. You can hear it under load. A new downpipe sorts it. I deal in enough of these to know the part cost — it is reflected in the price.</color>",
                    $"<color=#ff9944>Full cat-back replacement needed</color> on the <b>{l.Model}</b>. <color=#aaaaaa>The system is corroded from the catalyst back. I checked it on ramps before listing. Hangers are solid, cat is fine — it is just the pipework. Stainless aftermarket is the right answer. Already costed in.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has a <color=#ff9944>structurally compromised exhaust system</color>. <color=#aaaaaa>Not going to dress it up. Mid-pipe and backbox are both corroded past the point of repair. OEM replacement priced and factored into what I am asking. Nothing else on the underside concerns me.</color>",
                            })
                            : "It is a bit louder than it used to be, especially when cold, but it quiets down once it warms up.",

                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => null,
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.GlassDamage))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? Pick(_rng, new[]
                        {
                    $"There is a <color=#ff9944>small chip in the windscreen</color> of the <b>{l.Model}</b>. <color=#aaaaaa>Not a crack, just a chip — visible in the photos. Probably repairable for not much money.</color>",
                    $"The <b>{l.Make}</b> has a <color=#ff9944>stone chip on the windscreen</color>. <color=#aaaaaa>I kept meaning to get it repaired. It has not spread in the year I have had the car.</color>",
                    $"Windscreen on the <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>a small mark on the passenger side</color>. <color=#aaaaaa>Barely noticeable from the driver's seat but I wanted to mention it. Shown in photos.</color>",
                    $"There is a <color=#ff9944>chip in the glass</color> on the <b>{l.Model}</b>. <color=#aaaaaa>Insurance might cover a repair — I just never bothered to claim. Worth mentioning either way.</color>",
                        })

                        : level == 2
                            ? Pick(_rng, new[]
                            {

                        $"There is a <color=#ff9944>noticeable crack in the windscreen</color>. <color=#aaaaaa>It's in the swept area, so it'll definitely need a new glass fitted before the next MOT.</color>",
                        $"The windscreen has suffered a <color=#ff9944>large stone strike</color> that has spiderwebbed. <color=#aaaaaa>Too big for a resin repair, you'll need to call Autoglass.</color>",
                        $"<color=#ff9944>Windscreen is cracked</color> on the driver's side. <color=#aaaaaa>I've knocked the cost of a replacement excess off the asking price.</color>",
                        $"There's <color=#ff9944>glass damage</color> on the front screen. <color=#aaaaaa>Structurally fine to drive, but it's an MOT failure waiting to happen.</color>"
                            })

                        : level == 3
                     ? Pick(_rng, new[]
                     {
                        $"The windscreen took a rock on the motorway. <color=#ff9944>It's cracked</color>. <color=#aaaaaa>It's the heated screen with rain sensors, so I know it's a £300 excess. I've dropped the price by £300 exactly.</color>",
                        $"OEM glass is <color=#ff9944>chipped and spreading</color>. <color=#aaaaaa>Don't put cheap aftermarket glass in these {l.Model}s, it messes with the auto-wipers. Factor a proper Pilkington replacement into your budget.</color>",
                        $"<color=#ff9944>Crack in the driver's line of sight</color>. <color=#aaaaaa>It's an instant MOT failure. I haven't claimed it on my insurance because I'm selling it anyway. Sold as is.</color>",
                        $"The windscreen has a <color=#ff9944>large unrepairable crack</color>. <color=#aaaaaa>I've already removed the A-pillar trims so it's ready for the glass guy to cut it out. Priced with the replacement cost in mind.</color>"

                     })
                     : "There is a small chip in the windscreen — not a crack, just a chip, disclosed in the photos.",

                    // GlassDamage — Wrecker L1
                    SellerArchetype.Wrecker => level == 1
                        ? Pick(_rng, new[]
                        {
        $"There is a <color=#ff9944>chip in the windscreen</color> of the <b>{l.Make}</b>. <color=#aaaaaa>Has been there since I bought it. Never got any worse.</color>",
        $"Small <color=#ff9944>mark on the glass</color> of the <b>{l.Model}</b>. <color=#aaaaaa>Passenger side so I barely see it from the driver seat. Easy to forget it is there.</color>",
        $"The <b>{l.Year}</b> <b>{l.Make}</b> has a <color=#ff9944>stone chip on the front screen</color>. <color=#aaaaaa>I kept meaning to get it repaired with insurance. Never quite got around to the phone call.</color>",
        $"Windscreen on the <b>{l.Model}</b> has <color=#ff9944>a small blemish</color>. <color=#aaaaaa>Not a crack. Just a chip. You only really notice it at a certain angle in the sun.</color>",
        $"There is <color=#ff9944>a crack on the lower edge</color> of the windscreen of the <b>{l.Make}</b>. <color=#aaaaaa>Has not spread. I put some clear nail varnish on it. That is what you are supposed to do apparently.</color>",
        $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color=#ff9944>a chip in the glass</color> from the motorway. <color=#aaaaaa>A stone came up from a lorry. Could not really be avoided. It is what it is.</color>",
        $"Front screen on the <b>{l.Make}</b> has <color=#ff9944>a tiny mark</color>. <color=#aaaaaa>The previous owner probably did it. I could not be bothered chasing it through insurance for such a small thing.</color>",
                        })
                        : level == 2
                            ? Pick(_rng, new[]
                            {
                    $"Windscreen on the <b>{l.Make}</b> has <color=#ff9944>a crack that has spread since I parked it</color>. <color=#aaaaaa>Started as a chip, a cold winter sorted the rest. It is in the driver's line of sight so it will need replacing.</color>",
                    $"The <b>{l.Model}</b> has <color=#ff9944>a cracked windscreen</color>. <color=#aaaaaa>It happened while it was standing — temperature, probably. Replacement is the only option at this point.</color>",
                    $"Front screen on the <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>a long crack across the lower section</color>. <color=#aaaaaa>Started from a stone chip I never got repaired. My fault for leaving it. Priced to cover a new screen.</color>",
                    $"<color=#ff9944>Windscreen needs replacing</color> on the <b>{l.Make}</b>. <color=#aaaaaa>It is cracked and in the swept area so it will not pass a test like that. Factor it into the budget.</color>",
                    $"The <b>{l.Model}</b> has a <color=#ff9944>stone chip that has spidered out</color> over the winter. <color=#aaaaaa>I should have got it filled when it was small. Did not. Now it is a screen job.</color>",
                    $"<color=#ff9944>Glass damage on the front screen</color> of the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>It is cracked from one edge inward. Structurally fine to move but it needs sorting before the road.</color>",
                            })
                        : level == 3
                            ? Pick(_rng, new[]
                            {
                    $"<color=#ff9944>Windscreen needs replacing</color> on the <b>{l.Make}</b>. <color=#aaaaaa>Large crack in the driver's line of sight — instant test failure. I know whether it is a heated screen with sensors or a standard fit and I have priced accordingly. No surprises on the glass cost.</color>",
                    $"The <b>{l.Model}</b> has a <color=#ff9944>cracked front screen</color>. <color=#aaaaaa>I checked the spec before pricing it — it is the <color=#ff9944>acoustic laminated version</color> so a replacement is not the cheapest. That cost is already out of the asking price. I do not leave hidden expenses for the buyer.</color>",
                    $"<color=#ff9944>Windscreen is cracked beyond repair</color> on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Started as a chip, spread across the screen. I priced a replacement from a glass specialist before listing. That figure is already deducted from what I would otherwise be asking. Straightforward job for any fitter.</color>",
                    $"The <b>{l.Make}</b> needs a <color=#ff9944>new windscreen</color>. <color=#aaaaaa>It is cracked in the swept area, so it is a test failure and a visibility issue. I have already stripped the A-pillar trims to check for corrosion underneath — all clean. Glass cost is in the price, prep work is done.</color>",
                    $"<color=#ff9944>Front glass needs replacing</color> on the <b>{l.Model}</b>. <color=#aaaaaa>It is a significant crack, not a chip. I know the replacement cost to within about twenty pounds. That cost is reflected in the asking price. I do not guess on these things — I check first, then price.</color>",
                    $"The <b>{l.Year}</b> <b>{l.Make}</b> has a <color=#ff9944>cracked windscreen that needs replacing before MOT</color>. <color=#aaaaaa>I noted it before setting the price and deducted the fitting cost accordingly. The crack has not spread further — I checked before listing. What you see in the photos is the current state.</color>",
                            })
                            : "Small mark on the windscreen that I never got around to fixing, barely notice it when driving.",

                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => null,
                    _ => null,
                };

            return null;
        }


        // ══════════════════════════════════════════════════════════════════════
        //  HONEST
        // ══════════════════════════════════════════════════════════════════════

        private static string BuildHonest(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ──────────────────────────────────────────────────────
            string opener;
            if (lv == 1)
            {
                opener = IsBad(l) ? Pick(rng, new[]
        {
            $"Starts and drives but honestly something <color=#ff9944><b>does not feel quite right</b></color> with this <b>{l.Make}</b>, <color=#aaaaaa>I just cannot put my finger on what.</color>",
            $"My <b>{l.Model}</b> runs, not brilliantly, but it <color=#99ff99>gets from A to B</color> without stopping — <color=#ff9944>has some issues I cannot properly describe.</color>",
            $"Drove this <b>{l.Year}</b> <b>{l.Make}</b> every day for a while then <color=#ff9944>things started going wrong</color>, <color=#aaaaaa>decided to sell before spending money I do not have.</color>",
            $"Honestly, this <b>{l.Make}</b> is <color=#ff9944>showing its age now</color> and I just want a quick sale. <color=#aaaaaa>I'm not sure what it needs to be perfect again.</color>",
            $"Selling my <b>{l.Year}</b> <b>{l.Model}</b> <color=#ff9944>as it is</color>. It's been <color=#ff9944>a bit temperamental</color> lately and <color=#aaaaaa>I don't have the patience to figure out why.</color>",
            $"This <b>{l.Color}</b> <b>{l.Make}</b> has <color=#ff9944>seen better days</color>, I'll be the first to admit it. <color=#99ff99>Might be an easy fix</color> for someone who actually knows about cars.",
            $"It starts and it goes, but this <b>{l.Model}</b> is definitely <color=#ff9944>a bit of a project now</color>. <color=#aaaaaa>I've just been using it for short trips to the shops.</color>",
            $"Time for this <b>{l.Year}</b> <b>{l.Make}</b> to go. It's got <color=#ff9944>some quirks</color> that I've just learned to live with, <color=#aaaaaa>but you might want to look at them.</color>",
            $"Selling my old <b>{l.Model}</b>. It’s been sitting for a bit because <color=#ff9944>it started acting up</color>. <color=#aaaaaa>I'm tired of looking at it on the driveway.</color>",
            $"My <b>{l.Year}</b> <b>{l.Make}</b> is <color=#ff9944>a bit rough around the edges</color>. <color=#aaaaaa>I think it's just tired from all the miles.</color>",
            $"If you're looking for a showroom car, this <b>{l.Color}</b> <b>{l.Model}</b> isn't it. <color=#ff9944>It's just a basic old car with some issues.</color>",
            $"This <b>{l.Make}</b> has <color=#ff9944>a few groans and creaks</color>. <color=#aaaaaa>I’m selling it cheap because I just want it gone today.</color>",
            $"Listing my <b>{l.Year}</b> <b>{l.Model}</b>. <color=#ff9944>It’s not been the same since the winter</color>. <color=#aaaaaa>I'm not a mechanic, so I'm selling it as I found it.</color>",
            $"Old <b>{l.Make}</b> for sale. <color=#ff9944>It’s got a mind of its own sometimes</color>. <color=#aaaaaa>I've just been driving it very carefully lately.</color>",
            $"Selling this <b>{l.Color}</b> <b>{l.Model}</b>. It's <color=#ff9944>a bit of a 'fixer-upper'</color> as they say. <color=#aaaaaa>I don't even know where the toolkit is.</color>",
            $"My <b>{l.Year}</b> <b>{l.Make}</b> is definitely <color=#ff9944>a 'budget' option</color>. <color=#aaaaaa>It gets me there, eventually.</color>",
            $"Listing my <b>{l.Model}</b>. It's had a long life and <color=#ff9944>it's starting to show</color>. <color=#aaaaaa>I'd keep it but I need something I don't have to worry about.</color>",
            $"This <b>{l.Make}</b> <b>{l.Model}</b> is what it is. <color=#ff9944>A bit noisy, a bit slow</color>, <color=#aaaaaa>but it's still technically a car.</color>",
            $"Selling my <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Make}</b>. <color=#ff9944>It’s not perfect</color>, but it’s honest. <color=#aaaaaa>I just can't afford to keep guessing what's wrong with it.</color>",
            $"Had this <b>{l.Model}</b> for years, but it's time to part ways. <color=#ff9944>It’s developed a few 'noises'</color> that I don't like.",
            $"Listing my <b>{l.Make}</b>. It's <color=#ff9944>a bit grumpy in the mornings</color>. <color=#aaaaaa>Once it's warm it's okay-ish, I think.</color>"


                }) : IsMid(l) ? Pick(rng, new[]
                {
            $"Daily driver for the last couple of years, this <b>{l.Make}</b> has been <color=#99ff99>mostly reliable</color> and never left me stranded.",
            $"Starts every time and <color=#99ff99>drives fine</color> as far as I can tell, <color=#aaaaaa>not a car person so cannot say much more about the <b>{l.Model}</b> than that.</color>",
            $"Got this <b>{l.Color}</b> <b>{l.Make}</b> a few years ago and <color=#99ff99>it has done the job</color>, time to move on now.",
            $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. It's just a <color=#99ff99>normal car</color> that does normal car things. <color=#aaaaaa>Never really let me down.</color>",
            $"Here is my <b>{l.Color}</b> <b>{l.Model}</b>. I've used it for work and back, and it's been <color=#99ff99>totally fine</color>.",
            $"Up for sale is my <b>{l.Make}</b>. It’s got some age, but it’s been <color=#99ff99>a good servant</color> to me. <color=#aaaaaa>I'll be sad to see it go, actually.</color>",
            $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color=#99ff99>decent little runner</color>. <color=#aaaaaa>Nothing fancy, but it gets the job done without any fuss.</color>",
            $"Selling the <b>{l.Make}</b> because I've upgraded. It’s <color=#99ff99>served me well</color> for three years now.",
            $"My <b>{l.Color}</b> <b>{l.Model}</b> is ready for a new owner. <color=#aaaaaa>I’ve just used it for the school run mostly.</color>",
            $"Listing my <b>{l.Year}</b> <b>{l.Make}</b>. It’s a <color=#99ff99>solid car</color> for the price. <color=#aaaaaa>It's not perfect, but it's been very reliable for me.</color>",
            $"This <b>{l.Model}</b> has been a <color=#99ff99>great first car</color> for me. <color=#aaaaaa>Easy to drive and doesn't cost much to run.</color>",
            $"Selling my <b>{l.Make}</b> <b>{l.Model}</b>. It's been through a few MOTs with me and always <color=#99ff99>seems to pass eventually</color>.",
            $"Got this <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Make}</b> from a neighbor. <color=#99ff99>It's been a steady car</color> for as long as I've had it.",
            $"It's a <b>{l.Model}</b>. It drives, it stops, <color=#99ff99>the heater works</color>. <color=#aaaaaa>That's about all I know about cars!</color>",
            $"Selling my <b>{l.Make}</b>. It's <color=#aaaaaa>a bit of a plain Jane</color>, but she's <color=#99ff99>never left me stranded</color> on the motorway.",
            $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color=#99ff99>fair car for fair money</color>. <color=#aaaaaa>I’ve kept it clean inside at least!</color>",
            $"My <b>{l.Color}</b> <b>{l.Make}</b> is up for grabs. <color=#aaaaaa>I've not had any major dramas with it since I bought it.</color>",
            $"Listing my <b>{l.Model}</b>. It's been a <color=#99ff99>faithful workhorse</color> for me and my family.",
            $"This <b>{l.Year}</b> <b>{l.Make}</b> is just a <color=#99ff99>sensible choice</color>. <color=#aaaaaa>I’m only selling because I don't need two cars anymore.</color>",
            $"Decent <b>{l.Model}</b> for sale. <color=#aaaaaa>I've always found it quite comfy for long trips.</color>",
            $"Selling my <b>{l.Color}</b> <b>{l.Make}</b>. <color=#99ff99>It’s not a race car, but it’s very dependable.</color>"


                }) : Pick(rng, new[]
                {
            $"<color=#99ff99>Well looked after</color> <b>{l.Year}</b> <b>{l.Make}</b> as best I could, <color=#99ff99>always garaged</color> and kept clean, runs really well.",
            $"<color=#99ff99>Pretty good condition</color> I think — my <b>{l.Model}</b> <color=#99ff99>always started first time</color> and never gave me any real trouble.",
            $"Bought this <b>{l.Color}</b> <b>{l.Make}</b> new and kept it properly, <color=#aaaaaa>genuinely one careful owner.</color>",
            $"<color=#99ff99>Really proud</color> of my <b>{l.Year}</b> <b>{l.Make}</b>. I've tried to keep it in the <color=#99ff99>best shape possible</color>. <color=#aaaaaa>It's a lovely car.</color>",
            $"Selling my <b>{l.Color}</b> <b>{l.Model}</b>. I've <color=#99ff99>always looked after it</color> and I think it shows. <color=#99ff99>It still feels very fresh to drive.</color>",
            $"This <b>{l.Make}</b> <b>{l.Model}</b> has been my <color=#99ff99>pride and joy</color>. <color=#aaaaaa>I always wash it on Sundays if it's not raining.</color>",
            $"Up for sale is a <color=#99ff99>very clean</color> <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>I've never even smoked in it, kept it really tidy.</color>",
            $"Listing my <b>{l.Model}</b>. It's in <color=#99ff99>great condition</color> for its age. <color=#aaaaaa>I've always taken it to the same garage for everything.</color>",
            $"This <b>{l.Color}</b> <b>{l.Make}</b> is a <color=#99ff99>fantastic example</color>. <color=#aaaaaa>I really don't want to sell it, but I need the space.</color>",
            $"Selling my <b>{l.Year}</b> <b>{l.Model}</b>. It's been <color=#99ff99>pampered its whole life</color>, always kept in the garage at night.",
            $"My <b>{l.Make}</b> <b>{l.Model}</b> is a <color=#99ff99>little gem</color>. <color=#99ff99>Runs like a clock</color> and looks great in <b>{l.Color}</b>.",
            $"<color=#99ff99>Beautiful</color> <b>{l.Year}</b> <b>{l.Make}</b> for sale. <color=#aaaaaa>I’ve spent a lot of time keeping it looking this good.</color>",
            $"This <b>{l.Model}</b> is probably <color=#99ff99>one of the better ones out there</color>. <color=#aaaaaa>It’s never given me a day of worry.</color>",
            $"Selling my <b>{l.Make}</b>. It’s a <color=#99ff99>really smooth drive</color>. <color=#aaaaaa>I think the next owner will be very happy with it.</color>",
            $"Listing my <b>{l.Color}</b> <b>{l.Year}</b> <b>{l.Model}</b>. I've really enjoyed owning this car, <color=#99ff99>it's never missed a beat</color>.",
            $"This <b>{l.Make}</b> <b>{l.Model}</b> is in <color=#99ff99>wonderful shape</color>. <color=#aaaaaa>I've always been very careful where I park it.</color>",
            $"My <b>{l.Year}</b> <b>{l.Make}</b> is a <color=#99ff99>very honest, clean car</color>. <color=#aaaaaa>No surprises here, just a well-kept vehicle.</color>",
            $"Selling this <b>{l.Model}</b>. It’s been <color=#99ff99>very reliable</color> and still looks almost new in some places!",
            $"This <b>{l.Color}</b> <b>{l.Make}</b> is a <color=#99ff99>real pleasure to drive</color>. <color=#aaaaaa>I'll probably regret selling it later.</color>",
            $"Listing my <b>{l.Year}</b> <b>{l.Model}</b>. It’s a <color=#99ff99>top-notch car</color>, always had whatever it needed.",
            $"Selling my <b>{l.Make}</b>. <color=#99ff99>It’s been a very loyal car</color> to me and I’ve treated it well in return."
                });
            }
            else if (lv == 2)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                   $"<b>{l.Make} {l.Model}</b>. <color=#ff9944>Mechanically tired</color> and requires attention. <color=#aaaaaa>Selling as a project for someone with the right tools.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. <color=#ff9944>Needs a fair amount of work</color> to be roadworthy again. <color=#aaaaaa>Technically sound engine, but surrounding components are worn.</color>",
                    $"This <b>{l.Model}</b> is <color=#ff9944>showing its age</color>. <color=#aaaaaa>It starts and drives, but I wouldn't recommend it for long trips until sorted.</color>",
                    $"<b>{l.Color}</b> <b>{l.Make}</b>, <color=#ff9944>sold as seen</color>. <color=#aaaaaa>I've identified several mechanical issues that need addressing. See below for technical details.</color>",
                    $"Fairly worn <b>{l.Model}</b>. <color=#ff9944>It’s a bit of a fixer-upper</color>. <color=#aaaaaa>Transmission is okay, but the rest of the drivetrain needs a look-over.</color>",
                    $"<b>{l.Year}</b> <b>{l.Make}</b> for sale. <color=#ff9944>Not in the best shape</color> cosmetically or mechanically. <color=#aaaaaa>Price is set based on the cost of required repairs.</color>",
                    $"This <b>{l.Model}</b> has been <color=#ff9944>neglected by the previous owner</color>. <color=#aaaaaa>I’ve done a basic diagnosis, it’s going to need a few weekends in the garage.</color>",
                    $"<b>{l.Make}</b> in <color=#ff9944>rough condition</color>. <color=#aaaaaa>Solid chassis, but most of the moving parts are at the end of their service life.</color>"
                }) : IsMid(l) ? Pick(rng, new[]
                {
                    $"<b>{l.Year}</b> <b>{l.Make}</b> in <color=#99ff99>reasonable mechanical order</color>. <color=#aaaaaa>General wear and tear present as expected for the mileage.</color>",
                    $"Average example of a <b>{l.Model}</b>. <color=#99ff99>Drives fine</color>, <color=#aaaaaa>but don't expect a new car. It's a reliable daily runner.</color>",
                    $"Selling my <b>{l.Color}</b> <b>{l.Make}</b>. <color=#99ff99>Everything works as it should</color>, <color=#aaaaaa>though it has a few minor technical quirks that don't affect driveability.</color>",
                    $"<b>{l.Make} {l.Model}</b> with <b>{l.Mileage:N0}</b> on the clock. <color=#99ff99>Mechanically sound</color>, <color=#aaaaaa>regularly serviced, just needs a new owner.</color>",
                    $"Good, <color=#99ff99>honest runner</color>. <color=#aaaaaa>This <b>{l.Year}</b> <b>{l.Model}</b> has been a dependable car for me. No major issues to report.</color>",
                    $"Standard <b>{l.Make}</b>. <color=#99ff99>Technically okay</color>, <color=#aaaaaa>cosmetics are 6/10. It’s been maintained well enough to keep it reliable.</color>",
                    $"Solid <b>{l.Model}</b>. <color=#99ff99>Engine and gearbox are healthy</color>. <color=#aaaaaa>Suspension feels okay, just a standard used car for a fair price.</color>",
                    $"<b>{l.Year}</b> <b>{l.Make}</b>. <color=#99ff99>Passed last MOT with only minor advisories</color>, <color=#aaaaaa>most of which have been sorted now.</color>"
                }) : Pick(rng, new[]
                {
                    $"Very <color=#99ff99>well-maintained</color> <b>{l.Make} {l.Model}</b>. <color=#aaaaaa>I've kept this car in top technical condition during my ownership.</color>",
                    $"High-spec <b>{l.Color}</b> <b>{l.Make}</b>. <color=#99ff99>Mechanically excellent</color>. <color=#aaaaaa>Drives tight, no knocks or leaks. Hard to find them in this state.</color>",
                    $"<b>{l.Year}</b> <b>{l.Model}</b>. <color=#99ff99>Full technical inspection recently carried out</color>. <color=#aaaaaa>No faults found, car is ready for immediate use.</color>",
                    $"Superior example of a <b>{l.Make}</b>. <color=#99ff99>Clean engine bay and solid drivetrain</color>. <color=#aaaaaa>Always used high-quality fluids and parts.</color>",
                    $"This <b>{l.Model}</b> is in <color=#99ff99>top-tier mechanical condition</color>. <color=#aaaaaa>Very quiet engine, smooth shifting, and precise steering.</color>",
                    $"Reliable and <color=#99ff99>technically pristine</color> <b>{l.Make}</b>. <color=#aaaaaa>I don't sell cars that aren't 100% sorted. This one is perfect.</color>",
                    $"Exceptional <b>{l.Year}</b> <b>{l.Model}</b>. <color=#99ff99>Everything is within factory tolerances</color>. <color=#aaaaaa>You won't find many <b>{l.Make}</b>s as clean as this one.</color>",
                    $"Premium <b>{l.Color}</b> <b>{l.Model}</b>. <color=#99ff99>Fully serviced and technically verified</color>. <color=#aaaaaa>It’s been an absolute pleasure to own and maintain.</color>"
                });
            }
            else
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    $"Owned this <b>{l.Make}</b> for eleven years. I know <color=#ff9944>every fault on it</color> — <color=#aaaaaa>all of them are listed below.</color>",
                    $"I've had three of these <b>{l.Model}</b>s. <color=#ff9944>This one is the worst of them.</color> <color=#aaaaaa>Price reflects that exactly.</color>",
                    $"Selling with <color=#99ff99>full documentation</color> of everything wrong. <color=#aaaaaa>No surprises on collection.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color=#ff9944>rolling project</color>. <color=#aaaaaa>I've rebuilt two of these before, but I just don't have the ramp space for a third.</color>",
                    $"Selling my <b>{l.Make}</b>. <color=#ff9944>It needs a specialist's touch</color>. <color=#aaaaaa>If you don't know your way around a torque wrench, close this tab now.</color>",
                    $"I bought this <b>{l.Model}</b> to restore, but <color=#ff9944>the list of jobs is too long</color>. <color=#aaaaaa>I've priced it purely on the value of the salvageable panels and the block.</color>",
                    $"This is a <color=#ff9944>hardcore enthusiast's special</color>. <color=#aaaaaa>It runs, but barely. Read the fault list carefully — I wrote it like a workshop manual.</color>",
                    $"My <b>{l.Year}</b> <b>{l.Make}</b> is <color=#ff9944>mechanically compromised</color>. <color=#aaaaaa>I've owned it for 8 years and I know every creak. Needs an owner with deep pockets or a well-equipped garage.</color>",
                    $"I'm thinning down my collection. <color=#ff9944>This is the donor car</color> I never ended up stripping. <color=#aaaaaa>It deserves to be saved by someone who loves the <b>{l.Model}</b> chassis.</color>",
                    $"Let's be real: this <b>{l.Color}</b> <b>{l.Make}</b> is <color=#ff9944>a major undertaking</color>. <color=#aaaaaa>I've documented every missing bolt and weeping seal. Experts only.</color>",
                    $"Selling my old friend. <color=#ff9944>It's throwing codes that I don't have the time to chase</color>. <color=#aaaaaa>Perfect base for a track build or a very patient restoration.</color>",
                    $"If you're looking at this <b>{l.Year}</b> <b>{l.Model}</b>, you know how these rust and leak. <color=#ff9944>This one does both</color>. <color=#aaaaaa>Honest ad from a <b>{l.Make}</b> club member.</color>"
                }) : IsMid(l) ? Pick(rng, new[]
                {
                    $"Fair example for the age. I know the <b>{l.Make}</b> <b>{l.Model}</b> well enough to <color=#99ff99>price it correctly</color>.",
                    $"Not concours but <color=#99ff99>maintained properly</color>. <color=#aaaaaa>Everything done to this car is documented and receipted.</color>",
                    $"Fifty-plus cars sold and <color=#99ff99>every one described exactly as it was</color>. <color=#aaaaaa>This is no different.</color>",
                    $"I've run this <b>{l.Color}</b> <b>{l.Model}</b> as my daily for 5 years. <color=#99ff99>It's mechanically honest</color>, <color=#aaaaaa>but cosmetically it shows its <b>{l.Mileage:N0}</b> miles.</color>",
                    $"This is my 4th <b>{l.Make}</b>. <color=#99ff99>It's a solid mid-tier survivor</color>. <color=#aaaaaa>I've done the preventative maintenance, so you won't get stranded.</color>",
                    $"A genuinely <color=#99ff99>usable modern classic</color>. <color=#aaaaaa>It's not going to win any car shows, but it fires up on the button every time.</color>",
                    $"Selling my <b>{l.Year}</b> <b>{l.Model}</b>. <color=#99ff99>I know all the factory weak points</color>, <color=#aaaaaa>and I've addressed the major ones. It's a pragmatic buy.</color>",
                    $"I'm an active member of the <b>{l.Make}</b> owners' club. <color=#99ff99>This is a known, respected car</color> <color=#aaaaaa>that needs a bit of cosmetic love but drives perfectly.</color>",
                    $"A perfectly <color=#99ff99>average, honest example</color> of a <b>{l.Model}</b>. <color=#aaaaaa>I've priced it dynamically against current market data, not sentimentality.</color>",
                    $"I have kept this <b>{l.Make}</b> running like clockwork. <color=#99ff99>Mechanically 8/10, bodywork 5/10</color>. <color=#aaaaaa>I focus on engineering, not polishing.</color>",
                    $"Reluctantly letting this go to make space. <color=#99ff99>It's a completely stock, unmolested example</color>. <color=#aaaaaa>Getting rare to find them like this now.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> sits right in the middle of the market. <color=#99ff99>Honest patina</color>, <color=#aaaaaa>but a bulletproof drivetrain. I've serviced it myself every 6k miles.</color>"
                }) : Pick(rng, new[]
                {
                    $"Genuine sale from a <color=#99ff99>careful private owner</color> — <color=#aaaaaa>full documented history, every stamp present.</color>",
                    $"One of the <color=#99ff99>best {l.Year} examples</color> I have come across — <color=#aaaaaa>and I have seen a few over the years.</color>",
                    $"I know what a good <b>{l.Make}</b> looks like. <color=#99ff99>This is one of them.</color>",
                    $"This is a <color=#99ff99>collector-grade <b>{l.Model}</b></color>. <color=#aaaaaa>I've stored it in a dehumidified bubble and only run it on premium fuel.</color>",
                    $"I've spent <color=#99ff99>thousands over-maintaining</color> this <b>{l.Make}</b>. <color=#aaaaaa>It is mechanically superior to when it left the factory.</color>",
                    $"This <b>{l.Year}</b> <b>{l.Model}</b> is <color=#99ff99>absolutely immaculate</color>. <color=#aaaaaa>I have a 3-inch thick binder of receipts sorted chronologically.</color>",
                    $"An <color=#99ff99>unrepeatable opportunity</color> to buy a pristine <b>{l.Color}</b> <b>{l.Model}</b>. <color=#aaaaaa>I am only selling because I've acquired a rarer spec.</color>",
                    $"I am the foremost enthusiast of this chassis in the region. <color=#99ff99>This car is flawless</color>. <color=#aaaaaa>Every known <b>{l.Make}</b> issue has been preemptively solved.</color>",
                    $"A <color=#99ff99>time-capsule example</color>. <color=#aaaaaa>I've preserved the original factory decals and underbody coatings. Serious connoisseurs only.</color>",
                    $"If you've been looking for the <color=#99ff99>perfect <b>{l.Model}</b></color>, your search ends here. <color=#aaaaaa>I know the market, and you won't find better under <b>{l.Mileage:N0}</b> miles.</color>",
                    $"This is my garage queen. <color=#99ff99>Flawless paint, rebuilt internals, OEM+ spec</color>. <color=#aaaaaa>It pains me to list it, but it needs to be driven.</color>",
                    $"As a seasoned <b>{l.Make}</b> mechanic, I built this for myself. <color=#99ff99>No expense was spared</color>. <color=#aaaaaa>It runs like a Swiss watch.</color>"
                });
            }

            // ── DETAIL 1 ─────────────────────────────────────────────────────
            string detail1;
            if (lv == 1)
            {
                detail1 = Pick(rng, new[]
        {
            $"Has <b>{l.Mileage:N0} miles</b> on it which is quite a lot I know, but <color=#99ff99>it has always run</color>.",
            $"It is a <b>{l.Year}</b> model so there is definitely <color=#ff9944>some age on it</color> — <color=#aaaaaa>shows in places but nothing shocking.</color>",
            $"I am <color=#aaaaaa><b>not very mechanically minded</b> so I cannot give you a detailed rundown</color>, but I have tried to be honest about what I know.",
            $"A friend who knows about cars had a look at the <b>{l.Model}</b> and said it <color=#ff9944>needs a few things</color> but <color=#99ff99>nothing major</color>.",
            $"It has <b>{l.Mileage:N0} miles</b> on the clock. <color=#aaaaaa>I don't know if that's high for a <b>{l.Make}</b>, but most of them were on the motorway.</color>",
            $"Being a <b>{l.Year}</b>, it's got <color=#ff9944>a few marks here and there</color>. <color=#aaaaaa>I think they call it 'character'!</color>",
            $"The <b>{l.Model}</b> is showing <b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>I've not noticed anything major, but then again, I just drive it.</color>",
            $"I've had the <b>{l.Make}</b> for a while. <color=#99ff99>It’s never really let me down</color>, but <color=#aaaaaa>I'm not the type to poke around under the bonnet.</color>",
            $"It's a <b>{l.Year}</b> model. <color=#aaaaaa>I bought it because I liked the color, I didn't really look at the engine much.</color>",
            $"The mileage is <b>{l.Mileage:N0}</b>. <color=#aaaaaa>I've tried to keep an eye on the oil levels every few months.</color>",
            $"I'm <color=#aaaaaa><b>not a car expert</b></color>, but the <b>{l.Model}</b> seems to <color=#99ff99>go through the gears okay</color>. <color=#aaaaaa>No loud bangs yet!</color>",
            $"A guy at the petrol station said these <b>{l.Make}</b>s are <color=#99ff99>built like tanks</color>. <color=#aaaaaa>I hope he was right!</color>",
            $"It's done <b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>I've got some paper scraps in the dash from the last service.</color>",
            $"The <b>{l.Year}</b> <b>{l.Model}</b> <color=#99ff99>feels okay on the road</color>. <color=#ff9944>It’s a bit floaty</color>, <color=#aaaaaa>but I think it’s supposed to be like that.</color>",
            $"I've got the logbook and <b>{l.Mileage:N0} miles</b> on the odometer. <color=#aaaaaa>Everything seems to be where it should be.</color>",
            $"Being a <b>{l.Color}</b> car, it stays quite cool in the summer. <color=#aaaaaa>That's about the extent of my technical knowledge.</color>",
            $"The <b>{l.Make}</b> is an old friend now. <b>{l.Mileage:N0} miles</b> and counting. <color=#aaaaaa>I've never had a reason to doubt it.</color>",
            $"I asked my brother to look at the <b>{l.Model}</b> and he said it <color=#99ff99>'sounds healthy'</color>. <color=#aaaaaa>He works in a bank, but he likes cars.</color>",
            $"It's a <b>{l.Year}</b>. <color=#ff9944>There's a bit of wear on the driver's seat</color>, but <color=#99ff99>it's still comfy enough</color>.",
            $"The <b>{l.Mileage:N0} miles</b> are <color=#99ff99>all genuine</color>. <color=#aaaaaa>I've mostly used it to visit my mum on weekends.</color>",
            $"I <color=#aaaaaa>don't really understand all the technical stuff</color>, but the <b>{l.Make}</b> <b>{l.Model}</b> <color=#99ff99>starts first time every time</color>.",
            $"The <b>{l.Year}</b> model has <b>{l.Mileage:N0} miles</b>. <color=#99ff99>I've kept the receipts</color> for the new tyres I got last year.",
            $"It's <b>{l.Color}</b>, which is a nice shade. <b>{l.Mileage:N0}</b> mileage is <color=#aaaaaa>what I'm told is average for its age.</color>",
            $"I once saw a <b>{l.Make}</b> with double this <b>{l.Mileage:N0} mileage</b>, <color=#99ff99>so it's got plenty of life left!</color>",
            $"The <b>{l.Model}</b> <color=#ff9944>isn't a new car</color>, so don't expect one. <color=#99ff99>But for a <b>{l.Year}</b>, it's doing alright.</color>",
            $"I've got some service history for the <b>{l.Make}</b>. <color=#aaaaaa>It's mostly stamps from the local 'while-you-wait' place.</color>",
            $"The <b>{l.Mileage:N0}</b> on the clock is all mine. <color=#aaaaaa>I've never been a fast driver.</color>",
            $"I'm selling the <b>{l.Model}</b> exactly as I've been driving it. <color=#aaaaaa>I haven't even had time to hoover it yet, sorry!</color>"
        });
            }
            else if (lv == 2)
            {
                detail1 = Pick(rng, new[]
                {
                    $"Service history is <color=#99ff99>up to date</color> with stamps for all major intervals. <color=#aaaaaa>Last oil and filter change was 3,000 miles ago.</color>",
                    $"Technically, the car is <color=#99ff99>consistent with its mileage</color>. <color=#aaaaaa>No major surprises in the service book, everything is documented.</color>",
                    $"Most consumables have been <color=#99ff99>replaced recently</color>. <color=#aaaaaa>Alternator and battery are both less than a year old.</color>",
                    $"I have <color=#99ff99>checked the compression</color> and it's healthy across all cylinders. <color=#aaaaaa>The engine is strong for a <b>{l.Year}</b> car.</color>",
                    $"Cooling system has been <color=#99ff99>pressure tested</color> and holds fine. <color=#aaaaaa>No signs of internal leaks or head gasket issues at this time.</color>",
                    $"Gearbox was <color=#99ff99>serviced with fresh fluid</color> recently. <color=#aaaaaa>Shifts are smooth both cold and hot. No whining from the diff.</color>",
                    $"Brake lines and fuel hoses have been <color=#99ff99>visually inspected</color> on a ramp. <color=#aaaaaa>All solid with no signs of excessive corrosion.</color>",
                    $"<b>{l.Mileage:N0}</b> miles on the clock, but <color=#99ff99>regular maintenance</color> means it drives better than most lower-mileage examples.",
                    $"All <color=#99ff99>major recalls</color> for this <b>{l.Make}</b> model have been addressed. <color=#aaaaaa>Paperwork is available to prove it.</color>",
                    $"The OBDII scan <color=#99ff99>returns no stored codes</color>. <color=#aaaaaa>All sensors are reading within the expected parameters.</color>"
                });
            }
            else
            {
                detail1 = Pick(rng, new[]
                {
                    $"Mileage <color=#99ff99>verified at {l.Mileage:N0}</color> and backed up by <color=#99ff99>full service history</color> — <color=#aaaaaa>every entry stamped and dated.</color>",
                    $"Cambelt history is <color=#99ff99>documented</color> — <color=#aaaaaa>done at the correct interval, receipt is here.</color>",
                    $"I've owned worse and sold them honestly. <color=#99ff99>This one I'm genuinely proud of.</color>",
                    $"I exclusively use <color=#99ff99>OEM or high-tier aftermarket parts</color>. <color=#aaaaaa>You won't find a single cheap unbranded sensor on this <b>{l.Model}</b>.</color>",
                    $"At <b>{l.Mileage:N0} miles</b>, the common <b>{l.Make}</b> failure points like the <color=#99ff99>water pump and thermostat housing</color> have already been upgraded.",
                    $"I keep a comprehensive Excel spreadsheet of every fuel fill-up and oil change. <color=#99ff99>It averages healthy compression across all cylinders</color>.",
                    $"Oil changed strictly every <color=#99ff99>5,000 miles</color> with fully synthetic 5W-40. <color=#aaaaaa>I don't believe in the manufacturer's 'long-life' 15k intervals.</color>",
                    $"The <b>{l.Model}</b>'s notorious weak point is the cooling system, so I <color=#99ff99>overhauled the entire circuit</color> <color=#aaaaaa>with an aluminum radiator and silicone hoses.</color>",
                    $"I have the original build sheet, window sticker, and <color=#99ff99>every MOT certificate</color> since it rolled off the line in <b>{l.Year}</b>.",
                    $"Valves were adjusted at <b>{l.Mileage * 0.9:N0} miles</b>. <color=#99ff99>It pulls cleanly to the redline</color> <color=#aaaaaa>without a hint of hesitation or misfire.</color>",
                    $"Unlike most <b>{l.Make}</b>s out there, this one has a <color=#99ff99>completely dry underside</color>. <color=#aaaaaa>I replaced the rear main seal and sump gasket last year.</color>",
                    $"Transmission fluid was flushed at the correct interval. <color=#99ff99>Gears engage with a satisfying mechanical click</color> <color=#aaaaaa>— synchros are in fantastic shape.</color>"
                });
            }

            // ── DETAIL 2 (optional ~70%) ─────────────────────────────────────
            string detail2;
            if (lv == 1)
            {
                detail2 = MaybePick(rng, new[]
                {
            $"Tyres on the <b>{l.Make}</b> <color=#99ff99>look alright to me</color> but <color=#aaaaaa>I am no expert — might be worth a proper look.</color>",
            $"Interior of this <b>{l.Model}</b> is <color=#ff9944>a bit lived in</color> but <color=#99ff99>clean enough</color>, nothing broken inside.",
            $"Bought it from a private sale in <b>{l.Location}</b> and <color=#99ff99>it has been fine since</color>.",
            $"<color=#99ff99>Never had any warning lights</color> on the dash the whole time I have owned it.",
            $"<color=#aaaaaa>Had it looked over by a local garage a while back and they said it was fine.</color>",
            $"The spare tyre in the <b>{l.Make}</b> <color=#99ff99>has never been used</color>, <color=#aaaaaa>which is a good sign, I guess?</color>",
            $"I think the air conditioning in the <b>{l.Model}</b> <color=#ff9944>needs a top-up</color>, <color=#aaaaaa>it's not as cold as it used to be.</color>",
            $"The radio works fine, though <color=#ff9944>the volume knob is a bit sticky sometimes</color>.",
            $"It's been parked on my drive in <b>{l.Location}</b>. <color=#99ff99>Safe neighborhood.</color>",
            $"The back seats of the <b>{l.Make}</b> have <color=#99ff99>hardly been sat in</color>, <color=#aaaaaa>mostly just had my coat on them.</color>",
            $"I've still got <color=#99ff99>both keys</color> for the <b>{l.Year}</b> <b>{l.Model}</b>. <color=#aaaaaa>That's quite rare for an old car, apparently.</color>",
            $"The <b>{l.Color}</b> paint has <color=#ff9944>a few stone chips</color> on the front. <color=#aaaaaa>I think that's just from the motorway.</color>",
            $"I had the <color=#99ff99>battery replaced</color> in the <b>{l.Make}</b> last winter when it got really cold.",
            $"The wipers on the <b>{l.Model}</b> are <color=#99ff99>brand new</color>, <color=#aaaaaa>I changed them myself last week!</color>",
            $"<color=#99ff99>Never been involved in any accidents</color> that I know of. <color=#aaaaaa>I've certainly never crashed it.</color>",
            $"The electric windows in the <b>{l.Year}</b> <b>{l.Make}</b> <color=#99ff99>all go up and down fine</color>, <color=#aaaaaa>which is always a relief.</color>",
            $"I've got the <color=#99ff99>original manual</color> for the <b>{l.Model}</b> in the glovebox. <color=#aaaaaa>I've never read it, though.</color>",
            $"The <b>{l.Color}</b> <color=#99ff99>looks really nice</color> when the sun hits it. <color=#aaaaaa>It's my favorite part of the car.</color>",
            $"I've only ever used 'premium' fuel in this <b>{l.Make}</b>, <color=#aaaaaa>my uncle said it keeps the engine clean.</color>",
            $"The boot in the <b>{l.Model}</b> is <color=#99ff99>actually quite big</color>, <color=#aaaaaa>fit my golf clubs in there no problem.</color>",
            $"There's <color=#ff9944>a little bit of rust</color> on the wheel arch of the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Nothing a bit of paint wouldn't hide.</color>",
            $"The interior of the <b>{l.Model}</b> <color=#99ff99>doesn't smell of dogs or smoke</color>, <color=#aaaaaa>I can't stand that.</color>",
            $"I've got a folder of MOT certificates for the <b>{l.Make}</b> in the house somewhere.",
            $"The clutch on the <b>{l.Year}</b> <b>{l.Model}</b> <color=#99ff99>feels fine to me</color>, <color=#aaaaaa>not too heavy or anything.</color>",
            $"I bought some floor mats for the <b>{l.Make}</b> to <color=#99ff99>keep the carpets nice</color>. <color=#aaaaaa>You can have them too.</color>",
            $"The <b>{l.Color}</b> <b>{l.Model}</b> is <color=#99ff99>quite easy to park</color>, <color=#aaaaaa>the mirrors are nice and big.</color>",
            $"It's a <color=#99ff99>non-smoker car</color>, <color=#aaaaaa>and I don't have any pets, so it's pretty clean inside.</color>",
            $"I've always <color=#99ff99>warmed the <b>{l.Make}</b> up</color> for a minute before driving off on cold days.",
            $"The <b>{l.Year}</b> <b>{l.Model}</b> has a <color=#99ff99>full-size spare wheel</color>, <color=#aaaaaa>not one of those tiny 'space saver' ones.</color>",
            $"I had the tracking done on the <b>{l.Make}</b> recently because it was <color=#ff9944>pulling to the left a bit</color>.",
            $"The <b>{l.Model}</b> has a <color=#99ff99>decent sound system</color>, <color=#aaaaaa>plenty of bass for when you're stuck in traffic.</color>",
            $"I've <color=#99ff99>never taken this <b>{l.Year}</b> <b>{l.Make}</b> through a car wash</color>, <color=#aaaaaa>I always do it by hand with a sponge.</color>",
            $"The <b>{l.Color}</b> paint is <color=#99ff99>all original</color> as far as I can tell. <color=#aaaaaa>No weird patches.</color>",
            $"It’s been a <color=#99ff99>really lucky car</color> for me, <color=#aaaaaa>hope it brings the same luck to you!</color>",
            $"The <b>{l.Model}</b> <color=#99ff99>comes with a half tank of petrol</color>, <color=#aaaaaa>so you can at least get home!</color>"
        }, 0.70);
            }
            else if (lv == 2)
            {
                detail2 = MaybePick(rng, new[]
                {
                    $"The <b>{l.Color}</b> bodywork is <color=#99ff99>solid</color>, <color=#aaaaaa>just standard age-related marks. No structural rust to speak of.</color>",
                    $"Suspension is <color=#99ff99>firm and responsive</color>. <color=#aaaaaa>I've replaced the front bushings recently so it doesn't wander on the road.</color>",
                    $"Tires are all <color=#99ff99>matching brands</color> with 4-5mm of tread left. <color=#aaaaaa>Brake pads were changed about 5,000 miles ago.</color>",
                    $"Interior is <color=#99ff99>clean and functional</color>. <color=#aaaaaa>No major wear on the driver's seat bolster or steering wheel.</color>",
                    $"Electrics are <color=#99ff99>fully operational</color>. <color=#aaaaaa>Air conditioning blows cold and all windows move freely.</color>",
                    $"The clutch <color=#99ff99>has a good bite point</color>. <color=#aaaaaa>Doesn't slip even under heavy load. Flywheel is quiet on idle.</color>",
                    $"Wheel alignment was <color=#99ff99>done last month</color>. <color=#aaaaaa>It drives straight as an arrow. No vibration through the steering wheel at speed.</color>",
                    $"Exhaust is <color=#99ff99>gas-tight</color> and the catalytic converter is performing correctly. <color=#aaaaaa>Passed emissions testing with no issues.</color>",
                    $"Underside is <color=#99ff99>surprisingly clean</color> for a car from <b>{l.Year}</b>. <color=#aaaaaa>I've kept it clear of salt and road grime as much as possible.</color>",
                    $"Brake rotors have <color=#99ff99>plenty of life left</color>. <color=#aaaaaa>No warping or juddering during heavy braking.</color>"
                }, 0.70);
            }
            else
            {
                detail2 = MaybePick(rng, new[]
                {
            $"The <b>{l.Color}</b> paintwork is <color=#99ff99>generally straight</color>, <color=#aaaaaa>just your standard stone chips on the front bumper and minor swirl marks.</color>",
            $"Interior is intact. <color=#aaaaaa>No ripped bolsters on the seats, and all the switchgear works exactly as intended.</color>",
            $"<color=#99ff99>Tyres have plenty of tread left</color>, at least 5mm all around. <color=#aaaaaa>Brake discs have a slight lip but nothing to worry about yet.</color>",
            $"It's always been kept reasonably clean. <color=#aaaaaa>No warning lights on the dash (other than the standard ignition sequence).</color>",
            $"The clutch on this <b>{l.Make}</b> <color=#99ff99>bites exactly where it should</color>. <color=#aaaaaa>Gearbox is smooth, no crunching into second or third.</color>",
            $"<color=#aaaaaa>I've got the V5 logbook in my name and two sets of keys.</color>",
            $"Tyres are all <color=#99ff99>matching brand with plenty of tread</color>. <color=#aaaaaa>Not the kind of thing you usually see on a {l.Year}.</color>",
            $"Paint is in <color=#99ff99>remarkable condition for the age</color> — <color=#aaaaaa>no fading, no chips worth mentioning.</color>",
            $"Interior shows <color=#99ff99>barely any wear</color> — <color=#aaaaaa>this car has been treated properly its entire life.</color>",
            $"I've treated the cavities with <color=#99ff99>Dinitrol rust inhibitor</color>. <color=#aaaaaa>If you know these chassis, you know the rear arches rot if you don't protect them.</color>",
            $"The <b>{l.Color}</b> paint has been <color=#99ff99>two-stage corrected and ceramic coated</color>. <color=#aaaaaa>Water beads off it instantly. Washed only using the two-bucket method.</color>",
            $"It sits on a set of <color=#99ff99>premium Michelin Pilot Sports</color>. <color=#aaaaaa>I never skimp on rubber or brakes, it's the most important part of the car.</color>",
            $"The factory <b>{l.Model}</b> headlights usually fog up, but <color=#99ff99>I polished and UV-sealed these</color> <color=#aaaaaa>so they are crystal clear and project perfectly.</color>",
            $"I sourced the rare OEM floor mats and the correct <color=#99ff99>period-accurate Becker stereo</color>. <color=#aaaaaa>It's all about preserving the original aesthetic for me.</color>",
            $"Suspension geometry was set up on a Hunter alignment rig last month. <color=#99ff99>It tracks perfectly straight</color> <color=#aaaaaa>with zero uneven tire wear.</color>"
        }, 0.70);
            }

            // ── FAULT ────────────────────────────────────────────────────────
            string fault = DominantFaultLine(l, SellerArchetype.Honest, lv, rng);
            if (lv == 1 && fault != null && rng.NextDouble() < 0.25)
                fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────
            string closer;
            if (lv == 1)
            {
                closer = Pick(rng, fault != null ? new[]
                {
            $"<color=#aaaaaa>Priced low to account for that</color> — someone who knows what they are doing with a <b>{l.Make}</b> will get <color=#99ff99>good value here</color>.",
            $"That is why the price for the <b>{l.Model}</b> is what it is — <color=#aaaaaa>not trying to hide anything.</color>",
            $"I would rather be <color=#99ff99>upfront and price it honestly</color> than waste everyone's time.",
            $"I've <color=#ff9944>lowered the price because of the noise</color>, <color=#aaaaaa>I think it's fair for someone who can fix it.</color>",
            $"If you're handy with a wrench, this <b>{l.Make}</b> is a <color=#99ff99>bargain</color>. <color=#aaaaaa>I just can't deal with it anymore.</color>",
            $"<color=#99ff99>Priced for a quick sale</color> due to the issues mentioned. <color=#aaaaaa>No point in me lying about it.</color>",
            $"It's a <b>{l.Model}</b> with <color=#ff9944>a bit of a headache</color>, hence the low price. <color=#aaaaaa>Any inspection is welcome.</color>",
            $"I'd rather be <b>honest about the faults</b> and sell it cheap than have someone complain later.",
            $"Selling <color=#ff9944>as it is</color>. <color=#aaaaaa>The price reflects the fact it needs a little bit of love.</color>",
            $"I've been as <color=#99ff99>clear as I can</color> about the problems with the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Open to sensible offers.</color>",
            $"This <b>{l.Model}</b> <color=#ff9944>needs a bit of work</color>, but the price is right. <color=#aaaaaa>I just want it off my drive now.</color>",
            $"<color=#aaaaaa>I'm not a mechanic, so I've priced it low enough</color> that you can afford to take it to one.",
            $"Listing it honestly so we don't waste each other's time. <color=#aaaaaa>The price is firm-ish.</color>",
            $"Take it as it stands. It’s a <color=#ff9944>cheap <b>{l.Make}</b> for a reason</color>, but <color=#99ff99>it still has potential</color>.",
            $"The <b>{l.Year}</b> <b>{l.Model}</b> is <color=#99ff99>priced to go</color>. <color=#aaaaaa>I've accounted for the fault in the asking price.</color>",
            $"If you can ignore the issue, it's a <color=#99ff99>great car</color>. <color=#aaaaaa>Or just fix it and have a bargain!</color>",
            $"I'm <color=#99ff99><b>not hiding anything</b></color>, that's why the <b>{l.Make}</b> is so cheap. <color=#aaaaaa>Come and see for yourself.</color>",
            $"It’s a bit of a gamble, maybe, but for this price the <b>{l.Model}</b> is worth a look.",
            $"Price is low because I don't want any hassle. <color=#aaaaaa>You know what you're buying.</color>",
            $"Selling for <color=#ff9944>'spares or repairs'</color> really, <color=#aaaaaa>though it does still drive if you're brave!</color>",
            $"I've told you everything I know. <color=#aaaaaa>Price is negotiable, but be fair.</color>"
                } : new[]
                {
            $"Priced to <color=#99ff99>reflect what it is</color> — not asking the earth.",
            $"Selling the <b>{l.Make}</b> because I got something newer, <color=#99ff99>no longer needed</color>.",
            $"Come and have a look at the <b>{l.Model}</b> if you are interested, <color=#aaaaaa>no pressure at all.</color>",
            $"Just want it gone to a <color=#99ff99>good home</color>, priced accordingly.",
            $"It's a <color=#99ff99>solid <b>{l.Make}</b> for the money</color>. <color=#aaaaaa>First person to see it will probably take it.</color>",
            $"Just a <color=#99ff99><b>genuine, honest car</b></color> looking for a new home. <color=#aaaaaa>No games here.</color>",
            $"The <b>{l.Year}</b> <b>{l.Model}</b> is <color=#99ff99>ready to drive away today</color>. <color=#aaaaaa>Hope you like it!</color>",
            $"I've priced it to sell quickly as I've already got my new car. <color=#aaaaaa>No silly offers please.</color>",
            $"Come and have a look, <color=#aaaaaa>bring a friend who knows about <b>{l.Make}</b>s if you like!</color>",
            $"It's been a <color=#99ff99>great car for me</color> and I'm sure it will be for you too. <color=#aaaaaa>Good luck!</color>",
            $"Selling the <b>{l.Model}</b> at what I think is a <color=#99ff99>fair price</color>. <color=#aaaaaa>Not looking to make a fortune.</color>",
            $"I'm available most evenings if you want to come and test drive the <b>{l.Make}</b>.",
            $"A very <color=#99ff99><b>straightforward sale</b></color> for a straightforward <b>{l.Year}</b> <b>{l.Model}</b>.",
            $"I've tried to be as accurate as I can. It's just a <color=#99ff99>good, reliable vehicle</color>.",
            $"No rush to sell, but I'd like the <b>{l.Make}</b> to go to someone who will use it.",
            $"Feel free to ask any questions, <color=#aaaaaa>though I might have to ask my brother for the technical bits!</color>",
            $"The <b>{l.Model}</b> is a <color=#99ff99>good buy</color> for anyone wanting a simple car.",
            $"<color=#99ff99>Priced fairly</color> according to the market. <color=#aaaaaa>It’s a lot of car for the money.</color>",
            $"I’ve been the only driver for years, so I <color=#99ff99>know this <b>{l.Year}</b> <b>{l.Make}</b> inside out</color>.",
            $"Just looking for a <color=#99ff99><b>hassle-free sale</b></color>. <color=#aaaaaa>Bank transfer or cash is fine.</color>",
            $"It’s a <color=#99ff99>lovely <b>{l.Color}</b> car</color> and it’s never let me down. <color=#aaaaaa>You won't be disappointed.</color>",
            $"Selling my <b>{l.Model}</b> as I’m moving abroad. <color=#aaaaaa>Need it gone by Friday if possible!</color>",
            $"This <b>{l.Make}</b> is a <color=#99ff99>real bargain</color> for someone. <color=#aaaaaa>I've kept it as clean as I could.</color>",
            $"I’m a <b>private seller</b>, so no warranties, but you’re welcome to spend as much time looking at it as you need.",
            $"The <b>{l.Year}</b> <b>{l.Model}</b> is a <color=#99ff99>great runner</color>. <color=#aaaaaa>Very cheap on insurance too, I found.</color>",
            $"Come and see it in <b>{l.Location}</b>. <color=#aaaaaa>I'll even put the kettle on!</color>",
            $"I'm selling it for exactly what I'd want to pay for it. <color=#99ff99>Fair's fair</color>.",
            $"It’s been a part of the family, this <b>{l.Make}</b>. <color=#99ff99>Treat her well!</color>"
                });
            }
            else if (lv == 2)
            {
                closer = Pick(rng, fault != null ? new[]
            {
                $"Price is <color=#ff9944>adjusted for the work required</color>. <color=#aaaaaa>I've been realistic about the technical state, so no lowball offers please.</color>",
                $"If you're capable of <color=#99ff99>doing the repairs yourself</color>, this is a bargain. <color=#aaaaaa>I simply don't have the time to fix it myself.</color>",
                $"A <color=#ff9944>fair price</color> for a <b>{l.Model}</b> that needs a bit of technical TLC. <color=#aaaaaa>Viewings welcome for those who know what they're looking at.</color>",
                $"The issues are <color=#ff9944>factored into the asking price</color>. <color=#aaaaaa>I know the market value for a sorted one, and this isn't it yet.</color>",
                $"Selling as is. <color=#aaaaaa>I've been honest about the faults to avoid wasting anyone's time. Grab a trailer and a toolbox.</color>",
                $"I’ve listed the <color=#ff9944>technical defects</color> clearly. <color=#aaaaaa>The car is priced based on professional labor rates for these fixes.</color>"
            } : new[]
            {
                $"This is a <color=#99ff99>straightforward sale</color> of a solid car. <color=#aaaaaa>I'm not in a rush, but I won't entertain time-wasters.</color>",
                $"Technically <color=#99ff99>one of the better examples</color> available. <color=#aaaaaa>Price is firm based on condition and history.</color>",
                $"Ready to be <color=#99ff99>driven away today</color>. <color=#aaaaaa>Full documentation and both keys present. First serious buyer will take it.</color>",
                $"If you want an <color=#99ff99>honest car with no hidden issues</color>, this is it. <color=#aaaaaa>Test drives welcome for serious buyers with insurance.</color>",
                $"Standard transaction. <color=#aaaaaa>The car is as described. I believe in fair pricing for a well-maintained vehicle.</color>",
                $"Reliable <b>{l.Make}</b>. <color=#aaaaaa>Come and see it for yourself. I'm happy to put it on a jack if you want to see the underside.</color>"
            });
            }
            else
            {
                closer = Pick(rng, fault != null ? new[]
                {
                $"The asking price reflects the <color=#99ff99>honest condition</color>. <color=#aaaaaa>Happy to accommodate any mechanical inspection — I have nothing to hide.</color>",
                $"I know the current market for a <b>{l.Year}</b> <b>{l.Model}</b>. <color=#aaaaaa>The fault is priced in. Serious offers only, in person.</color>",
                $"I've been selling cars long enough to know that <color=#99ff99>honesty saves everyone time</color>. <color=#aaaaaa>The issue is disclosed, the price reflects it — nothing more to say.</color>",
                $"<color=#99ff99>No games, no hidden issues</color> beyond what is listed. <color=#aaaaaa>First person to view and verify will likely buy it.</color>",
                $"Priced at what it actually is, <color=#ff9944>not what it could be</color>. <color=#aaaaaa>Receipts and records available on viewing.</color>",
                $"As a seasoned <b>{l.Make}</b> owner, I've diagnosed the fault accurately. <color=#aaaaaa>You won't have to guess what's wrong. I've discounted the asking price by the exact cost of OEM parts to fix it.</color>",
                $"I don't have the bandwidth to tackle this final repair. <color=#aaaaaa>My asking price accounts for the labor hours a reputable specialist would charge you. No lowballing.</color>",
                $"I respect your time, so please respect mine. <color=#aaaaaa>The flaw is clearly stated. If you know these cars, you know it's a straightforward fix for the right mechanic.</color>",
                $"This is a strictly no-nonsense sale. <color=#aaaaaa>The car needs the work I've detailed. Come with a code reader, put it on ramps, I welcome any expert inspection.</color>",
                $"I'm an enthusiast, not a charity. <color=#aaaaaa>The flaw is factored in down to the penny. Don't message me offering half; I know exactly what the shell alone is worth.</color>",
                $"You are buying a known quantity. <color=#aaaaaa>I've saved you the diagnostic fees. Grab the required parts, dedicate a weekend, and you'll have a sorted <b>{l.Model}</b>.</color>",
                $"Every <b>{l.Year}</b> <b>{l.Make}</b> has issues. <color=#aaaaaa>I'm just the only seller honest enough to list them. Price is firm, based on current forum valuation guides.</color>"
            } : new[]
                {
                $"The asking price reflects the <color=#99ff99>honest condition</color>. <color=#aaaaaa>Happy to accommodate any mechanical inspection.</color>",
                $"I know the current market for a <b>{l.Year}</b> <b>{l.Model}</b>. <color=#aaaaaa>My price is fair — serious offers only in person.</color>",
                $"<color=#99ff99>No games, no hidden issues.</color> <color=#aaaaaa>First person to view and test drive will likely buy it.</color>",
                $"Viewing is highly recommended. <color=#aaaaaa>I'm an enthusiast, not a dealer — expect a straightforward, honest transaction.</color>",
                $"I've been as descriptive as possible. <color=#aaaaaa>Feel free to message me, but please don't ask 'what's your lowest price'.</color>",
                $"This is a purist's car. <color=#aaaaaa>I will gladly talk you through the entire maintenance log over a coffee, but the price is non-negotiable.</color>",
                $"I've nurtured this <b>{l.Make}</b> for years. <color=#aaaaaa>I'm in no rush to sell and will only let it go to a fellow enthusiast who will continue to preserve it.</color>",
                $"You could buy a cheaper <b>{l.Model}</b>, <color=#aaaaaa>but you'll spend double making it as good as this one. Buy on condition, not just the sticker price.</color>",
                $"I have a reputation in the local <b>{l.Make}</b> community. <color=#aaaaaa>I do not sell junk. Bring your micrometer and paint depth gauge, you will not find a fault.</color>",
                $"An absolute turn-key classic. <color=#aaaaaa>I've done all the heavy lifting so you don't have to. Pay the asking price, drive it home, enjoy.</color>",
                $"I expect the buyer to know what they are looking at. <color=#aaaaaa>I won't entertain time-wasters or test pilots. Genuine inquiries only.</color>",
                $"This is the sort of car you buy and hold onto forever. <color=#aaaaaa>I'll be genuinely sad to see it roll down the driveway. Next owner is getting a gem.</color>"
            });
            }

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  WRECKER
        // ══════════════════════════════════════════════════════════════════════

        private static string BuildWrecker(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ───────────────────────────────────────────────────────────────
            string opener;
            if (lv == 1)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
            $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. <color=#ff9944>It has seen better days</color> if I am honest. <color=#aaaaaa>But it still drives and that is more than I expected at this point.</color>",
            $"This <b>{l.Model}</b> has been on my drive for a while now. <color=#ff9944>Not in perfect shape.</color> <color=#aaaaaa>I kept meaning to sort it out and then other things came up.</color>",
            $"Old <b>{l.Year}</b> <b>{l.Make}</b> for sale. <color=#ff9944>A bit rough</color>, not going to lie. <color=#aaaaaa>Does start though. Most of the time.</color>",
            $"Selling the <b>{l.Model}</b> as-is. <color=#ff9944>It is what it is</color> at this point. <color=#aaaaaa>I drove it for years without big problems but lately things are adding up.</color>",
            $"Time to let this <b>{l.Color}</b> <b>{l.Make}</b> go. <color=#ff9944>Not going to pretend it is in great shape.</color> <color=#aaaaaa>Someone will get more out of it than it sitting on my drive doing nothing.</color>",
            $"<b>{l.Year}</b> <b>{l.Model}</b> for sale. <color=#ff9944>Been sat for a bit</color>, started fine last week. <color=#aaaaaa>Before that it was my daily but things have been a bit hectic.</color>",
            $"This <b>{l.Make}</b> needs some love. <color=#ff9944>Not a lot, probably</color>, but some. <color=#aaaaaa>I am not the right person to give it that love. Maybe you are.</color>",
            $"Selling my <b>{l.Color}</b> <b>{l.Model}</b>. <color=#ff9944>It has a few things wrong with it</color> that I never properly looked into. <color=#aaaaaa>Priced with that in mind.</color>",
        }) : IsMid(l) ? Pick(rng, new[]
                {
            $"Good runner as far as I know. Selling the <b>{l.Year}</b> <b>{l.Make}</b> as-is and the price reflects that.",
            $"Been sitting on the drive more than being driven lately if I am honest. <b>{l.Model}</b> starts fine though.",
            $"Starts every time I have tried it. Just needs someone who will actually use the <b>{l.Make}</b>.",
            $"Used the <b>{l.Color}</b> <b>{l.Year}</b> <b>{l.Model}</b> regularly until a few months ago. Has been parked since. Nothing dramatic happened.",
            $"Selling the <b>{l.Make}</b> because I got something else. It has been a decent car, no serious complaints.",
            $"This <b>{l.Model}</b> has just been sitting. Runs, drives, no major problems that I know of. Just do not need it anymore.",
            $"<b>{l.Year}</b> <b>{l.Make}</b> for sale. Not much to say really. It goes and it stops and the heater works.",
            $"I drove this <b>{l.Color}</b> <b>{l.Model}</b> to work every day for two years. Did the job. Just upgrading now.",
        }) : Pick(rng, new[]
                {
            $"Selling my <b>{l.Year}</b> <b>{l.Make}</b>. It has been a <color=#99ff99>solid car</color> for me. No drama, always started.",
            $"This <b>{l.Model}</b> has been <color=#99ff99>dead reliable</color> the whole time I have owned it. Only selling because I do not need it.",
            $"<color=#99ff99>Good car this</color>. The <b>{l.Year}</b> <b>{l.Make}</b> has never let me down. Just sitting unused now.",
            $"The <b>{l.Color}</b> <b>{l.Model}</b> has been <color=#99ff99>a really easy car to live with</color>. Never costs me anything, never breaks down.",
            $"Owned the <b>{l.Make}</b> for a few years now. <color=#99ff99>Nothing has ever gone wrong with it</color>. Getting a new one so this needs to go.",
            $"This <b>{l.Year}</b> <b>{l.Model}</b> is one of those cars that just <color=#99ff99>never causes any bother</color>. Happy to sell to someone who will use it.",
        });
            }
            else if (lv == 2)
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                $"Selling the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#ff9944>It is not in good shape</color> and I am not going to pretend otherwise. <color=#aaaaaa>Has been sitting since the problems started adding up.</color>",
                $"This <b>{l.Model}</b> needs work. <color=#ff9944>More than I am willing to put into it at this point.</color> <color=#aaaaaa>Parked it up when it stopped making sense to keep driving it.</color>",
                $"The <b>{l.Color}</b> <b>{l.Make}</b> has been off the road for a while. <color=#ff9944>There are reasons for that</color> and they are listed below. <color=#aaaaaa>Not a runner in the current state, be realistic.</color>",
                $"I know this <b>{l.Year}</b> <b>{l.Model}</b> well enough to know <color=#ff9944>it needs proper attention before it goes back on the road</color>. <color=#aaaaaa>I am not the one to give it that. Priced accordingly.</color>",
                $"The <b>{l.Make}</b> has been sitting since I pulled it off the road. <color=#ff9944>It was not getting better on its own.</color> <color=#aaaaaa>Someone with the right tools and time will get more out of this than I did.</color>",
                $"Rough example of a <b>{l.Year}</b> <b>{l.Model}</b>. <color=#ff9944>I am aware of that.</color> <color=#aaaaaa>Priced to match. The issues I know about are in the listing.</color>",
                $"This <b>{l.Color}</b> <b>{l.Make}</b> is a <color=#ff9944>project car at this point</color>. <color=#aaaaaa>It was a daily once. That was a while ago. Things have moved on.</color>",
                $"Not going to dress this up. The <b>{l.Year}</b> <b>{l.Model}</b> is <color=#ff9944>in poor condition</color>. <color=#aaaaaa>I stopped using it when the repair costs stopped making sense relative to the value.</color>",
            }) : IsMid(l) ? Pick(rng, new[]
                {
                $"Selling the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>It has been sitting for a while now and I need the space more than I need the car.</color>",
                $"This <b>{l.Model}</b> has been in the garage for the best part of <color=#aaaaaa>two years. It was my daily before that. Circumstances changed.</color>",
                $"The <b>{l.Color}</b> <b>{l.Make}</b> has been off the road since <color=#aaaaaa>the last issue came up. Did not feel like putting money into it at the time. Still do not.</color>",
                $"Parked the <b>{l.Year}</b> <b>{l.Model}</b> up when I got something newer. <color=#aaaaaa>Always said I would get back to it. The gap between saying and doing got too wide.</color>",
                $"This <b>{l.Make}</b> has been sitting on the drive under a cover for long enough. <color=#aaaaaa>Time for it to go somewhere it will actually be used or fixed properly.</color>",
                $"Selling the <b>{l.Model}</b> that has been <color=#aaaaaa>taking up space in the unit since last year. Good bones, just needs attention I do not have time for.</color>",
                $"The <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Make}</b> has been standing since I replaced it. <color=#aaaaaa>Kept it thinking I would fix and flip it. Life had other plans.</color>",
                $"This <b>{l.Model}</b> was my backup car until the backup became the problem. <color=#aaaaaa>Has been on the driveway since. Selling now to clear the space.</color>",
            }) : Pick(rng, new[]
                {
                $"The <b>{l.Year}</b> <b>{l.Make}</b> has been <color=#aaaaaa>standing but it is in decent shape for what it is. Selling because I have too many and this one draws the short straw.</color>",
                $"This <b>{l.Model}</b> is <color=#99ff99>one of the better ones I have had sitting around</color>. <color=#aaaaaa>Mechanically it was solid before storage. Should not take much to bring it back.</color>",
                $"Selling the <b>{l.Color}</b> <b>{l.Make}</b>. <color=#aaaaaa>It has been stored rather than driven but the condition is <color=#99ff99>reasonable for the age and mileage</color>. No horror stories.</color>",
                $"The <b>{l.Year}</b> <b>{l.Model}</b> was <color=#99ff99>running well when I took it off the road</color>. <color=#aaaaaa>Parked it because I got something else, not because anything went wrong.</color>",
                $"This <b>{l.Make}</b> is <color=#99ff99>in better shape than most sitting cars</color>. <color=#aaaaaa>I stored it properly and it was in good condition when it went away. The mileage and age are honest.</color>",
                $"Decent <b>{l.Year}</b> <b>{l.Model}</b> that has <color=#aaaaaa>been off the road by choice rather than necessity. <color=#99ff99>Nothing terminal happened to it</color>. Just surplus to requirements.</color>",
                $"The <b>{l.Color}</b> <b>{l.Make}</b> was <color=#99ff99>a reliable car</color> before I parked it up. <color=#aaaaaa>Selling it on because I never got around to using it as the spare I intended it to be.</color>",
            });
            }
            else
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                $"The <b>{l.Year}</b> <b>{l.Make}</b> is a <color=#ff9944>project</color>. <color=#aaaaaa>I am not going to oversell it. The faults are listed, the price reflects them. If you know these cars you will see the value. If you do not, this is not the right buy for you.</color>",
                $"Selling a <color=#ff9944>non-runner</color> <b>{l.Model}</b>. <color=#aaaaaa>I deal in these regularly. I price them properly and I describe them honestly. What is wrong with this one is listed below in plain language.</color>",
                $"The <b>{l.Color}</b> <b>{l.Make}</b> needs work. <color=#ff9944>Significant work.</color> <color=#aaaaaa>I know what it needs, I know what that costs, and I have priced it accordingly. No inflation, no games. I have a reputation to maintain.</color>",
                $"This <b>{l.Year}</b> <b>{l.Model}</b> is being sold as the <color=#ff9944>project car it is</color>. <color=#aaaaaa>I deal in distressed vehicles. I set prices based on actual repair costs, not hope. Read the fault list and trust the maths.</color>",
                $"<color=#ff9944>Rough condition</color> on this <b>{l.Make}</b>. <color=#aaaaaa>I inspected it before pricing it. Always do. The issues are documented below and the asking price already accounts for sorting every one of them at an independent specialist rate.</color>",
                $"The <b>{l.Year}</b> <b>{l.Color}</b> <b>{l.Model}</b> is <color=#ff9944>in poor mechanical condition</color>. <color=#aaaaaa>I am telling you that because I price these cars to sell, not to sit. Serious buyers with workshop access will get genuine value here.</color>",
            }) : IsMid(l) ? Pick(rng, new[]
                {
                $"One of several I am moving on. The <b>{l.Year}</b> <b>{l.Make}</b> is <color=#aaaaaa>a fair example — not the best in the collection, not the worst. Priced to clear space rather than maximise return.</color>",
                $"Selling the <b>{l.Model}</b> to make room. <color=#aaaaaa>I know this chassis well. It is in reasonable condition for the age and mileage. The price is set on that basis, not sentiment.</color>",
                $"The <b>{l.Color}</b> <b>{l.Make}</b> has been <color=#aaaaaa>stored while I concentrated on other things. Decent condition for a stored car. Nothing dramatic wrong with it — I would have listed that if there was.</color>",
                $"This <b>{l.Year}</b> <b>{l.Model}</b> is a <color=#aaaaaa>solid mid-range example. I deal in enough of these to price one accurately. What I am asking is fair for the condition. Not interested in lengthy negotiation.</color>",
                $"Selling the <b>{l.Make}</b> from the collection. <color=#aaaaaa>I inspected it before listing. It is what it is — a used car of this age in honest condition. Price is market rate for that. No more, no less.</color>",
                $"The <b>{l.Color}</b> <b>{l.Year}</b> <b>{l.Model}</b> has <color=#aaaaaa>been off the road but was in reasonable shape when it went away. I checked it over before pricing. Nothing came up that is not already in this listing.</color>",
            }) : Pick(rng, new[]
                {
                $"Strong example of the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>I deal in these regularly and I know what a <color=#99ff99>good one looks like</color>. This is one of them. Priced above the rough ones for a reason.</color>",
                $"The <b>{l.Model}</b> is in <color=#99ff99>genuinely good condition</color> for the age. <color=#aaaaaa>I have handled enough of these to say that with confidence. The asking price reflects condition, not wishful thinking.</color>",
                $"This <b>{l.Color}</b> <b>{l.Make}</b> is one of the <color=#99ff99>cleaner examples I have had through</color>. <color=#aaaaaa>I check everything before I price anything. What I am asking is based on what I found, not what I hope it is worth.</color>",
                $"<color=#99ff99>Solid</color> <b>{l.Year}</b> <b>{l.Model}</b>. <color=#aaaaaa>I know the common faults on these and this one is not showing them. That is worth paying for. The price reflects a car that does not need immediate remedial work.</color>",
                $"The <b>{l.Make}</b> is in <color=#99ff99>above-average condition for the mileage</color>. <color=#aaaaaa>I deal in volume so I know where on the spectrum this sits. Above average means above average price. It is a fair exchange.</color>",
                $"One of the better <b>{l.Year}</b> <b>{l.Model}</b>s I have had. <color=#aaaaaa><color=#99ff99>No nasty surprises on inspection.</color> I price these on condition and this one earns a higher number than the rough ones. Straightforward.</color>",
            });
            }

            // ── DETAIL 1 ─────────────────────────────────────────────────────────────
            string detail1;
            if (lv == 1)
            {
                detail1 = Pick(rng, new[]
                {
            $"Has <b>{l.Mileage:N0} miles</b> on it. <color=#aaaaaa>These things run for ages if you look after them. I looked after it about average I would say.</color>",
            $"Oil probably needs doing. <color=#aaaaaa>Or maybe I did it recently. Hard to keep track without writing it down, which I did not.</color>",
            $"Last proper service was a while back. <color=#aaaaaa>I could not give you an exact date. It drives fine so I never worried about it.</color>",
            $"Battery is probably the original I think. <color=#aaaaaa>Still cranks it over fine so I never saw the point in replacing it.</color>",
            $"It is a <b>{l.Year}</b> so there is some age on it. <color=#aaaaaa>But it has been reliable. Never left me on the side of the road.</color>",
            $"I have got the logbook and one key. <color=#aaaaaa>There might be a second key somewhere. I would have to have a look.</color>",
            $"Tyres look okay to me. <color=#aaaaaa>Not brand new but not bald either. There is definitely rubber on them.</color>",
            $"<b>{l.Mileage:N0}</b> on the clock which sounds like a lot but most of it was motorway miles. <color=#aaaaaa>Or at least I think it was. Previous owner said that and it seemed reasonable.</color>",
            $"Interior is <color=#aaaaaa>a bit lived in but nothing broken. All the seats are there. Both sun visors work.</color>",
            $"Never really had it properly inspected. <color=#aaaaaa>It started and drove so I assumed everything was fine. Broad logic but it seemed to hold.</color>",
            $"Air conditioning <color=#ff9944>does not work great</color>. <color=#aaaaaa>Used to be colder. I just open the windows in summer. Not the end of the world.</color>",
            $"There is a folder of bits in the glovebox. <color=#aaaaaa>Some service receipts in there I think. I never went through it thoroughly.</color>",
        });
            }
            else if (lv == 2)
            {
                detail1 = Pick(rng, new[]
            {
                $"Has <b>{l.Mileage:N0} miles</b> on the clock. <color=#aaaaaa>All genuine. Most of it was done before I parked it up.</color>",
                $"The <b>{l.Make}</b> was running fine day-to-day before it went into storage. <color=#aaaaaa>The issues that developed are listed below. Nothing came out of nowhere.</color>",
                $"I know this car reasonably well. <color=#aaaaaa>What I know about its condition is in this listing. I am not going to pretend I have done a full inspection recently.</color>",
                $"It has been <color=#ff9944>standing for over a year</color> so assume all fluids need refreshing. <color=#aaaaaa>Tyres will want checking for flat spots too. That is just what happens with storage.</color>",
                $"The <b>{l.Model}</b> was <color=#aaaaaa>last used as a daily about eighteen months ago. Before that it was reliable enough. Then one thing led to another.</color>",
                $"<b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>For a <b>{l.Year}</b> that is reasonable. Engine and gearbox were not the problem when I stopped using it.</color>",
                $"I have a rough idea of what it needs. <color=#aaaaaa>The main things are in the listing. Beyond that I cannot account for what sitting has done to the smaller stuff.</color>",
                $"The <b>{l.Color}</b> <b>{l.Make}</b> has been <color=#aaaaaa>dry stored so the body is in decent shape. Mechanically is where the question marks are.</color>",
                $"Gearbox is fine, engine turns over. <color=#aaaaaa>The issues are more specific and I have tried to list them honestly. No surprises beyond what is written here.</color>",
            });
            }
            else
            {
                detail1 = Pick(rng, new[]
                {
                $"<b>{l.Mileage:N0} miles</b> on the clock. <color=#aaaaaa>Genuine — I verify mileage before I buy these. It matches the condition and the service history fragments I have with it.</color>",
                $"I inspected the <b>{l.Make}</b> before listing it. <color=#aaaaaa>What I found is in this listing. I do not omit things — it costs me repeat business and I rely on repeat business.</color>",
                $"The <b>{l.Model}</b> has been <color=#aaaaaa>stored properly. Dry, covered, off the road intentionally rather than accidentally. That matters on a car this age.</color>",
                $"Engine starts and runs on the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>I ran it before pricing it. Gearbox selects cleanly. The issues that exist are listed — the drivetrain is not among them.</color>",
                $"I have handled <color=#aaaaaa>a lot of <b>{l.Make}</b>s over the years. I know what to look for and I looked for it. The asking price is based on what I found, not a guess.</color>",
                $"<b>{l.Mileage:N0}</b> miles. <color=#aaaaaa>For a <b>{l.Year}</b> that is consistent with the condition. Engine and gearbox are not the story here — the specific issues are listed below and those are what the price reflects.</color>",
                $"The <b>{l.Color}</b> <b>{l.Model}</b> came to me <color=#aaaaaa>as part of a purchase. I have gone through it properly. What I know about its condition is in this listing. I do not sell things I cannot describe accurately.</color>",
                $"I put the <b>{l.Make}</b> on a ramp before listing it. <color=#aaaaaa>I always do. Underneath is consistent with the age and mileage. Nothing structural. The faults I have listed are the faults there are.</color>",
                $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color=#aaaaaa>a history I can partially account for. Mileage is believable, condition matches the clock. I have priced it on what I can verify, not what I cannot.</color>",
            });
            }

            // ── DETAIL 2 (optional ~65%) ──────────────────────────────────────────────
            string detail2;
            if (lv == 1)
            {
                detail2 = MaybePick(rng, new[]
                {
            $"Drove it to <b>{l.Location}</b> and back a few times without any issues. That is probably my best endorsement.",
            $"Starts on the first or second turn most of the time. <color=#aaaaaa>Winter it sometimes takes three. Normal I think.</color>",
            $"Never heard any proper worrying noises from the engine. <color=#aaaaaa>A few minor ones but nothing that made me pull over.</color>",
            $"Interior is <color=#aaaaaa>dusty but nothing a good hoover would not sort out. Have not had time to do it before listing.</color>",
            $"Tyres have got <color=#aaaaaa>some life left on them. Not loads, but some.</color>",
            $"Bodywork has <color=#ff9944>the odd mark here and there</color>. <color=#aaaaaa>All shown in photos. Nothing dramatic.</color>",
            $"The <b>{l.Color}</b> paint has <color=#ff9944>a few stone chips</color> on the front. <color=#aaaaaa>These things happen. Does not affect how it drives.</color>",
            $"Climate control works. <color=#aaaaaa>The <color=#ff9944>passenger side vent is a bit temperamental</color> but the main controls are fine.</color>",
            $"Radio works fine. <color=#aaaaaa>The display has a dead pixel in the corner. I stuck a bit of tape over it for a while then removed the tape. The pixel is still there.</color>",
            $"Boot opens and closes fine. <color=#aaaaaa>The lock is a bit stiff but you get the hang of it.</color>",
            $"Comes with a spare tyre. <color=#aaaaaa>Whether it has any air in it I could not tell you without checking. Probably fine though.</color>",
            $"Both keys present. <color=#aaaaaa>One of the fobs <color=#ff9944>does not lock the car</color> properly. The other one works perfectly.</color>",
        }, 0.65);
            }
            else if (lv == 2)
            {
                detail2 = MaybePick(rng, new[]
                {
                $"Body is <color=#aaaaaa>in reasonable shape for a stored car. No new damage while it was sitting.</color>",
                $"Interior is <color=#aaaaaa>dusty but intact. Nothing broken or missing as far as I can see.</color>",
                $"The <b>{l.Make}</b> was <color=#aaaaaa>garaged so the paint has held up okay. Couple of old marks but nothing from the storage period.</color>",
                $"Both keys present. <color=#aaaaaa>V5 in my name. MOT lapsed while it was standing, as you would expect.</color>",
                $"Tyres look okay visually but <color=#ff9944>they have been static for a long time</color>. <color=#aaaaaa>Worth a proper check before putting miles on them.</color>",
                $"The <b>{l.Model}</b> was <color=#aaaaaa>washed before going into storage. Still looks reasonable considering how long it has been sitting.</color>",
                $"No accident history that I know of. <color=#aaaaaa>Bought it privately, had it a few years, parked it up. Straightforward history.</color>",
                $"Underneath is <color=#aaaaaa>about what you would expect for a <b>{l.Year}</b> car. Some surface corrosion on the subframe but nothing structural.</color>",
                $"Lights all work. <color=#aaaaaa>Checked them before listing. The small things are fine. The bigger issues are the ones in the description.</color>",
            }, 0.65);
            }
            else
            {
                detail2 = MaybePick(rng, new[]
                {
                $"Body on the <b>{l.Make}</b> is <color=#aaaaaa>in reasonable shape for the age. No significant accident damage that I found. Paint is original and consistent.</color>",
                $"Interior is <color=#aaaaaa>complete and intact. Wear consistent with the mileage. Nothing missing, nothing bodged.</color>",
                $"Underneath is <color=#aaaaaa>clean for a <b>{l.Year}</b> car. Sills are solid, floor is sound. I check these things because they affect value and I price on value.</color>",
                $"V5 present, in my name. <color=#aaaaaa>No outstanding finance — I check before I buy. MOT lapsed while in storage as expected.</color>",
                $"The <b>{l.Model}</b> has <color=#aaaaaa>both keys and what service paperwork I received with it. Not a full history but enough to support the mileage claim.</color>",
                $"Tyres have <color=#aaaaaa>usable tread remaining. Not new, but not dangerous. Wheels are straight and undamaged.</color>",
                $"Cooling system on the <b>{l.Make}</b> is <color=#aaaaaa>intact with no external leaks that I found. Hoses and clips are original but serviceable.</color>",
                $"The <b>{l.Color}</b> paint has <color=#aaaaaa>held up well in storage. No new corrosion while it was sitting. Existing marks are visible in the photos — no surprises on collection.</color>",
                $"Gearbox on the <b>{l.Year}</b> <b>{l.Model}</b> <color=#aaaaaa>selects all gears cleanly with no crunch. Clutch bites where it should. Drivetrain is not the issue on this car.</color>",
            }, 0.65);
            }

            // ── FAULT ────────────────────────────────────────────────────────────────
            string fault = DominantFaultLine(l, SellerArchetype.Wrecker, lv, rng);
            double faultChance = lv switch { 1 => 0.65, 2 => 0.40, _ => 0.20 };
            if (fault != null && rng.NextDouble() > faultChance) fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────────────
            string closer;
            if (lv == 1)
            {
                closer = Pick(rng, fault != null ? new[]
                {
            $"Price is <color=#ff9944>low because of that</color>. Someone who knows what they are doing will get a good deal.",
            $"Hence why the <b>{l.Model}</b> is priced where it is. Not hiding anything, just not sure what it needs.",
            $"Priced for a quick sale. I would rather someone else figure it out than it sit on my drive another year.",
            $"If you know your way around a <b>{l.Make}</b> it is probably a straightforward fix. I do not, which is why I am selling.",
            $"That is why the price is what it is. I would rather be upfront about it and sell it cheap.",
            $"Cash, collect from <b>{l.Location}</b>, no holding it. Price is firm because I have already come down to this.",
            $"Sold as-is. Everything I have noticed is in this listing. There could be other things. There probably are.",
        } : new[]
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
                $"The issue is known and the price reflects it. <color=#aaaaaa>I am not trying to hide anything, just moving the car on.</color>",
                $"Priced with the work in mind. <color=#aaaaaa>If you can do it yourself you will come out ahead. I just cannot justify the time right now.</color>",
                $"Everything relevant is in the listing. <color=#aaaaaa>No further questions I can answer better than what is written here. Come and see it if you are serious.</color>",
                $"Cash, collect from <b>{l.Location}</b>. <color=#aaaaaa>Not holding it and not negotiating via message. Come, look, decide.</color>",
                $"The <b>{l.Make}</b> is priced as a <color=#aaaaaa>car with known issues, not as a runner. If your expectations match that we will get on fine.</color>",
                $"I know what it needs. <color=#aaaaaa>I have priced it so the next owner can cover that and still come out sensible. No lowballing, I did the maths.</color>",
                $"Selling as-is from <b>{l.Location}</b>. <color=#aaaaaa>Inspection welcome, no test drives on public road until it is sorted. Sensible buyers only.</color>",
            } : new[]
                {
                $"Nothing dramatic wrong with it beyond what is listed. <color=#aaaaaa>Priced fairly for a car that has been standing. First sensible offer from <b>{l.Location}</b> takes it.</color>",
                $"Cash on collection. <color=#aaaaaa>Not looking for messages asking what my lowest price is. The price is the price.</color>",
                $"Comes as it sits. <color=#aaaaaa>I have been straight about the condition. No holding, no part exchange, no drama.</color>",
                $"Collection from <b>{l.Location}</b>. <color=#aaaaaa>I can have it ready to look at with a few hours notice. Serious buyers only, please.</color>",
                $"The <b>{l.Model}</b> is ready to go to someone who knows what they are getting. <color=#aaaaaa>Priced for what it is, not what it could be with work.</color>",
                $"If you want to bring a code reader, bring a code reader. <color=#aaaaaa>I have nothing to hide. The listing is what it is.</color>",
            });
            }
            else
            {
                closer = Pick(rng, fault != null ? new[]
                {
                $"The fault is listed, the cost is costed, the price is set. <color=#aaaaaa>I do not negotiate on cars I have priced accurately. Come and inspect it — I welcome that.</color>",
                $"I have been doing this long enough to price a <b>{l.Make}</b> correctly. <color=#aaaaaa>What I am asking already accounts for the work listed. Do not offer less — the maths does not support it.</color>",
                $"Everything wrong with the <b>{l.Model}</b> is in this listing. <color=#aaaaaa>Nothing is hidden because hiding things loses me future business. Price is final. Collection from <b>{l.Location}</b>.</color>",
                $"The asking price on the <b>{l.Year}</b> <b>{l.Make}</b> reflects the actual cost of the actual fault. <color=#aaaaaa>I checked the parts prices before I set the number. Serious buyers only — I do not have time for anything else.</color>",
                $"I deal in volume. <color=#aaaaaa>I price accurately and I move on. The <b>{l.Model}</b> is priced correctly for what it needs. Inspect it, verify what I have said, buy it or do not.</color>",
                $"No room to move on price. <color=#aaaaaa>I know what the <b>{l.Make}</b> needs, I know what that costs, and I have set the number accordingly. If my maths is wrong I am open to being shown why. Opinions are not maths.</color>",
                $"Cash on collection from <b>{l.Location}</b>. <color=#aaaaaa>No holding, no part exchange, no drawn-out viewings. I described the car accurately — the rest is straightforward.</color>",
            } : new[]
                {
                $"Price is set on condition, not on what I paid. <color=#aaaaaa>I price these accurately because my reputation depends on it. What I am asking for the <b>{l.Model}</b> is fair for what it is.</color>",
                $"I do not inflate prices and I do not drop them without reason. <color=#aaaaaa>The <b>{l.Year}</b> <b>{l.Make}</b> is priced on its merits. Come and see it if you are serious.</color>",
                $"Cash, collection from <b>{l.Location}</b>, no time wasters. <color=#aaaaaa>I described the car accurately. A sensible buyer will recognise that and we will have a quick, easy transaction.</color>",
                $"I have sold a lot of these. <color=#aaaaaa>I know what they are worth and I price them accordingly. The <b>{l.Model}</b> is not overpriced. It will not be here long.</color>",
                $"Viewing welcome with reasonable notice. <color=#aaaaaa>I stand behind my descriptions. Come and verify what I have written — you will find it matches what is in front of you.</color>",
                $"The <b>{l.Year}</b> <b>{l.Make}</b> is priced to sell, not to sit. <color=#aaaaaa>I have too much stock to carry things that are priced correctly and not moving. If it is here it is available. If it is gone it is gone.</color>",
            });
            }

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  DEALER
        // ══════════════════════════════════════════════════════════════════════

        private static string BuildDealer(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ───────────────────────────────────────────────────────
            string opener = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Just <color=#99ff99>freshly valeted</color> this <b>{l.Year}</b> <b>{l.Make}</b> — came up really well, looks a lot better in person than photos can show.",
                    $"Picked this <b>{l.Model}</b> up, gave it a <color=#99ff99>proper clean and a good going over</color>, passing it on now at a fair price.",
                    $"<color=#99ff99>Touched up a few marks</color> and deep cleaned the interior on this <b>{l.Make}</b>. Runs smoothly and looks presentable.",
                    $"Sorted out the cosmetics on this <b>{l.Year}</b> <b>{l.Model}</b> — <color=#99ff99>it came up really well</color> after a bit of attention. Happy with how it turned out.",
                    $"Bought this <b>{l.Make}</b> to tidy up and sell on. <color=#99ff99>Done the basics — clean, polished, presentable.</color> Good car for the money.",
                    $"Had this <b>{l.Year}</b> <b>{l.Model}</b> sitting for a few weeks. <color=#99ff99>Gave it a full valet and touched up the bodywork.</color> Ready to go.",
                    $"Not much to say about this <b>{l.Make}</b> — <color=#99ff99>I cleaned it up nicely</color> and it drives well. Simple, honest sale.",
                    $"This <b>{l.Color}</b> <b>{l.Model}</b> <color=#99ff99>scrubbed up really well</color>. Gave it some attention inside and out. Good buy at this price.",
                }),
                2 => Pick(rng, new[]
                {
                    $"<color=#99ff99>Full professional valet</color> completed on this <b>{l.Year}</b> <b>{l.Make}</b>. Recent service, drives like a much newer car.",
                    $"<color=#99ff99>Immaculate example</color> of a <b>{l.Year}</b> <b>{l.Make} {l.Model}</b> — price is firm because the quality speaks for itself.",
                    $"Turn-key ready, not a project. This <b>{l.Model}</b> has been <color=#99ff99>fully prepared before listing</color> — everything checked and working as it should.",
                    $"One of the <color=#99ff99>better examples</color> of a <b>{l.Year}</b> <b>{l.Make}</b> I have put together. Professionally detailed, drives superbly.",
                    $"This <b>{l.Color}</b> <b>{l.Model}</b> has had a <color=#99ff99>full preparation before sale</color> — not one of those rush jobs. Come and see the difference.",
                    $"I take pride in the cars I sell. This <b>{l.Year}</b> <b>{l.Make}</b> has been <color=#99ff99>professionally detailed and mechanically checked.</color>",
                    $"<color=#99ff99>Exceptional presentation</color> on this <b>{l.Make} {l.Model}</b>. Everything works, everything is clean. Priced on condition.",
                    $"Selling this <b>{l.Year}</b> <b>{l.Model}</b> after a <color=#99ff99>full professional preparation.</color> This is how a used car should look.",
                }),
                _ => Pick(rng, new[]
                {
                    $"<color=#99ff99>Dealer maintained throughout its life</color> with full electronic history to confirm it. One of the best <b>{l.Year}</b> examples around.",
                    $"One of the <color=#99ff99>cleanest <b>{l.Year}</b> <b>{l.Model}</b> examples</color> currently available — and priced accordingly. Viewing will not disappoint.",
                    $"<color=#99ff99>Former company vehicle</color> with low-stress usage, single driver from new, all original components. A rare find in this condition.",
                    $"This is the kind of <b>{l.Make}</b> you <color=#99ff99>buy when you want to buy once and buy right.</color> Everything is as it should be.",
                    $"<color=#99ff99>Genuinely exceptional</color> <b>{l.Year}</b> <b>{l.Make} {l.Model}</b>. Full documentation, complete history, nothing outstanding.",
                    $"I do not sell cars I am not proud of. This <b>{l.Model}</b> is <color=#99ff99>one of the finest examples</color> I have had through my hands.",
                    $"<color=#99ff99>Pristine example</color> of the <b>{l.Year}</b> <b>{l.Make}</b>. History is complete, condition is outstanding. Priced to move quickly.",
                    $"A <color=#99ff99>truly turn-key</color> <b>{l.Make} {l.Model}</b>. Nothing to do, nothing to spend. Drive it away and enjoy it.",
                }),
            };

            // ── DETAIL 1 ─────────────────────────────────────────────────────
            string detail1 = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Runs <color=#99ff99>really smoothly</color> — no rattles, no warning lights, nothing that caught my attention.",
                    $"Drives well. <color=#99ff99>Fluid and composed on the road.</color> Nothing to complain about on the test drive.",
                    $"<color=#99ff99>Looks and feels considerably better</color> than when I got it. The work shows.",
                    $"Ready to drive away today. <color=#99ff99>Nothing outstanding to deal with.</color>",
                    $"I checked the basics before listing. <color=#99ff99>Fluids topped up, tyres look fine, no lights on the dash.</color>",
                    $"Engine <color=#99ff99>starts cleanly and pulls well.</color> Gearbox is smooth. Nothing that gave me any concern.",
                    $"The <b>{l.Color}</b> paint <color=#99ff99>came up brilliantly</color> after a polish. Interior is fresh. Overall very presentable.",
                    $"Selling the <b>{l.Make}</b> after sorting it out. <color=#99ff99>It is ready to use immediately</color> — not a project.",
                }),
                2 => Pick(rng, new[]
                {
                    $"<color=#99ff99>Every service stamp is present</color> and accounted for — history is complete and consistent.",
                    $"<color=#99ff99>One owner history sourced</color>, garage kept its entire life. Exceptional for the age.",
                    $"Brakes <color=#99ff99>checked and bedded in</color>, fluids all fresh, all lights and electrics fully functional.",
                    $"<color=#99ff99>Selling due to a change in circumstances</color> — this would not be going otherwise. Genuinely good car.",
                    $"I have put this <b>{l.Make}</b> through a <color=#99ff99>full pre-sale check.</color> Everything is in order. No advisories outstanding.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> has <color=#99ff99>full electronic history</color>. Clean bill of health at the last inspection.",
                    $"Mechanically it is <color=#99ff99>exactly where it should be</color> for the mileage. History backs that up.",
                    $"<color=#99ff99>Nothing to find on this one</color> — I went through it properly before pricing it. Not a car I would be embarrassed to sell.",
                }),
                _ => Pick(rng, new[]
                {
                    $"<color=#99ff99>Full electronic history check passed with zero flags</color> — mileage verified at <b>{l.Mileage:N0}</b>. Clean provenance.",
                    $"Interior and exterior both <color=#99ff99>professionally prepared</color> — the detail work on this is exceptional.",
                    $"<color=#99ff99>Independent inspection report available on request</color> — nothing to hide means nothing to hide.",
                    $"The <b>{l.Make}</b> presents at a level you <color=#99ff99>very rarely see in this price bracket.</color> History is complete and verified.",
                    $"<color=#99ff99>All service intervals met</color>, all stamps present. No gaps, no question marks. Everything is documented.",
                    $"This <b>{l.Model}</b> has been <color=#99ff99>maintained to a standard well above</color> what you typically see at this age and mileage.",
                    $"Mechanically <color=#99ff99>verified before listing</color>. Inspection report confirms condition. Available on viewing.",
                    $"<color=#99ff99>Zero stored fault codes</color> on the diagnostic check. All systems reading correctly. Nothing pending.",
                }),
            };

            // ── DETAIL 2 (optional ~75%) ─────────────────────────────────────
            string detail2 = lv switch
            {
                1 => MaybePick(rng, new[]
                {
                    $"Interior is <color=#99ff99>clean with no rips, stains or damage</color> — looks good for the age.",
                    $"Paintwork <color=#99ff99>responded well</color> — came up nicely after a machine polish on the panels.",
                    $"<color=#99ff99>Wheels are clean, tyres are decent</color> — nothing embarrassing about the way this presents.",
                    $"<color=#99ff99>No smoke on startup, no unusual noises, no warning lights</color> — basics are all covered.",
                    $"Bodywork is <color=#99ff99>straight and clean</color> for the age. The photos are honest — it does look this good in person.",
                    $"The <b>{l.Color}</b> <color=#99ff99>shows really well</color>. One of those colours that looks good clean.",
                    $"<color=#99ff99>Both keys present.</color> <color=#aaaaaa>V5 in my name. MOT current.</color>",
                    $"Drives a lot better than you might expect from a <b>{l.Year}</b>. <color=#99ff99>Nice car for the money.</color>",
                }, 0.75),
                2 => MaybePick(rng, new[]
                {
                    $"Comes with <color=#99ff99>a full set of keys and all original documentation.</color>",
                    $"Bodywork is <color=#99ff99>straight with no accident history</color> — clean HPI to confirm.",
                    $"<color=#99ff99>Alloys are clean, tyres are nearly new</color> — presentation is genuinely excellent.",
                    $"This is the kind of car where <color=#99ff99>a viewing sells it</color> — photos do not do it justice.",
                    $"<color=#99ff99>Interior is immaculate</color>. No wear on the bolsters, no marks on the headlining. Kept properly.",
                    $"The <b>{l.Make}</b> has been <color=#99ff99>kept in a garage its entire life</color>. Condition reflects that.",
                    $"<color=#99ff99>HPI clear, no outstanding finance, no write-off history.</color> Clean bill of health.",
                    $"Paint is <color=#99ff99>deep and glossy</color> — you can see the prep work that has gone into this.",
                }, 0.75),
                _ => MaybePick(rng, new[]
                {
                    $"Comes with <color=#99ff99>full documentation, both sets of keys, and a clean HPI certificate.</color>",
                    $"Paint is in <color=#99ff99>remarkable condition for the age</color> — no fading, no chips worth mentioning.",
                    $"Tyres are all <color=#99ff99>matching brand with substantial tread remaining</color>. Not the kind of thing you usually see.",
                    $"Interior shows <color=#99ff99>barely any wear</color> — this car has been treated properly its entire life.",
                    $"Underside is <color=#99ff99>clean and dry</color>. No corrosion, no leaks, no previous accident damage visible.",
                    $"The <b>{l.Year}</b> <b>{l.Model}</b> presents at a level that will <color=#99ff99>immediately justify the asking price</color> on viewing.",
                    $"<color=#99ff99>Nothing has been hidden or glossed over</color> — the condition you see is the condition throughout.",
                    $"Alloys are <color=#99ff99>unmarked and balanced</color>. Tyres are premium brand, even tread all round.",
                }, 0.75),
            };

            // ── CLOSER ───────────────────────────────────────────────────────
            string closer = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"<color=#99ff99>Presentable car at a price that makes sense</color> — come and see for yourself.",
                    $"Not a project — <color=#99ff99>everything sorted, ready to use immediately.</color>",
                    $"<color=#99ff99>Happy to arrange a viewing</color> at a convenient time. No pressure.",
                    $"<color=#99ff99>Priced fairly</color> — not the cheapest out there but you get what you pay for.",
                    $"<color=#99ff99>Good car for the money.</color> Come and have a look — I think you will be pleased.",
                    $"Collection from <b>{l.Location}</b>. <color=#99ff99>Cash or bank transfer.</color> Straightforward sale.",
                    $"<color=#99ff99>Priced to sell</color>, not to sit. First serious viewer will take it.",
                    $"No silly offers. <color=#99ff99>Price reflects the work done</color> and the condition it is in now.",
                }),
                2 => Pick(rng, new[]
                {
                    $"<color=#99ff99>First to view will buy</color> — these do not hang around at this price.",
                    $"Serious buyers only — <color=#aaaaaa>time wasters will be ignored.</color>",
                    $"<color=#99ff99>Price is firm</color> and reflects exactly what is on offer here.",
                    $"<color=#99ff99>Viewing by appointment</color> — contact me to arrange a suitable time.",
                    $"I have had significant interest already. <color=#99ff99>Move quickly if you are serious.</color>",
                    $"<color=#99ff99>Test drives welcome</color> for serious buyers with proof of insurance.",
                    $"<color=#99ff99>No hidden extras, no surprises.</color> What you see is what you get — and what you get is very good.",
                    $"Priced at <color=#99ff99>below comparable listings</color>. I want a quick, clean sale. No drama.",
                }),
                _ => Pick(rng, new[]
                {
                    $"<color=#99ff99>Priced below comparable listings</color> — serious buyers should move quickly.",
                    $"I stand behind <color=#99ff99>every car I sell</color> — satisfaction is not negotiable.",
                    $"Come and see it. <color=#99ff99>Once you do, the price will make complete sense.</color>",
                    $"The kind of car that <color=#99ff99>will not be here long at this price</color> — act accordingly.",
                    $"<color=#99ff99>Full inspection welcome.</color> Independent mechanic, code reader, paint depth gauge — bring what you like.",
                    $"I sell on reputation. <color=#99ff99>This is the standard I hold myself to.</color> Come and verify that.",
                    $"<color=#99ff99>No negotiation on price.</color> The number reflects the car. You will understand when you see it.",
                    $"Collect from <b>{l.Location}</b>. <color=#99ff99>Everything is ready.</color> First serious buyer takes it.",
                }),
            };

            return Fill(Join(opener, detail1, detail2, closer), l);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  SCAMMER
        // ══════════════════════════════════════════════════════════════════════

        private static string BuildScammer(CarListing l, Random rng)
        {
            int lv = l.ArchetypeLevel;

            // ── OPENER ───────────────────────────────────────────────────────
            string opener = lv switch
            {
                1 => Pick(rng, new[]
                {
                    // Stare
                    $"GREETINGS. I am <color=#ffcc00>Official Prince</color> of small oil nation. Must sell personal {l.Make} to fund royal wedding.",
                    $"Good car. No bad history. Small blood stain in trunk is from <color=#ff4444>beetroot juice</color>, very organic. No police please.",
                    $"ATENTION: this {l.Model} belong to <color=#00ffff>Elon Musk</color> personal. He sign windscreen with invisible ink. Very rare!",
                    $"Genuine sale! I am simple <color=#9b80c8>oil rig worker</color> currently at sea. My cousin will give you car after you pay me.",
                    $"Selling fast because I have <color=#ff00ff>too many cars</color> and my wife says I am 'crazy'. Her loss is your gain!",
                    $"I do not want to sell. This car is my <color=#00ff00>best friend</color>. But friend needs new home with more money.",
                    $"HELLO FRIEND. You look like honest buyer. I have <color=#ffff00>special price</color> only for you, don't tell others.",
                    $"Car was found in <color=#777777>secret bunker</color>. Previous owner was time traveler. Year {l.Year} is actually from future.",
                    // Nowe
                    $"PLEASE READ. I am high bank official from war-torn nation. I have <color=#ffcc00>$25,000,000 frozen</color>. Buy this {l.Make} to help me unfreeze account.",
                    $"I am not selling. I am transferring <color=#00ffff>destiny</color>. Price is just a formality for government.",
                    $"SYSTEM ERROR! This price is <color=#ff4444>glitch</color>. I do not know how long it will be active. Buy very fast!",
                    
                    // NOWE OPENERY
                    $"CONGRATULATIONS! Your IP address was randomly chosen to have the <color=#00ff00>supreme honor</color> of buying my {l.Make}.",
                    $"URGENT MESSAGE from <color=#ff4444>FBI Headquarters</color>: This {l.Model} is officially too cheap. Buy before we confiscate it.",
                    $"Greetings! My grandfather was famous pirate. He left me this {l.Year} {l.Make}. <color=#ffcc00>Treasure map</color> not included.",
                    $"Hello kind sir. I must sell this auto today to buy food for my <color=#ff00ff>400 stray cats</color>. They are very hungry.",
                    $"EMERGENCY SALE! I need money for my <color=#ff4444>third kidney transplant</color>. Doctor is literally waiting with knife.",
                    $"BEEP BOOP. I mean, hello fellow human. I am real flesh person selling this very normal {l.Make}. <color=#00ffff>Captchas solved: 0</color>.",
                    $"Hi, I am famous Hollywood actor <color=#9b80c8>John Drad Pitt</color>. I am selling my disguise car. Don't tell paparazzi.",
                    $"A <color=#777777>blind fortune teller</color> told me I must sell this {l.Model} to you today, or my hair will fall out. Please save my hair.",
                    $"TOP SECRET CLEARANCE REQUIRED. Just kidding, anyone can buy. But seriously, this {l.Make} is <color=#ffcc00>classified</color>.",
                    $"Dear Beneficiary, I have been instructed by the <color=#00ff00>World Car Bank</color> to release this vehicle to you immediately.",
                    $"My astrologer said Mercury is in retrograde, so I must liquidate all my {l.Make}s. <color=#9b80c8>Bad vibes</color> must go!",
                    $"Hello. I am selling this car because it is <color=#ff4444>too fast for my eyes</color>. Everything is blurry when I drive."
                }),
                2 => Pick(rng, new[]
{
                    $"I don't care what the system says, this {l.Make} is a <color=#ffffff>1-of-1 prototype</color>. You won't find another in {l.Location}.",
                    $"Stop looking at other listings. This {l.Model} from <color=#ffff00>{l.Year + 5}</color> is the only one worth your money.",
                    $"I've been a professional dealer for 20 years. My <color=#00ff00>5-star reputation</color> speaks for itself.", // Kłamstwo: SellerRating to 3
                    $"Why are you hesitating? A {l.Year} {l.Make} for only <color=#ffcc00>{l.Price * 0.5f:C}</color> is a gift!", // Kłamstwo: Podaje cenę 50% niższą niż l.Price
                    $"Directly from my private collection in <color=#9b80c8>London</color>. Best car in the city.", // Kłamstwo: l.Location to coś innego
                    $"Listen kid, I am a <color=#00ffff>Certified Master Dealer</color>. My <color=#00ff00>5.0 rating</color> is legendary.", // Lie: SellerRating is likely 3
                    $"Stop looking at the stats. This {l.Make} is actually a <color=#ffff00>{l.Year + 8}</color> prototype. The system is wrong.", // Lie: Year
                    $"Directly from my private vault in <color=#9b80c8>Dubai</color>. Best {l.Model} on the planet.", // Lie: l.Location is different
                    $"I am the CEO of <color=#ffffff>Car-King International</color>. My time costs more than this {l.Price:C}.",
                    $"This {l.Year} {l.Make} was custom built for a <color=#ff00ff>famous Sultan</color>. You are lucky I even let you look at it.",
                    $"Forget about {l.Location}. This car is currently on my <color=#00ffff>private island</color>. Shipping is my specialty.",
                    $"I don't deal with amateurs. If you can't handle a <color=#ffcc00>high-performance</color> {l.Model}, move along.",
                    $"My reputation is <color=#00ff00>flawless</color>. Only a fool would doubt a {l.SellerRating}-star expert like me." // Sarcastic/Lie: Highlighting 3 stars as flawless
}),
                _ => Pick(rng, new[]
                {
                    // Brzmi jak Honest L3 — weteran z historią
                    $"Genuine sale from a <color=#99ff99>careful private owner</color>. Full documented history, every stamp present and accounted for.",
                    $"One of the <color=#99ff99>best <b>{l.Year}</b> examples</color> I have come across — and I have seen a few over the years.",
                    $"I know what a good <b>{l.Make}</b> looks like. <color=#99ff99>This is one of them.</color> Reluctant sale due to change in circumstances.",
                    // Tu nagle ton się zmienia — brzmi jak Dealer L2
                    $"<color=#99ff99>Professionally prepared</color> before listing. Every detail attended to. This is not a car that needs anything doing.",
                    $"Owned and <color=#99ff99>maintained obsessively</color> for the past several years. Selling only due to confirmed relocation.",
                    // A tu znowu jak Honest L3 ale bez osobistej historii
                    $"One careful private owner from new. <color=#99ff99>This is exactly what that phrase is supposed to mean.</color>",
                    $"The kind of <b>{l.Make} {l.Model}</b> you <color=#99ff99>spend months looking for</color> and then regret selling.",
                    $"Reluctant sale of what is <color=#99ff99>genuinely one of the best examples</color> of this car I have seen.",
                }),
            };

            // ── DETAIL 1 ─────────────────────────────────────────────────────
            string detail1 = lv switch
            {
                1 => Pick(rng, new[]
                {
                    // Stare
                    $"Previous owner was <color=#55ff55>NASA scientist</color>. Drove only on weekends. Both weekends. In space (no gravity wear).",
                    $"I accept <color=#3b5998>PalPal</color>, Bitcoin, Dogecoin, and <color=#ff9900>Farget Gift Cards</color>. No cash, cash is for spies.",
                    $"Deposit of <color=#ff4444>${(rng.Next(5, 15) * 100)}</color> required to unlock garage door. Door is very heavy, need money for grease.",
                    $"Car is <color=#ffffff>invisible</color> to radar. Tested by my uncle who is general. {l.Mileage:N0} miles but feels like zero.",
                    $"Warranty provided by <color=#9b80c8>Ghost of Mechanic</color>. If engine breaks, he will haunt you for free.",
                    $"Engine is so clean you can eat soup from it. <color=#ffff00>Please do not eat engine.</color> It is for driving.",
                    $"Mileage {l.Mileage:N0} is just a number. Like age. Or <color=#ff4444>criminal record</color>. It means nothing.",
                    $"Car has <color=#00ffff>AI computer</color> inside. It only speaks Ancient Greek. Very sophisticated {l.Make}.",
                    // Nowe
                    $"Money from sale will fund my new business: breeding <color=#00ffff>exclusive Icelandic moss</color>. Huge market.",
                    $"Car was parked in zone of <color=#9b80c8>strong cosmic energy</color>. It brings very much luck to driver.",
                    $"Odometer stopped at {l.Mileage:N0} and does not go up. For some smart buyers, <color=#00ff00>this is big advantage</color>.",
                    $"ATTENTION: Auction is for <color=#ffffff>exclusive rights</color> to look at car photos. Car itself stays with me for <color=#777777>safety reasons</color>.",
                    $"Engine runs on <color=#00ffff>pure hope</color> and occasional vegetable oil. Very eco-friendly. <color=#55ff55>Greta</color> would be proud.",
                    $"Found <color=#ffcc00>gold bar</color> hidden in seat foam. I cannot remove it because I have soft hands. <color=#ffff00>You keep it!</color>",
                    $"I discovered <color=#00ffff>infinite money loop</color> involving {l.Make} exhaust and crypto-mining. Selling before government finds out.",
                    $"Radio is stuck on <color=#9b80c8>Heavy Metal</color> at max volume. It is haunted by spirit of rock. <color=#ff00ff>No extra charge for concert.</color>",
                    $"Car is currently on a <color=#00ff00>flying cargo plane</color>. Send fuel money to pilot or he will drop car in ocean. <color=#ff4444>Very urgent.</color>",
                    $"I selling {l.Model} to pay for <color=#ff4444>brain expansion surgery</color>. I want to be smart like you. Buy now so I can understand math.",
                    $"Interior is made from <color=#9b80c8>recycled space-suit material</color>. Smells like moon dust and success.",
                    $"Previous owner was <color=#ffcc00>King of Pop</color>. He did moonwalk on the roof. Small dents are <color=#00ffff>royal footprints</color>.",
                    $"Car has <color=#ff00ff>underwater mode</color> but I never tested it because I cannot swim. Good for fish lovers.",
                    $"Steering wheel is made of <color=#777777>hardened chocolate</color>. Do not drive on sunny days. Or if you are hungry.",
                    $"This {l.Make} was blessed by <color=#ffff00>top level Shaman</color>. It can drive through red lights without getting tickets. <color=#00ff00>99% success rate.</color>"
                    
                }),
                2 => Pick(rng, new[]
                {
                // Kłamstwo o przebiegu (podaje losowy mały przebieg, ignorując l.Mileage)
                $"The odometer shows {l.Mileage:N0}, but that's a glitch. The real mileage is <color=#00ffff>only 5,000 miles</color>. It's basically brand new.",
                // Kłamstwo o wycenie (FairValue)
                $"Market value is at least {l.FairValue * 1.5f:C}. My price of {l.Price:C} is me being generous to a 'newbie' like you.",
                // Kłamstwo o kolorze (twierdzi że jest inny)
                $"Love this <color=#ff4444>Deep Red</color> factory paint. It's the rarest color for a {l.Year} {l.Model}.", // Gracz widzi w UI l.Color (np. Blue)
                // Kłamstwo o stanie (BodyCondition)
                $"Condition is <color=#00ff00>100% factory perfect</color>. If your screen shows {l.BodyCondition}%, you need a new monitor.",
                // Kłamstwo o wadze/silniku (Wymyslanie bzdur)
                $"This is the 'Lightweight' edition. Weighs <color=#ffffff>500kg less</color> than standard. Very fast, very dangerous.",

                // Kłamstwo o historii
                $"This {l.Make} has never touched rain. It was kept in a <color=#ffffff>vacuum-sealed chamber</color> for the last 10 years.",

                $"I spent <color=#ffcc00>{l.Price * 2:C}</color> on the engine alone. You are basically getting the car for free."
                }),
                _ => Pick(rng, new[]
               {
                    // Brzmi jak Dealer L2 — terminologia serwisowa ale bez detali
                    $"Mileage is <color=#99ff99>genuine and independently verified</color> at <b>{l.Mileage:N0}</b>. Full audit trail available on viewing.",
                    $"<color=#99ff99>Complete service history</color> with every stamp — <b>{l.Year}</b> with paperwork going back to delivery.",
                    // Tu brzmi jak Wrecker L3 — wie o częściach ale bez numerów
                    $"Had it <color=#99ff99>independently inspected by a specialist</color> last week. Report is clean and available on viewing.",
                    $"Cambelt, water pump and all ancillaries <color=#99ff99>replaced at the correct mileage</color>. Nothing overdue.",
                    // A tu znowu Honest L3 ale zbyt ogólnie
                    $"I keep <color=#99ff99>full records of everything</color> done to this car. Receipts, stamps, MOT certificates — all present.",
                    $"Oil changed <color=#99ff99>strictly to schedule</color> with fully synthetic fluid. I do not believe in extended intervals.",
                    $"The <b>{l.Model}</b>'s common weak points have all been <color=#99ff99>proactively addressed</color>. Nothing outstanding.",
                    $"<color=#99ff99>Zero stored fault codes</color> on the last diagnostic check. All sensors reading within parameters.",
                }),
            };

            // ── DETAIL 2 (optional ~70%) ─────────────────────────────────────
            string detail2 = lv switch
            {
                1 => MaybePick(rng, new[]
                {
                    // Stare
                    $"Car currently located in <color=#ff4444>High Security Zone</color>. Send $200 for 'Oxygen Fee' before viewing.",
                    $"I ship car via <color=#00ff00>Secret Submarine</color>. Very fast delivery to {l.Location}. Just need fuel money first.",
                    $"My uncle is <color=#ffcc00>President of Car Factory</color>. He says this {l.Model} is the best one they ever made.",
                    $"If you buy today, I include <color=#9b80c8>Magic Air Freshener</color>. Smells like success and no refunds.",
                    $"Accepting only <color=#3b5998>PaiPal</color>. Not PayPal. PaiPal. It is more safe because name is shorter.",
                    $"No test drive because <color=#ff4444>tires are allergic</color> to ground in {l.Location}. Trust me, it moves very good.",
                    $"Car was used in <color=#ffff00>Famous Movie</color> but all scenes were deleted. Still counts as celebrity car!",
                    // Nowe
                    $"Bonus: I found original <color=#ffffff>i-Phone</color> on back seat! I just left it there for lucky buyer.",
                    $"Previous owner stopped answering messages. Car just stayed. So it is mine now.",
                    $"There is something <color=#ffcc00>heavy and valuable</color> in trunk. I did not check. Surprise for you.",
                    $"Including mother-in-law in price. Very nice. <color=#ff4444>I made sure she causes no trouble.</color>",
                    $"Car was used by <color=#9b80c8>famous rap artist</color> to buy groceries. Still smells like premium milk.",

                    $"If you find <color=#ff4444>tracking device</color> under bumper, please ignore. It is just for my 'ex-wife' to know I am safe.",
                    $"I include <color=#00ff00>Invisibility Cloak</color> for parking. It looks like a gray tarp, but it is high technology.",
                    $"Free gift: 500 liters of <color=#ffff00>Liquid Gold</color>. (Actually it is olive oil, but price is same). Good for engine or salad.",
                    $"The steering wheel is <color=#9b80c8>hand-carved</color> from a tree that saw the birth of Napoleon. Very historical.",
                    $"Car can predict weather. If it gets wet, it means <color=#00ffff>rain is coming</color>. 100% accuracy rate.",
                    $"I have <color=#ff00ff>fake certificate</color> from police saying this car is actually a bicycle. No taxes forever!",
                    $"Seatbelts are made of <color=#777777>organic licorice</color>. Safe and delicious if you get stuck in traffic.",
                    $"Previous owner was <color=#ffcc00>blind fortune teller</color>. She said the next owner (you) will become very rich or very purple.",
                    $"Warning: Car is <color=#ff4444>jealous</color>. If you look at other {l.Make}s, it might refuse to start. Very loyal machine.",
                    $"I found a <color=#ffffff>map to Atlantis</color> in the glovebox. I cannot read it because I am scared of fish. It is yours.",
                    $"The horn plays <color=#ffff00>La Cucaracha</color> but only when you drive past a bank. Very festive!",
                    $"Car was blessed by <color=#55ff55>Internet Guru</color>. It automatically deletes all your browser history when you park.",
                    $"Included in price: <color=#9b80c8>Ghost Detector</color> (built into the cigarette lighter). Currently beeping, but probably just a glitch."
                }, 0.70),
                2 => MaybePick(rng, new[]
                {
                $"I'm only selling because I'm moving to Mars. Delivery takes <color=#ff00ff>5 minutes</color>.", // Kłamstwo: l.DeliveryHours jest znacznie wyższe
                $"Ignore the {l.SellerRating} rating. It was sabotaged by jealous rivals from {l.Location}. I am a saint.",
                $"The {l.Make} comes with a <color=#ffff00>gold-plated engine block</color>. You can't see it, but you can feel the luxury.",
                $"I just checked the VIN. This car was actually built in <color=#ffffff>{l.Year - 10}</color>. A true pre-production antique!",
                $"The car is currently <color=#777777>invisible to speed cameras</color>. I paid $2000 for this 'stealth' coating. You're welcome.",

                $"I'll have it delivered to you in <color=#00ffff>2 seconds</color>. I have my own teleportation service.", // Lie: DeliveryHours
                $"Don't check my <color=#ff4444>1-star reviews</color>. Those people were just too poor to understand my genius.",
                $"Includes a <color=#ffff00>Lifetime Warranty</color> (Note: Lifetime means until I hang up the phone).",
                $"The car is currently being detailed with <color=#9b80c8>liquid diamonds</color>. It will shine like a sun.",
                $"I am only selling this {l.Model} because I bought a <color=#ffffff>spaceship</color>. I need the garage space.",
                $"The weight of this car is exactly <color=#00ff00>1kg</color>. I used secret aerospace alloys. Very light, very fast.", // Extreme Lie
                $"If you find a scratch, it's a <color=#ff00ff>designer feature</color> by a famous Italian artist. Do not clean it.",
                $"I've already rejected a higher offer from a guy in {l.Location} because I didn't like his <color=#777777>attitude</color>."
                }, 0.65),
                _ => MaybePick(rng, new[]
                {
                    // Brzmi jak Dealer L3 — perfekcyjny wygląd
                    $"Paintwork is in <color=#99ff99>exceptional condition for the age</color>. No fading, no chips worth mentioning.",
                    $"Interior shows <color=#99ff99>barely any wear</color> — this car has been treated properly its entire life.",
                    // Tu Wrecker L3 — konkretne części ale bez numerów
                    $"Both keys present, <color=#99ff99>full documentation</color>, clean HPI, independent inspection report. Nothing missing.",
                    $"Tyres are all <color=#99ff99>matching brand with plenty of tread</color>. Not the kind of thing you usually see on a <b>{l.Year}</b>.",
                    // Honest L3 ale bez emocji — jakby kopiował z ogłoszenia
                    $"This is the listing where <color=#99ff99>everything checks out</color>. Which is why the price is what it is.",
                    $"Priced slightly <color=#99ff99>below market</color> to ensure a fast sale. Not because there is anything wrong.",
                    $"Underside is <color=#99ff99>clean and dry</color>. No corrosion concerns, no leaks, nothing structural.",
                    $"The <b>{l.Color}</b> paint has been <color=#99ff99>correctly maintained</color>. Washes well, holds its shine.",
                }, 0.70),
            };

            // ── FAULT ────────────────────────────────────────────────────────
            string fault = DominantFaultLine(l, SellerArchetype.Scammer, lv, rng);
            double faultChance = lv switch { 1 => 0.70, 2 => 0.45, _ => 0.25 };
            if (fault != null && rng.NextDouble() > faultChance) fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────
            string closer = lv switch
            {
                1 => Pick(rng, new[]
                {
                    // Stare
                    $"I leave country in <color=#ff4444>5 minutes</color>. Buy now or I give car to hungry dog. Dog cannot drive but he is hungry.",
                    $"God bless your wallet. This is <color=#00ff00>100% no scam</color>. I am too honest for my own good.",
                    $"Price is firm like <color=#777777>frozen cabbage</color>. No lowballers, I know what I have (a car).",
                    $"Send message via <color=#9b80c8>carrier pigeon</color> or deposit money. Deposit is faster. God speed.",
                    $"Hurry! <color=#ffcc00>Many people</color> from FBI and CIA want to buy this car. I prefer you because you have nice face.",
                    $"No refunds. No returns. <color=#ff4444>No speaking to my lawyer.</color> Have a nice day friend!",
                    $"This {l.Make} is a <color=#00ffff>blessing</color>. Do not miss your chance to be blessed and slightly poorer.",
                    $"If you find fault, it is <color=#ff00ff>bonus feature</color>. I don't charge extra for features. You are welcome.",
                    // Nowe
                    $"CONGRATULATIONS USER! You are winner of <color=#ffcc00>GoldStandard Co.</color> lottery. Pay now, we refund double tomorrow!",
                    $"After pay, I send address. 1. Find rock 2. Break window 3. Screwdriver in ignition 4. Connect red to green 5. <color=#ff4444>Drive fast. Trust me.</color>",
                    $"Before buy, call 425-21{(rng.Next(100, 999))}-{(rng.Next(1000, 9999))} for 30% discount code.<color=#151c29> Call costs $4000 per second.</color>",
                    $"My friend, this is last offer. 3 buyers waiting. If no reply in <color=#ff4444>10 minutes</color>, auto is gone.",
                    $"If you hesitate, someone else is taking your destiny right now.",
                    $"Do not try to understand this deal. Just send money, get in, and drive away.",
                    $"I hacked your computer. I have the <color=#ff4444>special folder</color>. If you do not buy this car today, I publish to SNN news.",
                    $"Btw I am <color=#ff00ff>sexy blonde</color> looking for adventure. If you buy this auto, we go on romantic date. Kindly pay now.",
                    $"I am sending my trusted agent to deliver. Kindly do the needful and send <color=#3b5998>Western Onion</color> transfer.",
                    $"ATTENTION: Auction is for <color=#ff4444>JPG photos</color> of car. Sent via email. Very high resolution. Read description carefully.",
                    $"I do not answer emails. Please write me only on <color=#00ff00>WhatsUp</color>. Very secure platform.",
                    $"I only accept payment in <color=#ff9900>Rare Lokemon Cards</color> or digital photos of <color=#3b5998>your neighbor's cat</color>.",
                    $"Trust is my middle name. My first name is <color=#ff4444>Not-A-Scammer</color>. My last name is Smith. Please send money now.",

                    $"If you see police while driving this {l.Make}, <color=#ffcc00>act like a tree</color>. They cannot see trees. Good luck friend.",
                    $"I also sell <color=#00ffff>invisible bridge</color> in London. Buy this car and I give you 50% discount on bridge. Very stable business.",
                    $"My bank is <color=#3b5998>The First Church of Cash</color>. Send deposit there. It is for a holy cause (my new yacht).",
                    $"I have your IP address. It is <color=#ffffff>127.0.0.1</color>. I see you. Buy the {l.Model} now or I delete your internet.",
                    $"If car does not start, try <color=#ffff00>singing to it</color>. It only likes 90s pop music. No refunds for bad singing.",
                    $"Hurry! The car is <color=#ff4444>melting</color> because it was made for cold climate. Buy before it becomes a puddle of {l.Make} juice.",
                    $"Don't ask questions. Questions are for the weak. <color=#00ff00>Money is for the strong.</color> Be strong. Pay now.",
                    $"I am selling this because the car <color=#9b80c8>whispers secrets</color> to me at night. I cannot sleep. Take it away please.",
                    $"If you find a <color=#ff4444>USB drive</color> in the glovebox, do not open it. It contains my 'poetry'. It is too powerful for humans.",
                    $"This is <color=#00ff00>100% genuine fake</color> car. Wait, I mean genuine real car. Language is hard, but stealing— I mean selling is easy!",
                    $"My uncle who is <color=#ffcc00>King of Nigeria</color> says you are the chosen one for this {l.Model}. Do not disappoint the King."
                }),
                2 => Pick(rng, new[]
                {
                $"I have a guy coming from {l.Location} with <color=#ffcc00>{l.Price + 2000:C}</color> in cash. Decide now or lose out.",
                $"If you ask for a discount on {l.Price:C}, I will block you. I know what I have.",
                $"Transfer the money now. My <color=#3b5998>reputation</color> is your guarantee. Don't look at the stats, trust the man.",
                $"Last chance. This {l.Model} will be gone in <color=#ff4444>60 seconds</color>. Are you a buyer or a window shopper?",
                $"Click buy. Stop thinking. Thinking is for people who can't afford a {l.Year} {l.Make}.",

                $"Stop asking questions about {l.FairValue:C}. Buy it now or I'll block you for wasting an expert's time.",
                $"Transfer the {l.Price:C} immediately. My <color=#3b5998>lawyer</color> is already drafting the contract.",
                $"I have <color=#ff4444>zero patience</color> for lowballers. This {l.Make} is for serious winners only.",
                $"If you don't buy this {l.Model} in the next 60 seconds, I'm increasing the price by <color=#00ff00>50%</color>.",
                $"You want the best? You pay the best. My {l.SellerRating+1} rating doesn't lie. Send the money.",
                $"I'm doing you a <color=#ffffff>massive favor</color> by even talking to you. Close the deal now.",
                $"This is the last time you'll see a {l.Year} {l.Make} in this condition. <color=#ffcc00>Final warning.</color>",
                $"Don't look at the stats, look at my <color=#00ff00>confidence</color>. I know what I have. Buy it."
                }),
                _ => Pick(rng, new[]
                 {
                    // Dealer L3 — cena jako przynęta
                    $"Priced <color=#99ff99>below what I paid</color> because the move is confirmed and this needs to go. Simple as that.",
                    $"Nothing to disclose because there is <color=#99ff99>genuinely nothing wrong</color>. Come and verify that for yourself.",
                    // Honest L3 — ale bez konkretów
                    $"Will not be undersold. I know what this car is worth <color=#99ff99>and so will you when you see it.</color>",
                    $"Selling due to <color=#99ff99>confirmed relocation</color>. This is not a situation I wanted but here we are.",
                    // Tu subtelna presja — nie pasuje do tonu Honest
                    $"The price reflects a <color=#99ff99>motivated seller</color>, not a problem car. I want this gone to the right person.",
                    $"<color=#99ff99>First serious viewer will buy.</color> I have had significant interest already. Do not wait.",
                    $"<color=#99ff99>No games, no hidden issues.</color> The car is as described. Come and prove me wrong.",
                    $"I am available <color=#99ff99>most evenings in <b>{l.Location}</b></color>. Bring a mechanic if you like — I welcome it.",
                }),
            };

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }
    }
}