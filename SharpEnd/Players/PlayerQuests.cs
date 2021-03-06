﻿using SharpEnd.Network;
using SharpEnd.Utility;

namespace SharpEnd.Players
{
    internal sealed class PlayerQuests
    {
        private Player m_player;

        public PlayerQuests(Player player, DatabaseQuery query)
        {
            m_player = player;
        }

        public void Save()
        {

        }

        public void WriteInitial(OutPacket outPacket)
        {
            // NOTE: Started
            {
                outPacket
                    .WriteBoolean(true)
                    .WriteShort();
            }

            // NOTE: NX
            {
                outPacket.WriteShort();
            }

            // NOTE: Completed
            outPacket
                .WriteBoolean(true)
                .WriteShort();
        }

        public void Start(ushort questIdentifier, int npcIdentifier)
        {
            // TODO: Distribute rewards

            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_SHOW_LOG)
                    .WriteSByte(1)
                    .WriteInt(questIdentifier)
                    .WriteBoolean(true)
                    .WriteString(string.Empty);

                m_player.Send(outPacket.ToArray());
            }

            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_QUEST)
                    .WriteSByte(0x0B)
                    .WriteInt(questIdentifier)
                    .WriteInt(npcIdentifier)
                    .WriteHexString("00 00 00 00 01");

                m_player.Send(outPacket.ToArray());
            }
        }
    }
}
