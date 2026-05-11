// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipherBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed Serpent block cipher engines, providing the shared S-boxes, bitsliced linear
/// transform, prekey recurrence, and resource-disposal plumbing used by the standard <c>Serpent-128</c> variant and the
/// non-standard wide-block tweakable variants (<c>Serpent-256</c>, <c>Serpent-512</c>, <c>Serpent-1024</c>).
/// </summary>
/// <remarks>
/// <para>
/// Derived classes supply the state width (in 32-bit words), the round count, and their own <see cref="Encrypt"/> and
/// <see cref="Decrypt"/> implementations. The base class exposes the Serpent S-boxes <c>S0..S7</c> and their inverses, the
/// bitsliced linear transform <see cref="LinearTransform"/> and its inverse <see cref="InverseLinearTransform"/>, and the
/// round-key expansion helper <see cref="ExpandPrekeys"/> driven by the golden-ratio constant
/// <c>phi = 0x9E3779B9</c>.
/// </para>
/// <para>
/// External callers cannot derive new variants: the constructor and protected members are scoped <c>private protected</c>.
/// Use <see cref="Serpent128Cipher"/> or one of the wide-block <see cref="Serpent256Cipher"/> / <see cref="Serpent512Cipher"/>
/// / <see cref="Serpent1024Cipher"/> types directly, or compose with <see cref="IBlockCipherModeTransform"/> via
/// <see cref="BlockCipherModeFactory"/>.
/// </para>
/// </remarks>
public abstract partial class SerpentBlockCipherBase
    : IBlockCipher
{
    /// <summary>
    /// The golden-ratio fractional constant used in the Serpent prekey recurrence.
    /// </summary>
    private protected const uint Phi = 0x9E3779B9u;

    /// <summary>
    /// Indicates whether the instance has been disposed.
    /// </summary>
    private protected bool _disposed;

    /// <summary>
    /// The eight Serpent S-boxes, indexed <c>[sboxIndex * 16 + nibble]</c>.
    /// </summary>
    private static readonly byte[] s_sBoxes =
    [
        // S0
        3, 8, 15, 1, 10, 6, 5, 11, 14, 13, 4, 2, 7, 0, 9, 12,
        // S1
        15, 12, 2, 7, 9, 0, 5, 10, 1, 11, 14, 8, 6, 13, 3, 4,
        // S2
        8, 6, 7, 9, 3, 12, 10, 15, 13, 1, 14, 4, 0, 11, 5, 2,
        // S3
        0, 15, 11, 8, 12, 9, 6, 3, 13, 1, 2, 4, 10, 7, 5, 14,
        // S4
        1, 15, 8, 3, 12, 0, 11, 6, 2, 5, 4, 10, 9, 14, 7, 13,
        // S5
        15, 5, 2, 11, 4, 10, 9, 12, 0, 3, 14, 8, 13, 6, 7, 1,
        // S6
        7, 2, 12, 5, 8, 4, 6, 11, 14, 9, 1, 15, 13, 3, 10, 0,
        // S7
        1, 13, 15, 0, 14, 8, 2, 11, 7, 4, 12, 10, 9, 3, 5, 6,
    ];

    /// <summary>
    /// The eight Serpent inverse S-boxes, indexed <c>[sboxIndex * 16 + nibble]</c>.
    /// </summary>
    private static readonly byte[] s_invSBoxes =
    [
        // InvS0
        13, 3, 11, 0, 10, 6, 5, 12, 1, 14, 4, 7, 15, 9, 8, 2,
        // InvS1
        5, 8, 2, 14, 15, 6, 12, 3, 11, 4, 7, 9, 1, 13, 10, 0,
        // InvS2
        12, 9, 15, 4, 11, 14, 1, 2, 0, 3, 6, 13, 5, 8, 10, 7,
        // InvS3
        0, 9, 10, 7, 11, 14, 6, 13, 3, 5, 12, 2, 4, 8, 15, 1,
        // InvS4
        5, 0, 8, 3, 10, 9, 7, 14, 2, 12, 11, 6, 4, 15, 13, 1,
        // InvS5
        8, 15, 2, 9, 4, 1, 13, 14, 11, 6, 5, 3, 7, 12, 10, 0,
        // InvS6
        15, 10, 1, 13, 5, 3, 6, 0, 4, 9, 14, 7, 2, 12, 8, 11,
        // InvS7
        3, 0, 6, 13, 9, 14, 15, 8, 5, 12, 11, 7, 10, 1, 4, 2,
    ];

    /// <summary>
    /// Finalises the instance by releasing unmanaged resources before it is reclaimed by garbage collection.
    /// </summary>
    ~SerpentBlockCipherBase()
    {
        this.Dispose(false);
    }

    /// <inheritdoc />
    public abstract int BlockSize { get; }

    /// <inheritdoc />
    public abstract void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public abstract void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all internal buffers and sensitive material.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> when invoked from <see cref="Dispose()"/>; <see langword="false"/> when invoked from the
    /// finaliser.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        this._disposed = true;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }

    /// <summary>
    /// Applies the Serpent S-box identified by <paramref name="sBoxIndex"/> to the four 32-bit words in bitsliced form.
    /// </summary>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    /// <remarks>
    /// Each of the 32 bit columns across the four words is treated as a 4-bit nibble (bit 0 from <paramref name="x0"/>, bit 1
    /// from <paramref name="x1"/>, bit 2 from <paramref name="x2"/>, bit 3 from <paramref name="x3"/>) and mapped through the
    /// selected S-box. This is the canonical bitsliced S-box application used by Serpent and is intentionally portable rather
    /// than gate-optimised; correctness is anchored against the standard Serpent test vectors.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void ApplySBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        ReadOnlySpan<byte> table = s_sBoxes.AsSpan(sBoxIndex * 16, 16);
        uint y0 = 0, y1 = 0, y2 = 0, y3 = 0;

        for (var i = 0; i < 32; i++)
        {
            var nibble = (int)(((x0 >> i) & 1u)
                             | (((x1 >> i) & 1u) << 1)
                             | (((x2 >> i) & 1u) << 2)
                             | (((x3 >> i) & 1u) << 3));

            int s = table[nibble];
            y0 |= (uint)(s & 1) << i;
            y1 |= (uint)((s >> 1) & 1) << i;
            y2 |= (uint)((s >> 2) & 1) << i;
            y3 |= (uint)((s >> 3) & 1) << i;
        }

        x0 = y0;
        x1 = y1;
        x2 = y2;
        x3 = y3;
    }

    /// <summary>
    /// Applies the inverse of the Serpent S-box identified by <paramref name="sBoxIndex"/> to the four 32-bit words in
    /// bitsliced form.
    /// </summary>
    /// <param name="sBoxIndex">The S-box index in the range <c>0..7</c>.</param>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void ApplyInverseSBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        ReadOnlySpan<byte> table = s_invSBoxes.AsSpan(sBoxIndex * 16, 16);
        uint y0 = 0, y1 = 0, y2 = 0, y3 = 0;

        for (var i = 0; i < 32; i++)
        {
            var nibble = (int)(((x0 >> i) & 1u)
                             | (((x1 >> i) & 1u) << 1)
                             | (((x2 >> i) & 1u) << 2)
                             | (((x3 >> i) & 1u) << 3));

            int s = table[nibble];
            y0 |= (uint)(s & 1) << i;
            y1 |= (uint)((s >> 1) & 1) << i;
            y2 |= (uint)((s >> 2) & 1) << i;
            y3 |= (uint)((s >> 3) & 1) << i;
        }

        x0 = y0;
        x1 = y1;
        x2 = y2;
        x3 = y3;
    }

    /// <summary>
    /// Applies the Serpent bitsliced linear transform <c>L</c> to a four-word sub-state.
    /// </summary>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void LinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        x0 = BitOperations.RotateLeft(x0, 13);
        x2 = BitOperations.RotateLeft(x2, 3);
        x1 ^= x0 ^ x2;
        x3 ^= x2 ^ (x0 << 3);
        x1 = BitOperations.RotateLeft(x1, 1);
        x3 = BitOperations.RotateLeft(x3, 7);
        x0 ^= x1 ^ x3;
        x2 ^= x3 ^ (x1 << 7);
        x0 = BitOperations.RotateLeft(x0, 5);
        x2 = BitOperations.RotateLeft(x2, 22);
    }

    /// <summary>
    /// Applies the inverse of the Serpent bitsliced linear transform to a four-word sub-state.
    /// </summary>
    /// <param name="x0">The first state word, modified in place.</param>
    /// <param name="x1">The second state word, modified in place.</param>
    /// <param name="x2">The third state word, modified in place.</param>
    /// <param name="x3">The fourth state word, modified in place.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static void InverseLinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        x2 = BitOperations.RotateRight(x2, 22);
        x0 = BitOperations.RotateRight(x0, 5);
        x2 ^= x3 ^ (x1 << 7);
        x0 ^= x1 ^ x3;
        x3 = BitOperations.RotateRight(x3, 7);
        x1 = BitOperations.RotateRight(x1, 1);
        x3 ^= x2 ^ (x0 << 3);
        x1 ^= x0 ^ x2;
        x2 = BitOperations.RotateRight(x2, 3);
        x0 = BitOperations.RotateRight(x0, 13);
    }

    /// <summary>
    /// Runs the Serpent prekey recurrence to populate the prekey buffer.
    /// </summary>
    /// <param name="seed">
    /// The first <paramref name="window"/> entries of <paramref name="prekeys"/>, already seeded with the key material.
    /// </param>
    /// <param name="prekeys">The buffer receiving the expanded prekey sequence. Must start with <paramref name="seed"/>.</param>
    /// <param name="window">
    /// The recurrence window in words. Must be at least 8 to satisfy the Serpent recurrence indices <c>i-8, i-5, i-3, i-1</c>.
    /// </param>
    /// <remarks>
    /// The recurrence is the Serpent key schedule expansion
    /// <c>w[i] = ROL(w[i-window] ^ w[i-5] ^ w[i-3] ^ w[i-1] ^ phi ^ i, 11)</c>
    /// for <c>i = 0..prekeys.Length - window - 1</c>. The caller initialises <c>prekeys[0..window-1]</c> with the seed and
    /// this helper computes the remaining entries in place. Standard Serpent-128 uses <c>window = 8</c>; the wide-block
    /// variants use larger windows matched to their state width.
    /// </remarks>
    private protected static void ExpandPrekeys(ReadOnlySpan<uint> seed, Span<uint> prekeys, int window)
    {
        seed.CopyTo(prekeys);

        for (var i = 0; i + window < prekeys.Length; i++)
        {
            var value = prekeys[i] ^ prekeys[i + window - 5] ^ prekeys[i + window - 3] ^ prekeys[i + window - 1] ^ Phi ^ (uint)i;
            prekeys[i + window] = BitOperations.RotateLeft(value, 11);
        }
    }

    /// <summary>
    /// Returns the Serpent S-box index used by round-key <paramref name="roundIndex"/> in the key schedule.
    /// </summary>
    /// <param name="roundIndex">The round-key index, in the range <c>0..R</c>.</param>
    /// <returns>The S-box index, in the range <c>0..7</c>.</returns>
    /// <remarks>
    /// Serpent's key schedule applies the S-boxes to successive prekey words in descending cyclic order:
    /// <c>K_0 → S3, K_1 → S2, K_2 → S1, K_3 → S0, K_4 → S7, …</c>, following the standard formula
    /// <c>(3 − roundIndex) mod 8</c>. The same ordering is reused for the wide-block variants so that
    /// <c>K_0</c> always uses <c>S3</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static int KeyScheduleSBoxIndex(int roundIndex)
    {
        var value = (3 - roundIndex) & 7;
        return value;
    }
}
