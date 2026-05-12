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
/// Most callers should prefer the higher-level <see cref="Twofish"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract.
/// </para>
/// </remarks>
/// <seealso cref="Twofish"/>
public sealed class TwofishBlockCipher
    : IBlockCipher
{
    // Twofish is defined as a 16-round Feistel-like cipher over a 128-bit block.
    private const int Rounds = 16;

    // The key schedule produces K0..K39: 4 input-whitening words, 4 output-whitening words,
    // and 32 round subkey words used as pairs by the round F function.
    private const int ExpandedKeyWords = 40;

    // Each key-dependent S-box is byte-wide, so each precomputed MDS/S-box contribution table has 256 entries.
    private const int SBoxLength = 256;

    // Fixed 4x4 MDS matrix used by the Twofish g() function after the four key-dependent byte S-boxes.
    // Multiplication is performed over GF(2^8) using the MDS field polynomial with low reduction byte 0x69.
    private static readonly byte[,] s_mds =
    {
        { 0x01, 0xEF, 0x5B, 0x5B },
        { 0x5B, 0xEF, 0xEF, 0x01 },
        { 0xEF, 0x5B, 0x01, 0xEF },
        { 0xEF, 0x01, 0xEF, 0x5B },
    };

    // Reed-Solomon matrix used by the key schedule to derive S = (S{k-1}, ..., S0),
    // the key material that parameterises the four key-dependent S-boxes.
    // Multiplication is performed over GF(2^8) using the RS field polynomial with low reduction byte 0x4D.
    private static readonly byte[,] s_rs =
    {
        { 0x01, 0xA4, 0x55, 0x87, 0x5A, 0x58, 0xDB, 0x9E },
        { 0xA4, 0x56, 0x82, 0xF3, 0x1E, 0xC6, 0x68, 0xE5 },
        { 0x02, 0xA1, 0xFC, 0xC1, 0x47, 0xAE, 0x3D, 0x19 },
        { 0xA4, 0x55, 0x87, 0x5A, 0x58, 0xDB, 0x9E, 0x03 },
    };

    // Twofish defines q0 and q1 as fixed 8-bit permutations built from four 4-bit permutations.
    // The key-dependent S-boxes are formed by layering these q permutations with bytes from the S vector.
    private static readonly byte[] s_q0 = CreateQ(
        [0x8, 0x1, 0x7, 0xD, 0x6, 0xF, 0x3, 0x2, 0x0, 0xB, 0x5, 0x9, 0xE, 0xC, 0xA, 0x4],
        [0xE, 0xC, 0xB, 0x8, 0x1, 0x2, 0x3, 0x5, 0xF, 0x4, 0xA, 0x6, 0x7, 0x0, 0x9, 0xD],
        [0xB, 0xA, 0x5, 0xE, 0x6, 0xD, 0x9, 0x0, 0xC, 0x8, 0xF, 0x3, 0x2, 0x4, 0x7, 0x1],
        [0xD, 0x7, 0xF, 0x4, 0x1, 0x2, 0x6, 0xE, 0x9, 0xB, 0x3, 0x0, 0x8, 0x5, 0xC, 0xA]);

    private static readonly byte[] s_q1 = CreateQ(
        [0x2, 0x8, 0xB, 0xD, 0xF, 0x7, 0x6, 0xE, 0x3, 0x1, 0x9, 0x4, 0x0, 0xA, 0xC, 0x5],
        [0x1, 0xE, 0x2, 0xB, 0x4, 0xC, 0x3, 0x7, 0x6, 0xD, 0xA, 0x5, 0xF, 0x9, 0x0, 0x8],
        [0x4, 0xC, 0x7, 0x5, 0x1, 0x6, 0x9, 0xA, 0x0, 0xE, 0xD, 0x8, 0x2, 0xB, 0x3, 0xF],
        [0xB, 0x9, 0x5, 0x1, 0xC, 0x3, 0xD, 0xE, 0x6, 0x4, 0x7, 0xF, 0x2, 0x0, 0x8, 0xA]);

    // Expanded subkeys K0..K39, where K0..K3 are input whitening, K4..K7 are output whitening,
    // and K8..K39 are consumed in pairs by the F function across the 16 rounds.
    private readonly uint[] _k = new uint[ExpandedKeyWords];

    // Precomputed key-dependent S-box/MDS contribution tables. Each table represents one byte lane of g(),
    // allowing the runtime g() function to reduce to four lookups and XORs rather than recomputing q/MDS steps.
    private readonly uint[] _s1 = new uint[SBoxLength];
    private readonly uint[] _s2 = new uint[SBoxLength];
    private readonly uint[] _s3 = new uint[SBoxLength];
    private readonly uint[] _s4 = new uint[SBoxLength];

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwofishBlockCipher"/> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Must be 16, 24, or 32 bytes in length. Must not be <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is not 16, 24, or 32 bytes in length.
    /// </exception>
    public TwofishBlockCipher(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException("The Twofish key must be 16, 24, or 32 bytes in length.", nameof(key));

        this.InitialiseKeySchedule(key);
    }

    /// <inheritdoc />
    public int BlockSize => Twofish.BlockSizeBits;

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
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, Twofish.BlockSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, Twofish.BlockSizeBits / 8);

        // Split the 128-bit plaintext block into four little-endian 32-bit words, matching the Twofish
        // reference word layout P0..P3.
        var r0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        var r1 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        var r2 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        var r3 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);

        // Input whitening: R0..R3 = P0..P3 xor K0..K3.
        r0 ^= this._k[0];
        r1 ^= this._k[1];
        r2 ^= this._k[2];
        r3 ^= this._k[3];

        // Each loop body performs two Twofish rounds. The first half-round pair applies F(R0, R1)
        // to R2/R3; the second applies F(R2, R3) to R0/R1. This preserves the specified Feistel swap
        // without physically rotating the register variables after every round.
        for (var i = 0; i < 2 * Rounds; i += 4)
        {
            // Round i/2: compute T0 = g(R0) and T1 = g(ROL(R1, 8)), then combine them through
            // the pseudo-Hadamard transform with round subkeys K8+i and K9+i.
            var t0 = this.G0(r0);
            var t1 = this.G1(r1);

            // R2 is xor-mixed with F0, then rotated right as defined by the Twofish round function.
            r2 ^= t0 + t1 + this._k[8 + i];
            r2 = RotateRight(r2, 1);

            // R3 is rotated left before xor-mixing with F1.
            r3 = RotateLeft(r3, 1);
            r3 ^= t0 + (t1 << 1) + this._k[9 + i];

            // Round i/2 + 1: repeat the same F construction on the other word pair.
            t0 = this.G0(r2);
            t1 = this.G1(r3);

            r0 ^= t0 + t1 + this._k[10 + i];
            r0 = RotateRight(r0, 1);

            r1 = RotateLeft(r1, 1);
            r1 ^= t0 + (t1 << 1) + this._k[11 + i];
        }

        // Output whitening is applied after the final Feistel swap. Twofish emits C0..C3 as
        // R2^K4, R3^K5, R0^K6, R1^K7.
        r2 ^= this._k[4];
        r3 ^= this._k[5];
        r0 ^= this._k[6];
        r1 ^= this._k[7];

        BinaryPrimitives.WriteUInt32LittleEndian(output, r2);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], r3);
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], r0);
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], r1);
    }

    /// <inheritdoc />
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, Twofish.BlockSizeBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, Twofish.BlockSizeBits / 8);

        // Ciphertext is stored in the post-swap order C0..C3 = R2, R3, R0, R1.
        // Load directly into the corresponding working registers so the inverse whitening is explicit.
        var r2 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        var r3 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        var r0 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        var r1 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);

        // Remove output whitening before walking the round function backwards.
        r2 ^= this._k[4];
        r3 ^= this._k[5];
        r0 ^= this._k[6];
        r1 ^= this._k[7];

        // Invert two encryption rounds at a time, consuming the round subkeys in reverse order.
        for (var i = 0; i < 2 * Rounds; i += 4)
        {
            // Invert the second encryption round of the corresponding pair.
            var t0 = this.G0(r2);
            var t1 = this.G1(r3);

            // Undo the encryption order for R0: rotate left first, then remove the xor-mixed F0 value.
            r0 = RotateLeft(r0, 1);
            r0 ^= t0 + t1 + this._k[38 - i];

            // Undo the encryption order for R1: remove the xor-mixed F1 value, then rotate right.
            r1 ^= t0 + (t1 << 1) + this._k[39 - i];
            r1 = RotateRight(r1, 1);

            // Invert the first encryption round of the corresponding pair.
            t0 = this.G0(r0);
            t1 = this.G1(r1);

            r2 = RotateLeft(r2, 1);
            r2 ^= t0 + t1 + this._k[36 - i];

            r3 ^= t0 + (t1 << 1) + this._k[37 - i];
            r3 = RotateRight(r3, 1);
        }

        // Remove input whitening to recover P0..P3.
        r0 ^= this._k[0];
        r1 ^= this._k[1];
        r2 ^= this._k[2];
        r3 ^= this._k[3];

        BinaryPrimitives.WriteUInt32LittleEndian(output, r0);
        BinaryPrimitives.WriteUInt32LittleEndian(output[4..], r1);
        BinaryPrimitives.WriteUInt32LittleEndian(output[8..], r2);
        BinaryPrimitives.WriteUInt32LittleEndian(output[12..], r3);
    }

    /// <summary>
    /// Expands the user key into whitening subkeys, round subkeys, and key-dependent S-box contribution tables.
    /// </summary>
    /// <param name="key">
    /// The Twofish user key, supplied as 16, 24, or 32 bytes.
    /// </param>
    /// <remarks>
    /// <para>
    /// The Twofish key schedule splits the key into even and odd 32-bit word streams, derives the S vector using
    /// the fixed Reed-Solomon matrix, and then generates K0..K39 using the h() function and the pseudo-Hadamard
    /// transform.
    /// </para>
    /// <para>
    /// This implementation also precomputes the MDS output for each key-dependent S-box byte value so that g()
    /// can be evaluated during encryption and decryption using table lookups and XORs.
    /// </para>
    /// </remarks>
    private void InitialiseKeySchedule(ReadOnlySpan<byte> key)
    {
        // k is the number of 64-bit key words in the specification: 2, 3, or 4 for 128-, 192-, or 256-bit keys.
        var keyWords = key.Length / 8;

        Span<uint> me = stackalloc uint[4];
        Span<uint> mo = stackalloc uint[4];
        Span<uint> s = stackalloc uint[4];

        // Split each 64-bit key word Mi into even and odd 32-bit words Me[i] and Mo[i].
        for (var i = 0; i < keyWords; i++)
        {
            me[i] = BinaryPrimitives.ReadUInt32LittleEndian(key[(8 * i)..]);
            mo[i] = BinaryPrimitives.ReadUInt32LittleEndian(key[(8 * i + 4)..]);
        }

        // Apply the RS matrix to each 64-bit key word to produce the S-box key words.
        // The resulting S vector is stored in reverse order, as required by h(x, L) in the Twofish key schedule.
        for (var i = 0; i < keyWords; i++)
        {
            uint word = 0;

            for (var row = 0; row < 4; row++)
            {
                uint value = 0;

                for (var column = 0; column < 8; column++)
                {
                    value ^= GfMul(s_rs[row, column], key[(8 * i) + column], 0x4D);
                }

                word |= value << (8 * row);
            }

            s[keyWords - i - 1] = word;
        }

        // Precompute each byte-lane's q/S/MDS contribution. These tables encode the key-dependent S-boxes
        // and the fixed MDS multiplication used by g().
        for (var i = 0; i < SBoxLength; i++)
        {
            this._s1[i] = HSub((byte)i, s, keyWords, 0);
            this._s2[i] = HSub((byte)i, s, keyWords, 1);
            this._s3[i] = HSub((byte)i, s, keyWords, 2);
            this._s4[i] = HSub((byte)i, s, keyWords, 3);
        }

        // Generate two expanded key words per iteration. A = h(2i, Me), B = ROL(h(2i+1, Mo), 8),
        // K[2i] = A+B, and K[2i+1] = ROL(A+2B, 9).
        for (var i = 0; i < ExpandedKeyWords / 2; i++)
        {
            var a = H((byte)(2 * i), me, keyWords);
            var b = RotateLeft(H((byte)((2 * i) + 1), mo, keyWords), 8);

            a += b;
            this._k[2 * i] = a;

            a += b;
            this._k[(2 * i) + 1] = RotateLeft(a, 9);
        }
    }

    /// <summary>
    /// Evaluates the Twofish g() function for the first round input word.
    /// </summary>
    /// <param name="x">
    /// The 32-bit input word to transform.
    /// </param>
    /// <returns>
    /// The MDS-mixed 32-bit output of the key-dependent g() function.
    /// </returns>
    /// <remarks>
    /// The four byte lanes are passed through the precomputed key-dependent S-box/MDS contribution tables in
    /// little-endian byte order. XORing the four contributions is equivalent to applying the MDS matrix to the
    /// four S-box outputs.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint G0(uint x) =>
        this._s1[x & 0xFF] ^
        this._s2[(x >> 8) & 0xFF] ^
        this._s3[(x >> 16) & 0xFF] ^
        this._s4[(x >> 24) & 0xFF];

    /// <summary>
    /// Evaluates the Twofish g() function for the second round input word.
    /// </summary>
    /// <param name="x">
    /// The 32-bit input word to transform.
    /// </param>
    /// <returns>
    /// The MDS-mixed 32-bit output of the key-dependent g() function for the rotated byte-lane ordering.
    /// </returns>
    /// <remarks>
    /// Twofish defines the round function as T0 = g(R0) and T1 = g(ROL(R1, 8)). Rather than rotating
    /// <paramref name="x"/> first, this method reads the byte lanes using the equivalent shifted table order.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint G1(uint x) =>
        this._s2[x & 0xFF] ^
        this._s3[(x >> 8) & 0xFF] ^
        this._s4[(x >> 16) & 0xFF] ^
        this._s1[(x >> 24) & 0xFF];

    /// <summary>
    /// Applies the Twofish h() function to a repeated byte value using the supplied key-word stream.
    /// </summary>
    /// <param name="x">
    /// The byte input to h(), normally 2i or 2i+1 during expanded-key generation.
    /// </param>
    /// <param name="l">
    /// The key-word stream used by h(); this is either the even-key stream Me, the odd-key stream Mo, or the
    /// Reed-Solomon-derived S vector depending on the caller.
    /// </param>
    /// <param name="k">
    /// The number of 64-bit key words in the original Twofish key.
    /// </param>
    /// <returns>
    /// The 32-bit MDS output produced by the four h() byte lanes.
    /// </returns>
    private static uint H(byte x, ReadOnlySpan<uint> l, int k)
    {
        uint result = 0;

        // h() consists of four parallel byte lanes, each with its own q permutation sequence.
        // Each lane is then multiplied by the corresponding MDS column and XOR-combined into one word.
        for (var i = 0; i < 4; i++)
        {
            result ^= HSub(x, l, k, i);
        }

        return result;
    }

    /// <summary>
    /// Evaluates one byte lane of the Twofish h() function and returns its MDS column contribution.
    /// </summary>
    /// <param name="x">
    /// The byte input for this h() evaluation.
    /// </param>
    /// <param name="l">
    /// The key words used to key the q-permutation cascade.
    /// </param>
    /// <param name="k">
    /// The number of 64-bit key words in the original Twofish key.
    /// </param>
    /// <param name="i">
    /// The byte-lane and MDS column index, in the range 0..3.
    /// </param>
    /// <returns>
    /// The 32-bit contribution of this h() byte lane after key-dependent q substitutions and MDS multiplication.
    /// </returns>
    /// <remarks>
    /// The q-permutation cascade depends on the key size. A 128-bit key uses the final two keyed layers; 192-bit
    /// and 256-bit keys prepend one or two additional keyed q layers respectively. The lane index selects the
    /// q0/q1 ordering defined by the Twofish specification.
    /// </remarks>
    private static uint HSub(byte x, ReadOnlySpan<uint> l, int k, int i)
    {
        int value = x;

        // For 256-bit keys, prepend the fourth S-box key word layer.
        if (k == 4)
        {
            value = ((i == 1 || i == 2) ? s_q0[value] : s_q1[value]) ^
                    (byte)(l[3] >> (8 * i));
        }

        // For 192- and 256-bit keys, prepend the third S-box key word layer.
        if (k >= 3)
        {
            value = ((i == 2 || i == 3) ? s_q0[value] : s_q1[value]) ^
                    (byte)(l[2] >> (8 * i));
        }

        // Final two keyed q layers are present for all supported Twofish key sizes.
        value = ((i == 0 || i == 2) ? s_q0[value] : s_q1[value]) ^
                (byte)(l[1] >> (8 * i));

        value = ((i == 0 || i == 1) ? s_q0[value] : s_q1[value]) ^
                (byte)(l[0] >> (8 * i));

        // The last unkeyed q layer completes the byte-lane S-box before MDS multiplication.
        value = (i == 1 || i == 3) ? s_q0[value] : s_q1[value];

        return MdsMultiplyColumn(i, (byte)value);
    }

    /// <summary>
    /// Multiplies a byte value by one column of the fixed Twofish MDS matrix.
    /// </summary>
    /// <param name="column">
    /// The MDS column index, in the range 0..3.
    /// </param>
    /// <param name="value">
    /// The byte value to multiply into the selected MDS column.
    /// </param>
    /// <returns>
    /// A 32-bit little-endian word containing the four row products for the selected MDS column.
    /// </returns>
    private static uint MdsMultiplyColumn(int column, byte value)
    {
        var z0 = GfMul(s_mds[0, column], value, 0x69);
        var z1 = GfMul(s_mds[1, column], value, 0x69);
        var z2 = GfMul(s_mds[2, column], value, 0x69);
        var z3 = GfMul(s_mds[3, column], value, 0x69);

        return z0 | (z1 << 8) | (z2 << 16) | (z3 << 24);
    }

    /// <summary>
    /// Multiplies two bytes in the finite field GF(2^8) using the specified reduction polynomial byte.
    /// </summary>
    /// <param name="a">
    /// The first field element.
    /// </param>
    /// <param name="b">
    /// The second field element.
    /// </param>
    /// <param name="primitive">
    /// The low-byte reduction constant for the field polynomial used by the target Twofish matrix.
    /// </param>
    /// <returns>
    /// The product of <paramref name="a"/> and <paramref name="b"/> in GF(2^8).
    /// </returns>
    /// <remarks>
    /// Twofish uses different field polynomials for the MDS and RS matrices. Passing the reduction byte keeps the
    /// multiplication routine shared while preserving the specified field for each matrix.
    /// </remarks>
    private static uint GfMul(byte a, byte b, byte primitive)
    {
        byte result = 0;

        for (var i = 0; i < 8; i++)
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

    /// <summary>
    /// Constructs one of the fixed Twofish q permutations from its four 4-bit permutation tables.
    /// </summary>
    /// <param name="t0">
    /// The first 4-bit permutation table.
    /// </param>
    /// <param name="t1">
    /// The second 4-bit permutation table.
    /// </param>
    /// <param name="t2">
    /// The third 4-bit permutation table.
    /// </param>
    /// <param name="t3">
    /// The fourth 4-bit permutation table.
    /// </param>
    /// <returns>
    /// A 256-entry byte table representing the composed q permutation.
    /// </returns>
    /// <remarks>
    /// The Twofish q permutations are defined over two nibbles using XORs, one-bit nibble rotations, and four
    /// fixed 4-bit tables. Precomputing the composed 8-bit permutation keeps h() and the key schedule simpler.
    /// </remarks>
    private static byte[] CreateQ(byte[] t0, byte[] t1, byte[] t2, byte[] t3)
    {
        var q = new byte[256];

        for (var x = 0; x < q.Length; x++)
        {
            var a0 = x >> 4;
            var b0 = x & 0x0F;

            var a1 = a0 ^ b0;
            var b1 = a0 ^ RotateRight4(b0) ^ ((a0 << 3) & 0x0F);

            int a2 = t0[a1];
            int b2 = t1[b1];

            var a3 = a2 ^ b2;
            var b3 = a2 ^ RotateRight4(b2) ^ ((a2 << 3) & 0x0F);

            int a4 = t2[a3];
            int b4 = t3[b3];

            q[x] = (byte)((b4 << 4) | a4);
        }

        return q;
    }

    /// <summary>
    /// Rotates a 4-bit value right by one bit.
    /// </summary>
    /// <param name="value">
    /// The low-nibble value to rotate.
    /// </param>
    /// <returns>
    /// The rotated 4-bit value, masked to the low nibble.
    /// </returns>
    /// <remarks>
    /// This operation is used by the q-permutation construction defined by Twofish.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RotateRight4(int value) =>
        ((value >> 1) | ((value & 0x01) << 3)) & 0x0F;

    /// <summary>
    /// Rotates a 32-bit word left by the specified number of bits.
    /// </summary>
    /// <param name="value">
    /// The word to rotate.
    /// </param>
    /// <param name="bits">
    /// The number of bits to rotate by.
    /// </param>
    /// <returns>
    /// The rotated 32-bit word.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int bits) =>
        (value << bits) | (value >> (32 - bits));

    /// <summary>
    /// Rotates a 32-bit word right by the specified number of bits.
    /// </summary>
    /// <param name="value">
    /// The word to rotate.
    /// </param>
    /// <param name="bits">
    /// The number of bits to rotate by.
    /// </param>
    /// <returns>
    /// The rotated 32-bit word.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateRight(uint value, int bits) =>
        (value >> bits) | (value << (32 - bits));

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
