using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using ContentPatcher;
using GenericModConfigMenu;

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
        }

        [EventPriority(EventPriority.Low)]
        private void OnAssetRequested2(object? sender, AssetRequestedEventArgs e)
        {
            if (Context.IsWorldReady && Game1.player is not null)
            {
                foreach (string npc in Game1.characterData.Keys)
                {
                    if (Game1.getCharacterFromName(npc) is null) continue;
                    if (!Game1.player.friendshipData.TryGetValue(npc, out var _)) continue;
                    if (!Game1.getCharacterFromName(npc, true).datable.Value) continue;

                    if (e.NameWithoutLocale.IsEquivalentTo("Characters\\Dialogue\\" + npc))
                    {
                        monitor.Log($"{npc}");
                        e.Edit(asset =>
                        {
                            var data = asset.AsDictionary<string, string>().Data;
                            if (!IsNpcPolyamorous(Game1.player.spouse) && Game1.player.spouse != npc)
                            {
                                data.Remove("RejectItem_(O)458");
                                if (data.Any())
                                if (data.ContainsKey("Dating_NonPolyamorousNPC"))
                                {
                                    monitor.Log($"trying to give {npc} ");
                                    data.Add("RejectItem_(O)458", data["Dating_NonPolyamorousNPC"].ToString());
                                }
                                else
                                {
                                    data.Add("RejectItem_(O)458", I18n.Dating_NonPolyamorousNPC().Replace("$SpouseName$", Game1.getCharacterFromName(Game1.player.spouse).displayName, StringComparison.OrdinalIgnoreCase));
                                }
                            }
                            else if (!IsNpcPolyamorous(npc) && IsDatingOtherPeople(Game1.player, npc) && Game1.player.spouse != npc)
                            {
                                data.Remove("RejectItem_(O)458");
                                if (data.ContainsKey("Dating_IsNonPolyamorousNPC"))
                                {
                                    data.Add("RejectItem_(O)458", data["Dating_IsNonPolyamorousNPC"]);
                                }
                                else
                                {
                                    data.Add("RejectItem_(O)458", I18n.Dating_IsNonPolyamorousNPC());
                                }
                            }
                        });
                    }
                }
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var GMCM = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            var contentPatcher = helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

            if (GMCM is not null)
            {
                GMCM.Register(
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config)
                );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Pendant());

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_BuyPendantsAnytime_Name(),
                    tooltip: () => I18n.Config_BuyPendantsAnytime_Description(),
                    getValue: () => Config.BuyPendantsAnytime,
                    setValue: value => Config.BuyPendantsAnytime = value
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PendantPrice_Name(),
                    tooltip: () => I18n.Config_PendantPrice_Description(),
                    getValue: () => Config.PendantPrice,
                    setValue: value => Config.PendantPrice = value,
                    min: 0
                    );

                GMCM.AddParagraph(
                    mod: ModManifest,
                    text: () => "\n"
                    );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Divorce());

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PreventHostileDivorces_Name(),
                    tooltip: () => I18n.Config_PreventHostileDivorces_Description(),
                    getValue: () => Config.PreventHostileDivorces,
                    setValue: value => Config.PreventHostileDivorces = value
                    );

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_ComplexDivorce_Name(),
                    tooltip: () => I18n.Config_ComplexDivorce_Description(),
                    getValue: () => Config.ComplexDivorce,
                    setValue: value => Config.ComplexDivorce = value
                    );

                GMCM.AddParagraph(
                    mod: ModManifest,
                    text: () => "\n"
                    );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Children()
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_MaxChildren_Name(),
                    tooltip: () => I18n.Config_MaxChildren_Description(),
                    getValue: () => Config.MaxChildren,
                    setValue: value => Config.MaxChildren = value
                    );

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_ShowParentNames_Name(),
                    tooltip: () => I18n.Config_ShowParentNames_Description(),
                    getValue: () => Config.ShowParentNames,
                    setValue: value => Config.ShowParentNames = value
                    );

                GMCM.AddParagraph(
                    mod: ModManifest,
                    text: () => "\n"
                    );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Spouse()
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseInBed_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseInBed_Description(),
                    getValue: () => Config.PercentChanceForSpouseInBed,
                    setValue: value => Config.PercentChanceForSpouseInBed = value
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseInKitchen_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseInKitchen_Description(),
                    getValue: () => Config.PercentChanceForSpouseInKitchen,
                    setValue: value => Config.PercentChanceForSpouseInKitchen = value
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseAtPatio_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseAtPatio_Description(),
                    getValue: () => Config.PercentChanceForSpouseAtPatio,
                    setValue: value => Config.PercentChanceForSpouseAtPatio = value
                    );
            }

#pragma warning disable IDE0031 // Use null propagation
            if (contentPatcher is not null)
            {
                contentPatcher.RegisterToken(ModManifest, "PlayerSpouses", () =>
                {
                    Farmer player;

                    if (Context.IsWorldReady)
                        player = Game1.player;
                    else if (SaveGame.loaded?.player != null)
                        player = SaveGame.loaded.player;
                    else
                        return null;

                    var spouses = GetSpouses(player, true).Keys.ToList();
                    spouses.Sort(delegate (string a, string b) {
                        player.friendshipData.TryGetValue(a, out Friendship af);
                        player.friendshipData.TryGetValue(b, out Friendship bf);
                        if (af == null && bf == null)
                            return 0;
                        if (af == null)
                            return -1;
                        if (bf == null)
                            return 1;
                        if (af.WeddingDate == bf.WeddingDate)
                            return 0;
                        return af.WeddingDate > bf.WeddingDate ? -1 : 1;
                    });
                    return spouses.ToArray();
                });
            }
#pragma warning restore IDE0031 // Use null propagation
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
