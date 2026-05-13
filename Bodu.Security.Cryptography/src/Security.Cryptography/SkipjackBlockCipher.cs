// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the <c>Skipjack</c> block cipher engine, operating on 64-bit blocks with an 80-bit key
/// over 32 rounds. Skipjack was designed by the United States National Security Agency (NSA) and declassified in 1998; the
/// key schedule and Rule A / Rule B alternation are binary-compatible with the NSA reference implementation published in
/// FIPS PUB 185 (1994).
/// </summary>
/// <seealso href="https://csrc.nist.gov/csrc/media/publications/fips/185/archive/1994-02-09/documents/fips185.pdf">FIPS PUB 185 — Escrowed Encryption Standard (Skipjack)</seealso>
/// <remarks>
/// <para>
/// Skipjack is a legacy symmetric block cipher whose 32 rounds alternate between two nonlinear rules known as <em>Rule A</em> and
/// <em>Rule B</em>. In this implementation the round-key byte pointer advances by one per round and each round uses a constant
/// equal to <c>k + 1</c>.
/// </para>
/// <para>
/// This cipher is included for compatibility and historical purposes only. Due to its small key and block sizes, Skipjack is not
/// considered secure for use in new systems or applications.
/// </para>
/// <list type="bullet">
/// <item>
/// <description><b>Block size:</b><c>8 bytes</c> (64 bits)</description>
/// </item>
/// <item>
/// <description><b>Key size:</b><c>10 bytes</c> (80 bits)</description>
/// </item>
/// <item>
/// <description><b>Rounds:</b><c>32</c> (16 × Rule A + 16 × Rule B)</description>
/// </item>
/// </list>
/// <para>
/// This implementation is constant-time in its control flow, but the S-box lookup table remains data-dependent. As such, this
/// implementation is <b>not</b> hardened against timing or cache-based side-channel attacks.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Skipjack"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract. Use <see cref="SkipjackBlockCipher"/> directly only
/// when composing the raw block primitive with an <see cref="IBlockCipherModeTransform"/> (for example via
/// <see cref="BlockCipherModeFactory"/>) or with an <see cref="IPaddingStrategy"/>.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs. SymmetricAlgorithm</seealso>
/// <seealso cref="Skipjack"/>
public sealed class SkipjackBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// Length of the Skipjack key is 80 bits (10 bytes).
    /// </summary>
    public const int KeySize = 80;

    // Fixed Skipjack F-table. This 256-entry byte substitution table is the only nonlinear component used by the
    // 16-bit G permutation. Every round calls G once, and G performs four F-table substitutions keyed by four
    // consecutive bytes from the 80-bit key schedule.
    private static readonly byte[] s_ftable =
    [
        0xa3, 0xd7, 0x09, 0x83, 0xf8, 0x48, 0xf6, 0xf4, 0xb3, 0x21, 0x15, 0x78, 0x99, 0xb1, 0xaf, 0xf9,
        0xe7, 0x2d, 0x4d, 0x8a, 0xce, 0x4c, 0xca, 0x2e, 0x52, 0x95, 0xd9, 0x1e, 0x4e, 0x38, 0x44, 0x28,
        0x0a, 0xdf, 0x02, 0xa0, 0x17, 0xf1, 0x60, 0x68, 0x12, 0xb7, 0x7a, 0xc3, 0xe9, 0xfa, 0x3d, 0x53,
        0x96, 0x84, 0x6b, 0xba, 0xf2, 0x63, 0x9a, 0x19, 0x7c, 0xae, 0xe5, 0xf5, 0xf7, 0x16, 0x6a, 0xa2,
        0x39, 0xb6, 0x7b, 0x0f, 0xc1, 0x93, 0x81, 0x1b, 0xee, 0xb4, 0x1a, 0xea, 0xd0, 0x91, 0x2f, 0xb8,
        0x55, 0xb9, 0xda, 0x85, 0x3f, 0x41, 0xbf, 0xe0, 0x5a, 0x58, 0x80, 0x5f, 0x66, 0x0b, 0xd8, 0x90,
        0x35, 0xd5, 0xc0, 0xa7, 0x33, 0x06, 0x65, 0x69, 0x45, 0x00, 0x94, 0x56, 0x6d, 0x98, 0x9b, 0x76,
        0x97, 0xfc, 0xb2, 0xc2, 0xb0, 0xfe, 0xdb, 0x20, 0xe1, 0xeb, 0xd6, 0xe4, 0xdd, 0x47, 0x4a, 0x1d,
        0x42, 0xed, 0x9e, 0x6e, 0x49, 0x3c, 0xcd, 0x43, 0x27, 0xd2, 0x07, 0xd4, 0xde, 0xc7, 0x67, 0x18,
        0x89, 0xcb, 0x30, 0x1f, 0x8d, 0xc6, 0x8f, 0xaa, 0xc8, 0x74, 0xdc, 0xc9, 0x5d, 0x5c, 0x31, 0xa4,
        0x70, 0x88, 0x61, 0x2c, 0x9f, 0x0d, 0x2b, 0x87, 0x50, 0x82, 0x54, 0x64, 0x26, 0x7d, 0x03, 0x40,
        0x34, 0x4b, 0x1c, 0x73, 0xd1, 0xc4, 0xfd, 0x3b, 0xcc, 0xfb, 0x7f, 0xab, 0xe6, 0x3e, 0x5b, 0xa5,
        0xad, 0x04, 0x23, 0x9c, 0x14, 0x51, 0x22, 0xf0, 0x29, 0x79, 0x71, 0x7e, 0xff, 0x8c, 0x0e, 0xe2,
        0x0c, 0xef, 0xbc, 0x72, 0x75, 0x6f, 0x37, 0xa1, 0xec, 0xd3, 0x8e, 0x62, 0x8b, 0x86, 0x10, 0xe8,
        0x08, 0x77, 0x11, 0xbe, 0x92, 0x4f, 0x24, 0xc5, 0x32, 0x36, 0x9d, 0xcf, 0xf3, 0xa6, 0xbb, 0xac,
        0x5e, 0x6c, 0xa9, 0x13, 0x57, 0x25, 0xb5, 0xe3, 0xbd, 0xa8, 0x3a, 0x01, 0x05, 0x59, 0x2a, 0x46
    ];

