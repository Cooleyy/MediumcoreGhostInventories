using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Microsoft.Xna.Framework;

namespace MediumcoreGhostInventories
{
    public class PlayerDeathInventory
    {
        public Item[] deathInventory;
        public Item[] deathArmor;
        public Item[] deathDye;
        public Item[] deathMiscEquips;
        public Item[] deathMiscDyes;
        public int numOfItems = 0;
        public string playerName;

        public PlayerDeathInventory(Item[] dInventory, Item[] dArmor, Item[] dDye, Item[] dMiscEquips, Item[] dMiscDye, string name)
        {
            deathInventory = dInventory;
            deathArmor = dArmor;
            deathDye = dDye;
            deathMiscEquips = dMiscEquips;
            deathMiscDyes = dMiscDye;

            playerName = name;

            GetNumOfItems();
        }

        private void GetNumOfItems()
        {
            CountItems(deathInventory);
            CountItems(deathArmor);
            CountItems(deathMiscEquips);
            CountItems(deathDye);
            CountItems(deathMiscDyes);
        }

        private void CountItems(Item[] items)
        {
            foreach (Item item in items)
            {
                if (item.Name != "")
                    numOfItems++;
            }
        }

        public void DropInventory(Rectangle rect)
        {
            DropItemArray(rect, deathInventory);
            DropItemArray(rect, deathArmor);
            DropItemArray(rect, deathMiscEquips);
            DropItemArray(rect, deathDye);
            DropItemArray(rect, deathMiscDyes);
        }

        private void DropItemArray(Rectangle rect, Item[] items)
        {
            foreach (Item item in items)
            {
                if (item.Name != "")
                    Item.NewItem(rect, item.netID, Stack: item.stack, prefixGiven: item.prefix);
            }
        }

        private void SendSpawnItem(Rectangle rect, Item item)
        {
            ModContent.GetInstance<MediumcoreGhostInventories>().Logger.Debug("sending spawn item");
            ModPacket packet = ModContent.GetInstance<MediumcoreGhostInventories>().GetPacket();

            packet.Write((byte)MediumcoreGhostInventories.MediumcoreGhostInventoriesMessageType.DropInventory);

            packet.Write(rect.X);
            packet.Write(rect.Y);
            packet.Write(rect.Height);
            packet.Write(rect.Width);

            packet.Write(item.netID);
            packet.Write(item.stack);
            packet.Write(item.prefix);

            packet.Send();
        }
    }
}
