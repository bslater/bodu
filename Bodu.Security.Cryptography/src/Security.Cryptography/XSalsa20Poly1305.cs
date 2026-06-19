// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides authenticated encryption using the XSalsa20-Poly1305 construction of NaCl / libsodium
/// <c>crypto_secretbox</c>, with the Bodu AEAD combined layout <c>ciphertext ‖ tag</c>. Accepts a 256-bit key and a
/// 192-bit nonce and produces a 128-bit authentication tag. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cryptographic body matches secretbox: a 256-bit subkey is derived from the key and the first 128 bits of the
/// nonce via HSalsa20, Salsa20 runs under that subkey with the trailing 64 bits of the nonce, the leading 32 bytes of
/// the counter-0 keystream block form the one-time <see cref="Poly1305" /> key, the message is encrypted with the
/// keystream from byte 32 onward, and the tag is computed over the ciphertext alone.
/// </para>
/// <para>
/// <strong>Wire format is not libsodium combined-mode.</strong> libsodium <c>crypto_secretbox_easy</c> emits
/// <c>tag ‖ ciphertext</c>; this type emits <c>ciphertext ‖ tag</c> to match the rest of the Bodu AEAD surface. The
/// ciphertext and tag bytes are identical — only the order differs. Use <see cref="ToLibsodiumCombined" /> /
/// <see cref="FromLibsodiumCombined" /> at the interop boundary to convert between the two layouts.
/// </para>
/// <para>
/// <strong>No associated data.</strong> The secretbox construction does not authenticate associated data; supplying
/// non-empty associated data to <see cref="IAeadTransform.Encrypt" /> or <see cref="IAeadTransform.Decrypt" /> throws
/// <see cref="ArgumentException" />. When associated-data support is required, use <see cref="XSalsa20Poly1305Aead" />
/// (XSalsa20 with RFC 8439 framing) or <see cref="XChaCha20Poly1305" />.
/// </para>
/// <para>
/// Each instance is single-use. A new instance must be created for every message. Reusing a nonce under the same key
/// destroys confidentiality and authenticity.
/// </para>
/// <para>
/// <strong>Key separation.</strong> Do not reuse a key across this construction, the raw <see cref="XSalsa20" /> stream
/// cipher, or any other AEAD construction unless an external key-separation scheme derives an independent key for each
/// use.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var enc = new XSalsa20Poly1305(key, nonce);
/// byte[] sealed_ = enc.Encrypt(plaintext); // ciphertext || tag, no associated data
/// using var dec = new XSalsa20Poly1305(key, nonce);
/// byte[] recovered = dec.Decrypt(sealed_);
///]]>
/// </code>
/// </example>
/// <seealso href="https://nacl.cr.yp.to/secretbox.html">NaCl crypto_secretbox</seealso> <seealso cref="XSalsa20" />
/// <seealso cref="XSalsa20Poly1305Aead" /> <seealso cref="XChaCha20Poly1305" /> <seealso cref="IStreamAeadTransform" />
public sealed class XSalsa20Poly1305
    : Poly1305AeadTransform
{
    /// <summary>Length of the XSalsa20-Poly1305 key is 256 bits (32 bytes).</summary>
    public const int KeySize = KeyBytes * 8;

    /// <summary>Length of the XSalsa20-Poly1305 nonce is 192 bits (24 bytes).</summary>
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

    /// <summary>
    /// Converts a Bodu combined output <c>ciphertext ‖ tag</c> into the libsodium combined layout
    /// <c>tag ‖ ciphertext</c>.
    /// </summary>
    /// <param name="ciphertextThenTag">The Bodu output: ciphertext followed by the 16-byte tag.</param>
    /// <param name="tagThenCiphertext">
    /// Receives the libsodium layout: the 16-byte tag followed by the ciphertext. May alias
    /// <paramref name="ciphertextThenTag" /> for an in-place conversion.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextThenTag" /> is shorter than the tag, or <paramref name="tagThenCiphertext" /> is too
    /// small.
    /// </exception>
    public static void ToLibsodiumCombined(ReadOnlySpan<byte> ciphertextThenTag, Span<byte> tagThenCiphertext)
    {
        int ciphertextLength = ValidateCombined(ciphertextThenTag, tagThenCiphertext);

        Span<byte> tag = stackalloc byte[TagBytes];
        ciphertextThenTag[ciphertextLength..].CopyTo(tag);
        ciphertextThenTag[..ciphertextLength].CopyTo(tagThenCiphertext[TagBytes..]);
        tag.CopyTo(tagThenCiphertext[..TagBytes]);
    }

    /// <summary>
    /// Converts a libsodium combined output <c>tag ‖ ciphertext</c> into the Bodu combined layout
    /// <c>ciphertext ‖ tag</c>.
    /// </summary>
    /// <param name="tagThenCiphertext">The libsodium output: the 16-byte tag followed by the ciphertext.</param>
    /// <param name="ciphertextThenTag">
    /// Receives the Bodu layout: the ciphertext followed by the 16-byte tag. May alias
    /// <paramref name="tagThenCiphertext" /> for an in-place conversion.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="tagThenCiphertext" /> is shorter than the tag, or <paramref name="ciphertextThenTag" /> is too
    /// small.
    /// </exception>
    public static void FromLibsodiumCombined(ReadOnlySpan<byte> tagThenCiphertext, Span<byte> ciphertextThenTag)
    {
        int ciphertextLength = ValidateCombined(tagThenCiphertext, ciphertextThenTag);

        Span<byte> tag = stackalloc byte[TagBytes];
        tagThenCiphertext[..TagBytes].CopyTo(tag);
        tagThenCiphertext[TagBytes..].CopyTo(ciphertextThenTag[..ciphertextLength]);
        tag.CopyTo(ciphertextThenTag[ciphertextLength..]);
    }

    /// <inheritdoc />
    protected override bool SupportsAssociatedData => false;

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

    /// <summary>
    /// Validates the source and destination buffers for a layout conversion and returns the ciphertext length.
    /// </summary>
    /// <param name="source">The combined input buffer (either layout).</param>
    /// <param name="destination">The combined output buffer (the other layout).</param>
    /// <returns>The ciphertext length, <c>source.Length - <see cref="Poly1305AeadTransform.TagBytes" /></c>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> is shorter than the tag, or <paramref name="destination" /> is too small.
    /// </exception>
    private static int ValidateCombined(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (source.Length < TagBytes)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_CiphertextTooShort, TagBytes),
                nameof(source));
        }

        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, source.Length);

        return source.Length - TagBytes;
    }
}
