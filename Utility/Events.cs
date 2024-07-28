using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace Polyamory
{
    internal partial class Polyamory
    {
        [EventPriority(EventPriority.Low)]
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo($"{modid}\\PolyamoryData"))
            {
                e.LoadFromModFile<Dictionary<string, PolyamoryData>>("Assets\\DefaultData.json", AssetLoadPriority.High);
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Strings\\StringsFromCSFiles"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;

                    data.Add("RejectBouquet_IsMonogamous_PlayerWithOtherPeople", I18n.RejectBouquet_IsMonogamous_PlayerWithOtherPeople());
                    data.Add("RejectBouquet_IsPolyamorous_PlayerWithSomeoneMonogamous", I18n.RejectBouquet_IsPolyamorous_PlayerWithSomeoneMonogamous());
                    data.Add("RejectMermaidPendant_IsMonogamous_PlayerWithOtherPeople", I18n.RejectMermaidPendant_IsMonogamous_PlayerWithOtherPeople());
                    data.Add("RejectMermaidPendant_IsPolyamorous_PlayerWithSomeoneMonogamous", I18n.RejectMermaidPendant_IsPolyamorous_PlayerWithSomeoneMonogamous());
                    data.Add("RejectRoommateProposal_IsMonogamous_PlayerWithOtherPeople", I18n.RejectRoommateProposal_IsMonogamous_PlayerWithOtherPeople());
                    data.Add("RejectRoommateProposal_IsPolyamorous_PlayerWithSomeoneMonogamous", I18n.RejectRoommateProposal_IsPolyamorous_PlayerWithSomeoneMonogamous());
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
                    if (data.TryGetValue(key, out var value))
                    {
                        string newValue = value.Replace("5000", $"{Config.PendantPrice}");
                        data[key] = newValue;
                    }
                });
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
                            else if (Game1.timeOfDay < 2000 && Game1.timeOfDay >= 600)
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
