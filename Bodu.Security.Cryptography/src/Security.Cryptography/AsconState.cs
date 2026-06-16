// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the 320-bit ASCON permutation state as five 64-bit words, and provides the Ascon-p permutation used by
/// all members of the ASCON algorithm family defined in NIST SP 800-232.
/// </summary>
/// <remarks>
/// <para>
/// The ASCON permutation operates over a 320-bit state composed of five 64-bit words <c>S0</c>…<c>S4</c>. Each call to
/// <see cref="Permute" /> applies a sequence of identical rounds, each consisting of three layers:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Constant addition</b>: a round-dependent constant is XORed into <c>S2</c> to break round symmetry.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Substitution layer</b>: a 5-bit S-box is applied in bit-sliced fashion across all 64 bit-columns of the state
/// simultaneously, providing non-linearity.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Linear diffusion layer</b>: each word is XORed with two rotated copies of itself using word-specific rotation
/// constants, ensuring full diffusion across the 320-bit state.
/// </description>
/// </item>
/// </list>
/// <para>
/// This struct is declared <see langword="internal" /> and is shared by <see cref="AsconHash{T}" />,
/// <see cref="AsconAead128" />, <see cref="AsconXof128" />, and <see cref="AsconCxof128" />.
/// </para>
/// </remarks>
internal struct AsconState
{
    /// <summary>
    /// State word 0.
    /// </summary>
    public ulong S0;

    /// <summary>
    /// State word 1.
    /// </summary>
    public ulong S1;

    /// <summary>
    /// State word 2.
    /// </summary>
    public ulong S2;

    /// <summary>
    /// State word 3.
    /// </summary>
    public ulong S3;

    /// <summary>
    /// State word 4.
    /// </summary>
    public ulong S4;

    /// <summary>
    /// Applies the Ascon-p permutation with the specified number of rounds to this state.
    /// </summary>
    /// <param name="rounds">
    /// The number of rounds to apply. Must be between 1 and 12 inclusive. Common values are 6 (Ascon-p6), 8 (Ascon-p8),
    /// and 12 (Ascon-p12).
    /// </param>
    /// <remarks>
    /// Rounds are numbered 0–11; a call with <paramref name="rounds" /> = <c>r</c> applies rounds <c>12 − r</c> through
    /// <c>11</c>, preserving the standard constant-addition schedule.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped state-word assignments mirror the bit-sliced Ascon permutation steps and preserve a compact, specification-like layout for the five-word state transformation.")]
    public void Permute(int rounds)
    {
        ulong s0 = S0, s1 = S1, s2 = S2, s3 = S3, s4 = S4;

        int start = 12 - rounds;
        for (int i = start; i < 12; i++)
        {
            // Constant addition: XOR round constant into s2. The constant for round i is (15-i)<<4 | i,
            // giving 0xf0 for round 0 down to 0x4b for round 11.
            s2 ^= (ulong)((0x0f - i) << 4 | i);

            // Substitution layer: bit-sliced 5-bit Ascon S-box applied to all 64 bit-columns.
            s0 ^= s4; s4 ^= s3; s2 ^= s1;

            ulong t0 = ~s0 & s1;
            ulong t1 = ~s1 & s2;
            ulong t2 = ~s2 & s3;
            ulong t3 = ~s3 & s4;
            ulong t4 = ~s4 & s0;

            s0 ^= t1; s1 ^= t2; s2 ^= t3; s3 ^= t4; s4 ^= t0;
            s1 ^= s0; s0 ^= s4; s3 ^= s2; s2 = ~s2;

            // Linear diffusion layer: each word XORed with two rotated copies.
            s0 ^= s0.RotateBitsRightUnchecked(19) ^ s0.RotateBitsRightUnchecked(28);
            s1 ^= s1.RotateBitsRightUnchecked(61) ^ s1.RotateBitsRightUnchecked(39);
            s2 ^= s2.RotateBitsRightUnchecked(1) ^ s2.RotateBitsRightUnchecked(6);
            s3 ^= s3.RotateBitsRightUnchecked(10) ^ s3.RotateBitsRightUnchecked(17);
            s4 ^= s4.RotateBitsRightUnchecked(7) ^ s4.RotateBitsRightUnchecked(41);
        }

        S0 = s0; S1 = s1; S2 = s2; S3 = s3; S4 = s4;
    }

    /// <summary>
    /// XORs a 16-byte (128-bit, two-word) rate block into state words <c>S0</c> and <c>S1</c> using little-endian word
    /// order as specified by NIST SP 800-232.
    /// </summary>
    /// <param name="block">The 16-byte block to absorb. Must be exactly 16 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AbsorbRate128(ReadOnlySpan<byte> block)
    {
        S0 ^= BinaryPrimitives.ReadUInt64LittleEndian(block);
        S1 ^= BinaryPrimitives.ReadUInt64LittleEndian(block[8..]);
    }

    /// <summary>
    /// XORs a single 8-byte (64-bit, one-word) rate block into state word <c>S0</c> using little-endian word order as
    /// specified by NIST SP 800-232.
    /// </summary>
    /// <param name="block">The 8-byte block to absorb. Must be exactly 8 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AbsorbRate64(ReadOnlySpan<byte> block) =>
        S0 ^= BinaryPrimitives.ReadUInt64LittleEndian(block);

    /// <summary>
    /// Reads the current rate (128-bit, two-word) as 16 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">Destination span. Must be at least 16 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SqueezeRate128(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, S0);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], S1);
    }

    /// <summary>
    /// Reads the current rate (64-bit, one-word) as 8 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">Destination span. Must be at least 8 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SqueezeRate64(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt64LittleEndian(destination, S0);

    /// <summary>
    /// Zeroes all five state words, clearing any sensitive intermediate state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() =>
        S0 = S1 = S2 = S3 = S4 = 0UL;
}
