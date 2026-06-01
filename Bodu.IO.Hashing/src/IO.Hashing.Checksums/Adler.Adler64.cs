// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes a 64-bit Adler-style checksum using the prime modulus 4294967291. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Adler-64 extends the Adler-32 construction into the 64-bit domain. It maintains two 64-bit running sums (A and B)
/// and combines them as <c><![CDATA[(B << 32) | A]]></c>. The modulus 4294967291 (the largest prime less than 2<sup>32
/// </sup>) reduces overflow pressure and improves checksum stability over large inputs compared to Adler-32.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 64 bits (8 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Modulus: <c>4294967291</c> (largest prime &lt; 2<sup>32</sup>).
/// </description>
/// </item>
/// <item>
/// <description>
/// Compatibility: <strong>not</strong> a standardized wire format — consumers must agree on Adler-64 explicitly.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Adler64.</strong> Pick <see cref="Adler64" /> when 32 bits are not enough — very large
/// datasets where the Adler-32 collision floor becomes a concern, multi-gigabyte block-storage checksums, or
/// high-throughput logging pipelines. For interoperability with deflate / gzip / PNG use <see cref="Adler32" />; for
/// stronger error detection at 64 bits prefer <see cref="Crc" /> with <see cref="CrcStandard.CRC64_XZ" />.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// var adler = new Adler64();
/// byte[] checksum = adler.ComputeHash(largePayload);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Adler{T}"/> <seealso cref="Adler32"/> <seealso cref="Adler32C"/>
public sealed class Adler64
    : Adler64Base
{
    private const ulong Adler64Modulo = 4294967291UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler64" /> class using the Adler-64 prime modulus (4294967291).
    /// </summary>
    public Adler64()
        : base(Adler64Modulo)
    {
    }
}
