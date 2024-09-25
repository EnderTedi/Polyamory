using StardewValley;
using StardewModdingAPI;

namespace Polyamory.Tokens
{
    internal class PlayerSpouses : AdvancedToken
    {
        public Dictionary<string, List<string>> SpousesPerPlayer;

        public PlayerSpouses()
        {
            SpousesPerPlayer = new()
            {
                [main] = new List<string>(),
                [local] = new List<string>()
            };
        }

        public override bool RequiresInput()
        {
            return false;
        }

        public override bool CanHaveMultipleValues(string? input = null)
        {
            return true; 
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

            Farmer Player;

            if (args.Count == 1)
                Player = Game1.player;
            else if (args[1].Split('=')[1].Equals("main", StringComparison.OrdinalIgnoreCase))
                Player = Game1.MasterPlayer;
            else
                Player = Game1.player;

            var Spouses = Polyamory.GetSpouses(Player, true).Keys.ToList();
            Spouses.Sort(delegate (string a, string b) {
                Player.friendshipData.TryGetValue(a, out Friendship af);
                Player.friendshipData.TryGetValue(b, out Friendship bf);
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

            return Spouses;
        }

        protected override bool DidDataChange()
        {
            bool HasChanged = false;

            if (!Polyamory.GetSpouses(Game1.player, true).Keys.ToList().Equals(SpousesPerPlayer[local]))
            {
                SpousesPerPlayer[local] = Polyamory.GetSpouses(Game1.MasterPlayer, true).Keys.ToList();
                HasChanged = true;
            }

            //Applies to all players, not just main player.
            if (!Polyamory.GetSpouses(Game1.MasterPlayer, true).Keys.ToList().Equals(SpousesPerPlayer[main]))
            {
                SpousesPerPlayer[main] = Polyamory.GetSpouses(Game1.MasterPlayer, true).Keys.ToList();
                HasChanged = true;
            }

            return HasChanged;
        }
    }
}
