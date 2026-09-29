// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakPermutation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the <c>Keccak-f[1600]</c> permutation (NIST FIPS 202) over a 25-lane 64-bit state, shared by the SHA-3,
/// SHAKE, and the ML-KEM / ML-DSA sampling pipelines.
/// </summary>
internal static partial class KeccakPermutation
{
    /// <summary>The number of 64-bit lanes in the Keccak-f[1600] state.</summary>
    internal const int StateWords = 25;

    /// <summary>
    /// Gets the round constants for the ι (iota) step — 24 values, one per round.
    /// </summary>
    /// <remarks>
    /// The span reads the constants from the assembly's data, so reading them initializes no class and calls no helper:
    /// the four-way kernel, whose state vectors are all live when it first reads them, would otherwise keep the state
    /// on the stack for all 24 rounds.
    /// </remarks>
    private static ReadOnlySpan<ulong> RoundConstants =>
    [
        0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808AUL, 0x8000000080008000UL,
        0x000000000000808BUL, 0x0000000080000001UL, 0x8000000080008081UL, 0x8000000000008009UL,
        0x000000000000008AUL, 0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000AUL,
        0x000000008000808BUL, 0x800000000000008BUL, 0x8000000000008089UL, 0x8000000000008003UL,
        0x8000000000008002UL, 0x8000000000000080UL, 0x000000000000800AUL, 0x800000008000000AUL,
        0x8000000080008081UL, 0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL,
    ];

    /// <summary>
    /// Applies the full <c>Keccak-f[1600]</c> permutation — 24 rounds of θ, ρ, π, χ, and ι — to the supplied 25-word
    /// state in place.
    /// </summary>
    /// <param name="state">The 25-element state to permute. Modified in place.</param>
    /// <exception cref="ArgumentException"><paramref name="state" /> is not exactly 25 elements long.</exception>
    /// <remarks>
    /// <para>
    /// The lanes live in locals for all 24 rounds, <c>a{x + 5y}</c> for lane (x, y). Each round computes the five
    /// column parities and the θ offsets, then builds the next state one output row at a time: ρ and π are folded into
    /// which θ-adjusted lane, rotated by which constant, lands in each position <c>b{x + 5y}</c>, and χ and ι combine
    /// each row of <c>b</c> back into <c>a</c>. Every index and rotation is a constant, so there are no table lookups,
    /// modulo operations, or bounds checks inside the loop.
    /// </para>
    /// <para>
    /// No lane is ever used as an index or a branch condition, so the permutation takes the same time and touches the
    /// same memory whatever the state holds.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static void Permute(Span<ulong> state)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(state, StateWords);

        ref ulong lanes = ref MemoryMarshal.GetReference(state);
        var a00 = Unsafe.Add(ref lanes, 0);
        var a01 = Unsafe.Add(ref lanes, 1);
        var a02 = Unsafe.Add(ref lanes, 2);
        var a03 = Unsafe.Add(ref lanes, 3);
        var a04 = Unsafe.Add(ref lanes, 4);
        var a05 = Unsafe.Add(ref lanes, 5);
        var a06 = Unsafe.Add(ref lanes, 6);
        var a07 = Unsafe.Add(ref lanes, 7);
        var a08 = Unsafe.Add(ref lanes, 8);
        var a09 = Unsafe.Add(ref lanes, 9);
        var a10 = Unsafe.Add(ref lanes, 10);
        var a11 = Unsafe.Add(ref lanes, 11);
        var a12 = Unsafe.Add(ref lanes, 12);
        var a13 = Unsafe.Add(ref lanes, 13);
        var a14 = Unsafe.Add(ref lanes, 14);
        var a15 = Unsafe.Add(ref lanes, 15);
        var a16 = Unsafe.Add(ref lanes, 16);
        var a17 = Unsafe.Add(ref lanes, 17);
        var a18 = Unsafe.Add(ref lanes, 18);
        var a19 = Unsafe.Add(ref lanes, 19);
        var a20 = Unsafe.Add(ref lanes, 20);
        var a21 = Unsafe.Add(ref lanes, 21);
        var a22 = Unsafe.Add(ref lanes, 22);
        var a23 = Unsafe.Add(ref lanes, 23);
        var a24 = Unsafe.Add(ref lanes, 24);

