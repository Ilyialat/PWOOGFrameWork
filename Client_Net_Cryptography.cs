using System;

using System.Collections.Generic;
using System.Text;

using System.IO;
using System.Security.Cryptography;

namespace PWOOGFrameWork
{
    public partial class PWClient
    {
        private static byte[] Assign(byte[] bt, byte[] assignArray)
        {
            byte[] result = new byte[bt.Length + assignArray.Length];
            for (int i = 0; i < bt.Length; i++) result[i] = bt[i];
            for (int i = 0; i < assignArray.Length; i++) result[i + bt.Length] = assignArray[i];
            return result;
        }

        private class ActionQueueAsync
        {
            private Queue<Action> queue;
            private object wrap;
            private bool autoEnd;

            public ActionQueueAsync(bool autoEnd)
            {
                queue = new Queue<Action>();
                wrap = new object();
                this.autoEnd = autoEnd;
            }
            public void Start(Action task)
            {
                lock (wrap)
                {
                    queue.Enqueue(task);
                    if (queue.Count == 1)
                        StartFirst();
                }
            }
            private void StartFirst()
            {
                Action d = queue.Peek();
                if (autoEnd)
                    d += End;
                System.Threading.Tasks.Task.Factory.StartNew(d);
            }
            public void End()
            {
                lock (wrap)
                {
                    queue.Dequeue();
                    if (queue.Count != 0)
                        StartFirst();
                }
            }
        }
        private static class RandomUtil
        {
            private static Random rand = new Random();

            public static byte[] NextBytes(int count)
            {
                byte[] buf = new byte[count];
                rand.NextBytes(buf);
                return buf;
            }
            public static int Next(int a, int b)
            {
                return rand.Next(a, b);
            }
        }
        private static class MD5Hash
        {
            private static MD5 md5 = MD5.Create();
            public static byte[] GetHash(string login, string password, byte[] key)
            {
                byte[] bt = Encoding.ASCII.GetBytes(login + password);
                return new HMACMD5(md5.ComputeHash(bt)).ComputeHash(key);
            }
            public static byte[] GetRC4Key(byte[] encordechash, string login, byte[] hash)
            {
                byte[] loginbt = Encoding.ASCII.GetBytes(login);
                byte[] nhash = Assign(hash, encordechash);

                byte[] result = new HMACMD5(loginbt).ComputeHash(nhash);
                return result;
            }
            public static byte[] GetDecodeHash()
            {
                return RandomUtil.NextBytes(16);
            }
        }
        private class PWCrypt
        {
            private RC4 Encode; //enc
            private RC4 Decode; //dec
            private Unpack unpack = new Unpack();

            public PWCrypt(byte[] RC4Encode, byte[] RC4Decode)
            {
                Encode = new RC4(); //enc
                Decode = new RC4(); //dec

                Encode.Shuffle(RC4Encode);
                Decode.Shuffle(RC4Decode);
            }
            public void Encrypt(ref byte[] packet)
            {
                for (int i = 0; i < packet.Length; i++)
                    packet[i] = Encode.Encode(packet[i]);
            }
            public void Decrypt(ref byte[] packet)
            {
                for (int i = 0; i < packet.Length; i++)
                    packet[i] = Decode.Encode(packet[i]);

                using (MemoryStream ms = new MemoryStream())
                {
                    foreach (byte bt in packet)
                    {
                        byte[] r = unpack.AddByte(bt).ToArray();
                        ms.Write(r, 0, r.Length);
                    }
                    packet = ms.ToArray();
                }
            }

            private class RC4
            {
                private byte m_Shift1;
                private byte m_Shift2;
                private byte[] m_Table = new byte[256];

                public RC4()
                {
                    for (int i = 0; i < 256; i++)
                        m_Table[i] = Convert.ToByte(i);
                    m_Shift1 = 0;
                    m_Shift2 = 0;
                }
                public void Shuffle(byte[] Key)
                {
                    byte Shift = 0;
                    for (int i = 0; i < 256; i++)
                    {
                        byte A = Key[i % 16];
                        Shift += (byte)(A + m_Table[i]);

                        byte B = m_Table[i];
                        m_Table[i] = m_Table[Shift];
                        m_Table[Shift] = B;
                    }
                }
                public byte Encode(byte InPacket)
                {
                    m_Shift1++;
                    byte A = m_Table[m_Shift1];
                    m_Shift2 += A;
                    byte B = m_Table[m_Shift2];
                    m_Table[m_Shift2] = A;
                    m_Table[m_Shift1] = B;
                    byte C = (byte)(A + B);
                    byte D = m_Table[C];
                    return (byte)(InPacket ^ D);
                }
            }
            private class Unpack
            {
                UInt32 m_Code1;
                UInt32 m_Code2;
                UInt32 m_Stage;
                UInt32 m_Shift;
                byte m_PackedOffset;
                readonly List<byte> m_Packed = new List<byte>();
                readonly List<byte> m_Unpacked = new List<byte>();


