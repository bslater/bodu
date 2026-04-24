// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein.512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// Supplying a non-empty <see cref="Skein{T}.Key" /> turns the instance into the keyed Skein-MAC-512 variant by prepending
/// a <c>KEY</c> UBI phase to the standard <c>CFG → MSG → OUT</c> pipeline. The key length is not fixed: any byte
/// sequence from zero up to <see cref="Skein{T}.MaxKeySizeBytes" /> bytes is valid.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var skein = new Skein512();                  // Skein-512-512 (the canonical configuration)
/// byte[] digest = skein.ComputeHash(message);
///
/// using var skeinMac = new Skein512 { Key = key };   // Skein-MAC-512-512
/// byte[] tag = skeinMac.ComputeHash(message);
/// </code>
/// </example>
/// <seealso cref="Threefish512Cipher" />
public sealed class Skein512
    : Skein<Skein512>
{
    /// <summary>
    /// The state / block size, in bytes, of the Skein-512 variant.
    /// </summary>
    public const int BlockSizeBytes = 64;

    /// <summary>
    /// The set of output sizes, in bits, permitted by <see cref="Skein512" />.
    /// </summary>
    private static readonly int[] PermittedHashSizes = { 128, 160, 224, 256, 384, 512 };

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
    /// The requested output size, in bits. Must be one of <c>128</c>, <c>160</c>, <c>224</c>, <c>256</c>, <c>384</c>, or <c>512</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the permitted output sizes for Skein-512.
    /// </exception>
    public Skein512(int hashSize)
        : base(new Threefish512Cipher(new byte[BlockSizeBytes], new byte[16]), hashSize, PermittedHashSizes)
    { }
}
