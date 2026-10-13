// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32C.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes a 32-bit Adler-style checksum using the power-of-two modulus 65536 instead of the prime 65521. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This variant maintains two running sums (A and B) and combines them as <c><![CDATA[(B << 16) | A]]></c>, but uses a
/// modulus of 65536 rather than the standard 65521. It runs the same scalar loop as <see cref="Adler32" />, so the
/// power-of-two modulus brings no speed advantage here, and it detects fewer errors than the prime modulus. Outputs
/// differ from standard <see cref="Adler32" /> and are not interchangeable.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 32 bits (4 bytes).</description>
/// </item>
/// <item>
/// <description>Modulus: <c>65536</c> (power of two).</description>
/// </item>
/// <item>
/// <description>Compatibility: <strong>not</strong> interchangeable with <see cref="Adler32" /> outputs.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Adler32C.</strong> Pick <see cref="Adler32C" /> only to match an existing system that
/// computes Adler sums modulo 65536. For new internal checksums <see cref="Adler32" /> costs the same and detects more
/// errors, and for any boundary that interoperates with zlib, deflate, gzip, PNG, or other external systems use
/// <see cref="Adler32" /> - its output is the only one those systems will accept.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// // Adler sum modulo 65536, for a system that uses this variant.
/// var adler = new Adler32C();
/// byte[] checksum = adler.ComputeHash(largeBlock);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Adler{T}"/> <seealso cref="Adler32"/> <seealso cref="Adler64"/>
public sealed class Adler32C
    : Adler32Base
{
    /// <summary>The power-of-two modulus used in place of the standard Adler-32 prime.</summary>
    private const uint Adler32CModulo = 65536U;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler32C" /> class using the power-of-two modulus (65536).
    /// </summary>
    public Adler32C()
        : base(Adler32CModulo)
    {
    }
}
