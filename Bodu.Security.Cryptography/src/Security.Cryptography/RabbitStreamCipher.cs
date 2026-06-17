// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitStreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the Rabbit keystream primitive specified by RFC 4503. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Rabbit binds a 128-bit key and an optional 64-bit IV at construction and produces 16-byte keystream blocks on demand
/// through <see cref="NextKeystreamBlock(Span{byte})" />. Unlike ChaCha20 and Salsa20 it has no seekable block counter:
/// its keystream is the output of an evolving internal state (eight 32-bit state variables and eight 32-bit counters
/// driven by a coupled non-linear next-state function), so blocks must be produced strictly in sequence. This is
/// exactly the contract the engine-owns-advancement <see cref="IStreamCipher" /> interface expresses.
/// </para>
/// <para>
/// The implementation follows the RFC 4503 octet conventions exactly: the key and IV are interpreted as big-endian
/// integers split into 16-bit subkeys (I2OSP), and each keystream block is serialized as the big-endian (I2OSP)
/// representation of the 128-bit extraction value. This reproduces the RFC 4503 Appendix A conformance vectors and the
/// Appendix B internal-state debugging vectors byte-for-byte. (Note that some implementations — for example Crypto++
/// and libtomcrypt — use a self-consistent little-endian convention whose key, IV, and keystream octets are
/// byte-reversed relative to the RFC.)
/// </para>
/// <para>
/// Key setup expands the key into the state and counter variables, iterates the next-state function four times, and
/// re-keys the counters from the state. When an IV is supplied, IV setup mixes the 64-bit IV into a working copy of the
/// counters and iterates four more times; the master state from key setup is preserved so that many IVs can be derived
/// from one key. Each emitted block runs one next-state iteration and then extracts 128 bits from the state.
/// </para>
/// </remarks>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc4503">RFC 4503 — A Description of the Rabbit Stream Cipher
/// Algorithm</seealso> <seealso cref="Rabbit" />
internal sealed partial class RabbitStreamCipher
    : IStreamCipher
{
    /// <summary>
    /// The required key length, in bytes (128 bits).
    /// </summary>
    internal const int KeySizeBytes = 16;

    /// <summary>
    /// The Rabbit IV length, in bytes (64 bits).
    /// </summary>
    internal const int NonceSizeBytes = 8;

    /// <summary>
    /// The keystream block length, in bytes (128 bits).
    /// </summary>
    internal const int BlockSizeBytes = 16;

    /// <summary>
    /// The first of the three repeating counter-increment constants A0–A7 (RFC 4503 §2.5).
    /// </summary>
    /// <remarks>
    /// The eight counter-increment constants A0–A7 alternate these three repeating words across the counter system.
    /// </remarks>
    private const uint A0 = 0x4D34D34D;

    /// <summary>
    /// The second of the three repeating counter-increment constants A0–A7 (RFC 4503 §2.5).
    /// </summary>
    private const uint A1 = 0xD34D34D3;

    /// <summary>
    /// The third of the three repeating counter-increment constants A0–A7 (RFC 4503 §2.5).
    /// </summary>
    private const uint A2 = 0x34D34D34;

    /// <summary>
    /// The eight 32-bit state variables of the Rabbit working state.
    /// </summary>
    private readonly uint[] _x = new uint[8];

    /// <summary>
    /// The eight 32-bit counter variables of the Rabbit working state.
    /// </summary>
    private readonly uint[] _c = new uint[8];

    /// <summary>
    /// The counter carry bit propagated across next-state iterations.
    /// </summary>
    private uint _carry;

    /// <summary>
    /// Indicates whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitStreamCipher" /> class from the supplied key, performing key
    /// setup but no IV setup.
    /// </summary>
    /// <param name="key">The 16-byte (128-bit) key.</param>
    /// <remarks>
    /// This produces the key-only keystream defined by RFC 4503 Appendix A.1 (testing without IV setup). The caller is
    /// responsible for validating the key length.
    /// </remarks>
    internal RabbitStreamCipher(ReadOnlySpan<byte> key)
    {
        KeySetup(key);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitStreamCipher" /> class from the supplied key and IV.
    /// </summary>
    /// <param name="key">The 16-byte (128-bit) key.</param>
    /// <param name="nonce">The 8-byte (64-bit) IV.</param>
    /// <remarks>
    /// Key setup runs first, then the 64-bit IV is mixed into the counters per RFC 4503 §3.2. The caller is responsible
    /// for validating the lengths.
    /// </remarks>
    internal RabbitStreamCipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        KeySetup(key);
        IvSetup(nonce);
    }

    /// <inheritdoc />
    public int BlockSize => BlockSizeBytes;

    /// <inheritdoc />
    public void NextKeystreamBlock(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, 0, BlockSizeBytes);

        NextState();

        // Extract 128 bits (RFC 4503 §2.6) and serialize as the big-endian (I2OSP) representation of S, most
        // significant word first. S[127..112] = X6[31..16] ^ X1[15..0], down to S[15..0] = X0[15..0] ^ X5[31..16].
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(0, 4), ((Hi(_x[6]) ^ Lo(_x[1])) << 16) | (Lo(_x[6]) ^ Hi(_x[3])));
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), ((Hi(_x[4]) ^ Lo(_x[7])) << 16) | (Lo(_x[4]) ^ Hi(_x[1])));
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), ((Hi(_x[2]) ^ Lo(_x[5])) << 16) | (Lo(_x[2]) ^ Hi(_x[7])));
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), ((Hi(_x[0]) ^ Lo(_x[3])) << 16) | (Lo(_x[0]) ^ Hi(_x[5])));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        Array.Clear(_x, 0, _x.Length);
        Array.Clear(_c, 0, _c.Length);
        _carry = 0;
        _disposed = true;
    }

    /// <summary>
    /// Gets the low 16 bits of a word.
    /// </summary>
    /// <param name="w">The word.</param>
    /// <returns><c>w &amp; 0xFFFF</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Lo(uint w) => w & 0xFFFF;

    /// <summary>
    /// Gets the high 16 bits of a word, shifted down.
    /// </summary>
    /// <param name="w">The word.</param>
    /// <returns><c>w &gt;&gt; 16</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hi(uint w) => w >> 16;

    /// <summary>
    /// Reads the big-endian 16-bit subkey at the given subkey index from a 16-byte key, where subkey 0 is the
    /// least-significant 16 bits of the 128-bit big-endian key integer (RFC 4503 I2OSP convention).
    /// </summary>
    /// <param name="key">The 16-byte key.</param>
    /// <param name="index">The subkey index, 0–7.</param>
    /// <returns>The 16-bit subkey value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint SubKey(ReadOnlySpan<byte> key, int index)
    {
        int offset = 14 - (index * 2);
        return (uint)((key[offset] << 8) | key[offset + 1]);
    }

    /// <summary>
    /// Reads the big-endian 16-bit IV subword at the given index from an 8-byte IV, where subword 0 is the
    /// least-significant 16 bits of the 64-bit big-endian IV integer.
    /// </summary>
    /// <param name="nonce">The 8-byte IV.</param>
    /// <param name="index">The subword index, 0–3.</param>
    /// <returns>The 16-bit IV subword value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint IvWord(ReadOnlySpan<byte> nonce, int index)
    {
        int offset = 6 - (index * 2);
        return (uint)((nonce[offset] << 8) | nonce[offset + 1]);
    }

    /// <summary>
    /// The Rabbit g-function: squares a 32-bit value to 64 bits, then folds the high and low halves together by XOR.
    /// </summary>
    /// <param name="u">The 32-bit input.</param>
    /// <returns>The 32-bit g-function result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint G(uint u)
    {
        ulong square = (ulong)u * u;
        return (uint)(square ^ (square >> 32));
    }

    /// <summary>
    /// Expands the key into the state and counter variables and runs the four key-setup iterations (RFC 4503 §3.1).
    /// </summary>
    /// <param name="key">The 16-byte key.</param>
    private void KeySetup(ReadOnlySpan<byte> key)
    {
        ExpandKey(key);

        for (int i = 0; i < 4; i++)
            NextState();

        // Re-key the counters from the iterated state.
        for (int i = 0; i < 8; i++)
            _c[i] ^= _x[(i + 4) & 7];
    }

    /// <summary>
    /// Mixes the 64-bit IV into the counters and runs the four IV-setup iterations (RFC 4503 §3.2).
    /// </summary>
    /// <param name="nonce">The 8-byte IV.</param>
    private void IvSetup(ReadOnlySpan<byte> nonce)
    {
        MixIv(nonce);

        for (int i = 0; i < 4; i++)
            NextState();
    }

    /// <summary>
    /// Mixes the 64-bit IV into the eight counters (the XOR step of RFC 4503 §3.2), without iterating the next-state
    /// function.
    /// </summary>
    /// <param name="nonce">The 8-byte IV.</param>
    private void MixIv(ReadOnlySpan<byte> nonce)
    {
        uint iv0 = IvWord(nonce, 0), iv1 = IvWord(nonce, 1), iv2 = IvWord(nonce, 2), iv3 = IvWord(nonce, 3);

        _c[0] ^= (iv1 << 16) | iv0;
        _c[1] ^= (iv3 << 16) | iv1;
        _c[2] ^= (iv3 << 16) | iv2;
        _c[3] ^= (iv2 << 16) | iv0;
        _c[4] ^= (iv1 << 16) | iv0;
        _c[5] ^= (iv3 << 16) | iv1;
        _c[6] ^= (iv3 << 16) | iv2;
        _c[7] ^= (iv2 << 16) | iv0;
    }

    /// <summary>
    /// Advances the counter system and the eight state variables by one Rabbit next-state iteration (RFC 4503
    /// §2.4–2.5).
    /// </summary>
    private void NextState()
    {
        // Snapshot the counters so each carry is derived from the pre-update value.
        uint c0 = _c[0], c1 = _c[1], c2 = _c[2], c3 = _c[3];
        uint c4 = _c[4], c5 = _c[5], c6 = _c[6], c7 = _c[7];

        unchecked
        {
            _c[0] = c0 + A0 + _carry;
            _c[1] = c1 + A1 + (_c[0] < c0 ? 1u : 0u);
            _c[2] = c2 + A2 + (_c[1] < c1 ? 1u : 0u);
            _c[3] = c3 + A0 + (_c[2] < c2 ? 1u : 0u);
            _c[4] = c4 + A1 + (_c[3] < c3 ? 1u : 0u);
            _c[5] = c5 + A2 + (_c[4] < c4 ? 1u : 0u);
            _c[6] = c6 + A0 + (_c[5] < c5 ? 1u : 0u);
            _c[7] = c7 + A1 + (_c[6] < c6 ? 1u : 0u);
            _carry = _c[7] < c7 ? 1u : 0u;

            uint g0 = G(_x[0] + _c[0]);
            uint g1 = G(_x[1] + _c[1]);
            uint g2 = G(_x[2] + _c[2]);
            uint g3 = G(_x[3] + _c[3]);
            uint g4 = G(_x[4] + _c[4]);
            uint g5 = G(_x[5] + _c[5]);
            uint g6 = G(_x[6] + _c[6]);
            uint g7 = G(_x[7] + _c[7]);

            _x[0] = g0 + BitOperations.RotateLeft(g7, 16) + BitOperations.RotateLeft(g6, 16);
            _x[1] = g1 + BitOperations.RotateLeft(g0, 8) + g7;
            _x[2] = g2 + BitOperations.RotateLeft(g1, 16) + BitOperations.RotateLeft(g0, 16);
            _x[3] = g3 + BitOperations.RotateLeft(g2, 8) + g1;
            _x[4] = g4 + BitOperations.RotateLeft(g3, 16) + BitOperations.RotateLeft(g2, 16);
            _x[5] = g5 + BitOperations.RotateLeft(g4, 8) + g3;
            _x[6] = g6 + BitOperations.RotateLeft(g5, 16) + BitOperations.RotateLeft(g4, 16);
            _x[7] = g7 + BitOperations.RotateLeft(g6, 8) + g5;
        }
    }
}
