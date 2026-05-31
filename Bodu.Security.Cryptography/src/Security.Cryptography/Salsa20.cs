// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Salsa20 stream cipher specified by Daniel J. Bernstein. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Salsa20 is an additive stream cipher and an eSTREAM software-portfolio finalist. It uses a 128- or 256-bit key, a
/// 64-bit nonce, and a 64-bit block counter to generate a keystream that is XORed with the plaintext. This class
/// exposes the <em>raw</em>, confidentiality-only cipher with no authentication; for new designs prefer an AEAD
/// construction, or the closely related <see cref="ChaCha20" /> which has largely superseded Salsa20.
/// </para>
/// <para>
/// Like the other stream ciphers in this library it is self-inverse:
/// <see cref="SymmetricStreamAlgorithm.CreateEncryptor()" /> and
/// <see cref="SymmetricStreamAlgorithm.CreateDecryptor()" /> are interchangeable. The nonce is supplied through the
/// <see cref="SymmetricStreamAlgorithm.Nonce" /> property.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Key size: 128 or 256 bits (16 or 32 bytes); 256-bit is the default.
/// </description>
/// </item>
/// <item>
/// <description>
/// Nonce (IV) size: 64 bits (8 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Block counter: 64-bit, starting at <see cref="InitialCounter" /> (default 0).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Nonce reuse is catastrophic.</strong> A given <c>(key, nonce)</c> pair must encrypt at most one message. The
/// 64-bit nonce is too short to generate randomly without collision risk; for random nonces prefer the extended-nonce
/// <see cref="XSalsa20" /> variant.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var salsa = new Salsa20();
/// salsa.GenerateKey(); // 256-bit
/// salsa.GenerateNonce(); // 64-bit nonce
/// byte[] ciphertext = salsa.Encrypt(plaintext);
/// byte[] roundTrip  = salsa.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="https://cr.yp.to/snuffle/spec.pdf">Salsa20 specification (Bernstein, 2005)</seealso>
/// <seealso cref="XSalsa20" /> <seealso cref="ChaCha20" />
public sealed class Salsa20
    : SymmetricStreamAlgorithm
{
    /// <summary>
    /// The default Salsa20 key size, in bits (256).
    /// </summary>
    internal const int DefaultKeySizeBits = Salsa20StreamCipher.KeySize256Bytes * 8;

    /// <summary>
    /// The Salsa20 nonce size, in bits (64).
    /// </summary>
    internal const int NonceSizeBits = Salsa20StreamCipher.NonceSizeBytes * 8;

    private static readonly KeySizes[] s_keySizes = [new KeySizes(128, 256, 128)];

    /// <summary>
    /// Initializes a new instance of the <see cref="Salsa20" /> class with default parameters.
    /// </summary>
    /// <remarks>
    /// The default configuration uses a 256-bit key and a 64-bit nonce, with the block counter starting at 0. Assign
    /// <see cref="SymmetricStreamAlgorithm.KeySize" /> to 128 before generating or supplying a key to use a 128-bit
    /// key.
    /// </remarks>
    public Salsa20()
        : base(DefaultKeySizeBits, s_keySizes, NonceSizeBits)
    {
    }

    /// <summary>
    /// Gets or sets the initial 64-bit block counter used when generating the keystream.
    /// </summary>
    /// <value>The starting block-counter value. The default is 0.</value>
    /// <returns>The block-counter value applied to the first keystream block.</returns>
    /// <remarks>
    /// eSTREAM test vectors start the counter at 0. Set this before creating an encryptor or decryptor when matching an
    /// external counter convention.
    /// </remarks>
    public ulong InitialCounter { get; set; }

    /// <summary>
    /// Creates a new <see cref="Salsa20" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Salsa20" /> instance.</returns>
    public static Salsa20 Create() =>
        new();

    /// <inheritdoc />
    protected override IStreamCipher CreateStreamCipher(byte[] key, byte[] nonce) =>
        new Salsa20StreamCipher(key, nonce, InitialCounter);
}
