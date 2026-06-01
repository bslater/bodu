// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Twofish.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
/// Twofish is a symmetric-key block cipher designed by Bruce Schneier, John Kelsey, Doug Whiting, David Wagner, Chris
/// Hall, and Niels Ferguson, and submitted as one of the five finalists in the Advanced Encryption Standard (AES)
/// competition. The reference paper, <em>Twofish: A 128-Bit Block Cipher</em> (1998), specifies a 16-round Feistel
/// network with key-dependent S-boxes, an MDS-based linear layer, and a Pseudo-Hadamard Transform providing diffusion
/// across the Feistel halves. Twofish operates on 128-bit blocks and supports 128-bit, 192-bit, and 256-bit keys.
/// </para>
/// <para>
/// This class integrates with the .NET <see cref="SymmetricAlgorithm" /> framework and supports standard block cipher
/// modes via the <see cref="BlockMode" /> property. The default mode is <see cref="CipherModeKind.CBC" /> with
/// <see cref="PaddingMode.PKCS7" /> padding.
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
/// 16-round Feistel structure with key-dependent S-boxes and an MDS-based linear layer.
/// </description>
/// </item>
/// <item>
/// <description>
/// Default mode: <see cref="CipherModeKind.CBC" />; default padding: <see cref="PaddingMode.PKCS7" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Twofish.</strong> Pick Twofish when interoperability with existing Twofish-based code or
/// formats is required, or when you want an AES finalist with strong software performance and a different design
/// philosophy from AES. For new general-purpose work, <see cref="System.Security.Cryptography.Aes" /> is the right
/// default — hardware acceleration on most modern CPUs makes it the fastest option as well as the most widely vetted.
/// Reach for <see cref="Serpent128" /> when you specifically want the higher round count conservatism of that AES
/// finalist.
/// </para>
/// <note type="important"> For new general-purpose application encryption, prefer <see cref="Aes" /> unless Twofish
/// compatibility is specifically required. </note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var twofish = new Twofish();
/// twofish.GenerateKey(); // 256-bit by default
/// twofish.GenerateIV();
/// byte[] ciphertext = twofish.Encrypt(plaintext);
/// byte[] roundTrip = twofish.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://www.schneier.com/wp-content/uploads/2016/02/paper-twofish-paper.pdf">Twofish: A 128-Bit Block
/// Cipher (Schneier, Kelsey, Whiting, Wagner, Hall, Ferguson, 1998)</seealso>
/// <seealso href="../guides/cryptography/twofish.html">Using Twofish (guide with full encrypt / decrypt examples)
/// </seealso> <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Twofish
    : SymmetricAlgorithm
{
    /// <summary>
    /// Length of the Twofish block is 128 bits (16 bytes).
    /// </summary>
    internal const int BlockSizeBits = 128;

    /// <summary>
    /// Length of the minimum permitted Twofish key is 128 bits (16 bytes).
    /// </summary>
    internal const int MinKeySize = 128;

    /// <summary>
    /// Length of the maximum permitted Twofish key is 256 bits (32 bytes).
    /// </summary>
    internal const int MaxKeySize = 256;

    // Twofish has a single fixed 128-bit block size.
    private static readonly KeySizes[] s_twofishBlockSizes = [new KeySizes(BlockSizeBits, BlockSizeBits, 0)];

    // Legal key sizes are 128, 192, and 256 bits.
    private static readonly KeySizes[] s_twofishKeySizes = [new KeySizes(128, 256, 64)];

    private bool _disposed;
    private CipherModeKind _blockMode = CipherModeKind.CBC;
    private PaddingModeKind _blockPadding = PaddingModeKind.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Twofish" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default configuration uses a 128-bit block, a 256-bit key, CBC cipher mode, and PKCS7 padding.
    /// </para>
    /// </remarks>
    public Twofish()
    {
        BlockSizeValue = BlockSizeBits;
        LegalBlockSizesValue = s_twofishBlockSizes;

        KeySizeValue = 256;
        LegalKeySizesValue = s_twofishKeySizes;

        FeedbackSizeValue = BlockSizeBits;
        ModeValue = CipherMode.CBC;
        PaddingValue = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherModeKind" /> values. The default is <see cref="CipherModeKind.CBC" />.
    /// </value>
    public CipherModeKind BlockMode
    {
        get => _blockMode;
        set
        {
            _blockMode = value;

            if (Enum.TryParse<CipherMode>(value.ToString(), out CipherMode mode) &&
                Enum.IsDefined(mode))
            {
                ModeValue = mode;
            }
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
        get => _blockPadding;
        set
        {
            _blockPadding = value;
            if (Enum.TryParse<PaddingMode>(value.ToString(), out PaddingMode mode) && Enum.IsDefined(mode))
                PaddingValue = mode;
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
                _blockPadding = bpm;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Twofish" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Twofish" /> instance.</returns>
    public static new Twofish Create() =>
        new();

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidKeySize(rgbKey, KeySize, LegalKeySizes);
        CryptographyThrowHelper.ThrowIfInvalidIVForMode(rgbIV, BlockMode, BlockSize, LegalBlockSizes);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new TwofishTransform(engine, BlockMode, BlockPadding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidKeySize(rgbKey, KeySize, LegalKeySizes);
        CryptographyThrowHelper.ThrowIfInvalidIVForMode(rgbIV, BlockMode, BlockSize, LegalBlockSizes);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new TwofishTransform(engine, BlockMode, BlockPadding, rgbIV, true);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        ThrowIfDisposed();
        IVValue = CryptographyHelper.GetRandomBytes(BlockSizeValue / 8);
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        ThrowIfDisposed();
        KeyValue = CryptographyHelper.GetRandomBytes(KeySizeValue / 8);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CryptographyHelper.Clear(Key);
                CryptographyHelper.Clear(IVValue!);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private static TwofishBlockCipher CreateCipher(byte[] key) =>
        new(key);

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif

}
