using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace Polyamory
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

        [HarmonyPatch(typeof(Farmer), nameof(Farmer.spouse))]
        public static class FarmerPatch_spouse
        {
            public static void Postfix(Farmer __instance, ref string __result)
            {
                if (skipSpouse)
                    return;
                try
                {
                    if (__instance.spouse != null && __instance.friendshipData.ContainsKey(Polyamory.tempSpouse.Name) && __instance.friendshipData[Polyamory.tempSpouse.Name].IsMarried())
                    {
                        __result = Polyamory.tempSpouse.Name;
                    }
                    else
                    {
                        var spouses = Polyamory.GetSpouses(__instance, true);
                        string aspouse = null;
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
                    {
                        monitor.Log($"Failed in {nameof(FarmerPatch_spouse)}\n{ex}", LogLevel.Error);
                    }
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
                        monitor.Log($"Failed in {nameof(FarmerPatch_getSpouse)}:\n{ex}", LogLevel.Error);
                    }
                }
            }
        }
    }
}