        for (int round = 0; round < 24; round++)
        {
            // θ: the parity of each column, and the offset each column receives from its neighbors.
            var c0 = a00 ^ a05 ^ a10 ^ a15 ^ a20;
            var c1 = a01 ^ a06 ^ a11 ^ a16 ^ a21;
            var c2 = a02 ^ a07 ^ a12 ^ a17 ^ a22;
            var c3 = a03 ^ a08 ^ a13 ^ a18 ^ a23;
            var c4 = a04 ^ a09 ^ a14 ^ a19 ^ a24;
            var d0 = c4 ^ c1.RotateBitsLeftUnchecked(1);
            var d1 = c0 ^ c2.RotateBitsLeftUnchecked(1);
            var d2 = c1 ^ c3.RotateBitsLeftUnchecked(1);
            var d3 = c2 ^ c4.RotateBitsLeftUnchecked(1);
            var d4 = c3 ^ c0.RotateBitsLeftUnchecked(1);

            // ρ and π: lane (x, y), θ-adjusted and rotated by its offset, moves to position (y, 2x + 3y).
            var b00 = a00 ^ d0;
            var b01 = (a06 ^ d1).RotateBitsLeftUnchecked(44);
            var b02 = (a12 ^ d2).RotateBitsLeftUnchecked(43);
            var b03 = (a18 ^ d3).RotateBitsLeftUnchecked(21);
            var b04 = (a24 ^ d4).RotateBitsLeftUnchecked(14);
            var b05 = (a03 ^ d3).RotateBitsLeftUnchecked(28);
            var b06 = (a09 ^ d4).RotateBitsLeftUnchecked(20);
            var b07 = (a10 ^ d0).RotateBitsLeftUnchecked(3);
            var b08 = (a16 ^ d1).RotateBitsLeftUnchecked(45);
            var b09 = (a22 ^ d2).RotateBitsLeftUnchecked(61);
            var b10 = (a01 ^ d1).RotateBitsLeftUnchecked(1);
            var b11 = (a07 ^ d2).RotateBitsLeftUnchecked(6);
            var b12 = (a13 ^ d3).RotateBitsLeftUnchecked(25);
            var b13 = (a19 ^ d4).RotateBitsLeftUnchecked(8);
            var b14 = (a20 ^ d0).RotateBitsLeftUnchecked(18);
            var b15 = (a04 ^ d4).RotateBitsLeftUnchecked(27);
            var b16 = (a05 ^ d0).RotateBitsLeftUnchecked(36);
            var b17 = (a11 ^ d1).RotateBitsLeftUnchecked(10);
            var b18 = (a17 ^ d2).RotateBitsLeftUnchecked(15);
            var b19 = (a23 ^ d3).RotateBitsLeftUnchecked(56);
            var b20 = (a02 ^ d2).RotateBitsLeftUnchecked(62);
            var b21 = (a08 ^ d3).RotateBitsLeftUnchecked(55);
            var b22 = (a14 ^ d4).RotateBitsLeftUnchecked(39);
            var b23 = (a15 ^ d0).RotateBitsLeftUnchecked(41);
            var b24 = (a21 ^ d1).RotateBitsLeftUnchecked(2);

            // χ and ι: each output lane mixes the next two in its row; the round constant enters lane (0, 0).
            a00 = b00 ^ (~b01 & b02) ^ RoundConstants[round];
            a01 = b01 ^ (~b02 & b03);
            a02 = b02 ^ (~b03 & b04);
            a03 = b03 ^ (~b04 & b00);
            a04 = b04 ^ (~b00 & b01);
            a05 = b05 ^ (~b06 & b07);
            a06 = b06 ^ (~b07 & b08);
            a07 = b07 ^ (~b08 & b09);
            a08 = b08 ^ (~b09 & b05);
            a09 = b09 ^ (~b05 & b06);
            a10 = b10 ^ (~b11 & b12);
            a11 = b11 ^ (~b12 & b13);
            a12 = b12 ^ (~b13 & b14);
            a13 = b13 ^ (~b14 & b10);
            a14 = b14 ^ (~b10 & b11);
            a15 = b15 ^ (~b16 & b17);
            a16 = b16 ^ (~b17 & b18);
            a17 = b17 ^ (~b18 & b19);
            a18 = b18 ^ (~b19 & b15);
            a19 = b19 ^ (~b15 & b16);
            a20 = b20 ^ (~b21 & b22);
            a21 = b21 ^ (~b22 & b23);
            a22 = b22 ^ (~b23 & b24);
            a23 = b23 ^ (~b24 & b20);
            a24 = b24 ^ (~b20 & b21);
        }

        Unsafe.Add(ref lanes, 0) = a00;
        Unsafe.Add(ref lanes, 1) = a01;
        Unsafe.Add(ref lanes, 2) = a02;
        Unsafe.Add(ref lanes, 3) = a03;
        Unsafe.Add(ref lanes, 4) = a04;
        Unsafe.Add(ref lanes, 5) = a05;
        Unsafe.Add(ref lanes, 6) = a06;
        Unsafe.Add(ref lanes, 7) = a07;
        Unsafe.Add(ref lanes, 8) = a08;
        Unsafe.Add(ref lanes, 9) = a09;
        Unsafe.Add(ref lanes, 10) = a10;
        Unsafe.Add(ref lanes, 11) = a11;
        Unsafe.Add(ref lanes, 12) = a12;
        Unsafe.Add(ref lanes, 13) = a13;
        Unsafe.Add(ref lanes, 14) = a14;
        Unsafe.Add(ref lanes, 15) = a15;
        Unsafe.Add(ref lanes, 16) = a16;
        Unsafe.Add(ref lanes, 17) = a17;
        Unsafe.Add(ref lanes, 18) = a18;
        Unsafe.Add(ref lanes, 19) = a19;
        Unsafe.Add(ref lanes, 20) = a20;
        Unsafe.Add(ref lanes, 21) = a21;
        Unsafe.Add(ref lanes, 22) = a22;
        Unsafe.Add(ref lanes, 23) = a23;
        Unsafe.Add(ref lanes, 24) = a24;
    }
}
