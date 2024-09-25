using Microsoft.CodeAnalysis.Operations;
using StardewModdingAPI;
using StardewValley;
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
            bool isMonogamous = false;

            if (args.Count == 2 && args[1].Split('=')[1].Equals("main"))
            {
                isMonogamous = Polyamory.IsWithMonogamousNPC(Game1.MasterPlayer);
            }
            else if (args.Count == 2 && args[1].Split('=')[1].Equals("local") || args.Count == 1)
            {
                isMonogamous = Polyamory.IsWithMonogamousNPC(Game1.player);
            }
            else foreach (Farmer player in Game1.getAllFarmers())
                {
                    isMonogamous = Polyamory.IsWithMonogamousNPC(player);
                    if (isMonogamous) break;
                }

            return isMonogamous ? new List<string>() { "true" } : new List<string>() { "false" };
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
