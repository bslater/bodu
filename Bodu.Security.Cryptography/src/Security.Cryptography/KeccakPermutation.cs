// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the <c>Keccak-f[1600]</c> permutation (NIST FIPS 202) over a 25-lane 64-bit state, shared by the SHA-3,
/// SHAKE, and the ML-KEM / ML-DSA sampling pipelines.
/// </summary>
internal static class KeccakPermutation
{
    /// <summary>
    /// The number of 64-bit lanes in the Keccak-f[1600] state.
    /// </summary>
    internal const int StateWords = 25;

    /// <summary>
    /// Round constants for the ι (iota) step — 24 values, one per round.
    /// </summary>
    private static readonly ulong[] s_roundConstants =
    [
        0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808AUL, 0x8000000080008000UL,
        0x000000000000808BUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
        0x000000000000008AUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000AUL,
        0x000000008000808BUL, 0x800000000000008BUL, 0x8000000000008089UL, 0x8000000000008003UL,
        0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800AUL, 0x800000008000000AUL,
        0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL,
    ];

    /// <summary>
    /// ρ (rho) rotation offsets indexed as rho[x + 5y].
    /// </summary>
#pragma warning disable SA1137 // Elements should have the same indentation
    private static readonly int[] s_rho =
    [
         0,  1, 62, 28, 27,
        36, 44,  6, 55, 20,
         3, 10, 43, 25, 39,
        41, 45, 15, 21,  8,
        18,  2, 61, 56, 14,
    ];

    /// <summary>
    /// π (pi) permutation indices mapping state[i] → B[pi[i]].
    /// </summary>
    private static readonly int[] s_pi =
    [
         0, 10, 20,  5, 15,
        16,  1, 11, 21,  6,
         7, 17,  2, 12, 22,
        23,  8, 18,  3, 13,
        14, 24,  9, 19,  4,
    ];
#pragma warning restore SA1137 // Elements should have the same indentation

    /// <summary>
    /// Applies the full <c>Keccak-f[1600]</c> permutation — 24 rounds of θ, ρ, π, χ, and ι — to the supplied 25-word
    /// state in place.
    /// </summary>
    /// <param name="state">The 25-element state to permute. Modified in place.</param>
    /// <exception cref="ArgumentException"><paramref name="state" /> is not exactly 25 elements long.</exception>
    internal static void Permute(Span<ulong> state)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(state, StateWords);

        Span<ulong> c = stackalloc ulong[5];
        Span<ulong> b = stackalloc ulong[StateWords];

        for (int round = 0; round < 24; round++)
        {
            // θ (theta): column parity and mixing.
            for (int x = 0; x < 5; x++)
                c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];

            for (int x = 0; x < 5; x++)
            {
                ulong d = c[(x + 4) % 5] ^ c[(x + 1) % 5].RotateBitsLeftUnchecked(1);
                for (int y = 0; y < 5; y++)
                    state[x + (y * 5)] ^= d;
            }

            // ρ and π combined: rotate each lane and scatter to the π-permuted position.
            for (int i = 0; i < StateWords; i++)
                b[s_pi[i]] = state[i].RotateBitsLeftUnchecked(s_rho[i]);

            // χ (chi): non-linear mixing within each row.
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                    state[x + (y * 5)] = b[x + (y * 5)] ^ ((~b[((x + 1) % 5) + (y * 5)]) & b[((x + 2) % 5) + (y * 5)]);
            }

            // ι (iota): XOR a round constant into lane (0,0).
            state[0] ^= s_roundConstants[round];
        }

        CryptographyHelper.Clear(c);
        CryptographyHelper.Clear(b);
    }
}
