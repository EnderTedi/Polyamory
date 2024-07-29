using StardewModdingAPI;
namespace Polyamory.Patchers
{
    internal class Game1Patcher
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

        public static class Game1Patch_getCharacterFromName
        {
            public static void Prefix(string name)
            {
                if (EventPatcher.startingLoadActors)
                    lastGotCharacter = name;
            }
            
        }
    }
}
