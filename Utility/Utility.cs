using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Characters;
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

        public static bool IsDatingOtherPeople(Farmer farmer, string? currNpc = null)
        {
            bool IsDating = false;
            foreach (string npc in Game1.characterData.Keys)
            {
                if (currNpc is not null && npc == currNpc) continue;
                farmer.friendshipData.TryGetValue(npc, out var friendship);
                if (friendship is not null && friendship.IsDating())
                {
                    IsDating = true;
                }
            }
            return IsDating;
        }

        public static List<string> PeopleDating(Farmer farmer)
        {
            List<string>? partners = new();
            foreach (string npc in Game1.characterData.Keys)
            {
                farmer.friendshipData.TryGetValue(npc, out var friendship);
                if (friendship is not null && friendship.IsDating())
                {
                    partners.Add(npc);
                }
            }

            return partners;
        }

        public static bool IsValidDating(Farmer farmer, string npc)
        {
            if (Game1.getCharacterFromName(npc) is null) return false;

            if (!HasChemistry(farmer, npc)) return false;

            var dating = PeopleDating(farmer);
            if (dating is null) return true;

            foreach (string partner in dating)
            {
                if (partner == npc) continue;

                if (partner != npc && !IsNpcPolyamorous(npc)) return false;

                if (!HasChemistry(farmer, partner)) return false;

                if (!IsNpcPolyamorous(partner)) return false;
            }

            return true;
        }

        public static bool IsWithMonogamousNPC(Farmer farmer)
        {
            var partners = PeopleDating(farmer);
            foreach (string partner in partners)
            {
                if (!IsNpcPolyamorous(partner))
                    return true;
            }
            return false;
        }

        public static bool HasChemistry(Farmer farmer, string npc, string? newNpc = null)
        {
            Dictionary<string, PolyamoryData>? data = helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"{modid}/PolyamoryData");

            var partners = PeopleDating(farmer);
            foreach (string partner in partners)
            {
                if (newNpc is not null)
                {
                    data.TryGetValue(newNpc, out PolyamoryData? npcData);
                    if (npcData is not null)
                    {
#pragma warning disable CS8604 // Possible null reference argument.
                        if (npcData.PositiveChemistry is not null && !npcData.NegativeChemistry.Contains(partner))
                            return false;
                        else if (npcData.NegativeChemistry is not null && npcData.NegativeChemistry.Contains(partner))
                            return false;
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                }

                data.TryGetValue(partner, out PolyamoryData? spouseData);
                if (spouseData is null)
                    continue;
#pragma warning disable IDE0075 // Simplify conditional expression
                if (spouseData.PositiveChemistry is not null && (!spouseData.PositiveChemistry.Contains(npc) || newNpc == null ? false : !spouseData.PositiveChemistry.Contains(newNpc)))
                    return false;
                else if (spouseData.NegativeChemistry is not null && (spouseData.NegativeChemistry.Contains(npc) || newNpc == null ? false : spouseData.NegativeChemistry.Contains(newNpc)))
                    return false;
#pragma warning restore IDE0075 // Simplify conditional expression
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
#if !RELEASE
            monitor.Log($"Checking for extra spouses in {farmer.friendshipData.Count()} friends");
#endif
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
#if !RELEASE
            monitor.Log($"reloaded {Spouses[farmer.UniqueMultiplayerID].Count} spouses for {farmer.Name} {farmer.UniqueMultiplayerID}");
#endif
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
#if !RELEASE
                    monitor.Log("No official spouse, setting official spouse to: " + spouses.First().Key);
#endif
                    farmer.spouse = spouses.First().Key;
                }
            }

            foreach (string name in farmer.friendshipData.Keys)
            {
                if (farmer.friendshipData[name].IsEngaged())
                {
#if !RELEASE
                    monitor.Log($"{farmer.Name} is engaged to: {name} {farmer.friendshipData[name].CountdownToWedding} days until wedding");
#endif
                    if (farmer.friendshipData[name].WeddingDate.TotalDays < new WorldDate(Game1.Date).TotalDays)
                    {
#if !RELEASE
                        monitor.Log("invalid engagement: " + name);
#endif
                        farmer.friendshipData[name].WeddingDate.TotalDays = new WorldDate(Game1.Date).TotalDays + 1;
                    }
                    if (farmer.spouse != name)
                    {
#if !RELEASE
                        monitor.Log("setting spouse to engagee: " + name);
#endif
                        farmer.spouse = name;
                    }
                }
                if (farmer.friendshipData[name].IsMarried() && farmer.spouse != name)
                {
#if !RELEASE
                    monitor.Log($"{farmer.Name} is married to: {name}");
#endif
                    if (farmer.spouse != null && farmer.friendshipData[farmer.spouse] != null && !farmer.friendshipData[farmer.spouse].IsMarried() && !farmer.friendshipData[farmer.spouse].IsEngaged())
                    {
#if !RELEASE
                        monitor.Log("invalid ospouse, setting ospouse to " + name);
#endif
                        farmer.spouse = name;
                    }
                    if (farmer.spouse == null)
                    {
#if !RELEASE
                        monitor.Log("null ospouse, setting ospouse to " + name);
#endif
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
#if !RELEASE
                    monitor.Log($"Wiping divorce for {f}");
#endif
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
#if !RELEASE
                monitor.Log("no spouses");
#endif
                return;
            }

            ShuffleList(ref allSpouses);

            List<string> BedSpouses = new();
            string? KitchenSpouse = null;
            string? PorchSpouse = null;
            string? PatioSpouse = null;

            foreach (NPC spouse in allSpouses)
            {

                if (helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
                {
                    IDictionary npcExtData = Game1.content.Load<IDictionary>("spacechase0.SpaceCore/NpcExtensionData");
                    if (npcExtData is not null && npcExtData.Contains($"{spouse}"))
                    {
                        var entry = npcExtData[spouse.Name] as dynamic;
                        if (entry?.IgnoreMarriageSchedule ?? false)
                        {
                            continue;
                        }
                    }
                }


                var data = helper.GameContent.Load<Dictionary<string, PolyamoryData>>($"{modid}/PolyamoryData");

                bool WillGoOutsideToday = spouse.Schedule != null;
                bool CanGoInTheSun = data != null && data.ContainsKey(spouse.Name) && data[spouse.Name].CanGoOutInTheSun;
                    
                if (!farmHouse.Equals(spouse.currentLocation))
                {
                    continue;
                }
                int type = random.Next(0, 100);

                if (BedSpouses.Count <= MathF.Ceiling(Spouses.Count / 4) && (!farmer.friendshipData[spouse.Name].IsRoommate()) && HasSleepingAnimation(spouse.Name) && type < Config.PercentChanceForSpouseInBed)
                {
                    BedSpouses.Add(spouse.Name);
                }
                else if (KitchenSpouse is null && type < Config.PercentChanceForSpouseInBed + Config.PercentChanceForSpouseInKitchen)
                {
                    KitchenSpouse = spouse.Name;
                }
                else if (PorchSpouse is null && !WillGoOutsideToday && CanGoInTheSun && type < Config.PercentChanceForSpouseInBed + Config.PercentChanceForSpouseInKitchen + Config.PercentChangeForSpouseInPorch)
                {
                    PorchSpouse = spouse.Name;
                }
                else if (type < Config.PercentChanceForSpouseInBed + Config.PercentChanceForSpouseInKitchen + Config.PercentChangeForSpouseInPorch + Config.PercentChanceForSpouseAtPatio)
                {
                    if (PatioSpouse is null && !Game1.isRaining && !Game1.IsWinter && !WillGoOutsideToday && CanGoInTheSun)
                    {
                        spouse.setUpForOutdoorPatioActivity();
                        PatioSpouse = spouse.Name;
                    }
                }
            }

            Point SpouseRoomSpot = farmHouse.spouseRoomSpot;
            foreach (NPC spouse in allSpouses)
            {
                if (helper.ModRegistry.IsLoaded("spacechase0.SpaceCore"))
                {
                    IDictionary npcExtData = Game1.content.Load<IDictionary>("spacechase0.SpaceCore/NpcExtensionData");
                    if (npcExtData is not null && npcExtData.Contains($"{spouse}"))
                    {
                        var entry = npcExtData[spouse.Name] as dynamic;
                        if (entry?.IgnoreMarriageSchedule ?? false)
                        {
                            continue;
                        }
                    }
                }

                if (PatioSpouse == spouse.Name) continue;

                if (BedSpouses is not null && BedSpouses.Contains(spouse.Name))
                {
#if !RELEASE
                    monitor.Log("Placing bed spouses");
#endif
                    Vector2 bedSpot = GetSpouseBedPosition(farmHouse, spouse.Name) / 64f;
                    spouse.setTileLocation(bedSpot);
                    BedSpouses.Remove(spouse.Name);
                }
                else if (KitchenSpouse is not null && KitchenSpouse == spouse.Name)
                {
                    Point KitchenSpot = farmHouse.getKitchenStandingSpot();
                    spouse.setTilePosition(KitchenSpot);
                    spouse.setRandomAfternoonMarriageDialogue(Game1.timeOfDay, farmHouse, false);
                    KitchenSpouse = null;
                }
                else if (PorchSpouse is not null && PorchSpouse == spouse.Name)
                {
                    Point PorchSpot = farmHouse.getPorchStandingSpot();
                    Game1.warpCharacter(spouse, "Farm", PorchSpot);
                    spouse.faceDirection(2);
                    PorchSpouse = null;
                }
                else if (SpouseRoomSpot.X > -1 && !IsTileOccupied(farmHouse, SpouseRoomSpot, spouse.Name))
                {
                    spouse.setTilePosition(SpouseRoomSpot);
                    spouse.setSpouseRoomMarriageDialogue();
                }
                else
                {
                    Point RandomSpot = farmHouse.getRandomOpenPointInHouse(random);
                    int i = 0;
                    while (i < 100 && RandomSpot == Point.Zero)
                    {
                        RandomSpot = farmHouse.getRandomOpenPointInHouse(random);
                    }
                    if (RandomSpot == Point.Zero)
                    {
                        Vector2 bedSpot = GetSpouseBedPosition(farmHouse, spouse.Name) / 64f;
                        spouse.setTileLocation(bedSpot);
                        continue;
                    }

                    spouse.setTilePosition(RandomSpot);
                    spouse.faceDirection(random.Next(0, 4));
                    spouse.setRandomAfternoonMarriageDialogue(Game1.timeOfDay, farmHouse, false);
                }
            }
        }

        private static string? SleepAnimation(string name)
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
            return anim;
        }

        public static bool HasSleepingAnimation(string name)
        {
            string? sleepAnim = SleepAnimation(name);
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
            List<string> configSpouses = Config.SpouseSleepOrder.Split(',').Where(s => s.Length > 0).ToList();
            List<string> spouses = new();
            foreach (string s in configSpouses)
            {
                if (sleepSpouses.Contains(s))
                    spouses.Add(s);
            }

            foreach (string s in sleepSpouses)
            {
                if (!spouses.Contains(s))
                {
                    spouses.Add(s);
                    configSpouses.Add(s);
                }
            }
            string configString = string.Join(",", configSpouses);
            if (configString != Config.SpouseSleepOrder)
            {
                Config.SpouseSleepOrder = configString;
                helper.WriteConfig(Config);
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
            float x = 64 + ((allBedmates.IndexOf(name) + 1) / (float)(allBedmates.Count + 1) * (GetBedWidth() - 1) * 64);
            return new Vector2(bedStart.X * 64 + x, bedStart.Y * 64 + bedSleepOffset - (GetTopOfHeadSleepOffset(name) * 4));
        }

        private static bool IsTileOccupied(GameLocation location, Point tileLocation, string characterToIgnore)
        {
            Rectangle tileLocationRect = new(tileLocation.X * 64 + 1, tileLocation.Y * 64 + 1, 62, 62);

            for (int i = 0; i < location.characters.Count; i++)
            {
                if (location.characters[i] != null && !location.characters[i].Name.Equals(characterToIgnore) && location.characters[i].GetBoundingBox().Intersects(tileLocationRect))
                {
#if !RELEASE
                    monitor.Log($"Tile {tileLocation} is occupied by {location.characters[i].Name}");
#endif
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
            int top = 0;

            if (name == "Krobus")
                return 8;

            Texture2D tex = Game1.content.Load<Texture2D>($"Characters\\{Game1.getCharacterFromName(name).getTextureName()}");

            string? sleepAnim = SleepAnimation(name);
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
#if !RELEASE
                    monitor.Log($"Sleep pos couldn't get pixel at {startx + i % 16},{starty + i / 16} ");
#endif
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

        public enum DialogueType
        {
            Bouquet = 0,
            Pendant = 1,
            Roommate = 2
        }

        public static Dialogue FetchAppropriateDialogue(NPC npc, Farmer who, DialogueType DType)
        {
            int Type = (int)DType;
            var partners = PeopleDating(who);
            string[] DialogueType = { "RejectBouquet", "RejectMermaidPendant", "RejectRoommateProposal" };
            if (!IsNpcPolyamorous(npc.Name))
            {
                Dialogue dialogue = npc.TryGetDialogue(DialogueType[Type] + "_IsMonogamous_PlayerWithOtherPeople", Game1.getCharacterFromName(partners[random.Next(0, partners.Count - 1)]).displayName);
                return dialogue ?? Dialogue.FromTranslation(npc, "Strings\\StringsFromCSFiles:" + DialogueType[Type] + "_IsMonogamous_PlayerWithOtherPeople", Game1.getCharacterFromName(partners[random.Next(0, partners.Count - 1)]).displayName);
            }
            else
            {
                Dialogue dialogue = npc.TryGetDialogue(DialogueType[Type] + "IsPolyamorous_PlayerWithSomeoneMonogamous", Game1.getCharacterFromName(partners[0]).displayName);
                return dialogue ?? Dialogue.FromTranslation(npc, "Strings\\StringsFromCSFiles:" + DialogueType[Type] + "_IsPolyamorous_PlayerWithSomeoneMonogamous", Game1.getCharacterFromName(partners[0]).displayName);
            }
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
