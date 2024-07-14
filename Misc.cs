using StardewValley;

namespace Polyamory
{
    internal partial class Polyamory
    {
        public static void ReloadSpouses(Farmer farmer)
        {
            Spouses[farmer.UniqueMultiplayerID] = new Dictionary<string, NPC>();
            UnofficialSpouses[farmer.UniqueMultiplayerID] = new Dictionary<string, NPC>();
            string ospouse = farmer.spouse;
            if (ospouse != null)
            {
                var npc = Game1.getCharacterFromName(ospouse);
                if (npc is not null)
                {
                    Spouses[farmer.UniqueMultiplayerID][ospouse] = npc;
                }
            }
            SMonitor.Log($"Checking for extra spouses in {farmer.friendshipData.Count()} friends");
            foreach (string friend in farmer.friendshipData.Keys)
            {
                if (farmer.friendshipData[friend].IsMarried() && friend != farmer.spouse)
                {
                    var npc = Game1.getCharacterFromName(friend, true);
                    if (npc != null)
                    {
                        Spouses[farmer.UniqueMultiplayerID][friend] = npc;
                        UnofficialSpouses[farmer.UniqueMultiplayerID][friend] = npc;
                    }
                }
            }
            if (farmer.spouse is null && Spouses[farmer.UniqueMultiplayerID].Any())
                farmer.spouse = Spouses[farmer.UniqueMultiplayerID].First().Key;
            SMonitor.Log($"reloaded {Spouses[farmer.UniqueMultiplayerID].Count} spouses for {farmer.Name} {farmer.UniqueMultiplayerID}");
        }


        public static Dictionary<string, NPC> GetSpouses(Farmer farmer, bool all)
        {
            if (!Spouses.ContainsKey(farmer.UniqueMultiplayerID) || (Spouses[farmer.UniqueMultiplayerID].Count == 0 && farmer.spouse != null))
            {
                ReloadSpouses(farmer);
            }
            if (farmer.spouse == null && Spouses[farmer.UniqueMultiplayerID].Count > 0)
            {
                farmer.spouse = Spouses[farmer.UniqueMultiplayerID].First().Key;
            }
            return all ? Spouses[farmer.UniqueMultiplayerID] : UnofficialSpouses[farmer.UniqueMultiplayerID];
        }
    }
}
