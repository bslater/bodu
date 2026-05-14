// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the <c>Threefish-1024</c> tweakable symmetric block cipher, which operates on 1024-bit
/// (128-byte) blocks using a 1024-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is the tweakable block cipher underlying the Skein hash function. This variant supports a variety of cipher block modes
/// (CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode"/> property, and is suitable for scenarios such as disk encryption
/// or format-preserving encryption where a tweak is useful.
/// </para>
/// <para>For other block sizes, see <see cref="Threefish256"/> and <see cref="Threefish512"/>.</para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 1024 bits (128 bytes).</description></item>
///   <item><description>Key size: 1024 bits (128 bytes).</description></item>
///   <item><description>Tweak size: 128 bits (16 bytes).</description></item>
///   <item><description>Default mode: <see cref="CipherModeKind.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Threefish-1024.</strong> The widest Threefish variant — pick it when the surrounding
/// construction (Skein-1024, custom long-tweak schemes, or extreme-margin disk-encryption layouts) requires the
/// 1024-bit block. Throughput is lower than the 512-bit variant; the 256/512-bit variants are more practical
/// defaults unless the wider block is a hard requirement.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var tf = Threefish1024.Create();
/// tf.Key   = RandomNumberGenerator.GetBytes(128); // 1024-bit key
/// tf.IV    = RandomNumberGenerator.GetBytes(128); // matches the 1024-bit block
/// tf.Tweak = RandomNumberGenerator.GetBytes(16);  // 128-bit tweak
///
/// byte[] ciphertext = tf.Encrypt(plaintext);
/// byte[] roundTrip  = tf.Decrypt(ciphertext);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/threefish-1024.html">Using Threefish-1024 (guide with full encrypt / decrypt examples)</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
/// <seealso cref="Threefish{T}"/>
/// <seealso cref="Threefish256"/>
/// <seealso cref="Threefish512"/>
/// <seealso cref="Threefish1024Cipher"/>
/// <seealso cref="Skein1024"/>
public sealed class Threefish1024
    : Threefish
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish1024"/> class using a 1024-bit block size, 1024-bit key, and 128-bit tweak.
    /// </summary>
    public Threefish1024()
        : base(1024, 128) { }

    /// <summary>
    /// Creates a new <see cref="Threefish1024"/> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Threefish1024"/> instance.</returns>
    /// <remarks>
    /// The key, initialization vector, and tweak are generated on demand the first time they are accessed unless assigned explicitly
    /// via <see cref="SymmetricAlgorithm.Key"/>, <see cref="SymmetricAlgorithm.IV"/>, or <see cref="TweakableSymmetricAlgorithm.Tweak"/>.
    /// </remarks>
    public new static Threefish1024 Create()
    {
        return new Threefish1024();
    }

    /// <inheritdoc />
    protected override ThreefishBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Threefish1024Cipher(key, tweak);
}
