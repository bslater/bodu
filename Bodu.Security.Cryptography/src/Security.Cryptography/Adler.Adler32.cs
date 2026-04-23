// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes the zlib-compatible <c>Adler-32</c> checksum of the input data using the standard modulus 65521. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Adler-32 is a non-cryptographic checksum algorithm developed by Mark Adler for the zlib compression library. It produces a 32-bit
/// checksum by maintaining two running sums (A and B) and combining them as <c><![CDATA[(B << 16) | A]]></c>. This implementation follows
/// the original specification using the modulus 65521 (the largest prime less than 2<sup>16</sup>).
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <seealso href="../guides/cryptography/hashing.html#pattern-1--a-non-cryptographic-checksum">Non-cryptographic checksum guide</seealso>
public sealed class Adler32
    : Adler32Base
{
    /// <summary>
    /// The standard Adler-32 modulus (65521), used in the original zlib implementation.
    /// </summary>
    private const uint Adler32_Modulo = 65521; // Largest prime smaller than 2^16

    /// <summary>
    /// Initialises a new instance of the <see cref="Adler32" /> class using the standard Adler-32 modulus (65521).
    /// </summary>
    public Adler32()
        : base(Adler32_Modulo)
    { }
}
