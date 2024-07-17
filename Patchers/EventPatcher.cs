using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace Polyamory.Patchers
{
    internal class EventPatcher
    {
#pragma warning disable CS8618
        public static IModHelper helper;
        public static IMonitor monitor;
#pragma warning restore CS8618
        public static bool startingLoadActors = false;

        public static void Initialize(IModHelper Helper, IMonitor Monitor)
        {
            helper = Helper;
            monitor = Monitor;
        }

        [HarmonyPatch(typeof(Event), nameof(Event.answerDialogueQuestion))]
        public static class EventPatch_answerDialogueQuestion
        {
            public static bool Prefix(Event __instance, NPC who, string answerKey)
            {
                try
                {

                    if (answerKey == "danceAsk" && !who.HasPartnerForDance && Game1.player.friendshipData[who.Name].IsMarried())
                    {
                        string accept = "";
                        if (who.Gender != Gender.Male)
                        {
                            if (who.Gender == Gender.Female)
                            {
                                accept = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1634");
                            }
                        }
                        else
                        {
                            accept = Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1633");
                        }
                        try
                        {
                            Game1.player.changeFriendship(250, Game1.getCharacterFromName(who.Name, true));
                        }
                        catch
                        {
                        }
                        Game1.player.dancePartner.Value = who;
                        who.setNewDialogue(accept, false, false);
                        using (List<NPC>.Enumerator enumerator = __instance.actors.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                NPC j = enumerator.Current;
                                if (j.CurrentDialogue != null && j.CurrentDialogue.Count > 0 && j.CurrentDialogue.Peek().getCurrentDialogue().Equals("..."))
                                {
                                    j.CurrentDialogue.Clear();
                                }
                            }
                        }
                        Game1.drawDialogue(who);
                        who.immediateSpeak = true;
                        who.facePlayer(Game1.player);
                        who.Halt();
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

        [HarmonyPatch(typeof(Event.DefaultCommands), nameof(Event.DefaultCommands.LoadActors))]
        public static class EventPatch_DefaultCommands_LoadActors
        {
            public static void Prefix()
            {
                try
                {
                    startingLoadActors = true;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Prefix)}:\n{ex}", LogLevel.Error);
                }
            }

            public static void Postfix()
            {
                try
                {
                    startingLoadActors = false;
                    Game1Patcher.lastGotCharacter = null;

                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(Postfix)}:\n{ex}", LogLevel.Error);
                }
            }
        }
    }
}

