// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitStreamCipher.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
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
/// driven by a coupled non-linear next-state function), so blocks must be produced strictly in sequence. This is exactly
/// the contract the engine-owns-advancement <see cref="IStreamCipher" /> interface expresses.
/// </para>
/// <para>
/// Key setup expands the key into the state and counter variables, iterates the next-state function four times, and
/// re-keys the counters from the state. When an IV is supplied, IV setup mixes the 64-bit IV into a working copy of the
/// counters and iterates four more times; the master state from key setup is preserved so that many IVs can be derived
/// from one key. Each emitted block runs one next-state iteration and then extracts 128 bits from the state.
/// </para>
/// </remarks>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc4503">RFC 4503 — A Description of the Rabbit Stream Cipher
/// Algorithm</seealso>
/// <seealso cref="Rabbit" />
internal sealed class RabbitStreamCipher
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

    // The eight counter-increment constants A0..A7 (RFC 4503 §2.5), alternating three repeating words.
    private const uint A0 = 0x4D34D34D;

    private const uint A1 = 0xD34D34D3;
    private const uint A2 = 0x34D34D34;

    // The working state: eight 32-bit state variables, eight 32-bit counters, and the counter carry bit.
    private readonly uint[] _x = new uint[8];
    private readonly uint[] _c = new uint[8];
    private uint _carry;
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

        // Extract 128 bits: each output word combines a state word with cross-word halves (RFC 4503 §2.6).
        uint s0 = _x[0] ^ (_x[5] >> 16) ^ (_x[3] << 16);
        uint s1 = _x[2] ^ (_x[7] >> 16) ^ (_x[5] << 16);
        uint s2 = _x[4] ^ (_x[1] >> 16) ^ (_x[7] << 16);
        uint s3 = _x[6] ^ (_x[3] >> 16) ^ (_x[1] << 16);

        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(0, 4), s0);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(4, 4), s1);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(8, 4), s2);
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(12, 4), s3);
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
        uint k0 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(0, 4));
        uint k1 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(4, 4));
        uint k2 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8, 4));
        uint k3 = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(12, 4));

        _x[0] = k0;
        _x[2] = k1;
        _x[4] = k2;
        _x[6] = k3;
        _x[1] = (k3 << 16) | (k2 >> 16);
        _x[3] = (k0 << 16) | (k3 >> 16);
        _x[5] = (k1 << 16) | (k0 >> 16);
        _x[7] = (k2 << 16) | (k1 >> 16);

        _c[0] = BitOperations.RotateLeft(k2, 16);
        _c[2] = BitOperations.RotateLeft(k3, 16);
        _c[4] = BitOperations.RotateLeft(k0, 16);
        _c[6] = BitOperations.RotateLeft(k1, 16);
        _c[1] = (k0 & 0xFFFF0000) | (k1 & 0xFFFF);
        _c[3] = (k1 & 0xFFFF0000) | (k2 & 0xFFFF);
        _c[5] = (k2 & 0xFFFF0000) | (k3 & 0xFFFF);
        _c[7] = (k3 & 0xFFFF0000) | (k0 & 0xFFFF);

        _carry = 0;

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
        uint i0 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(0, 4));
        uint i2 = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(4, 4));
        uint i1 = (i0 >> 16) | (i2 & 0xFFFF0000);
        uint i3 = (i2 << 16) | (i0 & 0x0000FFFF);

        _c[0] ^= i0;
        _c[1] ^= i1;
        _c[2] ^= i2;
        _c[3] ^= i3;
        _c[4] ^= i0;
        _c[5] ^= i1;
        _c[6] ^= i2;
        _c[7] ^= i3;

        for (int i = 0; i < 4; i++)
            NextState();
    }

    /// <summary>
    /// Advances the counter system and the eight state variables by one Rabbit next-state iteration (RFC 4503 §2.4–2.5).
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
