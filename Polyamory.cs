using HarmonyLib;
using Polyamory.Patchers;
using StardewModdingAPI;
using StardewValley;

namespace Polyamory
{
    internal class ModConfig
    {
        public bool BuyPendantsAnytime { get; set; } = false;
        public int PendantPrice { get; set; } = 5000;
        public bool PreventHostileDivorces { get; set; } = true;
        public bool ComplexDivorce { get; set; } = true;
        public int MaxChildren { get; set; } = 2;
        public bool ShowParentNames { get; set; } = false;
        public int PercentChanceForSpouseInBed { get; set; } = 25;
        public int PercentChanceForSpouseInKitchen { get; set; } = 25;
        public int PercentChanceForSpouseAtPatio { get; set; } = 25;
    }

    internal partial class Polyamory : Mod
    {
#pragma warning disable CS8618
        public static IMonitor monitor;
        public static IModHelper helper;
        public static ModConfig Config;
#pragma warning restore CS8618
        public static Random random = new();
        public static NPC? tempSpouse;
        public static Dictionary<long, Dictionary<string, NPC>> Spouses = new();
        public static Dictionary<long, Dictionary<string, NPC>> UnofficialSpouses = new();
        public static string? spouseToDivorce = null;
        public static int divorceHeartsLost;
        public static string? farmHelperSpouse = null;
        public static int bedSleepOffset = 76;

        private static readonly string modid = "EnderTedi.Polyamory";

        public override void Entry(IModHelper Helper)
        {
            monitor = Monitor;
            helper = Helper;
            Config = Helper.ReadConfig<ModConfig>();

            I18n.Init(Helper.Translation);
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.Content.AssetRequested += OnAssetRequested2;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;

            FarmerPatcher.Initialize(Helper, Monitor);
            NPCPatcher.Initialize(Helper, Monitor);
            Game1Patcher.Initialize(Helper, Monitor);
            EventPatcher.Initialize(Helper, Monitor);

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll(typeof(Polyamory).Assembly);
            harmony.Patch(
               original: AccessTools.PropertyGetter(typeof(Farmer), nameof(Farmer.spouse)),
               postfix: new HarmonyMethod(typeof(FarmerPatcher.FarmerPatch_spouse), nameof(FarmerPatcher.FarmerPatch_spouse.Postfix))
            );
            harmony.Patch(
                original: AccessTools.GetDeclaredMethods(typeof(Game1)).Where(m => m.Name == "getCharacterFromName" && m.ReturnType == typeof(NPC)).First(),
                prefix: new HarmonyMethod(typeof(Game1Patcher.Game1Patch_getCharacterFromName), nameof(Game1Patcher.Game1Patch_getCharacterFromName.Prefix))
                );

            Helper.ConsoleCommands.Add("Polyamory.IsNpcPolyamorous", "Returns whether the specified NPCs are polyamorous.\nAccepts internal NPC names or \"All\" for all npcs.", (cmd, args) =>
            {
                if (args.Length < 1)
                {
                    monitor.Log("No arguments given.", LogLevel.Error);
                    return;
                }

                if (!Context.IsWorldReady)
                {
                    monitor.Log("Save not loaded.", LogLevel.Error);
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
                                monitor.Log($"{key}: {IsNpcPolyamorous(key)}", LogLevel.Info);
                            }
                            return;
                        }
                        if (!Game1.characterData.ContainsKey(value))
                        {
                            monitor.Log($"{value} is not a valid NPC.", LogLevel.Warn);
                        }
                        else
                        {
                            monitor.Log($"{value}: {IsNpcPolyamorous(value)}", LogLevel.Info);
                        }
                    }
                }
            });

            Helper.ConsoleCommands.Add("Polyamory.HasChemistry", "...", (cmd, args) =>
            {
                if (args.Length < 1)
                {
                    monitor.Log("No arguments given.", LogLevel.Error);
                    return;
                }
                
                if (args.Length > 1)
                {
                    monitor.Log("Too many arguments given.", LogLevel.Error);
                    return;
                }

                if (!Context.IsWorldReady)
                {
                    monitor.Log("Save not loaded.", LogLevel.Error);
                    return;
                }
                monitor.Log($"{HasChemistry(Game1.player, args[0])}", LogLevel.Info);
            });
        }
    }
}
