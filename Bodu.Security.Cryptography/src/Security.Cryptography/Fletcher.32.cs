// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>Fletcher-32</c> hash algorithm. This variant performs a non-cryptographic checksum
    /// calculation using two 16-bit accumulators to efficiently detect errors in byte sequences. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Fletcher32" /> is a lightweight checksum algorithm that produces a 32-bit output by iteratively processing input bytes
    /// using two 16-bit rolling sums. It was introduced by Brian Kernighan and Dennis Ritchie and is commonly used in networking, file
    /// validation, and embedded systems where error detection is required but cryptographic strength is not.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    /// <seealso href="../guides/cryptography/hashing.html#pattern-1--a-non-cryptographic-checksum">Non-cryptographic checksum guide</seealso>
    public sealed class Fletcher32
        : Security.Cryptography.Fletcher<Fletcher32>
    {
        // Define a constant for the hash size
        private const int FletcherHashSize = 32;

        /// <summary>
        /// Initializes a new instance of the <see cref="Fletcher32" /> class with a 32-bit hash size.
        /// </summary>
        public Fletcher32()
            : base(FletcherHashSize)
        { }
    }
}