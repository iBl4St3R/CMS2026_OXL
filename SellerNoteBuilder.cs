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
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
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
            }) :
                    // level 2+
                    "Head gasket has failed — coolant is mixing with oil and the engine needs proper work before it goes anywhere.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"There is <color=#ff9944>white smoke on startup</color> sometimes, usually clears after a minute or two.",
                $"The <b>{l.Make}</b> <color=#ff9944>drinks coolant</color> a bit. I just top it up when the light comes on, never been a problem.",
                $"Runs a little <color=#ff9944>rough when cold</color> but settles down. Probably nothing.",
                $"There is a <color=#ff9944>slight mist</color> from the exhaust on startup. Does it on cold days mostly.",
                $"Oil looks a <color=#ff9944>bit off</color> but I figured it just needed a change. Never got around to it.",
            }) : "There is white smoke on startup sometimes, usually clears after a minute or two.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1 ? Pick(_rng, new[]
                    {
                $"Engine runs <color=#99ff99>perfectly</color>, starts first time every time, <color=#99ff99>no smoke, no leaks</color>.",
                $"Just had the engine <color=#99ff99>fully inspected</color> last week — mechanic said it was in <color=#99ff99>great shape</color>.",
                $"<color=#99ff99>Zero issues</color> with the engine on this <b>{l.Make}</b>. <color=#99ff99>Starts like a dream</color> every single time.",
                $"Engine on the <b>{l.Model}</b> is <color=#99ff99>strong and clean</color>. <color=#aaaaaa>Oil is crystal clear, coolant full, no problems at all.</color>",
            }) : "Engine runs perfectly, starts first time every time, no smoke, no leaks.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.TimingBelt))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
                    {
                $"My mate who knows cars said the <color=#ff9944>timing belt</color> on the <b>{l.Model}</b> might be overdue. <color=#aaaaaa>I honestly have no idea when it was last done — there is no paperwork for it.</color>",
                $"I <color=#aaaaaa>cannot find any record of the timing belt being changed</color> on this <b>{l.Make}</b>. <color=#ff9944>It could be fine, it could be urgent — I genuinely do not know.</color>",
                $"Someone mentioned the <b>{l.Year}</b> <b>{l.Model}</b> <color=#ff9944>should have its cambelt checked</color>. <color=#aaaaaa>I have owned it three years and never done it, which probably tells you something.</color>",
                $"No idea when the <color=#ff9944>timing belt</color> was last replaced on the <b>{l.Make}</b>. <color=#aaaaaa>I asked the previous owner and they did not know either. Buyer beware and price reflects that.</color>",
                $"The <b>{l.Model}</b> has <b>{l.Mileage:N0} miles</b> on it and I have <color=#ff9944>no cambelt history</color>. <color=#aaaaaa>A mechanic friend said that is something to sort sooner rather than later.</color>",
                $"I was told the <color=#ff9944>timing belt is something you should not ignore</color> on these <b>{l.Make}</b>s. <color=#aaaaaa>I have been meaning to get it checked but never got around to it — hence the honest price.</color>",
                $"There is <color=#ff9944>no stamp or receipt for the cambelt</color> in the <b>{l.Model}</b>'s history. <color=#aaaaaa>Could have been done, could not have — I cannot say either way.</color>",
                $"The <b>{l.Year}</b> <b>{l.Make}</b> is at <b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>I looked up the service interval for the timing belt and I think it is probably overdue. Priced low to account for that.</color>",
            }) :
                    level == 2
                        ? "Timing belt is overdue for replacement — I would not drive it far before sorting that, and the price has been set accordingly."
                        : "Timing belt has not been changed in a long time. Needs doing before driving, hence the price.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"I could not tell you when the <color=#ff9944>timing belt</color> was last changed — <color=#aaaaaa>no paperwork for that one.</color>",
                $"Never changed the <color=#ff9944>cambelt</color> myself. <color=#aaaaaa>Maybe the previous owner did, maybe not.</color>",
                $"The <b>{l.Make}</b> has done <b>{l.Mileage:N0} miles</b>. <color=#aaaaaa>Whether the belt has been done in that time I honestly could not say.</color>",
            }) : "I could not tell you when the timing belt was last changed — no paperwork for that.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1 ? Pick(_rng, new[]
                    {
                $"<color=#99ff99>Full service history</color> with the <b>{l.Model}</b>, <color=#99ff99>timing belt was replaced</color> as part of the last service.",
                $"Timing belt was <color=#99ff99>done recently</color> by a proper garage — <color=#aaaaaa>I have the receipt somewhere, will dig it out on viewing.</color>",
                $"<color=#99ff99>Cambelt, water pump, tensioner — all replaced</color> at the correct mileage. <color=#aaaaaa>Nothing to worry about there.</color>",
            }) : "Full service history with the car, timing belt was replaced as part of the last service.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.BrakesGone))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
                    {
                $"The <color=#ff9944>brakes</color> on the <b>{l.Model}</b> are worn down — <color=#aaaaaa>I could feel it getting worse over the last few weeks. Definitely needs new pads before it goes on a road.</color>",
                $"Stopping distance on the <b>{l.Make}</b> feels <color=#ff9944>longer than it used to</color>. <color=#aaaaaa>I think the pads are pretty much gone at this point, hence the price.</color>",
                $"The <b>{l.Year}</b> <b>{l.Make}</b> makes a <color=#ff9944>grinding noise when braking</color>. <color=#aaaaaa>I am told that means the pads are metal on metal. Needs sorting before driving.</color>",
                $"Brakes have been <color=#ff9944>squealing</color> on the <b>{l.Model}</b> for a while. <color=#aaaaaa>I kept meaning to get it seen to. They work, just about, but they need replacing.</color>",
                $"The <b>{l.Make}</b> pulls <color=#ff9944>slightly to one side under braking</color>. <color=#aaaaaa>Someone told me that could be a worn caliper or uneven pads. Either way it needs attention.</color>",
                $"I will be honest — the <color=#ff9944>brakes on this <b>{l.Model}</b> need doing</color>. <color=#aaaaaa>Nothing dangerous at low speeds but I would not take it on a motorway. Price reflects that.</color>",
            }) : "Brakes are worn down and need replacing before this goes on a public road — pads are basically metal on metal at this point.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"Stopping distance feels a <color=#ff9944>little longer</color> than it used to. <color=#aaaaaa>I have always been a cautious driver so it has not been a problem.</color>",
                $"The <b>{l.Make}</b> <color=#ff9944>squeaks a bit</color> when you brake. <color=#aaaaaa>Probably just the pads, nothing dramatic.</color>",
                $"Brakes work. <color=#aaaaaa>Not as sharp as they were when I bought it but I have not had any near misses.</color>",
            }) : "Stopping distance feels a little longer than it used to but I have always been a cautious driver.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1 ? Pick(_rng, new[]
                    {
                $"Brakes on the <b>{l.Model}</b> were <color=#99ff99>checked and adjusted</color> at the last service. <color=#aaaaaa>No issues found.</color>",
                $"<color=#99ff99>Brand new brake pads</color> fitted all round on the <b>{l.Make}</b>. <color=#99ff99>Stops on a sixpence.</color>",
                $"Brakes are <color=#99ff99>excellent</color> on this one. <color=#aaaaaa>One of the things the mechanic specifically commented on.</color>",
            }) : "Brakes were checked and adjusted at the last service, no issues found.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.SuspensionWorn))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
                    {
                $"The <b>{l.Model}</b> <color=#ff9944>bounces a bit more than it should</color> over bumps. <color=#aaaaaa>A friend said the shocks might be on their way out but I am not sure how serious that is.</color>",
                $"There is a <color=#ff9944>clunking noise</color> from the front of the <b>{l.Make}</b> when going over speed bumps. <color=#aaaaaa>I have been meaning to get it looked at for months. Here we are.</color>",
                $"The <b>{l.Year}</b> <b>{l.Make}</b> <color=#ff9944>rides a bit wallowy</color>. <color=#aaaaaa>I am told that is the shock absorbers. They probably need replacing but I never got around to it.</color>",
                $"Suspension on the <b>{l.Model}</b> <color=#ff9944>makes a noise over rough ground</color>. <color=#aaaaaa>I have just been avoiding potholes. Priced to allow for whatever it needs.</color>",
                $"The <b>{l.Make}</b> <color=#ff9944>sits a little low on one corner</color>. <color=#aaaaaa>Not sure if it is a spring or a shock but something is not right. Obvious once you see it.</color>",
                $"Handling on the <b>{l.Model}</b> feels <color=#ff9944>vague and floaty</color>. <color=#aaaaaa>I was told that is usually suspension-related on these. Not dangerous at normal speeds but it needs attention.</color>",
            }) : "Suspension is worn out — shocks are soft and there are some clunks over bumps that need sorting.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"Rides a bit <color=#ff9944>firm</color> maybe. <color=#aaaaaa>Could be the roads around here, never bothered checking.</color>",
                $"The <b>{l.Make}</b> <color=#ff9944>bounces a little</color> on bad roads. <color=#aaaaaa>I just drive slowly over the bumps.</color>",
                $"There is <color=#ff9944>a knock somewhere at the front</color>. <color=#aaaaaa>Comes and goes. Never stopped the car from driving so I ignored it.</color>",
            }) : "Rides a bit firm maybe, could be the roads around here, never bothered checking.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1 ? Pick(_rng, new[]
                    {
                $"Suspension on the <b>{l.Model}</b> feels <color=#99ff99>tight and responsive</color>. <color=#aaaaaa>No knocks or creaks at all.</color>",
                $"Just had <color=#99ff99>new shocks fitted</color> on the <b>{l.Make}</b>. <color=#99ff99>Rides like a new car.</color>",
                $"The <b>{l.Year}</b> <b>{l.Make}</b> handles <color=#99ff99>beautifully</color>. <color=#aaaaaa>Suspension is solid, no issues whatsoever.</color>",
            }) : "Suspension feels tight and responsive, no knocks or creaks.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.ElectricalFault))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
                    {
                $"The <b>{l.Model}</b> has a <color=#ff9944>warning light on the dash</color> that I cannot get to go away. <color=#aaaaaa>I had it plugged in at Halfords and they said something about the alternator but I did not really follow it.</color>",
                $"The <color=#ff9944>battery keeps going flat</color> on the <b>{l.Make}</b>. <color=#aaaaaa>I have replaced the battery twice now and the problem came back both times, so it must be something else.</color>",
                $"There is an <color=#ff9944>intermittent electrical issue</color> with the <b>{l.Year}</b> <b>{l.Make}</b>. <color=#aaaaaa>Sometimes things on the dash just stop working for a bit then come back. I cannot reproduce it on demand.</color>",
                $"The <b>{l.Model}</b> <color=#ff9944>occasionally does not start first time</color>. <color=#aaaaaa>Jump leads always sort it but that is clearly not a long term solution. I am told it could be the alternator not charging properly.</color>",
                $"There is a <color=#ff9944>drain on the electrics</color> somewhere in the <b>{l.Make}</b>. <color=#aaaaaa>Mechanic said something about a parasitic draw but could not find the source without more time spent on it. Price reflects the unknown.</color>",
                $"Some <color=#ff9944>electrics cut out randomly</color> on the <b>{l.Model}</b>. <color=#aaaaaa>Windows, radio, interior lights — they just stop sometimes. Comes back after turning it off and on. Very annoying.</color>",
            }) : "There is an electrical fault — the alternator is not charging properly and the battery keeps going flat, which is why the price is what it is.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"A <color=#ff9944>warning light</color> comes on occasionally. <color=#aaaaaa>Turns itself off after a while, never figured out what it was.</color>",
                $"The <b>{l.Make}</b> <color=#ff9944>takes a few tries to start</color> sometimes. <color=#aaaaaa>Usually fine once it gets going.</color>",
                $"One of the <color=#ff9944>windows stopped working</color> a while back. <color=#aaaaaa>I just leave it closed. Not a big deal really.</color>",
            }) : "A warning light comes on occasionally, turns itself off after a while, never figured out what it was.",

                    SellerArchetype.Dealer => null,

                    SellerArchetype.Scammer => level == 1 ? Pick(_rng, new[]
                    {
                $"<color=#99ff99>All electrics working perfectly</color> on the <b>{l.Model}</b>. <color=#aaaaaa>No warning lights on the dash at all.</color>",
                $"Electrics on the <b>{l.Make}</b> are <color=#99ff99>absolutely fine</color>. <color=#99ff99>New battery fitted</color> recently too.",
                $"<color=#99ff99>Everything works</color> — windows, lights, radio, all of it. <color=#aaaaaa>Had no electrical issues in all the time I have owned the <b>{l.Model}</b>.</color>",
            }) : "All electrics working perfectly, no warning lights on the dash.",

                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.ExhaustRusted))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
                    {
                $"The exhaust on the <b>{l.Model}</b> is <color=#ff9944>getting rusty</color>. <color=#aaaaaa>Nothing hanging off yet but I was told it will need attention before long.</color>",
                $"There is <color=#ff9944>visible rust on the exhaust</color> of the <b>{l.Make}</b>. <color=#aaaaaa>It is a bit louder than it used to be because of it. Disclosed in the photos.</color>",
                $"The <b>{l.Year}</b> <b>{l.Make}</b> exhaust <color=#ff9944>rattles slightly</color>. <color=#aaaaaa>I think it is the rust causing it to work loose at one of the joints. Should not be a huge job.</color>",
                $"Exhaust on the <b>{l.Model}</b> has <color=#ff9944>seen better days</color>. <color=#aaaaaa>Rusty in places — nothing has fallen off yet but it is not far away. Honest price for an honest car.</color>",
            }) : "Exhaust has some rust on it and will need attention before long — nothing structural but worth knowing.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"It is a <color=#ff9944>bit louder</color> than it used to be, especially when cold. <color=#aaaaaa>Quiets down once it warms up.</color>",
                $"The <b>{l.Make}</b> <color=#ff9944>blows a tiny bit</color> from somewhere under the car. <color=#aaaaaa>I can barely hear it inside with the radio on.</color>",
            }) : "It is a bit louder than it used to be, especially when cold, but it quiets down once it warms up.",

                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => null,
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.GlassDamage))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1 ? Pick(_rng, new[]
                    {
                $"There is a <color=#ff9944>small chip in the windscreen</color> of the <b>{l.Model}</b>. <color=#aaaaaa>Not a crack, just a chip — visible in the photos. Probably repairable for not much money.</color>",
                $"The <b>{l.Make}</b> has a <color=#ff9944>stone chip on the windscreen</color>. <color=#aaaaaa>I kept meaning to get it repaired. It has not spread in the year I have had the car.</color>",
                $"Windscreen on the <b>{l.Year}</b> <b>{l.Make}</b> has <color=#ff9944>a small mark on the passenger side</color>. <color=#aaaaaa>Barely noticeable from the driver's seat but I wanted to mention it. Shown in photos.</color>",
                $"There is a <color=#ff9944>chip in the glass</color> on the <b>{l.Model}</b>. <color=#aaaaaa>Insurance might cover a repair — I just never bothered to claim. Worth mentioning either way.</color>",
            }) : "There is a small chip in the windscreen — not a crack, just a chip, disclosed in the photos.",

                    SellerArchetype.Wrecker => level == 1 ? Pick(_rng, new[]
                    {
                $"Small <color=#ff9944>mark on the windscreen</color> that I never got around to fixing. <color=#aaaaaa>Barely notice it when driving.</color>",
                $"There is a <color=#ff9944>chip in the glass</color>. <color=#aaaaaa>Has been there since I bought it. Never got worse.</color>",
            }) : "Small mark on the windscreen that I never got around to fixing, barely notice it when driving.",

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
                    "Rough condition and I will not pretend otherwise — good project car for someone with the time and skills.",
                    "Mechanically tired but still drives, everything I know about its faults is disclosed here.",
                    "Several things need attention on this one — priced to reflect the actual state of it, not wishful thinking.",
                }) : IsMid(l) ? Pick(rng, new[]
                {
                    $"Standard condition for a {l.Year} with this kind of mileage — nothing critical outstanding.",
                    "Decent runner, a couple of small things to sort but nothing that will leave you at the side of the road.",
                    "Used regularly and serviced when it needed it — what you see is honestly what you get.",
                }) : Pick(rng, new[]
                {
                    "Good honest example, well maintained throughout its life and nothing hidden.",
                    "Solid car, everything works as it should, priced fairly for the actual condition.",
                    $"Well looked after {l.Year} with a clear service history — no surprises waiting for the new owner.",
                });
            }
            else
            {
                opener = IsBad(l) ? Pick(rng, new[]
                {
                    "Fair condition — I have documented every fault and every repair, nothing has been swept under the rug.",
                    "Not concours but every issue is accounted for and priced in — I have sold enough cars to know that honesty saves everyone time.",
                    "Priced for what it actually is rather than what I might wish it were — receipts and records available on viewing.",
                }) : IsMid(l) ? Pick(rng, new[]
                {
                    "Fair example with full paperwork — maintained properly and every receipt is here to prove it.",
                    $"Solid {l.Year} that has been looked after correctly — priced at market for its actual condition, not an aspirational figure.",
                    "Everything done to this car has been recorded and receipted — no guesswork, no hidden surprises.",
                }) : Pick(rng, new[]
                {
                    "Genuine sale from a careful private owner — full documented history, every stamp present.",
                    $"One of the best {l.Year} examples I have come across and I have seen a few over the years.",
                    "Fifty-plus cars sold over the years and every single one described exactly as it was — this is no different.",
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
                    $"{l.Mileage:N0} on the clock and genuine — nothing wound back.",
                    $"{l.Year} model, had it serviced within the last year and it came back with a clean bill of health on the major stuff.",
                    $"Oil changed regularly, tyres have decent tread remaining, recent MOT with no advisories.",
                    $"Mileage is honest at {l.Mileage:N0} — I have the old MOT certificates to back it up if needed.",
                });
            }
            else
            {
                detail1 = Pick(rng, new[]
                {
                    $"Mileage verified at {l.Mileage:N0} and backed up by full service history — every entry stamped and dated.",
                    $"{l.Year} — serviced on schedule without exception, receipts for everything going back years.",
                    $"I have owned worse and sold them honestly too — this one I am genuinely proud of.",
                    $"Receipts from new, mileage at {l.Mileage:N0}, every service done at the right interval.",
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
                    "Interior is clean and tidy, no rips or damage to report.",
                    $"Always garaged overnight which has kept the bodywork in decent shape for a {l.Year}.",
                    "No warning lights, no unusual noises, drives as it should for the age and mileage.",
                    "Cambelt was done at the correct interval — paperwork available.",
                    $"Bought it with {l.Mileage:N0} on it and have done very few miles since — mostly just ticking over.",
                }, 0.70);
            }
            else
            {
                detail2 = MaybePick(rng, new[]
                {
                    "Cambelt, water pump, and all consumables done at correct intervals — nothing overdue.",
                    "Bodywork is straight and original, never repaired or resprayed as far as I can tell.",
                    $"Purchased from new in {l.Location} and kept locally its whole life — no motorway abuse.",
                    "I have sold over fifty cars privately and never had a complaint — my reputation matters more than a quick sale.",
                    "Viewing strongly encouraged — this is the kind of car that sells itself once you see it in person.",
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
                closer = Pick(rng, new[]
                {
                    "Viewing welcome any reasonable time, no pressure and no games.",
                    "Ask me anything — if I know the answer I will tell you, if I do not I will say so.",
                    "Sensible asking price based on actual condition, open to serious offers.",
                    "What you see is what you get — no hidden nasties, no last-minute surprises.",
                    "Happy to have it inspected by a mechanic of your choice before purchase.",
                });
            }
            else
            {
                closer = Pick(rng, new[]
                {
                    "Viewing strongly encouraged — price is firm because the car justifies it.",
                    "I would rather it go to someone who will appreciate it than sell it fast to the wrong person.",
                    "Lowballers will be politely ignored — the price reflects exactly what the car is.",
                    "Sold with the same honesty I would expect if I were on the other side of the deal.",
                    "No rush, no games — serious buyers only and they will not be disappointed.",
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

            // ── OPENER ───────────────────────────────────────────────────────
            string opener = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Good runner as far as I know, selling as-is and the price reflects that.",
                    $"Been sitting on the drive more than being driven lately if I am honest.",
                    $"Starts fine every time I have tried it, just needs someone who will actually use it.",
                    $"Used it regularly until a few months ago, has been parked up since — nothing dramatic happened.",
                }),
                2 => Pick(rng, new[]
                {
                    "Selling this quickly, no time for extended viewings or back-and-forth.",
                    "Works, drives, stops — that is genuinely all I can tell you and it is all you need to know.",
                    "Listed a few this week, not going to pretend I know the full history of each one.",
                    "No time for questions about specific parts or service records — price is what it is.",
                }),
                _ => Pick(rng, new[]
                {
                    "One of several I am moving on — making space more than making money.",
                    $"Been under a tarp for a while now, exact duration is a bit unclear if I am honest.",
                    "Too many in the collection, this one needs to go to make room for other things.",
                    $"Acquired this one as part of a lot and never got around to doing anything with it.",
                }),
            };

            // ── DETAIL 1 ─────────────────────────────────────────────────────
            string detail1 = lv switch
            {
                1 => Pick(rng, new[]
                {
                    $"Has {l.Mileage:N0} miles on it which is whatever, these things run for ages if you look after them.",
                    $"Oil probably needs doing — or maybe I did it recently, hard to keep track.",
                    $"Last proper service was a while back, I could not give you an exact date.",
                    $"Battery is probably the original — still cranks it over fine so I never replaced it.",
                    $"It is a {l.Year} so there is some age on it but it has been a reliable thing.",
                }),
                2 => Pick(rng, new[]
                {
                    $"Photos were taken recently enough, what you see is what you get.",
                    $"If something needs doing you will find out when you buy it — I have not done a full inspection.",
                    $"Condition is what it is — it moves under its own power and that is the main thing.",
                    $"I have not checked it over in detail, no time for that, price accounts for the unknown.",
                    $"Has {l.Mileage:N0} on it — genuine as far as I know, I have not had reason to doubt it.",
                }),
                _ => Pick(rng, new[]
                {
                    $"Everything is original as far as I can tell — nothing has been replaced in a very long time.",
                    $"Was running when I last tried it — that was {(rng.Next(2) == 0 ? "a few months" : "last year")} ago.",
                    $"Stored in covered conditions, mostly dry, away from the worst of the weather.",
                    $"No service history to speak of and no paperwork — it is what it is.",
                    $"Has {l.Mileage:N0} on the clock, no idea if that is accurate, never had cause to check.",
                }),
            };

            // ── DETAIL 2 (optional ~65%) ─────────────────────────────────────
            string detail2 = lv switch
            {
                1 => MaybePick(rng, new[]
                {
                    "Tyres look like they have got some life left in them, at least on the outside.",
                    "Interior is a bit dusty but nothing a hoover would not sort out.",
                    $"Drove it to {l.Location} and back without any issues, which is about as much testing as I did.",
                    "Starts on the first or second turn every time, which is good enough for me.",
                    "Never heard any worrying noises from the engine, for whatever that is worth.",
                }, 0.65),
                2 => MaybePick(rng, new[]
                {
                    "Not going to arrange a mechanic inspection or anything like that — buy it as seen.",
                    "The photos show exactly what is there — nothing hidden, nothing staged.",
                    "Cash on collection, no holding it, first come first served.",
                    "Not interested in part exchange or any complicated arrangements.",
                    "Been through similar cars before — this one is priced to go, not to sit.",
                }, 0.65),
                _ => MaybePick(rng, new[]
                {
                    $"This is number {rng.Next(6, 18)} in the current collection — some are better, some are worse.",
                    "Did it run when parked? Almost certainly yes. Does it run now? One way to find out.",
                    "No MOT, no tax, trailer it away or sort it out yourself — no problem either way.",
                    "I know what I paid for these and I know what the parts are worth — this price is fair for what it is.",
                    "Garage kept for the most part — the roof leaks in one corner but that side of the car is fine.",
                }, 0.65),
            };

            // ── FAULT ────────────────────────────────────────────────────────
            string fault = DominantFaultLine(l, SellerArchetype.Honest, lv, rng);
            double faultChance = lv switch { 1 => 0.65, 2 => 0.40, _ => 0.20 };
            if (fault != null && rng.NextDouble() > faultChance) fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────
            string closer = lv switch
            {
                1 => Pick(rng, new[]
                {
                    "Price is firm, not looking for negotiations.",
                    "Priced for a quick sale — someone will get a decent deal here.",
                    "Just needs someone who will use it rather than let it sit.",
                    "No swaps, cash only, collect from my address.",
                }),
                2 => Pick(rng, new[]
                {
                    "Collect this week or I will relist — no time to wait around.",
                    "No negotiation, no sob stories, no holding it.",
                    "Price is price — take it or leave it, genuinely do not mind.",
                    "First with the cash takes it, simple as that.",
                }),
                _ => Pick(rng, new[]
                {
                    "Priced to clear space rather than make money — take it away and do what you like with it.",
                    "Not going to chase anyone up — if it goes it goes, if not I will deal with it another way.",
                    "Selling the collection in batches — this one is in the current batch.",
                    "One way to find out if it starts — come and turn the key.",
                }),
            };

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
                    "Freshly detailed inside and out — looks a lot better in person than any photos can show.",
                    "Picked this up, gave it a proper clean and a good going over, passing it on now.",
                    "Touched up a few marks, deep cleaned the interior, runs smoothly and looks presentable.",
                    $"Sorted out the cosmetics on this one — it came up really well after a bit of attention.",
                }),
                2 => Pick(rng, new[]
                {
                    "Full professional valet completed, recent service, drives like a much newer car.",
                    $"Immaculate example of a {l.Year} {l.Make} {l.Model} — price is firm because the quality speaks for itself.",
                    "Turn-key ready, not a project, everything checked and working as it should.",
                    "Had this fully prepared before listing — not one of those rush jobs.",
                }),
                _ => Pick(rng, new[]
                {
                    $"Dealer maintained throughout its life with full electronic history to confirm it.",
                    $"One of the cleanest {l.Year} {l.Model} examples currently available — and priced accordingly.",
                    "Former company vehicle with low-stress usage, single driver from new, all original components.",
                    $"This is the kind of {l.Make} you buy when you want to buy once and buy right.",
                }),
            };

            // ── DETAIL 1 ─────────────────────────────────────────────────────
            string detail1 = lv switch
            {
                1 => Pick(rng, new[]
                {
                    "Runs really smoothly now, no rattles, no warning lights showing.",
                    "Drives well — fluid, composed, nothing to complain about on the road.",
                    "Looks and feels considerably better than when I got it — the work shows.",
                    "Ready to drive away today, nothing outstanding to deal with.",
                }),
                2 => Pick(rng, new[]
                {
                    $"Every service stamp is present and accounted for, history is complete.",
                    "One owner history sourced, garage kept its entire life, exceptional for the age.",
                    $"Brakes checked and bedded in, fluids all topped up, all lights and electrics functional.",
                    "Selling due to a change in circumstances — this would not be going otherwise.",
                }),
                _ => Pick(rng, new[]
                {
                    $"Full electronic history check passed with zero flags — mileage verified at {l.Mileage:N0}.",
                    "Interior and exterior both professionally prepared — the detail work on this is exceptional.",
                    $"WBAC valued this significantly higher than my asking price — genuine price reduction this week only.",
                    "Full independent inspection report available on request — nothing to hide means nothing to hide.",
                }),
            };

            // ── DETAIL 2 (optional ~75%) ─────────────────────────────────────
            string detail2 = lv switch
            {
                1 => MaybePick(rng, new[]
                {
                    "Interior is clean with no rips, stains or damage — looks good.",
                    "Paintwork responds well — came up nicely after a machine polish on the panels.",
                    $"Wheels are clean, tyres are decent, nothing embarrassing about the way this presents.",
                    "No smoke on startup, no unusual noises, no warning lights — basics are all covered.",
                }, 0.75),
                2 => MaybePick(rng, new[]
                {
                    $"Comes with {(rng.Next(2) == 0 ? "a full set of keys" : "two keys")} and all original documentation.",
                    $"Bodywork is straight with no accident history — clean HPI to confirm.",
                    "Alloys are clean, tyres are nearly new, presentation is genuinely excellent.",
                    "This is the kind of car where a viewing sells it — photos do not do it justice.",
                }, 0.75),
                _ => MaybePick(rng, new[]
                {
                    $"Comes with full documentation, both sets of keys, and a clean HPI certificate.",
                    "Paint is in remarkable condition for the age — no fading, no chips worth mentioning.",
                    $"Tyres are all matching brand with plenty of tread — not the kind of thing you see on a {l.Year}.",
                    "Interior shows barely any wear — this car has been treated properly its entire life.",
                }, 0.75),
            };

            // ── CLOSER ───────────────────────────────────────────────────────
            string closer = lv switch
            {
                1 => Pick(rng, new[]
                {
                    "Presentable car at a price that makes sense — come and see for yourself.",
                    "Not a project — everything sorted, ready to use immediately.",
                    "Happy to arrange a viewing at a convenient time.",
                    "Priced fairly — not the cheapest out there but you get what you pay for.",
                }),
                2 => Pick(rng, new[]
                {
                    "First to view will buy — these do not hang around at this price.",
                    "Serious buyers only — tyre kickers and lowballers will be ignored.",
                    "Price is firm and reflects exactly what is on offer here.",
                    "Viewing by appointment — contact me to arrange a time.",
                }),
                _ => Pick(rng, new[]
                {
                    "Priced below comparable listings — serious buyers should move quickly.",
                    "I stand behind every car I sell — satisfaction is not negotiable.",
                    "Come and see it — once you do, the price will make complete sense.",
                    "The kind of car that will not be here long at this price — act accordingly.",
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
                    "Reluctant sale of what is genuinely one of the best examples of this car I have seen.",
                    $"Owned and maintained obsessively for the past six years — selling only due to relocation.",
                    "One careful private owner from new — this is exactly what that phrase is supposed to mean.",
                    $"The kind of {l.Make} {l.Model} you spend months looking for and then regret selling.",
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
                    $"Mileage is genuine and independently verified at {l.Mileage:N0} — full audit trail available.",
                    $"Complete service history with every stamp — {l.Year} with paperwork going back to delivery.",
                    "Had it independently inspected by a specialist last week — report is clean and available on viewing.",
                    $"Cambelt, water pump, and all ancillaries replaced at the correct mileage — nothing overdue.",
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
                    "Paintwork is in exceptional condition — no chips, no fading, no repairs.",
                    $"Interior is immaculate — looks genuinely like a car with half the mileage.",
                    $"Both keys, full documentation, clean HPI, and an independent inspection report — nothing missing.",
                    "This is the listing where everything checks out — which is why the price is what it is.",
                    "Priced slightly below market to ensure a fast sale — not because there is anything wrong.",
                }, 0.70),
            };

            // ── FAULT ────────────────────────────────────────────────────────
            string fault = DominantFaultLine(l, SellerArchetype.Honest, lv, rng);
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
                    "Priced below what I paid because the move is confirmed and this needs to go — simple as that.",
                    "Nothing to disclose because there is genuinely nothing wrong — come and verify that for yourself.",
                    "Will not be undersold — I know what this car is worth and so will you when you see it.",
                    "Selling due to confirmed relocation — this is not a situation I wanted but here we are.",
                    "The price reflects a motivated seller, not a problem car — I want this to go to the right person.",
                }),
            };

            return Fill(Join(opener, detail1, detail2, fault, closer), l);
        }
    }
}