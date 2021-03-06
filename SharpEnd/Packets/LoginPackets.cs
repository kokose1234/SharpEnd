﻿using MySql.Data.MySqlClient;
using SharpEnd.Network;
using SharpEnd.Servers;
using SharpEnd.Utility;
using System;
using System.Collections.Generic;

namespace SharpEnd.Packets
{
    internal static class LoginPackets
    {
        public static byte[] Handshake(byte[] riv, byte[] siv)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteUShort(15)
                    .WriteUShort(Application.Version.Version)
                    .WriteString(Application.Version.Patch)
                    .WriteBytes(riv)
                    .WriteBytes(siv)
                    .WriteByte((byte)Application.Version.Localisation)
                    .WriteByte();

                return outPacket.ToArray();
            }
        }



        public static byte[] PrivateServerAuth(int response)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_PRIVATE_SERVER_AUTH)
                    .WriteInt(response);

                return outPacket.ToArray();
            }
        }

        public static byte[] Start()
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_START)
                    .WriteBoolean(true);

                return outPacket.ToArray();
            }
        }

        public static byte[] AuthServer(bool enable)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_AUTH_SERVER)
                    .WriteBoolean(enable);

                return outPacket.ToArray();
            }
        }

        public static byte[] LoginError(uint type)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_AUTHENTICATION)
                    .WriteUInt(type)
                    .WriteByte()
                    .WriteByte();

                return outPacket.ToArray();
            }
        }

        private enum PrivateStatusIDFlag : uint
        {
            PS_PrimaryTrace = 0x1,
            PS_SecondaryTrace = 0x2,
            PS_AdminClient = 0x4,
            PS_MobMoveObserve = 0x8,
            PS_ManagerAccount = 0x10,
            PS_OutSourceSuperGM = 0x20,
            PS_UserGM = 0x80,
            PS_TesterAccount = 0x100,
            PS_SubTesterAccount = 0x200,
        }

        public static byte[] LoginSuccess(Client client)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_AUTHENTICATION)
                    .WriteUInt()
                    .WriteByte()
                    .WriteByte()
                    .WriteString(client.Account.Username)
                    .WriteInt(client.Account.Identifier);

                byte admin = 1, tradeBlock = 0;

                byte gradeCode = (byte)(admin << 1 | tradeBlock << 4 | 0);

                outPacket.WriteByte(gradeCode);

                bool manager = true, tester = true, subTester = true;

                uint privateStatusFlag = 0;

                if (admin == 1) privateStatusFlag += (uint)PrivateStatusIDFlag.PS_AdminClient;
                if (manager) privateStatusFlag += (uint)PrivateStatusIDFlag.PS_ManagerAccount;
                if (tester) privateStatusFlag += (uint)PrivateStatusIDFlag.PS_TesterAccount;
                if (subTester) privateStatusFlag += (uint)PrivateStatusIDFlag.PS_SubTesterAccount;

                outPacket.WriteUInt(privateStatusFlag);

                outPacket
                    .WriteZero(6)
                    .WriteString(client.Account.Username)
                    .WriteHexString("03 00 00 00 00 00 00 00 00 00 30 58 67 CF E4 F3 CA 01 3D 00 00 00 01 08 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 00 01 00 01 01 00 01 01 00 01 01 00 01 01 00 00 01 00 00 01 00 01 01 00 00 FF FF FF FF 01 04")
                    .WriteLong(client.Identifier);

                return outPacket.ToArray();
            }
        }

        public static byte[] WorldInformation(WorldServer world)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_WORLD_INFORMATION)
                    .WriteByte(world.Identifier)
                    .WriteString("Scania")
                    .WriteByte()
                    .WriteString("Welcome to SharpEnd.")
                    .WriteShort(100)
                    .WriteShort(100)
                    .WriteByte()
                    .WriteByte(1);

                for (byte i = 0; i < 1; i++)
                {
                    outPacket
                        .WriteString($"{"Scania"}-{i}")
                        .WriteInt(1)
                        .WriteByte(world.Identifier)
                        .WriteByte(i)
                        .WriteBoolean(false); // NOTE: Adult channel.
                }

                outPacket
                    .WriteShort()
                    .WriteInt()
                    .WriteByte();

                return outPacket.ToArray();
            }
        }

        public static byte[] WorldEnd()
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_WORLD_INFORMATION)
                    .WriteSByte(-1)
                    .WriteBoolean(false)
                    .WriteBoolean(false)
                    .WriteBoolean(false);

                return outPacket.ToArray();
            }
        }

        public static byte[] WorldStatus(short status)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_WORLD_STATUS)
                    .WriteShort(status);

                return outPacket.ToArray();
            }
        }

        public static byte[] PlayerList(byte count, DatabaseQuery query, EPICState picState)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_PLAYER_LIST)
                    .WriteByte()
                    .WriteString("normal")
                    .WriteInt()
                    .WriteByte(1)
                    .WriteInt()
                    .WriteLong()
                    .WriteByte()
                    .WriteInt();

                outPacket.WriteByte(count);

                while (query.NextRow())
                {
                    AddPlayerEntry(outPacket, query);

                    outPacket
                        .WriteByte()
                        .WriteBoolean(false); // NOTE: Rankings
                }

                outPacket
                    .WriteByte((byte)picState)
                    .WriteByte()
                    .WriteInt(9)
                    .WriteInt()
                    .WriteInt(-1)
                    .WriteLong()
                    .WriteByte()
                    .WriteZero(5);

                return outPacket.ToArray();
            }
        }

        private static void AddPlayerEntry(OutPacket outPacket, DatabaseQuery query)
        {
            // NOTE: Statistics
            outPacket
                .WriteInt(query.Get<int>("identifier"))
                .WriteInt(query.Get<int>("identifier"))
                .WriteInt()
                .WriteString(query.Get<string>("name"), 13)
                .WriteByte(query.Get<byte>("gender"))
                .WriteByte(query.Get<byte>("skin"))
                .WriteInt(query.Get<int>("face"))
                .WriteInt(query.Get<int>("hair"))
                .WriteSByte(-1) // NOTE: nMixBaseHairColor
                .WriteSByte() // NOTE: nMixAddHairColor
                .WriteSByte() // NOTE: nMixHairBaseProb
                .WriteByte(query.Get<byte>("level"))
                .WriteUShort(query.Get<ushort>("job"))
                .WriteUShort(query.Get<ushort>("strength"))
                .WriteUShort(query.Get<ushort>("dexterity"))
                .WriteUShort(query.Get<ushort>("intelligence"))
                .WriteUShort(query.Get<ushort>("luck"))
                .WriteUInt(query.Get<uint>("health"))
                .WriteUInt(query.Get<uint>("max_health"))
                .WriteUInt(query.Get<uint>("mana"))
                .WriteUInt(query.Get<uint>("max_mana"))
                .WriteUShort(query.Get<ushort>("ability_points"));

            if (GameLogicUtilities.HasSeparatedSkillPoints(query.Get<ushort>("job")))
            {
                outPacket.WriteByte(); // NOTE: Does it really matter?
            }
            else
            {
                outPacket.WriteUShort(query.Get<ushort>("skill_points"));
            }

            outPacket
                .WriteULong(query.Get<ulong>("experience"))
                .WriteInt(query.Get<int>("fame"))
                .WriteInt()
                .WriteInt()
                .WriteInt(query.Get<int>("map_identifier"))
                .WriteByte(query.Get<byte>("map_spawn"))
                .WriteInt() // NOTE: Unknown
                .WriteUShort(query.Get<ushort>("sub_job"))
                .WriteByte() // NOTE: Fatigue
                .WriteInt(); // NOTE: Date

            // NOTE: Traits
            {
                int count = 6;

                while (count-- > 0)
                {
                    outPacket.WriteInt();
                }

                count = 6;

                while (count-- > 0)
                {
                    outPacket.WriteShort();
                }
            }

            outPacket
                .WriteByte() // NOTE: Unknown
                .WriteLong() // NOTE: Unknown
                .WriteInt() // NOTE: Battle experience
                .WriteByte() // NOTE: Battle rank
                .WriteInt() // NOTE: Battle points
                .WriteByte(5) // NOTE: Battle mode level
                .WriteByte(6) // NOTE: Battle mode type
                .WriteInt(); // NOTE: Event points

            // NOTE: Part Time Job
            {
                outPacket
                    .WriteByte() // NOTE: Job
                    .WriteLong() // NOTE: Time left
                    .WriteInt() // NOTE: Reward/duration
                    .WriteBoolean(false); // NOTE: Finished
            }

            // NOTE: Cards
            {
                int count = 9;

                while (count-- > 0)
                {
                    outPacket
                        .WriteInt()
                        .WriteByte()
                        .WriteInt();
                }
            }

            outPacket
                .WriteLong() // NOTE: Last login date
                .WriteBoolean(false); // NOTE: Burning event

            // NOTE: Appereance
            outPacket
                .WriteByte(query.Get<byte>("gender"))
                .WriteByte(query.Get<byte>("skin"))
                .WriteInt(query.Get<int>("face"))
                .WriteInt(query.Get<ushort>("job"))
                .WriteBoolean(true)
                .WriteInt(query.Get<int>("hair"));

            SortedDictionary<byte, int> visibleLayer = new SortedDictionary<byte, int>();
            SortedDictionary<byte, int> hiddenLayer = new SortedDictionary<byte, int>();
            SortedDictionary<byte, int> totemLayer = new SortedDictionary<byte, int>();

            using (DatabaseQuery equipmentQuery = Database.Query("SELECT item_identifier,inventory_slot FROM player_item WHERE player_identifier=@player_identifier AND inventory_slot<0", new MySqlParameter("@player_identifier", query.Get<int>("identifier"))))
            {
                while (equipmentQuery.NextRow())
                {
                    int itemIdentifier = equipmentQuery.Get<int>("item_identifier");
                    short inventorySlot = Math.Abs(equipmentQuery.Get<short>("inventory_slot"));

                    if (inventorySlot > 100)
                    {
                        inventorySlot -= 100;
                    }

                    visibleLayer.Add((byte)inventorySlot, itemIdentifier);
                }
            }

            foreach (KeyValuePair<byte, int> entry in visibleLayer)
            {
                outPacket
                    .WriteByte(entry.Key)
                    .WriteInt(entry.Value);
            }
            outPacket.WriteSByte(-1);

            foreach (KeyValuePair<byte, int> entry in hiddenLayer)
            {
                outPacket
                    .WriteByte(entry.Key)
                    .WriteInt(entry.Value);
            }
            outPacket.WriteSByte(-1);

            foreach (KeyValuePair<byte, int> entry in totemLayer)
            {
                outPacket
                    .WriteByte(entry.Key)
                    .WriteInt(entry.Value);
            }
            outPacket.WriteSByte(-1);

            outPacket
                .WriteInt(hiddenLayer.GetOrDefault((byte)11, 0))
                .WriteInt(visibleLayer.GetOrDefault((byte)11, 0))
                .WriteInt(visibleLayer.GetOrDefault((byte)15, 0)) // TODO: Find the correct slot
                .WriteBoolean(false) // NOTE: Elf ears
                .WriteInt() // NOTE: Pet 1
                .WriteInt() // NOTE: Pet 2
                .WriteInt() // NOTE: Pet 3
                .WriteByte() // NOTE: Mixed hair color
                .WriteByte(); // NOTE: Mixed hair percent
        }

        public static byte[] PlayerNameCheck(string name, bool unusable)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_PLAYER_NAME_CHECK)
                    .WriteString(name)
                    .WriteBoolean(unusable);

                return outPacket.ToArray();
            }
        }

        public static byte[] PlayerCreate(DatabaseQuery query)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_PLAYER_CREATE)
                    .WriteBoolean(false);

                AddPlayerEntry(outPacket, query);

                return outPacket.ToArray();
            }
        }

        private static readonly byte[] channelIP = new byte[4] { 8, 31, 99, 141 };
        private static readonly byte[] chatIP = new byte[4] { 8, 31, 99, 133 };

        public static byte[] ServerIP(ushort port, int playerIdentifier)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_SERVER_IP)
                    .WriteByte()
                    .WriteByte()
                    .WriteBytes(channelIP)
                    .WriteUShort(port)
                    .WriteBytes(chatIP)
                    .WriteUShort(port)
                    .WriteInt(playerIdentifier)
                    .WriteByte()
                    .WriteInt()
                    .WriteByte()
                    .WriteLong();

                return outPacket.ToArray();
            }
        }
    }
}
