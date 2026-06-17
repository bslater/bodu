// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the hash for the input data using the <c>Fletcher-64</c> hash algorithm. This variant performs a
/// non-cryptographic checksum calculation using two 32-bit accumulators to efficiently detect errors in byte sequences.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Fletcher64" /> is a non-cryptographic hash algorithm that computes a 64-bit checksum by iteratively
/// processing input bytes using two 32-bit rolling sums. It was introduced by Brian Kernighan and Dennis Ritchie and is
/// commonly used in applications such as network protocols, data validation, and embedded systems where simple and
/// efficient error detection is sufficient.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 64 bits (8 bytes).</description>
/// </item>
/// <item>
/// <description>Accumulator width: two 32-bit rolling sums (A and B).</description>
/// </item>
/// <item>
/// <description>Modulus: <c>4294967295</c>.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Fletcher64.</strong> Pick <see cref="Fletcher64" /> for very large datasets where the 32-bit
/// collision floor becomes a concern — multi-gigabyte file integrity, large block-storage checksums, or high-throughput
/// logging pipelines. For stronger error detection at comparable width prefer <see cref="Crc" /> with
/// <see cref="CrcStandard.CRC64_XZ" /> or <see cref="CrcStandard.CRC64_ECMA182" />.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// var f64 = new Fletcher64();
/// byte[] checksum = f64.ComputeHash(largePayload);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Fletcher{T}"/> <seealso cref="Fletcher16"/> <seealso cref="Fletcher32"/>
public sealed class Fletcher64
    : Fletcher<Fletcher64>
{
    /// <summary>The output width, in bits, of the Fletcher-64 algorithm.</summary>
    private const int FletcherHashSize = 64;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fletcher64" /> class with a 64-bit hash size.
    /// </summary>
    public Fletcher64()
        : base(FletcherHashSize)
    {
    }
}
