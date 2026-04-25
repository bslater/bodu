// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Camellia.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Camellia symmetric block cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Camellia is a symmetric-key block cipher with a 128-bit block size and support for 128-bit, 192-bit, and
/// 256-bit keys. It was jointly developed by NTT and Mitsubishi Electric and is specified by RFC 3713.
/// </para>
/// <para>
/// This class integrates with the .NET <see cref="SymmetricAlgorithm" /> framework and supports standard block
/// cipher modes via the <see cref="BlockMode" /> property. The default mode is <see cref="CipherBlockMode.CBC" />
/// with <see cref="PaddingMode.PKCS7" /> padding.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/camellia.html">Using Camellia</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Camellia
    : SymmetricAlgorithm
{
    /// <summary>
    /// The Camellia block size, in bits.
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

    private static readonly KeySizes[] CamelliaBlockSizes = { new KeySizes(BlockSizeBits, BlockSizeBits, 0) };

    private static readonly KeySizes[] CamelliaKeySizes = { new KeySizes(128, 256, 64) };

    private bool _disposed;
    private CipherBlockMode _blockMode = CipherBlockMode.CBC;

    /// <summary>
    /// Initializes a new instance of the <see cref="Camellia" /> class with default parameters.
    /// </summary>
    public Camellia()
    {
        this.BlockSizeValue = BlockSizeBits;
        this.LegalBlockSizesValue = CamelliaBlockSizes;

        this.KeySizeValue = 256;
        this.LegalKeySizesValue = CamelliaKeySizes;

        this.FeedbackSizeValue = BlockSizeBits;
        this.ModeValue = CipherMode.CBC;
        this.PaddingValue = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    public CipherBlockMode BlockMode
    {
        get => this._blockMode;
        set
        {
            this._blockMode = value;

            if (Enum.TryParse<CipherMode>(value.ToString(), out var mode) &&
                Enum.IsDefined(mode))
            {
                this.ModeValue = mode;
            }
        }
    }

    /// <summary>
    /// Creates a new <see cref="Camellia" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Camellia" /> instance.</returns>
    public new static Camellia Create() => new Camellia();

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new CamelliaTransform(engine, this.BlockMode, this.Padding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new CamelliaTransform(engine, this.BlockMode, this.Padding, rgbIV, true);
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

    private static IBlockCipher CreateCipher(byte[] key) => new CamelliaBlockCipher(key);

    private void Validate(byte[] key, byte[] iv)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(iv);

        if (key.Length != this.KeySizeBytes)
            throw new CryptographicException(
                string.Format(ResourceStrings.CryptographicException_InvalidKeySize,
                              key.Length * 8,
                              CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)),
                nameof(key));

        if (iv.Length != this.BlockSizeBytes)
            throw new CryptographicException(
                string.Format(ResourceStrings.CryptographicException_InvalidIVSize,
                              iv.Length * 8,
                              CryptoHelpers.FormatLegalSizes(this.LegalBlockSizesValue)),
                nameof(iv));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(nameof(Camellia));
#endif
    }
}