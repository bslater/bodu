// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv1a32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>FNV-1a</c> 32-bit hash algorithm. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Fowler�Noll�Vo (FNV) family of hash functions provides a fast, non-cryptographic mechanism for producing fixed-size hash values
    /// from arbitrary-length input data. The FNV-1a variant improves diffusion by applying a bitwise XOR before multiplying each byte with
    /// a fixed FNV prime.
    /// </para>
    /// <para>
    /// The <see cref="Fnv1a32" /> implementation uses a 32-bit hash size with a prime of <c>0x01000193</c> and an offset basis of
    /// <c>0x811C9DC5</c>. This configuration is widely used in hash tables, checksums, and non-cryptographic fingerprinting.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    /// <seealso href="../guides/cryptography/hashing.html#pattern-1--a-non-cryptographic-checksum">Non-cryptographic checksum guide</seealso>
    public sealed class Fnv1a32
        : Fnv
    {
        private const ulong OffsetBasis = 0x811C9DC5UL;
        private const ulong Prime = 0x01000193UL;

        /// <summary>
        /// Initializes a new instance of the <see cref="Fnv1a32" /> class using standard FNV-1a 32-bit parameters.
        /// </summary>
        /// <remarks>
        /// <para>This constructor configures the FNV algorithm to use the 32-bit variant of FNV-1a, with the following predefined values:</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Parameter</term>
        /// <description>Value</description>
        /// </listheader>
        /// <item>
        /// <term>Hash Size</term>
        /// <description>32 bits</description>
        /// </item>
        /// <item>
        /// <term>FNV Prime</term>
        /// <description><c>0x01000193</c></description>
        /// </item>
        /// <item>
        /// <term>Offset Basis</term>
        /// <description><c>0x811C9DC5</c></description>
        /// </item>
        /// <item>
        /// <term>Variant</term>
        /// <description>FNV-1a (XOR before multiply)</description>
        /// </item>
        /// </list>
        /// </remarks>
        public Fnv1a32()
            : base(hashSize: 32, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: true)
        {
        }
    }
}