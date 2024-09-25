using StardewModdingAPI;
using StardewValley;

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

        public override bool TryValidateInput(string input, out string error)
        {
            List<string> args = input?.ToLower()?.Trim()?.Split('|').ToList() ?? new List<string>();
            error = "";

            foreach (string arg in args)
            {
                Polyamory.monitor.Log(arg, LogLevel.Alert);
            }

            if (args.Count == 1)
            {
                return true;
            }

            if (args.Count > 2)
            {
                error = "Too many inputs.";
                return false;
            }
            else if (args.Count == 2 && !args[1].Contains("player"))
            {
                error = "Input is invalid. Expected one of 'Player'";
                return false;
            }
            else if (args.Count == 2 && args[1].Split('=').Length == 2 && !args[1].Split('=')[1].Equals("main") && !args[0].Split('=')[1].Equals("local") && !args[0].Split('=')[1].Equals("any"))
            {
                error = "Player input is invalid. Expected one of 'Main', 'Local' or 'Any'.";
                return false;
            }

            return true;
        }

        public override IEnumerable<string> GetValues(string input)
        {
            List<string> args = input?.ToLower()?.Trim()?.Split('|').ToList() ?? new List<string>();

            bool isDating = false;
            if (args.Count == 2 && args[1].Split('=')[1].Equals("main"))
                isDating = Polyamory.IsDatingOtherPeople(Game1.MasterPlayer);
            else if (args.Count == 2 && args[1].Split('=')[1].Equals("local") || args.Count == 1)
                isDating = Polyamory.IsDatingOtherPeople(Game1.player);
            else foreach (Farmer player in Game1.getAllFarmers())
                {
                    if (Polyamory.IsDatingOtherPeople(player))
                    {
                        isDating = true;
                        break;
                    }
                }

            return isDating ? new List<string>() { "true" } : new List<string>() { "false" };
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
