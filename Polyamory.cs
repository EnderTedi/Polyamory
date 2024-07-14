using HarmonyLib;
using Polyamory.Patchers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.WorldMaps;

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

    internal class PolyamoryData
    {
        public bool IsPolyamorous { get; set; } = true;
        public bool IgnoreRejectDialogue { get; set; } = false;

        public Dictionary<string, Array>? Exclusions { get; set; }
        public Dictionary<string, Array>? Inclusions { get; set; }
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

        private static readonly string modid = "EnderTedi.Polyamory";

        public override void Entry(IModHelper Helper)
        {
            I18n.Init(Helper.Translation);
            Helper.Events.Content.AssetRequested += OnAssetRequested;
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            Config = Helper.ReadConfig<ModConfig>();
            var harmony = new Harmony(this.ModManifest.UniqueID);
            monitor = Monitor;
            helper = Helper;
            Initialize(Helper, Monitor);
            FarmerPatcher.Initialize(Helper, Monitor);
            NPCPatcher.Initialize(Helper, Monitor);

            harmony.PatchAll(typeof(Polyamory).Assembly);
             harmony.Patch(
               original: AccessTools.PropertyGetter(typeof(Farmer), nameof(Farmer.spouse)),
               postfix: new HarmonyMethod(typeof(FarmerPatcher.FarmerPatch_spouse), nameof(FarmerPatcher.FarmerPatch_spouse.Postfix))
            );

            Helper.ConsoleCommands.Add("Polyamory.IsNpcPolyamorous", "Returns whether the specified NPCs are polyamorous.\nAccepts internal NPC names or \"All\" for all npcs.", (cmd, args) =>
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
                            Monitor.Log($"{value} is not a valid NPC.", LogLevel.Warn);
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

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var GMCM = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");

            if (GMCM is not null)
            {
                GMCM.Register(
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config)
                );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Pendant());

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_BuyPendantsAnytime_Name(),
                    tooltip: () => I18n.Config_BuyPendantsAnytime_Description(),
                    getValue: () => Config.BuyPendantsAnytime,
                    setValue: value => Config.BuyPendantsAnytime = value
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PendantPrice_Name(),
                    tooltip: () => I18n.Config_PendantPrice_Description(),
                    getValue: () => Config.PendantPrice,
                    setValue: value => Config.PendantPrice = value,
                    min: 0
                    );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Divorce());

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PreventHostileDivorces_Name(),
                    tooltip: () => I18n.Config_PreventHostileDivorces_Description(),
                    getValue: () => Config.PreventHostileDivorces,
                    setValue: value => Config.PreventHostileDivorces = value
                    );

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_ComplexDivorce_Name(),
                    tooltip: () => I18n.Config_ComplexDivorce_Description(),
                    getValue: () => Config.ComplexDivorce,
                    setValue: value => Config.ComplexDivorce = value
                    );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Children()
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_MaxChildren_Name(),
                    tooltip: () => I18n.Config_MaxChildren_Description(),
                    getValue: () => Config.MaxChildren,
                    setValue: value => Config.MaxChildren = value
                    );

                GMCM.AddBoolOption(
                    mod: ModManifest,
                    name: () => I18n.Config_ShowParentNames_Name(),
                    tooltip: () => I18n.Config_ShowParentNames_Description(),
                    getValue: () => Config.ShowParentNames,
                    setValue: value => Config.ShowParentNames = value
                    );

                GMCM.AddSectionTitle(
                    mod: ModManifest,
                    text: () => I18n.Config_Section_Spouse()
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseInBed_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseInBed_Description(),
                    getValue: () => Config.PercentChanceForSpouseInBed,
                    setValue: value => Config.PercentChanceForSpouseInBed = value
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseInKitchen_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseInKitchen_Description(),
                    getValue: () => Config.PercentChanceForSpouseInKitchen,
                    setValue: value => Config.PercentChanceForSpouseInKitchen = value
                    );

                GMCM.AddNumberOption(
                    mod: ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseAtPatio_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseAtPatio_Description(),
                    getValue: () => Config.PercentChanceForSpouseAtPatio,
                    setValue: value => Config.PercentChanceForSpouseAtPatio = value
                    );
            }
        }
    }
}
