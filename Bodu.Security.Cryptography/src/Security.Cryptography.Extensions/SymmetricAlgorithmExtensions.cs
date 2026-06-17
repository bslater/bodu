// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Extends <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> with one-shot encrypt/decrypt of buffers,
/// stream-to-stream pipelines, awaitable async variants, and try-pattern transform creation that surfaces validation
/// failures without exceptions.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> ships with the building blocks —
/// <c>CreateEncryptor</c>, <c>CreateDecryptor</c>, and <c>CryptoStream</c> — but stops short of the operations that
/// callers actually want to invoke: "encrypt this byte array", "decrypt this stream into that one", or "give me back
/// the encryptor only if the key/IV check out". Production code that does this work tends to repeat the same setup:
/// instantiate the transform, wrap it in a <see cref="System.Security.Cryptography.CryptoStream" />, drain the source,
/// dispose everything in the right order. This class collapses that into a single call per scenario, with overloads
/// aligned to the input shape.
/// </para>
/// <para>
/// The API surface clusters into three groups:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Encrypt / Decrypt — one-shot</term>
/// <description>
/// <c>Encrypt</c> and <c>Decrypt</c> overloads accepting <see cref="byte" />[], a slice (<c>offset</c>/<c>count</c>),
/// <see cref="System.ReadOnlySpan{T}" />, or <see cref="System.ReadOnlyMemory{T}" />, returning the result as a freshly
/// allocated array. Use these when the entire payload fits in memory.
/// </description>
/// </item>
/// <item>
/// <term>Encrypt / Decrypt — stream</term>
/// <description>
/// <c>Encrypt(sourceStream, targetStream)</c> / <c>Decrypt(sourceStream, targetStream)</c> with awaitable
/// <c>EncryptAsync</c> / <c>DecryptAsync</c> variants. The buffer size defaults to <see cref="DefaultBufferSize" /> (80
/// KiB); callers can override it with the explicit <c>bufferSize</c> overload. Streams are not disposed by these
/// methods.
/// </description>
/// </item>
/// <item>
/// <term>Try-pattern transform creation</term>
/// <description>
/// <c>TryCreateEncryptor</c> / <c>TryCreateDecryptor</c> — surface key/IV validation failures as a
/// <see langword="false" /> return rather than a <see cref="System.Security.Cryptography.CryptographicException" />.
/// Useful when accepting key material from configuration or user input.
/// </description>
/// </item>
/// </list>
/// <para>
/// All operations honor the algorithm's configured <see cref="System.Security.Cryptography.SymmetricAlgorithm.Mode" />
/// and <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding" />; for AEAD modes (GCM, CCM, EAX, …) prefer
/// the dedicated <see cref="AeadBlockCipherModeTransformExtensions" /> in the same namespace, which handles associated
/// data and tag layout. Async overloads honor <see cref="System.Threading.CancellationToken" /> at every read boundary.
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> instances are not thread-safe; share them only behind
/// explicit synchronization.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using Aes aes = Aes.Create();
/// aes.Key = key;
/// aes.IV = iv;
///
/// // 1. One-shot encrypt of an in-memory span; one-shot decrypt back.
/// byte[] ciphertext = aes.Encrypt(plaintext.AsSpan());
/// byte[] roundTrip = aes.Decrypt(ciphertext);
///
/// // 2. Stream-encrypt a large file with the default 80 KiB buffer.
/// using FileStream src = File.OpenRead("plain.bin");
/// using FileStream dst = File.Create("cipher.bin");
/// int bytesWritten = aes.Encrypt(src, dst);
///
/// // 3. Validate caller-supplied key material without catching CryptographicException.
/// if (!aes.TryCreateDecryptor(suppliedKey, suppliedIv, out ICryptoTransform? decryptor))
///     return BadRequest("invalid key/iv combination");
/// using (decryptor) { /* … decrypt with the validated transform … */ }
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class SymmetricAlgorithmExtensions
{
    /// <summary>The default buffer size, in bytes, used when reading from or writing to streams during encryption and decryption operations.</summary>
    public const int DefaultBufferSize = 81920;
}
