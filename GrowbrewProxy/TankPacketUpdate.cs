// thanks to iProgramInCpp#0001, most things are made by him in the GrowtopiaCustomClient, I have just rewritten it into c# and maybe also improved. -playingo
// edit: this is ACTUALLY not rewritten and straight taken from iProgramInCpp#0001!
using System;
using System.Collections.Generic;

namespace GrowbrewProxy
{
    internal class TankPacket
    {
        public int PacketType;
        public int NetID;
        public int SecondaryNetID;
        public int ExtDataMask;
        public int CharacterState => ExtDataMask;
        public float Padding;
        public int MainValue;
        public int TilePlaced => MainValue;
        public float X, Y;
        public float XSpeed, YSpeed;
        public int SecondaryPadding;
        public int PunchX, PunchY;
        public int ExtDataSize => ExtData.Count;
        public List<byte> ExtData = new();
        public byte[] ExtData_Alt;

        public byte[] PackForSendingRaw()
        {
            byte[] b = new byte[57 + ExtDataSize]; // append an extra byte to avoid errors
            Array.Copy(BitConverter.GetBytes(PacketType), b, 4);
            Array.Copy(BitConverter.GetBytes(NetID), 0, b, 4, 4);
            Array.Copy(BitConverter.GetBytes(SecondaryNetID), 0, b, 8, 4);
            Array.Copy(BitConverter.GetBytes(ExtDataMask), 0, b, 12, 4);
            Array.Copy(BitConverter.GetBytes(Padding), 0, b, 16, 4);
            Array.Copy(BitConverter.GetBytes(MainValue), 0, b, 20, 4);
            Array.Copy(BitConverter.GetBytes(X), 0, b, 24, 4);
            Array.Copy(BitConverter.GetBytes(Y), 0, b, 28, 4);
            Array.Copy(BitConverter.GetBytes(XSpeed), 0, b, 32, 4);
            Array.Copy(BitConverter.GetBytes(YSpeed), 0, b, 36, 4);
            Array.Copy(BitConverter.GetBytes(SecondaryPadding), 0, b, 40, 4);
            Array.Copy(BitConverter.GetBytes(PunchX), 0, b, 44, 4);
            Array.Copy(BitConverter.GetBytes(PunchY), 0, b, 48, 4);
            Array.Copy(BitConverter.GetBytes(ExtDataSize), 0, b, 52, 4);
            byte[] dat = ExtData.ToArray();
            int datLength = dat.Length;
            if (datLength > 0)
            {
                Buffer.BlockCopy(dat, 0, b, 56, datLength);
            }

            return b;
        }

        public byte[] PackForSendingAsPacket()
        {
            byte[] m = PackForSendingRaw();
            byte[] s = new byte[m.Length + 4];
            Array.Copy(m, 0, s, 4, m.Length);
            return s;
        }

        public static TankPacket Unpack(byte[] data)
        {
            TankPacket dataStruct = new()
            {
                // should/must contain these...
                PacketType = BitConverter.ToInt32(data, 0),
                NetID = BitConverter.ToInt32(data, 4),
                SecondaryNetID = BitConverter.ToInt32(data, 8),
                ExtDataMask = BitConverter.ToInt32(data, 12),
                Padding = BitConverter.ToInt32(data, 16),
                MainValue = BitConverter.ToInt32(data, 20),
                X = BitConverter.ToSingle(data, 24),
                Y = BitConverter.ToSingle(data, 28),
                XSpeed = BitConverter.ToSingle(data, 32),
                YSpeed = BitConverter.ToSingle(data, 36),
                SecondaryPadding = BitConverter.ToInt32(data, 40),
                PunchX = BitConverter.ToInt32(data, 44),
                PunchY = BitConverter.ToInt32(data, 48)
            };

            // this is very i might be unsure...
            //int len = BitConverter.ToInt32(data, 52);
            // dataStruct.ExtData_Alt = new byte[len];
            //Array.Copy(data, 56, dataStruct.ExtData_Alt, 0, len);



            return dataStruct;
        }

        public static TankPacket UnpackFromPacket(byte[] p)
        {
            TankPacket packet = new();
            if (p.Length >= 48)
            {
                byte[] s = new byte[p.Length - 4];
                Array.Copy(p, 4, s, 0, s.Length);
                packet = Unpack(s);
            }
            return packet;
        }
    }
}
