// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein.256.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>Skein-256</c> variant of the Skein hash function, built on top of the
/// <see cref="Threefish256Cipher" /> tweakable block cipher. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Skein256" /> operates on a 256-bit internal state in 32-byte blocks. The permitted output sizes are 128,
/// 160, 224, and 256 bits; 256 bits is the default and matches the Skein 1.3 specification's canonical truncation set
/// for this state size.
/// </para>
/// <para>
/// Supplying a non-empty <see cref="Skein.Key" /> turns the instance into the keyed Skein-MAC-256 variant by
/// prepending a <c>KEY</c> UBI phase to the standard <c>CFG → MSG → OUT</c> pipeline. The key length is not fixed: any
/// byte sequence from zero up to <see cref="Skein.MaxKeySize" /> / 8 bytes is valid.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>State / block size: 256 bits (32 bytes).</description>
/// </item>
/// <item>
/// <description>Output sizes: 128, 160, 224, or 256 bits — default 256.</description>
/// </item>
/// <item>
/// <description>
/// Underlying cipher: <see cref="Threefish256Cipher" /> tweakable block cipher under UBI mode.
/// </description>
/// </item>
/// <item>
/// <description>Optional variable-length key: 0–<see cref="Skein.MaxKeySize" /> / 8 bytes.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose Skein-256.</strong> The narrowest Skein variant — pick it when 32-byte output is enough and
/// the surrounding system has standardized on Skein. <see cref="Skein512" /> is the more common default and offers a
/// stronger security margin; <see cref="Skein1024" /> is for cases that want the widest state. For new general-purpose
/// hashing without an interop constraint <see cref="Blake2b" /> or SHA-2 is the more widely deployed default.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var skein = new Skein256(); // Skein-256-256
/// byte[] digest = skein.ComputeHash(message);
///
/// using var skeinMac = new Skein256 { Key = key }; // Skein-MAC-256-256
/// byte[] tag = skeinMac.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="Threefish256Cipher"/> <seealso cref="Skein"/> <seealso cref="Skein512"/>
/// <seealso cref="Skein1024"/> <seealso cref="Threefish256"/>
public sealed class Skein256
    : Skein
{
    /// <summary>The set of output sizes, in bits, permitted by <see cref="Skein256" />.</summary>
    private static readonly int[] s_permittedHashSizes = [128, 160, 224, 256];

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein256" /> class that produces a 256-bit digest.
    /// </summary>
    public Skein256()
        : this(256)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Skein256" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">
    /// The requested output size, in bits. Must be one of <c>128</c>, <c>160</c>, <c>224</c>, or <c>256</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the permitted output sizes for Skein-256.
    /// </exception>
    public Skein256(int hashSize)
        : base(new Threefish256Cipher(new byte[32], new byte[16]), hashSize, s_permittedHashSizes)
    { }
}
