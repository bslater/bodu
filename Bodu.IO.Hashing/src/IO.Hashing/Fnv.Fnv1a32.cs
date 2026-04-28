// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv1a32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1a</c> 32-bit hash algorithm. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// The FNV-1a variant XORs each input byte before multiplying by the FNV prime, improving avalanche
/// behaviour relative to the original FNV-1 form. The 32-bit configuration uses prime <c>0x01000193</c> and
/// offset basis <c>0x811C9DC5</c>.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Fnv1a32
    : Fnv<Fnv1a32>
{
    private const ulong OffsetBasis = 0x811C9DC5UL;
    private const ulong Prime = 0x01000193UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv1a32" /> class using standard FNV-1a 32-bit parameters.
    /// </summary>
    public Fnv1a32()
        : base(hashSize: 32, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: true)
    {
    }
}
