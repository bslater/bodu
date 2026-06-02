// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305Ietf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides authenticated encryption with associated data (AEAD) using the extended-nonce XSalsa20 stream cipher with
/// the RFC 8439 Poly1305 framing. Accepts a 256-bit key and a 192-bit nonce and produces a 128-bit authentication tag.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This construction mirrors <see cref="XChaCha20Poly1305" /> but substitutes the <see cref="XSalsa20" /> keystream for
/// XChaCha20: a 256-bit subkey is derived from the key and the first 128 bits of the nonce via HSalsa20, and Salsa20
/// runs under that subkey with the trailing 64 bits of the nonce. The counter-0 keystream block yields the one-time
/// <see cref="Poly1305" /> key, the message is encrypted from counter 1 onward, and the tag authenticates
/// <c>AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)</c>.
/// </para>
/// <para>
/// <strong>Interoperability.</strong> Unlike <see cref="XSalsa20Poly1305" />, this type is <em>not</em> compatible with
/// NaCl / libsodium <c>crypto_secretbox</c>: it adds RFC 8439-style associated-data support and length framing that the
/// classic secretbox construction does not define. Choose this type when a consistent AEAD-with-AAD surface across the
/// XChaCha20 and XSalsa20 families is wanted; choose <see cref="XSalsa20Poly1305" /> for interoperability with existing
/// secretbox deployments.
/// </para>
/// <para>
/// Each instance is single-use. A new instance must be created for every message. Reusing a nonce under the same key
/// destroys confidentiality and authenticity. Call <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" />
/// before <see cref="IAeadBlockCipherModeTransform.Encrypt" /> or <see cref="IAeadBlockCipherModeTransform.Decrypt" />,
/// passing an empty span if there is no associated data.
/// </para>
/// </remarks>
/// <seealso href="https://cr.yp.to/snuffle/xsalsa-20081128.pdf">Extending the Salsa20 nonce (Bernstein, 2008)</seealso>
/// <seealso cref="XSalsa20" /> <seealso cref="XSalsa20Poly1305" /> <seealso cref="XChaCha20Poly1305" />
/// <seealso cref="IAeadBlockCipherModeTransform" />
public sealed class XSalsa20Poly1305Ietf
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
    /// Initializes a new instance of the <see cref="XSalsa20Poly1305Ietf" /> class with the specified key and nonce.
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
    public XSalsa20Poly1305Ietf(byte[] key, byte[] nonce)
        : this(
            key is null ? throw new ArgumentNullException(nameof(key)) : key.AsSpan(),
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : nonce.AsSpan())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XSalsa20Poly1305Ietf" /> class with the specified key and nonce
    /// spans.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">
    /// The 192-bit (24-byte) nonce. Must be unique for every message encrypted under the same key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly 32 bytes, or <paramref name="nonce" /> is not exactly 24 bytes.
    /// </exception>
    public XSalsa20Poly1305Ietf(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
        : base(key, nonce)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Derives the 256-bit Salsa20 subkey from the key and the first 128 bits of the nonce via HSalsa20, then returns a
    /// Salsa20 engine under that subkey with the trailing 64 bits of the extended nonce.
    /// </remarks>
    protected override IStreamCipher CreateEngine() =>
        XSalsa20EngineFactory.Create(Key, Nonce);
}
