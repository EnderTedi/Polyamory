using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace Polyamory
{
    internal partial class Polyamory
    {
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo($"{modid}\\PolyamoryData"))
                e.LoadFrom(() => new Dictionary<string, PolyamoryData>(), AssetLoadPriority.Low);

            if (e.NameWithoutLocale.IsEquivalentTo("Strings\\StringsFromCSFiles"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    data.Add("Dating_NonPolyamorousNPC", I18n.Dating_NonPolyamorousNPC());
                    data.Add("Dating_IsNonPolyamorousNPC", I18n.Dating_IsNonPolyamorousNPC());
                    data.Add("Marriage_NonPolyamorousNPC", I18n.Marriage_NonPolyamorousNPC());
                    data.Add("Marriage_IsNonPolyamorousNPC", I18n.Marriage_IsNonPolyamorousNPC());
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data\\Events\\HaleyHouse"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    foreach (string key in data.Keys)
                    {
                        if (key.StartsWith("195019") || key.StartsWith("195012"))
                        {
                            data.Remove(key);
                        }
                    }
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data\\Events\\Saloon"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    foreach (string key in data.Keys)
                    {
                        if (key.StartsWith("195099") || key.StartsWith("195013"))
                        {
                            data.Remove(key);
                        }
                    }
                });
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Strings\\Locations"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    string key = "Beach_Mariner_PlayerBuyItem_AnswerYes";
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    if (data.TryGetValue(key, out string value))
                    {
                        string newValue = value.ToString().Replace("5000", Config.PendantPrice.ToString());
                        data[key] = newValue;
                    }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                });
            }
        }

        [EventPriority(EventPriority.Low)]
        private void OnAssetRequested2(object? ender, AssetRequestedEventArgs e)
        {
            if (Context.IsWorldReady && Game1.player is not null)
            {
                foreach (string npc in Game1.characterData.Keys)
                {
                    if (Game1.player.spouse is null) break;
                    if (Game1.getCharacterFromName(npc) is null
                        || !Game1.player.friendshipData.TryGetValue(npc, out var _)
                        || !Game1.getCharacterFromName(npc, true).datable.Value
                        || Game1.player.getFriendshipHeartLevelForNPC(npc) < 8) continue;

                    if (e.NameWithoutLocale.IsEquivalentTo("Characters\\Dialogue\\" + npc))
                    {
                        monitor.Log($"{npc}");
                        e.Edit(asset =>
                        {
                            var data = asset.AsDictionary<string, string>().Data;
                            if (!IsNpcPolyamorous(npc) && !IsValidDating(Game1.player, npc))
                            {
                                data.Remove("RejectItem_(O)458");
                                if (data.ContainsKey("CantDate_IsMonogamousNPC"))
                                {
                                    data.Add("RejectItem_(O)458", data["Dating_IsNonPolyamorousNPC"]);
                                }
                                else
                                {
                                    data.Add("RejectItem_(O)458", I18n.Dating_IsNonPolyamorousNPC());
                                }
                            }
                            else if (!IsValidDating(Game1.player, npc))
                            {
                                data.Remove("RejectItem_(O)458");
                                if (data.Any())
                                    if (data.ContainsKey("CantDate_DatingNonPolyamorousNPC"))
                                    {
                                        monitor.Log($"trying to give {npc} ");
                                        data.Add("RejectItem_(O)458", data["CantDating_DatingMonogamousNPC"]);
                                    }
                                    else
                                    {
                                        data.Add("RejectItem_(O)458", I18n.Dating_NonPolyamorousNPC().Replace("$SpouseName$", Game1.getCharacterFromName(Game1.player.spouse).displayName, StringComparison.OrdinalIgnoreCase));
                                    }
                            }
                        }, AssetEditPriority.Late + 1);
                    }
                }
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ModAPIs.LoadAPIs();
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            Spouses.Clear();
            UnofficialSpouses.Clear();
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            ResetSpouses(Game1.player);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            ResetDivorces();
            ResetSpouses(Game1.player);

            foreach (GameLocation location in Game1.locations)
            {
                if (ReferenceEquals(location.GetType(), typeof(FarmHouse)))
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    PlaceSpousesInFarmhouse(location as FarmHouse);
#pragma warning restore CS8604 // Possible null reference argument.
                }
            }
            if (Game1.IsMasterGame)
            {
                Game1.getFarm().addSpouseOutdoorArea(Game1.player.spouse ?? "");
                farmHelperSpouse = GetRandomSpouse(Game1.MasterPlayer);
            }
            foreach (Farmer f in Game1.getAllFarmers())
            {
                var spouses = GetSpouses(f, true).Keys;
                foreach (string s in spouses)
                {
                    monitor.Log($"{f.Name} is married to {s}");
                }
            }
        }

        private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            foreach (GameLocation location in Game1.locations)
            {

                if (location is FarmHouse)
                {
                    FarmHouse? fh = location as FarmHouse;
                    if (fh?.owner == null)
                        continue;

                    List<string> allSpouses = GetSpouses(fh.owner, true).Keys.ToList();
                    List<string> bedSpouses = ReorderSpousesForSleeping(allSpouses.FindAll((s) => fh.owner.friendshipData[s].RoommateMarriage));

                    using IEnumerator<NPC> characters = fh.characters.GetEnumerator();
                    while (characters.MoveNext())
                    {
                        var character = characters.Current;
                        if (!(character.currentLocation == fh))
                        {
                            character.farmerPassesThrough = false;
                            character.HideShadow = false;
                            character.isSleeping.Value = false;
                            continue;
                        }

                        if (allSpouses.Contains(character.Name))
                        {

                            if (IsInBed(fh, character.GetBoundingBox()))
                            {
                                character.farmerPassesThrough = true;

                                if (!character.isMoving() /*&& (kissingAPI == null || kissingAPI.LastKissed(character.Name) < 0 || kissingAPI.LastKissed(character.Name) > 2)*/)
                                {
                                    Vector2 bedPos = GetSpouseBedPosition(fh, character.Name);
                                    if (Game1.timeOfDay >= 2000 || Game1.timeOfDay <= 600)
                                    {
                                        character.position.Value = bedPos;

                                        if (Game1.timeOfDay >= 2200)
                                        {
                                            character.ignoreScheduleToday = true;
                                        }
                                        if (!character.isSleeping.Value)
                                        {
                                            character.isSleeping.Value = true;

                                        }
                                        if (character.Sprite.CurrentAnimation == null)
                                        {
                                            if (!HasSleepingAnimation(character.Name))
                                            {
                                                character.Sprite.StopAnimation();
                                                character.faceDirection(0);
                                            }
                                            else
                                            {
                                                character.playSleepingAnimation();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        character.faceDirection(3);
                                        character.isSleeping.Value = false;
                                    }
                                }
                                else
                                {
                                    character.isSleeping.Value = false;
                                }
                                character.HideShadow = true;
                            }
                            else if (Game1.timeOfDay < 2000 && Game1.timeOfDay > 600)
                            {
                                character.farmerPassesThrough = false;
                                character.HideShadow = false;
                                character.isSleeping.Value = false;
                            }
                        }
                    }
                }
            }
        }
    }
}