#pragma warning disable SA1132 // Do not combine fields
    // Expanded round-key byte streams. Round k uses _key0[k].._key3[k], equivalent to
    // key[(4k + 0) mod 10] through key[(4k + 3) mod 10]. Keeping four arrays avoids modulo arithmetic in G/H.
    private readonly int[] _key0, _key1, _key2, _key3;
#pragma warning restore SA1132 // Do not combine fields

    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipjackBlockCipher"/> class using the supplied 80-bit key.
    /// </summary>
    /// <param name="keyBytes">
    /// Exactly 10 bytes of key material.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="keyBytes"/> is not exactly 10 bytes long.
    /// </exception>
    public SkipjackBlockCipher(ReadOnlySpan<byte> keyBytes)
    {
        if (keyBytes.Length != KeySize / 8)
            throw new ArgumentException(
                CryptoResourceStrings.ArgumentException_Skipjack_InvalidKeyLength,
                nameof(keyBytes));

        this._key0 = new int[32];
        this._key1 = new int[32];
        this._key2 = new int[32];
        this._key3 = new int[32];

        // Expand the 80-bit Skipjack key into the 32 round-key byte groups consumed by G/H.
        // The specification advances the key byte pointer by four bytes inside G for each round and wraps mod 10.
        // Precomputing these positions gives round k direct access to cv0..cv3 without changing the algorithm.
        for (var i = 0; i < 32; i++)
        {
            this._key0[i] = keyBytes[(i * 4 + 0) % 10];
            this._key1[i] = keyBytes[(i * 4 + 1) % 10];
            this._key2[i] = keyBytes[(i * 4 + 2) % 10];
            this._key3[i] = keyBytes[(i * 4 + 3) % 10];
        }
    }

    /// <inheritdoc />
    /// <value>Length of the Skipjack block is 64 bits (8 bytes).</value>
    /// <remarks>The block size is fixed at 64 bits (8 bytes) and cannot be changed.</remarks>
    public int BlockSize => Skipjack.SkipjackBlockSize;

    /// <summary>
    /// Decrypts a single 64-bit ciphertext block.
    /// </summary>
    /// <param name="input">
    /// Ciphertext of exactly 8 bytes.
    /// </param>
    /// <param name="output">
    /// Buffer that receives the decrypted plaintext. Must be exactly 8 bytes.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input"/> or <paramref name="output"/> is not exactly 8 bytes.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The cipher instance has been disposed.
    /// </exception>
    /// <remarks>
    /// Mirrors the FIPS PUB 185 decrypt sequence, including the word-order swap in the input/output stages. Decryption
    /// applies the inverse round rules in reverse round-key order, using H as the inverse of G.
    /// </remarks>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSize / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSize / 8);
        this.ThrowIfDisposed();

        // Ciphertext is emitted by encryption as w1,w2,w3,w4. The inverse formulation loads it as
        // w2,w1,w4,w3 so that the following reverse Rule B/Rule A operations mirror the reference implementation.
        var w2 = (input[0] << 8) + (input[1] & 0xff);
        var w1 = (input[2] << 8) + (input[3] & 0xff);
        var w4 = (input[4] << 8) + (input[5] & 0xff);
        var w3 = (input[6] << 8) + (input[7] & 0xff);

        // Start from round 32. The round counter k is zero-based internally, while the Skipjack counter value
        // inserted into Rule A/Rule B is k + 1.
        var k = 31;

        // Undo the same 32 rounds in reverse: two cycles of eight inverse Rule B rounds followed by eight inverse
        // Rule A rounds. This is the reverse of encryption's A^8, B^8, A^8, B^8 schedule.
        for (var t = 0; t < 2; t++)
        {
            // Inverse Rule B for eight rounds. H(k, w1) reverses the G permutation used during encryption.
            for (var i = 0; i < 8; i++)
            {
                var tmp = w4;
                w4 = w3;
                w3 = w2;
                w2 = this.H(k, w1);
                w1 = w2 ^ tmp ^ (k + 1);
                k--;
            }

            // Inverse Rule A for eight rounds.
            for (var i = 0; i < 8; i++)
            {
                var tmp = w4;
                w4 = w3;
                w3 = w1 ^ w2 ^ (k + 1);
                w2 = this.H(k, w1);
                w1 = tmp;
                k--;
            }
        }

        // Store the recovered plaintext in the Skipjack 4-word big-endian block order.
        output[0] = (byte)(w2 >> 8);
        output[1] = (byte)w2;
        output[2] = (byte)(w1 >> 8);
        output[3] = (byte)w1;
        output[4] = (byte)(w4 >> 8);
        output[5] = (byte)w4;
        output[6] = (byte)(w3 >> 8);
        output[7] = (byte)w3;
    }

    /// <summary>
    /// Securely clears key material and marks the instance as disposed.
    /// </summary>
    public void Dispose()
    {
        if (!this._disposed)
        {
            CryptoHelpers.Clear(this._key0);
            CryptoHelpers.Clear(this._key1);
            CryptoHelpers.Clear(this._key2);
            CryptoHelpers.Clear(this._key3);

            this._disposed = true;
        }
    }

    /// <summary>
    /// Encrypts a single 64-bit block.
    /// </summary>
    /// <param name="input">
    /// The plaintext block to encrypt. Must be exactly 8 bytes.
    /// </param>
    /// <param name="output">
    /// Buffer that receives the 8-byte ciphertext. Must be exactly 8 bytes.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input"/> or <paramref name="output"/> is not exactly 8 bytes.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The cipher instance has been disposed.
    /// </exception>
    /// <remarks>
    /// The routine implements the FIPS PUB 185 Skipjack schedule: the key pointer advances by four key bytes inside G
    /// for each round, and the rule counter injected into each round is <c>k + 1</c>.
    /// </remarks>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSize / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSize / 8);
        this.ThrowIfDisposed();

        // Split the 64-bit plaintext block into four big-endian 16-bit words W1..W4, matching the Skipjack
        // reference algorithm's word-oriented round descriptions.
        var w1 = (input[0] << 8) + (input[1] & 0xff);
        var w2 = (input[2] << 8) + (input[3] & 0xff);
        var w3 = (input[4] << 8) + (input[5] & 0xff);
        var w4 = (input[6] << 8) + (input[7] & 0xff);

        // k is the zero-based round-key index. The Skipjack rule counter value used by the round equations is k + 1.
        var k = 0;

        // Skipjack encryption applies 32 rounds in four groups:
        //   rounds  1.. 8: Rule A
        //   rounds  9..16: Rule B
        //   rounds 17..24: Rule A
        //   rounds 25..32: Rule B
        for (var t = 0; t < 2; t++)
        {
            // Rule A: shift the four 16-bit words and combine G(k, W1), the outgoing W4, and the round counter.
            for (var i = 0; i < 8; i++)
            {
                var tmp = w4;
                w4 = w3;
                w3 = w2;
                w2 = this.G(k, w1);
                w1 = w2 ^ tmp ^ (k + 1);
                k++;
            }

            // Rule B: use the same G(k, W1) primitive, but feed the nonlinear/counter mix into W3 and rotate W4 into W1.
            for (var i = 0; i < 8; i++)
            {
                var tmp = w4;
                w4 = w3;
                w3 = w1 ^ w2 ^ (k + 1);
                w2 = this.G(k, w1);
                w1 = tmp;
                k++;
            }
        }

        // Store the ciphertext as four big-endian 16-bit words in W1..W4 order.
        output[0] = (byte)(w1 >> 8);
        output[1] = (byte)w1;
        output[2] = (byte)(w2 >> 8);
        output[3] = (byte)w2;
        output[4] = (byte)(w3 >> 8);
        output[5] = (byte)w3;
        output[6] = (byte)(w4 >> 8);
        output[7] = (byte)w4;
    }

    /// <summary>
    /// Reads a big-endian 16-bit unsigned integer from the specified byte span.
    /// </summary>
    /// <param name="s">
    /// The source byte span.
    /// </param>
    /// <param name="o">
    /// The byte offset at which to read.
    /// </param>
    /// <returns>
    /// The 16-bit value read in big-endian order.
    /// </returns>
    /// <remarks>
    /// Skipjack represents each 64-bit block as four big-endian 16-bit words. This helper keeps that convention
    /// explicit if the block load/store code is later refactored to use helper calls.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadBE16(ReadOnlySpan<byte> s, int o) =>
        (ushort)((s[o] << 8) | s[o + 1]);

    /// <summary>
    /// Writes a 16-bit unsigned integer to the specified byte span in big-endian order.
    /// </summary>
    /// <param name="d">
    /// The destination byte span.
    /// </param>
    /// <param name="o">
    /// The byte offset at which to write.
    /// </param>
    /// <param name="v">
    /// The 16-bit value to write.
    /// </param>
    /// <remarks>
    /// Skipjack represents each 64-bit block as four big-endian 16-bit words. This helper keeps that convention
    /// explicit if the block load/store code is later refactored to use helper calls.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBE16(Span<byte> d, int o, ushort v)
    {
        d[o] = (byte)(v >> 8);
        d[o + 1] = (byte)v;
    }

    /// <summary>
    /// Applies the forward Skipjack <c>G</c> permutation to a 16-bit word for the specified round.
    /// </summary>
    /// <param name="k">
    /// The zero-based round-key index, in the range 0..31.
    /// </param>
    /// <param name="w">
    /// The 16-bit input word.
    /// </param>
    /// <returns>
    /// The 16-bit output of the forward <c>G</c> permutation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The Skipjack <c>G</c> permutation splits <paramref name="w"/> into two bytes and applies four keyed F-table
    /// substitutions. For round <paramref name="k"/>, the key bytes are equivalent to
    /// <c>key[(4k + 0) mod 10]</c> through <c>key[(4k + 3) mod 10]</c>.
    /// </para>
    /// <para>
    /// The returned word is formed from the final two internal bytes as <c>g5 || g6</c>.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int G(int k, int w)
    {
        int g1, g2, g3, g4, g5, g6;

        // Split the 16-bit input into the two starting bytes g1 || g2.
        g1 = (w >> 8) & 0xff;
        g2 = w & 0xff;

        // Four alternating F-table substitutions and XOR feedback steps implement the Skipjack G permutation.
        g3 = s_ftable[g2 ^ this._key0[k]] ^ g1;
        g4 = s_ftable[g3 ^ this._key1[k]] ^ g2;
        g5 = s_ftable[g4 ^ this._key2[k]] ^ g3;
        g6 = s_ftable[g5 ^ this._key3[k]] ^ g4;

        return (g5 << 8) + g6;
    }

    /// <summary>
    /// Applies the inverse Skipjack <c>H</c> permutation, where <c>H = G⁻¹</c>, to a 16-bit word for the specified round.
    /// </summary>
    /// <param name="k">
    /// The zero-based round-key index, in the range 0..31.
    /// </param>
    /// <param name="w">
    /// The 16-bit input word to inverse-permute.
    /// </param>
    /// <returns>
    /// The 16-bit output of the inverse <c>H</c> permutation.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Decryption uses <c>H</c> to reverse the <c>G</c> permutation applied during encryption. It consumes the same
    /// round-key bytes as <c>G</c>, but applies them in reverse substitution order.
    /// </para>
    /// <para>
    /// The input word is interpreted as <c>h2 || h1</c> so the final return can reconstruct the original
    /// <c>g1 || g2</c> value as <c>h6 || h5</c>.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int H(int k, int w)
    {
        // Split the 16-bit input into the terminal bytes of G, then unwind the four F-table/XOR steps.
        var h1 = w & 0xff;
        var h2 = (w >> 8) & 0xff;

        var h3 = s_ftable[h2 ^ this._key3[k]] ^ h1;
        var h4 = s_ftable[h3 ^ this._key2[k]] ^ h2;
        var h5 = s_ftable[h4 ^ this._key1[k]] ^ h3;
        var h6 = s_ftable[h5 ^ this._key0[k]] ^ h4;

        return (h6 << 8) + h5;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }
}
