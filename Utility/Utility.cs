using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using System.Collections;

namespace Polyamory
{
    internal partial class Polyamory
    {
        private readonly static Dictionary<string, int> topOfHeadOffsets = new();

        public static bool IsNpcPolyamorous(string npc)
        {
            helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"{modid}/PolyamoryData").TryGetValue(npc, out PolyamoryData? data);
            if (data == null || data.IsPolyamorous == true)
                return true;
            return false;
        }

        public static bool IsDatingOtherPeople(Farmer farmer, string currNpc)
        {
            bool IsDating = false;
            foreach (string npc in Game1.characterData.Keys)
            {
                if (npc == currNpc) continue;

                monitor.Log($"checking {npc}");
                farmer.friendshipData.TryGetValue(npc, out var friendship);
                friendship ??= farmer.friendshipData[npc] = new Friendship();
                monitor.Log($"{npc}, {friendship.Status}");
                if (friendship.Status is not FriendshipStatus.Friendly && friendship.Status is not FriendshipStatus.Divorced)
                {
                    IsDating = true;
                }
            }
            return IsDating;
        }

        public static bool IsValidEngagement(Farmer farmer, string npc)
        {
            if (Game1.getCharacterFromName(npc) is null || (!IsNpcPolyamorous(npc) && farmer.isMarriedOrRoommates()))
                return false;
            if (IsMarriedToNonPolyamorousNPC(farmer) && farmer.isMarriedOrRoommates())
                return false;
            if (!HasChemistry(farmer, npc))
                return false;

            return true;
        }

        public static bool IsMarriedToNonPolyamorousNPC(Farmer farmer)
        {
            var spouses = GetSpouses(farmer, true);
            foreach (string spouse in spouses.Keys)
            {
                if (!IsNpcPolyamorous(spouse))
                    return true;
            }
            return false;
        }

        public static bool HasChemistry(Farmer farmer, string npc)
        {
            Dictionary<string, PolyamoryData>? data = helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"{modid}/PolyamoryData");
            data.TryGetValue(npc, out PolyamoryData? npcData);

            var spouses = GetSpouses(farmer, true);
            foreach (string spouse in spouses.Keys)
            {
                if (npcData is not null)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    if (npcData.PositiveChemistry is not null && !npcData.NegativeChemistry.Contains(spouse))
                        return false;
                    else if (npcData.NegativeChemistry is not null && npcData.NegativeChemistry.Contains(spouse))
                        return false;
#pragma warning restore CS8604 // Possible null reference argument.
                }

