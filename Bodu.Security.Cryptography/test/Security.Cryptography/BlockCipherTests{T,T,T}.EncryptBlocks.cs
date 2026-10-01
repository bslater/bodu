// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests{T,T,T}.EncryptBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// The run lengths, in blocks, the multi-block tests use: a single block, a short run, runs either side of and
    /// well past 256 blocks, which is where a 16-byte cipher's 4 KiB bulk chunks divide, and a run past 64 KiB for a
    /// 16-byte cipher, where <see cref="AesBlockCipher" /> hands the whole run to the platform in one call.
    /// </summary>
    private static readonly int[] s_blockRunLengths = [1, 3, 255, 256, 257, 520, 4200];

    /// <summary>
    /// Verifies that encrypting a run of blocks in one call produces, for every block, what encrypting it on its own
    /// produces - the ECB semantics every counter mode relies on when it hands a whole run of counters to the cipher.
    /// </summary>
    /// <param name="variant">The cipher configuration under test.</param>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void EncryptBlocks_WhenRunSpansSeveralBlocks_ShouldMatchEncryptPerBlock(TVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        foreach (int blocks in s_blockRunLengths)
        {
            byte[] input = BuildRun(blocks * spec.BlockSize, seed: blocks);
            byte[] expected = new byte[input.Length];
            for (int offset = 0; offset < input.Length; offset += spec.BlockSize)
                cipher.Encrypt(input.AsSpan(offset, spec.BlockSize), expected.AsSpan(offset, spec.BlockSize));

            byte[] actual = new byte[input.Length];
            cipher.EncryptBlocks(input, actual);

            CollectionAssert.AreEqual(expected, actual, $"{blocks} blocks");
        }
    }

    /// <summary>
    /// Builds a deterministic pseudo-random run of <paramref name="length" /> bytes.
    /// </summary>
    /// <param name="length">The run's length, in bytes.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The run.</returns>
    private static byte[] BuildRun(int length, int seed)
    {
        byte[] run = new byte[length];
        new Random(seed).NextBytes(run);
        return run;
    }
}
