using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Polyamory.Patchers
{
    internal class PathFindControllerPatcher
    {

#pragma warning disable CS8618
        private static IModHelper helper;
        private static IMonitor monitor;
        private static ModConfig config;
#pragma warning restore CS8618

        internal static void Initialize(IModHelper Helper, IMonitor Monitor, ModConfig Config)
        {
            helper = Helper;
            monitor = Monitor;
            config = Config;
        }

        public static void PathFindController_Prefix(Character c, GameLocation location, ref Point endPoint)
        {
            try
            {
#pragma warning disable CS8602
                if (c is not NPC || !(c as NPC).IsVillager || !(c as NPC).isMarried() || location is not FarmHouse || endPoint == (location as FarmHouse).getEntryLocation())
#pragma warning restore CS8602
                    return;

#pragma warning disable CS8604
                if (Polyamory.IsInBed(location as FarmHouse, new Rectangle(endPoint.X * 64, endPoint.Y * 64, 64, 64)))
                {
                    Point point = Polyamory.GetSpouseBedEndPoint(location as FarmHouse, c.Name);
#pragma warning restore CS8604
                    if (point.X < 0 || point.Y < 0)
                    {
                        monitor.Log($"Error setting bed endpoint for {c.Name}", LogLevel.Warn);
                    }
                    else
                    {
                        endPoint = point;
                        monitor.Log($"Moved {c.Name} bed endpoint to {endPoint}");
                    }
                }
                else if (IsColliding(c, location, endPoint))
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    var point = (location as FarmHouse).getRandomOpenPointInHouse(Game1.random);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    if (point != Point.Zero)
                    {
                        endPoint = point;
                        monitor.Log($"Moved {c.Name} endpoint to random point {endPoint}");
                    }
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Failed in {nameof(PathFindController_Prefix)}:\n{ex}", LogLevel.Error);
            }
        }

        private static bool IsColliding(Character c, GameLocation location, Point endPoint)
        {

            monitor.Log($"Checking {c.Name} endpoint in farmhouse");
            using IEnumerator<Character> characters = location.characters.GetEnumerator();
            while (characters.MoveNext())
            {
                if (characters.Current != c)
                {
#pragma warning disable CS8602
                    if (characters.Current.TilePoint == endPoint || (characters.Current is NPC && (characters.Current as NPC).controller?.endPoint == endPoint))
#pragma warning restore CS8602
                    {
                        monitor.Log($"{c.Name} endpoint {endPoint} collides with {characters.Current.Name}");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
