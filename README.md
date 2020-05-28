# MediumcoreGhostInventories

Terraria mod that on player death spawns a ghost that holds the players inventory.

This mods purpose is to remove the need to do inventory management every time you die. It will also make sure your dropped inventories are saved when you close the server/game.
Whenever a player dies a ghost will spawn where they died that can then be chatted with and will give the player the option to retrieve their inventory just as it was when they died.
These inventories and positions will be saved with the world and when loading the world the ghosts will respawn in the saved position.
Only the player whos name matches the player whos inventory the ghost stores can retrieve it but anyone can remove the ghost and have it drop the items.
If two players die very close together the second ghost should search for a better position a little further away on the x axis, this stops them stacking on top of one another.

Possible Issues:
Accessories or items that prevent death will probably cause the ghost to still spawn meaning inventory will be duplicated.

Thanks to Logicon and Exuvo whos code was used to create the foundation for this mod.
