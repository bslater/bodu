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
/// rounds in the style of Threefish. It supports the extended block cipher modes exposed by <see cref="CipherModeKind"/> via
/// the <see cref="Serpent.BlockMode"/> property.
/// </para>
/// <para>
/// For other block sizes, see <see cref="Serpent512"/> and <see cref="Serpent1024"/>.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 256 bits (32 bytes).</description></item>
///   <item><description>Key size: 256 bits (32 bytes).</description></item>
///   <item><description>Tweak size: 128 bits (16 bytes).</description></item>
///   <item><description>48 rounds; tweak subkey injected every 4 rounds.</description></item>
///   <item><description>Default mode: <see cref="CipherModeKind.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Serpent-256.</strong> Pick the wide-block Serpent variants only for experimental work
/// where a tweakable wide-block cipher with Serpent's round function is wanted. <see cref="Threefish256"/> is a
/// better-studied alternative for the same role. Use <see cref="Serpent128"/> for any production scenario that
/// requires interoperable Serpent.
/// </para>
/// <note type="important">
/// Serpent-256 (this type) is a <strong>non-standard Serpent-derived construction</strong> and is not interoperable with any
/// reference Serpent implementation. For standard, externally vetted Serpent, use <see cref="Serpent128"/>.
/// </note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var serpent = Serpent256.Create();
/// serpent.Key   = RandomNumberGenerator.GetBytes(32); // 256-bit key
/// serpent.IV    = RandomNumberGenerator.GetBytes(32); // matches the 256-bit block
/// serpent.Tweak = RandomNumberGenerator.GetBytes(16); // 128-bit tweak
///
/// byte[] ciphertext = serpent.Encrypt(plaintext);
/// byte[] roundTrip  = serpent.Decrypt(ciphertext);
/// </code>
/// </example>
public sealed class Serpent256
    : Serpent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent256"/> class using a 256-bit block size, 256-bit key, and 128-bit
    /// tweak.
    /// </summary>
    public Serpent256()
        : base(256, 128) { }

    /// <summary>
    /// Creates a new <see cref="Serpent256"/> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Serpent256"/> instance.</returns>
    /// <remarks>
    /// The key, initialization vector, and tweak are generated on demand the first time they are accessed unless assigned
    /// explicitly via <see cref="SymmetricAlgorithm.Key"/>, <see cref="SymmetricAlgorithm.IV"/>, or
    /// <see cref="TweakableSymmetricAlgorithm.Tweak"/>.
    /// </remarks>
    public new static Serpent256 Create() => new Serpent256();

    /// <inheritdoc />
    protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Serpent256Cipher(key, tweak);
}
