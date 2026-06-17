// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20StreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
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
/// original state and serialized little-endian.
/// </para>
/// </remarks>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc8439">RFC 8439 — ChaCha20 and Poly1305 for IETF Protocols</seealso>
/// <seealso href="https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-xchacha">draft-irtf-cfrg-xchacha — XChaCha:
/// eXtended-nonce ChaCha and AEAD_XChaCha20_Poly1305</seealso> <seealso cref="ChaCha20" /> <seealso cref="XChaCha20" />
internal sealed class ChaCha20StreamCipher
    : IStreamCipher
{
    /// <summary>
    /// The required key length, in bytes (256 bits).
    /// </summary>
    internal const int KeySizeBytes = 32;

    /// <summary>
    /// The ChaCha20 nonce length, in bytes (96 bits), as specified by RFC 8439.
    /// </summary>
    internal const int NonceSizeBytes = 12;

    /// <summary>
    /// The HChaCha20 input nonce length, in bytes (128 bits), consumed during XChaCha20 subkey derivation.
    /// </summary>
    internal const int HChaChaNonceSizeBytes = 16;

    /// <summary>
    /// The keystream block length, in bytes (512 bits).
    /// </summary>
    internal const int BlockSizeBytes = 64;

    /// <summary>
    /// The number of ChaCha20 rounds (ten column-round / diagonal-round double rounds).
    /// </summary>
    private const int Rounds = 20;

    /// <summary>
    /// The first little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.
    /// </summary>
    private const uint Sigma0 = 0x61707865;

    /// <summary>
    /// The second little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.
    /// </summary>
    private const uint Sigma1 = 0x3320646e;

    /// <summary>
    /// The third little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.
    /// </summary>
    private const uint Sigma2 = 0x79622d32;

    /// <summary>
    /// The fourth little-endian word of the ASCII constant <c>"expand 32-byte k"</c>.
    /// </summary>
    private const uint Sigma3 = 0x6b206574;

    /// <summary>
    /// The 256-bit key expanded into eight little-endian 32-bit words.
    /// </summary>
    private readonly uint[] _key = new uint[8];

    /// <summary>
    /// The first 32-bit little-endian word of the 96-bit nonce.
    /// </summary>
    private readonly uint _nonce0;

    /// <summary>
    /// The second 32-bit little-endian word of the 96-bit nonce.
    /// </summary>
    private readonly uint _nonce1;

    /// <summary>
    /// The third 32-bit little-endian word of the 96-bit nonce.
    /// </summary>
    private readonly uint _nonce2;

    /// <summary>
    /// The block counter supplied at construction for the first keystream block.
    /// </summary>
    private readonly uint _initialCounter;

    /// <summary>
    /// The current block counter, advanced after each keystream block is produced.
    /// </summary>
    private uint _counter;

    /// <summary>
    /// Indicates whether the block counter has wrapped back to its initial value, marking the keystream as exhausted.
    /// </summary>
    private bool _counterExhausted;

    /// <summary>
    /// Indicates whether the instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaCha20StreamCipher" /> class from the supplied key, nonce, and
    /// initial block counter.
    /// </summary>
    /// <param name="key">The 32-byte (256-bit) key.</param>
    /// <param name="nonce">The 12-byte (96-bit) nonce.</param>
    /// <param name="initialCounter">The block counter for the first keystream block.</param>
    /// <remarks>
    /// The key and nonce are expanded into little-endian 32-bit words and copied into the engine; the caller's buffers
    /// are not retained. The caller is responsible for validating the lengths.
    /// </remarks>
    internal ChaCha20StreamCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, uint initialCounter)
    {
        for (int i = 0; i < 8; i++)
            _key[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));

        _nonce0 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        _nonce1 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        _nonce2 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(8, 4));

        _initialCounter = initialCounter;
        _counter = initialCounter;
    }

    /// <inheritdoc />
    public int BlockSize => BlockSizeBytes;

    /// <inheritdoc />
    public void NextKeystreamBlock(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, BlockSizeBytes);

        if (_counterExhausted)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_StreamCounterExhausted);

        uint counter = _counter;

        // Advance the counter, latching exhaustion once it wraps back to the initial value so the next call
        // fails instead of silently reusing keystream.
        _counter++;
        if (_counter == _initialCounter)
            _counterExhausted = true;

        // Initial state: constants | key | counter | nonce.
        uint j0 = Sigma0, j1 = Sigma1, j2 = Sigma2, j3 = Sigma3;
        uint j4 = _key[0], j5 = _key[1], j6 = _key[2], j7 = _key[3];
        uint j8 = _key[4], j9 = _key[5], j10 = _key[6], j11 = _key[7];
        uint j12 = counter, j13 = _nonce0, j14 = _nonce1, j15 = _nonce2;

        uint x0 = j0, x1 = j1, x2 = j2, x3 = j3;
        uint x4 = j4, x5 = j5, x6 = j6, x7 = j7;
        uint x8 = j8, x9 = j9, x10 = j10, x11 = j11;
        uint x12 = j12, x13 = j13, x14 = j14, x15 = j15;

        for (int i = 0; i < Rounds; i += 2)
        {
            // Column round.
            QuarterRound(ref x0, ref x4, ref x8, ref x12);
            QuarterRound(ref x1, ref x5, ref x9, ref x13);
            QuarterRound(ref x2, ref x6, ref x10, ref x14);
            QuarterRound(ref x3, ref x7, ref x11, ref x15);

            // Diagonal round.
            QuarterRound(ref x0, ref x5, ref x10, ref x15);
            QuarterRound(ref x1, ref x6, ref x11, ref x12);
            QuarterRound(ref x2, ref x7, ref x8, ref x13);
            QuarterRound(ref x3, ref x4, ref x9, ref x14);
        }

        // Add the original state back in, then serialize little-endian.
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(0, 4), x0 + j0);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(4, 4), x1 + j1);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(8, 4), x2 + j2);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(12, 4), x3 + j3);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(16, 4), x4 + j4);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(20, 4), x5 + j5);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(24, 4), x6 + j6);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(28, 4), x7 + j7);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(32, 4), x8 + j8);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(36, 4), x9 + j9);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(40, 4), x10 + j10);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(44, 4), x11 + j11);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(48, 4), x12 + j12);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(52, 4), x13 + j13);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(56, 4), x14 + j14);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(60, 4), x15 + j15);
    }

    /// <summary>
    /// Derives a 32-byte subkey from a key and a 16-byte nonce using HChaCha20, as required by the XChaCha20
    /// extended-nonce construction.
    /// </summary>
    /// <param name="key">The 32-byte (256-bit) key.</param>
    /// <param name="nonce">The first 16 bytes (128 bits) of the 24-byte XChaCha20 nonce.</param>
    /// <param name="subkey">A span of at least 32 bytes that receives the derived subkey.</param>
    /// <remarks>
    /// HChaCha20 runs the ChaCha20 round function over a state seeded with the constant, key, and 128-bit nonce, but —
    /// unlike the keystream block function — does <em>not</em> add the original state back in. The subkey is the
    /// concatenation of the first and last four words of the transformed state.
    /// </remarks>
    internal static void HChaCha20(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> subkey)
    {
        uint x0 = Sigma0, x1 = Sigma1, x2 = Sigma2, x3 = Sigma3;
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
            QuarterRound(ref x0, ref x4, ref x8, ref x12);
            QuarterRound(ref x1, ref x5, ref x9, ref x13);
            QuarterRound(ref x2, ref x6, ref x10, ref x14);
            QuarterRound(ref x3, ref x7, ref x11, ref x15);

            QuarterRound(ref x0, ref x5, ref x10, ref x15);
            QuarterRound(ref x1, ref x6, ref x11, ref x12);
            QuarterRound(ref x2, ref x7, ref x8, ref x13);
            QuarterRound(ref x3, ref x4, ref x9, ref x14);
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

        Array.Clear(_key, 0, _key.Length);
        _disposed = true;
    }

    /// <summary>
    /// Applies the ChaCha20 quarter-round operation to four state words in place, per RFC 8439 Section 2.1.
    /// </summary>
    /// <param name="a">The first state word.</param>
    /// <param name="b">The second state word.</param>
    /// <param name="c">The third state word.</param>
    /// <param name="d">The fourth state word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / XOR / rotate steps mirror the RFC 8439 quarter-round definition and preserve a compact, specification-like layout.")]
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        a += b; d ^= a; d = BitOperations.RotateLeft(d, 16);
        c += d; b ^= c; b = BitOperations.RotateLeft(b, 12);
        a += b; d ^= a; d = BitOperations.RotateLeft(d, 8);
        c += d; b ^= c; b = BitOperations.RotateLeft(b, 7);
    }
}
