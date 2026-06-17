// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the common <see cref="IStreamAeadTransform" /> implementation shared by the extended-nonce Poly1305 AEAD
/// constructions — argument validation, buffer-overlap rules, single-use lifecycle, and secure clearing of retained key
/// material. Derived types supply the keystream engine and, when required, an alternative framing.
/// </summary>
/// <remarks>
/// <para>
/// All constructions in this family bind a 256-bit key and a 192-bit nonce and emit a 128-bit tag, with the wire format
/// <c>ciphertext ‖ tag</c>. Associated data is passed directly to <see cref="Encrypt" /> / <see cref="Decrypt" /> and
/// defaults to empty; there is no separate associated-data step. By default the transform applies the RFC 8439 framing
/// (<see cref="Poly1305AeadCore.SealRfc8439" /> / <see cref="Poly1305AeadCore.OpenRfc8439" />); a derived type may
/// override <see cref="SealCore" /> and <see cref="OpenCore" /> to substitute a different framing, as the NaCl
/// <c>crypto_secretbox</c> construction does.
/// </para>
/// <para>
/// Instances are stateful, not thread-safe, and single-use per message. A second call to <see cref="Encrypt" /> or
/// <see cref="Decrypt" /> — including after a tag-mismatch failure — throws <see cref="InvalidOperationException" />.
/// </para>
/// <para>
/// <strong>Buffer overlap.</strong> Exact in-place operation is supported: the plaintext (or ciphertext) may begin at
/// the same location as the output. Any other (partial) overlap is rejected with <see cref="ArgumentException" />.
/// </para>
/// </remarks>
public abstract class Poly1305AeadTransform
    : IStreamAeadTransform
{
    /// <summary>Length of the key, in bytes (256 bits).</summary>
    internal const int KeyBytes = 32;

    /// <summary>Length of the extended nonce, in bytes (192 bits).</summary>
    internal const int NonceBytes = 24;

    /// <summary>Length of the authentication tag, in bytes (128 bits).</summary>
    internal const int TagBytes = Poly1305AeadCore.TagBytes;

    /// <summary>Length of the authentication tag, in bits (128).</summary>
    private const int TagSizeBits = TagBytes * 8;

    /// <summary>The retained 256-bit secret key, cleared on disposal.</summary>
    private readonly byte[] _key;

    /// <summary>The retained 192-bit nonce, cleared on disposal.</summary>
    private readonly byte[] _nonce;

    /// <summary>A value indicating whether this single-use transform has already processed a message.</summary>
    private bool _completed;

    /// <summary>A value indicating whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Poly1305AeadTransform" /> class with the specified key and nonce.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">
    /// The 192-bit (24-byte) nonce. Must be unique for every message encrypted under the same key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly <see cref="KeyBytes" /> bytes, or <paramref name="nonce" /> is not
    /// exactly <see cref="NonceBytes" /> bytes.
    /// </exception>
    protected Poly1305AeadTransform(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(key, KeyBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(nonce, NonceBytes);

        _key = key.ToArray();
        _nonce = nonce.ToArray();
    }

    /// <inheritdoc />
    /// <value>Length of the authentication tag is 128 bits (16 bytes).</value>
    public int TagSize => TagSizeBits;

    /// <summary>
    /// Gets a value indicating whether this construction authenticates associated data.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> for the RFC 8439-framed constructions; <see langword="false" /> for the secretbox
    /// construction, which has no associated-data input.
    /// </returns>
    protected virtual bool SupportsAssociatedData => true;

    /// <summary>
    /// Gets the retained secret key.
    /// </summary>
    /// <returns>A read-only view over the 32-byte key.</returns>
    protected ReadOnlySpan<byte> Key => _key;

    /// <summary>
    /// Gets the retained nonce.
    /// </summary>
    /// <returns>A read-only view over the 24-byte nonce.</returns>
    protected ReadOnlySpan<byte> Nonce => _nonce;

    /// <inheritdoc />
    public int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output, ReadOnlySpan<byte> associatedData = default)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();
        ThrowIfAssociatedDataUnsupported(associatedData);

        int required = checked(plaintext.Length + TagBytes);
        if (output.Length < required)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, required),
                nameof(output));

        ThrowIfPartialOverlap(plaintext, output);

        try
        {
            using IStreamCipher engine = CreateEngine();
            return SealCore(engine, associatedData, plaintext, output);
        }
        finally
        {
            _completed = true;
        }
    }

    /// <inheritdoc />
    public int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output, ReadOnlySpan<byte> associatedData = default)
    {
        ThrowIfDisposed();
        ThrowIfCompleted();
        ThrowIfAssociatedDataUnsupported(associatedData);

        if (ciphertextWithTag.Length < TagBytes)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, TagBytes),
                nameof(ciphertextWithTag));

        int plaintextLength = ciphertextWithTag.Length - TagBytes;
        if (output.Length < plaintextLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_OutputBufferTooSmall, plaintextLength),
                nameof(output));

        ThrowIfPartialOverlap(ciphertextWithTag, output);

        try
        {
            using IStreamCipher engine = CreateEngine();
            return OpenCore(engine, associatedData, ciphertextWithTag, output);
        }
        finally
        {
            _completed = true;
        }
    }

    /// <summary>
    /// Releases the resources used by this instance and clears the retained key and nonce from memory.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographicOperations.ZeroMemory(_key);
        CryptographicOperations.ZeroMemory(_nonce);

        _completed = true;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates the keystream engine for a single message, positioned at block counter 0.
    /// </summary>
    /// <returns>
    /// A freshly constructed <see cref="IStreamCipher" /> bound to <see cref="Key" /> and <see cref="Nonce" />.
    /// </returns>
    /// <remarks>
    /// The caller owns the returned engine and disposes it after the message completes.
    /// </remarks>
    protected abstract IStreamCipher CreateEngine();

    /// <summary>
    /// Encrypts and authenticates a message. The default implementation applies the RFC 8439 framing.
    /// </summary>
    /// <param name="engine">The keystream engine, positioned at block counter 0.</param>
    /// <param name="associatedData">The associated data to authenticate.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">Receives the ciphertext followed by the authentication tag.</param>
    /// <returns>The number of bytes written.</returns>
    protected virtual int SealCore(
        IStreamCipher engine,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> plaintext,
        Span<byte> output) =>
        Poly1305AeadCore.SealRfc8439(engine, associatedData, plaintext, output);

    /// <summary>
    /// Verifies and decrypts a message. The default implementation applies the RFC 8439 framing.
    /// </summary>
    /// <param name="engine">The keystream engine, positioned at block counter 0.</param>
    /// <param name="associatedData">The associated data that must match the value used at encryption time.</param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its authentication tag.</param>
    /// <param name="output">Receives the recovered plaintext.</param>
    /// <returns>The number of plaintext bytes written.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    protected virtual int OpenCore(
        IStreamCipher engine,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> ciphertextWithTag,
        Span<byte> output) =>
        Poly1305AeadCore.OpenRfc8439(engine, associatedData, ciphertextWithTag, output);

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="input" /> and <paramref name="output" /> overlap
    /// by any amount other than an exact same-start in-place arrangement.
    /// </summary>
    /// <param name="input">The plaintext or ciphertext span.</param>
    /// <param name="output">The destination span.</param>
    /// <exception cref="ArgumentException">The buffers partially overlap.</exception>
    private static void ThrowIfPartialOverlap(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        if (input.Overlaps(output, out int elementOffset) && elementOffset != 0)
            throw new ArgumentException(CryptoResourceStrings.Crypt_Invalid_PartialBufferOverlap, nameof(output));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if non-empty associated data is supplied to a construction that does
    /// not authenticate it.
    /// </summary>
    /// <param name="associatedData">The associated data supplied by the caller.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="SupportsAssociatedData" /> is <see langword="false" /> and <paramref name="associatedData" /> is not
    /// empty.
    /// </exception>
    private void ThrowIfAssociatedDataUnsupported(ReadOnlySpan<byte> associatedData)
    {
        if (!SupportsAssociatedData && !associatedData.IsEmpty)
            throw new ArgumentException(
                CryptoResourceStrings.Crypt_Invalid_SecretboxAssociatedData,
                nameof(associatedData));
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if this instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Throws an <see cref="InvalidOperationException" /> if this instance has already completed a message.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The instance has already encrypted or decrypted a message.
    /// </exception>
    private void ThrowIfCompleted()
    {
        if (_completed)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_TransformAlreadyFinalized);
    }
}
