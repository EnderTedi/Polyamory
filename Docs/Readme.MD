**Polyamory** is a [Stardew Valley](http://stardewvalley.net/) Mod which provides another take on adding polyamory mechanics to the game. 
Polyamory prioritizes Mod Author customizability by introducing the Monogamy/Polyamory system for NPCs. 

## Content
* [For Players](#For-Players)
	* [Install](#Install)
* [For Modders](#For-Modders)
	* [NPC Polyamory Data](#NPC-Polyamory-Data)
	* [Custom Dialogue Keys](#Custom-Dialogue-Keys)
	* [Content Patcher Tokens](#Content-Patcher-Tokens)
	* [C# API](#C#-API)
* [See Also](#See-Also)



## For Players
### Install
1. [Install the latest version of SMAPI](https://smapi.io/).
2. Install [this mod from Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/26081). -- Currently Unavailable, download from the Releases tab on Github.
3. Run the game using SMAPI.

That's it.

## For Modders
### NPC Polyamory Data

Polyamory Data is located in the `EnderTedi.Polyamory/Data` asset. It is a Dictionary with the key being an NPC's Internal Name and the value being a model with the fields:

<table>
<tr>
	<th> Field </th>
	<th> Description </th>
</tr>
<tr>
	<td> IsPolyamorous </td>
	<td> (Optional) Dictates if the NPC is Polyamorous (true) or Monogamous (false). Default true. </td>
</tr>
<tr>
	<td> PositiveChemistry </td>
	<td> (Optional) A list of NPC Internal Names. If the field is not null the NPC will refuse to date and marry the player if dating anyone not on the list. Has the opposite effect compared to NegativeChemistry. Default null</td>
</tr>
<tr>
	<td> NegativeChemistry </td>
	<td> (Optional) A list of NPC Internal Names. If the field is not null the NPC will refuse to date and marry the player if dating anyone on the list. Has the opposite effect compared to PositiveChemistry. Default null</td>
</tr>
</table>

### Custom Dialogue Keys

Polyamory adds 4 additional Dialogue Keys located in the `Characters\\Dialogue\\<NPC>` asset.

<table>
<tr>
	<th> Dialogue Key </th>
	<th> Description </th>
</tr>
<tr>
	<td> CantDate_DatingMonogamousNPC </td>
	<td> Triggers when the player tries to give a bouquet to an NPC when already dating or being married to a Monogamous NPC. </td>
</tr>
<tr>
	<td> CantDate_IsMonogamousNPC </td>
	<td> Triggers when the player tries to give a bouquet to a Monogamous NPC when already dating or being married to other NPC(s). </td>
</tr>
<tr>
	<td> CantMarry_MarriedMonogamousNPC </td>
	<td> Triggers when the player tries to give a mermaid's pendant to an NPC when already dating or being married to a Monogamous NPC. Required if you have custom reject dialogue for the mermaid's pendant which bypasses the vanilla functions to marry the npc. </td>
</tr>
<tr>
	<td> CantMarry_IsMonogamousNPC </td>
	<td> Triggers when the player tries to give a mermaid's pendant to a Monogamous NPC when already dating or being married to other NPC(s). Required if you have custom reject dialogue for the mermaid's pendant which bypasses the vanilla functions to marry the npc. </td>
</tr>
</table>

### Content Patcher Tokens

Polyamory adds 2 additional Content Patcher Tokens:

<table>
<tr>
	<th> Token </th>
	<th> Description </th>
</tr>
<tr>
	<td> EnderTedi.Polyamory/PlayerSpouses </td>
	<td> Exports a list of all the spouses for the player. </td>
</tr>
<tr>
	<td> EnderTedi.Polyamory/IsDatingAnyone </td>
	<td> Returns true if the player is dating anyone, else false. </td>
</tr>
</table>

### C# API

Polyamory provides an C# API:
- `void PlaceSpousesInFarmhouse(FarmHouse farmHouse)` Places Spouses in the farmhouse.
- `Dictionary<string, NPC> GetSpouses(Farmer farmer, bool all = true);` Returns spouses for the given farmer.
- `void SetLastPregnantSpouse(string name);` Sets the last spouse to be pregnant.
- `bool IsNpcPolyamorous(string npc);` Returns whether the specified npc is polyamorous.
- `List<string> PeopleDating(Farmer farmer);` Returns a list with the names of all the NPCs the specified farmer is dating.
- `bool IsValidEngagement(Farmer farmer, string npc);` Returns whether the specified farmer can get engaged to the specified NPC.
- `bool IsValidDating(Farmer farmer, string npc);` Returns whether the specified farmer can date the specified NPC.
- `bool HasChemistry(Farmer farmer, string npc);` Returns whether the specified farmer's current partners have chemistry with the specified NPC.

## See Also
* [Nexus](https://www.nexusmods.com/stardewvalley/mods/26081)
* [Changelog](Changelog.MD)
