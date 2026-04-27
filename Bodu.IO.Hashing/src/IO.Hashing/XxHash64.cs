// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHash64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

/// <summary>
/// Computes a 64-bit (8-byte) non-cryptographic hash using the <c>xxHash64</c> algorithm by Yann Collet.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This type is superseded by <see cref="System.IO.Hashing.XxHash64" />, which is part of the .NET base
/// class library and available on .NET 7 and later. Prefer the BCL implementation for all new code.
/// </para>
/// <para>
/// <see cref="XxHash64" /> is optimised for 64-bit platforms. It maintains four 64-bit accumulators that
/// process input in 32-byte stripes when the input is 32 bytes or longer. For shorter input, a simpler
/// single-accumulator path is used. The tail of up to 31 bytes is consumed in 8-byte, 4-byte, and 1-byte
/// steps before a three-round avalanche finalisation.
/// </para>
/// <para>
/// A 64-bit seed may be supplied at construction time to vary the output for identical input.
/// </para>
/// <note type="important">
/// This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for password hashing,
/// digital signatures, or any application requiring adversarial collision resistance.
/// </note>
/// </remarks>
[Obsolete("Use System.IO.Hashing.XxHash64 instead. This implementation is superseded by the built-in BCL type available on .NET 7 and later.")]
public sealed class XxHash64
    : XxHash<XxHash64>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash64" /> class with a seed of zero.
    /// </summary>
    public XxHash64()
        : this(0uL)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash64" /> class with the specified seed.
    /// </summary>
    /// <param name="seed">The 64-bit seed used to initialise the hash state.</param>
    public XxHash64(ulong seed)
        : base(8)
    {
        this.Seed = seed;
    }

    /// <summary>
    /// Gets the 64-bit seed used to initialise the hash computation.
    /// </summary>
    /// <returns>The seed value supplied at construction time, or zero if no seed was specified.</returns>
    public ulong Seed { get; }

    /// <summary>
    /// Computes the 64-bit xxHash of the provided input span.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>An 8-byte array containing the little-endian encoded 64-bit hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        int len = source.Length;
        int pos = 0;
        ulong acc;

        if (len >= 32)
        {
            // Initialise four accumulators from the seed.
            ulong v1 = unchecked(this.Seed + Prime64_1 + Prime64_2);
            ulong v2 = unchecked(this.Seed + Prime64_2);
            ulong v3 = this.Seed;
            ulong v4 = unchecked(this.Seed - Prime64_1);

            // Process all full 32-byte stripes.
            while (pos + 32 <= len)
            {
                v1 = Round64(v1, BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(pos, 8)));
                v2 = Round64(v2, BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(pos + 8, 8)));
                v3 = Round64(v3, BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(pos + 16, 8)));
                v4 = Round64(v4, BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(pos + 24, 8)));
                pos += 32;
            }

            // Merge accumulators.
            acc = unchecked(
                RotateLeft64(v1, 1) +
                RotateLeft64(v2, 7) +
                RotateLeft64(v3, 12) +
                RotateLeft64(v4, 18));

            acc = MergeAccumulator64(acc, v1);
            acc = MergeAccumulator64(acc, v2);
            acc = MergeAccumulator64(acc, v3);
            acc = MergeAccumulator64(acc, v4);
        }
        else
        {
            acc = unchecked(this.Seed + Prime64_5);
        }

        acc = unchecked(acc + (ulong)len);

        // Consume remaining 8-byte groups.
        while (pos + 8 <= len)
        {
            acc ^= Round64(0uL, BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(pos, 8)));
            acc = unchecked(RotateLeft64(acc, 27) * Prime64_1 + Prime64_4);
            pos += 8;
        }

        // Consume remaining 4-byte groups.
        while (pos + 4 <= len)
        {
            acc ^= unchecked((ulong)BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(pos, 4)) * Prime64_1);
            acc = unchecked(RotateLeft64(acc, 23) * Prime64_2 + Prime64_3);
            pos += 4;
        }

        // Consume remaining single bytes.
        while (pos < len)
        {
            acc ^= unchecked((ulong)source[pos] * Prime64_5);
            acc = unchecked(RotateLeft64(acc, 11) * Prime64_1);
            pos++;
        }

        // Avalanche finalisation.
        acc ^= acc >> 33;
        acc = unchecked(acc * Prime64_2);
        acc ^= acc >> 29;
        acc = unchecked(acc * Prime64_3);
        acc ^= acc >> 32;

        byte[] result = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(result, acc);
        return result;
    }
}
