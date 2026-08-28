// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20StreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the Salsa20 keystream primitive specified by Daniel J. Bernstein, plus the HSalsa20 subkey-derivation
/// function used by the extended-nonce XSalsa20 construction. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher binds a 128- or 256-bit key, a 64-bit nonce, and an initial 64-bit block counter at construction and
/// produces 64-byte keystream blocks on demand through <see cref="NextKeystreamBlock(Span{byte})" />. It is a pure
/// keystream generator and never observes plaintext; <see cref="StreamCipherTransform" /> combines the keystream with
/// the message by XOR.
/// </para>
/// <para>
/// The core function follows Bernstein's specification: a 4×4 matrix of 32-bit little-endian words seeded with the
/// constant <c>"expand 32-byte k"</c> (256-bit key) or <c>"expand 16-byte k"</c> (128-bit key, with the 16 key bytes
/// repeated), the key, a 64-bit nonce, and a 64-bit block counter, transformed by twenty rounds (ten column-round /
/// row-round double rounds) of the quarter-round operation, then added word-wise to the original state and serialized
/// little-endian.
/// </para>
/// </remarks>
/// <seealso href="https://cr.yp.to/snuffle/spec.pdf">Salsa20 specification (Bernstein, 2005)</seealso>
/// <seealso href="https://cr.yp.to/snuffle/xsalsa-20081128.pdf">Extending the Salsa20 nonce (Bernstein, 2008)</seealso>
/// <seealso cref="Salsa20" /> <seealso cref="XSalsa20" />
internal sealed class Salsa20StreamCipher
    : IStreamCipher
{
    /// <summary>The 128-bit key length, in bytes.</summary>
    internal const int KeySize128Bytes = 16;

    /// <summary>The 256-bit key length, in bytes.</summary>
    internal const int KeySize256Bytes = 32;

    /// <summary>The Salsa20 nonce length, in bytes (64 bits).</summary>
    internal const int NonceSizeBytes = 8;

    /// <summary>The HSalsa20 input nonce length, in bytes (128 bits), consumed during XSalsa20 subkey derivation.</summary>
    internal const int HSalsaNonceSizeBytes = 16;

    /// <summary>The keystream block length, in bytes (512 bits).</summary>
    internal const int BlockSizeBytes = 64;

    /// <summary>The number of Salsa20 rounds (ten column-round / row-round double rounds).</summary>
    private const int Rounds = 20;

    /// <summary>The first little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, seeded into state word 0 for a 256-bit key.</summary>
    private const uint Sigma0 = 0x61707865;

    /// <summary>The second little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, seeded into state word 5 for a 256-bit key.</summary>
    private const uint Sigma1 = 0x3320646e;

    /// <summary>The third little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, seeded into state word 10 for a 256-bit key.</summary>
    private const uint Sigma2 = 0x79622d32;

    /// <summary>The fourth little-endian word of the ASCII constant <c>"expand 32-byte k"</c>, seeded into state word 15 for a 256-bit key.</summary>
    private const uint Sigma3 = 0x6b206574;

    /// <summary>The first little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, seeded into state word 0 for a 128-bit key.</summary>
    private const uint Tau0 = 0x61707865;

    /// <summary>The second little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, seeded into state word 5 for a 128-bit key.</summary>
    private const uint Tau1 = 0x3120646e;

    /// <summary>The third little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, seeded into state word 10 for a 128-bit key.</summary>
    private const uint Tau2 = 0x79622d36;

    /// <summary>The fourth little-endian word of the ASCII constant <c>"expand 16-byte k"</c>, seeded into state word 15 for a 128-bit key.</summary>
    private const uint Tau3 = 0x6b206574;

    /// <summary>The 16 state words (j0..j15) of the Salsa20 matrix, with words 8 and 9 reserved for the block counter.</summary>
    private readonly uint[] _state = new uint[16];

    /// <summary>The block counter value supplied at construction for the first keystream block.</summary>
    private readonly ulong _initialCounter;

    /// <summary>The block counter for the next keystream block.</summary>
    private ulong _counter;

    /// <summary>Indicates whether the 64-bit block counter has wrapped back to its initial value, exhausting the keystream.</summary>
    private bool _counterExhausted;

    /// <summary>Indicates whether the instance has been disposed and its state cleared.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Salsa20StreamCipher" /> class from the supplied key, nonce, and
    /// initial block counter.
    /// </summary>
    /// <param name="key">The 16-byte (128-bit) or 32-byte (256-bit) key.</param>
    /// <param name="nonce">The 8-byte (64-bit) nonce.</param>
    /// <param name="initialCounter">The 64-bit block counter for the first keystream block.</param>
    /// <remarks>
    /// The key and nonce are expanded into the Salsa20 state matrix; the caller's buffers are not retained. The caller
    /// is responsible for validating the lengths.
    /// </remarks>
    internal Salsa20StreamCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong initialCounter)
    {
        ExpandKey(_state, key, nonce);

        _initialCounter = initialCounter;
        _counter = initialCounter;
    }

    /// <inheritdoc />
    public int BlockSize => BlockSizeBytes;

    /// <inheritdoc />
    public void NextKeystreamBlock(Span<byte> destination)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, BlockSizeBytes);

        if (_counterExhausted)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_StreamCounterExhausted);

        // Inject the 64-bit block counter into words 8 (low) and 9 (high).
        _state[8] = (uint)_counter;
        _state[9] = (uint)(_counter >> 32);

        _counter++;
        if (_counter == _initialCounter)
            _counterExhausted = true;

        Core(_state, destination);
    }

    /// <summary>
    /// Derives a 32-byte subkey from a key and a 16-byte nonce using HSalsa20, as required by the XSalsa20
    /// extended-nonce construction.
    /// </summary>
    /// <param name="key">The 32-byte (256-bit) key.</param>
    /// <param name="nonce">The first 16 bytes (128 bits) of the 24-byte XSalsa20 nonce.</param>
    /// <param name="subkey">A span of at least 32 bytes that receives the derived subkey.</param>
    /// <remarks>
    /// HSalsa20 runs the Salsa20 round function over a state seeded with the constant, key, and 128-bit nonce, but —
    /// unlike the keystream core — does <em>not</em> add the original state back in. The subkey is taken from the
    /// diagonal words of the transformed state (positions 0, 5, 10, 15, 6, 7, 8, 9).
    /// </remarks>
    internal static void HSalsa20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> subkey)
    {
        uint x0 = Sigma0;
        uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(0, 4));
        uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4, 4));
        uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8, 4));
        uint x4 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));
        uint x5 = Sigma1;
        uint x6 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        uint x7 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        uint x8 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));
        uint x9 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(12, 4));
        uint x10 = Sigma2;
        uint x11 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(16, 4));
        uint x12 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(20, 4));
        uint x13 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(24, 4));
        uint x14 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(28, 4));
        uint x15 = Sigma3;

        for (int i = 0; i < Rounds; i += 2)
        {
            // Column round.
            QuarterRound(ref x0, ref x4, ref x8, ref x12);
            QuarterRound(ref x5, ref x9, ref x13, ref x1);
            QuarterRound(ref x10, ref x14, ref x2, ref x6);
            QuarterRound(ref x15, ref x3, ref x7, ref x11);

            // Row round.
            QuarterRound(ref x0, ref x1, ref x2, ref x3);
            QuarterRound(ref x5, ref x6, ref x7, ref x4);
            QuarterRound(ref x10, ref x11, ref x8, ref x9);
            QuarterRound(ref x15, ref x12, ref x13, ref x14);
        }

        // Subkey = diagonal words (no state add-back).
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(0, 4), x0);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(4, 4), x5);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(8, 4), x10);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(12, 4), x15);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(16, 4), x6);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(20, 4), x7);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(24, 4), x8);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(28, 4), x9);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographyHelper.Clear(_state);
        _disposed = true;
    }

    /// <summary>
    /// Seeds a Salsa20 state matrix from the key and nonce, leaving words 8 and 9 (the block counter) zeroed.
    /// </summary>
    /// <param name="state">The 16-word state matrix to populate.</param>
    /// <param name="key">The 16- or 32-byte key.</param>
    /// <param name="nonce">The 8-byte nonce.</param>
    /// <remarks>
    /// For a 256-bit key the second key half occupies words 11–14; for a 128-bit key the single 16-byte key is repeated
    /// into both halves, and the <c>"expand 16-byte k"</c> constants are used.
    /// </remarks>
    private static void ExpandKey(uint[] state, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        bool is256 = key.Length == KeySize256Bytes;

        state[0] = is256 ? Sigma0 : Tau0;
        state[5] = is256 ? Sigma1 : Tau1;
        state[10] = is256 ? Sigma2 : Tau2;
        state[15] = is256 ? Sigma3 : Tau3;

        // First key half -> words 1..4.
        state[1] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(0, 4));
        state[2] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4, 4));
        state[3] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8, 4));
        state[4] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));

        // Second key half -> words 11..14. For a 128-bit key the same 16 bytes are repeated.
        ReadOnlySpan<byte> secondHalf = is256 ? key.Slice(16, 16) : key.Slice(0, 16);
        state[11] = BinaryPrimitives.ReadUInt32LittleEndian(secondHalf.Slice(0, 4));
        state[12] = BinaryPrimitives.ReadUInt32LittleEndian(secondHalf.Slice(4, 4));
        state[13] = BinaryPrimitives.ReadUInt32LittleEndian(secondHalf.Slice(8, 4));
        state[14] = BinaryPrimitives.ReadUInt32LittleEndian(secondHalf.Slice(12, 4));

        // Nonce -> words 6, 7.
        state[6] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        state[7] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));

        // Words 8, 9 (counter) left zero; set per block in NextKeystreamBlock.
        state[8] = 0;
        state[9] = 0;
    }

    /// <summary>
    /// Runs the Salsa20 core function over <paramref name="state" /> and writes the 64-byte keystream block.
    /// </summary>
    /// <param name="state">The seeded state matrix (with the block counter injected).</param>
    /// <param name="destination">A span of at least 64 bytes that receives the keystream block.</param>
    private static void Core(uint[] state, Span<byte> destination)
    {
        uint x0 = state[0], x1 = state[1], x2 = state[2], x3 = state[3];
        uint x4 = state[4], x5 = state[5], x6 = state[6], x7 = state[7];
        uint x8 = state[8], x9 = state[9], x10 = state[10], x11 = state[11];
        uint x12 = state[12], x13 = state[13], x14 = state[14], x15 = state[15];

        for (int i = 0; i < Rounds; i += 2)
        {
            // Column round.
            QuarterRound(ref x0, ref x4, ref x8, ref x12);
            QuarterRound(ref x5, ref x9, ref x13, ref x1);
            QuarterRound(ref x10, ref x14, ref x2, ref x6);
            QuarterRound(ref x15, ref x3, ref x7, ref x11);

            // Row round.
            QuarterRound(ref x0, ref x1, ref x2, ref x3);
            QuarterRound(ref x5, ref x6, ref x7, ref x4);
            QuarterRound(ref x10, ref x11, ref x8, ref x9);
            QuarterRound(ref x15, ref x12, ref x13, ref x14);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(0, 4), x0 + state[0]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(4, 4), x1 + state[1]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(8, 4), x2 + state[2]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(12, 4), x3 + state[3]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(16, 4), x4 + state[4]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(20, 4), x5 + state[5]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(24, 4), x6 + state[6]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(28, 4), x7 + state[7]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(32, 4), x8 + state[8]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(36, 4), x9 + state[9]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(40, 4), x10 + state[10]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(44, 4), x11 + state[11]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(48, 4), x12 + state[12]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(52, 4), x13 + state[13]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(56, 4), x14 + state[14]);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(60, 4), x15 + state[15]);
    }

    /// <summary>
    /// Applies the Salsa20 quarter-round operation to four state words in place, per Bernstein's specification.
    /// </summary>
    /// <param name="a">The first state word.</param>
    /// <param name="b">The second state word.</param>
    /// <param name="c">The third state word.</param>
    /// <param name="d">The fourth state word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / rotate / XOR steps mirror the Salsa20 quarter-round definition and preserve a compact, specification-like layout.")]
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        b ^= BitOperations.RotateLeft(a + d, 7);
        c ^= BitOperations.RotateLeft(b + a, 9);
        d ^= BitOperations.RotateLeft(c + b, 13);
        a ^= BitOperations.RotateLeft(d + c, 18);
    }
}
