// ListingPersistence.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CMS2026_OXL
{
    public static class ListingPersistence
    {
        private static string SavePath =>
            Path.Combine(Application.dataPath, "..", "Mods", "CMS2026_OXL", "listings.dat");

        // ═════════════════════════════════════════════════════════════════════
        //  SAVE
        // ═════════════════════════════════════════════════════════════════════

        public static void Save(List<CarListing> listings, float gameTime)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# OXL listings snapshot — do not edit manually");
                sb.AppendLine($"# game_time = {gameTime:F1}");
                sb.AppendLine($"# count = {listings.Count}");
                sb.AppendLine();

                int saved = 0;
                foreach (var l in listings)
                {
                    float remaining = l.ExpiresAt - gameTime;
                    if (remaining <= 0f) continue;

                    sb.AppendLine("[listing]");
                    sb.AppendLine($"make={l.Make}");
                    sb.AppendLine($"model={l.Model}");
                    sb.AppendLine($"year={l.Year}");
                    sb.AppendLine($"color={l.Color}");
                    sb.AppendLine($"color_index={l.ColorIndex}");
                    sb.AppendLine($"registration={l.Registration}");
                    sb.AppendLine($"image_folder={l.ImageFolder}");
                    sb.AppendLine($"internal_id={l.InternalId}");
                    sb.AppendLine($"price={l.Price}");
                    sb.AppendLine($"fair_value={l.FairValue}");
                    // float z InvariantCulture — bezpieczne na każdej lokalizacji
                    sb.AppendLine($"apparent={l.ApparentCondition.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"actual={l.ActualCondition.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"body={l.BodyCondition.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)}");
                    sb.AppendLine($"archetype={l.Archetype}");
                    sb.AppendLine($"archetype_level={l.ArchetypeLevel}");
                    sb.AppendLine($"faults={(int)l.Faults}");
                    sb.AppendLine($"mileage={l.Mileage}");
                    sb.AppendLine($"location={l.Location}");
                    sb.AppendLine($"delivery_hours={l.DeliveryHours}");
                    sb.AppendLine($"seller_rating={l.SellerRating}");
                    sb.AppendLine($"remaining_sec={remaining.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}");
                    // Nota Base64 — unika problemów ze znakami specjalnymi i nowymi liniami
                    sb.AppendLine($"seller_note_b64={Convert.ToBase64String(Encoding.UTF8.GetBytes(l.SellerNote ?? ""))}");
                    // PhotoFiles — każdy na osobnej linii, bez trim przy odczycie
                    sb.AppendLine($"photo_count={l.PhotoFiles.Count}");
                    for (int i = 0; i < l.PhotoFiles.Count; i++)
                        sb.AppendLine($"photo_{i}={l.PhotoFiles[i]}");

                    sb.AppendLine();
                    saved++;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.WriteAllText(SavePath, sb.ToString(), Encoding.UTF8);
                OXLPlugin.Log.Msg($"[OXL:PERSIST] Saved {saved} listings.");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL:PERSIST] Save error: {ex.Message}");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  LOAD
        // ═════════════════════════════════════════════════════════════════════

        public static List<CarListing> Load(float currentGameTime)
        {
            var result = new List<CarListing>();

            if (!File.Exists(SavePath))
            {
                OXLPlugin.Log.Msg("[OXL:PERSIST] No listings.dat — starting fresh.");
                return result;
            }

            try
            {
                var lines = File.ReadAllLines(SavePath, Encoding.UTF8);
                CarListing current = null;
                int photoCount = 0;
                int photosRead = 0;
                float remaining = 0f;

                foreach (var rawLine in lines)
                {
                    string line = rawLine.TrimEnd(); // tylko trailing whitespace
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    if (line == "[listing]")
                    {
                        CommitListing(result, current, remaining, currentGameTime);
                        current = new CarListing();
                        photoCount = 0;
                        photosRead = 0;
                        remaining = 0f;
                        continue;
                    }

                    if (current == null) continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    // val — NIE trim dla ścieżek (mogą zawierać spacje),
                    // ale photo_N obsługujemy osobno
                    string val = line.Substring(eq + 1);

                    switch (key)
                    {
                        case "make": current.Make = val.Trim(); break;
                        case "model": current.Model = val.Trim(); break;
                        case "year": TrySetInt(val, v => current.Year = v); break;
                        case "color": current.Color = val.Trim(); break;
                        case "color_index": TrySetInt(val, v => current.ColorIndex = v); break;
                        case "registration": current.Registration = val.Trim(); break;
                        case "image_folder": current.ImageFolder = val.Trim(); break;
                        case "internal_id": current.InternalId = val.Trim(); break;
                        case "price": TrySetInt(val, v => current.Price = v); break;
                        case "fair_value": TrySetInt(val, v => current.FairValue = v); break;
                        case "apparent": TrySetFloat(val, v => current.ApparentCondition = v); break;
                        case "actual": TrySetFloat(val, v => current.ActualCondition = v); break;
                        case "body": TrySetFloat(val, v => current.BodyCondition = v); break;
                        case "archetype":
                            if (Enum.TryParse(val.Trim(), out SellerArchetype arch))
                                current.Archetype = arch;
                            break;
                        case "archetype_level": TrySetInt(val, v => current.ArchetypeLevel = v); break;
                        case "faults":
                            TrySetInt(val, v => current.Faults = (FaultFlags)v);
                            break;
                        case "mileage": TrySetInt(val, v => current.Mileage = v); break;
                        case "location": current.Location = val.Trim(); break;
                        case "delivery_hours": TrySetInt(val, v => current.DeliveryHours = v); break;
                        case "seller_rating": TrySetInt(val, v => current.SellerRating = v); break;
                        case "remaining_sec": TrySetFloat(val, v => remaining = v); break;
                        case "seller_note_b64":
                            try
                            {
                                current.SellerNote = Encoding.UTF8.GetString(
                                    Convert.FromBase64String(val.Trim()));
                            }
                            catch { current.SellerNote = ""; }
                            break;
                        case "photo_count":
                            TrySetInt(val, v =>
                            {
                                photoCount = v;
                                current.PhotoFiles = new List<string>(v);
                            });
                            break;
                        default:
                            // photo_N — val bez trim, ścieżka może mieć spacje
                            if (key.StartsWith("photo_") && photosRead < photoCount)
                            {
                                current.PhotoFiles.Add(val);
                                photosRead++;
                            }
                            break;
                    }
                }

                // Zatwierdź ostatni listing
                CommitListing(result, current, remaining, currentGameTime);

                OXLPlugin.Log.Msg($"[OXL:PERSIST] Loaded {result.Count} listings from previous session.");
            }
            catch (Exception ex)
            {
                OXLPlugin.Log.Msg($"[OXL:PERSIST] Load error: {ex.Message}");
            }

            return result;
        }

        public static void Delete()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch { }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void CommitListing(List<CarListing> list, CarListing l,
                                          float remaining, float gameTime)
        {
            if (l == null || remaining <= 0f) return;
            l.ExpiresAt = gameTime + remaining;
            list.Add(l);
        }

        private static void TrySetInt(string val, Action<int> set)
        {
            if (int.TryParse(val.Trim(), out int v)) set(v);
        }

        private static void TrySetFloat(string val, Action<float> set)
        {
            if (float.TryParse(val.Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out float v)) set(v);
        }
    }
}