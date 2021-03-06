﻿using SharpEnd.Drawing;
using SharpEnd.Maps;
using SharpEnd.Network;
using SharpEnd.Players;
using SharpEnd.Servers;
using System.Collections.Generic;

namespace SharpEnd.Handlers
{
    internal static class InventoryHandlers
    {
        [PacketHandler(EHeader.CMSG_INVENTORY_SORT)]
        public static void SortHandler(Client client, InPacket inPacket)
        {
            inPacket.ReadInt(); // NOTE: Ticks
        }

        [PacketHandler(EHeader.CMSG_INVENTORY_GATHER)]
        public static void GatherHandler(Client client, InPacket inPacket)
        {
            inPacket.ReadInt(); // NOTE: Ticks
        }

        [PacketHandler(EHeader.CMSG_INVENTORY_OPERATION)]
        public static void OperationHandler(Client client, InPacket inPacket)
        {
            var player = client.Player;

            inPacket.ReadInt(); // NOTE: Ticks
            EInventoryType inventory = (EInventoryType)inPacket.ReadByte();
            short sourceSlot = inPacket.ReadShort();
            short destinationSlot = inPacket.ReadShort();
            ushort quantity = inPacket.ReadUShort();

            PlayerItem item = player.Items[inventory, sourceSlot];

            if (item == null)
            {
                return;
            }

            if (destinationSlot < 0)
            {
                item.Equip();
            }
            else if (sourceSlot < 0 && destinationSlot > 0)
            {
                item.Unequip(destinationSlot);
            }
            else if (destinationSlot == 0)
            {
                item.Drop(quantity);
            }
            else
            {
                item.Move(destinationSlot);
            }
        }

        [PacketHandler(EHeader.CMSG_INVENTORY_MESO_DROP)]
        public static void MesoDropHandler(Client client, InPacket inPacket)
        {
            var player = client.Player;

            inPacket.ReadInt(); // NOTE: Ticks
            int amount = inPacket.ReadInt();

            if (amount < 10 || amount > 50000 || amount > player.Items.Meso)
            {
                return;
            }

            player.Items.ModifyMeso(-amount, true);

            Meso meso = new Meso(amount)
            {
                Dropper = player,
                Owner = null
            };

            player.Map.Drops.Add(meso);
        }

        [PacketHandler(EHeader.CMSG_INVENTORY_PICKUP)]
        public static void PickupHandler(Client client, InPacket inPacket)
        {
            var player = client.Player;

            inPacket.ReadInt(); // NOTE: Ticks
            inPacket.Skip(1);
            Point position = inPacket.ReadPoint();
            int objectIdentifier = inPacket.ReadInt();

            Drop drop;

            try
            {
                drop = player.Map.Drops[objectIdentifier];
            }
            catch (KeyNotFoundException)
            {
                return;
            }

            if (drop.Picker == null)
            {
                drop.Picker = player;

                if (drop is Meso)
                {
                    player.Items.ModifyMeso(((Meso)drop).Amount, true); // TODO: Check for max meso.
                }
                else if (drop is PlayerItem)
                {
                    ((PlayerItem)drop).Slot = player.Items.GetNextFreeSlot(((PlayerItem)drop).Inventory); // TODO: Check for full inventory.

                    player.Items.Add((PlayerItem)drop);
                }

                player.Map.Drops.Remove(drop);

                // TODO: Show gain packet.
            }
        }
    }
}
