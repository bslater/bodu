// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the zlib-compatible <c>Adler-32</c> checksum of the input data using the standard modulus 65521. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Adler-32 is a non-cryptographic checksum algorithm developed by Mark Adler for the zlib compression library. It
/// produces a 32-bit checksum by maintaining two running sums (A and B) and combining them as
/// <c><![CDATA[(B << 16) | A]]></c>, using the modulus 65521 (the largest prime less than 2<sup>16</sup>).
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 32 bits (4 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Modulus: <c>65521</c> (largest prime &lt; 2<sup>16</sup>).
/// </description>
/// </item>
/// <item>
/// <description>
/// Specification: zlib (RFC 1950); standard for deflate / PNG chunk integrity.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Adler32.</strong> The right choice whenever interoperability with zlib, deflate, gzip, or PNG
/// is required — every Adler-32 produced or consumed by those formats is bit-identical to this class's output. Faster
/// than CRC at the cost of weaker error coverage; pick <see cref="Crc" /> with <see cref="CrcStandard.CRC32_ISOHDLC" />
/// when error-detection guarantees matter more than throughput, and <see cref="Adler64" /> for very large inputs where
/// the Adler-32 collision floor becomes a concern.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// zlib-compatible Adler-32 of a deflate payload.
/// var adler = new Adler32();
/// byte[] checksum = adler.ComputeHash(deflateBlock);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Adler{T}"/> <seealso cref="Adler32C"/> <seealso cref="Adler64"/>
public sealed class Adler32
    : Adler32Base
{
    private const uint Adler32Modulo = 65521U;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler32" /> class using the standard Adler-32 modulus (65521).
    /// </summary>
    public Adler32()
        : base(Adler32Modulo)
    {
    }
}
