using System;
using System.Collections.Generic;
using System.Text;

namespace BgfxEx
{
    public static partial class bgfx_hash
    {
        private const uint m = 0x5bd1e995;
        private const int r = 24;

        /// <summary>
        /// 改造自 https://github.com/jitbit/MurmurHash.net/blob/master/MurmurHash.cs
        /// </summary>
        public struct HashMurmur2
        {
            public uint hash;
            public uint cap;
            public int offset;
            private byte[] _buffer;
            private Memory<byte> _memory;

            public void exp_ushort(uint size)
            {
                cap += sizeof(ushort) * (size / 2);
            }

            public void exp_end()
            {
                _buffer = new byte[cap];
                _memory = new Memory<byte>(_buffer);
            }

            public void begin(uint seed = 0)
            {
                hash = seed;
                cap = 0;
                offset = 0;
            }

            public unsafe void add(ushort* ptr, uint size = 0)
            {
                if (size != 0)
                {
                    Span<ushort> data = new Span<ushort>(ptr, (int)size);
                    var bytes = data.ToArray();

                    fixed (void* slice = &_memory.Span.Slice(offset, (int)size * 2).GetPinnableReference())
                    {
                        Buffer.MemoryCopy(ptr, slice, size * 2, size * 2);
                    }
                    offset += (int)size * 2;
                }
                else
                {
                    ushort value = *ptr;
                    _buffer[offset] = (byte)value;
                    _buffer[offset + 1] = (byte)(value >> 8);
                    offset += 2;
                }
            }

            public uint end()
            {
                if(cap == 0)
                {
                    return 0;
                }
                uint h = hash ^ (uint)cap;
                int currentIndex = 0;
                while (cap >= 4)
                {
                    uint k = (uint)(_buffer[currentIndex++] | _buffer[currentIndex++] << 8 | _buffer[currentIndex++] << 16 | _buffer[currentIndex++] << 24);
                    k *= m;
                    k ^= k >> r;
                    k *= m;

                    h *= m;
                    h ^= k;
                    cap -= 4;
                }

                switch (cap)
                {
                    case 3:
                        h ^= (ushort)(_buffer[currentIndex++] | _buffer[currentIndex++] << 8);
                        h ^= (uint)(_buffer[currentIndex] << 16);
                        h *= m;
                        break;
                    case 2:
                        h ^= (ushort)(_buffer[currentIndex++] | _buffer[currentIndex] << 8);
                        h *= m;
                        break;
                    case 1:
                        h ^= _buffer[currentIndex];
                        h *= m;
                        break;
                    default:
                        break;
                }

                h ^= h >> 13;
                h *= m;
                h ^= h >> 15;
                return h;
            }
        }
    }
}
