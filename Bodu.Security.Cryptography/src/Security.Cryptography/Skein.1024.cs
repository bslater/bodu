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
/// <see cref="Skein.Key" /> turns the instance into the keyed Skein-MAC-1024 variant by prepending a <c>KEY</c> UBI
/// phase to the standard <c>CFG → MSG → OUT</c> pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var skein = new Skein1024();                 // Skein-1024-1024
/// byte[] digest = skein.ComputeHash(message);
/// </code>
/// </example>
/// <seealso cref="Threefish1024Cipher" />
public sealed class Skein1024
    : Skein
{
    /// <summary>
    /// The state / block size, in bytes, of the Skein-1024 variant.
    /// </summary>
    public const int BlockSizeBytes = 128;

    /// <summary>
    /// The set of output sizes, in bits, permitted by <see cref="Skein1024" />.
    /// </summary>
    private static readonly int[] PermittedHashSizes = { 384, 512, 1024 };

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
        : base(new Threefish1024Cipher(new byte[BlockSizeBytes], new byte[16]), hashSize, PermittedHashSizes)
    { }

    /// <summary>
    /// Gets the fully qualified algorithm name, including the state size and the configured output size.
    /// </summary>
    /// <returns>A string of the form <c>"Skein-1024-<i>n</i>"</c>, e.g. <c>"Skein-1024-1024"</c>.</returns>
    public string AlgorithmName => $"Skein-1024-{this.HashSize}";
}
