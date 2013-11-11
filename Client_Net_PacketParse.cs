using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PWOOGFrameWork
{
    public partial class PWClient
    {
        private class PWStream
        {
            private MemoryStream ms;
            public ushort Header { get; private set; }
            public bool IsContainer { get; private set; }
            public long Position { get { return ms.Position; ; } }
            public long Length { get { return ms.Length; } }

            public static PWStream[] FromBuff(byte[] buff)
            {
                List<PWStream> result = new List<PWStream>();

                ushort length;
                for (int i = 0; i < buff.Length; i += length)
                {
                    ushort header;
                    int len = 4;
                    int _len = buff.Length - i;
                    if (_len < len) //Не хватает места
                        len = _len;
                    using (MemoryStream ms = new MemoryStream(buff, i, len))
                    {
                        header = new CompactUInt16(ms);
                        length = new CompactUInt16(ms);
                        i += (int)ms.Position;
                    }

                    PWStream pw = new PWStream(buff, i, length);
                    pw.Header = header;
                    pw.IsContainer = false;
                    result.Add(pw);
                }
                return result.ToArray();
            }
            public static PWStream[] FromContainer(PWStream c) //Container
            {
                c.IsContainer = true;
                List<PWStream> result = new List<PWStream>();
                ushort length;
                for (; c.Position < c.Length; )
                {
                    byte _header = c.ReadByte();
                    if (_header != 0x22) //Не ешё один контейнер
                        throw new ArgumentException();

                    length = c.ReadFullCompactUInt16();
                    if (length < 0x80)
                        length -= 1;
                    else
                    {
                        c.ReadByte(2); //мусор
                        length -= 2;
                    }

                    ushort header = c.ReadUInt16();

                    length -= 2; //ushort = 2bt

                    PWStream pw = new PWStream(c.ms, length);
                    pw.Header = header;
                    pw.IsContainer = true;
                    result.Add(pw);
                }
                return result.ToArray();
            }

            public PWStream(MemoryStream ms, int length)
            {
                byte[] pkt = new byte[length];
                ms.Read(pkt, 0, pkt.Length);
                this.ms = new MemoryStream(pkt, 0, pkt.Length);
            }
            public PWStream(byte[] bt, int offset, int length)
            {
                this.ms = new MemoryStream(bt, offset, length);
            }

            //Добрая память... =(
            /*public T Read<T>()
            {
                Type t = typeof(T);
                object result = null;

                if (t == typeof(byte))
                {
                    result = (byte)ms.ReadByte();
                }
                else if (t == typeof(ushort))
                {
                    byte[] bt = new byte[2];
                    ms.Read(bt, 0, bt.Length);
                    if (!IsContainer)
                        Array.Reverse(bt);
                    result = BitConverter.ToUInt16(bt, 0);
                }
                else if (t == typeof(uint))
                {
                    byte[] bt = new byte[4];
                    ms.Read(bt, 0, bt.Length);
                    if (!IsContainer)
                        Array.Reverse(bt);
                    result = BitConverter.ToUInt32(bt, 0);
                }
                else if (t == typeof(int))
                {
                    byte[] bt = new byte[4];
                    ms.Read(bt, 0, bt.Length);
                    if (!IsContainer)
                        Array.Reverse(bt);
                    result = BitConverter.ToInt32(bt, 0);
                }
                else if (t == typeof(float))
                {
                    byte[] bt = new byte[4];
                    ms.Read(bt, 0, bt.Length);
                    if (!IsContainer)
                        Array.Reverse(bt);
                    result = BitConverter.ToSingle(bt, 0);
                }

                return (T)result;
            }
            public T[] Read<T>(int count)
            {
                T[] result = new T[count];
                for (int i = 0; i < count; i++)
                    result[i] = Read<T>();
                return result;
            }*/
            public Byte ReadByte()
            {
                return (Byte)ms.ReadByte();
            }
            public Byte[] ReadByte(int count)
            {
                Byte[] result = new Byte[count];
                for (int i = 0; i < count; i++)
                    result[i] = ReadByte();
                return result;
            }
            public UInt16 ReadUInt16()
            {
                byte[] bt = new byte[2];
                ms.Read(bt, 0, bt.Length);
                if (!IsContainer)
                    Array.Reverse(bt);
                return BitConverter.ToUInt16(bt, 0);
            }
            public UInt16[] ReadUInt16(int count)
            {
                UInt16[] result = new UInt16[count];
                for (int i = 0; i < count; i++)
                    result[i] = ReadUInt16();
                return result;
            }
            public UInt32 ReadUInt32()
            {
                byte[] bt = new byte[4];
                ms.Read(bt, 0, bt.Length);
                if (!IsContainer)
                    Array.Reverse(bt);
                return BitConverter.ToUInt32(bt, 0);
            }
            public UInt32[] ReadUInt32(int count)
            {
                UInt32[] result = new UInt32[count];
                for (int i = 0; i < count; i++)
                    result[i] = ReadUInt32();
                return result;
            }
            public Single ReadSingle()
            {
                byte[] bt = new byte[4];
                ms.Read(bt, 0, bt.Length);
                if (!IsContainer)
                    Array.Reverse(bt);
                return BitConverter.ToSingle(bt, 0);
            }
            public Single[] ReadSingle(int count)
            {
                Single[] result = new Single[count];
                for (int i = 0; i < count; i++)
                    result[i] = ReadSingle();
                return result;
            }


            //Made in China
            public ushort ReadCompactUInt16()
            {
                return new CompactUInt16(ms);
            }
            public ushort ReadFullCompactUInt16()
            {
                byte[] bt = ReadByte(2);
                return new CompactUInt16(bt);
            }
            //China_end
            public string ReadAString(int length)
            {
                byte[] bt = new byte[length];
                ms.Read(bt, 0, bt.Length);
                return Encoding.GetEncoding(1251).GetString(bt, 0, bt.Length);
            }
            public string ReadUString(int length)
            {
                byte[] bt = new byte[length];
                ms.Read(bt, 0, bt.Length);
                return Encoding.Unicode.GetString(bt, 0, bt.Length);
            }
            public string ReadFullUString()
            {
                List<byte> uStr = new List<byte>();
                for (byte[] bt = ReadByte(2); !(bt[0] == 0 && bt[1] == 0); bt = ReadByte(2)) uStr.AddRange(bt);
                return Encoding.Unicode.GetString(uStr.ToArray());
            }
        }
        private struct p_SPacket
        {
            public byte[] Packet
            {
                get
                {
                    List<byte> result = new List<byte>(packet);
                    if (!IsToContainer)
                    {
                        result.InsertRange(0, ((CompactUInt16)result.Count).GetBytes());
                        result.InsertRange(0, ((CompactUInt16)Head).GetBytes());
                    }
                    else
                    {
                        result.InsertRange(0, ((CompactUInt16)Head).GetFullBytes());
                        result.InsertRange(0, ((CompactUInt16)result.Count).GetBytes());
                    }
                    return result.ToArray();
                }
            }
            private List<byte> packet;
            public ushort Head { get; private set; }
            public bool IsToContainer { get; private set; }

            public p_SPacket(ushort head, bool isToContainer = true)
                : this()
            {
                packet = new List<byte>();
                this.Head = head;
                this.IsToContainer = isToContainer;
            }

            //Светлая память... =(((
            /*public void Add<T>(T toAdd)
            {
                Type t = typeof(T);
                object obj = (object)toAdd;

                if (t == typeof(byte))
                {
                    packet.Add((byte)obj);
                }
                else if (t == typeof(ushort))
                {
                    byte[] bts = BitConverter.GetBytes((ushort)obj);
                    if (!IsToContainer)
                        Array.Reverse(bts);
                    packet.AddRange(bts);
                }
                else if (t == typeof(uint))
                {
                    byte[] bts = BitConverter.GetBytes((uint)obj);
                    if (!IsToContainer)
                        Array.Reverse(bts);
                    packet.AddRange(bts);
                }
                else if (t == typeof(int))
                {
                    byte[] bts = BitConverter.GetBytes((int)obj);
                    if (!IsToContainer)
                        Array.Reverse(bts);
                    packet.AddRange(bts);
                }
                else if (t == typeof(float))
                {
                    byte[] bts = BitConverter.GetBytes((float)obj);
                    if (!IsToContainer)
                        Array.Reverse(bts);
                    packet.AddRange(bts);
                }
                else if (t == typeof(byte[]))
                {
                    packet.AddRange((byte[])obj);
                }
            }*/
            public void AddByte(Byte value)
            {
                packet.Add(value);
            }
            public void AddByte(Byte[] value)
            {
                packet.AddRange(value);
            }
            public void AddUInt16(UInt16 value)
            {
                byte[] bt = BitConverter.GetBytes(value);
                if (!IsToContainer)
                    Array.Reverse(bt);
                packet.AddRange(bt);
            }
            public void AddUInt32(UInt32 value)
            {
                byte[] bt = BitConverter.GetBytes(value);
                if (!IsToContainer)
                    Array.Reverse(bt);
                packet.AddRange(bt);
            }
            public void AddSingle(Single value)
            {
                byte[] bt = BitConverter.GetBytes(value);
                if (!IsToContainer)
                    Array.Reverse(bt);
                packet.AddRange(bt);
            }

            public void AddAString(string str)
            {
                byte[] aStr = Encoding.GetEncoding(1251).GetBytes(str);
                packet.AddRange(((CompactUInt16)aStr.Length).GetBytes());
                packet.AddRange(aStr);
            }
            public void AddUString(string str)
            {
                byte[] uStr = Encoding.Unicode.GetBytes(str);
                packet.AddRange(((CompactUInt16)uStr.Length).GetBytes());
                packet.AddRange(uStr);
            }
            public void AddFullUString(string str)
            {
                List<byte> uStr = new List<byte>();
                uStr.AddRange(Encoding.Unicode.GetBytes(str));
                uStr.AddRange(new byte[] { 0, 0 });
                uStr.AddRange(RandomUtil.NextBytes(62 - uStr.Count));
                packet.AddRange(uStr);
            }
        }
        private struct p_SContainer
        {
            public byte[] Container
            {
                get
                {
                    List<byte> container = new List<byte>();
                    foreach (var elm in packets)
                        container.AddRange(elm.Packet);

                    container.InsertRange(0, ((CompactUInt16)container.Count).GetBytes());
                    container.Insert(0, 0x22);
                    return container.ToArray();
                }
            }

            private List<p_SPacket> packets;

            public p_SContainer(p_SPacket pkt)
            {
                this.packets = new List<p_SPacket>();
                Add(pkt);
            }

            public p_SContainer(List<p_SPacket> packets)
            {
                this.packets = packets;
            }
            public void Add(p_SPacket pkt)
            {
                this.packets.Add(pkt);
            }
        }
    }
}