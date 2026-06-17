// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for the non-standard wide-block tweakable Serpent variants (
/// <see cref="Serpent256" />, <see cref="Serpent512" />, and <see cref="Serpent1024" />).
/// </summary>
/// <remarks>
/// <para>
/// Each variant accepts a key whose size in bits matches its block size (256, 512, or 1024 bits) together with a
/// 128-bit tweak. Derived classes must implement <see cref="CreateCipher(byte[], byte[])" /> to instantiate the
/// appropriate concrete engine.
/// </para>
/// <para>
/// The <see cref="BlockMode" /> property replaces the standard <see cref="SymmetricAlgorithm.Mode" /> property,
/// enabling the use of additional or non-standard block cipher modes such as <see cref="CipherModeKind.CTR" /> and
/// <see cref="CipherModeKind.OFB" />.
/// </para>
/// <note type="important"> The wide-block Serpent family is a **non-standard, experimental construction** and is not
/// interoperable with any reference Serpent implementation. For standard, externally vetted Serpent, use
/// <see cref="Serpent128" />. </note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Use a concrete wide-block variant — Serpent-256 over a CTR mode.
/// using TweakableSymmetricAlgorithm alg = new Serpent256();
/// alg.GenerateKey();
/// alg.GenerateIV();
/// alg.GenerateTweak();
/// alg.BlockMode = CipherModeKind.CTR;
///
/// using ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV, alg.Tweak);
/// using var cipherText = new MemoryStream();
/// using (var cs = new CryptoStream(cipherText, encryptor, CryptoStreamMode.Write))
///     cs.Write(plaintext, 0, plaintext.Length);
///
/// // For standard, externally vetted Serpent use Serpent128 instead — the wide-block variants
/// // are experimental and not interoperable with reference Serpent implementations.
///]]>
/// </example>
public abstract class Serpent
    : TweakableSymmetricAlgorithm
{
    /// <summary>The block size in bytes.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Exposed as a protected field so derived wide-block Serpent types can read the block byte count directly on cipher-construction paths without virtual dispatch.")]
    protected readonly int BlockSizeBytes;

    /// <summary>The key size in bytes.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Exposed as a protected field so derived wide-block Serpent types can read the key byte count directly on cipher-construction paths without virtual dispatch.")]
    protected readonly int KeySizeBytes;

    /// <summary>The default tweak size in bytes, used when no tweak length is otherwise specified.</summary>
    private readonly int _defaultTweakSizeBytes;

    /// <summary>A value indicating whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent" /> class with the specified block and tweak sizes.
    /// </summary>
    /// <param name="blockSizeBits">
    /// The block size in bits. Must match the wide-block Serpent variant block size (256, 512, or 1024).
    /// </param>
    /// <param name="tweakSizeBits">The tweak size in bits (128 for all wide-block Serpent variants).</param>
    protected Serpent(int blockSizeBits, int tweakSizeBits)
    {
        BlockSizeValue = KeySizeValue = blockSizeBits;
        FeedbackSizeValue = 8;

        BlockSizeBytes = KeySizeBytes = blockSizeBits / 8;
        _defaultTweakSizeBytes = tweakSizeBits / 8;

        LegalBlockSizesValue = [new KeySizes(blockSizeBits, blockSizeBits, 0)];
        LegalKeySizesValue = [new KeySizes(blockSizeBits, blockSizeBits, 0)];
        LegalTweakSizesValue = [new KeySizes(tweakSizeBits, tweakSizeBits, 0)];
        TweakSizeValue = tweakSizeBits;

        ModeValue = CipherMode.CBC;
        Padding = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherModeKind" /> values. The default is <see cref="CipherModeKind.CBC" />.
    /// </value>
    /// <remarks>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property when used with
    /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports, including
    /// <see cref="CipherModeKind.CTR" /> and <see cref="CipherModeKind.OFB" />.
    /// </remarks>
    public CipherModeKind BlockMode { get; set; } = CipherModeKind.CBC;

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidKeySize(rgbKey, KeySize, LegalKeySizes);
        CryptographyThrowHelper.ThrowIfInvalidIVForMode(rgbIV, BlockMode, BlockSize, LegalBlockSizes);
        CryptographyThrowHelper.ThrowIfInvalidTweakSize(tweak, TweakSize, LegalTweakSizes);

        IBlockCipher engine = CreateCipher(rgbKey, tweak);
        return new SerpentTransform(engine, BlockMode, Padding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidKeySize(rgbKey, KeySize, LegalKeySizes);
        CryptographyThrowHelper.ThrowIfInvalidIVForMode(rgbIV, BlockMode, BlockSize, LegalBlockSizes);
        CryptographyThrowHelper.ThrowIfInvalidTweakSize(tweak, TweakSize, LegalTweakSizes);

        IBlockCipher engine = CreateCipher(rgbKey, tweak);
        return new SerpentTransform(engine, BlockMode, Padding, rgbIV, true);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        ThrowIfDisposed();
        IVValue = CryptographyHelper.GetRandomBytes(BlockSizeBytes);
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        ThrowIfDisposed();
        KeyValue = CryptographyHelper.GetRandomBytes(KeySizeBytes);
    }

    /// <inheritdoc />
    public override void GenerateTweak()
    {
        ThrowIfDisposed();
        TweakValue = CryptographyHelper.GetRandomBytes(_defaultTweakSizeBytes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Marks the instance as disposed, zeroes any retained key and IV buffers, and delegates the tweak buffer cleanup
    /// to the base implementation.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CryptographyHelper.Clear(KeyValue);
                CryptographyHelper.Clear(IVValue);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> whose <see cref="ObjectDisposedException.ObjectName" /> matches
    /// the concrete algorithm type's <see cref="Type.FullName" /> if the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Instantiates the concrete Serpent block cipher with the specified key and tweak.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="tweak">The tweak value.</param>
    /// <returns>A configured <see cref="IBlockCipher" /> instance for encryption or decryption.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    protected abstract IBlockCipher CreateCipher(byte[] key, byte[] tweak);
}
