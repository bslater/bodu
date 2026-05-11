// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish.512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the <c>Threefish-512</c> tweakable symmetric block cipher, which operates on 512-bit
/// (64-byte) blocks using a 512-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is the tweakable block cipher underlying the Skein hash function. This variant supports a variety of cipher block modes
/// (CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode"/> property, and is suitable for scenarios such as disk encryption
/// or format-preserving encryption where a tweak is useful.
/// </para>
/// <para>For other block sizes, see <see cref="Threefish256"/> and <see cref="Threefish1024"/>.</para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 512 bits (64 bytes).</description></item>
///   <item><description>Key size: 512 bits (64 bytes).</description></item>
///   <item><description>Tweak size: 128 bits (16 bytes).</description></item>
///   <item><description>Default mode: <see cref="CipherBlockMode.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Threefish-512.</strong> The most common Threefish width — the wider 512-bit block lifts
/// the birthday bound to <c>~2<sup>256</sup></c> blocks, removing the SWEET32-style concerns that 64-bit and 128-bit
/// block ciphers run into when re-used heavily under one key. A natural fit when building Skein-style constructions
/// (Skein-512 wraps this engine under the UBI mode). Use <see cref="Threefish256"/> when the smaller block matches
/// the surrounding scheme; use <see cref="Threefish1024"/> when even more block width is required.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var tf = Threefish512.Create();
/// tf.Key   = RandomNumberGenerator.GetBytes(64); // 512-bit key
/// tf.IV    = RandomNumberGenerator.GetBytes(64); // matches the 512-bit block
/// tf.Tweak = RandomNumberGenerator.GetBytes(16); // 128-bit tweak
///
/// byte[] ciphertext = tf.Encrypt(plaintext);
/// byte[] roundTrip  = tf.Decrypt(ciphertext);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/threefish-512.html">Using Threefish-512 (guide with full encrypt / decrypt examples)</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Threefish512
    : Threefish
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish512"/> class using a 512-bit block size, 512-bit key, and 128-bit tweak.
    /// </summary>
    public Threefish512()
        : base(512, 128) { }

    /// <summary>
    /// Creates a new <see cref="Threefish512"/> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Threefish512"/> instance.</returns>
    /// <remarks>
    /// The key, initialisation vector, and tweak are generated on demand the first time they are accessed unless assigned explicitly
    /// via <see cref="SymmetricAlgorithm.Key"/>, <see cref="SymmetricAlgorithm.IV"/>, or <see cref="TweakableSymmetricAlgorithm.Tweak"/>.
    /// </remarks>
    public new static Threefish512 Create()
    {
        return new Threefish512();
    }

    /// <inheritdoc />
    protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Threefish512Cipher(key, tweak);
}
