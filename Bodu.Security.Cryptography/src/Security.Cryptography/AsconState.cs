// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconState.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the 320-bit ASCON permutation state as five 64-bit words, and provides the Ascon-p permutation
/// used by all members of the ASCON algorithm family defined in NIST SP 800-232.
/// </summary>
/// <remarks>
/// <para>
/// The ASCON permutation operates over a 320-bit state composed of five 64-bit words <c>S0</c>…<c>S4</c>.
/// Each call to <see cref="Permute" /> applies a sequence of identical rounds, each consisting of three layers:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Constant addition</b>: a round-dependent constant is XORed into <c>S2</c> to break round symmetry.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Substitution layer</b>: a 5-bit S-box is applied in bit-sliced fashion across all 64 bit-columns of
/// the state simultaneously, providing non-linearity.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Linear diffusion layer</b>: each word is XORed with two rotated copies of itself using word-specific
/// rotation constants, ensuring full diffusion across the 320-bit state.
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
    /// <summary>State word 0.</summary>
    public ulong S0;

    /// <summary>State word 1.</summary>
    public ulong S1;

    /// <summary>State word 2.</summary>
    public ulong S2;

    /// <summary>State word 3.</summary>
    public ulong S3;

    /// <summary>State word 4.</summary>
    public ulong S4;

    /// <summary>
    /// Applies the Ascon-p permutation with the specified number of rounds to this state.
    /// </summary>
    /// <param name="rounds">
    /// The number of rounds to apply. Must be between 1 and 12 inclusive. Common values are 6 (Ascon-p6),
    /// 8 (Ascon-p8), and 12 (Ascon-p12).
    /// </param>
    /// <remarks>
    /// Rounds are numbered 0–11; a call with <paramref name="rounds" /> = <c>r</c> applies rounds
    /// <c>12 − r</c> through <c>11</c>, preserving the standard constant-addition schedule.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Permute(int rounds)
    {
        ulong s0 = this.S0, s1 = this.S1, s2 = this.S2, s3 = this.S3, s4 = this.S4;

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
            s0 ^= BitOperations.RotateRight(s0, 19) ^ BitOperations.RotateRight(s0, 28);
            s1 ^= BitOperations.RotateRight(s1, 61) ^ BitOperations.RotateRight(s1, 39);
            s2 ^= BitOperations.RotateRight(s2,  1) ^ BitOperations.RotateRight(s2,  6);
            s3 ^= BitOperations.RotateRight(s3, 10) ^ BitOperations.RotateRight(s3, 17);
            s4 ^= BitOperations.RotateRight(s4,  7) ^ BitOperations.RotateRight(s4, 41);
        }

        this.S0 = s0; this.S1 = s1; this.S2 = s2; this.S3 = s3; this.S4 = s4;
    }

    /// <summary>
    /// XORs a 16-byte (128-bit, two-word) rate block into state words <c>S0</c> and <c>S1</c> using
    /// big-endian word order as required by the ASCON sponge construction.
    /// </summary>
    /// <param name="block">The 16-byte block to absorb. Must be exactly 16 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AbsorbRate128(ReadOnlySpan<byte> block)
    {
        this.S0 ^= BinaryPrimitives.ReadUInt64BigEndian(block);
        this.S1 ^= BinaryPrimitives.ReadUInt64BigEndian(block.Slice(8));
    }

    /// <summary>
    /// XORs a single 8-byte (64-bit, one-word) rate block into state word <c>S0</c> using big-endian
    /// word order as required by the ASCON sponge construction.
    /// </summary>
    /// <param name="block">The 8-byte block to absorb. Must be exactly 8 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AbsorbRate64(ReadOnlySpan<byte> block) =>
        this.S0 ^= BinaryPrimitives.ReadUInt64BigEndian(block);

    /// <summary>
    /// Reads the current rate (128-bit, two-word) as 16 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">Destination span. Must be at least 16 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SqueezeRate128(Span<byte> destination)
    {
        BinaryPrimitives.WriteUInt64BigEndian(destination, this.S0);
        BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(8), this.S1);
    }

    /// <summary>
    /// Reads the current rate (64-bit, one-word) as 8 bytes into <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">Destination span. Must be at least 8 bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SqueezeRate64(Span<byte> destination) =>
        BinaryPrimitives.WriteUInt64BigEndian(destination, this.S0);

    /// <summary>
    /// Zeroes all five state words, clearing any sensitive intermediate state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() =>
        this.S0 = this.S1 = this.S2 = this.S3 = this.S4 = 0UL;
}
