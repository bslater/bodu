// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305Aead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides authenticated encryption with associated data (AEAD) using the XSalsa20 stream cipher with the RFC 8439
/// Poly1305 framing. Accepts a 256-bit key and a 192-bit nonce and produces a 128-bit authentication tag. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Bodu-defined construction.</strong> This is <em>not</em> an IETF-standardised AEAD: there is no RFC, no IETF
/// registry assignment, and no published interoperability vector for XSalsa20 combined with the RFC 8439 Poly1305
/// framing. It is also <em>not</em> wire-compatible with NaCl / libsodium <c>crypto_secretbox</c>. Use it only when
/// both peers are Bodu, or when a protocol explicitly specifies this hybrid. For interoperable choices use
/// <see cref="XChaCha20Poly1305" /> (AEAD with associated data) or <see cref="XSalsa20Poly1305" /> (secretbox).
/// </para>
/// <para>
/// The construction mirrors <see cref="XChaCha20Poly1305" /> but substitutes the <see cref="XSalsa20" /> keystream: a
/// 256-bit subkey is derived from the key and the first 128 bits of the nonce via HSalsa20, and Salsa20 runs under that
/// subkey with the trailing 64 bits of the nonce. The counter-0 keystream block yields the one-time
/// <see cref="Poly1305" /> key, the message is encrypted from counter 1 onward, and the tag authenticates
/// <c>AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)</c>.
/// </para>
/// <para>
/// Each instance is single-use. A new instance must be created for every message. Reusing a nonce under the same key
/// destroys confidentiality and authenticity. Associated data is passed directly to
/// <see cref="IAeadTransform.Encrypt" /> or <see cref="IAeadTransform.Decrypt" /> and defaults to empty; the emitted
/// wire format is <c>ciphertext ‖ tag</c>.
/// </para>
/// <para>
/// <strong>Key separation.</strong> Do not reuse a key across this construction, the raw <see cref="XSalsa20" /> stream
/// cipher, the <see cref="XSalsa20Poly1305" /> secretbox, or any other AEAD construction unless an external key-
/// separation scheme derives an independent key for each use.
/// </para>
/// </remarks>
/// <seealso href="https://cr.yp.to/snuffle/xsalsa-20081128.pdf">Extending the Salsa20 nonce (Bernstein, 2008)</seealso>
/// <seealso cref="XSalsa20" /> <seealso cref="XSalsa20Poly1305" /> <seealso cref="XChaCha20Poly1305" />
/// <seealso cref="IStreamAeadTransform" />
public sealed class XSalsa20Poly1305Aead
    : Poly1305AeadTransform
{
    /// <summary>Length of the XSalsa20-Poly1305 key is 256 bits (32 bytes).</summary>
    public const int KeySize = KeyBytes * 8;

    /// <summary>Length of the XSalsa20-Poly1305 nonce is 192 bits (24 bytes).</summary>
    public const int NonceSize = NonceBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="XSalsa20Poly1305Aead" /> class with the specified key and nonce.
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
    public XSalsa20Poly1305Aead(byte[] key, byte[] nonce)
        : this(
            key is null ? throw new ArgumentNullException(nameof(key)) : key.AsSpan(),
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : nonce.AsSpan())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XSalsa20Poly1305Aead" /> class with the specified key and nonce
    /// spans.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">
    /// The 192-bit (24-byte) nonce. Must be unique for every message encrypted under the same key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly 32 bytes, or <paramref name="nonce" /> is not exactly 24 bytes.
    /// </exception>
    public XSalsa20Poly1305Aead(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
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
