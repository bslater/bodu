// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20StreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
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
/// little-endian. <see cref="Salsa20Core" /> computes it, one block at a time for
/// <see cref="NextKeystreamBlock(Span{byte})" /> and in runs of 4, 8 or 16 blocks for
/// <see cref="XorKeystreamBlocks(ReadOnlySpan{byte}, Span{byte})" />.
/// </para>
/// </remarks>
/// <seealso href="https://cr.yp.to/snuffle/spec.pdf">Salsa20 specification (Bernstein, 2005)</seealso>
/// <seealso href="https://cr.yp.to/snuffle/xsalsa-20081128.pdf">Extending the Salsa20 nonce (Bernstein, 2008)</seealso>
/// <seealso cref="Salsa20" /> <seealso cref="XSalsa20" />
internal sealed class Salsa20StreamCipher
    : IBulkStreamCipher
{
    /// <summary>The 128-bit key length, in bytes.</summary>
    internal const int KeySize128Bytes = Salsa20Core.Key128Bytes;

    /// <summary>The 256-bit key length, in bytes.</summary>
    internal const int KeySize256Bytes = Salsa20Core.Key256Bytes;

    /// <summary>The Salsa20 nonce length, in bytes (64 bits).</summary>
    internal const int NonceSizeBytes = Salsa20Core.NonceBytes;

    /// <summary>The HSalsa20 input nonce length, in bytes (128 bits), consumed during XSalsa20 subkey derivation.</summary>
    internal const int HSalsaNonceSizeBytes = 16;

    /// <summary>The keystream block length, in bytes (512 bits).</summary>
    internal const int BlockSizeBytes = Salsa20Core.BlockBytes;

    /// <summary>The number of Salsa20 rounds (ten column-round / row-round double rounds).</summary>
    private const int Rounds = 20;

    /// <summary>The Salsa20 state: the constants, the key and the nonce, its counter words unused.</summary>
    private readonly uint[] _state = new uint[Salsa20Core.StateWords];

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
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> is neither 16 nor 32 bytes long, or <paramref name="nonce" /> is not 8 bytes long.
    /// </exception>
    /// <remarks>
    /// The key and nonce are expanded into the Salsa20 state matrix; the caller's buffers are not retained.
    /// </remarks>
    internal Salsa20StreamCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ulong initialCounter)
    {
        Salsa20Core.Initialize(_state, key, nonce);

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

        ulong counter = _counter;

        // Advance the counter, latching exhaustion once it wraps back to the initial value so the next call
        // fails instead of silently reusing keystream.
        _counter++;
        if (_counter == _initialCounter)
            _counterExhausted = true;

        Salsa20Core.Block(_state, counter, destination);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The keystream holds 2^64 blocks from the initial counter. A request for more than remain writes the blocks that
    /// do, latches exhaustion, and then throws, as the same number of calls to
    /// <see cref="NextKeystreamBlock(Span{byte})" /> would.
    /// </remarks>
    public void XorKeystreamBlocks(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (input.Length % BlockSizeBytes != 0) throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_InputLengthBlockMultiple, BlockSizeBytes), nameof(input));
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, input.Length);

        int blocks = input.Length / BlockSizeBytes;
        if (blocks == 0)
            return;

        if (_counterExhausted)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_StreamCounterExhausted);

        // One fewer than the blocks that remain, which is 2^64 on a fresh engine and so does not fit in 64 bits.
        ulong spare = ulong.MaxValue - (_counter - _initialCounter);
        int available = (ulong)(blocks - 1) <= spare ? blocks : (int)(spare + 1);

        Salsa20Core.XorBlocks(_state, _counter, input.Slice(0, available * BlockSizeBytes), output);

        _counter += (ulong)available;
        if (_counter == _initialCounter)
            _counterExhausted = true;

        if (available < blocks)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_StreamCounterExhausted);
    }

    /// <summary>
    /// Derives a 32-byte subkey from a key and a 16-byte nonce using HSalsa20, as required by the XSalsa20
    /// extended-nonce construction.
    /// </summary>
    /// <param name="key">The 32-byte (256-bit) key.</param>
    /// <param name="nonce">The first 16 bytes (128 bits) of the 24-byte XSalsa20 nonce.</param>
    /// <param name="subkey">A span of at least 32 bytes that receives the derived subkey.</param>
    /// <remarks>
    /// HSalsa20 runs the Salsa20 round function over a state seeded with the constant, key, and 128-bit nonce, but -
    /// unlike the keystream core - does <em>not</em> add the original state back in. The subkey is taken from the
    /// diagonal words of the transformed state (positions 0, 5, 10, 15, 6, 7, 8, 9).
    /// </remarks>
    internal static void HSalsa20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> subkey)
    {
        uint x0 = Salsa20Core.Sigma0;
        uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(0, 4));
        uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4, 4));
        uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8, 4));
        uint x4 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));
        uint x5 = Salsa20Core.Sigma1;
        uint x6 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        uint x7 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        uint x8 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));
        uint x9 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(12, 4));
        uint x10 = Salsa20Core.Sigma2;
        uint x11 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(16, 4));
        uint x12 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(20, 4));
        uint x13 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(24, 4));
        uint x14 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(28, 4));
        uint x15 = Salsa20Core.Sigma3;

        for (int i = 0; i < Rounds; i += 2)
        {
            // Column round.
            Salsa20Core.QuarterRound(ref x0, ref x4, ref x8, ref x12);
            Salsa20Core.QuarterRound(ref x5, ref x9, ref x13, ref x1);
            Salsa20Core.QuarterRound(ref x10, ref x14, ref x2, ref x6);
            Salsa20Core.QuarterRound(ref x15, ref x3, ref x7, ref x11);

            // Row round.
            Salsa20Core.QuarterRound(ref x0, ref x1, ref x2, ref x3);
            Salsa20Core.QuarterRound(ref x5, ref x6, ref x7, ref x4);
            Salsa20Core.QuarterRound(ref x10, ref x11, ref x8, ref x9);
            Salsa20Core.QuarterRound(ref x15, ref x12, ref x13, ref x14);
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
}
