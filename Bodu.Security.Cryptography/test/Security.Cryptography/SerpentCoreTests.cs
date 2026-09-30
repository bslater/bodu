// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="SerpentCore" />, the Serpent round function behind <see cref="Serpent128Cipher" /> and the
/// wide-block variants, grouped into member-named partial files. The S-box circuits are held to the S-box tables over
/// every 4-bit input in every bit position, the rounds to <see cref="SerpentReference" />, the table-driven Serpent-128
/// the core replaced, and every many-block kernel, driven explicitly whichever one dispatch picks, to the single-block
/// entry points.
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
    /// Yields seeded keys and tweaks for a wide block, each with its oracle, its round keys with the tweak folded in,
    /// and seeded blocks to transform under it.
    /// </summary>
    /// <param name="words">The number of words in a block: 8, 16 or 32.</param>
    /// <param name="rounds">The number of rounds, a positive multiple of 8.</param>
    /// <param name="seed">The seed of the generator.</param>
    /// <returns>The cases, each named for its failure messages.</returns>
    private static IEnumerable<(string Name, SerpentWideReference Reference, uint[] RoundKeys, byte[][] Blocks)> WideBlockCases(int words, int rounds, int seed)
    {
        var random = new Random(seed);
        for (int c = 0; c < 4; c++)
        {
            byte[] key = new byte[words * 4];
            byte[] tweak = new byte[16];
            random.NextBytes(key);
            random.NextBytes(tweak);
            var reference = new SerpentWideReference(key, tweak, rounds);

            byte[][] blocks = new byte[4][];
            for (int b = 0; b < blocks.Length; b++)
            {
                blocks[b] = new byte[words * 4];
                random.NextBytes(blocks[b]);
            }

            yield return ($"seeded key {c}", reference, reference.FoldedRoundKeys(), blocks);
        }
    }

    /// <summary>
    /// Encrypts each block of a run on its own with the single-block entry point.
    /// </summary>
    /// <param name="roundKeys">The round keys.</param>
    /// <param name="input">The plaintext, a whole number of blocks.</param>
    /// <returns>The ciphertext.</returns>
    private static byte[] EncryptPerBlock(uint[] roundKeys, byte[] input)
    {
        byte[] output = new byte[input.Length];
        for (int offset = 0; offset < input.Length; offset += SerpentCore.BlockBytes)
            SerpentCore.EncryptBlock(roundKeys, input.AsSpan(offset, SerpentCore.BlockBytes), output.AsSpan(offset, SerpentCore.BlockBytes));

        return output;
    }

    /// <summary>
    /// Decrypts each block of a run on its own with the single-block entry point.
    /// </summary>
    /// <param name="roundKeys">The round keys.</param>
    /// <param name="input">The ciphertext, a whole number of blocks.</param>
    /// <returns>The plaintext.</returns>
    private static byte[] DecryptPerBlock(uint[] roundKeys, byte[] input)
    {
        byte[] output = new byte[input.Length];
        for (int offset = 0; offset < input.Length; offset += SerpentCore.BlockBytes)
            SerpentCore.DecryptBlock(roundKeys, input.AsSpan(offset, SerpentCore.BlockBytes), output.AsSpan(offset, SerpentCore.BlockBytes));

        return output;
    }

    /// <summary>
    /// Parses a kernel name, marking the test inconclusive when the processor cannot run that kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static SerpentCore.KernelKind ParseSupportedKernel(string name)
    {
        SerpentCore.KernelKind kernel = Enum.Parse<SerpentCore.KernelKind>(name);
        if (!SerpentCore.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
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
