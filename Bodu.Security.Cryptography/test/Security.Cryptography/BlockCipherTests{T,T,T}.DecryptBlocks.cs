// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests{T,T,T}.DecryptBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// Verifies that decrypting a run of blocks in one call produces, for every block, what decrypting it on its own
    /// produces.
    /// </summary>
    /// <param name="variant">The cipher configuration under test.</param>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void DecryptBlocks_WhenRunSpansSeveralBlocks_ShouldMatchDecryptPerBlock(TVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);

        foreach (int blocks in s_blockRunLengths)
        {
            byte[] input = BuildRun(blocks * spec.BlockSize, seed: -blocks);
            byte[] expected = new byte[input.Length];
            for (int offset = 0; offset < input.Length; offset += spec.BlockSize)
                cipher.Decrypt(input.AsSpan(offset, spec.BlockSize), expected.AsSpan(offset, spec.BlockSize));

            byte[] actual = new byte[input.Length];
            cipher.DecryptBlocks(input, actual);

            CollectionAssert.AreEqual(expected, actual, $"{blocks} blocks");
        }
    }
}
