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
    $"GREETINGS. I am <color=#ffcc00>Official Prince</color> of small oil nation. Must sell personal {l.Make} to fund royal wedding.",
    $"Good car. No bad history. Small blood stain in trunk is from <color=#ff4444>beetroot juice</color>, very organic. No police please.",
    $"ATENTION: this {l.Model} belong to <color=#00ffff>Elon Musk</color> personal. He sign windscreen with invisible ink. Very rare!",
    $"Genuine sale! I am simple <color=#9b80c8>oil rig worker</color> currently at sea. My cousin will give you car after you pay me.",
    $"Selling fast because I have <color=#ff00ff>too many cars</color> and my wife says I am 'crazy'. Her loss is your gain!",
    $"I do not want to sell. This car is my <color=#00ff00>best friend</color>. But friend needs new home with more money.",
    $"HELLO FRIEND. You look like honest buyer. I have <color=#ffff00>special price</color> only for you, don't tell others.",
    $"Car was found in <color=#777777>secret bunker</color>. Previous owner was time traveler. Year {l.Year} is actually from future.",
}),
                2 => Pick(rng, new[]
                {
                    "Selling this on behalf of a family member who is currently working overseas.",
                    "Bought at a private auction, selling on privately — runs and drives without issue.",
                    $"Just had a full service completed last month — all receipts will be provided at collection.",
                    "Clean example, well looked after, reluctant sale due to a change in circumstances.",
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
    $"Previous owner was <color=#55ff55>NASA scientist</color>. Drove only on weekends. Both weekends. In space (no gravity wear).",
    $"I accept <color=#3b5998>PalPal</color>, Bitcoin, Dogecoin, and <color=#ff9900>Farget Gift Cards</color>. No cash, cash is for spies.",
    $"Deposit of <color=#ff4444>${(rng.Next(5, 15) * 100)}</color> required to unlock garage door. Door is very heavy, need money for grease.",
    $"Car is <color=#ffffff>invisible</color> to radar. Tested by my uncle who is general. {l.Mileage:N0} miles but feels like zero.",
    $"Warranty provided by <color=#9b80c8>Ghost of Mechanic</color>. If engine breaks, he will haunt you for free.",
    $"Engine is so clean you can eat soup from it. <color=#ffff00>Please do not eat engine.</color> It is for driving.",
    $"Mileage {l.Mileage:N0} is just a number. Like age. Or <color=#ff4444>criminal record</color>. It means nothing.",
    $"Car has <color=#00ffff>AI computer</color> inside. It only speaks Ancient Greek. Very sophisticated {l.Make}.",
}),
                2 => Pick(rng, new[]
                {
                    $"Mileage is {l.Mileage:N0} and fully verified — I have documentation to support this.",
                    "Minor seep from the valve cover gasket, completely normal for this age of engine, costs very little to fix.",
                    "Photos taken this morning in natural light — car is in storage, collection only.",
                    $"Owner relocated abroad last month — I have been given power of attorney to handle the sale.",
                    "Bought it at a main dealer auction, I have the purchase invoice if needed.",
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
    $"Car currently located in <color=#ff4444>High Security Zone</color>. Send $200 for 'Oxygen Fee' before viewing.",
    $"I ship car via <color=#00ff00>Secret Submarine</color>. Very fast delivery to {l.Location}. Just need fuel money first.",
    $"My uncle is <color=#ffcc00>President of Car Factory</color>. He says this {l.Model} is the best one they ever made.",
    $"If you buy today, I include <color=#9b80c8>Magic Air Freshener</color>. Smells like success and no refunds.",
    $"Accepting only <color=#3b5998>PaiPal</color>. Not PayPal. PaiPal. It is more safe because name is shorter.",
    $"No test drive because <color=#ff4444>tires are allergic</color> to ground in {l.Location}. Trust me, it moves very good.",
    $"Car was used in <color=#ffff00>Famous Movie</color> but all scenes were deleted. Still counts as celebrity car!",
}, 0.70),
                2 => MaybePick(rng, new[]
                {
                    "Bodywork is straight with no accident history — clean HPI available.",
                    $"Comes with both keys and a full set of documentation.",
                    "Alloys are clean, tyres are good, interior shows minimal wear.",
                    "Not a car that has been sitting around — used regularly and maintained properly.",
                    "Happy to provide any additional photos or information on request.",
                }, 0.70),
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
    $"I leave country in <color=#ff4444>5 minutes</color>. Buy now or I give car to hungry dog. Dog cannot drive but he is hungry.",
    $"God bless your wallet. This is <color=#00ff00>100% no scam</color>. I am too honest for my own good.",
    $"Price is firm like <color=#777777>frozen cabbage</color>. No lowballers, I know what I have (a car).",
    $"Send message via <color=#9b80c8>carrier pigeon</color> or deposit money. Deposit is faster. God speed.",
    $"Hurry! <color=#ffcc00>Many people</color> from FBI and CIA want to buy this car. I prefer you because you have nice face.",
    $"No refunds. No returns. <color=#ff4444>No speaking to my lawyer.</color> Have a nice day friend!",
    $"This {l.Make} is a <color=#00ffff>blessing</color>. Do not miss your chance to be blessed and slightly poorer.",
    $"If you find fault, it is <color=#ff00ff>bonus feature</color>. I don't charge extra for features. You are welcome.",
}),
                2 => Pick(rng, new[]
                {
                    "A couple of very minor things to sort but nothing that would put a competent buyer off.",
                    "Collection only — no delivery, no escrow, straightforward private sale.",
                    "Photos are accurate and up to date — what you see is what you will collect.",
                    "Move quickly on this one — at this price it will not be here long.",
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