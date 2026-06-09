// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Extensions;

/// <summary>
/// Extends <see cref="NonCryptographicHashAlgorithm" /> with one-shot hashing, streaming and async input, and
/// constant-time hash verification — the high-level surface that <see cref="NonCryptographicHashAlgorithm" /> itself
/// omits.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NonCryptographicHashAlgorithm" /> intentionally exposes only the low-level
/// <see cref="NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" /> /
/// <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> primitives. Real-world callers want to ask
/// higher-level questions: "compute the hash of this byte array", "stream-hash this file", "does this download match
/// the expected digest?". This class adds those operations as a coherent set, mirroring the shape of
/// <see cref="System.Security.Cryptography.HashAlgorithm" /> so the same call patterns work whether the underlying
/// algorithm is cryptographic (SHA-256) or non-cryptographic (CRC, xxHash, Fletcher).
/// </para>
/// <para>
/// The API surface clusters into four groups:
/// </para>
/// <list type="bullet">
/// <item>
/// <term>Append</term>
/// <description>
/// <c>AppendData</c> / <c>AppendDataAsync</c> — feed a buffer or stream into the algorithm's running state, with a
/// tunable read-buffer size for stream input.
/// </description>
/// </item>
/// <item>
/// <term>One-shot compute</term>
/// <description>
/// <c>ComputeHash</c> / <c>ComputeHashAsync</c> — append, finalize, and reset in a single call, returning the digest as
/// a freshly allocated <see cref="byte" /> array. Inputs include byte arrays, byte-array slices,
/// <see cref="System.ReadOnlyMemory{T}" />, and <see cref="System.IO.Stream" />.
/// </description>
/// </item>
/// <item>
/// <term>Throwing verification</term>
/// <description>
/// <c>VerifyHash</c> / <c>VerifyHashAsync</c> — compute the digest of an input and compare it against an expected value
/// supplied as either bytes or a hexadecimal string. Returns <see langword="true" /> on match, throws on argument
/// problems.
/// </description>
/// </item>
/// <item>
/// <term>Try-pattern verification</term>
/// <description>
/// <c>TryVerifyHash</c> / <c>TryVerifyHashAsync</c> — non-throwing counterparts. Suitable when the expected hash is
/// user-supplied and a malformed input should not surface as an exception.
/// </description>
/// </item>
/// </list>
/// <para>
/// All hash comparisons go through
/// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})" />
/// , so verification is constant-time and safe to use against attacker-supplied digests even though the underlying
/// algorithm itself provides no preimage resistance. The algorithm instance is stateful and reset after each
/// <c>ComputeHash</c> or <c>VerifyHash</c> call, but it is not thread-safe — share instances only behind explicit
/// synchronization. Stream overloads do not dispose the supplied stream.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using System.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// // 1. One-shot hash of a byte buffer using xxHash64.
/// var xx = new XxHash64();
/// byte[] digest = xx.ComputeHash(File.ReadAllBytes("payload.bin"));
///
/// // 2. Stream-hash a large file without loading it into memory.
/// using FileStream fs = File.OpenRead("payload.bin"); 
/// byte[] streamDigest = xx.ComputeHash(fs);
///
/// // 3. Verify a downloaded artefact against an expected hex digest, without throwing on a malformed string.
/// var crc = new Crc32();
/// if (crc.TryVerifyHash(File.ReadAllBytes("artefact.zip"), expectedHex: "deadbeef"))
///     Console.WriteLine("artefact verified");
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class NonCryptographicHashAlgorithmExtensions
{
}
