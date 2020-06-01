using Terraria.ModLoader;
using System.IO;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace MediumcoreGhostInventories
{
    public class MediumcoreGhostInventories : Mod
    {
        public override void HandlePacket(BinaryReader reader, int fromWho)
        {
            Dictionary<Point, PlayerDeathInventory> playerDeathInventoryMap = ModContent.GetInstance<MediumcoreGhostInventoriesWorld>().playerDeathInventoryMap;
            MediumcoreGhostInventoriesMessageType msgType = (MediumcoreGhostInventoriesMessageType)reader.ReadByte();
            
            switch(msgType)
            {
                case MediumcoreGhostInventoriesMessageType.KillNPC:
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();

                    playerDeathInventoryMap.Remove(new Point(x, y));

                    if (Main.netMode == NetmodeID.Server)
                        SendKill(fromWho, x, y);

                    break;
                case MediumcoreGhostInventoriesMessageType.DropInventory:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Point position = new Point(reader.ReadInt32(), reader.ReadInt32());
                        Rectangle rect = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

                        if (playerDeathInventoryMap.ContainsKey(position))
                            playerDeathInventoryMap[position].DropInventory(rect);
                        else
                            Logger.Warn($"MediumcoreGhostInventoriesMod: Inventory not found: {position}");
                    }
                    else
                        Logger.Warn("Received server packet - ignore");

                    break;
                case MediumcoreGhostInventoriesMessageType.SpawnNPC:
                    int positionX = reader.ReadInt32();
                    int positionY = reader.ReadInt32();

                    NPC.NewNPC(positionX, positionY, NPCType("GhostInventory"), ai0: positionX, ai1: positionY);

                    break;
                case MediumcoreGhostInventoriesMessageType.SetFavourites:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Point deathPosition = new Point(reader.ReadInt32(), reader.ReadInt32());
                        Logger.Debug($"deathPosition {deathPosition}");

                        List<int> favouritedItems = new List<int>();

                        int currentIndex = reader.ReadInt32();

                        while (currentIndex != 100)
                        {
                            favouritedItems.Add(currentIndex);
                            currentIndex = reader.ReadInt32();
                        }

                        Task waitForInventory = Task.Run(() => {
                            while (!playerDeathInventoryMap.ContainsKey(deathPosition))
                                Thread.Sleep(20);

                            foreach (int i in favouritedItems)
                                ModContent.GetInstance<MediumcoreGhostInventoriesWorld>().playerDeathInventoryMap[deathPosition].deathInventory[i].favorited = true;

                            NetMessage.SendData(MessageID.WorldData);
                        });
                        break;
                    }
                    else
                        Logger.Warn("Received server packet - ignore");

                    break;
                default:
                    Logger.WarnFormat($"MediumcoreGhostInventoriesMod: Unknown Message type: {msgType}");
                    break;
            }
        }

        private void SendKill(int killer, int x, int y)
        {
            ModPacket packet = GetPacket();
            packet.Write((byte)MediumcoreGhostInventoriesMessageType.KillNPC);
            packet.Write(x);
            packet.Write(y);
            packet.Send(ignoreClient:killer);
        }

        internal enum MediumcoreGhostInventoriesMessageType : byte
        {
            KillNPC,
            DropInventory,
            SpawnNPC,
            SetFavourites
        }
    }
}