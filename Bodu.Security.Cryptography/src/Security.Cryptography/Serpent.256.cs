// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the non-standard wide-block tweakable <c>Serpent-256</c> symmetric block cipher, which
/// operates on 256-bit (32-byte) blocks using a 256-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This variant runs the Serpent round function over an eight-word state for 48 rounds, injecting a tweak subkey every four
/// rounds in the style of Threefish. It supports the extended block cipher modes exposed by <see cref="CipherBlockMode" /> via
/// the <see cref="Serpent.BlockMode" /> property.
/// </para>
/// <para>
/// For other block sizes, see <see cref="Serpent512" /> and <see cref="Serpent1024" />.
/// </para>
/// <note type="important">
/// Serpent-256 (this type) is a **non-standard Serpent-derived construction** and is not interoperable with any reference
/// Serpent implementation. For standard, externally vetted Serpent, use <see cref="Serpent128" />.
/// </note>
/// </remarks>
public sealed class Serpent256
    : Serpent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent256" /> class using a 256-bit block size, 256-bit key, and 128-bit
    /// tweak.
    /// </summary>
    public Serpent256()
        : base(256, 128) { }

    /// <summary>
    /// Creates a new <see cref="Serpent256" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Serpent256" /> instance.</returns>
    /// <remarks>
    /// The key, initialisation vector, and tweak are generated on demand the first time they are accessed unless assigned
    /// explicitly via <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, or
    /// <see cref="TweakableSymmetricAlgorithm.Tweak" />.
    /// </remarks>
    public new static Serpent256 Create() => new Serpent256();

    /// <inheritdoc />
    protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Serpent256Cipher(key, tweak);
}
