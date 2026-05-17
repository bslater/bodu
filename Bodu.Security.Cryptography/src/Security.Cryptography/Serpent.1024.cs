// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the non-standard wide-block tweakable <c>Serpent-1024</c> symmetric block
/// cipher, which operates on 1024-bit (128-byte) blocks using a 1024-bit key and a 128-bit tweak. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// This variant runs the Serpent round function over a thirty-two-word state for 80 rounds, injecting a tweak subkey
/// every four rounds in the style of Threefish. It supports the extended block cipher modes exposed by
/// <see cref="CipherModeKind" /> via the <see cref="Serpent.BlockMode" /> property.
/// </para>
/// <para>
/// For other block sizes, see <see cref="Serpent256" /> and <see cref="Serpent512" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Block size: 1024 bits (128 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Key size: 1024 bits (128 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Tweak size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// 80 rounds; tweak subkey injected every 4 rounds.
/// </description>
/// </item>
/// <item>
/// <description>
/// Default mode: <see cref="CipherModeKind.CBC" />; default padding: <see cref="PaddingMode.PKCS7" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Serpent-1024.</strong> The widest wide-block Serpent variant — purely experimental, for
/// scenarios where a 1024-bit tweakable block with Serpent's round function is required. <see cref="Threefish1024" />
/// is the better-studied alternative. Throughput drops markedly relative to the 256/512-bit Serpent variants. Use
/// <see cref="Serpent128" /> for any production scenario that requires interoperable Serpent.
/// </para>
/// <note type="important"> Serpent-1024 (this type) is a <strong>non-standard Serpent-derived construction</strong> and
/// is not interoperable with any reference Serpent implementation. For standard, externally vetted Serpent, use
/// <see cref="Serpent128" />. </note>
/// </remarks>
/// <example>
/// <code language="csharp"> using System.Security.Cryptography; using Bodu.Security.Cryptography; using
/// Bodu.Security.Cryptography.Extensions; using var serpent = Serpent1024.Create(); serpent.Key =
/// RandomNumberGenerator.GetBytes(128); // 1024-bit key serpent.IV = RandomNumberGenerator.GetBytes(128); // matches
/// the 1024-bit block serpent.Tweak = RandomNumberGenerator.GetBytes(16); // 128-bit tweak byte[] ciphertext =
/// serpent.Encrypt(plaintext); byte[] roundTrip = serpent.Decrypt(ciphertext); </code>
/// </example>
public sealed class Serpent1024
    : Serpent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent1024" /> class using a 1024-bit block size, 1024-bit key,
    /// and 128-bit tweak.
    /// </summary>
    public Serpent1024()
        : base(1024, 128) { }

    /// <summary>
    /// Creates a new <see cref="Serpent1024" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Serpent1024" /> instance.</returns>
    /// <remarks>
    /// The key, initialization vector, and tweak are generated on demand the first time they are accessed unless
    /// assigned explicitly via <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, or
    /// <see cref="TweakableSymmetricAlgorithm.Tweak" />.
    /// </remarks>
    public new static Serpent1024 Create() => new Serpent1024();

    /// <inheritdoc />
    protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Serpent1024Cipher(key, tweak);
}
