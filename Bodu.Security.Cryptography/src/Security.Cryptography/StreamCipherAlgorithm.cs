// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the common base for the library's additive stream ciphers, integrating them with the
/// <see cref="SymmetricAlgorithm" /> contract while sharing key and nonce validation, key / nonce generation, and
/// disposal.
/// </summary>
/// <remarks>
/// <para>
/// A stream cipher has no cipher block and applies neither a block cipher mode nor padding. This base nonetheless
/// derives from <see cref="SymmetricAlgorithm" /> so that instances flow through <see cref="CryptoStream" /> and the
/// rest of the BCL crypto pipeline exactly like the library's block ciphers. The nonce is supplied as the
/// <see cref="SymmetricAlgorithm.IV" />; to satisfy the framework's IV-length handling the
/// <see cref="SymmetricAlgorithm.BlockSize" /> is reported as the nonce length in bits. The actual transform processes
/// data one byte at a time and imposes no alignment requirement on callers.
/// </para>
/// <para>
/// Because additive stream ciphers are self-inverse, <see cref="CreateEncryptor(byte[], byte[])" /> and
/// <see cref="CreateDecryptor(byte[], byte[])" /> are interchangeable; both delegate to
/// <see cref="CreateStreamCipher(byte[], byte[])" />, which a derived class implements to build a configured
/// <see cref="IStreamCipher" /> engine from the validated key and nonce.
/// </para>
/// <para>
/// The <see cref="IStreamCipherAlgorithm" /> marker lets callers and test harnesses distinguish these ciphers from
/// genuine block ciphers without type-name checks — for example, to skip padding-mode conformance suites that have no
/// meaning for a stream cipher.
/// </para>
/// </remarks>
/// <seealso cref="IStreamCipher" />
/// <seealso cref="StreamCipherTransform" />
public abstract class StreamCipherAlgorithm
    : SymmetricAlgorithm, IStreamCipherAlgorithm
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCipherAlgorithm" /> class with a single fixed key size and a
    /// fixed nonce size.
    /// </summary>
    /// <param name="keySizeBits">The required key size, in bits.</param>
    /// <param name="nonceSizeBits">The required nonce (IV) size, in bits.</param>
    /// <remarks>
    /// The legal key and block (nonce) sizes are fixed to the single supplied size. A stream cipher exposes no cipher
    /// mode or padding; <see cref="Mode" /> is fixed to <see cref="CipherMode.ECB" /> and <see cref="Padding" /> to
    /// <see cref="PaddingMode.None" />, and assigning any other value throws <see cref="CryptographicException" />.
    /// </remarks>
    protected StreamCipherAlgorithm(int keySizeBits, int nonceSizeBits)
        : this(keySizeBits, [new KeySizes(keySizeBits, keySizeBits, 0)], nonceSizeBits)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCipherAlgorithm" /> class with a default key size, an explicit
    /// set of legal key sizes, and a fixed nonce size.
    /// </summary>
    /// <param name="defaultKeySizeBits">The default key size, in bits, used by <see cref="GenerateKey" />.</param>
    /// <param name="legalKeySizes">The legal key sizes the cipher accepts.</param>
    /// <param name="nonceSizeBits">The required nonce (IV) size, in bits.</param>
    /// <remarks>
    /// Use this overload for ciphers that accept more than one key length (for example Salsa20, which accepts 128-bit
    /// and 256-bit keys). A stream cipher exposes no cipher mode or padding; <see cref="Mode" /> is fixed to
    /// <see cref="CipherMode.ECB" /> and <see cref="Padding" /> to <see cref="PaddingMode.None" />, and assigning any
    /// other value throws <see cref="CryptographicException" />.
    /// </remarks>
    protected StreamCipherAlgorithm(int defaultKeySizeBits, KeySizes[] legalKeySizes, int nonceSizeBits)
    {
        KeySizeValue = defaultKeySizeBits;
        LegalKeySizesValue = legalKeySizes;

        BlockSizeValue = nonceSizeBits;
        LegalBlockSizesValue = [new KeySizes(nonceSizeBits, nonceSizeBits, 0)];

        ModeValue = CipherMode.ECB;
        PaddingValue = PaddingMode.None;
    }

    /// <summary>
    /// Gets the required nonce size, in bits.
    /// </summary>
    /// <value>The nonce size, in bits.</value>
    /// <returns>The number of bits a valid nonce must contain.</returns>
    /// <remarks>
    /// The nonce is supplied through <see cref="SymmetricAlgorithm.IV" />. To satisfy the framework's IV-length
    /// handling the nonce size is also reported as the <see cref="SymmetricAlgorithm.BlockSize" />, so
    /// <see cref="NonceSize" /> always equals <see cref="SymmetricAlgorithm.BlockSize" /> for a stream cipher. This
    /// property exists to name the concept unambiguously; a stream cipher has no block in the block-cipher sense.
    /// </remarks>
    public int NonceSize => BlockSizeValue;

    /// <summary>
    /// Gets the required nonce length, in bytes.
    /// </summary>
    /// <value>The nonce length, in bytes.</value>
    /// <returns>The number of bytes a valid nonce must contain.</returns>
    protected int NonceLengthInBytes => BlockSizeValue / 8;

    /// <summary>
    /// Gets or sets the cipher mode. A stream cipher has no chaining or feedback mode, so this is fixed to
    /// <see cref="CipherMode.ECB" />.
    /// </summary>
    /// <value>Always <see cref="CipherMode.ECB" />.</value>
    /// <returns><see cref="CipherMode.ECB" />, the only mode an additive stream cipher supports.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="CryptographicException">A value other than <see cref="CipherMode.ECB" /> is assigned.</exception>
    /// <remarks>
    /// A stream cipher does not chain blocks, so no block-cipher mode applies. <see cref="CipherMode.ECB" /> is reported
    /// as the least misleading sentinel because it implies no feedback or chaining. Assigning any other value throws
    /// rather than silently accepting an inert setting.
    /// </remarks>
    public override CipherMode Mode
    {
        get => CipherMode.ECB;
        set
        {
            ThrowIfDisposed();

            if (value != CipherMode.ECB)
                throw new CryptographicException(
                    string.Format(CultureInfo.InvariantCulture, CryptoResourceStrings.Crypt_Invalid_StreamCipherMode, value));

            ModeValue = CipherMode.ECB;
        }
    }

    /// <summary>
    /// Gets or sets the padding mode. A stream cipher imposes no block alignment, so this is fixed to
    /// <see cref="PaddingMode.None" />.
    /// </summary>
    /// <value>Always <see cref="PaddingMode.None" />.</value>
    /// <returns><see cref="PaddingMode.None" />, the only padding an additive stream cipher supports.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="CryptographicException">A value other than <see cref="PaddingMode.None" /> is assigned.</exception>
    /// <remarks>
    /// An additive stream cipher processes data one byte at a time and never pads. Assigning any padding mode other than
    /// <see cref="PaddingMode.None" /> throws rather than silently accepting an inert setting.
    /// </remarks>
    public override PaddingMode Padding
    {
        get => PaddingMode.None;
        set
        {
            ThrowIfDisposed();

            if (value != PaddingMode.None)
                throw new CryptographicException(
                    string.Format(CultureInfo.InvariantCulture, CryptoResourceStrings.Crypt_Invalid_StreamCipherPadding, value));

            PaddingValue = PaddingMode.None;
        }
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV) =>
        CreateTransform(rgbKey, rgbIV);

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV) =>
        CreateTransform(rgbKey, rgbIV);

    /// <inheritdoc />
    public override void GenerateKey()
    {
        ThrowIfDisposed();
        KeyValue = CryptoHelpers.GetRandomBytes(KeySizeValue / 8);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        ThrowIfDisposed();
        IVValue = CryptoHelpers.GetRandomBytes(BlockSizeValue / 8);
    }

    /// <summary>
    /// Builds a configured <see cref="IStreamCipher" /> engine from the validated key and nonce.
    /// </summary>
    /// <param name="key">The key, already validated to the algorithm's key size.</param>
    /// <param name="nonce">The nonce, already validated to the algorithm's nonce size.</param>
    /// <returns>A new <see cref="IStreamCipher" /> engine positioned at the start of its keystream.</returns>
    /// <remarks>
    /// Implementations receive a key and nonce whose lengths have already been checked by the base class, so they need
    /// only construct their engine. Ownership of the returned engine transfers to the caller.
    /// </remarks>
    protected abstract IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(KeyValue);
                CryptoHelpers.Clear(IVValue);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if this instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif

    /// <summary>
    /// Validates the key and nonce, builds the engine, and wraps it in a self-inverse stream transform.
    /// </summary>
    /// <param name="rgbKey">The key.</param>
    /// <param name="rgbIV">The nonce.</param>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The key or nonce length is invalid.</exception>
    private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV)
    {
        ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, KeySize, LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidNonceSize(rgbIV, NonceLengthInBytes);

        // ThrowIfInvalidNonceSize guarantees rgbIV is non-null past this point.
        IStreamCipher engine = CreateStreamCipher(rgbKey, rgbIV!);
        return new StreamCipherTransform(engine);
    }
}
