using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polyamory.Patchers
{
    internal class Divorce
    {
#pragma warning disable CS8618
        private static IModHelper helper;
        private static IMonitor monitor;
        private static ModConfig config;
#pragma warning restore CS8618
        private static string? complexDivorceSpouse;

        internal static void Initialize(IModHelper Helper, IMonitor Monitor, ModConfig Config)
        {
            helper = Helper;
            monitor = Monitor;
            config = Config;
        }

        public static void AfterDialogueBehavior(Farmer who, string whichAnswer)
        {
#if !RELEASE
            monitor.Log("answer " + whichAnswer);
#endif
            if (Polyamory.GetSpouses(who, true).ContainsKey(whichAnswer))
            {
#if !RELEASE
                monitor.Log("divorcing " + whichAnswer);
#endif
                string s2 = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Question_" + whichAnswer, whichAnswer);
                if (s2 == null || s2 == "Strings\\Locations:ManorHouse_DivorceBook_Question_" + whichAnswer)
                {
                    s2 = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Question");
                }
                List<Response> responses = new()
                    {
                        new Response($"divorce_Yes_{whichAnswer}", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes"))
                    };

                if (config.ComplexDivorce)
                {
                    responses.Add(new Response($"divorce_complex_{whichAnswer}", helper.Translation.Get("divorce_complex")));
                }

                responses.Add(new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No")));
                (Game1.activeClickableMenu as DialogueBox)?.closeDialogue();
                Game1.currentLocation.createQuestionDialogue(s2, responses.ToArray(), "PolyamoryDivorce");
            }
            else if (whichAnswer.StartsWith("divorce_Yes_"))
            {
#if !RELEASE
                monitor.Log("confirmed " + whichAnswer);
#endif
                string spouse = whichAnswer.Split('_')[2];
                if (Game1.player.Money >= 50000 || spouse == "Krobus")
                {
#if !RELEASE
                    monitor.Log("divorce initiated successfully");
#endif
                    if (!Game1.player.isRoommate(spouse))
                    {
                        Game1.player.Money -= 50000;
                        Polyamory.divorceHeartsLost = config.PreventHostileDivorces ? 0 : -1;
                    }
                    else
                    {
                        Polyamory.divorceHeartsLost = 0;
                    }
                    Polyamory.spouseToDivorce = spouse;
                    Game1.player.divorceTonight.Value = true;
                    string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Filed_" + spouse, spouse);
                    if (s == "Strings\\Locations:ManorHouse_DivorceBook_Filed_" + spouse)
                    {
                        s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Filed");
                    }
                    Game1.drawObjectDialogue(s);
                    if (!Game1.player.isRoommate(spouse))
                    {
                        Game1.Multiplayer.globalChatInfoMessage("Divorce", new string[]
                        {
                            Game1.player.Name
                        });
                    }
                }
                else
                {
#if !RELEASE
                    monitor.Log("not enough money to divorce");
#endif
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
                }
            }
            else if (whichAnswer.StartsWith("divorce_complex_"))
            {
                complexDivorceSpouse = whichAnswer.Replace("divorce_complex_", "");
                Polyamory.divorceHeartsLost = 1;
                ShowNextDialogue("divorce_fault_", Game1.currentLocation);
            }
            else if (whichAnswer.StartsWith("divorce_fault_"))
            {
#if !RELEASE
                monitor.Log("divorce fault");
#endif
                string r = helper.Translation.Get(whichAnswer);
                if (r != null)
                {
                    if (int.TryParse(r.Split('#')[^1], out int lost))
                    {
                        Polyamory.divorceHeartsLost += lost;
                    }
                }
                string nextKey = $"divorce_{r?.Split('#')[^2]}reason_";
                Translation test = helper.Translation.Get(nextKey + "q");
                if (!test.HasValue())
                {
                    ShowNextDialogue($"divorce_method_", Game1.currentLocation);
                    return;
                }
                ShowNextDialogue($"divorce_{r?.Split('#')[^2]}reason_", Game1.currentLocation);
            }
            else if (whichAnswer.Contains("reason_"))
            {
#if !RELEASE
                monitor.Log("divorce reason");
#endif
                string r = helper.Translation.Get(whichAnswer);
                if (r != null)
                {
                    if (int.TryParse(r.Split('#')[^1], out int lost))
                    {
                        Polyamory.divorceHeartsLost += lost;
                    }
                }

                ShowNextDialogue($"divorce_method_", Game1.currentLocation);
            }
            else if (whichAnswer.StartsWith("divorce_method_"))
            {
#if !RELEASE
                monitor.Log("divorce method");
#endif
                Polyamory.spouseToDivorce = complexDivorceSpouse;
                string r = helper.Translation.Get(whichAnswer);
                if (r != null)
                {
                    if (int.TryParse(r.Split('#')[^1], out int lost))
                    {
                        Polyamory.divorceHeartsLost += lost;
                    }
                }

                if (Game1.player.Money >= 50000 || complexDivorceSpouse == "Krobus")
                {
                    if (!Game1.player.isRoommate(complexDivorceSpouse))
                    {
                        int money = 50000;
                        if (int.TryParse(r?.Split('#')[^2], out int mult))
                        {
                            money = (int)Math.Round(money * mult / 100f);
                        }
#if !RELEASE
                        monitor.Log($"money cost {money}");
#endif
                        Game1.player.Money -= 50000;
                    }
                    Game1.player.divorceTonight.Value = true;
                    string s = Game1.content.LoadString("Strings\\Locations:ManorHouse_DivorceBook_Filed_" + complexDivorceSpouse, complexDivorceSpouse);
                    if (s == null || s == "Strings\\Locations:ManorHouse_DivorceBook_Filed_" + complexDivorceSpouse)
                    {
                        s = Game1.content.LoadStringReturnNullIfNotFound("Strings\\Locations:ManorHouse_DivorceBook_Filed");
                    }
                    Game1.drawObjectDialogue(s);
                    if (!Game1.player.isRoommate(complexDivorceSpouse))
                    {
                        Game1.Multiplayer.globalChatInfoMessage("Divorce", new string[]
                        {
                                    Game1.player.Name
                        });
                    }
#if !RELEASE
                    monitor.Log($"hearts lost {Polyamory.divorceHeartsLost}");
#endif
                }
                else
                {
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\UI:NotEnoughMoney1"));
                }
            }
        }

        private static void ShowNextDialogue(string key, GameLocation l)
        {
            (Game1.activeClickableMenu as DialogueBox)?.closeDialogue();
            Translation s2 = helper.Translation.Get($"{key}q");
            if (!s2.HasValue())
            {
#if !RELEASE
                monitor.Log("no dialogue: " + s2.ToString(), LogLevel.Error);
#endif
                return;
            }
#if !RELEASE
            monitor.Log("has dialogue: " + s2.ToString());
#endif
            List<Response> responses = new();
            int i = 1;
            while (true)
            {
                Translation r = helper.Translation.Get($"{key}{i}");
                if (!r.HasValue())
                    break;
                string str = r.ToString().Split('#')[0];
#if !RELEASE
                monitor.Log(str);
#endif
                responses.Add(new Response(key + i, str));
                i++;
            }
#if !RELEASE
            monitor.Log("next question: " + s2.ToString());
#endif
            Game1.currentLocation.lastQuestionKey = "";
            Game1.isQuestion = true;
            Game1.dialogueUp = true;
            l.createQuestionDialogue(s2, responses.ToArray(), "PolyamoryDivorce");
        }
    }
}
