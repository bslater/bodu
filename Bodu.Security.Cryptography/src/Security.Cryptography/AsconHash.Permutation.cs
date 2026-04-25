// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash.Permutation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

public abstract partial class AsconHash<T>
{
    /// <summary>
    /// Applies the Ascon-p permutation with the specified number of rounds to the internal five-word state.
    /// </summary>
    /// <param name="rounds">
    /// The number of rounds to apply. When called during initialisation or squeezing this is always 12 (Ascon-p12). During
    /// absorption the value is determined by the concrete variant: 12 for <c>ASCON-HASH256</c> and 8 for
    /// <c>ASCON-HASHA256</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Each round consists of three layers applied in sequence:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// <b>Constant addition</b>: a round-dependent constant is XORed into the third state word (<c>_s2</c>) to break symmetry
    /// between rounds.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Substitution layer</b>: a 5-bit S-box is applied in bit-sliced fashion across all 64 columns of the state
    /// simultaneously, providing non-linearity.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Linear diffusion layer</b>: each state word is XORed with two rotated copies of itself using word-specific rotation
    /// constants, ensuring full diffusion across the 320-bit state.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyPermutation(int rounds)
    {
        ulong s0 = this._s0, s1 = this._s1, s2 = this._s2, s3 = this._s3, s4 = this._s4;

        int start = 12 - rounds;
        for (int i = start; i < 12; i++)
        {
            // Constant addition: XOR round constant into s2. The constant for round i is (15-i)<<4 | i,
            // giving 0xf0 for round 0 down to 0x4b for round 11.
            s2 ^= (ulong)((0x0f - i) << 4 | i);

            // Substitution layer: bit-sliced application of the 5-bit Ascon S-box to all 64 columns.
            s0 ^= s4; s4 ^= s3; s2 ^= s1;

            ulong t0 = ~s0 & s1;
            ulong t1 = ~s1 & s2;
            ulong t2 = ~s2 & s3;
            ulong t3 = ~s3 & s4;
            ulong t4 = ~s4 & s0;

            s0 ^= t1; s1 ^= t2; s2 ^= t3; s3 ^= t4; s4 ^= t0;
            s1 ^= s0; s0 ^= s4; s3 ^= s2; s2 = ~s2;

            // Linear diffusion layer: each word is XORed with two rotated copies.
            s0 ^= BitOperations.RotateRight(s0, 19) ^ BitOperations.RotateRight(s0, 28);
            s1 ^= BitOperations.RotateRight(s1, 61) ^ BitOperations.RotateRight(s1, 39);
            s2 ^= BitOperations.RotateRight(s2,  1) ^ BitOperations.RotateRight(s2,  6);
            s3 ^= BitOperations.RotateRight(s3, 10) ^ BitOperations.RotateRight(s3, 17);
            s4 ^= BitOperations.RotateRight(s4,  7) ^ BitOperations.RotateRight(s4, 41);
        }

        this._s0 = s0; this._s1 = s1; this._s2 = s2; this._s3 = s3; this._s4 = s4;
    }
}
