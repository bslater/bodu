// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the raw ChaCha20 stream cipher defined by RFC 8439. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// ChaCha20 is an additive stream cipher designed by Daniel J. Bernstein. It uses a 256-bit key, a 96-bit nonce, and a
/// 32-bit block counter to generate a keystream that is XORed with the plaintext. This class exposes the <em>raw</em>
/// keystream cipher — confidentiality only, with no authentication. For authenticated encryption use the BCL's
/// <see cref="System.Security.Cryptography.ChaCha20Poly1305" />; the raw cipher exists for libsodium-, Noise-, and
/// age-style protocols that build their own authentication layer or use ChaCha20 as a keystream primitive.
/// </para>
/// <para>
/// Like the BCL stream constructions, this cipher is self-inverse:
/// <see cref="SymmetricAlgorithm.CreateEncryptor()" /> and <see cref="SymmetricAlgorithm.CreateDecryptor()" /> are
/// interchangeable. The nonce is supplied as the <see cref="SymmetricAlgorithm.IV" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Key size: 256 bits (32 bytes).</description>
/// </item>
/// <item>
/// <description>Nonce (IV) size: 96 bits (12 bytes).</description>
/// </item>
/// <item>
/// <description>Block counter: 32-bit, starting at <see cref="InitialCounter" /> (default 0).</description>
/// </item>
/// </list>
/// <para>
/// <strong>Nonce reuse is catastrophic.</strong> A given <c>(key, nonce)</c> pair must encrypt at most one message. The
/// 96-bit nonce is too short to generate randomly at high volume without collision risk; for random nonces prefer the
/// extended-nonce <see cref="XChaCha20" /> variant. The cipher tracks the 32-bit counter and throws
/// <see cref="CryptographicException" /> rather than reuse keystream if it would overflow (after roughly 256 GiB under
/// a single nonce).
/// </para>
/// <para>
/// <strong>Note on <see cref="SymmetricAlgorithm.BlockSize" />.</strong> A stream cipher has no block. To integrate
/// with the <see cref="SymmetricAlgorithm" /> contract this class reports its block size as the 96-bit nonce length so
/// that <see cref="SymmetricAlgorithm.IV" /> handling and <see cref="SymmetricAlgorithm.GenerateIV" /> behave naturally.
/// The actual transform processes data one byte at a time and imposes no alignment requirement on callers.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var chacha = new ChaCha20();
/// chacha.GenerateKey(); // 256-bit
/// chacha.GenerateIV();  // 96-bit nonce
/// byte[] ciphertext = chacha.Encrypt(plaintext);
/// byte[] roundTrip  = chacha.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc8439">RFC 8439 — ChaCha20 and Poly1305 for IETF
/// Protocols</seealso>
/// <seealso cref="XChaCha20" />
public sealed class ChaCha20
    : StreamCipherAlgorithm
{
    /// <summary>
    /// The required ChaCha20 key size, in bits (256).
    /// </summary>
    internal const int KeySizeBits = ChaCha20StreamCipher.KeySizeBytes * 8;

    /// <summary>
    /// The ChaCha20 nonce size, in bits (96).
    /// </summary>
    internal const int NonceSizeBits = ChaCha20StreamCipher.NonceSizeBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaCha20" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 256-bit key and a 96-bit nonce, with the block counter starting at 0.
    /// </remarks>
    public ChaCha20()
        : base(KeySizeBits, NonceSizeBits)
    {
    }

    /// <summary>
    /// Gets or sets the initial 32-bit block counter used when generating the keystream.
    /// </summary>
    /// <value>The starting block-counter value. The default is 0.</value>
    /// <returns>The block-counter value applied to the first keystream block.</returns>
    /// <remarks>
    /// RFC 8439 keystream examples start the counter at 0 or 1 depending on the construction; libsodium's raw
    /// <c>crypto_stream_chacha20_ietf</c> starts at 0. Set this before creating an encryptor or decryptor when matching
    /// an external counter convention.
    /// </remarks>
    public uint InitialCounter { get; set; }

    /// <summary>
    /// Creates a new <see cref="ChaCha20" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="ChaCha20" /> instance.</returns>
    public static new ChaCha20 Create() =>
        new();

    /// <inheritdoc />
    protected override IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce) =>
        new ChaCha20StreamCipher(key, nonce, InitialCounter);
}
