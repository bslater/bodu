// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.BlockSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="IBlockCipher.BlockSize" /> (in bits) returns the value declared in the
    /// <see cref="BlockCipherSpecification" /> (in bytes) for the given variant.
    /// </summary>
    /// <param name="variant">The cipher variant that determines the expected block size.</param>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants))]
    public void BlockSize_WhenAccessed_ShouldReturnExpected(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher engine = CreateBlockCipher(variant);

        Assert.AreEqual(
            specification.BlockSize * 8,
            engine.BlockSize,
            $"[{variant}] Expected BlockSize of {specification.BlockSize * 8} bits but got {engine.BlockSize}.");
    }
}
