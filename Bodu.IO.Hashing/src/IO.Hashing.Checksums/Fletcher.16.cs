// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher.16.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the hash for the input data using the <c>Fletcher-16</c> hash algorithm. This variant performs a
/// non-cryptographic checksum calculation using two 8-bit accumulators to detect errors in small byte sequences. This
/// class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Fletcher16" /> is a lightweight checksum algorithm that produces a 16-bit output by iteratively summing
/// input bytes using two rolling accumulators. It was introduced by Brian Kernighan and Dennis Ritchie and is commonly
/// used in network protocols, embedded systems, and file integrity checks where performance and simplicity are
/// preferred over cryptographic strength.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 16 bits (2 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Accumulator width: two 8-bit rolling sums (A and B).
/// </description>
/// </item>
/// <item>
/// <description>
/// Modulus: <c>255</c>.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Fletcher16.</strong> Pick <see cref="Fletcher16" /> for embedded protocols and short-frame
/// links where 16 bits is enough — Modbus ASCII, RPL message integrity, and similar resource-constrained settings. For
/// general file integrity prefer <see cref="Fletcher32" />; for stronger error-detection guarantees prefer a 16-bit CRC
/// such as <see cref="Crc" /> with <see cref="CrcStandard.CRC16_MODBUS" />.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// var f16 = new Fletcher16();
/// byte[] checksum = f16.ComputeHash(framePayload);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Fletcher{T}"/> <seealso cref="Fletcher32"/> <seealso cref="Fletcher64"/>
public sealed class Fletcher16
    : Fletcher<Fletcher16>
{
    private const int FletcherHashSize = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fletcher16" /> class with a 16-bit hash size.
    /// </summary>
    public Fletcher16()
        : base(FletcherHashSize)
    {
    }
}
