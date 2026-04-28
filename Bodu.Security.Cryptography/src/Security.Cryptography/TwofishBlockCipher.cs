// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the core Twofish block cipher engine, implementing low-level encryption and decryption of individual
/// 128-bit blocks.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the raw Twofish block primitive. It operates on 128-bit blocks and supports 128-bit,
/// 192-bit, and 256-bit keys.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Twofish" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract.
/// </para>
/// </remarks>
/// <seealso cref="Twofish" />
public sealed class TwofishBlockCipher
    : IBlockCipher
{
    private const int BlockSizeInBytes = 16;
    private const int Rounds = 16;
    private const int ExpandedKeyWords = 40;
    private const int SBoxLength = 256;

    private static readonly byte[,] Mds =
    {
        { 0x01, 0xEF, 0x5B, 0x5B },
        { 0x5B, 0xEF, 0xEF, 0x01 },
        { 0xEF, 0x5B, 0x01, 0xEF },
        { 0xEF, 0x01, 0xEF, 0x5B },
    };

    private static readonly byte[,] Rs =
    {
        { 0x01, 0xA4, 0x55, 0x87, 0x5A, 0x58, 0xDB, 0x9E },
        { 0xA4, 0x56, 0x82, 0xF3, 0x1E, 0xC6, 0x68, 0xE5 },
        { 0x02, 0xA1, 0xFC, 0xC1, 0x47, 0xAE, 0x3D, 0x19 },
        { 0xA4, 0x55, 0x87, 0x5A, 0x58, 0xDB, 0x9E, 0x03 },
    };

    private static readonly byte[] Q0 = CreateQ(
        new byte[] { 0x8, 0x1, 0x7, 0xD, 0x6, 0xF, 0x3, 0x2, 0x0, 0xB, 0x5, 0x9, 0xE, 0xC, 0xA, 0x4 },
        new byte[] { 0xE, 0xC, 0xB, 0x8, 0x1, 0x2, 0x3, 0x5, 0xF, 0x4, 0xA, 0x6, 0x7, 0x0, 0x9, 0xD },
        new byte[] { 0xB, 0xA, 0x5, 0xE, 0x6, 0xD, 0x9, 0x0, 0xC, 0x8, 0xF, 0x3, 0x2, 0x4, 0x7, 0x1 },
        new byte[] { 0xD, 0x7, 0xF, 0x4, 0x1, 0x2, 0x6, 0xE, 0x9, 0xB, 0x3, 0x0, 0x8, 0x5, 0xC, 0xA });

    private static readonly byte[] Q1 = CreateQ(
        new byte[] { 0x2, 0x8, 0xB, 0xD, 0xF, 0x7, 0x6, 0xE, 0x3, 0x1, 0x9, 0x4, 0x0, 0xA, 0xC, 0x5 },
        new byte[] { 0x1, 0xE, 0x2, 0xB, 0x4, 0xC, 0x3, 0x7, 0x6, 0xD, 0xA, 0x5, 0xF, 0x9, 0x0, 0x8 },
        new byte[] { 0x4, 0xC, 0x7, 0x5, 0x1, 0x6, 0x9, 0xA, 0x0, 0xE, 0xD, 0x8, 0x2, 0xB, 0x3, 0xF },
        new byte[] { 0xB, 0x9, 0x5, 0x1, 0xC, 0x3, 0xD, 0xE, 0x6, 0x4, 0x7, 0xF, 0x2, 0x0, 0x8, 0xA });

    private readonly uint[] _k = new uint[ExpandedKeyWords];
    private readonly uint[] _s1 = new uint[SBoxLength];
    private readonly uint[] _s2 = new uint[SBoxLength];
    private readonly uint[] _s3 = new uint[SBoxLength];
    private readonly uint[] _s4 = new uint[SBoxLength];

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwofishBlockCipher" /> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Must be 16, 24, or 32 bytes in length. Must not be <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not 16, 24, or 32 bytes in length.
    /// </exception>
    public TwofishBlockCipher(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException("The Twofish key must be 16, 24, or 32 bytes in length.", nameof(key));

        this.InitialiseKeySchedule(key);
    }

    /// <inheritdoc />
    public int BlockSize => BlockSizeInBytes;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this._disposed)
        {
            Array.Clear(this._k, 0, this._k.Length);
            Array.Clear(this._s1, 0, this._s1.Length);
            Array.Clear(this._s2, 0, this._s2.Length);
            Array.Clear(this._s3, 0, this._s3.Length);
            Array.Clear(this._s4, 0, this._s4.Length);

            this._disposed = true;
        }
    }

    /// <inheritdoc />
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeInBytes);

        uint r0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint r1 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(4));
        uint r2 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(8));
        uint r3 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(12));

        r0 ^= this._k[0];
        r1 ^= this._k[1];
        r2 ^= this._k[2];
        r3 ^= this._k[3];

        for (int i = 0; i < 32; i += 4)
        {
            uint t0 = this.G0(r0);
            uint t1 = this.G1(r1);

            r2 ^= t0 + t1 + this._k[8 + i];
            r2 = RotateRight(r2, 1);

            r3 = RotateLeft(r3, 1);
            r3 ^= t0 + (t1 << 1) + this._k[9 + i];

            t0 = this.G0(r2);
            t1 = this.G1(r3);

            r0 ^= t0 + t1 + this._k[10 + i];
            r0 = RotateRight(r0, 1);

            r1 = RotateLeft(r1, 1);
            r1 ^= t0 + (t1 << 1) + this._k[11 + i];
        }

        r2 ^= this._k[4];
        r3 ^= this._k[5];
        r0 ^= this._k[6];
        r1 ^= this._k[7];

        BinaryPrimitives.WriteUInt32LittleEndian(output, r2);
        BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(4), r3);
        BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(8), r0);
        BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(12), r1);
    }

    /// <inheritdoc />
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeInBytes);

        uint r2 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint r3 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(4));
        uint r0 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(8));
        uint r1 = BinaryPrimitives.ReadUInt32LittleEndian(input.Slice(12));

        r2 ^= this._k[4];
        r3 ^= this._k[5];
        r0 ^= this._k[6];
        r1 ^= this._k[7];

        for (int i = 0; i < 32; i += 4)
        {
            uint t0 = this.G0(r2);
            uint t1 = this.G1(r3);

            r0 = RotateLeft(r0, 1);
            r0 ^= t0 + t1 + this._k[38 - i];

            r1 ^= t0 + (t1 << 1) + this._k[39 - i];
            r1 = RotateRight(r1, 1);

            t0 = this.G0(r0);
            t1 = this.G1(r1);

            r2 = RotateLeft(r2, 1);
            r2 ^= t0 + t1 + this._k[36 - i];

            r3 ^= t0 + (t1 << 1) + this._k[37 - i];
            r3 = RotateRight(r3, 1);
        }

        r0 ^= this._k[0];
        r1 ^= this._k[1];
        r2 ^= this._k[2];
        r3 ^= this._k[3];

        BinaryPrimitives.WriteUInt32LittleEndian(output, r0);
        BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(4), r1);
        BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(8), r2);
        BinaryPrimitives.WriteUInt32LittleEndian(output.Slice(12), r3);
    }

    private void InitialiseKeySchedule(ReadOnlySpan<byte> key)
    {
        int keyWords = key.Length / 8;

        Span<uint> me = stackalloc uint[4];
        Span<uint> mo = stackalloc uint[4];
        Span<uint> s = stackalloc uint[4];

        for (int i = 0; i < keyWords; i++)
        {
            me[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8 * i));
            mo[i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(8 * i + 4));
        }

        for (int i = 0; i < keyWords; i++)
        {
            uint word = 0;

            for (int row = 0; row < 4; row++)
            {
                uint value = 0;

                for (int column = 0; column < 8; column++)
                {
                    value ^= GfMul(Rs[row, column], key[(8 * i) + column], 0x4D);
                }

                word |= value << (8 * row);
            }

            s[keyWords - i - 1] = word;
        }

        for (int i = 0; i < SBoxLength; i++)
        {
            this._s1[i] = HSub((byte)i, s, keyWords, 0);
            this._s2[i] = HSub((byte)i, s, keyWords, 1);
            this._s3[i] = HSub((byte)i, s, keyWords, 2);
            this._s4[i] = HSub((byte)i, s, keyWords, 3);
        }

        for (int i = 0; i < 20; i++)
        {
            uint a = H((byte)(2 * i), me, keyWords);
            uint b = RotateLeft(H((byte)((2 * i) + 1), mo, keyWords), 8);

            a += b;
            this._k[2 * i] = a;

            a += b;
            this._k[(2 * i) + 1] = RotateLeft(a, 9);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint G0(uint x) =>
        this._s1[x & 0xFF] ^
        this._s2[(x >> 8) & 0xFF] ^
        this._s3[(x >> 16) & 0xFF] ^
        this._s4[(x >> 24) & 0xFF];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint G1(uint x) =>
        this._s2[x & 0xFF] ^
        this._s3[(x >> 8) & 0xFF] ^
        this._s4[(x >> 16) & 0xFF] ^
        this._s1[(x >> 24) & 0xFF];

    private static uint H(byte x, ReadOnlySpan<uint> l, int k)
    {
        uint result = 0;

        for (int i = 0; i < 4; i++)
        {
            result ^= HSub(x, l, k, i);
        }

        return result;
    }

    private static uint HSub(byte x, ReadOnlySpan<uint> l, int k, int i)
    {
        int value = x;

        if (k == 4)
        {
            value = ((i == 1 || i == 2) ? Q0[value] : Q1[value]) ^
                    (byte)(l[3] >> (8 * i));
        }

        if (k >= 3)
        {
            value = ((i == 2 || i == 3) ? Q0[value] : Q1[value]) ^
                    (byte)(l[2] >> (8 * i));
        }

        value = ((i == 0 || i == 2) ? Q0[value] : Q1[value]) ^
                (byte)(l[1] >> (8 * i));

        value = ((i == 0 || i == 1) ? Q0[value] : Q1[value]) ^
                (byte)(l[0] >> (8 * i));

        value = (i == 1 || i == 3) ? Q0[value] : Q1[value];

        return MdsMultiplyColumn(i, (byte)value);
    }

    private static uint MdsMultiplyColumn(int column, byte value)
    {
        uint z0 = GfMul(Mds[0, column], value, 0x69);
        uint z1 = GfMul(Mds[1, column], value, 0x69);
        uint z2 = GfMul(Mds[2, column], value, 0x69);
        uint z3 = GfMul(Mds[3, column], value, 0x69);

        return z0 | (z1 << 8) | (z2 << 16) | (z3 << 24);
    }

    private static uint GfMul(byte a, byte b, byte primitive)
    {
        byte result = 0;

        for (int i = 0; i < 8; i++)
        {
            if ((b & 0x01) != 0)
                result ^= a;

            a = (byte)(((a & 0x80) != 0)
                ? (a << 1) ^ primitive
                : a << 1);

            b >>= 1;
        }

        return result;
    }

    private static byte[] CreateQ(byte[] t0, byte[] t1, byte[] t2, byte[] t3)
    {
        var q = new byte[256];

        for (int x = 0; x < q.Length; x++)
        {
            int a0 = x >> 4;
            int b0 = x & 0x0F;

            int a1 = a0 ^ b0;
            int b1 = a0 ^ RotateRight4(b0) ^ ((a0 << 3) & 0x0F);

            int a2 = t0[a1];
            int b2 = t1[b1];

            int a3 = a2 ^ b2;
            int b3 = a2 ^ RotateRight4(b2) ^ ((a2 << 3) & 0x0F);

            int a4 = t2[a3];
            int b4 = t3[b3];

            q[x] = (byte)((b4 << 4) | a4);
        }

        return q;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RotateRight4(int value) => ((value >> 1) | ((value & 0x01) << 3)) & 0x0F;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int bits) => (value << bits) | (value >> (32 - bits));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateRight(uint value, int bits) => (value >> bits) | (value << (32 - bits));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(nameof(TwofishBlockCipher));
#endif
    }
}