                data.TryGetValue(spouse, out PolyamoryData? spouseData);
                if (spouseData is null)
                    continue;
                if (spouseData.PositiveChemistry is not null && !spouseData.PositiveChemistry.Contains(npc))
                    return false;
                else if (spouseData.NegativeChemistry is not null && spouseData.NegativeChemistry.Contains(npc))
                    return false;
            }
            return true;
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

        internal static void ResetDivorces()
        {
            if (!Config.PreventHostileDivorces)
                return;
            List<string> friends = Game1.player.friendshipData.Keys.ToList();
            foreach (string f in friends)
            {
                if (Game1.player.friendshipData[f].Status == FriendshipStatus.Divorced)
                {
                    monitor.Log($"Wiping divorce for {f}");
                    if (Game1.player.friendshipData[f].Points < 8 * 250)
                        Game1.player.friendshipData[f].Status = FriendshipStatus.Friendly;
                    else
                        Game1.player.friendshipData[f].Status = FriendshipStatus.Dating;
                }
            }
        }

        public static string? GetRandomSpouse(Farmer f)
        {
            var spouses = GetSpouses(f, true);
            if (spouses.Count == 0)
                return null;
            ShuffleDic(ref spouses);
            return spouses.Keys.ToArray()[0];
        }

        public static void PlaceSpousesInFarmhouse(FarmHouse farmHouse)
        {
            Farmer farmer = farmHouse.owner;

            if (farmer == null)
                return;

            List<NPC> allSpouses = GetSpouses(farmer, true).Values.ToList();

            if (allSpouses.Count == 0)
            {
                monitor.Log("no spouses");
                return;
            }

            ShuffleList(ref allSpouses);

            List<string> bedSpouses = new();
            string? kitchenSpouse = null;

            foreach (NPC spouse in allSpouses)
            {
                IDictionary npcExtData = Game1.content.Load<IDictionary>("spacechase0.SpaceCore/NpcExtensionData");
                if (npcExtData is not null && npcExtData.Contains($"{spouse}"))
                {
                    var entry = npcExtData[spouse.Name] as dynamic;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (entry.IgnoreMarriageSchedule)
                    {
                        continue;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
                if (!farmHouse.Equals(spouse.currentLocation))
                {
                    monitor.Log($"{spouse.Name} is not in farm house ({spouse.currentLocation.Name})");
                    continue;
                }
                int type = random.Next(0, 100);

                monitor.Log($"spouse rand {type}, bed: {Config.PercentChanceForSpouseInBed} kitchen {Config.PercentChanceForSpouseInKitchen}");

                if (type < Config.PercentChanceForSpouseInBed)
                {
                    if (bedSpouses.Count < 1 && (!farmer.friendshipData[spouse.Name].IsRoommate()) && HasSleepingAnimation(spouse.Name))
                    {
                        monitor.Log("made bed spouse: " + spouse.Name);
                        bedSpouses.Add(spouse.Name);
                    }

                }
                else if (type < Config.PercentChanceForSpouseInBed + Config.PercentChanceForSpouseInKitchen)
                {
                    if (kitchenSpouse == null)
                    {
                        monitor.Log("made kitchen spouse: " + spouse.Name);
                        kitchenSpouse = spouse.Name;
                    }
                }
                else if (type < Config.PercentChanceForSpouseInBed + Config.PercentChanceForSpouseInKitchen + Config.PercentChanceForSpouseAtPatio)
                {
                    if (!Game1.isRaining && !Game1.IsWinter && !Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth).Equals("Sat") && !spouse.Name.Equals("Krobus") && spouse.Schedule == null)
                    {
                        monitor.Log("made patio spouse: " + spouse.Name);
                        spouse.setUpForOutdoorPatioActivity();
                        monitor.Log($"{spouse.Name} at {spouse.currentLocation.Name} {spouse.TilePoint}");
                    }
                }
            }
        }

        private static string SleepAnimation(string name)
        {
            string? anim = null;
            if (Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions").ContainsKey(name.ToLower() + "_sleep"))
            {
                anim = Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions")[name.ToLower() + "_sleep"];
            }
            else if (Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions").ContainsKey(name + "_Sleep"))
            {
                anim = Game1.content.Load<Dictionary<string, string>>("Data\\animationDescriptions")[name + "_Sleep"];
            }
#pragma warning disable CS8603 // Possible null reference return.
            return anim;
#pragma warning restore CS8603 // Possible null reference return.
        }

        public static bool HasSleepingAnimation(string name)
        {
            string sleepAnim = SleepAnimation(name);
            if (sleepAnim == null || !sleepAnim.Contains('/'))
                return false;

            if (!int.TryParse(sleepAnim.Split('/')[0], out int sleepidx))
                return false;

            Texture2D tex = helper.GameContent.Load<Texture2D>($"Characters/{name}");

            if (sleepidx / 4 * 32 >= tex.Height)
            {
                return false;
            }
            return true;
        }

        public static List<string> ReorderSpousesForSleeping(List<string> sleepSpouses)
        {
            List<string> spouses = new();

            foreach (string s in sleepSpouses)
            {
                if (!spouses.Contains(s))
                {
                    spouses.Add(s);
                }
            }

            return spouses;
        }

        public static Point GetBedStart(FarmHouse fh)
        {
            if (fh?.GetSpouseBed()?.GetBedSpot() == null)
                return Point.Zero;
            return new Point(fh.GetSpouseBed().GetBedSpot().X - 1, fh.GetSpouseBed().GetBedSpot().Y - 1);
        }

        public static List<string> GetBedSpouses(FarmHouse fh)
        {
            return GetSpouses(fh.owner, true).Keys.ToList().FindAll(s => !fh.owner.friendshipData[s].RoommateMarriage);
        }

        public static int GetBedWidth()
        {
            /*if (bedTweaksAPI != null)
            {
                return bedTweaksAPI.GetBedWidth();
            }
            else*/
            {
                return 3;
            }
        }

        public static Vector2 GetSpouseBedPosition(FarmHouse fh, string name)
        {
            var allBedmates = GetBedSpouses(fh);

            Point bedStart = GetBedStart(fh);
            int x = 64 + (int)((allBedmates.IndexOf(name) + 1) / (float)(allBedmates.Count + 1) * (GetBedWidth() - 1) * 64);
            return new Vector2(bedStart.X * 64 + x, bedStart.Y * 64 + bedSleepOffset - (GetTopOfHeadSleepOffset(name) * 4));
        }

        private static bool IsTileOccupied(GameLocation location, Point tileLocation, string characterToIgnore)
        {
            Rectangle tileLocationRect = new(tileLocation.X * 64 + 1, tileLocation.Y * 64 + 1, 62, 62);

            for (int i = 0; i < location.characters.Count; i++)
            {
                if (location.characters[i] != null && !location.characters[i].Name.Equals(characterToIgnore) && location.characters[i].GetBoundingBox().Intersects(tileLocationRect))
                {
                    monitor.Log($"Tile {tileLocation} is occupied by {location.characters[i].Name}");

                    return true;
                }
            }
            return false;
        }

        public static Point GetSpouseBedEndPoint(FarmHouse fh, string name)
        {
            var bedSpouses = GetBedSpouses(fh);

            Point bedStart = fh.GetSpouseBed().GetBedSpot();
            int bedWidth = GetBedWidth();

            int x = (int)(bedSpouses.IndexOf(name) / (float)(bedSpouses.Count) * (bedWidth - 1));
            if (x < 0)
                return Point.Zero;
            return new Point(bedStart.X + x, bedStart.Y);
        }

        public static int GetTopOfHeadSleepOffset(string name)
        {
            if (topOfHeadOffsets.ContainsKey(name))
            {
                return topOfHeadOffsets[name];
            }
            //SMonitor.Log($"dont yet have offset for {name}");
            int top = 0;

            if (name == "Krobus")
                return 8;

            Texture2D tex = Game1.content.Load<Texture2D>($"Characters\\{Game1.getCharacterFromName(name).getTextureName()}");

            string sleepAnim = SleepAnimation(name);
            if (sleepAnim == null || !int.TryParse(sleepAnim.Split('/')[0], out int sleepidx))
                sleepidx = 8;

            if ((sleepidx * 16) / 64 * 32 >= tex.Height)
            {
                sleepidx = 8;
            }


            Color[] colors = new Color[tex.Width * tex.Height];
            tex.GetData(colors);

            int startx = (sleepidx * 16) % 64;
            int starty = (sleepidx * 16) / 64 * 32;

            for (int i = 0; i < 16 * 32; i++)
            {
                int idx = startx + (i % 16) + (starty + i / 16) * 64;
                if (idx >= colors.Length)
                {
                    monitor.Log($"Sleep pos couldn't get pixel at {startx + i % 16},{starty + i / 16} ");
                    break;
                }
                Color c = colors[idx];
                if (c != Color.Transparent)
                {
                    top = i / 16;
                    break;
                }
            }
            topOfHeadOffsets.Add(name, top);
            return top;
        }

        public static bool IsInBed(FarmHouse fh, Rectangle box)
        {
            int bedWidth = GetBedWidth();
            Point bedStart = GetBedStart(fh);
            Rectangle bed = new(bedStart.X * 64, bedStart.Y * 64, bedWidth * 64, 3 * 64);

            if (box.Intersects(bed))
            {
                return true;
            }
            return false;
        }

        public static void ShuffleList<T>(ref List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

#pragma warning disable CS8714
        public static void ShuffleDic<T1, T2>(ref Dictionary<T1, T2> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                (list[list.Keys.ToArray()[n]], list[list.Keys.ToArray()[k]]) = (list[list.Keys.ToArray()[k]], list[list.Keys.ToArray()[n]]);
            }
        }
#pragma warning restore CS8714
    }
}
