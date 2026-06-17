// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Hc128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the HC-128 stream cipher specified by Hongjun Wu. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// HC-128 is a software-oriented additive stream cipher and a member of the eSTREAM software portfolio. It uses a
/// 128-bit key and a 128-bit IV and drives two large secret tables to produce a keystream that is XORed with the
/// plaintext. This class exposes the <em>raw</em>, confidentiality-only cipher with no authentication; for new designs
/// prefer an AEAD construction.
/// </para>
/// <para>
/// Like the other stream ciphers in this library it is self-inverse:
/// <see cref="SymmetricStreamAlgorithm.CreateEncryptor()" /> and
/// <see cref="SymmetricStreamAlgorithm.CreateDecryptor()" /> are interchangeable. The IV is supplied through the
/// <see cref="SymmetricStreamAlgorithm.Nonce" /> property.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Key size: 128 bits (16 bytes).</description>
/// </item>
/// <item>
/// <description>Nonce (IV) size: 128 bits (16 bytes).</description>
/// </item>
/// </list>
/// <para>
/// <strong>Nonce reuse is catastrophic.</strong> A given <c>(key, IV)</c> pair must encrypt at most one message;
/// reusing it reveals the XOR of the plaintexts. HC-128 has a relatively expensive per-instance initialization (it
/// warms up two 512-word tables), so reuse one configured instance across a single message rather than re-creating it
/// per block.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var hc128 = new Hc128();
/// hc128.GenerateKey(); // 128-bit
/// hc128.GenerateNonce(); // 128-bit IV
/// byte[] ciphertext = hc128.Encrypt(plaintext);
/// byte[] roundTrip  = hc128.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://www.ecrypt.eu.org/stream/hc256pf.html">HC-128 (eSTREAM software portfolio)</seealso>
/// <seealso cref="ChaCha20" /> <seealso cref="Rabbit" />
public sealed class Hc128
    : SymmetricStreamAlgorithm
{
    /// <summary>The required HC-128 key size, in bits (128).</summary>
    internal const int KeySizeBits = Hc128StreamCipher.KeySizeBytes * 8;

    /// <summary>The HC-128 IV size, in bits (128).</summary>
    internal const int NonceSizeBits = Hc128StreamCipher.NonceSizeBytes * 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hc128" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 128-bit key and a 128-bit IV.
    /// </remarks>
    public Hc128()
        : base(KeySizeBits, NonceSizeBits)
    {
    }

    /// <summary>
    /// Creates a new <see cref="Hc128" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Hc128" /> instance.</returns>
    public static Hc128 Create() =>
        new();

    /// <inheritdoc />
    protected override IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce) =>
        new Hc128StreamCipher(key, nonce);
}
