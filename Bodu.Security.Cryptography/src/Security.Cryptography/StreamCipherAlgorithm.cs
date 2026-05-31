// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
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
    /// Initializes a new instance of the <see cref="StreamCipherAlgorithm" /> class with the specified key and nonce
    /// sizes.
    /// </summary>
    /// <param name="keySizeBits">The required key size, in bits.</param>
    /// <param name="nonceSizeBits">The required nonce (IV) size, in bits.</param>
    /// <remarks>
    /// The legal key and block (nonce) sizes are fixed to the single supplied size. A stream cipher exposes no cipher
    /// mode or padding; the inherited <see cref="SymmetricAlgorithm.Mode" /> and
    /// <see cref="SymmetricAlgorithm.Padding" /> values are inert.
    /// </remarks>
    protected StreamCipherAlgorithm(int keySizeBits, int nonceSizeBits)
    {
        KeySizeValue = keySizeBits;
        LegalKeySizesValue = [new KeySizes(keySizeBits, keySizeBits, 0)];

        BlockSizeValue = nonceSizeBits;
        LegalBlockSizesValue = [new KeySizes(nonceSizeBits, nonceSizeBits, 0)];

        ModeValue = CipherMode.CBC;
        PaddingValue = PaddingMode.None;
    }

    /// <summary>
    /// Gets the required nonce length, in bytes.
    /// </summary>
    /// <value>The nonce length, in bytes.</value>
    /// <returns>The number of bytes a valid nonce must contain.</returns>
    protected int NonceLengthInBytes => BlockSizeValue / 8;

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
        KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySizeValue / 8);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        ThrowIfDisposed();
        IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeValue / 8);
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
        ChaCha20Helpers.ThrowIfInvalidNonceSize(rgbIV, NonceLengthInBytes);

        // ThrowIfInvalidNonceSize guarantees rgbIV is non-null past this point.
        IStreamCipher engine = CreateStreamCipher(rgbKey, rgbIV!);
        return new StreamCipherTransform(engine);
    }
}
