// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the core Camellia block cipher engine, implementing low-level encryption and decryption of individual
/// 128-bit blocks. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Camellia is a symmetric-key block cipher jointly developed by NTT and Mitsubishi Electric and specified in
/// RFC 3713. It operates on a fixed 128-bit block size and accepts 128-bit, 192-bit, and 256-bit keys. The
/// 128-bit key variant applies 18 Feistel rounds (three 6-round groups separated by FL/FL<sup>−1</sup> layers),
/// while the 192-bit and 256-bit key variants apply 24 rounds (four 6-round groups).
/// </para>
/// <para>
/// This type exposes the raw Camellia block primitive. Key scheduling, FL/FL<sup>−1</sup> layer management, and
/// the F-function are handled internally. Most callers should prefer the higher-level <see cref="Camellia" />
/// class, which exposes the standard <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract.
/// Use <see cref="CamelliaBlockCipher" /> directly only when composing the raw block primitive with an
/// <see cref="IBlockCipherModeTransform" /> or <see cref="IPaddingStrategy" />.
/// </para>
/// <para>
/// This implementation is binary-compatible with the test vectors published in RFC 3713, Appendix A, and the
/// reference implementation maintained by NTT.
/// </para>
/// <list type="bullet">
/// <item><description><b>Block size:</b> 16 bytes (128 bits)</description></item>
/// <item><description><b>Key sizes:</b> 16 bytes (128 bits), 24 bytes (192 bits), 32 bytes (256 bits)</description></item>
/// <item><description><b>Rounds:</b> 18 (128-bit key) or 24 (192/256-bit key)</description></item>
/// </list>
/// </remarks>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc3713">RFC 3713 — A Description of the Camellia Encryption Algorithm</seealso>
/// <seealso cref="Camellia" />
public sealed class CamelliaBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// The Camellia block size, in bytes.
    /// </summary>
    public const int BlockBytes = 16;

    // SBOX1 from RFC 3713 Appendix B.1 — verified bijection (all 256 values 0x00..0xFF appear exactly once).
    private static readonly byte[] s_sbox1 = new byte[256]
    {
        0x70, 0x82, 0x2C, 0xEC, 0xB3, 0x27, 0xC0, 0xE5, 0xE4, 0x85, 0x57, 0x35, 0xEA, 0x0C, 0xAE, 0x41,
        0x23, 0xEF, 0x6B, 0x93, 0x45, 0x19, 0xA5, 0x21, 0xED, 0x0E, 0x4F, 0x4E, 0x1D, 0x65, 0x92, 0xBD,
        0x86, 0xB8, 0xAF, 0x8F, 0x7C, 0xEB, 0x1F, 0xCE, 0x3E, 0x30, 0xDC, 0x5F, 0x5E, 0xC5, 0x0B, 0x1A,
        0xA6, 0xE1, 0x39, 0xCA, 0xD5, 0x47, 0x5D, 0x3D, 0xD9, 0x01, 0x5A, 0xD6, 0x51, 0x56, 0x6C, 0x4D,
        0x8B, 0x0D, 0x9A, 0x66, 0xFB, 0xCC, 0xB0, 0x2D, 0x74, 0x12, 0x2B, 0x20, 0xF0, 0xB1, 0x84, 0x99,
        0xDF, 0x4C, 0xCB, 0xC2, 0x34, 0x7E, 0x76, 0x05, 0x6D, 0xB7, 0xA9, 0x31, 0xD1, 0x17, 0x04, 0xD7,
        0x14, 0x58, 0x3A, 0x61, 0xDE, 0x1B, 0x11, 0x1C, 0x32, 0x0F, 0x9C, 0x16, 0x53, 0x18, 0xF2, 0x22,
        0xFE, 0x44, 0xCF, 0xB2, 0xC3, 0xB5, 0x7A, 0x91, 0x24, 0x08, 0xE8, 0xA8, 0x60, 0xFC, 0x69, 0x50,
        0xAA, 0xD0, 0xA0, 0x7D, 0xA1, 0x89, 0x62, 0x97, 0x54, 0x5B, 0x1E, 0x95, 0xE0, 0xFF, 0x64, 0xD2,
        0x10, 0xC4, 0x00, 0x48, 0xA3, 0xF7, 0x75, 0xDB, 0x8A, 0x03, 0xE6, 0xDA, 0x09, 0x3F, 0xDD, 0x94,
        0x87, 0x5C, 0x83, 0x02, 0xCD, 0x4A, 0x90, 0x33, 0x73, 0x67, 0xF6, 0xF3, 0x9D, 0x7F, 0xBF, 0xE2,
        0x52, 0x9B, 0xD8, 0x26, 0xC8, 0x37, 0xC6, 0x3B, 0x81, 0x96, 0x6F, 0x4B, 0x13, 0xBE, 0x63, 0x2E,
        0xE9, 0x79, 0xA7, 0x8C, 0x9F, 0x6E, 0xBC, 0x8E, 0x29, 0xF5, 0xF9, 0xB6, 0x2F, 0xFD, 0xB4, 0x59,
        0x78, 0x98, 0x06, 0x6A, 0xE7, 0x46, 0x71, 0xBA, 0xD4, 0x25, 0xAB, 0x42, 0x88, 0xA2, 0x8D, 0xFA,
        0x72, 0x07, 0xB9, 0x55, 0xF8, 0xEE, 0xAC, 0x0A, 0x36, 0x49, 0x2A, 0x68, 0x3C, 0x38, 0xF1, 0xA4,
        0x40, 0x28, 0xD3, 0x7B, 0xBB, 0xC9, 0x43, 0xC1, 0x15, 0xE3, 0xAD, 0xF4, 0x77, 0xC7, 0x80, 0x9E,
    };

    // SIGMA constants for key schedule derivation (RFC 3713 §2.4).
    private static readonly ulong[] s_sigma = new ulong[]
    {
        0xA09E667F3BCC908BUL,
        0xB67AE8584CAA73B2UL,
        0xC6EF372FE94F82BEUL,
        0x54FF53A5F1D36F1CUL,
        0x10E527FADE682D1DUL,
        0xB05688C2B3E6C1FDUL,
    };

    private readonly ulong[] _kw;
    private readonly ulong[] _k;
    private readonly ulong[] _ke;
    private readonly bool _use192or256;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="CamelliaBlockCipher" /> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Must be 16, 24, or 32 bytes (128, 192, or 256 bits) in length. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not 16, 24, or 32 bytes in length.
    /// </exception>
    public CamelliaBlockCipher(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException("The Camellia key must be 16, 24, or 32 bytes in length.", nameof(key));

        _use192or256 = key.Length > 16;
        _kw = new ulong[4];
        _k = new ulong[_use192or256 ? 24 : 18];
        _ke = new ulong[_use192or256 ? 6 : 4];

        ExpandKey(key);
    }

    /// <inheritdoc />
    /// <remarks>The block size is fixed at 16 bytes (128 bits) regardless of key size.</remarks>
    public int BlockSize => BlockBytes;

    /// <summary>
    /// Decrypts a single 128-bit ciphertext block.
    /// </summary>
    /// <param name="input">The ciphertext block to decrypt. Must be exactly 16 bytes.</param>
    /// <param name="output">The buffer that receives the plaintext block. Must be exactly 16 bytes.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> or <paramref name="output" /> is not exactly 16 bytes in length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The cipher instance has been disposed.</exception>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockBytes);

        // Ciphertext was produced with D2 in the upper 8 bytes and D1 in the lower 8 bytes,
        // so read them in the same order and apply the reverse key schedule.
        ulong d1 = BinaryPrimitives.ReadUInt64BigEndian(input);
        ulong d2 = BinaryPrimitives.ReadUInt64BigEndian(input.Slice(8));

        d1 ^= _kw[2];
        d2 ^= _kw[3];

        if (!_use192or256)
        {
            // 18-round decryption (128-bit key) — round keys applied in reverse order.
            d2 ^= F(d1, _k[17]);
            d1 ^= F(d2, _k[16]);
            d2 ^= F(d1, _k[15]);
            d1 ^= F(d2, _k[14]);
            d2 ^= F(d1, _k[13]);
            d1 ^= F(d2, _k[12]);

            d1 = Fl(d1, _ke[3]);
            d2 = FlinV(d2, _ke[2]);

            d2 ^= F(d1, _k[11]);
            d1 ^= F(d2, _k[10]);
            d2 ^= F(d1, _k[9]);
            d1 ^= F(d2, _k[8]);
            d2 ^= F(d1, _k[7]);
            d1 ^= F(d2, _k[6]);

            d1 = Fl(d1, _ke[1]);
            d2 = FlinV(d2, _ke[0]);

            d2 ^= F(d1, _k[5]);
            d1 ^= F(d2, _k[4]);
            d2 ^= F(d1, _k[3]);
            d1 ^= F(d2, _k[2]);
            d2 ^= F(d1, _k[1]);
            d1 ^= F(d2, _k[0]);
        }
        else
        {
            // 24-round decryption (192/256-bit key) — round keys applied in reverse order.
            d2 ^= F(d1, _k[23]);
            d1 ^= F(d2, _k[22]);
            d2 ^= F(d1, _k[21]);
            d1 ^= F(d2, _k[20]);
            d2 ^= F(d1, _k[19]);
            d1 ^= F(d2, _k[18]);

            d1 = Fl(d1, _ke[5]);
            d2 = FlinV(d2, _ke[4]);

            d2 ^= F(d1, _k[17]);
            d1 ^= F(d2, _k[16]);
            d2 ^= F(d1, _k[15]);
            d1 ^= F(d2, _k[14]);
            d2 ^= F(d1, _k[13]);
            d1 ^= F(d2, _k[12]);

            d1 = Fl(d1, _ke[3]);
            d2 = FlinV(d2, _ke[2]);

            d2 ^= F(d1, _k[11]);
            d1 ^= F(d2, _k[10]);
            d2 ^= F(d1, _k[9]);
            d1 ^= F(d2, _k[8]);
            d2 ^= F(d1, _k[7]);
            d1 ^= F(d2, _k[6]);

            d1 = Fl(d1, _ke[1]);
            d2 = FlinV(d2, _ke[0]);

            d2 ^= F(d1, _k[5]);
            d1 ^= F(d2, _k[4]);
            d2 ^= F(d1, _k[3]);
            d1 ^= F(d2, _k[2]);
            d2 ^= F(d1, _k[1]);
            d1 ^= F(d2, _k[0]);
        }

        // Post-whitening: undo the encryption pre-whitening (kw1 was applied to D1, kw2 to D2).
        d2 ^= _kw[0];
        d1 ^= _kw[1];

        // Plaintext M = D2 || D1.
        BinaryPrimitives.WriteUInt64BigEndian(output, d2);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(8), d1);
    }

    /// <summary>
    /// Securely clears all expanded subkey material and marks the instance as disposed.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            CryptoHelpers.Clear(_kw);
            CryptoHelpers.Clear(_k);
            CryptoHelpers.Clear(_ke);

            _disposed = true;
        }
    }

    /// <summary>
    /// Encrypts a single 128-bit plaintext block.
    /// </summary>
    /// <param name="input">The plaintext block to encrypt. Must be exactly 16 bytes.</param>
    /// <param name="output">The buffer that receives the ciphertext block. Must be exactly 16 bytes.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input" /> or <paramref name="output" /> is not exactly 16 bytes in length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The cipher instance has been disposed.</exception>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockBytes);

        ulong d1 = BinaryPrimitives.ReadUInt64BigEndian(input);
        ulong d2 = BinaryPrimitives.ReadUInt64BigEndian(input.Slice(8));

        d1 ^= _kw[0];
        d2 ^= _kw[1];

        if (!_use192or256)
        {
            // 18-round encryption (128-bit key).
            d2 ^= F(d1, _k[0]);
            d1 ^= F(d2, _k[1]);
            d2 ^= F(d1, _k[2]);
            d1 ^= F(d2, _k[3]);
            d2 ^= F(d1, _k[4]);
            d1 ^= F(d2, _k[5]);

            d1 = Fl(d1, _ke[0]);
            d2 = FlinV(d2, _ke[1]);

            d2 ^= F(d1, _k[6]);
            d1 ^= F(d2, _k[7]);
            d2 ^= F(d1, _k[8]);
            d1 ^= F(d2, _k[9]);
            d2 ^= F(d1, _k[10]);
            d1 ^= F(d2, _k[11]);

            d1 = Fl(d1, _ke[2]);
            d2 = FlinV(d2, _ke[3]);

            d2 ^= F(d1, _k[12]);
            d1 ^= F(d2, _k[13]);
            d2 ^= F(d1, _k[14]);
            d1 ^= F(d2, _k[15]);
            d2 ^= F(d1, _k[16]);
            d1 ^= F(d2, _k[17]);
        }
        else
        {
            // 24-round encryption (192/256-bit key).
            d2 ^= F(d1, _k[0]);
            d1 ^= F(d2, _k[1]);
            d2 ^= F(d1, _k[2]);
            d1 ^= F(d2, _k[3]);
            d2 ^= F(d1, _k[4]);
            d1 ^= F(d2, _k[5]);

            d1 = Fl(d1, _ke[0]);
            d2 = FlinV(d2, _ke[1]);

            d2 ^= F(d1, _k[6]);
            d1 ^= F(d2, _k[7]);
            d2 ^= F(d1, _k[8]);
            d1 ^= F(d2, _k[9]);
            d2 ^= F(d1, _k[10]);
            d1 ^= F(d2, _k[11]);

            d1 = Fl(d1, _ke[2]);
            d2 = FlinV(d2, _ke[3]);

            d2 ^= F(d1, _k[12]);
            d1 ^= F(d2, _k[13]);
            d2 ^= F(d1, _k[14]);
            d1 ^= F(d2, _k[15]);
            d2 ^= F(d1, _k[16]);
            d1 ^= F(d2, _k[17]);

            d1 = Fl(d1, _ke[4]);
            d2 = FlinV(d2, _ke[5]);

            d2 ^= F(d1, _k[18]);
            d1 ^= F(d2, _k[19]);
            d2 ^= F(d1, _k[20]);
            d1 ^= F(d2, _k[21]);
            d2 ^= F(d1, _k[22]);
            d1 ^= F(d2, _k[23]);
        }

        // Post-whitening: D2 becomes the upper half of the ciphertext block.
        d2 ^= _kw[2];
        d1 ^= _kw[3];

        BinaryPrimitives.WriteUInt64BigEndian(output, d2);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(8), d1);
    }

    /// <summary>
    /// Expands the supplied key into the whitening keys (<c>_kw</c>), round subkeys (<c>_k</c>), and FL/FL⁻¹
    /// layer keys (<c>_ke</c>) according to RFC 3713 §2.4.
    /// </summary>
    /// <param name="key">The raw key material (16, 24, or 32 bytes).</param>
    private void ExpandKey(ReadOnlySpan<byte> key)
    {
        ulong klhi = BinaryPrimitives.ReadUInt64BigEndian(key);
        ulong kllo = BinaryPrimitives.ReadUInt64BigEndian(key.Slice(8));

        ulong krhi, krlo;
        if (key.Length == 16)
        {
            krhi = 0;
            krlo = 0;
        }
        else if (key.Length == 24)
        {
            krhi = BinaryPrimitives.ReadUInt64BigEndian(key.Slice(16));
            // The 64-bit pad for a 192-bit key is the bitwise complement of the first KR word.
            krlo = ~krhi;
        }
        else
        {
            krhi = BinaryPrimitives.ReadUInt64BigEndian(key.Slice(16));
            krlo = BinaryPrimitives.ReadUInt64BigEndian(key.Slice(24));
        }

        // Derive KA from KL and KR via four Feistel rounds using SIGMA1..4.
        ulong d1 = klhi ^ krhi;
        ulong d2 = kllo ^ krlo;

        d2 ^= F(d1, s_sigma[0]);
        d1 ^= F(d2, s_sigma[1]);
        d1 ^= klhi;
        d2 ^= kllo;
        d2 ^= F(d1, s_sigma[2]);
        d1 ^= F(d2, s_sigma[3]);

        ulong kahi = d1;
        ulong kalo = d2;

        if (!_use192or256)
        {
            // 128-bit key schedule (RFC 3713 §2.4.1).
            ulong hi, lo;

            (_kw[0], _kw[1]) = (klhi, kllo);

            (_k[0], _k[1]) = (kahi, kalo);

            (hi, lo) = RotL128(klhi, kllo, 15);
            (_ke[0], _ke[1]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 15);
            (_k[2], _k[3]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 30);
            (_k[4], _k[5]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 45);
            (_k[6], _k[7]) = (hi, lo);

            // k9 takes only the upper half of (KA <<< 45).
            (hi, lo) = RotL128(kahi, kalo, 45);
            _k[8] = hi;

            // k10 takes only the lower half of (KL <<< 60).
            (hi, lo) = RotL128(klhi, kllo, 60);
            _k[9] = lo;

            (hi, lo) = RotL128(kahi, kalo, 60);
            (_k[10], _k[11]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 77);
            (_ke[2], _ke[3]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 94);
            (_k[12], _k[13]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 94);
            (_k[14], _k[15]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 111);
            (_k[16], _k[17]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 111);
            (_kw[2], _kw[3]) = (hi, lo);
        }
        else
        {
            // Derive KB from KA and KR via two additional Feistel rounds using SIGMA5..6.
            d1 = kahi ^ krhi;
            d2 = kalo ^ krlo;
            d2 ^= F(d1, s_sigma[4]);
            d1 ^= F(d2, s_sigma[5]);
            ulong kbhi = d1;
            ulong kblo = d2;

            // 192/256-bit key schedule (RFC 3713 §2.4.2).
            ulong hi, lo;

            (_kw[0], _kw[1]) = (klhi, kllo);

            (_k[0], _k[1]) = (kbhi, kblo);

            (hi, lo) = RotL128(krhi, krlo, 15);
            (_ke[0], _ke[1]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 15);
            (_k[2], _k[3]) = (hi, lo);

            (hi, lo) = RotL128(krhi, krlo, 30);
            (_k[4], _k[5]) = (hi, lo);

            (hi, lo) = RotL128(kbhi, kblo, 30);
            (_k[6], _k[7]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 45);
            (_ke[2], _ke[3]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 45);
            (_k[8], _k[9]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 60);
            (_k[10], _k[11]) = (hi, lo);

            (hi, lo) = RotL128(krhi, krlo, 60);
            (_k[12], _k[13]) = (hi, lo);

            (hi, lo) = RotL128(kbhi, kblo, 60);
            (_k[14], _k[15]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 77);
            (_ke[4], _ke[5]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 77);
            (_k[16], _k[17]) = (hi, lo);

            (hi, lo) = RotL128(krhi, krlo, 94);
            (_k[18], _k[19]) = (hi, lo);

            (hi, lo) = RotL128(kahi, kalo, 94);
            (_k[20], _k[21]) = (hi, lo);

            (hi, lo) = RotL128(klhi, kllo, 111);
            (_k[22], _k[23]) = (hi, lo);

            (hi, lo) = RotL128(kbhi, kblo, 111);
            (_kw[2], _kw[3]) = (hi, lo);
        }
    }

    /// <summary>
    /// Applies the Camellia F-function: XOR the 64-bit input with the round key, apply the four S-boxes and the
    /// P-function linear diffusion layer, and return the 64-bit result.
    /// </summary>
    /// <param name="x">The 64-bit data word.</param>
    /// <param name="ke">The 64-bit round subkey.</param>
    /// <returns>The transformed 64-bit output word.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong F(ulong x, ulong ke)
    {
        x ^= ke;

        // Apply the four Camellia S-boxes to each byte in order:
        //   Positions 0, 7 → SBOX1(b)
        //   Positions 1, 4 → SBOX2(b) = ROTL1(SBOX1(b))
        //   Positions 2, 5 → SBOX3(b) = ROTL7(SBOX1(b))
        //   Positions 3, 6 → SBOX4(b) = SBOX1(ROTL1(b))
        int t1 = s_sbox1[(int)(x >> 56)];
        int t2 = RotL1b(s_sbox1[(int)((x >> 48) & 0xFF)]);
        int t3 = RotR1b(s_sbox1[(int)((x >> 40) & 0xFF)]);
        int t4 = s_sbox1[RotL1b((int)((x >> 32) & 0xFF))];
        int t5 = RotL1b(s_sbox1[(int)((x >> 24) & 0xFF)]);
        int t6 = RotR1b(s_sbox1[(int)((x >> 16) & 0xFF)]);
        int t7 = s_sbox1[RotL1b((int)((x >> 8) & 0xFF))];
        int t8 = s_sbox1[(int)(x & 0xFF)];

        // P-function: MDS-style diffusion over 8 bytes (RFC 3713 §2.1).
        int u1 = t1 ^ t3 ^ t4 ^ t6 ^ t7 ^ t8;
        int u2 = t1 ^ t2 ^ t4 ^ t5 ^ t7 ^ t8;
        int u3 = t1 ^ t2 ^ t3 ^ t5 ^ t6 ^ t8;
        int u4 = t2 ^ t3 ^ t4 ^ t5 ^ t6 ^ t7;
        int u5 = t1 ^ t2 ^ t6 ^ t7 ^ t8;
        int u6 = t2 ^ t3 ^ t5 ^ t7 ^ t8;
        int u7 = t3 ^ t4 ^ t5 ^ t6 ^ t8;
        int u8 = t1 ^ t4 ^ t5 ^ t6 ^ t7;

        return ((ulong)(u1 & 0xFF) << 56) | ((ulong)(u2 & 0xFF) << 48)
             | ((ulong)(u3 & 0xFF) << 40) | ((ulong)(u4 & 0xFF) << 32)
             | ((ulong)(u5 & 0xFF) << 24) | ((ulong)(u6 & 0xFF) << 16)
             | ((ulong)(u7 & 0xFF) << 8)  |  (ulong)(u8 & 0xFF);
    }

    /// <summary>
    /// Applies the Camellia FL function to a 64-bit value using the supplied 64-bit subkey.
    /// </summary>
    /// <param name="x">The 64-bit input.</param>
    /// <param name="ke">The 64-bit subkey.</param>
    /// <returns>The transformed 64-bit output.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Fl(ulong x, ulong ke)
    {
        uint x1 = (uint)(x >> 32);
        uint x2 = (uint)x;
        uint k1 = (uint)(ke >> 32);
        uint k2 = (uint)ke;

        x2 ^= RotL1u(x1 & k1);
        x1 ^= (x2 | k2);

        return ((ulong)x1 << 32) | x2;
    }

    /// <summary>
    /// Applies the inverse Camellia FL function (FL⁻¹) to a 64-bit value using the supplied 64-bit subkey.
    /// </summary>
    /// <param name="x">The 64-bit input.</param>
    /// <param name="ke">The 64-bit subkey.</param>
    /// <returns>The inverse-transformed 64-bit output.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FlinV(ulong x, ulong ke)
    {
        uint y1 = (uint)(x >> 32);
        uint y2 = (uint)x;
        uint k1 = (uint)(ke >> 32);
        uint k2 = (uint)ke;

        y1 ^= (y2 | k2);
        y2 ^= RotL1u(y1 & k1);

        return ((ulong)y1 << 32) | y2;
    }

    /// <summary>
    /// Rotates a 128-bit value left by <paramref name="n" /> bits. The value is represented as a (hi, lo) pair of
    /// 64-bit words where <paramref name="hi" /> holds the most-significant bits.
    /// </summary>
    /// <param name="hi">The upper 64 bits of the 128-bit value.</param>
    /// <param name="lo">The lower 64 bits of the 128-bit value.</param>
    /// <param name="n">The rotation count in bits. Must be in the range 1..127.</param>
    /// <returns>The rotated (hi, lo) pair.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong hi, ulong lo) RotL128(ulong hi, ulong lo, int n)
    {
        if (n < 64)
            return ((hi << n) | (lo >> (64 - n)), (lo << n) | (hi >> (64 - n)));

        n -= 64;
        if (n == 0) return (lo, hi);
        return ((lo << n) | (hi >> (64 - n)), (hi << n) | (lo >> (64 - n)));
    }

    /// <summary>
    /// Rotates an 8-bit value left by 1 bit.
    /// </summary>
    /// <param name="x">The 8-bit value to rotate (only the lower 8 bits are used).</param>
    /// <returns>The left-rotated byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RotL1b(int x) => ((x << 1) | (x >> 7)) & 0xFF;

    /// <summary>
    /// Rotates an 8-bit value right by 1 bit (equivalent to a left rotation by 7 bits), producing SBOX3 outputs.
    /// </summary>
    /// <param name="x">The 8-bit value to rotate (only the lower 8 bits are used).</param>
    /// <returns>The right-rotated byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RotR1b(int x) => ((x >> 1) | (x << 7)) & 0xFF;

    /// <summary>
    /// Rotates a 32-bit unsigned integer left by 1 bit, as required by the FL/FL⁻¹ functions.
    /// </summary>
    /// <param name="x">The value to rotate.</param>
    /// <returns>The rotated 32-bit value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotL1u(uint x) => (x << 1) | (x >> 31);

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> if this cipher instance has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(nameof(CamelliaBlockCipher));
#endif
    }
}
