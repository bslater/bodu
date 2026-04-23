// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.Adler32C.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 32-bit Adler-style checksum using the power-of-two modulus 65536 for SIMD-friendly alignment. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This variant maintains two running sums (A and B) and combines them as <c><![CDATA[(B << 16) | A]]></c>, but uses a modulus of 65536
/// rather than the standard 65521 to enable cheaper modular reductions in vectorised code paths. Outputs differ from standard
/// <see cref="Adler32" /> and are not interchangeable.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Adler32C
    : Adler32Base
{
    /// <summary>
    /// The Adler-32C modulus (65536), used for performance-oriented implementations with SIMD alignment.
    /// </summary>
    private const uint Adler32C_Modulo = 65536; // 2^16, used in vectorized variants

    /// <summary>
    /// Initialises a new instance of the <see cref="Adler32C" /> class using the optimised modulus (65536).
    /// </summary>
    public Adler32C()
        : base(Adler32C_Modulo)
    { }
}
