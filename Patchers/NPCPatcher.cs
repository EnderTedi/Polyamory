﻿using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using System.Reflection;
using Object = StardewValley.Object;

namespace Polyamory.Patchers
{
    internal class NPCPatcher
    {

#pragma warning disable CS8618
        public static IModHelper helper;
        public static IMonitor monitor;
#pragma warning restore CS8618

        public static void Initialize(IModHelper Helper, IMonitor Monitor)
        {
            helper = Helper;
            monitor = Monitor;
        }

        [HarmonyPatch(typeof(NPC), nameof(NPC.tryToReceiveActiveObject))]
        public static class NPCPatch_tryToReceiveActiveObject
        {
            public static bool Prefix(NPC __instance, ref bool __result, Farmer who, bool probe = false)
            {
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
        }
    }
}
