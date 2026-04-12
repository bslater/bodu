// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using Bodu.Extensions;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bodu.Security.Cryptography
{
    public sealed class CityHash32
        : CityHashBase<CityHash32>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CityHash32" /> class.
        /// </summary>
        public CityHash32() : base(32) { }

        /// <inheritdoc />
        protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
        {
            uint result = source.Length switch
            {
                <= 4 => Hash32Len0to4(source),
                <= 12 => Hash32Len5to12(source),
                <= 24 => Hash32Len13to24(source),
                _ => Hash32Len25Plus(source)
            };

            byte[] buffer = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, result);
            return buffer;
        }

        private static uint Hash32Len0to4(ReadOnlySpan<byte> s)
        {
            uint b = 0, c = 9;
            foreach (byte t in s)
            {
                b = b * C1 + t;
                c ^= b;
            }
            return Mix(Mur(b, Mur((uint)s.Length, c)));
        }

        private static uint Hash32Len5to12(ReadOnlySpan<byte> s)
        {
            uint a = (uint)s.Length;
            uint b = a * 5;
            uint c = 9;
            uint d = b;

            a += BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(0, 4));
            b += BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length - 4, 4));
            c += BinaryPrimitives.ReadUInt32LittleEndian(s.Slice((s.Length >> 1) & 4, 4));

            return Mix(Mur(c, Mur(b, Mur(a, d))));
        }

        private static uint Hash32Len13to24(ReadOnlySpan<byte> s)
        {
            uint a = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice((s.Length >> 1) - 4, 4));
            uint b = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(4, 4));
            uint c = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length - 8, 4));
            uint d = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice((s.Length >> 1), 4));
            uint e = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(0, 4));
            uint f = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(s.Length - 4, 4));
            uint h = (uint)s.Length;

            return Mix(Mur(f, Mur(e, Mur(d, Mur(c, Mur(b, Mur(a, h)))))));
        }

        private static uint Hash32Len25Plus(ReadOnlySpan<byte> s)
        {
            int len = s.Length;
            uint h = (uint)len, g = h * C1, f = g;

            uint a0 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 4, 4)).RotateBitsRight(17) * C2;
            uint a1 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 8, 4)).RotateBitsRight(17) * C2;
            uint a2 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 16, 4)).RotateBitsRight(17) * C2;
            uint a3 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 12, 4)).RotateBitsRight(17) * C2;
            uint a4 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(len - 20, 4)).RotateBitsRight(17) * C2;

            h ^= a0;
            h = h.RotateBitsRight(19) * 5 + HashMagic;
            h ^= a2;
            h = h.RotateBitsRight(19) * 5 + HashMagic;

            g ^= a1;
            g = g.RotateBitsRight(19) * 5 + HashMagic;
            g ^= a3;
            g = g.RotateBitsRight(19) * 5 + HashMagic;

            f += a4;
            f = f.RotateBitsRight(19) * 5 + HashMagic;

            int iters = (len - 1) / 20;
            for (int i = 0; i < iters; i++)
            {
                int offset = i * 20;

                uint b0 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 0, 4)).RotateBitsRight(17) * C2;
                uint b1 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 4, 4));
                uint b2 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 8, 4)).RotateBitsRight(17) * C2;
                uint b3 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 12, 4)).RotateBitsRight(17) * C2;
                uint b4 = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(offset + 16, 4));

                h ^= b0;
                h = h.RotateBitsRight(18) * 5 + HashMagic;

                f += b1;
                f = f.RotateBitsRight(19) * C1;

                g += b2;
                g = g.RotateBitsRight(18) * 5 + HashMagic;

                h ^= b3 + b1;
                h = h.RotateBitsRight(19) * 5 + HashMagic;

                g ^= b4;
                g = BinaryPrimitives.ReverseEndianness(g) * 5;

                h += b4 * 5;
                h = BinaryPrimitives.ReverseEndianness(h);

                f += b0;

                Permute3(ref f, ref h, ref g);
            }

            g = g.RotateBitsRight(11) * C1;
            g = g.RotateBitsRight(17) * C1;

            f = f.RotateBitsRight(11) * C1;
            f = f.RotateBitsRight(17) * C1;

            h = (h + g).RotateBitsRight(19) * 5 + HashMagic;
            h = h.RotateBitsRight(17) * C1;
            h = (h + f).RotateBitsRight(19) * 5 + HashMagic;
            h = h.RotateBitsRight(17) * C1;

            return h;
        }
    }
}