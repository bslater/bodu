// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies XEX-based Tweaked CodeBook mode with ciphertext Stealing (XTS) to an underlying pair of
/// <see cref="IBlockCipher" /> instances, per IEEE Std 1619-2007 / NIST SP 800-38E.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/xts-mode.svg" alt="XTS data flow — the tweak cipher encrypts the sector number, successive α multiplications in GF(2^128) derive per-block tweaks T_j, and each block is XORed with T_j before and after the data cipher."/>
/// </para>
/// <para>
/// XTS requires two independent ciphers keyed with different material:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>dataCipher</c> (Key₁) — encrypts or decrypts the data. Shown as <b>E_K₁</b> in the central column of the diagram.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>tweakCipher</c> (Key₂) — encrypts the sector number (tweak). Shown as <b>E_K₂</b> on the left.
/// </description>
/// </item>
/// </list>
/// Using the same key for both reduces XTS to a single-key construction and weakens security.
/// </para>
/// <para>
/// For each 128-bit block j in a sector, the XEX construction is:
/// <code>
///<![CDATA[
/// T_j  = α^j ⊗ tweakCipher.Encrypt(tweak)     // Galois field multiplication
/// C_j  = dataCipher.Encrypt(P_j ⊕ T_j) ⊕ T_j  // encrypt
/// P_j  = dataCipher.Decrypt(C_j ⊕ T_j) ⊕ T_j  // decrypt
///]]>
/// </code>
/// The horizontal tweak bus in the diagram corresponds to this successive <c>·α</c> multiplication: each <b>·α </b> box
/// doubles the tweak in GF(2¹²⁸) so the Tⱼ arriving at cell <em>j</em> is αʲ times the base tweak. The two XOR nodes
/// inside each cell — before and after the data cipher — realize the <c>⊕ T_j</c> pairs in the equation above.
/// </para>
/// <para>
/// GF(2^128) multiplication uses the primitive polynomial x^128 + x^7 + x^2 + x + 1 with little-endian bit
/// representation (byte 0, bit 0 = coefficient of x^0), identical to IEEE 1619.
/// </para>
/// <para>
/// <strong>When to use XTS.</strong> The standard mode for sector-level disk encryption — used by BitLocker, FileVault,
/// dm-crypt/LUKS, VeraCrypt, and the IEEE 1619 disk-encryption specification. XTS is designed specifically for the
/// random-access, fixed-size-block setting where ciphertext expansion is impossible (the on-disk sector size cannot
/// grow), which means it provides confidentiality but <em>no authentication</em>. Do not use XTS for protecting
/// messages over untrusted channels — pick an AEAD mode (<see cref="GcmModeTransform" />,
/// <see cref="EaxModeTransform" />) for that. For new disk encryption designs that can afford a per-sector tag,
/// AEAD-based alternatives (Adiantum, AES-XTS-HMAC, or storage-specific AEAD modes) provide stronger guarantees.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
///
/// // XTS uses two independent keys — Key1 for data, Key2 for the tweak. Never share keys.
/// using IBlockCipher data = new AesBlockCipher(key1);
/// using IBlockCipher tweak = new AesBlockCipher(key2);
/// byte[] sectorNumber = BitConverter.GetBytes((long)42); // little-endian sector number, padded to block size
/// Array.Resize(ref sectorNumber, data.BlockSize / 8);
/// IBlockCipherModeTransform xts = new XtsModeTransform(data, tweak, sectorNumber);
/// byte[] ciphertext = new byte[plaintext.Length];
/// int written = xts.Transform(plaintext, ciphertext, encrypt: true);
///]]>
/// </code>
/// </example>
public sealed class XtsModeTransform
    : IBlockCipherModeTransform
{
    /// <summary>The data cipher (Key₁) used to encrypt or decrypt data blocks.</summary>
    private readonly IBlockCipher _cipher;

    /// <summary>The tweak cipher (Key₂) used to encrypt the sector number.</summary>
    private readonly IBlockCipher _tweakCipher;

    /// <summary>The sector number (tweak value) stored as a defensive copy of the supplied block-size byte array.</summary>
    private readonly byte[] _tweak;

    /// <summary>Indicates whether the instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="XtsModeTransform" /> class.
    /// </summary>
    /// <param name="dataCipher">The cipher keyed with Key₁, used to encrypt or decrypt data blocks.</param>
    /// <param name="tweakCipher">
    /// The cipher keyed with Key₂, used to encrypt the sector number. Must have the same block size as
    /// <paramref name="dataCipher" />.
    /// </param>
    /// <param name="tweak">
    /// The sector number encoded as a block-size byte array in little-endian order. A defensive copy is taken.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Block sizes differ, or <paramref name="tweak" /> length does not equal the block size.
    /// </exception>
    public XtsModeTransform(IBlockCipher dataCipher, IBlockCipher tweakCipher, byte[] tweak)
    {
        _cipher = dataCipher ?? throw new ArgumentNullException(nameof(dataCipher));
        _tweakCipher = tweakCipher ?? throw new ArgumentNullException(nameof(tweakCipher));
        if (tweak is null) throw new ArgumentNullException(nameof(tweak));
        if (tweakCipher.BlockSize != dataCipher.BlockSize)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_XtsCipherBlockSizeMismatch, tweakCipher.BlockSize, dataCipher.BlockSize),
                nameof(tweakCipher));
        }

        if (tweak.Length != dataCipher.BlockSize / 8)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_XtsTweakLength, tweak.Length, dataCipher.BlockSize / 8),
                nameof(tweak));
        }

        _tweak = (byte[])tweak.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = _cipher.BlockSize / 8;

        // Empty input is a no-op, consistent with CbcModeTransform.
        CryptographyThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);
        CryptographyThrowHelper.ThrowIfInvalidOverlap(input, output);

        // T_0 = tweakCipher.Encrypt(sector_number)
        Span<byte> T = stackalloc byte[blockSize];
        _tweakCipher.Encrypt(_tweak, T);

        Span<byte> buf = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);

            // XEX: out = cipher(in XOR T) XOR T
            for (int i = 0; i < blockSize; i++) buf[i] = (byte)(inBlock[i] ^ T[i]);
            if (encrypt)
                _cipher.Encrypt(buf, outBlock);
            else
                _cipher.Decrypt(buf, outBlock);
            for (int i = 0; i < blockSize; i++) outBlock[i] ^= T[i];

            // Advance tweak: T = α ⊗ T in GF(2^128), little-endian, poly 0x87 reduction.
            GfDouble(T);
        }

        return input.Length;
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the retained tweak so that key-equivalent state does not
    /// linger in memory after disposal. The underlying data and tweak <see cref="IBlockCipher" /> instances are not
    /// disposed by this type — ownership remains with the caller.
    /// </summary>
    /// <remarks>
    /// Idempotent.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographyHelper.Clear(_tweak);
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Multiplies <paramref name="t1" /> by α in GF(2^128) using the IEEE 1619 polynomial x^128 + x^7 + x^2 + x + 1,
    /// with little-endian bit ordering (byte 0 = x^0..x^7).
    /// </summary>
    /// <param name="t1">The 16-byte tweak block to double in GF(2<sup>128</sup>); updated in place.</param>
    private static void GfDouble(Span<byte> t1)
    {
        // Left-shift the 128-bit little-endian value. Carry propagates from byte[0] upward.
        int carry = 0;
        for (int i = 0; i < t1.Length; i++)
        {
            int t = t1[i];
            t1[i] = (byte)((t << 1) | carry);
            carry = t >> 7;
        }

        // If the MSB of byte[15] was set (= x^127 coefficient), reduce by 0x87 (= x^7+x^2+x+1).
        if (carry != 0) t1[0] ^= 0x87;
    }
}
