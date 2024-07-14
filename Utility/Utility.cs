using StardewModdingAPI;
using StardewValley;
using System.Threading;

namespace Polyamory
{
    internal partial class Polyamory
    {
        public static void Initialize(IModHelper Helper, IMonitor Monitor)
        {
            helper = Helper;
            monitor = Monitor;
        }

        public static bool IsNpcPolyamorous(string npc)
        {
            helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"{modid}/PolyamoryData").TryGetValue(npc, out PolyamoryData? data);
            if (data == null || data.IsPolyamorous == true)
                return true;
            return false;
        }

        public static bool IsValidEngagement(Farmer farmer, string npc)
        {
            if (Game1.getCharacterFromName(npc) is null || (!IsNpcPolyamorous(npc) && farmer.isMarriedOrRoommates()))
            {
                return false;
            }

            var spouses = GetSpouses(farmer, true);
            foreach (string spouse in spouses.Keys)
            {
                monitor.Log($"{npc}: {IsNpcPolyamorous(npc)}", LogLevel.Warn);
                if (!IsNpcPolyamorous(spouse) && farmer.isMarriedOrRoommates())
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsMarriedToNonPolyamorousNPC(Farmer farmer)
        {
            var spouses = GetSpouses(farmer, true);
            foreach (string spouse in spouses.Keys)
            {
                if (IsNpcPolyamorous(spouse))
                    return true;
            }
            return false;
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
            monitor.Log($"Checking for extra spouses in {farmer.friendshipData.Count()} friends");
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
            monitor.Log($"reloaded {Spouses[farmer.UniqueMultiplayerID].Count} spouses for {farmer.Name} {farmer.UniqueMultiplayerID}");
        }

        public static void ResetSpouses(Farmer farmer, bool force = false)
        {
            if (force)
            {
                Spouses.Remove(farmer.UniqueMultiplayerID);
                UnofficialSpouses.Remove(farmer.UniqueMultiplayerID);
            }
            Dictionary<string, NPC> spouses = GetSpouses(farmer, true);
            if (farmer.spouse == null)
            {
                if (spouses.Count > 0)
                {
                    monitor.Log("No official spouse, setting official spouse to: " + spouses.First().Key);
                    farmer.spouse = spouses.First().Key;
                }
            }

            foreach (string name in farmer.friendshipData.Keys)
            {
                if (farmer.friendshipData[name].IsEngaged())
                {
                    monitor.Log($"{farmer.Name} is engaged to: {name} {farmer.friendshipData[name].CountdownToWedding} days until wedding");
                    if (farmer.friendshipData[name].WeddingDate.TotalDays < new WorldDate(Game1.Date).TotalDays)
                    {
                        monitor.Log("invalid engagement: " + name);
                        farmer.friendshipData[name].WeddingDate.TotalDays = new WorldDate(Game1.Date).TotalDays + 1;
                    }
                    if (farmer.spouse != name)
                    {
                        monitor.Log("setting spouse to engagee: " + name);
                        farmer.spouse = name;
                    }
                }
                if (farmer.friendshipData[name].IsMarried() && farmer.spouse != name)
                {
                    //monitor.Log($"{f.Name} is married to: {name}");
                    if (farmer.spouse != null && farmer.friendshipData[farmer.spouse] != null && !farmer.friendshipData[farmer.spouse].IsMarried() && !farmer.friendshipData[farmer.spouse].IsEngaged())
                    {
                        monitor.Log("invalid ospouse, setting ospouse to " + name);
                        farmer.spouse = name;
                    }
                    if (farmer.spouse == null)
                    {
                        monitor.Log("null ospouse, setting ospouse to " + name);
                        farmer.spouse = name;
                    }
                }
            }
            ReloadSpouses(farmer);
        }
    }
}
