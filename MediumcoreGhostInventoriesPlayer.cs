using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ID;

namespace MediumcoreGhostInventories
{
    class MediumcoreGhostInventoriesPlayer : ModPlayer
    {
        private Dictionary<Point, PlayerDeathInventory> playerDeathInventoryMap;
        private const int SEARCH_DISTANCE = 32;
        private const int OFFSCREEN_COORDS = 704;

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            //if not mediumcore
            if (player.difficulty != (byte)1)
                return true;

            MediumcoreGhostInventoriesWorld currentWorld = ModContent.GetInstance<MediumcoreGhostInventoriesWorld>();

            playerDeathInventoryMap = currentWorld.playerDeathInventoryMap;

            Item[] deathInventory = new Item[player.inventory.Length];
            bool[] favourites = new bool[player.inventory.Length];
            Item[] deathArmor = new Item[player.armor.Length];
            Item[] deathDye = new Item[player.dye.Length];
            Item[] deathMiscEquips = new Item[player.miscEquips.Length];
            Item[] deathMiscDyes = new Item[player.miscDyes.Length];

            //If player is holding an item on their mouse, drop it
            if (Main.netMode != NetmodeID.Server && Main.mouseItem.type > ItemID.None)
            {
                int itemIndex = Item.NewItem(player.getRect(), Main.mouseItem.type, Stack: Main.mouseItem.stack, prefixGiven: Main.mouseItem.prefix);
                Main.item[itemIndex].noGrabDelay = 100;//Make player not instantly pick up item, without this the player will pick it up before dying
                NetMessage.SendData(MessageID.SyncItem, number: itemIndex); // Sync the item across clients
            }

            //Clears current player inventory and stores it in above arrays
            GetAndClearInventory(ref deathInventory, ref favourites, ref deathArmor, ref deathDye, ref deathMiscEquips, ref deathMiscDyes);
            PlayerDeathInventory currentInventory = new PlayerDeathInventory(deathInventory, deathArmor, deathDye, deathMiscEquips, deathMiscDyes, player.name);

            //Dont continue if inventory is just the starter inventory
            if (currentInventory.numOfItems == 3)
                if (currentInventory.deathInventory[0].Name == "Copper Shortsword" && currentInventory.deathInventory[1].Name == "Copper Pickaxe" && currentInventory.deathInventory[2].Name == "Copper Axe")
                    return true;

            //Dont continue if empty inventory
            if (currentInventory.numOfItems == 0)
                return true;

            //Set death position to the centre of the player
            Point deathPosition = new Point((int)player.position.X, (int)player.position.Y);
            mod.Logger.Debug($"player death position - {player.position}");

            //if near current position doesnt already have an inventory  on it and is within the map bounds spawn the npc and add to the inventory dictionary. Else search for a new untaken position
            if (CheckPosition(deathPosition))
            {
                playerDeathInventoryMap[deathPosition] = currentInventory;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);//Update inventory positions on clients to make sure they all match
                    SendSpawn(deathPosition.X, deathPosition.Y);//Send message to clients to spawn the npc
                }
                //Make sure npc isnt spawned yet on multiplayer clients incase they do not have updated deathInventoryMap, they will spawn the npc once they receive the spawnNPC message
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC(deathPosition.X, deathPosition.Y, mod.NPCType("GhostInventory"), ai0: deathPosition.X, ai1: deathPosition.Y);

