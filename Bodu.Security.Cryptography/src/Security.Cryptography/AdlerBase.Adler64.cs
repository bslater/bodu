// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>Adler-64</c> checksum algorithm. This variant calculates two running sums over the
    /// input bytes modulo 4294967291 and combines them into a 64-bit result. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Adler-64 is a non-cryptographic checksum algorithm that extends the principles of Adler-32 into a 64-bit domain. It maintains two
    /// 64-bit running sums (A and B), which are updated for each byte of input and combined into a final 64-bit checksum as <c><![CDATA[(B
    /// << 32) | A]]></c>.
    /// </para>
    /// <para>
    /// This implementation uses the prime number 4294967291 (the largest 32-bit unsigned prime) as the modulus, reducing the likelihood of
    /// overflow and improving checksum stability over large inputs compared to Adler-32.
    /// </para>
    /// <para>
    /// This class derives from <see cref="Adler64Base" />, which provides the reusable logic for Adler-style checksum accumulation and
    /// modular reduction.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class Adler64
        : Adler64Base
    {
        /// <summary>
        /// The standard Adler-64 modulus (4294967291), the largest 32-bit prime number.
        /// </summary>
        private const ulong Adler64_Modulo = 4294967291UL;

        /// <summary>
        /// Initializes a new instance of the <see cref="Adler64" /> class using the Adler-64 prime modulus (4294967291).
        /// </summary>
        public Adler64()
            : base(Adler64_Modulo)
        { }
    }
}