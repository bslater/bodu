// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHash32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

/// <summary>
/// Computes a 32-bit (4-byte) non-cryptographic hash using the <c>xxHash32</c> algorithm by Yann Collet.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This type is superseded by <see cref="System.IO.Hashing.XxHash32" />, which is part of the .NET base
/// class library and available on .NET 6 and later. Prefer the BCL implementation for all new code.
/// </para>
/// <para>
/// <see cref="XxHash32" /> is optimised for 32-bit platforms. It maintains four 32-bit accumulators that
/// process input in 16-byte stripes when the input is 16 bytes or longer. For shorter input, a simpler
/// single-accumulator path is used. The tail of up to 15 bytes is consumed in 4-byte and 1-byte steps
/// before a four-round avalanche finalisation.
/// </para>
/// <para>
/// A 32-bit seed may be supplied at construction time to vary the output for identical input.
/// </para>
/// <note type="important">
/// This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for password hashing,
/// digital signatures, or any application requiring adversarial collision resistance.
/// </note>
/// </remarks>
[Obsolete("Use System.IO.Hashing.XxHash32 instead. This implementation is superseded by the built-in BCL type available on .NET 6 and later.")]
public sealed class XxHash32
    : XxHash<XxHash32>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash32" /> class with a seed of zero.
    /// </summary>
    public XxHash32()
        : this(0u)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash32" /> class with the specified seed.
    /// </summary>
    /// <param name="seed">The 32-bit seed used to initialise the hash state.</param>
    public XxHash32(uint seed)
        : base(4)
    {
        this.Seed = seed;
    }

    /// <summary>
    /// Gets the 32-bit seed used to initialise the hash computation.
    /// </summary>
    /// <returns>The seed value supplied at construction time, or zero if no seed was specified.</returns>
    public uint Seed { get; }

    /// <summary>
    /// Computes the 32-bit xxHash of the provided input span.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>A 4-byte array containing the little-endian encoded 32-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        int len = source.Length;
        int pos = 0;
        uint acc;

        if (len >= 16)
        {
            // Initialise four accumulators from the seed.
            uint v1 = unchecked(this.Seed + Prime32_1 + Prime32_2);
            uint v2 = unchecked(this.Seed + Prime32_2);
            uint v3 = this.Seed;
            uint v4 = unchecked(this.Seed - Prime32_1);

            // Process all full 16-byte stripes.
            while (pos + 16 <= len)
            {
                v1 = Round32(v1, BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(pos, 4)));
                v2 = Round32(v2, BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(pos + 4, 4)));
                v3 = Round32(v3, BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(pos + 8, 4)));
                v4 = Round32(v4, BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(pos + 12, 4)));
                pos += 16;
            }

            // Merge accumulators.
            acc = unchecked(
                RotateLeft32(v1, 1) +
                RotateLeft32(v2, 7) +
                RotateLeft32(v3, 12) +
                RotateLeft32(v4, 18));
        }
        else
        {
            acc = unchecked(this.Seed + Prime32_5);
        }

        acc = unchecked(acc + (uint)len);

        // Consume remaining 4-byte groups.
        while (pos + 4 <= len)
        {
            acc = unchecked(acc + BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(pos, 4)) * Prime32_3);
            acc = unchecked(RotateLeft32(acc, 17) * Prime32_4);
            pos += 4;
        }

        // Consume remaining single bytes.
        while (pos < len)
        {
            acc = unchecked(acc + (uint)source[pos] * Prime32_5);
            acc = unchecked(RotateLeft32(acc, 11) * Prime32_1);
            pos++;
        }

        // Avalanche finalisation.
        acc ^= acc >> 15;
        acc = unchecked(acc * Prime32_2);
        acc ^= acc >> 13;
        acc = unchecked(acc * Prime32_3);
        acc ^= acc >> 16;

        byte[] result = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(result, acc);
        return result;
    }

    /// <summary>
    /// Applies a single xxHash32 accumulator mixing round.
    /// </summary>
    /// <param name="acc">The current accumulator value.</param>
    /// <param name="lane">The 32-bit lane word to fold in.</param>
    /// <returns>The updated accumulator value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Round32(uint acc, uint lane)
    {
        acc = unchecked(acc + lane * Prime32_2);
        acc = RotateLeft32(acc, 13);
        return unchecked(acc * Prime32_1);
    }
}
