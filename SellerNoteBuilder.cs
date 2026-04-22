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

        private static string DominantFaultLine(CarListing l, SellerArchetype arch, int level)
        {
            if (l.Faults.HasFlag(FaultFlags.HeadGasket))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? "A mechanic looked at it and mentioned something about the head gasket — I did not fully understand what that meant but it sounds expensive."
                        : "Head gasket has failed — coolant is mixing with oil and the engine needs proper work before it goes anywhere.",
                    SellerArchetype.Wrecker => "There is white smoke on startup sometimes, usually clears after a minute or two.",
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => "Engine runs perfectly, starts first time every time, no smoke, no leaks.",
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.TimingBelt))
                return arch switch
                {
                    SellerArchetype.Honest => level == 1
                        ? "My mate who knows cars said the timing belt might be overdue but I honestly have no idea when it was last done."
                        : "Timing belt is overdue for replacement — I would not drive it far before sorting that, and the price has been set accordingly.",
                    SellerArchetype.Wrecker => "I could not tell you when the timing belt was last changed — no paperwork for that.",
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => "Full service history with the car, timing belt was replaced as part of the last service.",
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.BrakesGone))
                return arch switch
                {
                    SellerArchetype.Honest => "Brakes are worn down and need replacing before this goes on a public road — pads are basically metal on metal at this point.",
                    SellerArchetype.Wrecker => "Stopping distance feels a little longer than it used to but I have always been a cautious driver.",
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => "Brakes were checked and adjusted at the last service, no issues found.",
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.SuspensionWorn))
                return arch switch
                {
                    SellerArchetype.Honest => "Suspension is worn out — shocks are soft and there are some clunks over bumps that need sorting.",
                    SellerArchetype.Wrecker => "Rides a bit firm maybe, could be the roads around here, never bothered checking.",
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => "Suspension feels tight and responsive, no knocks or creaks.",
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.ElectricalFault))
                return arch switch
                {
                    SellerArchetype.Honest => "There is an electrical fault — the alternator is not charging properly and the battery keeps going flat, which is why the price is what it is.",
                    SellerArchetype.Wrecker => "A warning light comes on occasionally, turns itself off after a while, never figured out what it was.",
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => "All electrics working perfectly, no warning lights on the dash.",
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.ExhaustRusted))
                return arch switch
                {
                    SellerArchetype.Honest => "Exhaust has some rust on it and will need attention before long — nothing structural but worth knowing.",
                    SellerArchetype.Wrecker => "It is a bit louder than it used to be, especially when cold, but it quiets down once it warms up.",
                    SellerArchetype.Dealer => null,
                    SellerArchetype.Scammer => null,
                    _ => null,
                };

            if (l.Faults.HasFlag(FaultFlags.GlassDamage))
                return arch switch
                {
                    SellerArchetype.Honest => "There is a small chip in the windscreen — not a crack, just a chip, disclosed in the photos.",
                    SellerArchetype.Wrecker => "Small mark on the windscreen that I never got around to fixing, barely notice it when driving.",
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
                    $"Starts and drives but honestly something does not feel quite right with it, I just cannot put my finger on what.",
                    $"Runs, not brilliantly, but it gets from A to B without stopping — has some issues I cannot properly describe.",
                    $"Drove this every day for a while then things started going wrong, decided to sell before spending money I do not have.",
                }) : IsMid(l) ? Pick(rng, new[]
                {
                    $"Daily driver for the last couple of years, been mostly reliable and never left me stranded.",
                    $"Starts every time and drives fine as far as I can tell, not a car person so cannot say much more than that.",
                    $"Got this a few years ago and it has done the job, time to move on now that I have something newer.",
                }) : Pick(rng, new[]
                {
                    $"Well looked after as best I could, always garaged and kept clean, runs really well.",
                    $"Pretty good condition I think — always started first time and never gave me any real trouble.",
                    $"Bought this new and kept it properly, full records available, genuinely one careful owner.",
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
                    $"Has {l.Mileage:N0} miles on it which is quite a lot I know, but it has always run.",
                    $"It is a {l.Year} so there is definitely some age on it — shows in places but nothing shocking.",
                    $"I am not very mechanically minded so I cannot give you a detailed rundown, but I have tried to be honest about what I know.",
                    $"A friend who knows about cars had a look and said it needs a few things but nothing that would make it undriveable.",
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
                    "Tyres look alright to me but I am no expert — might be worth a proper look.",
                    "Interior is a bit lived in but clean enough, nothing broken inside.",
                    $"Bought it from a private sale in {l.Location} and it has been fine since.",
                    "Never had any warning lights on the dash the whole time I have owned it.",
                    "Had it looked over by a local garage a while back and they said it was fine.",
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
            string fault = DominantFaultLine(l, SellerArchetype.Honest, lv);
            if (lv == 1 && fault != null && rng.NextDouble() < 0.25)
                fault = null;

            // ── CLOSER ───────────────────────────────────────────────────────
            string closer;
            if (lv == 1)
            {
                closer = Pick(rng, fault != null ? new[]
                {
                    "Priced low to account for that — someone who knows what they are doing will get good value here.",
                    "That is why the price is what it is — not trying to hide anything.",
                    "I would rather be upfront and price it honestly than waste everyone's time.",
                } : new[]
                {
                    "Priced to reflect what it is — not asking the earth.",
                    "Selling because I got something newer, no longer needed.",
                    "Come and have a look if you are interested, no pressure at all.",
                    "Just want it gone to a good home, priced accordingly.",
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
            string fault = DominantFaultLine(l, SellerArchetype.Wrecker, lv);
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
            string fault = DominantFaultLine(l, SellerArchetype.Scammer, lv);
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