                public Unpack()
                {
                    m_PackedOffset = 0;
                    m_Stage = 0;
                }
                bool HasBits(UInt32 Count)
                {
                    return (m_Packed.Count * 8 - m_PackedOffset) >= Count;
                }
                public List<byte> AddByte(byte InB)
                {
                    m_Packed.Add(InB);
                    var UnpackedChunk = new List<byte>();

                    for (; ; )
                    {
                        if (m_Stage == 0)
                        {
                            if (HasBits(4))
                            {
                                if (GetPackedBits(1) == 0)
                                {
                                    // 0-xxxxxxx
                                    m_Code1 = 1;
                                    m_Stage = 1;
                                    continue;
                                }
                                else
                                {
                                    if (GetPackedBits(1) == 0)
                                    {
                                        // 10-xxxxxxx
                                        m_Code1 = 2;
                                        m_Stage = 1;
                                        continue;
                                    }
                                    else
                                    {
                                        if (GetPackedBits(1) == 0)
                                        {
                                            // 110-xxxxxxxxxxxxx-*
                                            m_Code1 = 3;
                                            m_Stage = 1;
                                            continue;
                                        }
                                        else
                                        {
                                            if (GetPackedBits(1) == 0)
                                            {
                                                // 1110-xxxxxxxx-*
                                                m_Code1 = 4;
                                                m_Stage = 1;
                                                continue;
                                            }
                                            else
                                            {
                                                // 1111-xxxxxx-*
                                                m_Code1 = 5;
                                                m_Stage = 1;
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                                break;
                        }
                        else if (m_Stage == 1)
                        {
                            if (m_Code1 == 1)
                            {
                                if (HasBits(7))
                                {
                                    byte OutB = (byte)(GetPackedBits(7));
                                    UnpackedChunk.Add(OutB);
                                    m_Unpacked.Add(OutB);
                                    m_Stage = 0;
                                    continue;
                                }
                                else
                                    break;
                            }
                            else if (m_Code1 == 2)
                            {
                                if (HasBits(7))
                                {
                                    byte OutB = (byte)((GetPackedBits(7)) | 0x80);
                                    UnpackedChunk.Add(OutB);
                                    m_Unpacked.Add(OutB);
                                    m_Stage = 0;
                                    continue;
                                }
                                else
                                    break;
                            }
                            else if (m_Code1 == 3)
                            {
                                if (HasBits(13))
                                {
                                    m_Shift = GetPackedBits(13) + 0x140;
                                    m_Stage = 2;
                                    continue;
                                }
                                else
                                    break;
                            }
                            else if (m_Code1 == 4)
                            {
                                if (HasBits(8))
                                {
                                    m_Shift = GetPackedBits(8) + 0x40;
                                    m_Stage = 2;
                                    continue;
                                }
                                else
                                    break;
                            }
                            else if (m_Code1 == 5)
                            {
                                if (HasBits(6))
                                {
                                    m_Shift = GetPackedBits(6);
                                    m_Stage = 2;
                                    continue;
                                }
                                else
                                    break;
                            }
                        }
                        else if (m_Stage == 2)
                        {
                            if (m_Shift == 0)
                            {
                                // Guess !!!
                                if (m_PackedOffset != 0)
                                {
                                    m_PackedOffset = 0;
                                    //m_Packed.PopFront();
                                    m_Packed.RemoveAt(0);
                                }
                                m_Stage = 0;
                                continue;
                            }
                            m_Code2 = 0;
                            m_Stage = 3;
                            continue;
                        }
                        else if (m_Stage == 3)
                        {
                            if (HasBits(1))
                            {
                                if (GetPackedBits(1) == 0)
                                {
                                    m_Stage = 4;
                                    continue;
                                }
                                else
                                {
                                    m_Code2++;
                                    continue;
                                }
                            }
                            else
                                break;
                        }
                        else if (m_Stage == 4)
                        {
                            UInt32 CopySize = 0;
                            if (m_Code2 == 0)
                                CopySize = 3;
                            else
                            {
                                UInt32 Sz = m_Code2 + 1;
                                if (HasBits(Sz))
                                    CopySize = GetPackedBits(Sz) + (UInt32)(1 << ((Int32)Sz));
                                else
                                    break;
                            }

                            Copy(m_Shift, CopySize, ref UnpackedChunk);
                            m_Stage = 0;
                            continue;
                        }
                    }
                    return UnpackedChunk;
                }


                void Notify(string Msg)
                {
                    //Log.L(Msg);
                }
                void Copy(UInt32 Shift, UInt32 Size, ref List<byte> UnpackedChunk)
                {
                    for (UInt32 i = 0; i < Size; i++)
                    {
                        var PIndex = m_Unpacked.Count - Shift;
                        if (PIndex < 0)
                            Notify("Unpack error");
                        else
                        {
                            byte B = m_Unpacked[(Int32)PIndex];
                            m_Unpacked.Add(B);
                            UnpackedChunk.Add(B);
                        }
                    }
                }
                UInt32 GetPackedBits(UInt32 BitCount)
                {
                    if (BitCount > 16)
                        return 0;

                    if (!HasBits(BitCount))
                        Notify("Unpack bit stream overflow");

                    UInt32 AlBitCount = BitCount + m_PackedOffset;
                    UInt32 AlByteCount = (AlBitCount + 7) / 8;

                    UInt32 V = 0;
                    for (UInt32 i = 0; i < AlByteCount; i++)
                        V |= (UInt32)(m_Packed[(Int32)i]) << (Int32)(24 - i * 8);
                    V <<= m_PackedOffset;
                    V >>= (Int32)(32 - BitCount);

                    m_PackedOffset += (byte)BitCount;
                    Int32 FreeBytes = m_PackedOffset / 8;
                    if (FreeBytes != 0)
                    {
                        //m_Packed.PopFront(FreeBytes);
                        m_Packed.RemoveRange(0, FreeBytes);
                    }
                    m_PackedOffset %= 8;

                    return V;
                }
            }
        }
    }
}