// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcReference.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Minimal, bit-by-bit reference CRC implementation used by the test suite to independently verify the
    /// library's table-driven <see cref="Crc" /> against arbitrary <see cref="CrcStandard" /> definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implements the canonical "Williams" CRC divider MSB-first, with optional input/output reflection and
    /// final XOR as described in Ross Williams' <i>A Painless Guide to CRC Error Detection Algorithms</i> and
    /// the CRC RevEng catalogue. Works uniformly for all widths in the range 1 – 64 bits.
    /// </para>
    /// <para>
    /// The algorithm is deliberately trivial (no lookup tables, no unrolling) so that it does not share code
    /// paths with the library's production implementation. A bug in one of the two implementations is
    /// therefore highly likely to surface as a parity mismatch rather than a silent agreement.
    /// </para>
    /// </remarks>
    internal static class CrcReference
    {
        /// <summary>
        /// Computes the CRC of <paramref name="input" /> under the given <paramref name="standard" />.
        /// </summary>
        /// <param name="standard">The CRC parameter set to apply.</param>
        /// <param name="input">The input bytes to hash.</param>
        /// <returns>The finalized CRC value, width-masked to <paramref name="standard" />.Size bits.</returns>
        public static ulong Compute(CrcStandard standard, ReadOnlySpan<byte> input)
        {
            int width = standard.Size;
            ulong poly = standard.Polynomial;
            ulong mask = width == 64 ? ulong.MaxValue : (1UL << width) - 1UL;
            ulong topBit = 1UL << (width - 1);

            ulong crc = standard.InitialValue & mask;

            foreach (byte rawByte in input)
            {
                byte b = standard.ReflectIn ? ReverseByte(rawByte) : rawByte;

                // Feed the byte MSB-first through the divider. This is width-agnostic and avoids the
                // `crc ^= b << (width - 8)` trick which breaks for widths below 8 bits.
                for (int bit = 7; bit >= 0; bit--)
                {
                    ulong inputBit = (ulong)((b >> bit) & 1);
                    ulong topBitOfCrc = (crc & topBit) != 0UL ? 1UL : 0UL;
                    bool divide = (topBitOfCrc ^ inputBit) != 0UL;
                    crc = (crc << 1) & mask;
                    if (divide)
                        crc = (crc ^ poly) & mask;
                }
            }

            if (standard.ReflectOut)
                crc = ReflectBits(crc, width);

            return (crc ^ standard.XOrOut) & mask;
        }

        /// <summary>Packs the little-endian bytes produced by <see cref="Crc.ComputeHash(ReadOnlySpan{byte})" /> into a <see cref="ulong" /> for comparison.</summary>
        /// <param name="hash">The raw hash bytes as returned by the library.</param>
        /// <returns>The CRC value as a <see cref="ulong" />.</returns>
        public static ulong PackLittleEndian(ReadOnlySpan<byte> hash)
        {
            ulong value = 0UL;
            for (int i = 0; i < hash.Length; i++)
                value |= (ulong)hash[i] << (i * 8);
            return value;
        }

        private static byte ReverseByte(byte value)
        {
            uint v = value;
            v = ((v & 0xF0u) >> 4) | ((v & 0x0Fu) << 4);
            v = ((v & 0xCCu) >> 2) | ((v & 0x33u) << 2);
            v = ((v & 0xAAu) >> 1) | ((v & 0x55u) << 1);
            return (byte)v;
        }

        private static ulong ReflectBits(ulong value, int width)
        {
            ulong result = 0UL;
            for (int i = 0; i < width; i++)
            {
                if ((value & (1UL << i)) != 0UL)
                    result |= 1UL << (width - 1 - i);
            }
            return result;
        }
    }
}
