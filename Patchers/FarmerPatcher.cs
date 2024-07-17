using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace Polyamory.Patchers
{
    internal class FarmerPatcher
    {

#pragma warning disable CS8618
        public static IModHelper helper;
        public static IMonitor monitor;
#pragma warning restore CS8618
        public static bool skipSpouse = false;

        public static void Initialize(IModHelper Helper, IMonitor Monitor)
        {
            helper = Helper;
            monitor = Monitor;
        }

        public static class FarmerPatch_spouse
        {
            public static void Postfix(Farmer __instance, ref string __result)
            {
                if (skipSpouse)
                    return;
                try
                {
                    skipSpouse = true;
                    if (Polyamory.tempSpouse != null && __instance.friendshipData.ContainsKey(Polyamory.tempSpouse.Name) && __instance.friendshipData[Polyamory.tempSpouse.Name].IsMarried())
                    {
                        __result = Polyamory.tempSpouse.Name;
                    }
                    else
                    {
                        var spouses = Polyamory.GetSpouses(__instance, true);
                        string? aspouse = null;
                        foreach (var spouse in spouses)
                        {
                            aspouse ??= spouse.Key;
                            if (__instance.friendshipData.TryGetValue(spouse.Key, out var f) && f.IsEngaged())
                            {
                                __result = spouse.Key;
                                break;
                            }
                        }
                        if (__result is null && aspouse is not null)
                        {
                            __result = aspouse;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Postfix)}:\n{ex}", LogLevel.Error);
                }
                skipSpouse = false;
            }
        }

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.getSpouse))]
        public static class FarmerPatch_getSpouse
        {
            public static void Prefix(Farmer __instance, ref NPC __result)
            {
                try
                {

                    if (Polyamory.tempSpouse != null && __instance.friendshipData.ContainsKey(Polyamory.tempSpouse.Name) && __instance.friendshipData[Polyamory.tempSpouse.Name].IsMarried())
                    {
                        __result = Polyamory.tempSpouse;
                    }
                    else
                    {
                        var spouses = Polyamory.GetSpouses(__instance, true);
                        NPC? aspouse = null;
                        foreach (var spouse in spouses)
                        {
                            aspouse ??= spouse.Value;
                            if (__instance.friendshipData[spouse.Key].IsEngaged())
                            {
                                __result = spouse.Value;
                                break;
                            }
                        }
                        if (__result is null && aspouse is not null)
                        {
                            __result = aspouse;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.isMarriedOrRoommates))]
        public static class FarmerPatch_isMarriedOrRoommates
        {
            public static bool Prefix(Farmer __instance, ref bool __result)
            {
                try
                {
                    __result = __instance.team.IsMarried(__instance.UniqueMultiplayerID) || Polyamory.GetSpouses(__instance, true).Count > 0;
                    return false;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.checkAction))]
        public static class FarmerPatch_checkAction
        {
            public static bool Prefix(Farmer __instance, Farmer who, GameLocation location, ref bool __result)
            {
                try
                {
                    if (who.isRidingHorse())
                    {
                        who.Halt();
                    }
                    if (__instance.hidden.Value)
                    {
                        return true;
                    }
                    if (Game1.CurrentEvent == null && who.CurrentItem != null && who.CurrentItem.ParentSheetIndex == 801 && !__instance.isEngaged() && !who.isEngaged())
                    {
                        if (!Polyamory.IsMarriedToNonPolyamorousNPC(__instance) && !Polyamory.IsMarriedToNonPolyamorousNPC(who))
                        {
                            who.Halt();
                            who.faceGeneralDirection(__instance.getStandingPosition(), 0, false);
                            string question2 = Game1.content.LoadString("Strings\\UI:AskToMarry_" + (__instance.IsMale ? "Male" : "Female"), __instance.Name);
                            location.createQuestionDialogue(question2, location.createYesNoResponses(), delegate (Farmer _, string answer)
                            {
                                if (answer == "Yes")
                                {
                                    who.team.SendProposal(__instance, ProposalType.Marriage, who.CurrentItem.getOne());
                                    Game1.activeClickableMenu = new PendingProposalDialog();
                                }
                            }, null);
                            __result = true;
                            return false;
                        }
                        else
                        {
                            Game1.drawDialogueBox(I18n.PlayerMarriage_MarriedToNonPolyamorousNPC());
                            __result = false;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.GetSpouseFriendship))]
        public static class FarmerPatch_getSpouseFriendship
        {
            public static bool Prefix(Farmer __instance, ref Friendship __result)
            {
                try
                {

                    if (Polyamory.tempSpouse != null && __instance.friendshipData.ContainsKey(Polyamory.tempSpouse.Name) && __instance.friendshipData[Polyamory.tempSpouse.Name].IsMarried())
                    {
                        __result = __instance.friendshipData[Polyamory.tempSpouse.Name];
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.getChildren))]
        public static class FarmerPatch_getChildren
        {
            public static bool Prefix(Farmer __instance, ref List<Child> __result)
            {
                try
                {

                    if (EventPatcher.startingLoadActors && Environment.StackTrace.Contains("command_loadActors") && !Environment.StackTrace.Contains("addActor") && !Environment.StackTrace.Contains("Dialogue") && !Environment.StackTrace.Contains("checkForSpecialCharacters") && Game1Patcher.lastGotCharacter != null && __instance != null)
                    {
                        __result = Utility.getHomeOfFarmer(__instance)?.getChildren()?.FindAll(c => c.displayName.EndsWith($"({Game1Patcher.lastGotCharacter})")) ?? new List<Child>();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.doDivorce))]
        public static class FarmerPatch_doDivorce
        {
            public static bool Prefix(ref Farmer __instance)
            {
                try
                {
                    monitor.Log("Trying to divorce");
                    __instance.divorceTonight.Value = false;
                    if (!__instance.isMarriedOrRoommates() || Polyamory.spouseToDivorce == null)
                    {
                        monitor.Log("Tried to divorce but no spouse to divorce!");
                        return false;
                    }

                    string key = Polyamory.spouseToDivorce;

                    int points = 2000;
                    if (Polyamory.divorceHeartsLost < 0)
                    {
                        points = 0;
                    }
                    else
                    {
                        points -= Polyamory.divorceHeartsLost * 250;
                    }

                    if (__instance.friendshipData.ContainsKey(key))
                    {
                        monitor.Log($"Divorcing {key}");
                        __instance.friendshipData[key].Points = Math.Min(2000, Math.Max(0, points));
                        monitor.Log($"Resulting points: {__instance.friendshipData[key].Points}");

                        __instance.friendshipData[key].Status = points < 1000 ? FriendshipStatus.Divorced : FriendshipStatus.Friendly;
                        monitor.Log($"Resulting friendship status: {__instance.friendshipData[key].Status}");

                        __instance.friendshipData[key].RoommateMarriage = false;

                        NPC ex = Game1.getCharacterFromName(key);
                        ex.PerformDivorce();
                        if (__instance.spouse == key)
                        {
                            __instance.spouse = null;
                        }
                        Polyamory.Spouses.Remove(__instance.UniqueMultiplayerID);
                        Polyamory.UnofficialSpouses.Remove(__instance.UniqueMultiplayerID);
                        Polyamory.ResetSpouses(__instance);
                        helper.GameContent.InvalidateCache("Maps/FarmHouse1_marriage");
                        helper.GameContent.InvalidateCache("Maps/FarmHouse2_marriage");

                        monitor.Log($"New spouse: {__instance.spouse}, married {__instance.isMarriedOrRoommates()}");

                        Utility.getHomeOfFarmer(__instance).showSpouseRoom();
                        Utility.getHomeOfFarmer(__instance).setWallpapers();
                        Utility.getHomeOfFarmer(__instance).setFloors();

                        Game1.getFarm().addSpouseOutdoorArea(__instance.spouse ?? "");
                    }

                    Polyamory.spouseToDivorce = null;
                    return false;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }
    }
}
