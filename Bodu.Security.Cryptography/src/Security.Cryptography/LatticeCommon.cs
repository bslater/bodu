// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LatticeCommon.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the number-theoretic helpers shared by the lattice-based engines (ML-KEM / FIPS 203 and ML-DSA / FIPS 204):
/// modular exponentiation over each scheme's prime modulus and the bit-reversed index permutation used to build the NTT
/// twiddle tables.
/// </summary>
/// <remarks>
/// Both helpers run only during static twiddle-table construction over public constants, so they are not written for
/// constant-time execution — the values involved carry no secrets.
/// </remarks>
internal static class LatticeCommon
{
    /// <summary>
    /// Computes <c>value^exponent mod modulus</c> by square-and-multiply over public constants.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="exponent">The non-negative exponent.</param>
    /// <param name="modulus">The prime modulus (3329 for ML-KEM, 8380417 for ML-DSA).</param>
    /// <returns>The modular power in [0, modulus).</returns>
    internal static int PowMod(int value, int exponent, int modulus)
    {
        long result = 1;
        long basis = value % modulus;

        while (exponent > 0)
        {
            if ((exponent & 1) != 0)
                result = (result * basis) % modulus;

            basis = (basis * basis) % modulus;
            exponent >>= 1;
        }

        return (int)result;
    }

    /// <summary>
    /// Reverses the low <paramref name="bitCount" /> bits of an index (the NTT twiddle-table permutation — BitRev₇ for
    /// ML-KEM, BitRev₈ for ML-DSA).
    /// </summary>
    /// <param name="value">The index in [0, 2^<paramref name="bitCount" />).</param>
    /// <param name="bitCount">The number of low bits to reverse.</param>
    /// <returns>The bit-reversed index.</returns>
    internal static int BitReverse(int value, int bitCount)
    {
        int result = 0;
        for (int bit = 0; bit < bitCount; bit++)
            result |= ((value >> bit) & 1) << (bitCount - 1 - bit);

        return result;
    }
}
