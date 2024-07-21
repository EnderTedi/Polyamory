using ContentPatcher;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewValley;

namespace Polyamory
{
    internal class ModAPIs
    {

#pragma warning disable CS8618
        public static IModHelper helper;
        public static IMonitor monitor;
        public static ModConfig config;
        public static Polyamory mod;
#pragma warning restore CS8618

        public static void Initialize(IModHelper Helper, IMonitor Monitor, ModConfig Config, Polyamory Polyamory)
        {
            helper = Helper;
            monitor = Monitor;
            config = Config;
            mod = Polyamory;
        }

        public static void LoadAPIs()
        {
            var GMCM = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            var contentPatcher = helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");

            if (GMCM is not null)
            {
                GMCM.Register(
                    mod: mod.ModManifest,
                    reset: () => config = new ModConfig(),
                    save: () => helper.WriteConfig(config)
                );

                GMCM.AddSectionTitle(
                    mod: mod.ModManifest,
                    text: () => I18n.Config_Section_Pendant());

                GMCM.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_BuyPendantsAnytime_Name(),
                    tooltip: () => I18n.Config_BuyPendantsAnytime_Description(),
                    getValue: () => config.BuyPendantsAnytime,
                    setValue: value => config.BuyPendantsAnytime = value
                    );

                GMCM.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_PendantPrice_Name(),
                    tooltip: () => I18n.Config_PendantPrice_Description(),
                    getValue: () => config.PendantPrice,
                    setValue: value => config.PendantPrice = value,
                    min: 0
                    );

                GMCM.AddParagraph(
                    mod: mod.ModManifest,
                    text: () => ""
                    );

                GMCM.AddSectionTitle(
                    mod: mod.ModManifest,
                    text: () => I18n.Config_Section_Divorce());

                GMCM.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_PreventHostileDivorces_Name(),
                    tooltip: () => I18n.Config_PreventHostileDivorces_Description(),
                    getValue: () => config.PreventHostileDivorces,
                    setValue: value => config.PreventHostileDivorces = value
                    );

                GMCM.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_ComplexDivorce_Name(),
                    tooltip: () => I18n.Config_ComplexDivorce_Description(),
                    getValue: () => config.ComplexDivorce,
                    setValue: value => config.ComplexDivorce = value
                    );

                GMCM.AddParagraph(
                    mod: mod.ModManifest,
                    text: () => ""
                    );

                GMCM.AddSectionTitle(
                    mod: mod.ModManifest,
                    text: () => I18n.Config_Section_Children()
                    );

                GMCM.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_MaxChildren_Name(),
                    tooltip: () => I18n.Config_MaxChildren_Description(),
                    getValue: () => config.MaxChildren,
                    setValue: value => config.MaxChildren = value
                    );

                GMCM.AddBoolOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_ShowParentNames_Name(),
                    tooltip: () => I18n.Config_ShowParentNames_Description(),
                    getValue: () => config.ShowParentNames,
                    setValue: value => config.ShowParentNames = value
                    );

                GMCM.AddParagraph(
                    mod: mod.ModManifest,
                    text: () => ""
                    );

                GMCM.AddSectionTitle(
                    mod: mod.ModManifest,
                    text: () => I18n.Config_Section_Spouse()
                    );

                GMCM.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseInBed_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseInBed_Description(),
                    getValue: () => config.PercentChanceForSpouseInBed,
                    setValue: value => config.PercentChanceForSpouseInBed = value
                    );

                GMCM.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseInKitchen_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseInKitchen_Description(),
                    getValue: () => config.PercentChanceForSpouseInKitchen,
                    setValue: value => config.PercentChanceForSpouseInKitchen = value
                    );

                GMCM.AddNumberOption(
                    mod: mod.ModManifest,
                    name: () => I18n.Config_PercentChanceForSpouseAtPatio_Name(),
                    tooltip: () => I18n.Config_PercentChanceForSpouseAtPatio_Description(),
                    getValue: () => config.PercentChanceForSpouseAtPatio,
                    setValue: value => config.PercentChanceForSpouseAtPatio = value
                    );
            }

            if (contentPatcher is not null)
            {
                contentPatcher.RegisterToken(mod.ModManifest, "PlayerSpouses", () =>
                {
                    Farmer player;

                    if (Context.IsWorldReady)
                        player = Game1.player;
                    else if (SaveGame.loaded?.player != null)
                        player = SaveGame.loaded.player;
                    else
                        return null;

                    var spouses = Polyamory.GetSpouses(player, true).Keys.ToList();
                    spouses.Sort(delegate (string a, string b) {
                        player.friendshipData.TryGetValue(a, out Friendship af);
                        player.friendshipData.TryGetValue(b, out Friendship bf);
                        if (af == null && bf == null)
                            return 0;
                        if (af == null)
                            return -1;
                        if (bf == null)
                            return 1;
                        if (af.WeddingDate == bf.WeddingDate)
                            return 0;
                        return af.WeddingDate > bf.WeddingDate ? -1 : 1;
                    });
                    return spouses.ToArray();
                });

                contentPatcher.RegisterToken(mod.ModManifest, "IsDatingAnyone", getValue: () =>
                {
                    Farmer player;

                    if (Context.IsWorldReady)
                        player = Game1.player;
                    else if (SaveGame.loaded?.player != null)
                        player = SaveGame.loaded.player;
                    else
                        return new string[] { "false" };

                    return new string[] { Polyamory.IsDatingOtherPeople(player) ? "true" : "false" };
                });
            }
        }
    }
}
