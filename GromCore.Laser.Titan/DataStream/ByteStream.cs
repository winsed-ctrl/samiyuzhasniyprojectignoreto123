namespace GromCore.Laser.Titan.DataStream
{
    using Atrasis.Magic.Servers.Core.Libs.ZLib;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;
    using System;
    using System.Linq;
    using System.Text;
    using System.Numerics;

    public class ByteStream : ChecksumEncoder
    {
        private const int MAX_MESSAGE_BYTES = 16 * 1024 * 1024;
        private const int MAX_STRING_BYTES = 8 * 1024 * 1024;
        private const int MAX_ARRAY_BYTES = 8 * 1024 * 1024; 
        private int BitOffset;
        private byte[] Buffer;
        private int Length;
        private int Offset;
        
        public ByteStream(int capacity)
        {
            this.Buffer = new byte[capacity];
        }

        public ByteStream(byte[] buffer, int length)
        {
            this.Length = length;
            this.Buffer = buffer;
        }

        public bool CanRead(int count) => count >= 0 && Offset + count <= Length;
        #region Read

        public bool ReadBoolean()
        {
            if (BitOffset == 0) 
            { 
                EnsureReadable(1); 
                ++Offset; 
            }
            bool value = (Buffer[Offset - 1] & (1 << BitOffset)) != 0;
            BitOffset = (BitOffset + 1) & 7;
            return value;
        }

        public byte ReadByte()
        {
            BitOffset = 0;
            EnsureReadable(1);
            return Buffer[Offset++];
        }

        public short ReadShort()
        {
            BitOffset = 0;
            EnsureReadable(2);
            return (short)((Buffer[Offset++] << 8) | Buffer[Offset++]);
        }

        public int ReadInt()
        {
            BitOffset = 0;
            EnsureReadable(4);
            return (Buffer[Offset++] << 24) |
                   (Buffer[Offset++] << 16) |
                   (Buffer[Offset++] << 8) |
                    Buffer[Offset++];
        }

        public int ReadVInt()
        {
            this.BitOffset = 0;
            EnsureReadable(1);
            int value = 0;
            byte byteValue = this.Buffer[this.Offset++];

            if ((byteValue & 0x40) != 0)
            {
                value |= byteValue & 0x3F;

                if ((byteValue & 0x80) != 0)
                {
                    EnsureReadable(1);
                    value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 6;

                    if ((byteValue & 0x80) != 0)
                    {
                        EnsureReadable(1);
                        value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 13;

                        if ((byteValue & 0x80) != 0)
                        {
                            EnsureReadable(1);
                            value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 20;

                            if ((byteValue & 0x80) != 0)
                            {
                                EnsureReadable(1);
                                value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 27;
                                return (int)(value | 0x80000000);
                            }

                            return (int)(value | 0xF8000000);
                        }

                        return (int)(value | 0xFFF00000);
                    }

                    return (int)(value | 0xFFFFE000);
                }

                return (int)(value | 0xFFFFFFC0);
            }

            value |= byteValue & 0x3F;

            if ((byteValue & 0x80) != 0)
            {
                EnsureReadable(1);
                value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 6;

                if ((byteValue & 0x80) != 0)
                {
                    EnsureReadable(1);
                    value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 13;

                    if ((byteValue & 0x80) != 0)
                    {
                        EnsureReadable(1);
                        value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 20;

                        if ((byteValue & 0x80) != 0)
                        {
                            EnsureReadable(1);
                            value |= ((byteValue = this.Buffer[this.Offset++]) & 0x7F) << 27;
                        }
                    }
                }
            }

            return value;
        }

        public long ReadLong()
        {
            byte[] buff = ReadBytes(8, 8);
            return BitConverter.ToInt64(buff.Reverse().ToArray(), 0);
        }

        public long ReadVLong()
        {
            int high = ReadVInt();
            int low = ReadVInt();

            return ((long)high << 32) | (uint)low;
        }

        public int ReadBytesLength()
        {
            this.BitOffset = 0;
            EnsureReadable(4);
            return (this.Buffer[this.Offset++] << 24) |
                   (this.Buffer[this.Offset++] << 16) |
                   (this.Buffer[this.Offset++] << 8) |
                   this.Buffer[this.Offset++];
        }

        public byte[] ReadBytes(int length, int maxCapacity)
        {
            this.BitOffset = 0;

            if (length <= -1)
            {
                return null;
            }

            if (length <= maxCapacity)
            {
                EnsureReadable(length);
                byte[] array = new byte[length];
                System.Buffer.BlockCopy(this.Buffer, this.Offset, array, 0, length);
                this.Offset += length;
                return array;
            }

            return null;
        }

        public string ReadString(int maxCapacity = 9000000)
        {
            int length = this.ReadBytesLength();

            if (length <= -1)
            {
                return null;
            }
            else
            {
                if (length <= maxCapacity)
                {
                    string value = Encoding.UTF8.GetString(this.Buffer, this.Offset, length);
                    this.Offset += length;
                    return value;
                }

                return null;
            }
        }

        public string ReadStringReference(int maxCapacity)
        {
            int length = this.ReadBytesLength();

            if (length <= -1)
            {
                return string.Empty;
            }
            else
            {
                if (length <= maxCapacity)
                {
                    string value = Encoding.UTF8.GetString(this.Buffer, this.Offset, length);
                    this.Offset += length;
                    return value;
                }
            }

            return string.Empty;
        }

        #endregion



        public void ChronosTextEntry(string a1, int a2)
        {
            WriteString(a1);
            WriteVInt(a2);
        }
        public void ChronosFileEntry(string a1, string a2)
        {
            WriteString(a1);
            WriteString(a2);
        }
        public override bool WriteBoolean(bool value)
        {
            if (this.BitOffset == 0)
            {
                this.EnsureCapacity(1);
                this.Buffer[this.Offset++] = 0;
            }

            if (value)
            {
                this.Buffer[this.Offset - 1] |= (byte)(1 << this.BitOffset);
            }

            this.BitOffset = (this.BitOffset + 1) & 7;
            return value;
        }

        public override void WriteByte(byte value)
        {
            this.EnsureCapacity(1);

            this.BitOffset = 0;

            this.Buffer[this.Offset++] = value;
        }

        public override void WriteShort(short value)
        {
            this.EnsureCapacity(2);

            this.BitOffset = 0;

            this.Buffer[this.Offset++] = (byte)(value >> 8);
            this.Buffer[this.Offset++] = (byte)value;
        }
        public void WriteInt16(short value)
        {
            WriteShort(value);
        }

        public void WriteLongLong128(BigInteger value)
        {
            this.WriteIntToByteArray((uint)(value & uint.MaxValue));
            this.WriteIntToByteArray((uint)((value >> 32) & uint.MaxValue));
            this.WriteIntToByteArray((uint)((value >> 64) & uint.MaxValue));
            this.WriteIntToByteArray((uint)((value >> 96) & uint.MaxValue));
        }

        public void WriteCompressedString(string value)
        {
            this.BitOffset = 0;
            if (value == null) {
                value = "";
            }

            byte[] data = Encoding.UTF8.GetBytes(value);
            byte[] compressed = ZLibStream.CompressBuffer(data, CompressionLevel.Default);

            WriteInt(compressed.Length + 4);
            WriteIntLittleEndian(data.Length);
            WriteBytesWithoutLength(compressed, compressed.Length);
        }
        
        public override void WriteInt(int value)
        {
            this.EnsureCapacity(4);

            this.BitOffset = 0;

            this.Buffer[this.Offset++] = (byte)(value >> 24);
            this.Buffer[this.Offset++] = (byte)(value >> 16);
            this.Buffer[this.Offset++] = (byte)(value >> 8);
            this.Buffer[this.Offset++] = (byte)value;
        }
        public void WriteIntLittleEndian(int value)
        {
            this.EnsureCapacity(4);

            this.BitOffset = 0;

            this.Buffer[this.Offset++] = (byte)value; 
            this.Buffer[this.Offset++] = (byte)(value >> 8); 
            this.Buffer[this.Offset++] = (byte)(value >> 16);
            this.Buffer[this.Offset++] = (byte)(value >> 24);
        }

        public void WriteDataReference(int globalId)
        {
            if (globalId <= 0)
            {
                WriteVInt(0);
                return;
            }

            WriteVInt(globalId / 1000000);
            WriteVInt(globalId% 1000000);
        }
        public void WriteDataReference(int v1,int v2)
        {
            if (v1 == 0 && v2 == 0)
                WriteVInt(0);
            else
            {
                WriteVInt(v1);
                WriteVInt(v2);
            }
        }

        public void WriteLogicLong(int a1, int a2)
        {
            WriteVInt(a1);
            WriteVInt(a2);
        }

        public void WriteVIntLong(int a1, int a2)
        {
            WriteVInt(a1);
            WriteVInt(a2);
        }
        public override void WriteVInt(int value)
        {
            this.EnsureCapacity(5);
            this.BitOffset = 0;

            if (value >= 0)
            {
                if (value >= 64)
                {
                    if (value >= 0x2000)
                    {
                        if (value >= 0x100000)
                        {
                            if (value >= 0x8000000)
                            {
                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)((value >> 27) & 0xF);
                            }
                            else
                            {
                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)((value >> 20) & 0x7F);
                            }
                        }
                        else
                        {
                            this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                            this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                            this.Buffer[this.Offset++] = (byte)((value >> 13) & 0x7F);
                        }
                    }
                    else
                    {
                        this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                        this.Buffer[this.Offset++] = (byte)((value >> 6) & 0x7F);
                    }
                }
                else
                {
                    this.Buffer[this.Offset++] = (byte)(value & 0x3F);
                }
            }
            else
            {
                if (value <= -0x40)
                {
                    if (value <= -0x2000)
                    {
                        if (value <= -0x100000)
                        {
                            if (value <= -0x8000000)
                            {
                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)((value >> 27) & 0xF);
                            }
                            else
                            {
                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)((value >> 20) & 0x7F);
                            }
                        }
                        else
                        {
                            this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                            this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                            this.Buffer[this.Offset++] = (byte)((value >> 13) & 0x7F);
                        }
                    }
                    else
                    {
                        this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                        this.Buffer[this.Offset++] = (byte)((value >> 6) & 0x7F);
                    }
                }
                else
                {
                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x40);
                }
            }
        }

        public void WriteLogicUuid(long a1, long a2)
        {
            WriteOneLogicUuid(a1);
            WriteOneLogicUuid(a2);
        }

        public void WriteOneLogicUuid(long value)
        {
            this.EnsureCapacity(10);
            this.BitOffset = 0;

            if (value >= 0)
            {
                if (value >= 0x40)
                {
                    if (value >= 0x2000)
                    {
                        if (value >= 0x100000)
                        {
                            if (value >= 0x8000000)
                            {
                                if (value >= 0x400000000L)
                                {
                                    if (value >= 0x20000000000L)
                                    {
                                        if (value >= 0x1000000000000L)
                                        {
                                            if (value >= 0x80000000000000L)
                                            {
                                                if (value >= 0x4000000000000000L)
                                                {
                                                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 41) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 48) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 55) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)((value >> 62) & 0x7F);
                                                }
                                                else
                                                {
                                                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 41) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 48) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)((value >> 55) & 0x7F);
                                                }
                                            }
                                            else
                                            {
                                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 41) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)((value >> 48) & 0x7F);
                                            }
                                        }
                                        else
                                        {
                                            this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)((value >> 41) & 0x7F);
                                        }
                                    }
                                    else
                                    {
                                        this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)((value >> 34) & 0x7F);
                                    }
                                }
                                else
                                {
                                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)((value >> 27) & 0x7F);
                                }
                            }
                            else
                            {
                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)((value >> 20) & 0x7F);
                            }
                        }
                        else
                        {
                            this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                            this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                            this.Buffer[this.Offset++] = (byte)((value >> 13) & 0x7F);
                        }
                    }
                    else
                    {
                        this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x80);
                        this.Buffer[this.Offset++] = (byte)((value >> 6) & 0x7F);
                    }
                }
                else
                {
                    this.Buffer[this.Offset++] = (byte)(value & 0x3F);
                }
            }
            else
            {
                if (value <= -0x40)
                {
                    if (value <= -0x2000)
                    {
                        if (value <= -0x100000)
                        {
                            if (value <= -0x8000000)
                            {
                                if (value <= -0x400000000L)
                                {
                                    if (value <= -0x20000000000L)
                                    {
                                        if (value <= -0x1000000000000L)
                                        {
                                            if (value <= -0x80000000000000L)
                                            {
                                                if (value <= -0x4000000000000000L)
                                                {
                                                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 41) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 48) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 55) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)((value >> 62) & 0x7F);
                                                }
                                                else
                                                {
                                                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 41) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)(((value >> 48) & 0x7F) | 0x80);
                                                    this.Buffer[this.Offset++] = (byte)((value >> 55) & 0x7F);
                                                }
                                            }
                                            else
                                            {
                                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)(((value >> 41) & 0x7F) | 0x80);
                                                this.Buffer[this.Offset++] = (byte)((value >> 48) & 0x7F);
                                            }
                                        }
                                        else
                                        {
                                            this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)(((value >> 34) & 0x7F) | 0x80);
                                            this.Buffer[this.Offset++] = (byte)((value >> 41) & 0x7F);
                                        }
                                    }
                                    else
                                    {
                                        this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)(((value >> 27) & 0x7F) | 0x80);
                                        this.Buffer[this.Offset++] = (byte)((value >> 34) & 0x7F);
                                    }
                                }
                                else
                                {
                                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                    this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)(((value >> 13) & 0x7F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)(((value >> 20) & 0x7F) | 0x80);
                                    this.Buffer[this.Offset++] = (byte)((value >> 27) & 0x7F);
                                }
                            }
                            else
                            {
                                this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                                this.Buffer[this.Offset++] = (byte)(((value >> 6) & 0x7F) | 0x80);
                                this.Buffer[this.Offset++] = (byte)((value >> 13) & 0x7F);
                            }
                        }
                        else
                        {
                            this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0xC0);
                            this.Buffer[this.Offset++] = (byte)((value >> 6) & 0x7F);
                        }
                    }
                    else
                    {
                        this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x40);
                    }
                }
                else
                {
                    this.Buffer[this.Offset++] = (byte)((value & 0x3F) | 0x40);
                }
            }
        }

        public void WriteIntToByteArray(int value)
        {
            this.EnsureCapacity(4);
            this.BitOffset = 0;

            this.Buffer[this.Offset++] = (byte)(value >> 24);
            this.Buffer[this.Offset++] = (byte)(value >> 16);
            this.Buffer[this.Offset++] = (byte)(value >> 8);
            this.Buffer[this.Offset++] = (byte)value;
        }
        
        public void WriteIntToByteArray(uint value)
        {
            this.EnsureCapacity(4);
            this.BitOffset = 0;

            this.Buffer[this.Offset++] = (byte)(value >> 24);
            this.Buffer[this.Offset++] = (byte)(value >> 16);
            this.Buffer[this.Offset++] = (byte)(value >> 8);
            this.Buffer[this.Offset++] = (byte)value;
        }

        public void WriteLong(long value)
        {
            this.WriteIntToByteArray((int)(value >> 32));
            this.WriteIntToByteArray((int)value);
        }

        public void WriteLong(int high,int low)
        {
            this.WriteInt(high);
            this.WriteInt(low);
        }

        public override void WriteBytes(byte[] value, int length)
        {
            if (value == null)
            {
                this.WriteIntToByteArray(-1);
            }
            else
            {
                this.EnsureCapacity(length + 4);
                this.WriteIntToByteArray(length);

                System.Buffer.BlockCopy(value, 0, this.Buffer, this.Offset, length);

                this.Offset += length;
            }
        }

        public void WriteBytesWithoutLength(byte[] value, int length)
        {
            if (value != null)
            {
                this.EnsureCapacity(length);
                System.Buffer.BlockCopy(value, 0, this.Buffer, this.Offset, length);
                this.Offset += length;
            }
        }

        public override void WriteString(string value)
        {
            if (value == null)
            {
                this.WriteIntToByteArray(-1);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                int length = bytes.Length;

                if (length <= MAX_STRING_BYTES)
                {
                    this.EnsureCapacity(length + 4);
                    this.WriteIntToByteArray(length);

                    System.Buffer.BlockCopy(bytes, 0, this.Buffer, this.Offset, length);

                    this.Offset += length;
                }
                else
                {
                    this.WriteIntToByteArray(-1);
                }
            }
        }

        public override void WriteStringReference(string value)
        {
            if (value == null)
            {
                this.WriteIntToByteArray(-1);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            int length = bytes.Length;

            if (length <= MAX_STRING_BYTES)
            {
                this.EnsureCapacity(length + 4);
                this.WriteIntToByteArray(length);

                System.Buffer.BlockCopy(bytes, 0, this.Buffer, this.Offset, length);

                this.Offset += length;
            }
            else
            {
                this.WriteIntToByteArray(-1);
            }
        }

        public void SetByteArray(byte[] buffer, int length)
        {
            this.Offset = 0;
            this.BitOffset = 0;
            this.Buffer = buffer;
            this.Length = length;
        }

        public void ResetOffset()
        {
            this.Offset = 0;
            this.BitOffset = 0;
        }

        public void SetOffset(int offset)
        {
            this.Offset = offset;
            this.BitOffset = 0;
        }

        public byte[] RemoveByteArray()
        {
            byte[] byteArray = this.Buffer;
            this.Buffer = null;
            return byteArray;
        }
        public int GetLength()
        {
            if (this.Offset < this.Length)
            {
                return this.Length;
            }

            return this.Offset;
        }
        public byte[] GetByteArray() => this.Buffer;
        public int GetOffset() => this.Offset;

        public bool IsAtEnd() => this.Offset >= this.Length;

        public void Clear(int capacity)
        {
            this.Buffer = new byte[capacity];
            this.Offset = 0;
        }
        private static bool IsAddOverflow(int a, int b) => (b > 0 && a > int.MaxValue - b) || (b < 0 && a < int.MinValue - b);

        private void EnsureReadable(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (IsAddOverflow(Offset, count) || Offset + count > Length)
                throw new InvalidOperationException("buffer overflowed!");
        }

        private void EnsureWritable(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (IsAddOverflow(Offset, count)) throw new InvalidOperationException("buffer size overflowed!");

            if (Offset + count > MAX_MESSAGE_BYTES)
                throw new InvalidOperationException("message bigger than maximum!");

            EnsureCapacity(count);
        }

        public void EnsureCapacity(int additional)
        {
            if (additional < 0) throw new ArgumentOutOfRangeException(nameof(additional));
            long need = (long)Offset + additional;

            if (need <= Buffer.Length) return;
            long newLen = Math.Min(Math.Max((long)Buffer.Length * 2, need), MAX_MESSAGE_BYTES);
            if (newLen > int.MaxValue) throw new InvalidOperationException("buffer too large!!!");
            var tmp = new byte[newLen];
            System.Buffer.BlockCopy(Buffer, 0, tmp, 0, Buffer.Length);
            Buffer = tmp;
        }
        public void oldEnsureCapacity(int capacity)
        {
            int bufferLength = this.Buffer.Length;

            if (this.Offset + capacity > bufferLength)
            {
                byte[] tmpBuffer = new byte[this.Buffer.Length + capacity + 100];
                System.Buffer.BlockCopy(this.Buffer, 0, tmpBuffer, 0, bufferLength);
                this.Buffer = tmpBuffer;
            }
        }

        public void Destruct()
        {
            this.Buffer = null;
            this.BitOffset = 0;
            this.Length = 0;
            this.Offset = 0;
        }
    }
}
