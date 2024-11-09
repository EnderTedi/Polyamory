using Microsoft.CodeAnalysis.Operations;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polyamory.Tokens
{
    internal class HasMonogamousPartner : AdvancedToken
    {
        public Dictionary<string, bool> hasMonogamousPartner;

        internal static readonly string any = "anyPlayer";

        public HasMonogamousPartner()
        {
            hasMonogamousPartner = new()
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
            bool isMonogamous = false;
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
                    if (Polyamory.IsWithMonogamousNPC(player)) 
                    {
                        isMonogamous = true;
                        break;
                    }
                    
                }
            else
                isMonogamous = Polyamory.IsWithMonogamousNPC(Player);

            return isMonogamous ? ["true"] : [ "false" ];
        }

        protected override bool DidDataChange()
        {
            bool HasChanged = false;
            bool isMonogamous = false;

            foreach (string partner in Polyamory.PeopleDating(Game1.player))
            {
                if (Polyamory.IsNpcPolyamorous(partner))
                {
                    isMonogamous = true;
                    break;
                }
            }
            if (hasMonogamousPartner[local] != isMonogamous)
            {
                hasMonogamousPartner[local] = isMonogamous;
                HasChanged = true;
            }

            isMonogamous = false;

            //Applies to all players
            foreach (Farmer player in Game1.getAllFarmers())
            {
                foreach (string partner in Polyamory.PeopleDating(player))
                {
                    if (Polyamory.IsNpcPolyamorous(partner))
                    {
                        isMonogamous = true;
                        break;
                    }
                }
            }
            if (hasMonogamousPartner[any] != isMonogamous)
            {
                hasMonogamousPartner[any] = isMonogamous;
                HasChanged = true;
            }

            isMonogamous = false;

            //Applies to all players, not just main player.
            foreach (string partner in Polyamory.PeopleDating(Game1.MasterPlayer))
            {
                if (Polyamory.IsNpcPolyamorous(partner))
                {
                    isMonogamous = true;
                    break;
                }
            }
            if (hasMonogamousPartner[main] != isMonogamous)
            {
                hasMonogamousPartner[main] = isMonogamous;
                HasChanged = true;
            }

            return HasChanged;
        }
    }
}
