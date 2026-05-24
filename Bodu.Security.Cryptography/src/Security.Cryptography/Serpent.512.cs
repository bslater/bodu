// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent.512.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the non-standard wide-block tweakable <c>Serpent-512</c> symmetric block
/// cipher, which operates on 512-bit (64-byte) blocks using a 512-bit key and a 128-bit tweak. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// This variant runs the Serpent round function over a sixteen-word state for 64 rounds, injecting a tweak subkey every
/// four rounds in the style of Threefish. It supports the extended block cipher modes exposed by
/// <see cref="CipherModeKind" /> via the <see cref="Serpent.BlockMode" /> property.
/// </para>
/// <para>
/// For other block sizes, see <see cref="Serpent256" /> and <see cref="Serpent1024" />.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Block size: 512 bits (64 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Key size: 512 bits (64 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Tweak size: 128 bits (16 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// 64 rounds; tweak subkey injected every 4 rounds.
/// </description>
/// </item>
/// <item>
/// <description>
/// Default mode: <see cref="CipherModeKind.CBC" />; default padding: <see cref="PaddingMode.PKCS7" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Serpent-512.</strong> Pick the wide-block Serpent variants only for experimental work where a
/// tweakable wide-block cipher with Serpent's round function is wanted. <see cref="Threefish512" /> is a better-studied
/// alternative for the same role. Use <see cref="Serpent128" /> for any production scenario that requires interoperable
/// Serpent.
/// </para>
/// <note type="important"> Serpent-512 (this type) is a <strong>non-standard Serpent-derived construction</strong> and
/// is not interoperable with any reference Serpent implementation. For standard, externally vetted Serpent, use
/// <see cref="Serpent128" />. </note>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var serpent = Serpent512.Create();
/// serpent.Key = RandomNumberGenerator.GetBytes(64); // 512-bit key
/// serpent.IV = RandomNumberGenerator.GetBytes(64); // matches the 512-bit block
/// serpent.Tweak = RandomNumberGenerator.GetBytes(16); // 128-bit tweak
/// byte[] ciphertext = serpent.Encrypt(plaintext);
/// byte[] roundTrip = serpent.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
public sealed class Serpent512
    : Serpent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent512" /> class using a 512-bit block size, 512-bit key, and
    /// 128-bit tweak.
    /// </summary>
    public Serpent512()
        : base(512, 128) { }

    /// <summary>
    /// Creates a new <see cref="Serpent512" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Serpent512" /> instance.</returns>
    /// <remarks>
    /// The key, initialization vector, and tweak are generated on demand the first time they are accessed unless
    /// assigned explicitly via <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, or
    /// <see cref="TweakableSymmetricAlgorithm.Tweak" />.
    /// </remarks>
    public static new Serpent512 Create() => new();

    /// <inheritdoc />
    protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
        new Serpent512Cipher(key, tweak);
}
