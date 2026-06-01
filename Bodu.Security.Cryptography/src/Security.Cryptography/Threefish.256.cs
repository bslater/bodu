// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish.256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the <c>Threefish-256</c> tweakable symmetric block cipher, which operates on
/// 256-bit (32-byte) blocks using a 256-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is the tweakable block cipher underlying the Skein hash function. This variant supports a variety of
/// cipher block modes (CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode" /> property, and is suitable for
/// scenarios such as disk encryption or format-preserving encryption where a tweak is useful.
/// </para>
/// <para>
/// For other block sizes, see <see cref="Threefish512" /> and <see cref="Threefish1024" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Block size: 256 bits (32 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Key size: 256 bits (32 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Tweak size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Default mode: <see cref="CipherModeKind.CBC" />; default padding: <see cref="PaddingMode.PKCS7" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Threefish-256.</strong> Pick the 256-bit variant when you want a tweakable block cipher at a
/// comparable security margin to AES-256 with a smaller block than the wider Threefish variants — useful for per-record
/// encryption or short-tweak-driven schemes. Reach for <see cref="Threefish512" /> when a wider block reduces
/// birthday-bound exposure on long messages, or for <see cref="Threefish1024" /> when the surrounding construction
/// (e.g. a custom Skein-based AEAD) demands the widest block. For general-purpose encryption without a tweak
/// requirement, prefer <see cref="System.Security.Cryptography.Aes" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var tf = Threefish256.Create();
/// tf.Key = RandomNumberGenerator.GetBytes(32); // 256-bit key
/// tf.IV = RandomNumberGenerator.GetBytes(32); // matches the 256-bit block
/// tf.Tweak = RandomNumberGenerator.GetBytes(16); // 128-bit tweak
/// byte[] ciphertext = tf.Encrypt(plaintext);
/// byte[] roundTrip = tf.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/threefish-256.html">Using Threefish-256 (guide with full encrypt / decrypt
/// examples)</seealso> <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso> <seealso cref="Threefish"/>
/// <seealso cref="Threefish512"/> <seealso cref="Threefish1024"/> <seealso cref="Threefish256Cipher"/>
/// <seealso cref="Skein256"/>
public sealed class Threefish256
    : Threefish
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish256" /> class using a 256-bit block size, 256-bit key, and
    /// 128-bit tweak.
    /// </summary>
    public Threefish256()
        : base(256, 128) { }

    /// <summary>
    /// Creates a new <see cref="Threefish256" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Threefish256" /> instance.</returns>
    /// <remarks>
    /// The key, initialization vector, and tweak are generated on demand the first time they are accessed unless
    /// assigned explicitly via <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, or
    /// <see cref="TweakableSymmetricAlgorithm.Tweak" />.
    /// </remarks>
    public static new Threefish256 Create() => new();

    /// <inheritdoc />
    protected override ThreefishBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Threefish256Cipher(key, tweak);
}
