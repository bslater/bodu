// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash.128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 128-bit keyed hash using the <c>SipHash</c> algorithm by Aumasson and Bernstein. Produces a 16-byte authentication tag
/// from a 128-bit key, offering increased collision resistance over <see cref="SipHash64" />. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SipHash128" /> is parameterised as <c>SipHash-c-d</c>, where <c>c</c> is the number of compression rounds and
/// <c>d</c> is the number of finalisation rounds. The default configuration corresponds to <c>SipHash-2-4</c>.
/// </para>
/// <para>See <see cref="SipHash{T}" /> for a description of the round structure.</para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Tag size: 128 bits (16 bytes).</description></item>
///   <item><description>Key size: 128 bits (16 bytes).</description></item>
///   <item><description>Default parameterisation: SipHash-2-4 (2 compression rounds, 4 finalisation rounds).</description></item>
///   <item><description>Multi-message key reuse is supported — unlike <see cref="Poly1305"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose SipHash128.</strong> Pick <see cref="SipHash128"/> over <see cref="SipHash64"/> when
/// the wider 128-bit tag is required — multi-tenant hash tables with very large key spaces, fingerprint-style
/// cache keys, or short-message authentication where 64 bits would be marginal. For protocol-level MACs over
/// long messages prefer HMAC-SHA-256 or <see cref="Blake2b"/>-MAC.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using Bodu.Security.Cryptography;
///
/// using var sip = new SipHash128 { Key = sessionKey };
/// byte[] tag = sip.ComputeHash(message);
/// </code>
/// </example>
public sealed class SipHash128
    : SipHash<SipHash128>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SipHash128" /> class with a fixed 128-bit output size, the default
    /// <c>SipHash-2-4</c> parameterisation, and a freshly generated random key.
    /// </summary>
    /// <remarks>
    /// The instance is created with the following defaults:
    /// <list type="table">
    /// <listheader>
    /// <term>Property</term>
    /// <description>Default Value</description>
    /// </listheader>
    /// <item>
    /// <term><see cref="SipHash{T}.CompressionRounds" /></term>
    /// <description><see cref="SipHash{T}.MinCompressionRounds" /> (2)</description>
    /// </item>
    /// <item>
    /// <term><see cref="SipHash{T}.FinalizationRounds" /></term>
    /// <description><see cref="SipHash{T}.MinFinalizationRounds" /> (4)</description>
    /// </item>
    /// <item>
    /// <term><see cref="HashAlgorithm.HashSize" /></term>
    /// <description>128</description>
    /// </item>
    /// <item>
    /// <term><see cref="SipHash{T}.Key" /></term>
    /// <description>Cryptographically random 16-byte key containing no zero bytes.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public SipHash128()
        : base(128)
    {
    }
}
