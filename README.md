# MediumcoreGhostInventories

Terraria mod that on player death spawns a ghost that holds the players inventory.

This mods purpose is to remove the need to do inventory management every time you die as a mediumcore character. By default in Terraria when you die in mediumcore and close the server before picking up your items they are lost, this mod solves that as inventories will be saved with the map.

Whenever a player dies a ghost will spawn where they died that can then be chatted with and will give the player the option to retrieve their inventory just as it was when they died.
The inventory will be restored with all items, equips, dyes, ammo and money in the exact positions they were and also whether they were favourited or not.
These inventories and positions will be saved with the world and when loading the world the ghosts will respawn in the saved positions.
Ghosts will only spawn when a mediumcore character dies and any type of character can be on a server together.
All items currently on the player (except the starter inventory) will drop when replaced by the ghosts stored inventory.
The only item that drops on the ground when you die is anything held outside the inventory on the mouse.
Only the player whos name matches the player whos inventory the ghost stores can retrieve it but anyone can remove the ghost and have it drop the items.
If two players die very close together the second ghost should search for a better position a little further away to the left or right, this stops them stacking on top of one another.
If you die off the edge of the map the ghost will spawn at the closest position it can within the bounds of the map.
The only inventories that will not spawn a ghost are an empty inventory or the exact default Terraria starting inventory of the three copper tools.

Possible Issues:
Accessories or items from mods that prevent death will probably cause the ghost to still spawn meaning inventory will be duplicated.

Thanks to Logicon and Exuvo whos code was used to create the foundation for this mod.
