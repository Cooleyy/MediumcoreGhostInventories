using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.IO;
using System.Collections.Generic;

namespace MediumcoreGhostInventories.NPCs
{
    class GhostInventory : ModNPC
    {
        public PlayerDeathInventory storedInventory;
        private Point position;
        private bool attemptedToStoreInventory = false;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghost");
            Main.npcFrameCount[npc.type] = 4;
        }

        public override void SetDefaults()
        {
            npc.width = 30;
            npc.height = 24;
            npc.lifeMax = 100;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.friendly = true;
            npc.npcSlots = 0;
            npc.aiStyle = -1;
            //npc.netAlways = true;
            npc.dontTakeDamage = true;
            npc.immortal = true;
            npc.alpha = 50;
            npc.velocity = Vector2.Zero;
            npc.dontTakeDamageFromHostiles = true;
            npc.dontCountMe = true;
        }

        //Make sure NPC cannot die
        //public override bool CheckDead() => false;

        //Make sure does not despawn
        public override bool CheckActive() => false;

        public override bool CanChat() => true;
        
        public override void AI()
        {
            Lighting.AddLight(npc.position, 0.25F, 0.25F, 0.25F);

            //Get the inventory and corresponding player death position to be restored
            //The player death position is passed through the ai0 and ai1 parameters on the NewNPC method.
            // ai0 = deathPosition.X 
            // ai1 = deathPosition.Y
            if (!attemptedToStoreInventory)
            {
                position = new Point((int)npc.ai[0], (int)npc.ai[1]);
                npc.position = new Vector2(npc.ai[0], npc.ai[1]);
                mod.Logger.Debug($"ghost position - {position}");

                if (ModContent.GetInstance<MediumcoreGhostInventoriesWorld>().playerDeathInventoryMap.ContainsKey(position))
                    storedInventory = ModContent.GetInstance<MediumcoreGhostInventoriesWorld>().playerDeathInventoryMap[position];

                attemptedToStoreInventory = true;
            }
            //If inventory was attempted to be loaded and the inventory dict no longer contains the corresponding inventory, then kill this npc
            else if (!ModContent.GetInstance<MediumcoreGhostInventoriesWorld>().playerDeathInventoryMap.ContainsKey(position))
            {
                mod.Logger.Debug($"Killing ghost: no inventory - {position}");
                npc.active = false;
            }
        }

        public override string GetChat()
        {
            if (Main.player[Main.myPlayer].name == storedInventory.playerName)
            {
                return $"Hi {storedInventory.playerName} good to see you again...";
            }
            else
                return $"I am the ghost of {storedInventory.playerName}";
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            if (Main.player[Main.myPlayer].name == storedInventory.playerName)
            {
                button = $"Retrieve {storedInventory.numOfItems} items";
            }
            button2 = "Drop all items";
        }

        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            //First button = give inventory
            if (firstButton)
            {
                Player player = Main.player[Main.myPlayer];
                player.DropItems();
                GivePlayerInventoryBack(player);
            }
            //Second button = drop stored inventory
            else
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    SendDropInventory();
                else  
                    storedInventory.DropInventory(npc.getRect());
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                SendKill();

            mod.Logger.Debug($"Killing ghost: button clicked - {position}");
            npc.active = false;
            ModContent.GetInstance<MediumcoreGhostInventoriesWorld>().playerDeathInventoryMap.Remove(position);
        }

        //Send packet to the server to tell it to drop this npcs corresponding inventory on the ground
        private void SendDropInventory()
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)MediumcoreGhostInventories.MediumcoreGhostInventoriesMessageType.DropInventory);

            packet.Write(position.X);
            packet.Write(position.Y);

            Rectangle rect = npc.getRect();
            packet.Write(rect.X);
            packet.Write(rect.Y);
            packet.Write(rect.Height);
            packet.Write(rect.Width);

            packet.Send();
        }

        //Send packet to server to tell it to kill this npc and remove its corresponding inventory from the world
        private void SendKill()
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)MediumcoreGhostInventories.MediumcoreGhostInventoriesMessageType.KillNPC);

            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Send();
        }

        private void GivePlayerInventoryBack(Player player)
        {
            player.inventory = storedInventory.deathInventory;
            player.armor = storedInventory.deathArmor;
            player.dye = storedInventory.deathDye;
            player.miscEquips = storedInventory.deathMiscEquips;
            player.miscDyes = storedInventory.deathMiscDyes;
        }

        public override void FindFrame(int frameHeight)
        {
            //every 12th frame chose next sprite frame
            if (npc.frameCounter++ >= 12)
            {
                npc.frameCounter = 0;
                if ((npc.frame.Y += frameHeight) >= 4 * frameHeight)
                {
                    npc.frame.Y = 0;
                }
            }
        }
    }
}
