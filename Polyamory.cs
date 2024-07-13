using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;

namespace Polyamory
{
    internal class Polyamory : Mod
    {

        public static NPC tempSpouse = new();
        public static Dictionary<long, Dictionary<string, Array>> Spouses = new();

        private readonly string modid = "EnderTedi.Polyamory"; 
        
        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            helper.Events.Content.AssetRequested += OnAssetRequested;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            var monitor = Monitor;
            //FarmerPatches.Initialize(helper, monitor, harmony);

            helper.ConsoleCommands.Add("Polyamory.IsNpcPolyamorous", "Returns whether specified NPCs are polyamorous", (cmd, args) =>
            {
                if (args.Length < 1)
                {
                    Monitor.Log("No arguments given", LogLevel.Error);
                    return;
                }

                if (!Context.IsWorldReady)
                {
                    Monitor.Log("Load into a save before using.", LogLevel.Error);
                    return;
                }

                for (int i = 0; args.Length > i; i++) {
                    if (ArgUtility.TryGet(args, i, out string value, out string error))
                    {

                        if (args.Length == 1 && String.Equals(value, "All", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (string key in Game1.characterData.Keys)
                            {
                                if (Game1.characterData.TryGetValue(key, out CharacterData? data) && !data.CanBeRomanced)
                                {
                                    Monitor.Log($"{key} is not currently romancable.");
                                }
                                else
                                {
                                    Monitor.Log($"{key}: {IsNpcPolyamorous(key)}", LogLevel.Info);
                                }
                            }
                            return;
                        }
                        if (!Game1.characterData.ContainsKey(value))
                        {
                            Monitor.Log($"{value} is not a valid NPC. Make sure to sure internal name.", LogLevel.Warn);
                        }
                        else if (Game1.characterData.TryGetValue(value, out CharacterData? data) && !data.CanBeRomanced)
                        {
                            Monitor.Log($"{value} is not currently romancable.");
                        }
                        else
                        {
                            Monitor.Log($"{value}: {IsNpcPolyamorous(value)}", LogLevel.Info);
                        }
                    }
                }
            });
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo($"{modid}/PolyamoryData"))
                e.LoadFrom(() => new Dictionary<string, PolyamoryData>(), AssetLoadPriority.Low);
        }

        private bool IsNpcPolyamorous(string npc)
        {
            PolyamoryData? data = Helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"{modid}/PolyamoryData").GetValueOrDefault(npc);
            if (data == null || data.IsPolyamorous == true)
                return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Farmer), nameof(Farmer.getSpouse))]
    public static class Farmer_GetSpouse
    {

    }
}
