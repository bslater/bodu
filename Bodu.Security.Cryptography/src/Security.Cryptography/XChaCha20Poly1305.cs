// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20Poly1305.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides authenticated encryption with associated data (AEAD) using the extended-nonce <c>XChaCha20-Poly1305</c>
/// construction. Accepts a 256-bit key and a 192-bit nonce and produces a 128-bit authentication tag. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// <para>
/// XChaCha20-Poly1305 composes the extended-nonce <see cref="XChaCha20" /> stream cipher with the one-time
/// <see cref="Poly1305" /> MAC under the RFC 8439 AEAD framing, as specified by <c>draft-irtf-cfrg-xchacha</c> and
/// implemented by libsodium's <c>crypto_aead_xchacha20poly1305_ietf</c>. A 256-bit subkey is derived from the key and
/// the first 128 bits of the nonce via HChaCha20; the remaining 64 bits of the nonce, prefixed with four zero bytes,
/// form the 96-bit ChaCha20 nonce. The counter-0 keystream block yields the one-time Poly1305 key, the message is
/// encrypted from counter 1 onward, and the tag authenticates
/// <c>AAD ‖ pad16(AAD) ‖ ciphertext ‖ pad16(ciphertext) ‖ le64(|AAD|) ‖ le64(|ciphertext|)</c>.
/// </para>
/// <para>
/// The 192-bit nonce is large enough to be chosen at random per message without meaningful collision risk, which makes
/// XChaCha20-Poly1305 the safer choice for protocols that cannot guarantee a unique 96-bit nonce — the gap left by the
/// BCL's 96-bit-nonce <see cref="System.Security.Cryptography.ChaCha20Poly1305" />.
/// </para>
/// <para>
/// Each instance is single-use. A new instance must be created for every message. Reusing a nonce under the same key
/// destroys confidentiality and authenticity. Call <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" />
/// before <see cref="IAeadBlockCipherModeTransform.Encrypt" /> or <see cref="IAeadBlockCipherModeTransform.Decrypt" />,
/// passing an empty span if there is no associated data.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// // The convenience helpers size the output buffer and return a single freshly allocated array.
/// using IAeadBlockCipherModeTransform enc = new XChaCha20Poly1305(key, nonce);
/// byte[] sealed_ = enc.Encrypt(plaintext, associatedData: header);
/// using IAeadBlockCipherModeTransform dec = new XChaCha20Poly1305(key, nonce);
/// byte[] recovered = dec.Decrypt(sealed_, associatedData: header);
///]]>
/// </code>
/// </example>
/// <seealso href="https://datatracker.ietf.org/doc/html/draft-irtf-cfrg-xchacha">draft-irtf-cfrg-xchacha — XChaCha:
/// eXtended-nonce ChaCha and AEAD_XChaCha20_Poly1305</seealso> <seealso cref="XChaCha20" /> <seealso cref="Poly1305" />
/// <seealso cref="IAeadBlockCipherModeTransform" />
public sealed class XChaCha20Poly1305
    : Poly1305AeadTransform
{
    /// <summary>
    /// Length of the XChaCha20-Poly1305 key is 256 bits (32 bytes).
    /// </summary>
    public const int KeySize = KeyBytes * 8;

    /// <summary>
    /// Length of the XChaCha20-Poly1305 nonce is 192 bits (24 bytes).
    /// </summary>
    public const int NonceSize = NonceBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="XChaCha20Poly1305" /> class with the specified key and nonce.
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
    public XChaCha20Poly1305(byte[] key, byte[] nonce)
        : this(
            key is null ? throw new ArgumentNullException(nameof(key)) : key.AsSpan(),
            nonce is null ? throw new ArgumentNullException(nameof(nonce)) : nonce.AsSpan())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XChaCha20Poly1305" /> class with the specified key and nonce spans.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) secret key.</param>
    /// <param name="nonce">
    /// The 192-bit (24-byte) nonce. Must be unique for every message encrypted under the same key.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not exactly 32 bytes, or <paramref name="nonce" /> is not exactly 24 bytes.
    /// </exception>
    public XChaCha20Poly1305(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
        : base(key, nonce)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// Derives the 256-bit ChaCha20 subkey from the key and the first 128 bits of the nonce via HChaCha20, then returns
    /// a ChaCha20 engine under that subkey with a 96-bit nonce formed as four zero bytes followed by the trailing 64
    /// bits of the extended nonce.
    /// </remarks>
    protected override IStreamCipher CreateEngine()
    {
        Span<byte> subkey = stackalloc byte[ChaCha20StreamCipher.KeySizeBytes];
        Span<byte> chachaNonce = stackalloc byte[ChaCha20StreamCipher.NonceSizeBytes];

        try
        {
            ChaCha20StreamCipher.HChaCha20(Key, Nonce[..ChaCha20StreamCipher.HChaChaNonceSizeBytes], subkey);
            Nonce.Slice(ChaCha20StreamCipher.HChaChaNonceSizeBytes, 8).CopyTo(chachaNonce[4..]);

            return new ChaCha20StreamCipher(subkey, chachaNonce, initialCounter: 0);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(subkey);
        }
    }
}
