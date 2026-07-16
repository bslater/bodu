// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash.64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 64-bit keyed hash using the <see cref="SipHash" /> algorithm by Aumasson and Bernstein. Produces an
/// 8-byte authentication tag from a 128-bit key and is intended to protect hash tables against collision-based
/// denial-of-service attacks. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SipHash64" /> is parameterized as <c>SipHash-c-d</c>, where <c>c</c> is the number of compression rounds
/// and <c>d</c> is the number of finalization rounds. The default configuration corresponds to <c>SipHash-2-4</c>;
/// stronger parameterizations such as <c>SipHash-4-8</c> may be selected via
/// <see cref="SipHash.CompressionRounds" /> and <see cref="SipHash.FinalizationRounds" />.
/// </para>
/// <para>
/// See <see cref="SipHash" /> for a description of the round structure.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Tag size: 64 bits (8 bytes).</description>
/// </item>
/// <item>
/// <description>Key size: 128 bits (16 bytes).</description>
/// </item>
/// <item>
/// <description>Default parameterization: SipHash-2-4 (2 compression rounds, 4 finalization rounds).</description>
/// </item>
/// <item>
/// <description>Multi-message key reuse is supported — unlike <see cref="Poly1305" />.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose SipHash64.</strong> The right keyed hash for protecting in-memory hash tables and bloom
/// filters against collision-based denial-of-service attacks — fast, fixed cost, and the de-facto standard in language
/// runtimes (Python, Ruby, Rust, Perl). For 128-bit tag width or stronger long-term authentication guarantees pick
/// <see cref="SipHash128" />; for protocol-level message authentication use HMAC-SHA-256 or <see cref="Blake2b" />-MAC.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Security.Cryptography;
///
/// // 16-byte secret key — keep it private to your process; SipHash assumes attackers cannot guess it.
/// byte[] hashTableKey = RandomNumberGenerator.GetBytes(16);
/// using var sip = new SipHash64 { Key = hashTableKey };
/// byte[] tag = sip.ComputeHash(System.Text.Encoding.UTF8.GetBytes("user-supplied-key"));
///]]>
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/hashing.html#pattern-2--a-keyed-hash-siphash">Keyed-hash (SipHash) guide
/// </seealso> <seealso cref="SipHash"/> <seealso cref="SipHash128"/>
public sealed class SipHash64
    : SipHash
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SipHash64" /> class with a fixed 64-bit output size, the default
    /// <c>SipHash-2-4</c> parameterization, and a freshly generated random key.
    /// </summary>
    /// <remarks>
    /// The instance is created with the following defaults:
    /// <list type="table">
    /// <listheader>
    /// <term>Property</term>
    /// <description>Default Value</description>
    /// </listheader>
    /// <item>
    /// <term><see cref="SipHash.CompressionRounds" /></term>
    /// <description><see cref="SipHash.MinCompressionRounds" /> (2)</description>
    /// </item>
    /// <item>
    /// <term><see cref="SipHash.FinalizationRounds" /></term>
    /// <description><see cref="SipHash.MinFinalizationRounds" /> (4)</description>
    /// </item>
    /// <item>
    /// <term><see cref="HashAlgorithm.HashSize" /></term>
    /// <description>64</description>
    /// </item>
    /// <item>
    /// <term><see cref="KeyedBlockHashAlgorithm.Key" /></term>
    /// <description>Cryptographically random 16-byte key containing no zero bytes.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public SipHash64()
        : base(64)
    {
    }
}
