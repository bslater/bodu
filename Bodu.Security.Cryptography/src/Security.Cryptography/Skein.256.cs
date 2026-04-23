// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
/// Supplying a non-empty <see cref="Skein.Key" /> turns the instance into the keyed Skein-MAC-256 variant by prepending
/// a <c>KEY</c> UBI phase to the standard <c>CFG → MSG → OUT</c> pipeline. The key length is not fixed: any byte
/// sequence is valid.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var skein = new Skein256();                  // Skein-256-256
/// byte[] digest = skein.ComputeHash(message);
///
/// using var skeinMac = new Skein256 { Key = key };   // Skein-MAC-256-256
/// byte[] tag = skeinMac.ComputeHash(message);
/// </code>
/// </example>
/// <seealso cref="Threefish256Cipher" />
public sealed class Skein256
    : Skein
{
    /// <summary>
    /// The state / block size, in bytes, of the Skein-256 variant.
    /// </summary>
    public const int BlockSizeBytes = 32;

    /// <summary>
    /// The set of output sizes, in bits, permitted by <see cref="Skein256" />.
    /// </summary>
    private static readonly int[] PermittedHashSizes = { 128, 160, 224, 256 };

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
        : base(new Threefish256Cipher(new byte[BlockSizeBytes], new byte[16]), hashSize, PermittedHashSizes)
    { }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the state size and the configured output size.
    /// </summary>
    /// <returns>A string of the form <c>"Skein-256-<i>n</i>"</c>, e.g. <c>"Skein-256-256"</c>.</returns>
    public string AlgorithmName => $"Skein-256-{this.HashSize}";
}
