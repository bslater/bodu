// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>Adler-32</c> hash algorithm. This variant uses a modulus of 65536 and is optimized
    /// for alignment and performance in SIMD-capable environments. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Adler-32 is a non-cryptographic checksum algorithm developed by Mark Adler for use in the zlib compression library. It produces a
    /// 32-bit checksum by maintaining two running sums (A and B), which are updated while reading input data and then combined into the
    /// final result as <c><![CDATA[(B << 16) | A]]></c>.
    /// </para>
    /// <para>
    /// This variant uses a modulus of 65536 (rather than the standard 65521) to enable more efficient vectorized operations in performance-
    /// critical scenarios. It inherits from the abstract <see cref="Adler32Base" /> base class, which provides shared Adler-style logic.
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
        /// Initializes a new instance of the <see cref="Adler32C" /> class using the optimized modulus (65536).
        /// </summary>
        public Adler32C()
            : base(Adler32C_Modulo)
        { }
    }
}