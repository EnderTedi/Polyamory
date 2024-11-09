**Polyamory** is a [Stardew Valley](http://stardewvalley.net/) Mod which provides another take on adding polyamory mechanics to the game. 
Polyamory prioritizes Mod Author customizability by introducing the Monogamy/Polyamory system for NPCs. 

## Content
* [For Players](#For-Players)
	* [Install](#Install)
* [For Modders](#For-Modders)
	* [NPC Polyamory Data](#NPC-Polyamory-Data)
	* [Custom Dialogue Keys](#Custom-Dialogue-Keys)
	* [Content Patcher Tokens](#Content-Patcher-Tokens)
	* [C# API](#C-API)
* [See Also](#See-Also)



## For Players
### Install
1. [Install the latest version of SMAPI](https://smapi.io/).
2. Install [this mod from Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/26081). 
	* Currently Unavailable, download from the Releases tab on Github or from the Useful-Files channel on the East Scarp Discord.
3. Run the game using SMAPI.

That's it.

## For Modders
### NPC Polyamory Data

Polyamory Data is located in the `EnderTedi.Polyamory/PolyamoryData` asset. It is a Dictionary with the key being an NPC's Internal Name and the value being a model with the fields:

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
	<td> (Optional) A list of NPC Internal Names. If the field is not null the NPC will refuse to date, marry or room with the player if dating or rooming with anyone not on the list. Has the opposite effect compared to NegativeChemistry. Default null</td>
</tr>
<tr>
	<td> NegativeChemistry </td>
	<td> (Optional) A list of NPC Internal Names. If the field is not null the NPC will refuse to date, marry or room with the player if dating or rooming with anyone on the list. Has the opposite effect compared to PositiveChemistry. Default null</td>
</tr>
</table>

### Custom Dialogue Keys

Polyamory adds 4 additional Dialogue Keys located in the `Characters/Dialogue/<NPC>` asset.

<table>
<tr>
	<th> Dialogue Key </th>
	<th> Description </th>
</tr>
<tr>
	<td> RejectBouquet_IsPolyamorous_PlayerWithSomeoneMonogamous </td>
	<td> Triggers when the player tries to give a bouquet to an NPC when already dating or rooming with a Monogamous NPC. </td>
</tr>
<tr>
	<td> RejectBouquet_IsMonogamous_PlayerWithOtherPeople </td>
	<td> Triggers when the player tries to give a bouquet to a Monogamous NPC when already dating or rooming with other NPCs. </td>
</tr>
<tr>
	<td> RejectMermaidPendant_IsPolyamorous_PlayerWithSomeoneMonogamous </td>
	<td> Triggers when the player tries to give a mermaid's pendant to an NPC when already dating or rooming with a Monogamous NPC. Required if you have custom reject dialogue for the mermaid's pendant which bypasses the vanilla functions to marry the npc and the NPC is polyamorous. </td>
</tr>
<tr>
	<td> RejectMermaidPendant_IsMonogamous_PlayerWithOtherPeople </td>
	<td> Triggers when the player tries to give a mermaid's pendant to a Monogamous NPC when already dating or rooming with other NPCs. Required if you have custom reject dialogue for the mermaid's pendant which bypasses the vanilla functions to marry the npc and the NPC is monogamous. </td>
</tr>
<tr>
	<td> RejectRoommateProposal_IsPolyamorous_PlayerWithSomeoneMonogamous </td>
	<td> Triggers when the player tries to room with a Monogamous NPC when already dating or rooming with other NPCs. Required if you have custom reject dialogue for the roommate proposal item which bypasses the vanilla functions to roommate the npc and the NPC is polyamorous. </td>
</tr>
<tr>
	<td> RejectRoommateProposal_IsMonogamous_PlayerWithOtherPeople </td>
	<td> Triggers when the player tries to room with an NPC when already dating or rooming with a Monogamous NPC. Required if you have custom reject dialogue for the roommmate proposal item which bypasses the vanilla functions to roommate the npc and the NPC is monogamous. </td>
</tr>
</table>

### Content Patcher Tokens

Polyamory adds 3 additional Content Patcher Tokens:

<table>
<tr>
	<th> Token </th>
	<th> Description </th>
</tr>
<tr>
	<td> EnderTedi.Polyamory/PlayerSpouses </td>
	<td> Returns a list of all the spouses for the player. (Optional) Can take a player argument (one of 'Main' or 'Local'), example: 'EnderTedi.Polyamory/PlayerSpouses | Player=Main' </td>
</tr>
<tr>
	<td> EnderTedi.Polyamory/IsDatingAnyone </td>
	<td> Returns true if the player is dating anyone, else false. (Optional) Can take a player argument (one of 'Main', 'Local' or 'Any'), example: 'EnderTedi.Polyamory/IsDatingAnyone | Player=Local' </td>
</tr>
<tr>
	<td> EnderTedi.Polyamory/HasMonogamousPartner </td>
	<td> Returns true if the player is currently dating or rooming with a monogamous npc, else false. (Optional) Can take a player argument (one of 'Main', 'Local' or 'Any'), example: 'EnderTedi.Polyamory/HasMonogamousPartner | Player=Local' </td>
</tr>
</table>

### C# API

Polyamory provides an C# API:
- `void PlaceSpousesInFarmhouse(FarmHouse farmHouse)` Places Spouses in the farmhouse.
- `void SetLastPregnantSpouse(string name)` Sets the last spouse to be pregnant.
- `bool IsNpcPolyamorous(string npc)` Returns whether the specified npc is polyamorous.
- `bool IsValidDating(Farmer farmer, string npc, string? newNpc = null)` Returns whether the specified farmer can date/get engaged/room with the specified NPC.
- `bool HasChemistry(Farmer farmer, string npc)` Returns whether the specified farmer's current partners have chemistry with the specified NPC.
- `bool IsWithMonogamousNPC(Farmer farmer)` Returns whether the specified farmer is currently dating or rooming with a monogamous NPC.
- `List<string> PeopleDating(Farmer farmer)` Returns a list with the names of all the NPCs the specified farmer is dating.
- `Dictionary<string, NPC> GetSpouses(Farmer farmer, bool all = true)` Returns spouses for the given farmer.

## See Also
* [Nexus](https://www.nexusmods.com/stardewvalley/mods/26081)
* [Changelog](Changelog.MD)
