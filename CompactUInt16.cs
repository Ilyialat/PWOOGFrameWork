using System;

namespace PWOOGFrameWork
{
    public struct CompactUInt16
        {
            private ushort value;

            public CompactUInt16(ushort value)
            {
                this.value = value;
            }
            public CompactUInt16(System.IO.MemoryStream ms)
            {
                byte bt1 = (byte)ms.ReadByte();

                if (bt1 >= 0x80) //0x8* => у нас двухбайтная длина
                {
                    bt1 -= 0x80;
                    byte bt2 = (byte)ms.ReadByte();
                    value = BitConverter.ToUInt16(new byte[] { bt2, bt1 }, 0);
                }
                else
                    value = bt1;
            }
            public CompactUInt16(byte[] bt)
            {
                byte bt1 = bt[0];

                if (bt1 >= 0x80) //0x8* => у нас двухбайтная длина
                {
                    bt1 -= 0x80;
                    byte bt2 = bt[1];
                    value = BitConverter.ToUInt16(new byte[] { bt2, bt1 }, 0);
                }
                else
                    value = bt1;
            }

            public byte[] GetBytes()
            {
                //используется после захода в игру
                if (value >= 0x80) //128
                {
                    byte bt1 = (byte)(value / 128 - 1);
                    byte bt2 = (byte)(value - 128 * bt1);
                    bt1 += 0x80;
                    return new byte[] { bt1, bt2 };
                }
                return new byte[] { (byte)value };
            }
            public byte[] GetFullBytes()
            {
                byte bt1, bt2;
                bt2 = 0;
                //используется после захода в игру
                if (value >= 0x80) //128
                {
                    bt1 = (byte)(value / 128 - 1);
                    bt2 = (byte)(value - 128 * bt1);
                    bt1 += 0x80;
                }
                else
                    bt1 = (byte)value;
                return new byte[] { bt1, bt2 };
            }

            public static implicit operator ushort(CompactUInt16 v)
            {
                return v.value;
            }
            public static implicit operator CompactUInt16(ushort v)
            {
                return new CompactUInt16(v);
            }
        }
}
