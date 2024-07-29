using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using Point = Microsoft.Xna.Framework.Point;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Location = xTile.Dimensions.Location;
using Object = StardewValley.Object;

namespace Polyamory.Patchers
{
    internal class LocationPatcher
    {


#pragma warning disable CS8618
        private static IModHelper helper;
        private static IMonitor monitor;
        private static ModConfig config;
#pragma warning restore CS8618

        internal static void Initialize(IModHelper Helper, IMonitor Monitor, ModConfig Config)
        {
            helper = Helper;
            monitor = Monitor;
            config = Config;
        }

        [HarmonyPatch(typeof(FarmHouse), nameof(FarmHouse.GetSpouseBed))]
        public static class FarmHousePatch_getSpouseBed
        {
            public static void Postfix(FarmHouse __instance, ref BedFurniture __result)
            {
                try
                {

                    if (__result != null)
                        return;
                    __result = __instance.GetBed(BedFurniture.BedType.Double, 0);
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(FarmHousePatch_getSpouseBed)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(FarmHouse), nameof(FarmHouse.getSpouseBedSpot))]
        public static class FarmHousePatch_getSpouseBedSpot
        {
            public static bool Prefix(FarmHouse __instance, string spouseName, ref Point __result)
            {
                try
                {
                    if (spouseName == null)
                        return true;
                    var spouses = Polyamory.GetSpouses(__instance.owner, true);

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    if (!spouses.TryGetValue(spouseName, out NPC spouse) || spouse is null || spouse.isMoving() || !Polyamory.IsInBed(__instance, spouse.GetBoundingBox()))
                        return true;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                    __result = spouse.TilePoint;
                    return false;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(FarmHousePatch_getSpouseBedSpot)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Beach), "resetLocalState")]
        public static class BeachPatch_resetLocalState
        {
            public static void Postfix(Beach __instance)
            {
                try
                {

                    if (config.BuyPendantsAnytime)
                    {
                        helper.Reflection.GetField<NPC>(__instance, "oldMariner").SetValue(new NPC(new AnimatedSprite("Characters\\Mariner", 0, 16, 32), new Vector2(80f, 5f) * 64f, 2, "Old Mariner", null));
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(BeachPatch_resetLocalState)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(Beach), nameof(Beach.checkAction))]
        public static class BeachPatch_checkAction
        {
            public static bool Prefix(Beach __instance, Location tileLocation, Farmer who, ref bool __result, NPC ___oldMariner)
            {
                try
                {
                    if (___oldMariner != null && ___oldMariner.TilePoint.X == tileLocation.X && ___oldMariner.TilePoint.Y == tileLocation.Y)
                    {
                        string playerTerm = Game1.content.LoadString("Strings\\Locations:Beach_Mariner_Player_" + (who.IsMale ? "Male" : "Female"));
                        if (who.hasAFriendWithHeartLevel(10, true) && who.HouseUpgradeLevel == 0)
                        {
                            Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:Beach_Mariner_PlayerNotUpgradedHouse", playerTerm)));
                        }
                        else if (who.hasAFriendWithHeartLevel(10, true))
                        {
                            helper.GameContent.InvalidateCache("Strings\\Locations");
                            helper.GameContent.Load<Dictionary<string, string>>("Strings\\Locations");
                            Response[] answers = new Response[]
                            {
                            new("Buy", Game1.content.LoadString("Strings\\Locations:Beach_Mariner_PlayerBuyItem_AnswerYes")),
                            new("Not", Game1.content.LoadString("Strings\\Locations:Beach_Mariner_PlayerBuyItem_AnswerNo"))
                            };
                            __instance.createQuestionDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:Beach_Mariner_PlayerBuyItem_Question", playerTerm)), answers, "mariner");
                        }
                        else
                        {
                            Game1.drawObjectDialogue(Game1.parseText(Game1.content.LoadString("Strings\\Locations:Beach_Mariner_PlayerNoRelationship", playerTerm)));
                        }
                        __result = true;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(BeachPatch_checkAction)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkEventPrecondition), new Type[] { typeof(string), typeof(bool) })]
        public static class GameLocationPatch_checkEventPrecondition
        {
            public static void Prefix(ref string precondition)
            {
                try
                {
                    if (precondition == null || precondition == "")
                        return;
                    string[] split = precondition.Split('/');
                    if (split.Length == 0)
                        return;
                    if (Game1.player.eventsSeen.Contains(split[0]))
                    {
                        return;
                    }
                    Dictionary<string, NPC> spouses = Polyamory.GetSpouses(Game1.player, true);
                    for (int i = 1; i < split.Length; i++)
                    {
                        if (split[i].Length == 0)
                            continue;

                        if (split[i][0] == 'O')
                        {
#pragma warning disable IDE0057 // Use range operator
                            string name = split[i].Substring(2);
#pragma warning restore IDE0057 // Use range operator
                            if (Game1.player.spouse != name && spouses.ContainsKey(name))
                            {
#if !RELEASE
                                monitor.Log($"Got unofficial spouse requirement for event: {name}, switching event condition to isSpouse O");
#endif
                                split[i] = $"o {name}";
                            }
                        }
                        else if (split[i][0] == 'o')
                        {
#pragma warning disable IDE0057 // Use range operator
                            string name = split[i].Substring(2);
#pragma warning restore IDE0057 // Use range operator
                            if (Game1.player.spouse != name && spouses.ContainsKey(name))
                            {
#if !RELEASE
                                monitor.Log($"Got unofficial spouse barrier to event: {name}, switching event condition to notSpouse o");
#endif
                                split[i] = $"O {name}";
                            }
                        }
                    }
                    precondition = string.Join("/", split);
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(GameLocationPatch_checkEventPrecondition)}:\n{ex}", LogLevel.Error);
                }
            }
        }

        [HarmonyPatch(typeof(ManorHouse), nameof(ManorHouse.performAction))]
        public static class ManorHousePatch_performAction
        {
            public static bool Prefix(ManorHouse __instance, string[] action, Farmer who, ref bool __result)
            {
                try
                {
                    Polyamory.ResetSpouses(who);
                    Dictionary<string, NPC> spouses = Polyamory.GetSpouses(who, true);
                    if (action != null && who.IsLocalPlayer && !Game1.player.divorceTonight.Value && (Game1.player.isMarriedOrRoommates() || spouses.Count > 0))
                    {
                        switch (ArgUtility.Get(action, 0))
                        {
                            case "DivorceBook":
                                string str = helper.Translation.Get("divorce_who");
                                List<Response> responses = new();
                                foreach (NPC spouse in spouses.Values)
                                {
                                    responses.Add(new Response(spouse.Name, spouse.displayName));
                                }
                                responses.Add(new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")));
                                __instance.createQuestionDialogue(str, responses.ToArray(), "PolyamoryDivorce");
                                __result = true;
                                return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(ManorHousePatch_performAction)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ManorHouse), nameof(ManorHouse.answerDialogueAction))]
        public static class ManorHousePatch_answerDialogueAction
        {
            public static bool Prefix(string questionAndAnswer, ref bool __result)
            {
                if (questionAndAnswer.StartsWith("PolyamoryDivorce"))
                {
#pragma warning disable IDE0057 // Use range operator
                    Divorce.AfterDialogueBehavior(Game1.player, questionAndAnswer.Substring("PolyamoryDivorce".Length + 1));
#pragma warning restore IDE0057 // Use range operator
                    __result = true;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.answerDialogueAction))]
        public static class GameLocationPatch_answerDialogueAction
        {
            public static bool Prefix(string questionAndAnswer, ref bool __result)
            {
                if (questionAndAnswer == "mariner_Buy")
                {
                    if (Game1.player.Money < config.PendantPrice)
                    {
                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
                    }
                    else
                    {
                        Game1.player.Money -= config.PendantPrice;
                        Game1.player.addItemByMenuIfNecessary(new Object("(O)460", 1, false, -1, 0)
                        {
                            specialItem = true
                        }, null);
                        if (Game1.activeClickableMenu == null)
                        {
                            Game1.player.holdUpItemThenMessage(new Object("(O)460", 1, false, -1, 0), true);
                        }
                    }
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
