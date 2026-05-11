// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for the non-standard wide-block tweakable Serpent variants (<see cref="Serpent256"/>,
/// <see cref="Serpent512"/>, and <see cref="Serpent1024"/>).
/// </summary>
/// <remarks>
/// <para>
/// Each variant accepts a key whose size in bits matches its block size (256, 512, or 1024 bits) together with a 128-bit
/// tweak. Derived classes must implement <see cref="CreateCipher(byte[], byte[])"/> to instantiate the appropriate concrete
/// engine.
/// </para>
/// <para>
/// The <see cref="BlockMode"/> property replaces the standard <see cref="SymmetricAlgorithm.Mode"/> property, enabling the
/// use of additional or non-standard block cipher modes such as <see cref="CipherBlockMode.CTR"/> and
/// <see cref="CipherBlockMode.OFB"/>.
/// </para>
/// <note type="important">
/// The wide-block Serpent family is a **non-standard, experimental construction** and is not interoperable with any reference
/// Serpent implementation. For standard, externally vetted Serpent, use <see cref="Serpent128"/>.
/// </note>
/// </remarks>
public abstract class Serpent
    : TweakableSymmetricAlgorithm
{
    /// <summary>
    /// The block size in bytes.
    /// </summary>
    protected readonly int BlockSizeBytes;

    /// <summary>
    /// The key size in bytes.
    /// </summary>
    protected readonly int KeySizeBytes;

    private readonly int _defaultTweakSizeBytes;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent"/> class with the specified block and tweak sizes.
    /// </summary>
    /// <param name="blockSizeBits">The block size in bits. Must match the wide-block Serpent variant block size (256, 512, or 1024).</param>
    /// <param name="tweakSizeBits">The tweak size in bits (128 for all wide-block Serpent variants).</param>
    protected Serpent(int blockSizeBits, int tweakSizeBits)
    {
        this.BlockSizeValue = this.KeySizeValue = blockSizeBits;
        this.FeedbackSizeValue = 8;

        this.BlockSizeBytes = this.KeySizeBytes = blockSizeBits / 8;
        this._defaultTweakSizeBytes = tweakSizeBits / 8;

        this.LegalBlockSizesValue = new[] { new KeySizes(blockSizeBits, blockSizeBits, 0) };
        this.LegalKeySizesValue = new[] { new KeySizes(blockSizeBits, blockSizeBits, 0) };
        this.LegalTweakSizesValue = new[] { new KeySizes(tweakSizeBits, tweakSizeBits, 0) };
        this.TweakSizeValue = tweakSizeBits;

        this.ModeValue = CipherMode.CBC;
        this.Padding = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>One of the <see cref="CipherBlockMode"/> values. The default is <see cref="CipherBlockMode.CBC"/>.</value>
    /// <remarks>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode"/> property when used with
    /// <see cref="BlockCipherModeFactory"/> and the extended set of modes it supports, including
    /// <see cref="CipherBlockMode.CTR"/> and <see cref="CipherBlockMode.OFB"/>.
    /// </remarks>
    public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV, tweak);

        var engine = this.CreateCipher(rgbKey, tweak);
        return new SerpentTransform(engine, this.BlockMode, this.Padding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV, tweak);

        var engine = this.CreateCipher(rgbKey, tweak);
        return new SerpentTransform(engine, this.BlockMode, this.Padding, rgbIV, true);
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
    public override void GenerateTweak()
    {
        this.ThrowIfDisposed();
        this.TweakValue = CryptoHelpers.GetRandomNonZeroBytes(this._defaultTweakSizeBytes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Marks the instance as disposed, zeroes any retained key and IV buffers, and delegates the tweak buffer cleanup to the
    /// base implementation.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(this.KeyValue);
                CryptoHelpers.Clear(this.IVValue);
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> whose <see cref="ObjectDisposedException.ObjectName"/> matches the
    /// concrete algorithm type's <see cref="Type.FullName"/> if the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
    }

    /// <summary>
    /// Instantiates the concrete Serpent block cipher with the specified key and tweak.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="tweak">The tweak value.</param>
    /// <returns>A configured <see cref="IBlockCipher"/> instance for encryption or decryption.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    protected abstract IBlockCipher CreateCipher(byte[] key, byte[] tweak);

    /// <summary>
    /// Validates the provided key, IV, and tweak against expected lengths and legal sizes.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="iv">The initialisation vector.</param>
    /// <param name="tweak">The tweak value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/>, <paramref name="iv"/>, or <paramref name="tweak"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown when any input does not match the required length.</exception>
    protected void Validate(byte[] key, byte[] iv, byte[] tweak)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(iv);
        ThrowHelper.ThrowIfNull(tweak);

        if (key.Length != this.KeySizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)));

        if (iv.Length != this.BlockSizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidIVSize, iv.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalBlockSizes)));

        if (tweak.Length != this._defaultTweakSizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidTweakSize, tweak.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalTweakSizes)));
    }
}
