// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the <c>Threefish-256</c> tweakable symmetric block cipher, which operates on 256-bit
/// (32-byte) blocks using a 256-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is the tweakable block cipher underlying the Skein hash function. This variant supports a variety of cipher block modes
/// (CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode" /> property, and is suitable for scenarios such as disk encryption
/// or format-preserving encryption where a tweak is useful.
/// </para>
/// <para>For other block sizes, see <see cref="Threefish512" /> and <see cref="Threefish1024" />.</para>
/// </remarks>
/// <seealso href="../guides/cryptography/threefish-256.html">Using Threefish-256 (guide with full encrypt / decrypt examples)</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Threefish256
    : Threefish
{
    /// <summary>
    /// Initialises a new instance of the <see cref="Threefish256" /> class using a 256-bit block size, 256-bit key, and 128-bit tweak.
    /// </summary>
    public Threefish256()
        : base(256, 128) { }

    /// <summary>
    /// Creates a new <see cref="Threefish256" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Threefish256" /> instance.</returns>
    /// <remarks>
    /// The key, initialisation vector, and tweak are generated on demand the first time they are accessed unless assigned explicitly
    /// via <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, or <see cref="TweakableSymmetricAlgorithm.Tweak" />.
    /// </remarks>
    public new static Threefish256 Create()
    {
        return new Threefish256();
    }

    /// <inheritdoc />
    protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Threefish256Cipher(key, tweak);
}
