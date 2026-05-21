// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>Skein-1024</c> variant of the Skein hash function, built on top of the
/// <see cref="Threefish1024Cipher" /> tweakable block cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Skein1024" /> operates on a 1024-bit internal state in 128-byte blocks. It is the ultra-conservative
/// Skein variant: each Threefish-1024 call runs 80 rounds over sixteen 64-bit words, giving the widest state and the
/// highest security margin in the Skein family at the cost of reduced throughput for short inputs.
/// </para>
/// <para>
/// The permitted output sizes are 384, 512, and 1024 bits; 1024 bits is the default. Supplying a non-empty
/// <see cref="Skein{T}.Key" /> turns the instance into the keyed Skein-MAC-1024 variant by prepending a <c>KEY</c> UBI
/// phase to the standard <c>CFG → MSG → OUT</c> pipeline.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// State / block size: 1024 bits (128 bytes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Output sizes: 384, 512, or 1024 bits — default 1024.
/// </description>
/// </item>
/// <item>
/// <description>
/// Underlying cipher: <see cref="Threefish1024Cipher" /> tweakable block cipher under UBI mode.
/// </description>
/// </item>
/// <item>
/// <description>
/// Optional variable-length key: 0–<see cref="Skein{T}.MaxKeySize" /> / 8 bytes.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Skein-1024.</strong> The widest-state Skein variant — pick it when you want the largest
/// security margin in the Skein family or when the surrounding system explicitly requires Skein-1024. Throughput on
/// short inputs is lower than <see cref="Skein512" />; for general use the 512-bit variant is the recommended Skein
/// default.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var skein = new Skein1024(); // Skein-1024-1024
/// byte[] digest = skein.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="Threefish1024Cipher"/> <seealso cref="Skein{T}"/> <seealso cref="Skein256"/>
/// <seealso cref="Skein512"/> <seealso cref="Threefish1024"/>
public sealed class Skein1024
    : Skein<Skein1024>
{
    /// <summary>
    /// The set of output sizes, in bits, permitted by <see cref="Skein1024" />.
    /// </summary>
    private static readonly int[] s_permittedHashSizes = [384, 512, 1024];

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein1024" /> class that produces a 1024-bit digest.
    /// </summary>
    public Skein1024()
        : this(1024)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein1024" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">
    /// The requested output size, in bits. Must be one of <c>384</c>, <c>512</c>, or <c>1024</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the permitted output sizes for Skein-1024.
    /// </exception>
    public Skein1024(int hashSize)
        : base(new Threefish1024Cipher(new byte[128], new byte[16]), hashSize, s_permittedHashSizes)
    { }
}
