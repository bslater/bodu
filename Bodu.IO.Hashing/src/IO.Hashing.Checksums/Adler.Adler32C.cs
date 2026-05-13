// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32C.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes a 32-bit Adler-style checksum using the power-of-two modulus 65536 for SIMD-friendly alignment.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This variant maintains two running sums (A and B) and combines them as
/// <c><![CDATA[(B << 16) | A]]></c>, but uses a modulus of 65536 rather than the standard 65521 to enable
/// cheaper modular reductions in vectorized code paths. Outputs differ from standard <see cref="Adler32" />
/// and are not interchangeable.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Output size: 32 bits (4 bytes).</description></item>
///   <item><description>Modulus: <c>65536</c> (power of two; SIMD-friendly).</description></item>
///   <item><description>Compatibility: <strong>not</strong> interchangeable with <see cref="Adler32"/> outputs.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Adler32C.</strong> Pick <see cref="Adler32C"/> when both endpoints are under your
/// control and SIMD throughput on the modular-reduction step is the bottleneck — large in-memory streams,
/// internal block-level integrity checks, content-defined chunking. For any boundary that interoperates with
/// zlib, deflate, gzip, PNG, or other external systems, use <see cref="Adler32"/> instead — its output is the
/// only one those systems will accept.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// // SIMD-friendly Adler variant for high-throughput internal pipelines.
/// var adler = new Adler32C();
/// byte[] checksum = adler.ComputeHash(largeBlock);
/// </code>
/// </example>
/// </remarks>
public sealed class Adler32C
    : Adler32Base
{
    private const uint Adler32CModulo = 65536U;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler32C" /> class using the SIMD-friendly modulus
    /// (65536).
    /// </summary>
    public Adler32C()
        : base(Adler32CModulo)
    {
    }
}
