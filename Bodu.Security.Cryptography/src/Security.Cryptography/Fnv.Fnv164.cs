// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv164.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>FNV-1</c> 64-bit hash algorithm. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Fowler–Noll–Vo (FNV) family of hash functions provides a fast, non-cryptographic mechanism for producing fixed-size hash values
    /// from arbitrary-length input data. The FNV-1 variant applies multiplication before XOR and is suitable for high-throughput scenarios
    /// requiring fast, stable hashing.
    /// </para>
    /// <para>
    /// The <see cref="Fnv164" /> implementation uses a 64-bit hash size with a prime of <c>0x00000100000001B3</c> and an offset basis of
    /// <c>0xCBF29CE484222325</c>. It is well-suited for indexing, fingerprinting, and deduplication applications that prioritize
    /// performance over cryptographic strength.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class Fnv164 : Fnv
    {
        private const ulong OffsetBasis = 0xCBF29CE484222325UL;
        private const ulong Prime = 0x00000100000001B3UL;

        /// <summary>
        /// Initializes a new instance of the <see cref="Fnv164" /> class using standard FNV-1 64-bit parameters.
        /// </summary>
        /// <remarks>
        /// <para>This constructor configures the FNV algorithm to use the 64-bit variant of FNV-1, with the following predefined values:</para>
        /// <list type="table">
        /// <listheader>
        /// <term>Parameter</term>
        /// <description>Value</description>
        /// </listheader>
        /// <item>
        /// <term>Hash Size</term>
        /// <description>64 bits</description>
        /// </item>
        /// <item>
        /// <term>FNV Prime</term>
        /// <description><c>0x00000100000001B3</c></description>
        /// </item>
        /// <item>
        /// <term>Offset Basis</term>
        /// <description><c>0xCBF29CE484222325</c></description>
        /// </item>
        /// <item>
        /// <term>Variant</term>
        /// <description>FNV-1 (Multiply before XOR)</description>
        /// </item>
        /// </list>
        /// </remarks>
        public Fnv164()
            : base(hashSize: 64, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: false)
        {
        }
    }
}