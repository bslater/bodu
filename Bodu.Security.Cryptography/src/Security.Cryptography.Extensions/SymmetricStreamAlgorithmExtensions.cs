// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Extends <see cref="SymmetricStreamAlgorithm" /> with one-shot encrypt/decrypt of buffers and stream-to-stream
/// pipelines, mirroring the convenience surface offered for block ciphers.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SymmetricStreamAlgorithm" /> exposes the building blocks — <c>CreateEncryptor</c>, <c>CreateDecryptor</c>,
/// and <see cref="System.Security.Cryptography.CryptoStream" /> interop — but stops short of the operations callers
/// actually invoke: "encrypt this byte array" or "decrypt this stream into that one". This class collapses the usual
/// setup (instantiate the transform, drain the source, dispose in order) into a single call per scenario, with overloads
/// aligned to the input shape.
/// </para>
/// <para>
/// Because an additive stream cipher is self-inverse, <c>Encrypt</c> and <c>Decrypt</c> perform the identical XOR
/// operation; the two names exist purely to make calling code read naturally. Every overload uses the algorithm's
/// current <see cref="SymmetricStreamAlgorithm.Key" /> and <see cref="SymmetricStreamAlgorithm.Nonce" />. Streams are not
/// disposed by these methods, and <see cref="SymmetricStreamAlgorithm" /> instances are not thread-safe; share them only
/// behind explicit synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var chacha = new ChaCha20();
/// chacha.GenerateKey();
/// chacha.GenerateNonce();
///
/// byte[] ciphertext = chacha.Encrypt(plaintext);
/// byte[] roundTrip  = chacha.Decrypt(ciphertext);
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class SymmetricStreamAlgorithmExtensions
{
}
