using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Menus;

namespace Polyamory.Patchers
{
    internal class Pregnancy
    {

#pragma warning disable CS8618
        private static IMonitor monitor;
        private static ModConfig config;
#pragma warning restore CS8618
        public static NPC? lastPregnantSpouse;
        public static NPC? lastBirthingSpouse;

        internal static void Initialize(IMonitor Monitor, ModConfig Config)
        {
            monitor = Monitor;
            config = Config;
        }

        public static void AnswerPregnancyQuestion(Farmer who, string answer)
        {
            if (answer == "Yes" && who is not null && lastPregnantSpouse is not null && who.friendshipData.ContainsKey(lastPregnantSpouse.Name))
            {
                WorldDate birthingDate = new(Game1.Date);
                birthingDate.TotalDays += 14;
                who.friendshipData[lastPregnantSpouse.Name].NextBirthingDate = birthingDate;
            }
        }

        [HarmonyPatch(typeof(Utility), nameof(Utility.pickPersonalFarmEvent))]
        public static class UtilityPatch_pickPersonalFarmEvent
        {
            public static bool Prefix(ref FarmEvent? __result)
            {
                monitor.Log("picking event");
                if (Game1.weddingToday)
                {
                    __result = null;
                    return false;
                }



                List<NPC> allSpouses = Polyamory.GetSpouses(Game1.player, true).Values.ToList();

                Polyamory.ShuffleList(ref allSpouses);

                foreach (NPC spouse in allSpouses)
                {
                    if (spouse == null)
                    {
                        monitor.Log($"Utility_pickPersonalFarmEvent_Prefix spouse is null");
                        continue;
                    }
                    Farmer f = spouse.getSpouse();

                    Friendship friendship = f.friendshipData[spouse.Name];

                    if (friendship.DaysUntilBirthing <= 0 && friendship.NextBirthingDate != null)
                    {
                        lastPregnantSpouse = null;
                        lastBirthingSpouse = spouse;
                        __result = new BirthingEvent();
                        return false;
                    }
                }

                /*if (plannedParenthoodAPI is not null && plannedParenthoodAPI.GetPartnerTonight() is not null)
                {
                    SMonitor.Log($"Handing farm sleep event off to Planned Parenthood");
                    return true;
                }*/

                lastBirthingSpouse = null;
                lastPregnantSpouse = null;

                foreach (NPC spouse in allSpouses)
                {
                    if (spouse == null)
                        continue;
                    Farmer f = spouse.getSpouse();
                    if (f.friendshipData[spouse.Name].RoommateMarriage)
                        continue;

                    int heartsWithSpouse = f.getFriendshipHeartLevelForNPC(spouse.Name);
                    Friendship friendship = f.friendshipData[spouse.Name];
                    List<Child> kids = f.getChildren();
                    //int maxChildren = childrenAPI == null ? config.MaxChildren : childrenAPI.GetMaxChildren();
                    int maxChildren = config.MaxChildren;
                    FarmHouse fh = Utility.getHomeOfFarmer(f);
                    bool can = spouse.daysAfterLastBirth <= 0 && fh.cribStyle.Value > 0 && fh.upgradeLevel >= 2 && friendship.DaysUntilBirthing < 0 && heartsWithSpouse >= 10 && friendship.DaysMarried >= 7 && kids.Count < maxChildren;
                    monitor.Log($"Checking ability to get pregnant: {spouse.Name} {can}:{(fh.cribStyle.Value > 0 ? $" no crib" : "")}{(Utility.getHomeOfFarmer(f).upgradeLevel < 2 ? $" house level too low {Utility.getHomeOfFarmer(f).upgradeLevel}" : "")}{(friendship.DaysMarried < 7 ? $", not married long enough {friendship.DaysMarried}" : "")}{(friendship.DaysUntilBirthing >= 0 ? $", already pregnant (gives birth in: {friendship.DaysUntilBirthing})" : "")}");
                    if (can && Game1.player.currentLocation == Game1.getLocationFromName(Game1.player.homeLocation.Value) && Polyamory.random.NextDouble() < 0.05)
                    {
                        monitor.Log("Requesting a baby!");
                        lastPregnantSpouse = spouse;
                        __result = new QuestionEvent(1);
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(QuestionEvent), nameof(QuestionEvent.setUp))]
        public static class QuestionEventPatch_setUp
        {
            public static bool Prefix(int ___whichQuestion, ref bool __result)
            {
                if (___whichQuestion == 1)
                {
                    if (lastPregnantSpouse == null)
                    {
                        __result = true;
                        return false;
                    }
                    Response[] answers = new Response[]
                    {
                    new("Yes", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_Yes")),
                    new("Not", Game1.content.LoadString("Strings\\Events:HaveBabyAnswer_No"))
                    };

                    if (lastPregnantSpouse.Gender == Game1.player.Gender)
                    {
                        Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Events:HavePlayerBabyQuestion", lastPregnantSpouse.Name), answers, new GameLocation.afterQuestionBehavior(AnswerPregnancyQuestion), lastPregnantSpouse);
                    }
                    else
                    {
                        Game1.currentLocation.createQuestionDialogue(Game1.content.LoadString("Strings\\Events:HavePlayerBabyQuestion_Adoption", lastPregnantSpouse.Name), answers, new GameLocation.afterQuestionBehavior(AnswerPregnancyQuestion), lastPregnantSpouse);
                    }
                    Game1.messagePause = true;
                    __result = false;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(BirthingEvent), nameof(BirthingEvent.tickUpdate))]
        public static class BirthingEventPatch_tickUpdate
        {
            public static bool Prefix(GameTime time, BirthingEvent __instance, ref bool __result, ref int ___timer, ref bool ___naming, bool ___getBabyName, bool ___isMale, string ___babyName)
            {
                if (!___getBabyName)
                    return true;

                Game1.player.CanMove = false;
                ___timer += time.ElapsedGameTime.Milliseconds;
                Game1.fadeToBlackAlpha = 1f;

                if (!___naming)
                {
                    Game1.activeClickableMenu = new NamingMenu(new NamingMenu.doneNamingBehavior(__instance.returnBabyName), Game1.content.LoadString(___isMale ? "Strings\\Events:BabyNamingTitle_Male" : "Strings\\Events:BabyNamingTitle_Female"), "");
                    ___naming = true;
                }
                if (___babyName != null && ___babyName != "" && ___babyName.Length > 0)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    double chance = lastBirthingSpouse.Name.Equals("Maru") || lastBirthingSpouse.Name.Equals("Krobus") ? 0.5 : 0.0;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    chance += Game1.player.hasDarkSkin() ? 0.5 : 0.0;
                    bool isDarkSkinned = new Random((int)Game1.uniqueIDForThisGame + (int)Game1.stats.DaysPlayed).NextDouble() < chance;
                    string newBabyName = ___babyName;
                    List<NPC> all_characters = Utility.getAllCharacters();
                    bool collision_found = false;
                    do
                    {
                        collision_found = false;
#pragma warning disable IDE0063
                        using (List<NPC>.Enumerator enumerator = all_characters.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                if (enumerator.Current.Name.Equals(newBabyName))
                                {
                                    newBabyName += " ";
                                    collision_found = true;
                                    break;
                                }
                            }
                        }
#pragma warning restore IDE0063
                    }
                    while (collision_found);
                    Child baby = new(newBabyName, ___isMale, isDarkSkinned, Game1.player)
                    {
                        Age = 0,
                        Position = new Vector2(16f, 4f) * 64f + new Vector2(0f + Polyamory.random.Next(-64, 48), -24f + Polyamory.random.Next(-24, 24)),
                    };
                    baby.modData["aedenthorn.FreeLove/OtherParent"] = lastBirthingSpouse.Name;

                    Utility.getHomeOfFarmer(Game1.player).characters.Add(baby);
                    Game1.playSound("smallSelect");
                    Game1.getCharacterFromName(lastBirthingSpouse.Name).daysAfterLastBirth = 5;
                    Game1.player.friendshipData[lastBirthingSpouse.Name].NextBirthingDate = null;
                    if (Game1.player.getChildrenCount() == 2)
                    {
                        Game1.getCharacterFromName(lastBirthingSpouse.Name).shouldSayMarriageDialogue.Value = true;
                        Game1.getCharacterFromName(lastBirthingSpouse.Name).currentMarriageDialogue.Insert(0, new MarriageDialogueReference("Data\\ExtraDialogue", "NewChild_SecondChild" + Polyamory.random.Next(1, 3), true, Array.Empty<string>()));
                        Game1.getSteamAchievement("Achievement_FullHouse");
                    }
                    else if (lastBirthingSpouse.Gender == Game1.player.Gender)
                    {
                        Game1.getCharacterFromName(lastBirthingSpouse.Name).currentMarriageDialogue.Insert(0, new MarriageDialogueReference("Data\\ExtraDialogue", "NewChild_Adoption", true, new string[]
                        {
                        ___babyName
                        }));
                    }
                    else
                    {
                        Game1.getCharacterFromName(lastBirthingSpouse.Name).currentMarriageDialogue.Insert(0, new MarriageDialogueReference("Data\\ExtraDialogue", "NewChild_FirstChild", true, new string[]
                        {
                        ___babyName
                        }));
                    }
                    Game1.morningQueue.Enqueue(delegate
                    {
                        Game1.Multiplayer.globalChatInfoMessage("Baby", new string[]
                        {
                        Lexicon.capitalize(Game1.player.Name),
                        Game1.player.spouse,
                        Lexicon.getGenderedChildTerm(___isMale),
                        Lexicon.getPronoun(___isMale),
                        baby.displayName
                        });
                    });
                    if (Game1.keyboardDispatcher != null)
                    {
                        Game1.keyboardDispatcher.Subscriber = null;
                    }
                    Game1.player.Position = Utility.PointToVector2(Utility.getHomeOfFarmer(Game1.player).getBedSpot()) * 64f;
                    Game1.globalFadeToClear(null, 0.02f);
                    lastBirthingSpouse = null;
                    __result = true;
                    return false;
                }
                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(BirthingEvent), nameof(BirthingEvent.setUp))]
        public static class BirthingEventPatch_setUp
        {
            public static bool Prefix(ref bool ___isMale, ref string ___message, ref bool __result)
            {
                if (lastBirthingSpouse == null)
                {
                    __result = true;
                    return false;
                }
                NPC spouse = lastBirthingSpouse;
                Game1.player.CanMove = false;
                ___isMale = Polyamory.random.NextDouble() > 0.5f;
                if (spouse.Gender == Game1.player.Gender)
                {
                    ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_Adoption", Lexicon.getGenderedChildTerm(___isMale));
                }
                else if (spouse.Gender == 0)
                {
                    ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_PlayerMother", Lexicon.getGenderedChildTerm(___isMale));
                }
                else
                {
                    ___message = Game1.content.LoadString("Strings\\Events:BirthMessage_SpouseMother", Lexicon.getGenderedChildTerm(___isMale), spouse.displayName);
                }
                __result = false;
                return false;
            }
        }
    }
}