                if (Main.netMode == NetmodeID.MultiplayerClient && Array.Exists(favourites, element => element))
                    SendFavorites(favourites, deathPosition.X, deathPosition.Y);//Send favourited items to server
            }
            else
            {
                Point nextUntakenDeathPosition = FindUntakenDeathPosition(deathPosition);
                playerDeathInventoryMap[nextUntakenDeathPosition] = currentInventory;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);//Update inventory positions on clients to make sure they all match
                    SendSpawn(nextUntakenDeathPosition.X, nextUntakenDeathPosition.Y);//Send message to clients to spawn the npc
                }
                //Make sure npc isnt spawned yet on multiplayer clients incase they do not have updated deathInventoryMap, they will spawn the npc once they receive the spawnNPC message
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC(nextUntakenDeathPosition.X, nextUntakenDeathPosition.Y, mod.NPCType("GhostInventory"), ai0: nextUntakenDeathPosition.X, ai1: nextUntakenDeathPosition.Y);

                if (Main.netMode == NetmodeID.MultiplayerClient && Array.Exists(favourites, element => element))
                   SendFavorites(favourites, nextUntakenDeathPosition.X, nextUntakenDeathPosition.Y);//Send favourited items to server
            }

            return true;
        }

        private void SendFavorites(bool[] favourites, int x, int y)
        {
            ModPacket packet = mod.GetPacket(capacity: 1000);
            packet.Write((byte)MediumcoreGhostInventories.MediumcoreGhostInventoriesMessageType.SetFavourites);

            packet.Write(x);
            packet.Write(y);

            //For each favourited item, send the position of that item in the inventory
            for (int i = 0; i < favourites.Length; i++)
            {
                if (favourites[i])
                    packet.Write(i);
            }

            //Value way outside range of inventory length, when we read this value we will stop reading for favourited items.
            packet.Write(100);
            packet.Send();
        }

        //Tell clients to spawn the inventory npc
        private void SendSpawn(int x, int y)
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)MediumcoreGhostInventories.MediumcoreGhostInventoriesMessageType.SpawnNPC);

            packet.Write(x);
            packet.Write(y);
            packet.Send();
        }

        //Check if can place a ghost on this position
        private bool CheckPosition(Point position)
        {
            //if position outside map or offscreen on X axis
            if (position.X < Main.leftWorld + OFFSCREEN_COORDS || position.X > Main.rightWorld - OFFSCREEN_COORDS)
                return false;

            //if position outside map on Y axis
            if (position.Y < Main.topWorld + OFFSCREEN_COORDS || position.Y > Main.bottomWorld - OFFSCREEN_COORDS)
                return false;

            //Check within search distance on X axis for other inventories. If any are found then return false
            foreach (KeyValuePair<Point, PlayerDeathInventory> entry in playerDeathInventoryMap)
            {
                if (entry.Key.X - position.X < SEARCH_DISTANCE && position.X - entry.Key.X < SEARCH_DISTANCE)
                    return false;
            }
            return true;
        }

        //Search outwards alternating right then left SEARCH_DISTANCE units at a time looking for a usable position
        private Point FindUntakenDeathPosition(Point deathPosition)
        {
            //Keep position within bounds of visible world on Y axis
            deathPosition.Y = Utils.Clamp(deathPosition.Y, (int)Main.topWorld + OFFSCREEN_COORDS + SEARCH_DISTANCE, (int)Main.bottomWorld - OFFSCREEN_COORDS - SEARCH_DISTANCE);

            Point searchRight = deathPosition;
            Point searchLeft = deathPosition;

            do
            {
                //if not the end of the visible world to the right
                if (searchRight.X < Main.rightWorld - OFFSCREEN_COORDS)
                {
                    searchRight.X += SEARCH_DISTANCE;
                    if (CheckPosition(searchRight))
                        return searchRight;
                }

                //if not the end of the visible world to the left
                if (searchLeft.X > OFFSCREEN_COORDS)
                {
                    searchLeft.X -= SEARCH_DISTANCE;
                    if (CheckPosition(searchLeft))
                        return searchLeft;
                }

            } while (searchRight.X < Main.rightWorld - OFFSCREEN_COORDS || searchLeft.X > Main.leftWorld + OFFSCREEN_COORDS); //while within map bounds, should effectively never be false but just incase to stop infinite loop
            mod.Logger.Debug($"coudlnt find untaken position - {deathPosition}");
            return deathPosition;
        }

        private void GetAndClearInventory(ref Item[] deathInventory, ref bool[] favourites, ref Item[] deathArmor, ref Item[] deathDye, ref Item[] deathMiscEquips, ref Item[] deathMiscDyes)
        {
            //INVENTORY
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if(Main.netMode == NetmodeID.MultiplayerClient)
                    favourites[i] = player.inventory[i].favorited;

                deathInventory[i] = player.inventory[i];
                player.inventory[i] = new Item();
            }

            //ARMOR - SOCIAL
            for (int i = 0; i < player.armor.Length; i++)
            {
                deathArmor[i] = player.armor[i];
                player.armor[i] = new Item();
            }

            //DYES
            for (int i = 0; i < player.dye.Length; i++)
            {
                deathDye[i] = player.dye[i];
                player.dye[i] = new Item();
            }

            //EQUIPMENT
            for (int i = 0; i < player.miscEquips.Length; i++)
            {
                deathMiscEquips[i] = player.miscEquips[i];
                player.miscEquips[i] = new Item();
            }

            //EQUIPMENT - DYE
            for (int i = 0; i < player.miscDyes.Length; i++)
            {
                deathMiscDyes[i] = player.miscDyes[i];
                player.miscDyes[i] = new Item();
            }
        }
    }
}
