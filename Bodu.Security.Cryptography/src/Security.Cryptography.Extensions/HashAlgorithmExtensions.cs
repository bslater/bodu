// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Extends <see cref="HashAlgorithm"/> with incremental input, async hashing of streams, and constant-time verification
/// against expected digests supplied as bytes, spans, memory, or hex strings.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="HashAlgorithm"/> base type exposes a low-level API: <c>TransformBlock</c> / <c>TransformFinalBlock</c>
/// for incremental input, and <see cref="HashAlgorithm.ComputeHash(byte[])"/> for one-shot work. Production code that
/// hashes streams, verifies downloads, or matches user-supplied digests ends up writing the same boilerplate
/// repeatedly: read the stream into a buffer, push it through the algorithm, and compare the result with
/// <see cref="CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/> to avoid
/// timing leaks. This class collapses that boilerplate into a small, named API surface while keeping every comparison
/// constant-time.
/// </para>
/// <para>
/// The API surface clusters into three groups:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Append</term>
///     <description><c>AppendData</c> / <c>AppendDataAsync</c> — feed a span or stream into the algorithm's running
///     state, equivalent to a chained <c>TransformBlock</c> sequence but cancellation-aware in the async form.</description>
///   </item>
///   <item>
///     <term>Throwing verification</term>
///     <description><c>VerifyHash</c> / <c>VerifyHashAsync</c> — hash an input (byte array, span, memory, stream, or
///     <see cref="System.String"/> with a chosen <see cref="System.Text.Encoding"/>) and compare the digest against an
///     expected value given as bytes or as a hexadecimal string. Throws on a malformed input or hex.</description>
///   </item>
///   <item>
///     <term>Try-pattern verification</term>
///     <description><c>TryVerifyHash</c> / <c>TryVerifyHashAsync</c> — non-throwing counterparts that return
///     <see langword="false"/> for malformed expected hashes and <see langword="true"/> only when the inputs round-trip
///     and match. Suitable when the expected hash is user-supplied.</description>
///   </item>
/// </list>
/// <para>
/// Every digest comparison routes through
/// <see cref="CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/> so an
/// attacker cannot infer a partial-match by timing the call. The async overloads honour
/// <see cref="System.Threading.CancellationToken"/> at every read boundary. <see cref="HashAlgorithm"/> instances are
/// stateful and reset after each verification or compute call; they are not thread-safe, so share instances only behind
/// explicit synchronisation. Stream overloads do not dispose the supplied <see cref="System.IO.Stream"/>.
/// </para>
/// <para>
/// For non-cryptographic algorithms (CRC, xxHash, Fletcher, …) prefer the
/// <see cref="Bodu.IO.Hashing.Extensions.NonCryptographicHashAlgorithmExtensions"/> companion in <c>Bodu.IO.Hashing</c>;
/// both surfaces follow the same naming so call sites read identically.
/// </para>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using System.Text;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var sha = SHA256.Create();
///
/// // 1. Verify a UTF-8 string against an expected digest, throwing if anything is wrong.
/// byte[] expected = Convert.FromHexString("dffd6021bb2bd5b0af676290809ec3a53191dd81c7f70a4b28688a362182986f");
/// bool match = sha.VerifyHash("Hello, World!", Encoding.UTF8, expected);
///
/// // 2. Stream-verify a downloaded file against a hex digest, without throwing on malformed hex.
/// using FileStream fs = File.OpenRead("payload.bin");
/// bool ok = sha.TryVerifyHash(fs, expectedHex: "ba7816bf...");
///
/// // 3. Cancellable async verification of a network stream against a known digest.
/// await using Stream net = response.Content.ReadAsStream();
/// bool verified = await sha.VerifyHashAsync(net, expected, cancellationToken);
/// </code>
/// </example>
/// </remarks>
public static partial class HashAlgorithmExtensions
{
}
