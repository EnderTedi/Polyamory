using StardewValley;
using StardewValley.Extensions;

namespace Polyamory.Tokens
{
    internal class PlayerSpouses : AdvancedToken
    {
        public Dictionary<string, List<string>> SpousesPerPlayer;

        public PlayerSpouses()
        {
            SpousesPerPlayer = new()
            {
                [main] = [],
                [local] = []
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

        public override bool TryValidateInput(string? input, out string error)
        {
            List<string> args = input?.ToLower()?.Trim()?.Split('|').ToList() ?? [];
            error = "";

            string[] pargs = ["main", "local"];

            string playerarg;

            if (args.Count < 2)
            {
                return true;
            }

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

            error = $"Player argument is invalid. Got '{playerarg}', expected one of 'Main', 'Local'.";
            return false;
        }

        public override IEnumerable<string> GetValues(string input)
        {
            List<string> args = input?.ToLower()?.Trim()?.Split('|').ToList() ?? [];

            Farmer Player;

            if (!args.Any(l => l.StartsWithIgnoreCase("Player")))
                Player = Game1.player;
            else Player = args.First(l => l.StartsWithIgnoreCase("Player")).Split('=')[1].ToLower() switch
            {
                "main" => Game1.MasterPlayer,
                "local" => Game1.player,
                _ => Game1.player,
            };

            var Spouses = Polyamory.GetSpouses(Player, true).Keys.ToList();

            if (Spouses.Count <= 0)
            {
                return ["None"];
            }

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
                SpousesPerPlayer[local] = [.. Polyamory.GetSpouses(Game1.MasterPlayer, true).Keys];
                HasChanged = true;
            }

            //Applies to all players, not just main player.
            if (!Polyamory.GetSpouses(Game1.MasterPlayer, true).Keys.ToList().Equals(SpousesPerPlayer[main]))
            {
                SpousesPerPlayer[main] = [.. Polyamory.GetSpouses(Game1.MasterPlayer, true).Keys];
                HasChanged = true;
            }

            return HasChanged;
        }
    }
}
