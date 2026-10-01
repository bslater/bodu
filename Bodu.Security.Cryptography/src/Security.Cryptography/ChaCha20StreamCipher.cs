// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20StreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the raw ChaCha20 keystream primitive defined by RFC 8439, plus the HChaCha20 subkey-derivation function
/// used by the extended-nonce XChaCha20 construction. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher binds a 256-bit key, a 96-bit nonce, and an initial block counter at construction and produces 64-byte
/// keystream blocks on demand through <see cref="NextKeystreamBlock(Span{byte})" />. It is a pure keystream generator
/// and never observes plaintext; <see cref="StreamCipherTransform" /> combines the keystream with the message by XOR.
/// This mirrors the role <see cref="TwofishBlockCipher" /> plays for the block-cipher stack, and is the stream-cipher
/// analogue of <see cref="CtrModeTransform" />.
/// </para>
/// <para>
/// The round function follows RFC 8439 Section 2 exactly: a 4×4 matrix of 32-bit little-endian words seeded with the
/// constant <c>"expand 32-byte k"</c>, the key, a 32-bit block counter, and the 96-bit nonce, transformed by twenty
/// rounds (ten column-round / diagonal-round double rounds) of the quarter-round operation, then added word-wise to the
/// original state and serialized little-endian. <see cref="ChaCha20Core" /> computes it, one block at a time for
/// <see cref="NextKeystreamBlock(Span{byte})" /> and in runs of 4, 8 or 16 blocks for
/// <see cref="XorKeystreamBlocks(ReadOnlySpan{byte}, Span{byte})" />.
/// </para>
/// </remarks>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc8439">RFC 8439 - ChaCha20 and Poly1305 for IETF Protocols</seealso>
/// <seealso href="https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-xchacha">draft-irtf-cfrg-xchacha - XChaCha:
/// eXtended-nonce ChaCha and AEAD_XChaCha20_Poly1305</seealso> <seealso cref="ChaCha20" /> <seealso cref="XChaCha20" />
internal sealed class ChaCha20StreamCipher
    : IBulkStreamCipher
{
    /// <summary>The required key length, in bytes (256 bits).</summary>
    internal const int KeySizeBytes = ChaCha20Core.KeyBytes;

    /// <summary>The ChaCha20 nonce length, in bytes (96 bits), as specified by RFC 8439.</summary>
    internal const int NonceSizeBytes = ChaCha20Core.NonceBytes;

    /// <summary>The HChaCha20 input nonce length, in bytes (128 bits), consumed during XChaCha20 subkey derivation.</summary>
    internal const int HChaChaNonceSizeBytes = 16;

    /// <summary>The keystream block length, in bytes (512 bits).</summary>
    internal const int BlockSizeBytes = ChaCha20Core.BlockBytes;

    /// <summary>The number of ChaCha20 rounds (ten column-round / diagonal-round double rounds).</summary>
    private const int Rounds = 20;

    /// <summary>The ChaCha20 state: the constant, the key and the nonce, its counter word unused.</summary>
    private readonly uint[] _state = new uint[ChaCha20Core.StateWords];

    /// <summary>The block counter supplied at construction for the first keystream block.</summary>
    private readonly uint _initialCounter;

    /// <summary>The current block counter, advanced after each keystream block is produced.</summary>
    private uint _counter;

    /// <summary>Indicates whether the block counter has wrapped back to its initial value, marking the keystream as exhausted.</summary>
    private bool _counterExhausted;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaCha20StreamCipher" /> class from the supplied key, nonce, and
    /// initial block counter.
    /// </summary>
    /// <param name="key">The 32-byte (256-bit) key.</param>
    /// <param name="nonce">The 12-byte (96-bit) nonce.</param>
    /// <param name="initialCounter">The block counter for the first keystream block.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="key" /> is not 32 bytes long, or <paramref name="nonce" /> is not 12 bytes long.
    /// </exception>
    /// <remarks>
    /// The key and nonce are expanded into little-endian 32-bit words and copied into the engine; the caller's buffers
    /// are not retained.
    /// </remarks>
    internal ChaCha20StreamCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, uint initialCounter)
    {
        ChaCha20Core.Initialize(_state, key, nonce);

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

        uint counter = _counter;

        // Advance the counter, latching exhaustion once it wraps back to the initial value so the next call
        // fails instead of silently reusing keystream.
        _counter++;
        if (_counter == _initialCounter)
            _counterExhausted = true;

        ChaCha20Core.Block(_state, counter, destination);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The keystream holds 2^32 blocks from the initial counter. A request for more than remain writes the blocks that
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

        // The blocks that remain: 2^32 from the initial counter, less those already emitted.
        ulong remaining = (1UL << 32) - (uint)(_counter - _initialCounter);
        int available = (int)Math.Min((ulong)blocks, remaining);

        ChaCha20Core.XorBlocks(_state, _counter, input.Slice(0, available * BlockSizeBytes), output);

        _counter += (uint)available;
        if (_counter == _initialCounter)
            _counterExhausted = true;

        if (available < blocks)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_StreamCounterExhausted);
    }

    /// <summary>
    /// Derives a 32-byte subkey from a key and a 16-byte nonce using HChaCha20, as required by the XChaCha20
    /// extended-nonce construction.
    /// </summary>
    /// <param name="key">The 32-byte (256-bit) key.</param>
    /// <param name="nonce">The first 16 bytes (128 bits) of the 24-byte XChaCha20 nonce.</param>
    /// <param name="subkey">A span of at least 32 bytes that receives the derived subkey.</param>
    /// <remarks>
    /// HChaCha20 runs the ChaCha20 round function over a state seeded with the constant, key, and 128-bit nonce, but -
    /// unlike the keystream block function - does <em>not</em> add the original state back in. The subkey is the
    /// concatenation of the first and last four words of the transformed state.
    /// </remarks>
    internal static void HChaCha20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> subkey)
    {
        uint x0 = ChaCha20Core.Sigma0, x1 = ChaCha20Core.Sigma1, x2 = ChaCha20Core.Sigma2, x3 = ChaCha20Core.Sigma3;
        uint x4 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(0, 4));
        uint x5 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4, 4));
        uint x6 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8, 4));
        uint x7 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));
        uint x8 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(16, 4));
        uint x9 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(20, 4));
        uint x10 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(24, 4));
        uint x11 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(28, 4));
        uint x12 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        uint x13 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        uint x14 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));
        uint x15 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(12, 4));

        for (int i = 0; i < Rounds; i += 2)
        {
            ChaCha20Core.QuarterRound(ref x0, ref x4, ref x8, ref x12);
            ChaCha20Core.QuarterRound(ref x1, ref x5, ref x9, ref x13);
            ChaCha20Core.QuarterRound(ref x2, ref x6, ref x10, ref x14);
            ChaCha20Core.QuarterRound(ref x3, ref x7, ref x11, ref x15);

            ChaCha20Core.QuarterRound(ref x0, ref x5, ref x10, ref x15);
            ChaCha20Core.QuarterRound(ref x1, ref x6, ref x11, ref x12);
            ChaCha20Core.QuarterRound(ref x2, ref x7, ref x8, ref x13);
            ChaCha20Core.QuarterRound(ref x3, ref x4, ref x9, ref x14);
        }

        // Subkey = first four words || last four words (no state add-back).
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(0, 4), x0);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(4, 4), x1);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(8, 4), x2);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(12, 4), x3);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(16, 4), x12);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(20, 4), x13);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(24, 4), x14);
        BinaryPrimitives.WriteUInt32LittleEndian(subkey.Slice(28, 4), x15);
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
