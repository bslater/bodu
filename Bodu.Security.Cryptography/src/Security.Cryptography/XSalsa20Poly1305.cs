// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides authenticated encryption using the NaCl <c>crypto_secretbox</c> XSalsa20-Poly1305 construction. Accepts a
/// 256-bit key and a 192-bit nonce and produces a 128-bit authentication tag. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// XSalsa20-Poly1305 is the secret-key authenticated-encryption primitive behind NaCl / libsodium
/// <c>crypto_secretbox</c>. A 256-bit subkey is derived from the key and the first 128 bits of the nonce via HSalsa20,
/// and Salsa20 runs under that subkey with the trailing 64 bits of the nonce. The leading 32 bytes of the counter-0
/// keystream block form the one-time <see cref="Poly1305" /> key; the message is encrypted with the keystream from byte
/// 32 onward; and the tag is computed over the ciphertext alone.
/// </para>
/// <para>
/// <strong>No associated data.</strong> The classic secretbox construction does not authenticate associated data. For
/// interface compatibility this type implements <see cref="IAeadBlockCipherModeTransform" />, but
/// <see cref="ProcessAssociatedData" /> accepts only an empty span and otherwise throws. When associated-data support
/// is required, use <see cref="XSalsa20Poly1305Ietf" /> (XSalsa20 with RFC 8439 framing) or
/// <see cref="XChaCha20Poly1305" />.
/// </para>
/// <para>
/// <strong>Wire format.</strong> This type emits the ciphertext followed by the 16-byte tag (<c>ciphertext ‖ tag</c>),
/// matching the rest of the library's AEAD surface. NaCl's combined <c>crypto_secretbox</c> output places the tag first
/// (<c>tag ‖ ciphertext</c>); the ciphertext and tag bytes are identical, only their order differs.
/// </para>
/// <para>
/// Each instance is single-use. A new instance must be created for every message. Reusing a nonce under the same key
/// destroys confidentiality and authenticity.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using IAeadBlockCipherModeTransform enc = new XSalsa20Poly1305(key, nonce);
/// byte[] sealed_ = enc.Encrypt(plaintext); // ciphertext || tag, no associated data
/// using IAeadBlockCipherModeTransform dec = new XSalsa20Poly1305(key, nonce);
/// byte[] recovered = dec.Decrypt(sealed_);
///]]>
/// </code>
/// </example>
/// <seealso href="https://nacl.cr.yp.to/secretbox.html">NaCl crypto_secretbox</seealso> <seealso cref="XSalsa20" />
/// <seealso cref="XSalsa20Poly1305Ietf" /> <seealso cref="XChaCha20Poly1305" />
/// <seealso cref="IAeadBlockCipherModeTransform" />
public sealed class XSalsa20Poly1305
    : Poly1305AeadTransform
{
    /// <summary>
    /// Length of the XSalsa20-Poly1305 key is 256 bits (32 bytes).
    /// </summary>
    public const int KeySize = KeyBytes * 8;

    /// <summary>
    /// Length of the XSalsa20-Poly1305 nonce is 192 bits (24 bytes).
    /// </summary>
    public const int NonceSize = NonceBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="XSalsa20Poly1305" /> class with the specified key and nonce.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key. Must not be <see langword="null" />.</param>
    /// <param name="nonce">
    /// The 192-bit (24-byte) nonce. Must be unique for every message encrypted under the same key. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="nonce" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly 32 bytes, or <paramref name="nonce" /> is not exactly 24 bytes.
    /// </exception>
    public XSalsa20Poly1305(byte[] key, byte[] nonce)
        : this(
            key is null ? throw new ArgumentNullException(nameof(key)) : key.AsSpan(),
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : nonce.AsSpan())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XSalsa20Poly1305" /> class with the specified key and nonce spans.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">
    /// The 192-bit (24-byte) nonce. Must be unique for every message encrypted under the same key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly 32 bytes, or <paramref name="nonce" /> is not exactly 24 bytes.
    /// </exception>
    public XSalsa20Poly1305(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
        : base(key, nonce)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// The secretbox construction does not authenticate associated data. Supplying a non-empty span throws
    /// <see cref="ArgumentException" />; an empty span is accepted to satisfy the
    /// <see cref="IAeadBlockCipherModeTransform" /> contract.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="associatedData" /> is not empty.</exception>
    public override void ProcessAssociatedData(ReadOnlySpan<byte> associatedData)
    {
        if (!associatedData.IsEmpty)
            throw new ArgumentException(
                CryptoResourceStrings.Crypt_Invalid_SecretboxAssociatedData,
                nameof(associatedData));

        base.ProcessAssociatedData(associatedData);
    }

    /// <inheritdoc />
    protected override IStreamCipher CreateEngine() =>
        XSalsa20EngineFactory.Create(Key, Nonce);

    /// <inheritdoc />
    protected override int SealCore(
        IStreamCipher engine,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> plaintext,
        Span<byte> output) =>
        Poly1305AeadCore.SealSecretbox(engine, plaintext, output);

    /// <inheritdoc />
    protected override int OpenCore(
        IStreamCipher engine,
        ReadOnlySpan<byte> associatedData,
        ReadOnlySpan<byte> ciphertextWithTag,
        Span<byte> output) =>
        Poly1305AeadCore.OpenSecretbox(engine, ciphertextWithTag, output);
}
