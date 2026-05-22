// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed implementations of the Threefish tweakable symmetric block cipher
/// family (Threefish-256, Threefish-512, and Threefish-1024).
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher designed by Bruce Schneier, Niels Ferguson, Stefan Lucks, Doug Whiting, Mihir
/// Bellare, Tadayoshi Kohno, Jon Callas, and Jesse Walker as the core primitive of the Skein hash function, submitted
/// to the NIST SHA-3 competition (2008). Each variant operates on a block whose size in bits matches its key size (256,
/// 512, or 1024 bits) together with a 128-bit tweak. Derived classes must implement
/// <see cref="CreateCipher(byte[], byte[])" /> to instantiate the appropriate concrete engine.
/// </para>
/// <para>
/// The <see cref="BlockMode" /> property replaces the standard <see cref="SymmetricAlgorithm.Mode" /> property,
/// enabling the use of additional or non-standard block cipher modes such as <see cref="CipherModeKind.CTR" /> and
/// <see cref="CipherModeKind.OFB" />.
/// </para>
/// <para>
/// <strong>Concrete variants.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Threefish256" /> — 256-bit block, 256-bit key, 128-bit tweak.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Threefish512" /> — 512-bit block, 512-bit key, 128-bit tweak (the recommended general-purpose default).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Threefish1024" /> — 1024-bit block, 1024-bit key, 128-bit tweak.
/// </description>
/// </item>
/// </list>
/// <para>
/// Threefish is the cipher under the UBI mode of <see cref="Skein{T}" /> — the same key-and-tweak primitive that drives
/// Skein's hash compression. For a non-tweakable, hardware-accelerated default prefer
/// <see cref="System.Security.Cryptography.Aes" />. For try-pattern transform creation that surfaces bad key/IV/tweak
/// combinations as a <see langword="false" /> return, see
/// <see cref="Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions" />.
/// </para>
/// <note type="important">This class is not intended to be instantiated directly. Use <see cref="Threefish256" />,
/// <see cref="Threefish512" />, or <see cref="Threefish1024" /> instead.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Use the recommended general-purpose variant — Threefish-512 over a CTR mode.
/// using TweakableSymmetricAlgorithm alg = new Threefish512();
/// alg.GenerateKey();
/// alg.GenerateIV();
/// alg.GenerateTweak();
/// alg.BlockMode = CipherModeKind.CTR;
///
/// using ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV, alg.Tweak);
/// using var cipherText = new MemoryStream();
/// using (var cs = new CryptoStream(cipherText, encryptor, CryptoStreamMode.Write))
///     cs.Write(plaintext, 0, plaintext.Length);
///]]>
/// </example>
/// <seealso cref="Threefish256"/> <seealso cref="Threefish512"/> <seealso cref="Threefish1024"/>
/// <seealso cref="TweakableSymmetricAlgorithm"/> <seealso cref="Skein{T}"/>
/// <seealso href="https://www.schneier.com/wp-content/uploads/2016/02/skein.pdf">The Skein Hash Function Family
/// (Schneier et al., 2010) — specifies Threefish</seealso>
public abstract class Threefish
    : TweakableSymmetricAlgorithm
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish" /> class with the specified block and tweak sizes.
    /// </summary>
    /// <param name="blockSizeBits">
    /// The block size in bits. Must match the Threefish variant block size (256, 512, or 1024).
    /// </param>
    /// <param name="tweakSizeBits">The tweak size in bits. 128 bits for all Threefish variants.</param>
    /// <remarks>
    /// Sizes are stored in bits via <see cref="SymmetricAlgorithm.BlockSizeValue" />,
    /// <see cref="SymmetricAlgorithm.KeySizeValue" />, and <see cref="TweakableSymmetricAlgorithm.TweakSizeValue" />,
    /// matching the BCL convention. Conversion to bytes occurs only at the byte-array processing boundary (e.g.
    /// <see cref="GenerateKey" />, <see cref="GenerateIV" />, <see cref="GenerateTweak" />).
    /// </remarks>
    protected Threefish(int blockSizeBits, int tweakSizeBits)
    {
        this.BlockSizeValue = this.KeySizeValue = blockSizeBits;
        this.FeedbackSizeValue = 8;

        this.LegalBlockSizesValue = [new KeySizes(blockSizeBits, blockSizeBits, 0)];
        this.LegalKeySizesValue = [new KeySizes(blockSizeBits, blockSizeBits, 0)];
        this.LegalTweakSizesValue = [new KeySizes(tweakSizeBits, tweakSizeBits, 0)];
        this.TweakSizeValue = tweakSizeBits;

        this.ModeValue = CipherMode.CBC;
        this.Padding = PaddingMode.PKCS7;
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
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySize, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSize, this.LegalBlockSizes);
        CryptoHelpers.ThrowIfInvalidTweakSize(tweak, this.TweakSize, this.LegalTweakSizes);

        ThreefishBlockCipher engine = this.CreateCipher(rgbKey, tweak);
        return new ThreefishTransform(engine, this.BlockMode, this.Padding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySize, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSize, this.LegalBlockSizes);
        CryptoHelpers.ThrowIfInvalidTweakSize(tweak, this.TweakSize, this.LegalTweakSizes);

        ThreefishBlockCipher engine = this.CreateCipher(rgbKey, tweak);
        return new ThreefishTransform(engine, this.BlockMode, this.Padding, rgbIV, true);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeValue / 8);
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeValue / 8);
    }

    /// <inheritdoc />
    public override void GenerateTweak()
    {
        this.ThrowIfDisposed();
        this.TweakValue = CryptoHelpers.GetRandomNonZeroBytes(this.TweakSizeValue / 8);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Marks the instance as disposed, zeroes any retained key and IV buffers, and delegates the tweak buffer cleanup
    /// to the base implementation.
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
    /// Throws an <see cref="ObjectDisposedException" /> whose <see cref="ObjectDisposedException.ObjectName" /> matches
    /// the concrete algorithm type's <see cref="Type.FullName" /> if the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif

    /// <summary>
    /// Instantiates the concrete Threefish block cipher with the specified key and tweak.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="tweak">The tweak value.</param>
    /// <returns>A configured <see cref="IBlockCipher" /> instance for encryption or decryption.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    protected abstract ThreefishBlockCipher CreateCipher(byte[] key, byte[] tweak);
}
