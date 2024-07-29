using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Reflection.Emit;
using System.Reflection;

namespace Polyamory.Patchers
{
    internal class UIPatcher
    {

#pragma warning disable CS8618
        public static IModHelper helper;
        public static IMonitor monitor;
#pragma warning restore CS8618
        public static string? lastGotCharacter = null;

        public static void Initialize(IModHelper Helper, IMonitor Monitor)
        {
            helper = Helper;
            monitor = Monitor;
        }

        [HarmonyPatch(typeof(SocialPage), "drawNPCSlot")]
        public static class SocialPagePatch_drawNPCSlot
        {
            public static void Prefix(SocialPage __instance, int i)
            {
                try
                {
                    SocialPage.SocialEntry entry = __instance.GetSocialEntry(i);
                    if (entry.IsChild)
                    {
                        if (entry.DisplayName.EndsWith(")"))
                        {
                            AccessTools.FieldRefAccess<SocialPage.SocialEntry, string>(entry, "DisplayName") = string.Join(" ", entry.DisplayName.Split(' ').Reverse().Skip(1).Reverse());
                            __instance.SocialEntries[i] = entry;
                        }
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(SocialPagePatch_drawNPCSlot)}:\n{ex}", LogLevel.Error);
                }
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                if (helper.ModRegistry.IsLoaded("SG.Partners"))
                {
#if !RELEASE
                    monitor.Log("Keep Your Partners mod is loaded, not patching social page.");
#endif
                    return codes.AsEnumerable();
                }
                try
                {
                    MethodInfo m_IsMarried = AccessTools.Method(typeof(Farmer), "isMarried", null, null);
                    int index = codes.FindIndex((CodeInstruction c) => c.operand is not null && c.operand is MethodInfo info && info == m_IsMarried);
                    if (index > -1)
                    {
                        codes[index - 1].opcode = OpCodes.Nop;
                        codes[index].opcode = OpCodes.Nop;
                        codes[index + 1].opcode = OpCodes.Nop;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(SocialPagePatch_drawNPCSlot)}:\n{ex}", LogLevel.Error);
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(SocialPage), "drawFarmerSlot")]
        public static class SocialPagePatch_drawFarmerSlot
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                if (helper.ModRegistry.IsLoaded("SG.Partners"))
                {
#if !RELEASE
                    monitor.Log("Keep Your Partners mod is loaded, not patching social page.");
#endif
                    return codes.AsEnumerable();
                }
                try
                {
                    MethodInfo m_IsMarried = AccessTools.Method(typeof(Farmer), "isMarried", null, null);
                    int index = codes.FindIndex((CodeInstruction c) => c.operand is not null && c.operand is MethodInfo info && info == m_IsMarried);
                    if (index > -1)
                    {
                        codes[index - 1].opcode = OpCodes.Nop;
                        codes[index].opcode = OpCodes.Nop;
                        codes[index + 1].opcode = OpCodes.Nop;
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(SocialPagePatch_drawFarmerSlot)}:\n{ex}", LogLevel.Error);
                }
                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(SocialPage.SocialEntry), nameof(SocialPage.SocialEntry.IsMarriedToAnyone))]
        public static class SocialPagePath_SocialEntry_isMarriedToAnyone
        {
            public static bool Prefix(SocialPage.SocialEntry __instance, ref bool __result)
            {
                try
                {
                    foreach (Farmer farmer in Game1.getAllFarmers())
                    {
                        if (farmer.spouse == __instance.InternalName && farmer.friendshipData.TryGetValue(__instance.InternalName, out Friendship friendship) && friendship.IsMarried())
                        {
                            __result = true;
                        }
                    }
                    __result = false;
                    return false;
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(SocialPagePatch_drawNPCSlot)}:\n{ex}", LogLevel.Error);
                }
                return true;
            }
        }

        
        public static class DialogueBoxPatch_Constructor
        {
            public static void Prefix(ref List<string> dialogues)
            {
                try
                {
                    if (dialogues == null || dialogues.Count < 2)
                        return;

                    if (dialogues[1] == Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1826"))
                    {
                        List<string> newDialogues = new()
                    {
                        dialogues[0]
                    };



                        List<NPC> spouses = Polyamory.GetSpouses(Game1.player, true).Values.OrderBy(o => Game1.player.friendshipData[o.Name].Points).Reverse().Take(4).ToList();

                        List<int> which = new() { 0, 1, 2, 3 };

                        Polyamory.ShuffleList(ref which);

                        List<int> myWhich = new List<int>(which).Take(spouses.Count).ToList();

                        for (int i = 0; i < spouses.Count; i++)
                        {
                            switch (which[i])
                            {
                                case 0:
                                    newDialogues.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1827", spouses[i].displayName));
                                    break;
                                case 1:
                                    newDialogues.Add(((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1832") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1834")) + " " + ((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1837", spouses[i].displayName[0]) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1838", spouses[i].displayName[0])));
                                    break;
                                case 2:
                                    newDialogues.Add(Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1843", spouses[i].displayName));
                                    break;
                                case 3:
                                    newDialogues.Add(((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1831") : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1833")) + " " + ((spouses[i].Gender == 0) ? Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1837", spouses[i].displayName[0]) : Game1.content.LoadString("Strings\\StringsFromCSFiles:Event.cs.1838", spouses[i].displayName[i])));
                                    break;
                            }
                        }
                        dialogues = new List<string>(newDialogues);
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Failed in {nameof(DialogueBoxPatch_Constructor)}:\n{ex}", LogLevel.Error);
                }
            }
        }
    }
}
