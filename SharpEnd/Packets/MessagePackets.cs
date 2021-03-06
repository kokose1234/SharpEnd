﻿using SharpEnd.Network;

namespace SharpEnd.Packets
{
    internal static class MessagePackets
    {
        public static byte[] Notification(string text, EMessageType type)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_NOTIFICATION)
                    .WriteByte((byte)type);

                if (type == EMessageType.Header)
                {
                    outPacket.WriteBoolean(!string.IsNullOrEmpty(text));
                }

                outPacket.WriteString(text);

                if (type == EMessageType.Blue)
                {
                    outPacket.WriteInt();
                }

                return outPacket.ToArray();
            }
        }

        public static byte[] YellowMessage(string text)
        {
            using (OutPacket outPacket = new OutPacket())
            {
                outPacket
                    .WriteHeader(EHeader.SMSG_YELLOW_MESSAGE)
                    .WriteString(text);

                return outPacket.ToArray();
            }
        }
    }
}
