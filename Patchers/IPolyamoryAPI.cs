using StardewValley.Locations;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polyamory.Patchers
{
    public interface IPolyamoryAPI
    {
        /// <summary>
        /// Places Spouses in the farmhouse.
        /// </summary>
        /// <param name="farmHouse"></param>
        void PlaceSpousesInFarmhouse(FarmHouse farmHouse);

        /// <summary>
        /// Returns spouses for the given farmer.
        /// </summary>
        /// <param name="farmer">Farmer whose spouses to get.</param>
        /// <param name="all">Whether to return one spouse or all of them.</param>
        /// <returns></returns>
        Dictionary<string, NPC> GetSpouses(Farmer farmer, bool all = true);

        public Dictionary<string, NPC> GetSpouses(Farmer farmer, int all = -1);

        /// <summary>
        /// Sets the last spouse to be pregnant.
        /// </summary>
        /// <param name="name">The internal name of the spouse.</param>
        public void SetLastPregnantSpouse(string name);

        /// <summary>
        /// Returns whether the specified npc is polyamorous or not.
        /// </summary>
        /// <param name="npc">The npc's internal name.</param>
        /// <returns></returns>
        public bool IsNpcPolyamorous(string npc);
    }
}
