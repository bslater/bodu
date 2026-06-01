// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the common base for the library's additive (symmetric, shared-key) stream ciphers, sharing key and nonce
/// storage, validation, generation, transform creation, and disposal.
/// </summary>
/// <remarks>
/// <para>
/// A stream cipher has no cipher block and applies neither a block-cipher mode nor padding. Unlike a block cipher it is
/// not modelled on <see cref="SymmetricAlgorithm" />: there is no <c>Mode</c>, <c>Padding</c>, <c>BlockSize</c>, or
/// <c>IV</c>. Instead the cipher is parameterized by a <see cref="Key" /> and a <see cref="Nonce" />, and produces an
/// <see cref="ICryptoTransform" /> that XORs data with a key- and nonce-dependent keystream. The transform processes
/// data one byte at a time and imposes no alignment requirement on callers, so it composes naturally with
/// <see cref="CryptoStream" /> and any other consumer of the <see cref="ICryptoTransform" /> contract.
/// </para>
/// <para>
/// Because additive stream ciphers are self-inverse, <see cref="CreateEncryptor()" /> and
/// <see cref="CreateDecryptor()" /> are interchangeable; both delegate to <see cref="CreateTransform()" />, which in
/// turn calls <see cref="CreateStreamCipher(byte[], byte[])" /> — implemented by a derived class to build a configured
/// <see cref="IStreamCipher" /> engine from the validated key and nonce.
/// </para>
/// <para>
/// <strong>Nonce reuse is catastrophic.</strong> A given <c>(key, nonce)</c> pair must encrypt at most one message;
/// reusing a pair XORs two plaintexts under the same keystream and destroys confidentiality. Generate a fresh nonce per
/// message with <see cref="GenerateNonce" /> (or supply one explicitly) and never repeat it under the same key.
/// </para>
/// </remarks>
/// <seealso cref="IStreamCipher" /> <seealso cref="StreamCipherTransform" />
public abstract class SymmetricStreamAlgorithm
    : IDisposable
{
    private readonly int _nonceSizeBits;
    private readonly KeySizes[] _legalKeySizes;

    private int _keySizeBits;
    private byte[]? _key;
    private byte[]? _nonce;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SymmetricStreamAlgorithm" /> class with a single fixed key size and
    /// a fixed nonce size.
    /// </summary>
    /// <param name="keySizeBits">The required key size, in bits.</param>
    /// <param name="nonceSizeBits">The required nonce size, in bits.</param>
    /// <remarks>
    /// Use this overload for ciphers that accept exactly one key length. The legal key sizes are fixed to the single
    /// supplied size.
    /// </remarks>
    protected SymmetricStreamAlgorithm(int keySizeBits, int nonceSizeBits)
        : this(keySizeBits, [new KeySizes(keySizeBits, keySizeBits, 0)], nonceSizeBits)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SymmetricStreamAlgorithm" /> class with a default key size, an
    /// explicit set of legal key sizes, and a fixed nonce size.
    /// </summary>
    /// <param name="defaultKeySizeBits">The default key size, in bits, used by <see cref="GenerateKey" />.</param>
    /// <param name="legalKeySizes">The legal key sizes the cipher accepts. Must not be <see langword="null" />.</param>
    /// <param name="nonceSizeBits">The required nonce size, in bits.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="legalKeySizes" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Use this overload for ciphers that accept more than one key length (for example Salsa20, which accepts 128-bit
    /// and 256-bit keys).
    /// </remarks>
    protected SymmetricStreamAlgorithm(int defaultKeySizeBits, KeySizes[] legalKeySizes, int nonceSizeBits)
    {
        ThrowHelper.ThrowIfNull(legalKeySizes);

        _legalKeySizes = (KeySizes[])legalKeySizes.Clone();
        _keySizeBits = defaultKeySizeBits;
        _nonceSizeBits = nonceSizeBits;
    }

    /// <summary>
    /// Gets or sets the key size, in bits.
    /// </summary>
    /// <value>The key size, in bits. The default is the cipher's default key size.</value>
    /// <returns>
    /// The number of bits the current <see cref="Key" /> contains, or will contain when next generated.
    /// </returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="CryptographicException">
    /// The assigned value is not one of the <see cref="LegalKeySizes" />.
    /// </exception>
    /// <remarks>
    /// Assigning a new key size discards any key material previously held by <see cref="Key" />; the next read of
    /// <see cref="Key" /> generates a fresh key of the new size.
    /// </remarks>
    public int KeySize
    {
        get
        {
            ThrowIfDisposed();
            return _keySizeBits;
        }

        set
        {
            ThrowIfDisposed();

            if (!IsLegalKeySize(value, _legalKeySizes))
                throw new CryptographicException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CryptoResourceStrings.Crypt_Invalid_KeySize,
                        value,
                        CryptographyHelper.FormatLegalSizes(_legalKeySizes)));

            _keySizeBits = value;
            CryptographyHelper.ClearAndNullify(ref _key);
        }
    }

    /// <summary>
    /// Gets the required nonce size, in bits.
    /// </summary>
    /// <value>The nonce size, in bits.</value>
    /// <returns>The number of bits a valid <see cref="Nonce" /> must contain.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public int NonceSize
    {
        get
        {
            ThrowIfDisposed();
            return _nonceSizeBits;
        }
    }

    /// <summary>
    /// Gets the key sizes, in bits, that this cipher accepts.
    /// </summary>
    /// <value>A copy of the legal key-size descriptors.</value>
    /// <returns>A new array describing the permitted key sizes; mutating it does not affect the cipher.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public KeySizes[] LegalKeySizes
    {
        get
        {
            ThrowIfDisposed();
            return (KeySizes[])_legalKeySizes.Clone();
        }
    }

    /// <summary>
    /// Gets or sets the secret key.
    /// </summary>
    /// <value>A copy of the secret key bytes.</value>
    /// <returns>A new array containing the key; the cipher retains its own private copy.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// The length of the assigned value is not one of the <see cref="LegalKeySizes" />.
    /// </exception>
    /// <remarks>
    /// Reading this property generates a random key of <see cref="KeySize" /> bits on first access if none has been
    /// set. Both the getter and setter copy the array so callers cannot mutate the cipher's key through an alias.
    /// </remarks>
    public byte[] Key
    {
        get
        {
            ThrowIfDisposed();

            if (_key is null)
                GenerateKey();

            return (byte[])_key!.Clone();
        }

        set
        {
            ThrowIfDisposed();
            ThrowHelper.ThrowIfNull(value);
            CryptographyThrowHelper.ThrowIfInvalidKeySize(value, value.Length * 8, _legalKeySizes);

            CryptographyHelper.ClearAndNullify(ref _key);
            _key = (byte[])value.Clone();
            _keySizeBits = value.Length * 8;
        }
    }

    /// <summary>
    /// Gets or sets the nonce.
    /// </summary>
    /// <value>A copy of the nonce bytes.</value>
    /// <returns>A new array containing the nonce; the cipher retains its own private copy.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// The length of the assigned value is not <see cref="NonceSize" /> bits.
    /// </exception>
    /// <remarks>
    /// Reading this property generates a random nonce of <see cref="NonceSize" /> bits on first access if none has been
    /// set. A nonce must never be reused under the same key; see the type remarks.
    /// </remarks>
    public byte[] Nonce
    {
        get
        {
            ThrowIfDisposed();

            if (_nonce is null)
                GenerateNonce();

            return (byte[])_nonce!.Clone();
        }

        set
        {
            ThrowIfDisposed();
            ThrowHelper.ThrowIfNull(value);
            CryptographyThrowHelper.ThrowIfInvalidNonceSize(value, NonceLengthInBytes);

            CryptographyHelper.ClearAndNullify(ref _nonce);
            _nonce = (byte[])value.Clone();
        }
    }

    /// <summary>
    /// Gets the required nonce length, in bytes.
    /// </summary>
    /// <value>The nonce length, in bytes.</value>
    /// <returns>The number of bytes a valid nonce must contain.</returns>
    protected int NonceLengthInBytes => _nonceSizeBits / 8;

    /// <summary>
    /// Generates a random secret key of <see cref="KeySize" /> bits, replacing any existing key.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public void GenerateKey()
    {
        ThrowIfDisposed();

        CryptographyHelper.ClearAndNullify(ref _key);
        _key = CryptographyHelper.GetRandomBytes(_keySizeBits / 8);
    }

    /// <summary>
    /// Generates a random nonce of <see cref="NonceSize" /> bits, replacing any existing nonce.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public void GenerateNonce()
    {
        ThrowIfDisposed();

        CryptographyHelper.ClearAndNullify(ref _nonce);
        _nonce = CryptographyHelper.GetRandomBytes(_nonceSizeBits / 8);
    }

    /// <summary>
    /// Creates a self-inverse <see cref="ICryptoTransform" /> using the current <see cref="Key" /> and
    /// <see cref="Nonce" />.
    /// </summary>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <remarks>
    /// Because an additive stream cipher is self-inverse, this method is identical to <see cref="CreateDecryptor()" />.
    /// </remarks>
    public ICryptoTransform CreateEncryptor() =>
        CreateTransform(Key, Nonce);

    /// <summary>
    /// Creates a self-inverse <see cref="ICryptoTransform" /> from the supplied key and nonce.
    /// </summary>
    /// <param name="key">The secret key. Must not be <see langword="null" />.</param>
    /// <param name="nonce">The nonce. Must not be <see langword="null" />.</param>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">The key or nonce length is invalid.</exception>
    /// <remarks>
    /// The supplied values are used as-is for this transform and do not alter the cipher's <see cref="Key" /> or
    /// <see cref="Nonce" /> properties. Because an additive stream cipher is self-inverse, this method is identical to
    /// <see cref="CreateDecryptor(byte[], byte[])" />.
    /// </remarks>
    public ICryptoTransform CreateEncryptor(byte[] key, byte[] nonce) =>
        CreateTransform(key, nonce);

    /// <summary>
    /// Creates a self-inverse <see cref="ICryptoTransform" /> using the current <see cref="Key" /> and
    /// <see cref="Nonce" />.
    /// </summary>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <remarks>
    /// Because an additive stream cipher is self-inverse, this method is identical to <see cref="CreateEncryptor()" />.
    /// </remarks>
    public ICryptoTransform CreateDecryptor() =>
        CreateTransform(Key, Nonce);

    /// <summary>
    /// Creates a self-inverse <see cref="ICryptoTransform" /> from the supplied key and nonce.
    /// </summary>
    /// <param name="key">The secret key. Must not be <see langword="null" />.</param>
    /// <param name="nonce">The nonce. Must not be <see langword="null" />.</param>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">The key or nonce length is invalid.</exception>
    /// <remarks>
    /// The supplied values are used as-is for this transform and do not alter the cipher's <see cref="Key" /> or
    /// <see cref="Nonce" /> properties. Because an additive stream cipher is self-inverse, this method is identical to
    /// <see cref="CreateEncryptor(byte[], byte[])" />.
    /// </remarks>
    public ICryptoTransform CreateDecryptor(byte[] key, byte[] nonce) =>
        CreateTransform(key, nonce);

    /// <summary>
    /// Creates a self-inverse <see cref="ICryptoTransform" /> using the current <see cref="Key" /> and
    /// <see cref="Nonce" />.
    /// </summary>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public ICryptoTransform CreateTransform() =>
        CreateTransform(Key, Nonce);

    /// <summary>
    /// Validates the supplied key and nonce, builds the engine, and wraps it in a self-inverse stream transform.
    /// </summary>
    /// <param name="key">The secret key. Must not be <see langword="null" />.</param>
    /// <param name="nonce">The nonce. Must not be <see langword="null" />.</param>
    /// <returns>An <see cref="ICryptoTransform" /> that XORs data with the cipher keystream.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">The key or nonce length is invalid.</exception>
    /// <remarks>
    /// The supplied values are used as-is for this transform and do not alter the cipher's <see cref="Key" /> or
    /// <see cref="Nonce" /> properties.
    /// </remarks>
    public ICryptoTransform CreateTransform(byte[] key, byte[] nonce)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(nonce);
        CryptographyThrowHelper.ThrowIfInvalidKeySize(key, key.Length * 8, _legalKeySizes);
        CryptographyThrowHelper.ThrowIfInvalidNonceSize(nonce, NonceLengthInBytes);

        IStreamCipher engine = CreateStreamCipher(key, nonce);
        return new StreamCipherTransform(engine);
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

    /// <summary>
    /// Releases all resources used by the current instance and clears any sensitive key material.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the instance and, optionally, the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CryptographyHelper.Clear(_key);
            CryptographyHelper.Clear(_nonce);
        }

        _key = null;
        _nonce = null;
        _disposed = true;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if this instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Determines whether the supplied key size, in bits, is one of the cipher's legal key sizes.
    /// </summary>
    /// <param name="bitLength">The candidate key size, in bits.</param>
    /// <param name="legalKeySizes">The legal key-size descriptors to test against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="bitLength" /> is permitted by any descriptor in
    /// <paramref name="legalKeySizes" />; otherwise, <see langword="false" />.
    /// </returns>
    private static bool IsLegalKeySize(int bitLength, KeySizes[] legalKeySizes)
    {
        foreach (KeySizes keySizes in legalKeySizes)
        {
            if (bitLength < keySizes.MinSize || bitLength > keySizes.MaxSize)
                continue;

            if (keySizes.SkipSize == 0)
                return bitLength == keySizes.MinSize;

            if ((bitLength - keySizes.MinSize) % keySizes.SkipSize == 0)
                return true;
        }

        return false;
    }
}
