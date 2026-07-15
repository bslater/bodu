// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv1a32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1a</c> 32-bit hash algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The FNV-1a variant XORs each input byte before multiplying by the FNV prime, improving avalanche behavior relative
/// to the original FNV-1 form. The 32-bit configuration uses prime <c>0x01000193</c> and offset basis <c>0x811C9DC5</c>
/// .
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: 32 bits (4 bytes), big-endian.</description>
/// </item>
/// <item>
/// <description>Offset basis: <c>0x811C9DC5</c>.</description>
/// </item>
/// <item>
/// <description>FNV prime: <c>0x01000193</c>.</description>
/// </item>
/// <item>
/// <description>Variant: FNV-1a (XOR, then multiply).</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Fnv1a32.</strong> The recommended 32-bit FNV variant — better avalanche than
/// <see cref="Fnv132" /> at identical cost. Pick it for hash-table keying of short strings or identifiers when 32 bits
/// is enough; reach for <see cref="MurmurHash3_32" /> on inputs longer than ~16 bytes or when SMHasher quality matters,
/// and for <see cref="Fnv1a64" /> when 64 bits would meaningfully reduce collisions.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing;
/// using Bodu.IO.Hashing.Extensions;
///
/// var fnv = new Fnv1a32();
/// byte[] digest = fnv.ComputeHash(System.Text.Encoding.UTF8.GetBytes("user@example.com"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class Fnv1a32
    : Fnv<Fnv1a32>
{
    /// <summary>The FNV-1a 32-bit offset basis used as the initial hash state.</summary>
    private const ulong OffsetBasis = 0x811C9DC5UL;

    /// <summary>The FNV-1a 32-bit prime multiplied into the hash state for each input byte.</summary>
    private const ulong Prime = 0x01000193UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv1a32" /> class using standard FNV-1a 32-bit parameters.
    /// </summary>
    public Fnv1a32()
        : base(hashSize: 32, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: true)
    {
    }
}
