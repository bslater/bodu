// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes a 64-bit Adler-style checksum using the prime modulus 4294967291. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Adler-64 extends the Adler-32 construction into the 64-bit domain. It maintains two 64-bit running sums
/// (A and B) and combines them as <c><![CDATA[(B << 32) | A]]></c>. The modulus 4294967291 (the largest
/// prime less than 2<sup>32</sup>) reduces overflow pressure and improves checksum stability over large
/// inputs compared to Adler-32.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Adler64
    : Adler64Base
{
    private const ulong Adler64Modulo = 4294967291UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler64" /> class using the Adler-64 prime modulus
    /// (4294967291).
    /// </summary>
    public Adler64()
        : base(Adler64Modulo)
    {
    }
}
