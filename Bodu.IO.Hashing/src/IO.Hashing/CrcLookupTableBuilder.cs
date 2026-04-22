// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.IO.Hashing
{
    /// <summary>
    /// A utility class for handling CRC operations such as generating CRC lookup tables.
    /// </summary>
    public static class CrcLookupTableBuilder
    {
        /// <summary>
        /// Generates a CRC lookup table for a given bit size, polynomial, and reflection mode.
        /// </summary>
        /// <param name="size">The number of bits in the CRC (e.g., 8, 16, 32, 64).</param>
        /// <param name="polynomial">The CRC polynomial represented as an unsigned integer.</param>
        /// <param name="reflectIn">
        /// If <see langword="true" />, input bytes are reflected (bit-reversed) before CRC processing; otherwise, bits are used as-is.
        /// </param>
        /// <returns>
        /// An array of <see cref="ulong" /> values representing the CRC lookup table, with <c><![CDATA[1 << min(size, 8)]]></c> entries.
        /// </returns>
        /// <remarks>
        /// This method is typically used to precompute a table of CRC values for efficient byte-wise CRC calculation. The reflection
        /// setting determines whether the bits of the input byte are reversed prior to processing, which is common in some CRC variants.
        /// </remarks>
        public static ulong[] BuildLookupTable(int size, ulong polynomial, bool reflectIn)
        {
            ThrowHelper.ThrowIfOutOfRange(size, 1, 64);

            // Determine number of bits to process per lookup (typically 8 for byte-wise processing)
            int bitsPerTableEntry = size < 8 ? 1 : 8;
            int tableSize = 1 << bitsPerTableEntry;

            var table = new ulong[tableSize];
            ulong significantBitMask = 1UL << (size - 1);

            for (uint i = 0; i < tableSize; i++)
            {
                // Start with the input byte value
                ulong value = i;

                // Optionally reflect the bits of the input value
                if (reflectIn)
                    value = NumericExtensions.ReverseBitsUnchecked(value, bitsPerTableEntry);

                // Left-align the value to match the CRC size
                value <<= size - bitsPerTableEntry;

                // Apply the polynomial for each bit in the byte
                for (int bit = 0; bit < bitsPerTableEntry; bit++)
                {
                    bool msbSet = (value & significantBitMask) != 0;
                    value = msbSet ? (value << 1) ^ polynomial : value << 1;
                }

                // Optionally reflect the result and truncate to the desired CRC size
                if (reflectIn)
                    value = NumericExtensions.ReverseBitsUnchecked(value, size);

                // Mask off any bits beyond the desired CRC size
                value &= ulong.MaxValue >> (64 - size);

                table[i] = value;
            }

            return table;
        }
    }
}
