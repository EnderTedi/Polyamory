using StardewValley;
using StardewValley.Locations;

namespace Polyamory
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
        /// <returns>Dictionay<string, NPC></returns>
        Dictionary<string, NPC> GetSpouses(Farmer farmer, bool all = true);

        /// <summary>
        /// Sets the last spouse to be pregnant.
        /// </summary>
        /// <param name="name">The internal name of the spouse.</param>
        void SetLastPregnantSpouse(string name);

        /// <summary>
        /// Returns whether the specified npc is polyamorous.
        /// </summary>
        /// <param name="npc">The npc's internal name.</param>
        /// <returns>Bool</returns>
        bool IsNpcPolyamorous(string npc);

        /// <summary>
        /// Returns a List<String> with the names of all the NPCs the specified farmer is dating.
        /// </summary>
        /// <param name="farmer">Farmer whose partners to get.</param>
        /// <returns>List<String></returns>
        List<string> PeopleDating(Farmer farmer);

        /// <summary>
        /// Returns whether the specified farmer can date, marry or room with the specified NPC.
        /// </summary>
        /// <param name="farmer">Farmer to check against</param>
        /// <param name="npc">NPC to check against</param>
        /// <returns>Bool</returns>
        bool IsValidDating(Farmer farmer, string npc);

        /// <summary>
        /// Returns whether the specified farmer's current partners have chemistry with the specified NPC.
        /// </summary>
        /// <param name="farmer">Farmer to check against</param>
        /// <param name="npc">NPC to check against</param>
        /// <param name="newNpc">Extra npc the farmer isn't already dating or rooming with to check against.</param>
        /// <returns>Bool</returns>
        bool HasChemistry(Farmer farmer, string npc, string? newNpc = null);

        /// <summary>
        /// Returns whether the specified farmer is currently dating, married or rooming with a monogamous npc.
        /// </summary>
        /// <param name="farmer">Farmer to check against</param>
        /// <returns>Bool</returns>
        public bool IsWithMonogamousNPC(Farmer farmer);
    }
}
