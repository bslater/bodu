// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the hash for the input data using the <c>Fletcher-32</c> hash algorithm. This variant performs a
/// non-cryptographic checksum calculation using two 16-bit accumulators to efficiently detect errors in byte sequences.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Fletcher32" /> is a lightweight checksum algorithm that produces a 32-bit output by iteratively processing
/// input bytes using two 16-bit rolling sums. It was introduced by Brian Kernighan and Dennis Ritchie and is commonly
/// used in networking, file validation, and embedded systems where error detection is required but cryptographic strength
/// is not.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: 32 bits (4 bytes).</description></item>
///   <item><description>Accumulator width: two 16-bit rolling sums (A and B).</description></item>
///   <item><description>Modulus: <c>65535</c>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Fletcher32.</strong> The general-purpose Fletcher variant — TCP/UDP-style packet
/// checks, file integrity in streaming pipelines, and ZFS-class block sums. Faster than CRC at the cost of
/// weaker burst-error coverage; pick <see cref="Crc"/> with a 32-bit standard such as
/// <see cref="CrcStandard.CRC32_ISOHDLC"/> when error-detection guarantees matter more than throughput.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password
/// hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// var f32 = new Fletcher32();
/// byte[] checksum = f32.ComputeHash(payload);
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Fletcher{T}"/>
/// <seealso cref="Fletcher16"/>
/// <seealso cref="Fletcher64"/>
public sealed class Fletcher32
    : Fletcher<Fletcher32>
{
    private const int FletcherHashSize = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fletcher32" /> class with a 32-bit hash size.
    /// </summary>
    public Fletcher32()
        : base(FletcherHashSize)
    {
    }
}
