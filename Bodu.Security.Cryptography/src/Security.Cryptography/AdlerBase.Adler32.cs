// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>Adler-32</c> hash algorithm. This variant uses a modulus of 65521 and follows the
    /// standard Adler checksum specification for computing 32-bit checksums. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Adler-32 is a non-cryptographic checksum algorithm developed by Mark Adler for use in the zlib compression library. It produces a
    /// 32-bit checksum by maintaining two running sums (A and B), which are updated while reading input data and then combined into the
    /// final result as <c><![CDATA[(B << 16) | A]]></c>. This implementation follows the original specification using a modulus of 65521
    /// (the largest prime less than 2 <sup>16</sup>).
    /// </para>
    /// <para>
    /// This class is a concrete implementation of the abstract <see cref="Adler32Base" /> base class, which provides the shared logic for
    /// Adler-style checksums. Use this class when a standard, zlib-compatible Adler-32 checksum is required.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class Adler32
        : Adler32Base
    {
        /// <summary>
        /// The standard Adler-32 modulus (65521), used in the original zlib implementation.
        /// </summary>
        private const uint Adler32_Modulo = 65521; // Largest prime smaller than 2^16

        /// <summary>
        /// Initializes a new instance of the <see cref="Adler32" /> class using the standard Adler-32 modulus (65521).
        /// </summary>
        public Adler32()
            : base(Adler32_Modulo)
        { }
    }
}