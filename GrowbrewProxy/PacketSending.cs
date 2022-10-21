﻿// thanks to iProgramInCpp#0489, most things are made by him in the GrowtopiaCustomClient, I have just rewritten it into c# and maybe also improved. -playingo
using ENet.Managed;
using System;
using System.Text;

namespace GrowbrewProxy
{
    public class PacketSending
    {
        private readonly Random rand = new();
        public void SendData(byte[] data, ENetPeer peer, ENetPacketFlags flag = ENetPacketFlags.Reliable)
        {
            if (peer == null)
            {
                return;
            }

            if (peer.IsNull)
            {
                return;
            }

            if (peer.State != ENetPeerState.Connected)
            {
                return;
            }

            peer.Send((byte)rand.Next(0, 1), data, flag);

        }

        public void SendPacketRaw(int type, byte[] data, ENetPeer peer, ENetPacketFlags flag = ENetPacketFlags.Reliable)
        {
            if (peer == null)
            {
                return;
            }

            if (peer.IsNull)
            {
                return;
            }

            if (peer.State != ENetPeerState.Connected)
            {
                return;
            }

            byte[] packetData = new byte[data.Length + 5];
            Array.Copy(BitConverter.GetBytes(type), packetData, 4);
            Array.Copy(data, 0, packetData, 4, data.Length);
            SendData(packetData, peer);
        }

        public void SendPacket(int type, string str, ENetPeer peer, ENetPacketFlags flag = ENetPacketFlags.Reliable)
        {
            SendPacketRaw(type, Encoding.ASCII.GetBytes(str.ToCharArray()), peer);
        }

        public void SecondaryLogonAccepted(ENetPeer peer)
        {
            SendPacket((int)NetTypes.NetMessages.GENERIC_TEXT, string.Empty, peer);
        }

        public void InitialLogonAccepted(ENetPeer peer)
        {
            SendPacket((int)NetTypes.NetMessages.SERVER_HELLO, string.Empty, peer);
        }
    }
}
