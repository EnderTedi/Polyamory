using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;

namespace Polyamory
{
    internal class PolyamoryData
    {
        public bool IsPolyamorous { get; set; } = true;

        public Dictionary<string, Array>? Exclusions { get; set; }
        public Dictionary<string, Array>? Inclusions { get; set; }
    }

    internal partial class Polyamory : Mod
    {
#pragma warning disable CS8618
        public static IMonitor SMonitor;
        public static IModHelper SHelper;
#pragma warning restore CS8618
        public static NPC tempSpouse = new();
        public static Dictionary<long, Dictionary<string, NPC>> Spouses = new();
        public static Dictionary<long, Dictionary<string, NPC>> UnofficialSpouses = new();

        private readonly string modid = "EnderTedi.Polyamory";

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);
            helper.Events.Content.AssetRequested += OnAssetRequested;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            var monitor = Monitor;
            //FarmerPatches.Initialize(helper, monitor, harmony);

            harmony.PatchAll(typeof(Polyamory).Assembly);

            helper.ConsoleCommands.Add("Polyamory.IsNpcPolyamorous", "Returns whether the specified NPCs are polyamorous.\nAccepts internal NPC names or \"All\" for all npcs.", (cmd, args) =>
            {
                if (args.Length < 1)
                {
                    Monitor.Log("No arguments given.", LogLevel.Error);
                    return;
                }

                if (!Context.IsWorldReady)
                {
                    Monitor.Log("Save not loaded.", LogLevel.Error);
                    return;
                }

                for (int i = 0; args.Length > i; i++)
                {
                    if (ArgUtility.TryGet(args, i, out string value, out string error))
                    {

                        if (args.Length == 1 && string.Equals(value, "All", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (string key in Game1.characterData.Keys)
                            {
                                Monitor.Log($"{key}: {IsNpcPolyamorous(key)}", LogLevel.Info);
                            }
                            return;
                        }
                        if (!Game1.characterData.ContainsKey(value))
                        {
                            Monitor.Log($"{value} is not a valid NPC. Make sure to sure internal name.", LogLevel.Warn);
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
}
