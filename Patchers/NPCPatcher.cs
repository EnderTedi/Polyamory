using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Audio;
using StardewValley.Extensions;
using StardewValley.Locations;
using System.Reflection;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Color = Microsoft.Xna.Framework.Color;

using Microsoft.Xna.Framework;
using StardewValley.Characters;
using System.Collections;

namespace Polyamory.Patchers
{
    internal class NPCPatcher
    {
#pragma warning disable CS8618
        public static IModHelper helper;
        public static IMonitor monitor;
        public static ModConfig config;
#pragma warning restore CS8618

        public static void Initialize(IModHelper Helper, IMonitor Monitor, ModConfig Config)
        {
            helper = Helper;
            monitor = Monitor;
            config = Config;
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.tryToReceiveActiveObject))]
        public static class NPCPatch_tryToReceiveActiveObject
        {
            public static bool Prefix(NPC __instance, ref bool __result, ref string __state, Farmer who, bool probe = false)
            {
                if (who.friendshipData.ContainsKey(__instance.Name) && who.getFriendshipHeartLevelForNPC(__instance.Name) >= 8 && (__instance.modData is null || !__instance.modData.ContainsKey(Game1.uniqueIDForThisGame.ToString() + "PolyData")))
                {
#pragma warning disable CS8602
                    __instance.modData.Add(Game1.uniqueIDForThisGame.ToString() + "PolyData", "true");
#pragma warning restore CS8602
                    helper.GameContent.InvalidateCache("Characters\\Dialogue\\" + __instance.Name);
                    helper.GameContent.Load<Dictionary<string, string>>("Characters\\Dialogue\\" + __instance.Name);
                }

                if (Game1.player.spouse != null && Game1.player.spouse != __instance.Name && Polyamory.GetSpouses(who, true).Count != 0 && Polyamory.GetSpouses(who, true).ContainsKey(__instance.Name))
                {
                    __state = who.spouse;
                    who.spouse = __instance.Name;
                }

                Object activeObj = who.ActiveObject;
                bool canReceiveGifts = __instance.CanReceiveGifts();
                Dialogue pendantReject = __instance.TryGetDialogue("RejectItem_(O)460");
                helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"EnderTedi.Polyamory/PolyamoryData").TryGetValue(__instance.Name, out var data);

                switch (activeObj.QualifiedItemId)
                {
                    case "(O)460":
                        if (pendantReject is not null && canReceiveGifts && !probe && (data is null || data.UsePedantRejectDialogue == true))
                        {
                            monitor.Log("Will use reject dialogue");
                            __instance.setNewDialogue(pendantReject);
                            Game1.drawDialogue(__instance);
                            __result = false;
                        }
                        if (canReceiveGifts)
                        {
                            if (!probe)
                            {
                                monitor.Log($"Try give pendant to {__instance.Name}");
                                if (who.isEngaged())
                                {
                                    monitor.Log($"Tried to give pendant while engaged");

                                    __instance.CurrentDialogue.Push(new Dialogue(__instance, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3965", "3966"), true));
                                    Game1.drawDialogue(__instance);
                                    __result = false;
                                    return false;
                                }
                                if (!__instance.datable.Value || __instance.isMarriedOrEngaged() || (who.friendshipData.ContainsKey(__instance.Name) && who.friendshipData[__instance.Name].Points < 2500f * 0.6f))
                                {
                                    monitor.Log($"Tried to give pendant to someone not datable");

                                    if (Polyamory.random.NextDouble() < 0.5)
                                    {
                                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3969", __instance.displayName));
                                        __result = false;
                                        return false;
                                    }
                                    __instance.CurrentDialogue.Push(new Dialogue(__instance, "Strings\\StringsFromCSFiles:NPC.cs." + ((__instance.Gender == Gender.Female) ? "3970" : "3971"), false));
                                    Game1.drawDialogue(__instance);
                                    __result = false;
                                    return false;
                                }
                                else if (__instance.datable.Value && who.friendshipData.ContainsKey(__instance.Name) && who.friendshipData[__instance.Name].Points < 2500f)
                                {
                                    monitor.Log($"Tried to give pendant to someone not marriable");

                                    if (!who.friendshipData[__instance.Name].ProposalRejected)
                                    {
                                        __instance.CurrentDialogue.Push(new Dialogue(__instance, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3972", "3973"), false));
                                        Game1.drawDialogue(__instance);
                                        who.changeFriendship(-20, __instance);
                                        who.friendshipData[__instance.Name].ProposalRejected = true;
                                        __result = false;
                                        return false;
                                    }
                                    __instance.CurrentDialogue.Push(new Dialogue(__instance, "Strings\\StringsFromCSFiles:NPC.cs." + Game1.random.Choose("3974", "3975"), true));
                                    Game1.drawDialogue(__instance);
                                    who.changeFriendship(-50, __instance);
                                    __result = false;
                                    return false;
                                }
                                else
                                {
                                    monitor.Log($"Tried to give pendant to someone marriable");
                                    if (!__instance.datable.Value || who.HouseUpgradeLevel >= 1 && Polyamory.IsValidEngagement(who, __instance.Name))
                                    {
                                        monitor.Log($"{__instance.Name} is getting married", LogLevel.Warn);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                        typeof(NPC).GetMethod("engagementResponse", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { who, false });
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                                        __result = true;
                                        return false;
                                    }
                                    else if (!Polyamory.IsValidEngagement(who, __instance.Name))
                                    {
                                        if (Polyamory.IsMarriedToNonPolyamorousNPC(who))
                                        {
                                            monitor.Log($"{__instance.Name} can't get married. player married to monogamous npc", LogLevel.Warn);
                                            Dialogue npcDialog = __instance.TryGetDialogue("Marriage_NonPolyamorousNPC");
                                            Dialogue baseDialogue = new(__instance, "Strings\\StringsFromCSFiles:Marriage_NonPolyamorousNPC");
                                            if (npcDialog != null)
                                                __instance.CurrentDialogue.Push(npcDialog);
                                            else
                                                __instance.CurrentDialogue.Push(baseDialogue);
                                            Game1.drawDialogue(__instance);
                                            __result = false;
                                            return false;
                                        }
                                        else if (Polyamory.IsNpcPolyamorous(__instance.Name))
                                        {
                                            monitor.Log($"{__instance.Name} is monogamous NPC", LogLevel.Warn);
                                            Dialogue npcDialog = __instance.TryGetDialogue("Marriage_IsNonPolyamorousNPC");
                                            Dialogue baseDialogue = new(__instance, "Strings\\StringsFromCSFiles:Marriage_IsNonPolyamorousNPC");
                                            if (npcDialog != null)
                                                __instance.CurrentDialogue.Push(npcDialog);
                                            else
                                                __instance.CurrentDialogue.Push(baseDialogue);
                                            Game1.drawDialogue(__instance);
                                            __result = false;
                                            return false;
                                        }
                                    }
                                    monitor.Log($"Can't marry");
                                    if (Polyamory.random.NextDouble() < 0.5)
                                    {
                                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\StringsFromCSFiles:NPC.cs.3969", __instance.displayName));
                                        __result = false;
                                        return false;
                                    }
                                    __instance.CurrentDialogue.Push(new Dialogue(__instance, "Strings\\StringsFromCSFiles:NPC.cs.3972", false));
                                    Game1.drawDialogue(__instance);
                                    __result = false;
                                    return false;
                                }
                            }
                            __result = false;
                        }
                        break;
                }
                
                return true;
            }

            public static void Postfix(ref string __state, Farmer who)
            {
                if (!string.IsNullOrEmpty(__state))
                {
                    who.spouse = __state;
                }
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.tryToRetrieveDialogue))]
        public static class NPCPatch_tryToRetrieveDialogue
        {
            internal static bool Prefix(NPC __instance, ref Dialogue? __result, string appendToEnd)
            {
                try
                {
                    if (appendToEnd.Contains("_inlaw_") && Game1.player.friendshipData.ContainsKey(__instance.Name) && Game1.player.friendshipData[__instance.Name].IsMarried())
                    {
                        __result = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_tryToRetrieveDialogue)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.GetDispositionModifiedString))]
        public static class NPCPatch_GetDispositionModifiedString
        {
            internal static void Prefix(NPC __instance, ref bool __state)
            {
                try
                {
                    if (Game1.player.isMarriedOrRoommates() && Game1.player.friendshipData.ContainsKey(__instance.Name) && Game1.player.friendshipData[__instance.Name].IsMarried() && Game1.player.spouse != __instance.Name)
                    {
                       Polyamory.tempSpouse = __instance;
                        __state = true;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_GetDispositionModifiedString)}:\n{ex}", LogLevel.Error);
                }
            }

            internal static void Postfix(ref bool __state)
            {
                try
                {
                    if (__state)
                    {
                        Polyamory.tempSpouse = null;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_GetDispositionModifiedString)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.marriageDuties))]
        public static class NPCPatch_marriageDuties
        {
            public static void Prefix(NPC __instance)
            {
                try
                {
                    if (Polyamory.GetSpouses(Game1.player, false).ContainsKey(__instance.Name))
                    {
                        Polyamory.tempSpouse = __instance;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_marriageDuties)}:\n{ex}", LogLevel.Error);
                }
            }

            public static void Postfix(NPC __instance)
            {
                try
                {
                    if (Polyamory.tempSpouse == __instance)
                    {
                        Polyamory.tempSpouse = null;
                    }
                    return;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_marriageDuties)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(NPC), "engagementResponse")]
        public static class NPCPatch_engagementResponse
        {
            public static void Postfix(Farmer who)
            {
                Polyamory.ResetSpouses(who);
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.spouseObstacleCheck))]
        public static class NPCPatch_spouseObstacleCheck
        {
            public static bool Prefix(NPC __instance, GameLocation currentLocation, ref bool __result)
            {
                if (currentLocation is not FarmHouse)
                    return true;
                if (NPC.checkTileOccupancyForSpouse(currentLocation, __instance.Tile, __instance.Name))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    Game1.warpCharacter(__instance, __instance.DefaultMap, (Game1.getLocationFromName(__instance.DefaultMap) as FarmHouse).getSpouseBedSpot(__instance.Name));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    __instance.faceDirection(1);
                    __result = true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.isRoommate))]
        public static class NPCPatch_isRoommate
        {
            public static bool Prefix(NPC __instance, ref bool __result)
            {
                try
                {

                    if (!__instance.IsVillager)
                    {
                        __result = false;
                        return false;
                    }
                    foreach (Farmer f in Game1.getAllFarmers())
                    {
                        if (f.isRoommate(__instance.Name))
                        {
                            __result = true;
                            return false;
                        }
                    }
                    __result = false;
                    return false;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_isRoommate)}:\n{ex}", LogLevel.Error);
                    return true; // run original logic
                }
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.getSpouse))]
        public static class NPCPatch_getSpouse
        {
            public static bool Prefix(NPC __instance, ref Farmer __result)
            {
                foreach (Farmer f in Game1.getAllFarmers())
                {
                    if (f.friendshipData.ContainsKey(__instance.Name) && f.friendshipData[__instance.Name].IsMarried())
                    {
                        __result = f;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.isMarried))]
        public static class NPCPatch_isMarried
        {
            public static bool Prefix(NPC __instance, ref bool __result)
            {
                __result = false;
                if (!__instance.IsVillager)
                {
                    return false;
                }
                foreach (Farmer f in Game1.getAllFarmers())
                {
                    if (f.friendshipData.ContainsKey(__instance.Name) && f.friendshipData[__instance.Name].IsMarried())
                    {
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.isMarriedOrEngaged))]
        public static class NPCPatch_isMarriedOrEngaged
        {
            public static bool Prefix(NPC __instance, ref bool __result)
            {
                __result = false;
                if (!__instance.IsVillager)
                {
                    return false;
                }
                foreach (Farmer f in Game1.getAllFarmers())
                {
                    if (f.friendshipData.ContainsKey(__instance.Name) && (f.friendshipData[__instance.Name].IsMarried() || f.friendshipData[__instance.Name].IsEngaged()))
                    {
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), "loadCurrentDialogue")]
        public static class NPCPatch_loadCurrentDialogue
        {
            internal static void Prefix(NPC __instance, ref string? __state)
            {
                
                try
                {
                    IDictionary npcExtData = Game1.content.Load<IDictionary>("spacechase0.SpaceCore/NpcExtensionData");
                    if (npcExtData is not null && npcExtData.Contains($"{__instance.Name}") && Polyamory.GetSpouses(Game1.player, true).ContainsKey(__instance.Name))
                    {
                        var entry = npcExtData[__instance.Name] as dynamic;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        if (entry.IgnoreMarriageSchedule)
                        {
                            monitor.Log($"{__instance.Name}, {(bool)entry.IgnoreMarriageSchedule}", LogLevel.Alert);
                            __state = null;
                            if (Game1.player.spouse == __instance.Name)
                            {
                                __state = Game1.player.spouse;
                                Game1.player.spouse = "";
                                return;
                            }
                        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    }
                    if (Polyamory.GetSpouses(Game1.player, false).ContainsKey(__instance.Name))
                    {
                        __state = Game1.player.spouse;
                        Game1.player.spouse = __instance.Name;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_loadCurrentDialogue)}:\n{ex}", LogLevel.Error);
                }
            }


            public static void Postfix(string __state)
            {
                try
                {
                    if (__state != null)
                    {
                        Game1.player.spouse = __state;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_loadCurrentDialogue)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
        public static class NPCPatch_checkAction
        {
            public static bool Prefix(NPC __instance, ref bool __result, Farmer who)
            {
                if (__instance.IsInvisible || __instance.isSleeping.Value || !who.canMove || who.checkForQuestComplete(__instance, -1, -1, who.ActiveObject, null, -1, 5) || (who.pantsItem.Value?.ParentSheetIndex == 15 && (__instance.Name.Equals("Lewis") || __instance.Name.Equals("Marnie"))) || (__instance.Name.Equals("Krobus") && who.hasQuest("28")))
                    return true;

                try
                {
                    Polyamory.ResetSpouses(who);

                    if ((__instance.Name.Equals(who.spouse) || Polyamory.GetSpouses(who, true).ContainsKey(__instance.Name)) && __instance.Sprite.CurrentAnimation == null && who.IsLocalPlayer)
                    {
                        monitor.Log($"{__instance.Name} is married to {who.Name}");

                        __instance.faceDirection(-3);

                        if (who.friendshipData.ContainsKey(__instance.Name) && who.friendshipData[__instance.Name].Points >= 3125 && who.mailReceived.Add("CF_Spouse"))
                        {
                            monitor.Log($"getting starfruit");
                            __instance.CurrentDialogue.Push(new Dialogue(__instance, Game1.player.isRoommate(who.spouse) ? "Strings\\StringsFromCSFiles:Krobus_Stardrop" : "Strings\\StringsFromCSFiles:NPC.cs.4001", false));
                            Object stardrop = ItemRegistry.Create<Object>("(O)434", 1, 0, false);
                            stardrop.CanBeSetDown = false;
                            stardrop.CanBeGrabbed = false;
                            Game1.player.addItemByMenuIfNecessary(stardrop, null);
                            __instance.shouldSayMarriageDialogue.Value = false;
                            __instance.currentMarriageDialogue.Clear();
                            __result = true;
                            return false;
                        }
                        if (__instance.Sprite.CurrentAnimation == null && !__instance.hasTemporaryMessageAvailable() && __instance.currentMarriageDialogue.Count == 0 && __instance.CurrentDialogue.Count == 0 && Game1.timeOfDay < 2200 && !__instance.isMoving() && who.ActiveObject == null /*&& Polyamory.kissingAPI == null*/)
                        {
                            monitor.Log($"Trying to kiss/hug {__instance.Name}");

                            __instance.faceGeneralDirection(who.getStandingPosition(), 0, false);
                            who.faceGeneralDirection(__instance.getStandingPosition(), 0, false);
                            if (__instance.FacingDirection == 3 || __instance.FacingDirection == 1)
                            {

                                if (__instance.hasBeenKissedToday.Value)
                                {
                                    monitor.Log($"{__instance.Name} has been kissed today");
                                    return true;
                                }

                                int spouseFrame = __instance.GetData().KissSpriteIndex;
                                bool facingRight = __instance.GetData().KissSpriteFacingRight;
                                string name = __instance.Name;
                                bool flip = (facingRight && __instance.FacingDirection == 3) || (!facingRight && __instance.FacingDirection == 1);
                                if (who.getFriendshipHeartLevelForNPC(__instance.Name) >= 9)
                                {
                                    monitor.Log($"Can kiss/hug {__instance.Name}");

                                    int delay = Game1.IsMultiplayer ? 1000 : 10;
                                    __instance.movementPause = delay;
                                    __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                                {
                                    new (spouseFrame, delay, false, flip, new AnimatedSprite.endOfAnimationBehavior(__instance.haltMe), true)
                                });
                                    if (!__instance.hasBeenKissedToday.Value)
                                    {
                                        who.changeFriendship(10, __instance);
                                        if (who.friendshipData[__instance.Name].RoommateMarriage)
                                        {
                                            monitor.Log($"Hugging {__instance.Name}");
                                            Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite[]
                                            {
                                            new("LooseSprites\\emojis", new Rectangle(0, 0, 9, 9), 2000f, 1, 0, new Vector2(__instance.Tile.X, __instance.Tile.Y) * 64f + new Vector2(16f, -64f), false, false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f, false)
                                            {
                                                motion = new Vector2(0f, -0.5f),
                                                alphaFade = 0.01f
                                            }
                                            });
                                        }
                                        else
                                        {
                                            monitor.Log($"Kissing {__instance.Name}");
                                            Game1.Multiplayer.broadcastSprites(who.currentLocation, new TemporaryAnimatedSprite[]
                                            {
                                            new("LooseSprites\\Cursors", new Rectangle(211, 428, 7, 6), 2000f, 1, 0, new Vector2(__instance.Tile.X, __instance.Tile.Y) * 64f + new Vector2(16f, -64f), false, false, 1f, 0f, Color.White, 4f, 0f, 0f, 0f, false)
                                            {
                                                motion = new Vector2(0f, -0.5f),
                                                alphaFade = 0.01f
                                            }
                                            });
                                        }
                                        __instance.currentLocation.playSound("dwop", null, null, SoundContext.NPC);
                                        who.exhausted.Value = false;

                                    }
                                    __instance.hasBeenKissedToday.Value = true;
                                    __instance.Sprite.UpdateSourceRect();
                                }
                                else
                                {
                                    monitor.Log($"Kiss/hug rejected by {__instance.Name}");

                                    __instance.faceDirection((Polyamory.random.NextDouble() < 0.5) ? 2 : 0);
                                    __instance.doEmote(12, true);
                                }
                                int playerFaceDirection = 1;
                                if ((facingRight && !flip) || (!facingRight && flip))
                                {
                                    playerFaceDirection = 3;
                                }
                                who.PerformKiss(playerFaceDirection);
                                __result = true;
                                return false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_checkAction)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.playSleepingAnimation))]
        public static class NPCPatch_playSleepingAntimation
        {
            public static bool Prefix(NPC __instance, bool ___isPlayingSleepingAnimation)
            {
                try
                {
                    if (___isPlayingSleepingAnimation)
                        return true;
                    Dictionary<string, string> animationDescriptions = Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions");
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    if (animationDescriptions.TryGetValue(__instance.Name.ToLower() + "_sleep", out string sleepString) && !int.TryParse(sleepString.Split('/')[0], out int sleep_frame))
                        return false;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_playSleepingAntimation)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }

            public static void Postfix(NPC __instance)
            {
                try
                {
                    Dictionary<string, string> animationDescriptions = Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions");
                    if (!animationDescriptions.ContainsKey(__instance.Name.ToLower() + "_sleep") && animationDescriptions.ContainsKey(__instance.Name + "_Sleep"))
                    {
                        if (int.TryParse(animationDescriptions[__instance.Name + "_Sleep"].Split('/')[0], out int sleep_frame))
                        {
                            __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
                        {
                            new(sleep_frame, 100, false, false, null, false)
                        });
                            __instance.Sprite.loop = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_playSleepingAntimation)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.displayName), MethodType.Getter)]
        public static class NPCPatch_displayName
        {
            public static void Postfix(Character __instance, ref string __result)
            {
                try
                {
                    if (__instance.Name is null || __instance is not Child || !config.ShowParentNames || !__instance.modData.ContainsKey("EnderTedi.Polyamory/OtherParent"))
                        return;
                    __result = $"{__result} ({__instance.modData["EnderTedi.Polyamory/OtherParent"]})";
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(NPCPatch_displayName)}:\n{ex}", LogLevel.Error);
                }
            }
        }
    }
}
