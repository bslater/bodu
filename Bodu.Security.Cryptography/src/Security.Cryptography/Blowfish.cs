// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blowfish.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Blowfish symmetric block cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Blowfish is a symmetric-key block cipher designed by Bruce Schneier in 1993. It operates on 64-bit (8-byte) blocks and accepts a
/// variable-length key of between 32 and 448 bits (4 to 56 bytes). The cipher applies a 16-round Feistel network using four 256-entry
/// S-boxes and an 18-entry P-array, all initialised from the hexadecimal digits of pi (π). The key schedule is computationally
/// intensive by design, making brute-force attacks significantly more expensive.
/// </para>
/// <para>
/// This class integrates with the .NET <see cref="SymmetricAlgorithm"/> framework and supports standard block cipher modes via the
/// <see cref="BlockMode"/> property. The default mode is <see cref="CipherModeKind.CBC"/> with
/// <see cref="PaddingMode.PKCS7"/> padding.
/// </para>
/// <para>
/// For further details on the algorithm, see
/// <a href="https://www.schneier.com/academic/blowfish/">https://www.schneier.com/academic/blowfish/</a>.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 64 bits (8 bytes).</description></item>
///   <item><description>Key size: variable, 32–448 bits (4–56 bytes).</description></item>
///   <item><description>16-round Feistel network with key-dependent S-boxes initialised from the digits of π.</description></item>
///   <item><description>Default mode: <see cref="CipherModeKind.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Blowfish.</strong> Pick Blowfish only for interoperability with legacy systems that
/// already use it — its 64-bit block size invites SWEET32 birthday-bound attacks once roughly 32 GiB has been
/// encrypted under one key. For new designs use <see cref="System.Security.Cryptography.Aes"/>; bcrypt-style
/// password hashing schemes that derive from Blowfish are not in scope of this class.
/// </para>
/// <note type="important">
/// Blowfish has a 64-bit block size, which makes it vulnerable to birthday-bound attacks (SWEET32) when large volumes of data are
/// encrypted under the same key. For new applications, a cipher with a 128-bit or larger block size (such as AES) should be preferred.
/// </note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// // Legacy interop only.
/// using var blowfish = new Blowfish();
/// blowfish.Key = legacyKeyMaterial;             // 4–56 bytes
/// blowfish.IV  = RandomNumberGenerator.GetBytes(8); // matches the 64-bit block
///
/// byte[] ciphertext = blowfish.Encrypt(legacyPlaintext);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/blowfish.html">Using Blowfish (guide with full encrypt / decrypt examples)</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Blowfish
    : SymmetricAlgorithm
{
    /// <summary>
    /// Length of the Blowfish block is 64 bits (8 bytes).
    /// </summary>
    internal const int BlowFishBlockSize = 64;

    /// <summary>
    /// Length of the minimum permitted Blowfish key is 32 bits (4 bytes).
    /// </summary>
    internal const int MinKeySize = 32;

    /// <summary>
    /// Length of the maximum permitted Blowfish key is 448 bits (56 bytes).
    /// </summary>
    internal const int MaxKeySize = 448;

    // Blowfish has a single fixed 64-bit block size; expressed as a single-entry range with skip size 0.
    private static readonly KeySizes[] s_blowfishBlockSizes = [new KeySizes(BlowFishBlockSize, BlowFishBlockSize, 0)];

    // Legal key sizes span 32..448 bits in 8-bit (single-byte) increments.
    private static readonly KeySizes[] s_blowfishKeySizes = [new KeySizes(MinKeySize, MaxKeySize , 8)];

    private bool _disposed = false;

    private CipherModeKind _blockMode = CipherModeKind.CBC;

    private PaddingModeKind _blockPadding = PaddingModeKind.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Blowfish"/> class with default parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default configuration uses a 64-bit block, a 128-bit (16-byte) key, CBC cipher mode, and PKCS7 padding. Call
    /// <see cref="SymmetricAlgorithm.GenerateKey"/> and <see cref="SymmetricAlgorithm.GenerateIV"/> to produce random key material,
    /// or assign <see cref="SymmetricAlgorithm.Key"/> and <see cref="SymmetricAlgorithm.IV"/> directly before calling
    /// <see cref="CreateEncryptor(byte[], byte[])"/> or <see cref="CreateDecryptor(byte[], byte[])"/>.
    /// </para>
    /// </remarks>
    public Blowfish()
    {
        // Fixed 64-bit block — Blowfish does not support any other block size.
        this.BlockSizeValue = BlowFishBlockSize;
        this.LegalBlockSizesValue = s_blowfishBlockSizes;

        // Default to a 128-bit key, which sits in the middle of the permitted 32..448-bit range and matches common
        // expectations for modern symmetric usage.
        this.KeySizeValue = 128;
        this.LegalKeySizesValue = s_blowfishKeySizes;

        this.FeedbackSizeValue = BlockSize;
        this.ModeValue = CipherMode.CBC;
        this.PaddingValue = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherModeKind"/> values. The default is <see cref="CipherModeKind.CBC"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode"/> property for use with
    /// <see cref="BlockCipherModeFactory"/> and the extended set of modes it supports, including
    /// <see cref="CipherModeKind.CTR"/> and <see cref="CipherModeKind.OFB"/>, which are not available via the standard
    /// <see cref="CipherMode"/> enumeration.
    /// </para>
    /// <para>
    /// When the assigned value has a matching member in <see cref="CipherMode"/> (for example, CBC, ECB, CFB), the inherited
    /// <see cref="SymmetricAlgorithm.Mode"/> is kept in sync so that consumers inspecting the base property observe a consistent
    /// mode. Extended modes with no <see cref="CipherMode"/> equivalent leave the base property unchanged.
    /// </para>
    /// </remarks>
    public CipherModeKind BlockMode
    {
        get => this._blockMode;
        set
        {
            this._blockMode = value;

            // Keep the inherited Mode in sync where a direct CipherMode equivalent exists. Extended modes without a
            // mapping (e.g. CTR, OFB) intentionally leave ModeValue untouched.
            if (Enum.TryParse<CipherMode>(value.ToString(), out CipherMode mode) &&
                Enum.IsDefined(mode))
            {
                this.ModeValue = mode;
            }
        }
    }

    /// <summary>
    /// Gets or sets the extended padding mode used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="PaddingModeKind"/> values. The default is <see cref="PaddingModeKind.PKCS7"/>.
    /// </value>
    /// <remarks>
    /// When the assigned value has a matching member in <see cref="PaddingMode"/> (for example, PKCS7, Zeros,
    /// None), the inherited <see cref="SymmetricAlgorithm.Padding"/> is kept in sync. Extended modes with no
    /// <see cref="PaddingMode"/> equivalent (such as <see cref="PaddingModeKind.ISO7816_4"/>) leave the base
    /// property unchanged.
    /// </remarks>
    public PaddingModeKind BlockPadding
    {
        get => this._blockPadding;
        set
        {
            this._blockPadding = value;
            if (Enum.TryParse<PaddingMode>(value.ToString(), out PaddingMode mode) && Enum.IsDefined(mode))
                this.PaddingValue = mode;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Also synchronises <see cref="BlockPadding"/> when the assigned value has a matching member in
    /// <see cref="PaddingModeKind"/>.
    /// </remarks>
    public override PaddingMode Padding
    {
        get => base.Padding;
        set
        {
            base.Padding = value;
            if (Enum.TryParse<PaddingModeKind>(value.ToString(), out PaddingModeKind bpm) && Enum.IsDefined(bpm))
                this._blockPadding = bpm;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Blowfish"/> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Blowfish"/> instance.</returns>
    public new static Blowfish Create() => new Blowfish();

    /// <summary>
    /// Creates a symmetric <see cref="Blowfish"/> decryptor using the specified key and initialisation vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be between <see cref="MinKeySize"/> and
    /// <see cref="MaxKeySize"/> bytes in length. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="rgbIV">
    /// The initialisation vector. Must be exactly 8 bytes (64 bits) in length and must not be <see langword="null"/> for any
    /// cipher mode other than ECB.
    /// </param>
    /// <returns>A symmetric <see cref="Blowfish"/> decryptor object implementing <see cref="ICryptoTransform"/>.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey"/> or <paramref name="rgbIV"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey"/> is not within the permitted key size range, or <paramref name="rgbIV"/> has an invalid length for
    /// the configured <see cref="BlockMode"/>.
    /// </exception>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySizeValue, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSizeValue, this.LegalBlockSizes);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new BlowfishTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, false);
    }

    /// <summary>
    /// Creates a symmetric <see cref="Blowfish"/> encryptor using the specified key and initialisation vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be between <see cref="MinKeySize"/> and
    /// <see cref="MaxKeySize"/> bytes in length. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="rgbIV">
    /// The initialisation vector. Must be exactly 8 bytes (64 bits) in length and must not be <see langword="null"/> for any
    /// cipher mode other than ECB.
    /// </param>
    /// <returns>A symmetric <see cref="Blowfish"/> encryptor object implementing <see cref="ICryptoTransform"/>.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey"/> or <paramref name="rgbIV"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey"/> is not within the permitted key size range, or <paramref name="rgbIV"/> has an invalid length for
    /// the configured <see cref="BlockMode"/>.
    /// </exception>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySizeValue, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSizeValue, this.LegalBlockSizes);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new BlowfishTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, true);
    }

    /// <summary>
    /// Generates a cryptographically random initialisation vector (<see cref="SymmetricAlgorithm.IV"/>) suitable for use with the
    /// Blowfish algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated IV length is determined by the algorithm block size.
    /// </para>
    /// <para>
    /// The generated IV is assigned to <see cref="SymmetricAlgorithm.IV"/>.
    /// </para>
    /// <para>
    /// A new IV should be generated for each independent encryption operation when reusing a <see cref="Blowfish"/> instance with
    /// the same key.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeValue / 8);
    }

    /// <summary>
    /// Generates a cryptographically random key for use with the Blowfish algorithm using the currently configured
    /// <see cref="SymmetricAlgorithm.KeySize"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated key length is determined by <see cref="SymmetricAlgorithm.KeySize"/>.
    /// </para>
    /// <para>
    /// The generated key is assigned to <see cref="SymmetricAlgorithm.Key"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeValue / 8);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Blowfish"/> instance and, optionally, the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged
    /// resources.
    /// </param>
    /// <remarks>
    /// Clears any cached key material and initialisation vector before delegating to the base implementation. This method is
    /// idempotent and safe to call multiple times.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                // Zero sensitive key material and IV buffers so their contents do not linger in managed memory after disposal.
                CryptoHelpers.Clear(this.Key);
                CryptoHelpers.Clear(this.IVValue);
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Creates a new <see cref="BlowfishBlockCipher"/> engine initialised with the supplied key.
    /// </summary>
    /// <param name="key">The key material used to derive the P-array and S-boxes.</param>
    /// <returns>An <see cref="IBlockCipher"/> configured for single-block encryption and decryption.</returns>
    private static IBlockCipher CreateCipher(byte[] key) => new BlowfishBlockCipher(key);

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
