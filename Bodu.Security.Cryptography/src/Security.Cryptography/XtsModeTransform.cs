// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies XEX-based Tweaked CodeBook mode with ciphertext Stealing (XTS) to an underlying pair of
/// <see cref="IBlockCipher" /> instances, per IEEE Std 1619-2007 / NIST SP 800-38E.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/xts-mode.svg" alt="XTS data flow - the tweak cipher encrypts the sector number, successive α multiplications in GF(2^128) derive per-block tweaks T_j, and each block is XORed with T_j before and after the data cipher."/>
/// </para>
/// <para>
/// XTS requires two independent ciphers keyed with different material:
/// <list type="bullet">
/// <item>
/// <description>
/// <c>dataCipher</c> (Key₁) - encrypts or decrypts the data. Shown as <b>E_K₁</b> in the central column of the diagram.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>tweakCipher</c> (Key₂) - encrypts the sector number (tweak). Shown as <b>E_K₂</b> on the left.
/// </description>
/// </item>
/// </list>
/// Using the same key for both reduces XTS to a single-key construction and weakens security. Because
/// <see cref="IBlockCipher" /> exposes no key material, this type cannot detect Key₁ == Key₂; ensuring the two ciphers
/// are independently keyed is the caller's responsibility.
/// </para>
/// <para>
/// <strong>Implementation scope.</strong> This transform implements the XEX core for whole 128-bit blocks and does
/// <strong>not</strong> perform ciphertext stealing: input whose length is not a multiple of the block size is rejected
/// rather than stolen, so it is not interoperable with IEEE 1619 data units that end on a partial block. The GF(2<sup>128</sup>)
/// tweak reduction is defined only for 128-bit blocks, so both ciphers must have a 128-bit block size - the constructor
/// rejects any other width.
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
/// inside each cell - before and after the data cipher - realize the <c>⊕ T_j</c> pairs in the equation above.
/// </para>
/// <para>
/// GF(2^128) multiplication uses the primitive polynomial x^128 + x^7 + x^2 + x + 1 with little-endian bit
/// representation (byte 0, bit 0 = coefficient of x^0), identical to IEEE 1619.
/// </para>
/// <para>
/// <strong>When to use XTS.</strong> The standard mode for sector-level disk encryption - used by BitLocker, FileVault,
/// dm-crypt/LUKS, VeraCrypt, and the IEEE 1619 disk-encryption specification. XTS is designed specifically for the
/// random-access, fixed-size-block setting where ciphertext expansion is impossible (the on-disk sector size cannot
/// grow), which means it provides confidentiality but <em>no authentication</em>. Do not use XTS for protecting
/// messages over untrusted channels - pick an AEAD mode (<see cref="GcmModeTransform" />,
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
/// // XTS uses two independent keys - Key1 for data, Key2 for the tweak. Never share keys.
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
    /// <summary>The XTS block size, in bytes.</summary>
    private const int BlockBytes = 16;

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
        if (dataCipher.BlockSize != 128)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_XtsBlockSize, dataCipher.BlockSize),
                nameof(dataCipher));
        }

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
    /// <remarks>
    /// The tweaks for a run of up to 4 KiB are computed first, then the run is XORed with them, encrypted or decrypted
    /// with one multi-block call, and XORed with them again; the output is identical to transforming a block at a time.
    /// <paramref name="output" /> may be the same memory as <paramref name="input" />. The tweak and scratch buffers
    /// are cleared before the call returns.
    /// </remarks>
    [SkipLocalsInit]
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        int blockSize = _cipher.BlockSize / 8;

        // Empty input is a no-op, consistent with CbcModeTransform.
        CryptographyThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize, throwIfZero: false);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);
        CryptographyThrowHelper.ThrowIfInvalidOverlap(input, output);

        Span<byte> tweaks = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> work = stackalloc byte[CounterKeystream.BatchBytes];
        Span<byte> initialTweak = stackalloc byte[BlockBytes];
        int used = 0;

        try
        {
            // T_0 = tweakCipher.Encrypt(sector_number)
            _tweakCipher.Encrypt(_tweak, initialTweak);
            ulong low = BinaryPrimitives.ReadUInt64LittleEndian(initialTweak);
            ulong high = BinaryPrimitives.ReadUInt64LittleEndian(initialTweak.Slice(8));

            int offset = 0;
            while (offset < input.Length)
            {
                int length = Math.Min(tweaks.Length, input.Length - offset);
                for (int position = 0; position < length; position += BlockBytes)
                {
                    BinaryPrimitives.WriteUInt64LittleEndian(tweaks.Slice(position), low);
                    BinaryPrimitives.WriteUInt64LittleEndian(tweaks.Slice(position + 8), high);
                    MultiplyByAlpha(ref low, ref high);
                }

                // XEX: out = cipher(in XOR T) XOR T, a run at a time. The run is read into the scratch before any of its
                // output is written, so exact aliasing is safe.
                used = Math.Max(used, length);
                Span<byte> run = output.Slice(offset, length);
                CryptographyHelper.Xor(input.Slice(offset, length), tweaks[..length], work[..length]);
                if (encrypt)
                    _cipher.EncryptBlocks(work[..length], run);
                else
                    _cipher.DecryptBlocks(work[..length], run);

                CryptographyHelper.Xor(run, tweaks[..length], run);
                offset += length;
            }

            return input.Length;
        }
        finally
        {
            CryptographyHelper.Clear(initialTweak);
            CryptographyHelper.Clear(tweaks[..used]);
            CryptographyHelper.Clear(work[..used]);
        }
    }

    /// <summary>
    /// Releases the resources used by this instance and zeroes the retained tweak so that key-equivalent state does not
    /// linger in memory after disposal. The underlying data and tweak <see cref="IBlockCipher" /> instances are not
    /// disposed by this type - ownership remains with the caller.
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
    /// Multiplies a tweak by <c>α</c> in <c>GF(2¹²⁸)</c>, in XTS's little-endian representation: a one-bit left shift
    /// of the 128-bit value, reduced by <c>0x87</c> (<c>x⁷ + x² + x + 1</c>) when the <c>x¹²⁷</c> coefficient shifts
    /// out.
    /// </summary>
    /// <param name="low">The tweak's low 64 bits; updated in place.</param>
    /// <param name="high">The tweak's high 64 bits; updated in place.</param>
    /// <remarks>
    /// The reduction is masked rather than branched on, so the cost does not depend on the tweak.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void MultiplyByAlpha(ref ulong low, ref ulong high)
    {
        ulong reduction = 0x87UL & (0UL - (high >> 63));
        high = (high << 1) | (low >> 63);
        low = (low << 1) ^ reduction;
    }
}
