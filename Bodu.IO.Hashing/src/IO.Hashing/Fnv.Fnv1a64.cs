// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv1a64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1a</c> 64-bit hash algorithm. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// The FNV-1a variant XORs each input byte before multiplying by the FNV prime, improving avalanche
/// behaviour relative to the original FNV-1 form. The 64-bit configuration uses prime <c>0x100000001B3</c>
/// and offset basis <c>0xCBF29CE484222325</c>.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Fnv1a64
    : Fnv<Fnv1a64>
{
    private const ulong OffsetBasis = 0xCBF29CE484222325UL;
    private const ulong Prime = 0x00000100000001B3UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv1a64" /> class using standard FNV-1a 64-bit parameters.
    /// </summary>
    public Fnv1a64()
        : base(hashSize: 64, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: true)
    {
    }
}
