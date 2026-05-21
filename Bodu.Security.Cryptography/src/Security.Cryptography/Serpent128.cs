// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the canonical <c>Serpent</c> symmetric block cipher, which operates on 128-bit
/// (16-byte) blocks using a 128, 192, or 256-bit key. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Serpent is an Advanced Encryption Standard (AES) finalist by Ross Anderson, Eli Biham, and Lars Knudsen, first
/// published in 1998 (NESSIE submission, <em>Serpent: A Proposal for the Advanced Encryption Standard</em>). It is a
/// 32-round substitution–permutation network that combines eight 4-bit S-boxes with a bitsliced linear transform, and
/// is widely regarded as one of the most conservative designs among the AES finalists.
/// </para>
/// <para>
/// This class integrates with the standard <see cref="SymmetricAlgorithm" /> framework and supports the extended
/// block-cipher modes exposed by <see cref="CipherModeKind" />. The default configuration uses
/// <see cref="CipherModeKind.CBC" /> with <see cref="PaddingMode.PKCS7" />.
/// </para>
/// <para>
/// For larger block sizes with tweak support, see <see cref="Serpent256" />, <see cref="Serpent512" />, and
/// <see cref="Serpent1024" />. Those variants are a non-standard Serpent-derived construction; the type on this page is
/// the canonical, externally vetted 128-bit cipher.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Block size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Key sizes: 128, 192, or 256 bits.
/// </description>
/// </item>
/// <item>
/// <description>
/// Default mode: <see cref="CipherModeKind.CBC" />; default padding: <see cref="PaddingMode.PKCS7" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// 32 rounds, 8 × 4-bit S-boxes, bitsliced linear transform.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Serpent128.</strong> Pick Serpent when a deliberately conservative AES alternative is wanted
/// — the 32-round design has a wider security margin than AES's 14 rounds at 256-bit key, at the cost of noticeably
/// lower throughput. For general-purpose encryption with hardware acceleration prefer
/// <see cref="System.Security.Cryptography.Aes" />; for an alternative AES finalist with better software performance
/// prefer <see cref="Twofish" />. Use <see cref="Camellia" /> when ISO/IEC, CRYPTREC, or NESSIE approval is a
/// procurement requirement.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var serpent = new Serpent128();
/// serpent.GenerateKey(); // 256-bit by default
/// serpent.GenerateIV();
/// byte[] ciphertext = serpent.Encrypt(plaintext);
/// byte[] roundTrip = serpent.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://www.cl.cam.ac.uk/~rja14/serpent.html">Serpent home page (Anderson / Biham / Knudsen)
/// </seealso> <seealso href="https://www.cl.cam.ac.uk/~rja14/Papers/serpent.pdf">Serpent: A Proposal for the Advanced
/// Encryption Standard (1998)</seealso> <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics
/// </seealso> <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Serpent128
    : SymmetricAlgorithm
{
    /// <summary>
    /// Length of the Serpent block is 128 bits (16 bytes).
    /// </summary>
    internal const int BlockSizeBits = 128;

    private static readonly KeySizes[] s_serpentBlockSizes = [new KeySizes(BlockSizeBits, BlockSizeBits, 0)];

    // Serpent permits 128-, 192-, or 256-bit keys (the three AES key sizes). The step is 64 bits so the range
    // is expressed exactly by a single KeySizes entry.
    private static readonly KeySizes[] s_serpentKeySizes = [new KeySizes(128, 256, 64)];

    private bool _disposed;

    private CipherModeKind _blockMode = CipherModeKind.CBC;

    private PaddingModeKind _blockPadding = PaddingModeKind.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent128" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 128-bit block, a 128-bit key, CBC cipher mode, and PKCS7 padding. Call
    /// <see cref="SymmetricAlgorithm.GenerateKey" /> and <see cref="SymmetricAlgorithm.GenerateIV" /> to produce random
    /// key material, or assign <see cref="SymmetricAlgorithm.Key" /> and <see cref="SymmetricAlgorithm.IV" /> directly
    /// before calling <see cref="CreateEncryptor(byte[], byte[])" /> or <see cref="CreateDecryptor(byte[], byte[])" />.
    /// </remarks>
    public Serpent128()
    {
        this.BlockSizeValue = BlockSizeBits;
        this.LegalBlockSizesValue = s_serpentBlockSizes;

        this.KeySizeValue = 128;
        this.LegalKeySizesValue = s_serpentKeySizes;

        this.FeedbackSizeValue = 8;
        this.ModeValue = CipherMode.CBC;
        this.PaddingValue = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherModeKind" /> values. The default is <see cref="CipherModeKind.CBC" />.
    /// </value>
    /// <remarks>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property for use with
    /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports, including
    /// <see cref="CipherModeKind.CTR" /> and <see cref="CipherModeKind.OFB" />.
    /// </remarks>
    public CipherModeKind BlockMode
    {
        get => this._blockMode;
        set
        {
            this._blockMode = value;

            if (Enum.TryParse<CipherMode>(value.ToString(), out CipherMode mode) && Enum.IsDefined(mode))
                this.ModeValue = mode;
        }
    }

    /// <summary>
    /// Gets or sets the extended padding mode used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="PaddingModeKind" /> values. The default is <see cref="PaddingModeKind.PKCS7" />.
    /// </value>
    /// <remarks>
    /// When the assigned value has a matching member in <see cref="PaddingMode" /> (for example, PKCS7, Zeros, None),
    /// the inherited <see cref="SymmetricAlgorithm.Padding" /> is kept in sync. Extended modes with no
    /// <see cref="PaddingMode" /> equivalent (such as <see cref="PaddingModeKind.ISO7816_4" />) leave the base property
    /// unchanged.
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
    /// Also synchronizes <see cref="BlockPadding" /> when the assigned value has a matching member in
    /// <see cref="PaddingModeKind" />.
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
    /// Creates a new <see cref="Serpent128" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Serpent128" /> instance.</returns>
    public static new Serpent128 Create() => new();

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySize, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSize, this.LegalBlockSizes);

        IBlockCipher engine = new Serpent128Cipher(rgbKey);
        return new Serpent128Transform(engine, this.BlockMode, this.BlockPadding, rgbIV!, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySize, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSize, this.LegalBlockSizes);

        IBlockCipher engine = new Serpent128Cipher(rgbKey);
        return new Serpent128Transform(engine, this.BlockMode, this.BlockPadding, rgbIV!, true);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeBits / 8);
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeValue / 8);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                if (this.KeyValue is not null) CryptoHelpers.Clear(this.KeyValue);
                if (this.IVValue is not null) CryptoHelpers.Clear(this.IVValue);
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif

}
