// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128Cipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the canonical <c>Serpent</c> block cipher, which operates on 128-bit (16-byte) blocks using a 128, 192, or
/// 256-bit key.
/// </summary>
/// <remarks>
/// <para>
/// Serpent is a 32-round substitution–permutation network designed by Ross Anderson, Eli Biham, and Lars Knudsen as an
/// Advanced Encryption Standard (AES) candidate. Each round applies a round-key XOR, one of the eight 4-bit S-boxes
/// <c>S0..S7</c>, and the bitsliced linear transformation <c>L</c>. The final round replaces <c>L</c> with a post-round key
/// XOR. Shorter keys are padded to 256 bits by appending a <c>1</c> bit followed by zeros, per the Serpent specification.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Serpent128"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract. Use <see cref="Serpent128Cipher"/> directly only
/// when composing the raw block primitive with an <see cref="IBlockCipherModeTransform"/> or <see cref="IPaddingStrategy"/>.
/// </para>
/// </remarks>
/// <seealso cref="Serpent128"/>
public sealed class Serpent128Cipher
    : SerpentBlockCipherBase
{
    /// <summary>
    /// The Serpent block size in bits.
    /// </summary>
    public const int BlockSizeBits = 128;

    /// <summary>
    /// The number of cipher rounds executed by Serpent.
    /// </summary>
    private const int RoundCount = 32;

    /// <summary>
    /// The number of 32-bit round-key words (<c>(RoundCount + 1) * 4 = 132</c>).
    /// </summary>
    private const int RoundKeyWordCount = (RoundCount + 1) * 4;

    /// <summary>
    /// The expanded round keys (<c>K_0..K_32</c>), each four 32-bit words, laid out contiguously as 132 words.
    /// </summary>
    private readonly uint[] _roundKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent128Cipher"/> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The Serpent key. Length must be 16, 24, or 32 bytes (128, 192, or 256 bits). Shorter keys are padded to 256 bits per
    /// the Serpent specification.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> does not have a length of 16, 24, or 32 bytes.
    /// </exception>
    public Serpent128Cipher(ReadOnlySpan<byte> key)
    {
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, "128, 192, 256"),
                nameof(key));

        this._roundKeys = new uint[RoundKeyWordCount];
        BuildRoundKeys(key, this._roundKeys);
    }

    /// <inheritdoc />
    public override int BlockSize => BlockSizeBits;

    /// <inheritdoc />
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != BlockSizeBits / 8 || output.Length != BlockSizeBits / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidBlockLength, BlockSizeBits / 8));

        var x0 = BinaryReadUInt32LE(input, 0);
        var x1 = BinaryReadUInt32LE(input, 4);
        var x2 = BinaryReadUInt32LE(input, 8);
        var x3 = BinaryReadUInt32LE(input, 12);

        var rk = this._roundKeys;

        for (var r = 0; r < RoundCount - 1; r++)
        {
            var k = r * 4;
            x0 ^= rk[k];
            x1 ^= rk[k + 1];
            x2 ^= rk[k + 2];
            x3 ^= rk[k + 3];

            ApplySBox(r & 7, ref x0, ref x1, ref x2, ref x3);
            LinearTransform(ref x0, ref x1, ref x2, ref x3);
        }

        // Final round: no linear transform, followed by a post-round key XOR.
        var kFinal = (RoundCount - 1) * 4;
        x0 ^= rk[kFinal];
        x1 ^= rk[kFinal + 1];
        x2 ^= rk[kFinal + 2];
        x3 ^= rk[kFinal + 3];

        ApplySBox((RoundCount - 1) & 7, ref x0, ref x1, ref x2, ref x3);

        var kPost = RoundCount * 4;
        x0 ^= rk[kPost];
        x1 ^= rk[kPost + 1];
        x2 ^= rk[kPost + 2];
        x3 ^= rk[kPost + 3];

        BinaryWriteUInt32LE(output, 0, x0);
        BinaryWriteUInt32LE(output, 4, x1);
        BinaryWriteUInt32LE(output, 8, x2);
        BinaryWriteUInt32LE(output, 12, x3);
    }

    /// <inheritdoc />
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != BlockSizeBits / 8 || output.Length != BlockSizeBits / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidBlockLength, BlockSizeBits / 8));

        var x0 = BinaryReadUInt32LE(input, 0);
        var x1 = BinaryReadUInt32LE(input, 4);
        var x2 = BinaryReadUInt32LE(input, 8);
        var x3 = BinaryReadUInt32LE(input, 12);

        var rk = this._roundKeys;

        // Reverse of the final round.
        var kPost = RoundCount * 4;
        x0 ^= rk[kPost];
        x1 ^= rk[kPost + 1];
        x2 ^= rk[kPost + 2];
        x3 ^= rk[kPost + 3];

        ApplyInverseSBox((RoundCount - 1) & 7, ref x0, ref x1, ref x2, ref x3);

        var kFinal = (RoundCount - 1) * 4;
        x0 ^= rk[kFinal];
        x1 ^= rk[kFinal + 1];
        x2 ^= rk[kFinal + 2];
        x3 ^= rk[kFinal + 3];

        // Reverse of the remaining rounds.
        for (var r = RoundCount - 2; r >= 0; r--)
        {
            InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);
            ApplyInverseSBox(r & 7, ref x0, ref x1, ref x2, ref x3);

            var k = r * 4;
            x0 ^= rk[k];
            x1 ^= rk[k + 1];
            x2 ^= rk[k + 2];
            x3 ^= rk[k + 3];
        }

        BinaryWriteUInt32LE(output, 0, x0);
        BinaryWriteUInt32LE(output, 4, x1);
        BinaryWriteUInt32LE(output, 8, x2);
        BinaryWriteUInt32LE(output, 12, x3);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (this._disposed) return;

        if (disposing)
            CryptoHelpers.Clear(this._roundKeys);

        base.Dispose(disposing);
    }

    /// <summary>
    /// Reads a little-endian <see cref="uint"/> from <paramref name="buffer"/> at the specified <paramref name="offset"/>.
    /// </summary>
    /// <param name="buffer">The source byte span.</param>
    /// <param name="offset">The byte offset at which to read.</param>
    /// <returns>The little-endian <see cref="uint"/> value read from <paramref name="buffer"/>.</returns>
    private static uint BinaryReadUInt32LE(ReadOnlySpan<byte> buffer, int offset) =>
        System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(offset, 4));

    /// <summary>
    /// Writes <paramref name="value"/> to <paramref name="buffer"/> at the specified <paramref name="offset"/> in
    /// little-endian byte order.
    /// </summary>
    /// <param name="buffer">The destination byte span.</param>
    /// <param name="offset">The byte offset at which to write.</param>
    /// <param name="value">The value to write.</param>
    private static void BinaryWriteUInt32LE(Span<byte> buffer, int offset, uint value) =>
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer.Slice(offset, 4), value);

    /// <summary>
    /// Expands <paramref name="key"/> into the 132-word round-key schedule.
    /// </summary>
    /// <param name="key">
    /// The Serpent key (16, 24, or 32 bytes). Keys shorter than 32 bytes are padded per the Serpent specification.
    /// </param>
    /// <param name="roundKeys">The destination buffer, which must have exactly <see cref="RoundKeyWordCount"/> entries.</param>
    /// <remarks>
    /// Pads the key to 256 bits by appending a <c>1</c> bit immediately after the key material and then zeros. Seeds the prekey
    /// recurrence with the 8 padded words, applies the Serpent recurrence for 132 words, then applies the rotating S-box
    /// schedule in groups of four words to produce <c>K_0..K_32</c>.
    /// </remarks>
    private static void BuildRoundKeys(ReadOnlySpan<byte> key, uint[] roundKeys)
    {
        Span<byte> paddedKey = stackalloc byte[32];
        paddedKey.Clear();
        key.CopyTo(paddedKey);

        if (key.Length < 32)
            paddedKey[key.Length] = 0x01;

        Span<uint> seed = stackalloc uint[8];
        for (var i = 0; i < 8; i++)
            seed[i] = BinaryReadUInt32LE(paddedKey, i * 4);

        // Prekey layout: w[-8..-1] then w[0..131] laid out contiguously → 140 words.
        Span<uint> prekeys = stackalloc uint[8 + RoundKeyWordCount];
        ExpandPrekeys(seed, prekeys, 8);

        // Apply the rotating S-box schedule to successive 4-word groups of the generated prekey tail, producing K_0..K_32.
        for (var r = 0; r <= RoundCount; r++)
        {
            var src = 8 + r * 4;
            var x0 = prekeys[src];
            var x1 = prekeys[src + 1];
            var x2 = prekeys[src + 2];
            var x3 = prekeys[src + 3];

            ApplySBox(KeyScheduleSBoxIndex(r), ref x0, ref x1, ref x2, ref x3);

            var dst = r * 4;
            roundKeys[dst] = x0;
            roundKeys[dst + 1] = x1;
            roundKeys[dst + 2] = x2;
            roundKeys[dst + 3] = x3;
        }

        CryptoHelpers.Clear(prekeys);
        CryptoHelpers.Clear(seed);
        CryptoHelpers.Clear(paddedKey);
    }
}
