// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv132.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1</c> 32-bit hash algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The FNV-1 variant performs multiplication before XOR. The 32-bit configuration uses prime <c>0x01000193</c> and
/// offset basis <c>0x811C9DC5</c>.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 32 bits (4 bytes), little-endian.
/// </description>
/// </item>
/// <item>
/// <description>
/// Offset basis: <c>0x811C9DC5</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// FNV prime: <c>0x01000193</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Variant: FNV-1 (multiply, then XOR).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Fnv132.</strong> Pick <see cref="Fnv132" /> only when reproducing a digest from existing
/// FNV-1 32-bit consumers. For new code prefer <see cref="Fnv1a32" />: same parameters, better avalanche, identical
/// cost.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var fnv = new Fnv132();
/// byte[] digest = fnv.ComputeHash(System.Text.Encoding.UTF8.GetBytes("key"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class Fnv132
    : Fnv<Fnv132>
{
    private const ulong OffsetBasis = 0x811C9DC5UL;
    private const ulong Prime = 0x01000193UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv132" /> class using standard FNV-1 32-bit parameters.
    /// </summary>
    public Fnv132()
        : base(hashSize: 32, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: false)
    {
    }
}
