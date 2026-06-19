// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein{T}.512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>Skein-512</c> variant of the Skein hash function, built on top of the
/// <see cref="Threefish512Cipher" /> tweakable block cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Skein512" /> operates on a 512-bit internal state in 64-byte blocks. It is the primary Skein variant
/// recommended by the 1.3 specification for general-purpose use and is the version originally submitted to the NIST
/// SHA-3 competition. The permitted output sizes are 128, 160, 224, 256, 384, and 512 bits; 512 bits is the default.
/// </para>
/// <para>
/// Supplying a non-empty <see cref="Skein{T}.Key" /> turns the instance into the keyed Skein-MAC-512 variant by
/// prepending a <c>KEY</c> UBI phase to the standard <c>CFG → MSG → OUT</c> pipeline. The key length is not fixed: any
/// byte sequence from zero up to <see cref="Skein{T}.MaxKeySize" /> / 8 bytes is valid.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>State / block size: 512 bits (64 bytes).</description>
/// </item>
/// <item>
/// <description>Output sizes: 128, 160, 224, 256, 384, or 512 bits — default 512.</description>
/// </item>
/// <item>
/// <description>
/// Underlying cipher: <see cref="Threefish512Cipher" /> tweakable block cipher under UBI mode.
/// </description>
/// </item>
/// <item>
/// <description>Optional variable-length key: 0–<see cref="Skein{T}.MaxKeySize" /> / 8 bytes.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Skein-512.</strong> The general-purpose Skein default, recommended by the Skein 1.3
/// specification — pick this when reproducing or interoperating with Skein-based digests. For new code without an
/// interop requirement <see cref="Blake2b" /> is faster on contemporary 64-bit hardware and SHA-2 / SHA-3 are more
/// widely deployed. Use <see cref="Skein256" /> for narrower state, <see cref="Skein1024" /> for the widest.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var skein = new Skein512(); // Skein-512-512 (the canonical configuration)
/// byte[] digest = skein.ComputeHash(message);
///
/// using var skeinMac = new Skein512 { Key = key }; // Skein-MAC-512-512
/// byte[] tag = skeinMac.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="Threefish512Cipher"/> <seealso cref="Skein{T}"/> <seealso cref="Skein256"/>
/// <seealso cref="Skein1024"/> <seealso cref="Threefish512"/>
public sealed class Skein512
    : Skein<Skein512>
{
    /// <summary>The set of output sizes, in bits, permitted by <see cref="Skein512" />.</summary>
    private static readonly int[] s_permittedHashSizes = [128, 160, 224, 256, 384, 512];

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein512" /> class that produces a 512-bit digest.
    /// </summary>
    public Skein512()
        : this(512)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein512" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">
    /// The requested output size, in bits. Must be one of <c>128</c>, <c>160</c>, <c>224</c>, <c>256</c>, <c>384</c>,
    /// or <c>512</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the permitted output sizes for Skein-512.
    /// </exception>
    public Skein512(int hashSize)
        : base(new Threefish512Cipher(new byte[64], new byte[16]), hashSize, s_permittedHashSizes)
    { }
}
