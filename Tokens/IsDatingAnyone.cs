using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;

namespace Polyamory.Tokens
{
    internal class IsDatingAnyone : AdvancedToken
    {
        public Dictionary<string, bool> IsDating;

        internal static readonly string any = "anyPlayer";

        public IsDatingAnyone()
        {
            IsDating = new()
            {
                [main] = new(),
                [local] = new(),
                [any] = new()
            };
        }

        public override bool RequiresInput()
        {
            return false;
        }

        public override bool CanHaveMultipleValues(string? input = null)
        {
            return false; 
        }

        public override bool TryValidateInput(string? input, out string error)
        {
            List<string> args = input?.ToLower()?.Trim()?.Split('|').ToList() ?? [];
            error = "";

            string[] pargs = ["main", "local", "any"];

            if (args.Count < 2)
            {
                return true;
            }

            string playerarg;

            if (!args.Any(l => l.Trim().StartsWithIgnoreCase("Player") && !l.ContainsIgnoreCase("endertedi.polyamory/playerspouses")))
            {
                error = "Token has arguments but none of them are recognised. Expected one of 'Player'";
                return false;
            }

            if (!args.First(l => l.Trim().StartsWithIgnoreCase("Player")).Contains('='))
            {
                error = "Token has player argument but player not specified.";
                return false;
            }

            if (args.First(l => l.Trim().StartsWithIgnoreCase("Player")).Contains('='))
                playerarg = args.First(l => l.Trim().StartsWithIgnoreCase("Player")).Split('=')[1].Trim();
            else
                return true;

            if (pargs.Contains(playerarg.ToLower()))
                return true;

            error = $"Player argument is invalid. Got '{playerarg}', expected one of 'Main', 'Local', 'Any'";
            return false;
        }

        public override IEnumerable<string> GetValues(string input)
        {
            List<string> args = input?.ToLower()?.Trim()?.Split('|').ToList() ?? [];

            bool isDating = false;
            bool any = false;

            Farmer Player;

            if (!args.Any(l => l.StartsWithIgnoreCase("Player")))
                Player = Game1.player;
            else switch (args.First(l => l.StartsWithIgnoreCase("Player")).Split('=')[1].ToLower())
                {
                    case "main":
                        Player = Game1.MasterPlayer;
                        break;
                    case "any":
                        any = true;
                        Player = Game1.player;
                        break;
                    case "local":
                    default:
                        Player = Game1.player;
                        break;
                }

            if (any)
                foreach(Farmer player in Game1.getAllFarmers())
                {
                    if (Polyamory.IsDatingOtherPeople(player))
                    {
                        isDating = true;
                        break;
                    }
                    
                }
            else
                isDating = Polyamory.IsDatingOtherPeople(Player);

            return isDating ? [ "true" ] : [ "false" ];
        }

        protected override bool DidDataChange()
        {
            bool HasChanged = false;

            if (IsDating[local] == Polyamory.IsDatingOtherPeople(Game1.player))
            {
                IsDating[local] = Polyamory.IsDatingOtherPeople(Game1.player);
                HasChanged = true;
            }

            //Applies to all players
            bool isDating = false;
            foreach (Farmer player in Game1.getAllFarmers())
            {
                if (Polyamory.IsDatingOtherPeople(player))
                {
                    isDating = true;
                    break;
                }
            }
            if (IsDating[any] != isDating)
            {
                HasChanged = true;
            }

            //Applies to all players, not just main player.
            if (IsDating[main] == Polyamory.IsDatingOtherPeople(Game1.MasterPlayer))
            {
                IsDating[main] = Polyamory.IsDatingOtherPeople(Game1.MasterPlayer);
                HasChanged = true;
            }

            return HasChanged;
        }
    }
}
