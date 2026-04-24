// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the zlib-compatible <c>Adler-32</c> checksum of the input data using the standard modulus 65521.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Adler-32 is a non-cryptographic checksum algorithm developed by Mark Adler for the zlib compression
/// library. It produces a 32-bit checksum by maintaining two running sums (A and B) and combining them as
/// <c><![CDATA[(B << 16) | A]]></c>, using the modulus 65521 (the largest prime less than 2<sup>16</sup>).
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Adler32
    : Adler32Base
{
    private const uint Adler32Modulo = 65521U;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler32" /> class using the standard Adler-32 modulus
    /// (65521).
    /// </summary>
    public Adler32()
        : base(Adler32Modulo)
    {
    }
}
