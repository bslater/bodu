// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv132.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1</c> 32-bit hash algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Fowler�Noll�Vo (FNV) family of hash functions provides a fast, non-cryptographic mechanism for producing fixed-size hash values
/// from arbitrary-length input data. The FNV-1 variant performs multiplication before XOR and is the original formulation of the FNV hash.
/// </para>
/// <para>
/// The <see cref="Fnv132" /> implementation uses a 32-bit hash size with a prime of <c>0x01000193</c> and an offset basis of
/// <c>0x811C9DC5</c>. This configuration is suitable for lightweight checksums, hash tables, and general-purpose fingerprinting where
/// cryptographic security is not required.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Fnv132 : Fnv
{
    private const ulong OffsetBasis = 0x811C9DC5UL;
    private const ulong Prime = 0x01000193UL;

    /// <summary>
    /// Initialises a new instance of the <see cref="Fnv132" /> class using standard FNV-1 32-bit parameters.
    /// </summary>
    /// <remarks>
    /// <para>This constructor configures the FNV algorithm to use the 32-bit variant of FNV-1, with the following predefined values:</para>
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
    /// <description>FNV-1 (Multiply before XOR)</description>
    /// </item>
    /// </list>
    /// </remarks>
    public Fnv132()
        : base(hashSize: 32, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: false)
    {
    }
}
