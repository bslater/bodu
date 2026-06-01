// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fnv.Fnv164.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Computes the hash for the input data using the <c>FNV-1</c> 64-bit hash algorithm. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The FNV-1 variant performs multiplication before XOR. The 64-bit configuration uses prime <c>0x100000001B3</c> and
/// offset basis <c>0xCBF29CE484222325</c>.
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
/// Variant: FNV-1 (multiply, then XOR).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Fnv164.</strong> Pick <see cref="Fnv164" /> only when reproducing a digest from existing
/// FNV-1 64-bit consumers. For new code prefer <see cref="Fnv1a64" />: same parameters, better avalanche, identical
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
/// var fnv = new Fnv164();
/// byte[] digest = fnv.ComputeHash(System.Text.Encoding.UTF8.GetBytes("key"));
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class Fnv164
    : Fnv<Fnv164>
{
    private const ulong OffsetBasis = 0xCBF29CE484222325UL;
    private const ulong Prime = 0x00000100000001B3UL;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fnv164" /> class using standard FNV-1 64-bit parameters.
    /// </summary>
    public Fnv164()
        : base(hashSize: 64, prime: Prime, offsetBasis: OffsetBasis, useFnv1a: false)
    {
    }
}
