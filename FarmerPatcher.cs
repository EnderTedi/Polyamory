using HarmonyLib;
using StardewValley;

namespace Polyamory
{
    internal class FarmerPatcher {
        [HarmonyPatch(typeof(Farmer), nameof(Farmer.spouse))]
        public static class FarmerPatch
        {
            public static void Postfix(Farmer __instance, ref NPC __result)
            {
                if (Polyamory.tempSpouse != null && __instance.friendshipData.ContainsKey(Polyamory.tempSpouse.Name) && __instance.friendshipData[Polyamory.tempSpouse.Name].IsMarried())
				{
					__result = Game1.getCharacterFromName(Polyamory.tempSpouse.Name);
				}
            }
        }
        /*get
		{
			if (!string.IsNullOrEmpty(this.netSpouse.Value))
			{
				return this.netSpouse.Value;
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				this.netSpouse.Value = "";
			}
			else
			{
				this.netSpouse.Value = value;
			}
		}*/
    }
}
