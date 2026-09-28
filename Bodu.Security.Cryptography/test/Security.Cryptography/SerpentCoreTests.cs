// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="SerpentCore" />, the Serpent round function behind <see cref="Serpent128Cipher" /> and the
/// wide-block variants, grouped into member-named partial files. The S-box circuits are held to the S-box tables over
/// every 4-bit input in every bit position, and the rounds to <see cref="SerpentReference" />, the table-driven
/// Serpent-128 the core replaced.
/// </summary>
[TestClass]
public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Gets bitsliced S-box inputs: words whose 32 bit columns hold every 4-bit value twice, in each of four
    /// arrangements, followed by seeded random words.
    /// </summary>
    /// <value>The inputs, four words each.</value>
    private static IEnumerable<uint[]> BitslicedInputs
    {
        get
        {
            // Column i holds the nibble (i + shift) mod 16, so every value meets every bit position across the shifts.
            for (int shift = 0; shift < 16; shift += 4)
            {
                uint[] words = new uint[4];
                for (int column = 0; column < 32; column++)
                {
                    int nibble = (column + shift) & 15;
                    for (int bit = 0; bit < 4; bit++)
                        words[bit] |= (uint)((nibble >> bit) & 1) << column;
                }

                yield return words;
            }

            var random = new Random(0x5E4F_0001);
            for (int i = 0; i < 256; i++)
                yield return [(uint)random.NextInt64(0, 1L << 32), (uint)random.NextInt64(0, 1L << 32), (uint)random.NextInt64(0, 1L << 32), (uint)random.NextInt64(0, 1L << 32)];
        }
    }

    /// <summary>
    /// Returns a new array of seeded random bytes.
    /// </summary>
    /// <param name="random">The source of the bytes.</param>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The bytes.</returns>
    private static byte[] NextBytes(Random random, int length)
    {
        byte[] bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }
}
