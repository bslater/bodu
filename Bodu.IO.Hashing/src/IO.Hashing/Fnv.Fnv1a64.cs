// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv1a64.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1a</c> 64-bit hash algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The FNV-1a variant XORs each input byte before multiplying by the FNV prime, improving avalanche behavior relative
/// to the original FNV-1 form. The 64-bit configuration uses prime <c>0x100000001B3</c> and offset basis
/// <c>0xCBF29CE484222325</c>.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: 64 bits (8 bytes), little-endian.
/// </description>
/// </item>
/// <item>
/// <description>
/// Offset basis: <c>0xCBF29CE484222325</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// FNV prime: <c>0x00000100000001B3</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Variant: FNV-1a (XOR, then multiply).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Fnv1a64.</strong> The recommended FNV variant when 32 bits would invite collisions — wider
/// key spaces, content fingerprints, distributed cache keys. For inputs longer than ~16 bytes,
/// <see cref="Bodu.IO.Hashing.MurmurHash3_128" /> or <see cref="Bodu.IO.Hashing.CityHash64" /> generally distribute
/// better and run faster on modern 64-bit CPUs.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var fnv = new Fnv1a64();
/// byte[] digest = fnv.ComputeHash(System.Text.Encoding.UTF8.GetBytes("user@example.com"));
///]]>
/// </code>
/// </example>
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
