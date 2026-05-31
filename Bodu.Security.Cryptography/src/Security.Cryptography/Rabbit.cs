// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rabbit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Rabbit stream cipher specified by RFC 4503. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Rabbit is a high-speed additive stream cipher and an eSTREAM software-portfolio finalist. It uses a 128-bit key and a
/// 64-bit IV to drive an evolving internal state that emits a keystream XORed with the plaintext. This class exposes the
/// <em>raw</em>, confidentiality-only cipher with no authentication; for new designs prefer an AEAD construction.
/// </para>
/// <para>
/// Like the other stream ciphers in this library it is self-inverse:
/// <see cref="SymmetricStreamAlgorithm.CreateEncryptor()" /> and <see cref="SymmetricStreamAlgorithm.CreateDecryptor()" />
/// are interchangeable. The IV is supplied through the <see cref="SymmetricStreamAlgorithm.Nonce" /> property.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Key size: 128 bits (16 bytes).</description>
/// </item>
/// <item>
/// <description>Nonce (IV) size: 64 bits (8 bytes).</description>
/// </item>
/// </list>
/// <para>
/// <strong>Nonce reuse is catastrophic.</strong> A given <c>(key, IV)</c> pair must encrypt at most one message; reusing
/// it reveals the XOR of the plaintexts. Rabbit has no seekable counter, so a message is encrypted as one forward
/// keystream sequence.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var rabbit = new Rabbit();
/// rabbit.GenerateKey(); // 128-bit
/// rabbit.GenerateNonce(); // 64-bit IV
/// byte[] ciphertext = rabbit.Encrypt(plaintext);
/// byte[] roundTrip  = rabbit.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://www.rfc-editor.org/rfc/rfc4503">RFC 4503 — A Description of the Rabbit Stream Cipher
/// Algorithm</seealso>
/// <seealso cref="ChaCha20" />
/// <seealso cref="Salsa20" />
public sealed class Rabbit
    : SymmetricStreamAlgorithm
{
    /// <summary>
    /// The required Rabbit key size, in bits (128).
    /// </summary>
    internal const int KeySizeBits = RabbitStreamCipher.KeySizeBytes * 8;

    /// <summary>
    /// The Rabbit IV size, in bits (64).
    /// </summary>
    internal const int NonceSizeBits = RabbitStreamCipher.NonceSizeBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="Rabbit" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 128-bit key and a 64-bit IV.
    /// </remarks>
    public Rabbit()
        : base(KeySizeBits, NonceSizeBits)
    {
    }

    /// <summary>
    /// Creates a new <see cref="Rabbit" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Rabbit" /> instance.</returns>
    public static Rabbit Create() =>
        new();

    /// <inheritdoc />
    protected override IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce) =>
        new RabbitStreamCipher(key, nonce);
}
