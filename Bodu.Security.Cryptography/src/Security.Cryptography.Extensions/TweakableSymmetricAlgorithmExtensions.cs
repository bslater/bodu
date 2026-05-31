// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Extends <see cref="Bodu.Security.Cryptography.TweakableSymmetricAlgorithm" /> with try-pattern transform creation
/// that surfaces invalid key, IV, or tweak material as a <see langword="false" /> return rather than a thrown
/// <see cref="System.Security.Cryptography.CryptographicException" />.
/// </summary>
/// <remarks>
/// <para>
/// Tweakable block ciphers — Threefish in this library, and tweakable variants used as building blocks for AEAD
/// constructions — accept a third keying input alongside the key and IV: the <em>tweak</em>. The tweak is a per-message
/// value that varies the cipher's behavior without renegotiating a key, making it a natural fit for disk encryption,
/// authenticated modes, and protocols that bind ciphertext to a position or message identifier. Constructing an
/// encryptor or decryptor for one of these algorithms requires all three values to be valid in concert; the BCL-style
/// approach is to throw on the first invalid value, which is awkward when the values are user-supplied.
/// </para>
/// <para>
/// The API surface is intentionally narrow and parallels
/// <see cref="SymmetricAlgorithmExtensions.TryCreateEncryptor(System.Security.Cryptography.SymmetricAlgorithm, byte[], byte[], out System.Security.Cryptography.ICryptoTransform)" />
/// :
/// </para>
/// <list type="bullet">
/// <item>
/// <term><c>TryCreateEncryptor</c></term>
/// <description>
/// Wraps <see cref="Bodu.Security.Cryptography.TweakableSymmetricAlgorithm.CreateEncryptor()" /> and the explicit
/// <c>(key, iv, tweak)</c> overload, returning <see langword="false" /> on any cryptographic validation failure.
/// </description>
/// </item>
/// <item>
/// <term><c>TryCreateDecryptor</c></term>
/// <description>
/// The matching pair for <see cref="Bodu.Security.Cryptography.TweakableSymmetricAlgorithm.CreateDecryptor()" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// All four overloads validate <c>algorithm</c> eagerly and throw <see cref="System.ArgumentNullException" /> when it
/// is <see langword="null" />; <em>only</em> cryptographic failures from the underlying transform are converted to a
/// <see langword="false" /> return. The returned <see cref="System.Security.Cryptography.ICryptoTransform" /> is owned
/// by the caller and must be disposed.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using TweakableSymmetricAlgorithm tf = new Threefish256();
///
/// 1. Validate user-supplied key/IV/tweak without catching CryptographicException.
/// if (!tf.TryCreateEncryptor(suppliedKey, suppliedIv, suppliedTweak, out ICryptoTransform? encryptor))
///     return BadRequest("invalid key, iv, or tweak");
/// using (encryptor) { /* … encrypt with the validated transform … */ }
///
/// 2. Round-trip pairing — same try-pattern shape for the decryptor.
/// if (tf.TryCreateDecryptor(suppliedKey, suppliedIv, suppliedTweak, out ICryptoTransform? decryptor))
/// {
///     using (decryptor) { /* … decrypt … */ }
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class TweakableSymmetricAlgorithmExtensions
{
}
