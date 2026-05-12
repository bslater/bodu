// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
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
/// This implementation keeps the RFC 3713 model visible: the S-box material is represented by the published
/// <c>SBOX1</c> table, <c>SBOX2</c>/<c>SBOX3</c>/<c>SBOX4</c> are derived exactly as specified, the
/// <c>SIGMA</c> constants are stored as their 64-bit values, and whitening keys (<c>kw1..kw4</c>) are kept
/// separate from round keys (<c>k1..k18</c> or <c>k1..k24</c>) and FL/FL<sup>−1</sup> keys
/// (<c>ke1..ke4</c> or <c>ke1..ke6</c>).
/// </para>
/// <para>
/// This type exposes the raw Camellia block primitive. Most callers should prefer the higher-level
/// <see cref="Camellia"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract. Use
/// <see cref="CamelliaBlockCipher"/> directly only when composing the raw block primitive with an
/// <see cref="IBlockCipherModeTransform"/> or <see cref="IPaddingStrategy"/>.
/// </para>
/// <list type="bullet">
/// <item><description><b>Block size:</b> 16 bytes (128 bits)</description></item>
/// <item><description><b>Key sizes:</b> 16 bytes (128 bits), 24 bytes (192 bits), 32 bytes (256 bits)</description></item>
/// <item><description><b>Rounds:</b> 18 (128-bit key) or 24 (192/256-bit key)</description></item>
/// </list>
/// </remarks>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc3713">RFC 3713 — A Description of the Camellia Encryption Algorithm</seealso>
/// <seealso cref="Camellia"/>
public sealed class CamelliaBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// Length of the Camellia block is 128 bits (16 bytes). Internal constant kept for span-length validation;
    /// callers should read <see cref="BlockSize"/> instead.
    /// </summary>
    private const int BlockBits = 128;

    /// <summary>Length of the Camellia 128-bit key is 128 bits (16 bytes). Internal use only.</summary>
    private const int Key128SizeInBytes = 16;

    /// <summary>Length of the Camellia 192-bit key is 192 bits (24 bytes). Internal use only.</summary>
    private const int Key192SizeInBytes = 24;

    /// <summary>Length of the Camellia 256-bit key is 256 bits (32 bytes). Internal use only.</summary>
    private const int Key256SizeInBytes = 32;

    // SBOX1 from RFC 3713 Appendix A. SBOX2, SBOX3, and SBOX4 are derived from this table by the F function.
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

    // SIGMA1..SIGMA6 from RFC 3713 §2.4, represented as 64-bit constants.
    private static readonly ulong[] s_sigma = new ulong[]
    {
        0xA09E667F3BCC908BUL,
        0xB67AE8584CAA73B2UL,
        0xC6EF372FE94F82BEUL,
        0x54FF53A5F1D36F1CUL,
        0x10E527FADE682D1DUL,
        0xB05688C2B3E6C1FDUL,
    };

    // Whitening keys kw1..kw4 in RFC order.
    private readonly ulong[] _kw;

    // Round keys k1..k18 (128-bit key) or k1..k24 (192/256-bit key) in RFC order.
    private readonly ulong[] _k;

    // FL/FLINV keys ke1..ke4 (128-bit key) or ke1..ke6 (192/256-bit key) in RFC order.
    private readonly ulong[] _ke;

    private readonly bool _usesExtendedKeySchedule;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CamelliaBlockCipher"/> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Must be 16, 24, or 32 bytes (128, 192, or 256 bits) in length.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is not 16, 24, or 32 bytes in length.
    /// </exception>
    public CamelliaBlockCipher(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (Key128SizeInBytes or Key192SizeInBytes or Key256SizeInBytes))
            throw new ArgumentException(
                CryptoResourceStrings.ArgumentException_Camellia_InvalidKeyLength,
                nameof(key));

        _usesExtendedKeySchedule = key.Length > Key128SizeInBytes;
        _kw = new ulong[4];
        _k = new ulong[_usesExtendedKeySchedule ? 24 : 18];
        _ke = new ulong[_usesExtendedKeySchedule ? 6 : 4];

        ExpandKey(key);
    }

    /// <inheritdoc />
    /// <value>Length of the Camellia block is 128 bits (16 bytes).</value>
    /// <remarks>The block size is fixed at 128 bits (16 bytes) regardless of key size.</remarks>
    public int BlockSize => BlockBits;

    /// <summary>
    /// Decrypts a single 128-bit ciphertext block.
    /// </summary>
    /// <param name="input">The ciphertext block to decrypt. Must be exactly 16 bytes.</param>
    /// <param name="output">The buffer that receives the plaintext block. Must be exactly 16 bytes.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> or <paramref name="output"/> is not exactly 16 bytes in length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The cipher instance has been disposed.</exception>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockBits / 8);

        // Encryption produced (right ^ kw3) || (left ^ kw4) — restore the post-rounds halves first.
        var right = BinaryPrimitives.ReadUInt64BigEndian(input) ^ _kw[2];
        var left = BinaryPrimitives.ReadUInt64BigEndian(input[8..]) ^ _kw[3];

        if (!_usesExtendedKeySchedule)
        {
            // Reverse rounds 18..13.
            left ^= F(right, _k[17]);
            right ^= F(left, _k[16]);
            left ^= F(right, _k[15]);
            right ^= F(left, _k[14]);
            left ^= F(right, _k[13]);
            right ^= F(left, _k[12]);

            left = FlInv(left, _ke[2]);
            right = Fl(right, _ke[3]);

            // Reverse rounds 12..7.
            left ^= F(right, _k[11]);
            right ^= F(left, _k[10]);
            left ^= F(right, _k[9]);
            right ^= F(left, _k[8]);
            left ^= F(right, _k[7]);
            right ^= F(left, _k[6]);

            left = FlInv(left, _ke[0]);
            right = Fl(right, _ke[1]);

            // Reverse rounds 6..1.
            left ^= F(right, _k[5]);
            right ^= F(left, _k[4]);
            left ^= F(right, _k[3]);
            right ^= F(left, _k[2]);
            left ^= F(right, _k[1]);
            right ^= F(left, _k[0]);
        }
        else
        {
            // Reverse rounds 24..19.
            left ^= F(right, _k[23]);
            right ^= F(left, _k[22]);
            left ^= F(right, _k[21]);
            right ^= F(left, _k[20]);
            left ^= F(right, _k[19]);
            right ^= F(left, _k[18]);

            left = FlInv(left, _ke[4]);
            right = Fl(right, _ke[5]);

            // Reverse rounds 18..13.
            left ^= F(right, _k[17]);
            right ^= F(left, _k[16]);
            left ^= F(right, _k[15]);
            right ^= F(left, _k[14]);
            left ^= F(right, _k[13]);
            right ^= F(left, _k[12]);

            left = FlInv(left, _ke[2]);
            right = Fl(right, _ke[3]);

            // Reverse rounds 12..7.
            left ^= F(right, _k[11]);
            right ^= F(left, _k[10]);
            left ^= F(right, _k[9]);
            right ^= F(left, _k[8]);
            left ^= F(right, _k[7]);
            right ^= F(left, _k[6]);

            left = FlInv(left, _ke[0]);
            right = Fl(right, _ke[1]);

            // Reverse rounds 6..1.
            left ^= F(right, _k[5]);
            right ^= F(left, _k[4]);
            left ^= F(right, _k[3]);
            right ^= F(left, _k[2]);
            left ^= F(right, _k[1]);
            right ^= F(left, _k[0]);
        }

        // Undo input whitening: plaintext = (left ^ kw1) || (right ^ kw2).
        left ^= _kw[0];
        right ^= _kw[1];

        BinaryPrimitives.WriteUInt64BigEndian(output, left);
        BinaryPrimitives.WriteUInt64BigEndian(output[8..], right);
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
    /// <paramref name="input"/> or <paramref name="output"/> is not exactly 16 bytes in length.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The cipher instance has been disposed.</exception>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockBits / 8);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockBits / 8);

        var left = BinaryPrimitives.ReadUInt64BigEndian(input) ^ _kw[0];
        var right = BinaryPrimitives.ReadUInt64BigEndian(input[8..]) ^ _kw[1];

        if (!_usesExtendedKeySchedule)
        {
            // Rounds 1..6.
            right ^= F(left, _k[0]);
            left ^= F(right, _k[1]);
            right ^= F(left, _k[2]);
            left ^= F(right, _k[3]);
            right ^= F(left, _k[4]);
            left ^= F(right, _k[5]);

            left = Fl(left, _ke[0]);
            right = FlInv(right, _ke[1]);

            // Rounds 7..12.
            right ^= F(left, _k[6]);
            left ^= F(right, _k[7]);
            right ^= F(left, _k[8]);
            left ^= F(right, _k[9]);
            right ^= F(left, _k[10]);
            left ^= F(right, _k[11]);

            left = Fl(left, _ke[2]);
            right = FlInv(right, _ke[3]);

            // Rounds 13..18.
            right ^= F(left, _k[12]);
            left ^= F(right, _k[13]);
            right ^= F(left, _k[14]);
            left ^= F(right, _k[15]);
            right ^= F(left, _k[16]);
            left ^= F(right, _k[17]);
        }
        else
        {
            // Rounds 1..6.
            right ^= F(left, _k[0]);
            left ^= F(right, _k[1]);
            right ^= F(left, _k[2]);
            left ^= F(right, _k[3]);
            right ^= F(left, _k[4]);
            left ^= F(right, _k[5]);

            left = Fl(left, _ke[0]);
            right = FlInv(right, _ke[1]);

            // Rounds 7..12.
            right ^= F(left, _k[6]);
            left ^= F(right, _k[7]);
            right ^= F(left, _k[8]);
            left ^= F(right, _k[9]);
            right ^= F(left, _k[10]);
            left ^= F(right, _k[11]);

            left = Fl(left, _ke[2]);
            right = FlInv(right, _ke[3]);

            // Rounds 13..18.
            right ^= F(left, _k[12]);
            left ^= F(right, _k[13]);
            right ^= F(left, _k[14]);
            left ^= F(right, _k[15]);
            right ^= F(left, _k[16]);
            left ^= F(right, _k[17]);

            left = Fl(left, _ke[4]);
            right = FlInv(right, _ke[5]);

            // Rounds 19..24.
            right ^= F(left, _k[18]);
            left ^= F(right, _k[19]);
            right ^= F(left, _k[20]);
            left ^= F(right, _k[21]);
            right ^= F(left, _k[22]);
            left ^= F(right, _k[23]);
        }

        // Final swap and output whitening: ciphertext = (right ^ kw3) || (left ^ kw4).
        right ^= _kw[2];
        left ^= _kw[3];

        BinaryPrimitives.WriteUInt64BigEndian(output, right);
        BinaryPrimitives.WriteUInt64BigEndian(output[8..], left);
    }

    /// <summary>
    /// Expands the supplied key into the whitening keys (<c>_kw</c>), round subkeys (<c>_k</c>), and FL/FL<sup>−1</sup>
    /// layer keys (<c>_ke</c>) according to RFC 3713 §2.4.
    /// </summary>
    /// <param name="key">The raw key material (16, 24, or 32 bytes).</param>
    private void ExpandKey(ReadOnlySpan<byte> key)
    {
        var klHi = BinaryPrimitives.ReadUInt64BigEndian(key);
        var klLo = BinaryPrimitives.ReadUInt64BigEndian(key[8..]);

        ulong krHi;
        ulong krLo;

        if (key.Length == Key128SizeInBytes)
        {
            krHi = 0;
            krLo = 0;
        }
        else if (key.Length == Key192SizeInBytes)
        {
            krHi = BinaryPrimitives.ReadUInt64BigEndian(key[16..]);

            // The 64-bit pad for a 192-bit key is the bitwise complement of the first KR word (RFC 3713 §2.4).
            krLo = ~krHi;
        }
        else
        {
            krHi = BinaryPrimitives.ReadUInt64BigEndian(key[16..]);
            krLo = BinaryPrimitives.ReadUInt64BigEndian(key[24..]);
        }

        // KA derivation: four Feistel rounds keyed by SIGMA1..SIGMA4.
        var d1 = klHi ^ krHi;
        var d2 = klLo ^ krLo;

        d2 ^= F(d1, s_sigma[0]);
        d1 ^= F(d2, s_sigma[1]);

        d1 ^= klHi;
        d2 ^= klLo;

        d2 ^= F(d1, s_sigma[2]);
        d1 ^= F(d2, s_sigma[3]);

        var kaHi = d1;
        var kaLo = d2;

        if (!_usesExtendedKeySchedule)
        {
            Expand128BitKey(klHi, klLo, kaHi, kaLo);
            return;
        }

        // KB derivation: two further Feistel rounds keyed by SIGMA5..SIGMA6.
        d1 = kaHi ^ krHi;
        d2 = kaLo ^ krLo;

        d2 ^= F(d1, s_sigma[4]);
        d1 ^= F(d2, s_sigma[5]);

        Expand192Or256BitKey(klHi, klLo, krHi, krLo, kaHi, kaLo, d1, d2);
    }

    /// <summary>
    /// Expands a 128-bit key into RFC-order whitening, round, and FL/FL<sup>−1</sup> keys per RFC 3713 §2.4.1.
    /// </summary>
    /// <param name="klHi">The upper 64 bits of <c>KL</c>.</param>
    /// <param name="klLo">The lower 64 bits of <c>KL</c>.</param>
    /// <param name="kaHi">The upper 64 bits of <c>KA</c>.</param>
    /// <param name="kaLo">The lower 64 bits of <c>KA</c>.</param>
    private void Expand128BitKey(ulong klHi, ulong klLo, ulong kaHi, ulong kaLo)
    {
        // kw1, kw2 = KL <<< 0
        _kw[0] = klHi;
        _kw[1] = klLo;

        // k1, k2 = KA <<< 0
        _k[0] = kaHi;
        _k[1] = kaLo;

        // k3, k4 = KL <<< 15
        (_k[2], _k[3]) = RotL128(klHi, klLo, 15);

        // k5, k6 = KA <<< 15
        (_k[4], _k[5]) = RotL128(kaHi, kaLo, 15);

        // ke1, ke2 = KA <<< 30
        (_ke[0], _ke[1]) = RotL128(kaHi, kaLo, 30);

        // k7, k8 = KL <<< 45
        (_k[6], _k[7]) = RotL128(klHi, klLo, 45);

        // k9 = (KA <<< 45) >> 64 — upper half only.
        (var hi, _) = RotL128(kaHi, kaLo, 45);
        _k[8] = hi;

        // k10 = (KL <<< 60) & MASK64 — lower half only.
        (_, var lo) = RotL128(klHi, klLo, 60);
        _k[9] = lo;

        // k11, k12 = KA <<< 60
        (_k[10], _k[11]) = RotL128(kaHi, kaLo, 60);

        // ke3, ke4 = KL <<< 77
        (_ke[2], _ke[3]) = RotL128(klHi, klLo, 77);

        // k13, k14 = KL <<< 94
        (_k[12], _k[13]) = RotL128(klHi, klLo, 94);

        // k15, k16 = KA <<< 94
        (_k[14], _k[15]) = RotL128(kaHi, kaLo, 94);

        // k17, k18 = KL <<< 111
        (_k[16], _k[17]) = RotL128(klHi, klLo, 111);

        // kw3, kw4 = KA <<< 111
        (_kw[2], _kw[3]) = RotL128(kaHi, kaLo, 111);
    }

    /// <summary>
    /// Expands a 192-bit or 256-bit key into RFC-order whitening, round, and FL/FL<sup>−1</sup> keys per RFC 3713 §2.4.2.
    /// </summary>
    /// <param name="klHi">The upper 64 bits of <c>KL</c>.</param>
    /// <param name="klLo">The lower 64 bits of <c>KL</c>.</param>
    /// <param name="krHi">The upper 64 bits of <c>KR</c>.</param>
    /// <param name="krLo">The lower 64 bits of <c>KR</c>.</param>
    /// <param name="kaHi">The upper 64 bits of <c>KA</c>.</param>
    /// <param name="kaLo">The lower 64 bits of <c>KA</c>.</param>
    /// <param name="kbHi">The upper 64 bits of <c>KB</c>.</param>
    /// <param name="kbLo">The lower 64 bits of <c>KB</c>.</param>
    private void Expand192Or256BitKey(
        ulong klHi,
        ulong klLo,
        ulong krHi,
        ulong krLo,
        ulong kaHi,
        ulong kaLo,
        ulong kbHi,
        ulong kbLo)
    {
        // kw1, kw2 = KL <<< 0
        _kw[0] = klHi;
        _kw[1] = klLo;

        // k1, k2 = KB <<< 0
        _k[0] = kbHi;
        _k[1] = kbLo;

        // k3, k4 = KR <<< 15
        (_k[2], _k[3]) = RotL128(krHi, krLo, 15);

        // k5, k6 = KA <<< 15
        (_k[4], _k[5]) = RotL128(kaHi, kaLo, 15);

        // ke1, ke2 = KR <<< 30
        (_ke[0], _ke[1]) = RotL128(krHi, krLo, 30);

        // k7, k8 = KB <<< 30
        (_k[6], _k[7]) = RotL128(kbHi, kbLo, 30);

        // k9, k10 = KL <<< 45
        (_k[8], _k[9]) = RotL128(klHi, klLo, 45);

        // k11, k12 = KA <<< 45
        (_k[10], _k[11]) = RotL128(kaHi, kaLo, 45);

        // ke3, ke4 = KL <<< 60
        (_ke[2], _ke[3]) = RotL128(klHi, klLo, 60);

        // k13, k14 = KR <<< 60
        (_k[12], _k[13]) = RotL128(krHi, krLo, 60);

        // k15, k16 = KB <<< 60
        (_k[14], _k[15]) = RotL128(kbHi, kbLo, 60);

        // ke5, ke6 = KA <<< 77
        (_ke[4], _ke[5]) = RotL128(kaHi, kaLo, 77);

        // k17, k18 = KL <<< 77
        (_k[16], _k[17]) = RotL128(klHi, klLo, 77);

        // k19, k20 = KR <<< 94
        (_k[18], _k[19]) = RotL128(krHi, krLo, 94);

        // k21, k22 = KA <<< 94
        (_k[20], _k[21]) = RotL128(kaHi, kaLo, 94);

        // k23, k24 = KL <<< 111
        (_k[22], _k[23]) = RotL128(klHi, klLo, 111);

        // kw3, kw4 = KB <<< 111
        (_kw[2], _kw[3]) = RotL128(kbHi, kbLo, 111);
    }

    /// <summary>
    /// Applies the Camellia F-function: XOR the 64-bit input with the round key, apply the four S-boxes and the
    /// P-function linear diffusion layer, and return the 64-bit result.
    /// </summary>
    /// <param name="x">The 64-bit data word.</param>
    /// <param name="key">The 64-bit round subkey.</param>
    /// <returns>The transformed 64-bit output word.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong F(ulong x, ulong key)
    {
        x ^= key;

        // Apply the four Camellia S-boxes to each byte in RFC 3713 §2.1 order:
        //   Positions 0, 7 → SBOX1
        //   Positions 1, 4 → SBOX2
        //   Positions 2, 5 → SBOX3
        //   Positions 3, 6 → SBOX4
        int t1 = s_sbox1[(int)(x >> 56)];
        var t2 = SBox2((int)((x >> 48) & 0xFF));
        var t3 = SBox3((int)((x >> 40) & 0xFF));
        var t4 = SBox4((int)((x >> 32) & 0xFF));
        var t5 = SBox2((int)((x >> 24) & 0xFF));
        var t6 = SBox3((int)((x >> 16) & 0xFF));
        var t7 = SBox4((int)((x >> 8) & 0xFF));
        int t8 = s_sbox1[(int)(x & 0xFF)];

        // P-function: byte-wise MDS-style diffusion (RFC 3713 §2.1).
        var y1 = t1 ^ t3 ^ t4 ^ t6 ^ t7 ^ t8;
        var y2 = t1 ^ t2 ^ t4 ^ t5 ^ t7 ^ t8;
        var y3 = t1 ^ t2 ^ t3 ^ t5 ^ t6 ^ t8;
        var y4 = t2 ^ t3 ^ t4 ^ t5 ^ t6 ^ t7;
        var y5 = t1 ^ t2 ^ t6 ^ t7 ^ t8;
        var y6 = t2 ^ t3 ^ t5 ^ t7 ^ t8;
        var y7 = t3 ^ t4 ^ t5 ^ t6 ^ t8;
        var y8 = t1 ^ t4 ^ t5 ^ t6 ^ t7;

        return ((ulong)(y1 & 0xFF) << 56)
             | ((ulong)(y2 & 0xFF) << 48)
             | ((ulong)(y3 & 0xFF) << 40)
             | ((ulong)(y4 & 0xFF) << 32)
             | ((ulong)(y5 & 0xFF) << 24)
             | ((ulong)(y6 & 0xFF) << 16)
             | ((ulong)(y7 & 0xFF) << 8)
             | (ulong)(y8 & 0xFF);
    }

    /// <summary>
    /// Applies the Camellia FL function to a 64-bit value using the supplied 64-bit subkey.
    /// </summary>
    /// <param name="x">The 64-bit input.</param>
    /// <param name="key">The 64-bit subkey.</param>
    /// <returns>The transformed 64-bit output.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Fl(ulong x, ulong key)
    {
        var x1 = (uint)(x >> 32);
        var x2 = (uint)x;
        var k1 = (uint)(key >> 32);
        var k2 = (uint)key;

        x2 ^= (x1 & k1).RotateBitsLeftUnchecked(1);
        x1 ^= x2 | k2;

        return ((ulong)x1 << 32) | x2;
    }

    /// <summary>
    /// Applies the inverse Camellia FL function (FL<sup>−1</sup>) to a 64-bit value using the supplied 64-bit subkey.
    /// </summary>
    /// <param name="x">The 64-bit input.</param>
    /// <param name="key">The 64-bit subkey.</param>
    /// <returns>The inverse-transformed 64-bit output.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FlInv(ulong x, ulong key)
    {
        var y1 = (uint)(x >> 32);
        var y2 = (uint)x;
        var k1 = (uint)(key >> 32);
        var k2 = (uint)key;

        y1 ^= y2 | k2;
        y2 ^= (y1 & k1).RotateBitsLeftUnchecked(1);

        return ((ulong)y1 << 32) | y2;
    }

    /// <summary>
    /// Rotates a 128-bit value left by the specified number of bits. The value is represented as a (hi, lo) pair of
    /// 64-bit words where <paramref name="hi"/> holds the most-significant bits.
    /// </summary>
    /// <param name="hi">The upper 64 bits of the 128-bit value.</param>
    /// <param name="lo">The lower 64 bits of the 128-bit value.</param>
    /// <param name="bits">The rotation count, masked to the range 0..127.</param>
    /// <returns>The rotated (hi, lo) pair.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong hi, ulong lo) RotL128(ulong hi, ulong lo, int bits)
    {
        bits &= 127;

        if (bits == 0)
            return (hi, lo);

        if (bits < 64)
            return ((hi << bits) | (lo >> (64 - bits)), (lo << bits) | (hi >> (64 - bits)));

        bits -= 64;
        if (bits == 0)
            return (lo, hi);

        return ((lo << bits) | (hi >> (64 - bits)), (hi << bits) | (lo >> (64 - bits)));
    }

    /// <summary>
    /// Applies the Camellia <c>SBOX2</c> derivation: <c>SBOX2[x] = SBOX1[x] &lt;&lt;&lt; 1</c>.
    /// </summary>
    /// <param name="x">The 8-bit input index (only the lower 8 bits are used).</param>
    /// <returns>The transformed byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SBox2(int x) => RotateLeft1(s_sbox1[x]);

    /// <summary>
    /// Applies the Camellia <c>SBOX3</c> derivation: <c>SBOX3[x] = SBOX1[x] &lt;&lt;&lt; 7</c> (equivalently, a single right rotation).
    /// </summary>
    /// <param name="x">The 8-bit input index (only the lower 8 bits are used).</param>
    /// <returns>The transformed byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SBox3(int x) => RotateRight1(s_sbox1[x]);

    /// <summary>
    /// Applies the Camellia <c>SBOX4</c> derivation: <c>SBOX4[x] = SBOX1[x &lt;&lt;&lt; 1]</c>.
    /// </summary>
    /// <param name="x">The 8-bit input index (only the lower 8 bits are used).</param>
    /// <returns>The transformed byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SBox4(int x) => s_sbox1[RotateLeft1(x)];

    /// <summary>
    /// Rotates an 8-bit value left by 1 bit.
    /// </summary>
    /// <param name="x">The 8-bit value to rotate (only the lower 8 bits are used).</param>
    /// <returns>The left-rotated byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RotateLeft1(int x) => ((x << 1) | (x >> 7)) & 0xFF;

    /// <summary>
    /// Rotates an 8-bit value right by 1 bit.
    /// </summary>
    /// <param name="x">The 8-bit value to rotate (only the lower 8 bits are used).</param>
    /// <returns>The right-rotated byte value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RotateRight1(int x) => ((x >> 1) | (x << 7)) & 0xFF;

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
