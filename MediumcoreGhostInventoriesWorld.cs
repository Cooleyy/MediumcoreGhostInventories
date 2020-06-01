using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace MediumcoreGhostInventories
{
    class MediumcoreGhostInventoriesWorld : ModWorld
    {
        public Dictionary<Point, PlayerDeathInventory> playerDeathInventoryMap;

        public override void Initialize()
        {
            playerDeathInventoryMap = new Dictionary<Point, PlayerDeathInventory>();
        }

        public override TagCompound Save()
        {
            var inventoryMap = new List<TagCompound>();
            foreach (var inventory in playerDeathInventoryMap)
            {
                mod.Logger.Debug($"Saving inventory at  - {inventory.Key}");

                inventoryMap.Add(new TagCompound() {
                    { "positionX", inventory.Key.X },
                    { "positionY", inventory.Key.Y },
                    { "playerID", inventory.Value.playerName },
                    { "inventory", new List<Item>(inventory.Value.deathInventory) },
                    { "armor", new List<Item>(inventory.Value.deathArmor) },
                    { "dye", new List<Item>(inventory.Value.deathDye) },
                    { "miscEquips", new List<Item>(inventory.Value.deathMiscEquips) },
                    { "miscDyes", new List<Item>(inventory.Value.deathMiscDyes) },
                });
            }
            return new TagCompound
            {
                { "playerDeathInventoryMap", inventoryMap },
            };
        }

        public override void Load(TagCompound tag)
        {
            try
            {
                var inventoryMap = tag.GetList<TagCompound>("playerDeathInventoryMap");
                foreach (var inventory in inventoryMap)
                {
                    Point position = new Point(inventory.GetInt("positionX"), inventory.GetInt("positionY"));

                    mod.Logger.Debug($"Loading inventory at  - {position}");

                    string playerID = inventory.GetString("playerID");

                    Item[] dInventory = new Item[Main.player[0].inventory.Length];
                    Item[] dArmor = new Item[Main.player[0].armor.Length];
                    Item[] dDye = new Item[Main.player[0].dye.Length];
                    Item[] dMiscEquips = new Item[Main.player[0].miscEquips.Length];
                    Item[] dMiscDyes = new Item[Main.player[0].miscDyes.Length];

                    LoadItemList(inventory.Get<List<Item>>("inventory"), dInventory);
                    LoadItemList(inventory.Get<List<Item>>("armor"), dArmor);
                    LoadItemList(inventory.Get<List<Item>>("dye"), dDye);
                    LoadItemList(inventory.Get<List<Item>>("miscEquips"), dMiscEquips);
                    LoadItemList(inventory.Get<List<Item>>("miscDyes"), dMiscDyes);

                    playerDeathInventoryMap[position] = new PlayerDeathInventory(dInventory, dArmor, dDye, dMiscEquips, dMiscDyes, playerID);

                    //Spawn NPC for each loaded inventory and pass the X and Y position to its ai
                    NPC.NewNPC(position.X, position.Y, mod.NPCType("GhostInventory"), ai0: position.X, ai1: position.Y);
                }
            }
            catch (Exception e)
            {
                mod.Logger.Error("Error loading saved death inventories " + e.Message);
            }
        }

        private void LoadItemList(List<Item> items, Item[] inventory)
        {
            for (int i = 0; i < inventory.Length && i < items.Count; i++)
            {
                if ("Unloaded Item".Equals(items[i].Name))
                {
                    inventory[i] = new Item();
                }
                else
                {
                    inventory[i] = items[i];
                }
            }
        }

        //Send Inventories stored on the world to multiplayer clients
        public override void NetSend(BinaryWriter writer)
        {
            mod.Logger.Debug($"Net send");
            writer.Write(playerDeathInventoryMap.Count);
            foreach (var inventory in playerDeathInventoryMap)
            {
                writer.Write(inventory.Key.X);
                writer.Write(inventory.Key.Y);
                writer.Write(inventory.Value.playerName);

                SendItems(ref writer, inventory.Value.deathInventory, writeFavs: true);
                SendItems(ref writer, inventory.Value.deathArmor);
                SendItems(ref writer, inventory.Value.deathDye);
                SendItems(ref writer, inventory.Value.deathMiscEquips);
                SendItems(ref writer, inventory.Value.deathMiscDyes);
            }
        }

        //Adds number of Items and each Item to the writer
        private void SendItems(ref BinaryWriter writer, Item[] itemArray, bool writeFavs = false)
        {
            writer.Write(itemArray.Length);
            foreach (var item in itemArray)
                ItemIO.Send(item, writer, writeStack: true, writeFavourite: writeFavs);
        }

        //Receive Inventories stored on the world from the server and rebuilds the dictionary of inventories
        public override void NetReceive(BinaryReader reader)
        {
            mod.Logger.Debug($"Net receive");
            playerDeathInventoryMap = new Dictionary<Point, PlayerDeathInventory>();

            Point position = new Point();
            string playerName = "";
            int dInventoryLength = 0;
            int dArmorLength = 0;
            int dDyeLength = 0;
            int dMiscEquipsLength = 0;
            int dMiscDyesLength = 0;

            int numOfInventories = reader.ReadInt32();
            for (int i = 0; i < numOfInventories; i++)
            {
                position = new Point(reader.ReadInt32(), reader.ReadInt32());
                playerName = reader.ReadString();

                dInventoryLength = reader.ReadInt32();
                Item[] dInventory = new Item[dInventoryLength];
                for (int j = 0; j < dInventoryLength; j++)
                    dInventory[j] = ItemIO.Receive(reader, readStack: true, readFavorite: true);

                dArmorLength = reader.ReadInt32();
                Item[] dArmor = new Item[dArmorLength];
                for (int j = 0; j < dArmorLength; j++)
                    dArmor[j] = ItemIO.Receive(reader, readStack: true);

                dDyeLength = reader.ReadInt32();
                Item[] dDye = new Item[dDyeLength];
                for (int j = 0; j < dDyeLength; j++)
                    dDye[j] = ItemIO.Receive(reader, readStack: true);

                dMiscEquipsLength = reader.ReadInt32();
                Item[] dMiscEquips = new Item[dMiscEquipsLength];
                for (int j = 0; j < dMiscEquipsLength; j++)
                    dMiscEquips[j] = ItemIO.Receive(reader, readStack: true);

                dMiscDyesLength = reader.ReadInt32();
                Item[] dMiscDyes = new Item[dMiscDyesLength];
                for (int j = 0; j < dMiscDyesLength; j++)
                    dMiscDyes[j] = ItemIO.Receive(reader, readStack: true);

                playerDeathInventoryMap[position] = new PlayerDeathInventory(dInventory, dArmor, dDye, dMiscEquips, dMiscDyes, playerName);
            }
        }
    }
}
