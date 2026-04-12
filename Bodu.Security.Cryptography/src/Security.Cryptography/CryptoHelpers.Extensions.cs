using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public static partial class CryptoHelpers
    {
        /// <summary>
        /// Reverses the order of the least significant <paramref name="bitLength" /> bits in the specified <paramref name="value" />.
        /// </summary>
        /// <param name="value">The 64-bit unsigned integer to reflect.</param>
        /// <param name="bitLength">The number of least significant bits to reverse.</param>
        /// <returns>A <see cref="ulong" /> value whose least significant <paramref name="bitLength" /> bits have been reversed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ReflectBits(this ulong value, int bitLength)
        {
            ulong result = 0;

            for (int i = 0; i < bitLength; i++)
            {
                result = (result << 1) | (value & 1);
                value >>= 1;
            }

            return result;
        }
    }
}