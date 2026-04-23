// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv1a64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1a</c> 64-bit hash algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The Fowler�Noll�Vo (FNV) family of hash functions provides a fast, non-cryptographic mechanism for producing fixed-size hash values
/// from arbitrary-length input data. The FNV-1a variant improves diffusion by applying a bitwise XOR before multiplying each byte with
/// a fixed FNV prime.
/// </para>
/// <para>
/// The <see cref="Fnv1a64" /> implementation uses a 64-bit hash size with a prime of <c>0x00000100000001B3</c> and an offset basis of
/// <c>0xCBF29CE484222325</c>. This configuration is widely used for hash tables, fingerprinting, and checksumming scenarios where speed
/// and low collision rates are desired.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Fnv1a64 : Fnv
{
    private const ulong OffsetBasis = 0xCBF29CE484222325UL;
    private const ulong Prime = 0x00000100000001B3UL;

    /// <summary>
    /// Initialises a new instance of the <see cref="Fnv1a64" /> class using standard FNV-1a 64-bit parameters.
    /// </summary>
    /// <remarks>
    /// <para>This constructor configures the FNV algorithm to use the 64-bit variant of FNV-1a, with the following predefined values:</para>
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
    /// <description>FNV-1a (XOR before multiply)</description>
    /// </item>
    /// </list>
    /// </remarks>
    public Fnv1a64()
        : base(hashSize: 64, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: true)
    { }
}
