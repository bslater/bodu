// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Twofish.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Twofish symmetric block cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Twofish is a symmetric-key block cipher designed by Bruce Schneier, John Kelsey, Doug Whiting, David Wagner,
/// Chris Hall, and Niels Ferguson. It was one of the Advanced Encryption Standard finalists. Twofish operates on
/// 128-bit blocks and supports 128-bit, 192-bit, and 256-bit keys.
/// </para>
/// <para>
/// This class integrates with the .NET <see cref="SymmetricAlgorithm"/> framework and supports standard block
/// cipher modes via the <see cref="BlockMode"/> property. The default mode is <see cref="CipherBlockMode.CBC"/>
/// with <see cref="PaddingMode.PKCS7"/> padding.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 128 bits (16 bytes).</description></item>
///   <item><description>Key sizes: 128, 192, or 256 bits.</description></item>
///   <item><description>16-round Feistel structure with key-dependent S-boxes and an MDS-based linear layer.</description></item>
///   <item><description>Default mode: <see cref="CipherBlockMode.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Twofish.</strong> Pick Twofish when interoperability with existing Twofish-based code
/// or formats is required, or when you want an AES finalist with strong software performance and a different
/// design philosophy from AES. For new general-purpose work, <see cref="System.Security.Cryptography.Aes"/> is
/// the right default — hardware acceleration on most modern CPUs makes it the fastest option as well as the most
/// widely vetted. Reach for <see cref="Serpent128"/> when you specifically want the higher round count
/// conservatism of that AES finalist.
/// </para>
/// <note type="important">
/// For new general-purpose application encryption, prefer <see cref="Aes"/> unless Twofish compatibility is
/// specifically required.
/// </note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var twofish = new Twofish();
/// twofish.GenerateKey(); // 256-bit by default
/// twofish.GenerateIV();
///
/// byte[] ciphertext = twofish.Encrypt(plaintext);
/// byte[] roundTrip  = twofish.Decrypt(ciphertext);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/twofish.html">Using Twofish (guide with full encrypt / decrypt examples)</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Twofish
    : SymmetricAlgorithm
{
    /// <summary>
    /// The Twofish block size, in bits.
    /// </summary>
    internal const int BlockSizeBits = 128;

    /// <summary>
    /// The minimum permitted key size, in bytes.
    /// </summary>
    internal const int MinKeySizeBytes = 16;

    /// <summary>
    /// The maximum permitted key size, in bytes.
    /// </summary>
    internal const int MaxKeySizeBytes = 32;

    // Twofish has a single fixed 128-bit block size.
    private static readonly KeySizes[] s_twofishBlockSizes = [new KeySizes(BlockSizeBits, BlockSizeBits, 0)];

    // Legal key sizes are 128, 192, and 256 bits.
    private static readonly KeySizes[] s_twofishKeySizes = [new KeySizes(128, 256, 64)];

    private bool _disposed;
    private CipherBlockMode _blockMode = CipherBlockMode.CBC;
    private BlockPaddingMode _blockPadding = BlockPaddingMode.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Twofish"/> class with default parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default configuration uses a 128-bit block, a 256-bit key, CBC cipher mode, and PKCS7 padding.
    /// </para>
    /// </remarks>
    public Twofish()
    {
        this.BlockSizeValue = BlockSizeBits;
        this.LegalBlockSizesValue = s_twofishBlockSizes;

        this.KeySizeValue = 256;
        this.LegalKeySizesValue = s_twofishKeySizes;

        this.FeedbackSizeValue = BlockSizeBits;
        this.ModeValue = CipherMode.CBC;
        this.PaddingValue = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherBlockMode"/> values. The default is <see cref="CipherBlockMode.CBC"/>.
    /// </value>
    public CipherBlockMode BlockMode
    {
        get => this._blockMode;
        set
        {
            this._blockMode = value;

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
    /// One of the <see cref="BlockPaddingMode"/> values. The default is <see cref="BlockPaddingMode.PKCS7"/>.
    /// </value>
    /// <remarks>
    /// When the assigned value has a matching member in <see cref="PaddingMode"/> (for example, PKCS7, Zeros,
    /// None), the inherited <see cref="SymmetricAlgorithm.Padding"/> is kept in sync. Extended modes with no
    /// <see cref="PaddingMode"/> equivalent (such as <see cref="BlockPaddingMode.ISO7816_4"/>) leave the base
    /// property unchanged.
    /// </remarks>
    public BlockPaddingMode BlockPadding
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
    /// <see cref="BlockPaddingMode"/>.
    /// </remarks>
    public override PaddingMode Padding
    {
        get => base.Padding;
        set
        {
            base.Padding = value;
            if (Enum.TryParse<BlockPaddingMode>(value.ToString(), out BlockPaddingMode bpm) && Enum.IsDefined(bpm))
                this._blockPadding = bpm;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Twofish"/> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Twofish"/> instance.</returns>
    public new static Twofish Create() => new Twofish();

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new TwofishTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new TwofishTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, true);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeBytes);
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(this.Key);
                CryptoHelpers.Clear(this.IVValue!);
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
    }

    private int BlockSizeBytes => this.BlockSizeValue / 8;

    private int KeySizeBytes => this.KeySizeValue / 8;

    private static IBlockCipher CreateCipher(byte[] key) => new TwofishBlockCipher(key);

    private void Validate(byte[] key, byte[]? iv)
    {
        CryptoHelpers.ThrowIfInvalidKeySize(key, this.KeySizeBytes, this.LegalKeySizesValue);
        CryptoHelpers.ThrowIfInvalidIVForMode(iv, this.BlockMode, this.BlockSizeBytes, this.LegalBlockSizesValue);
    }

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
