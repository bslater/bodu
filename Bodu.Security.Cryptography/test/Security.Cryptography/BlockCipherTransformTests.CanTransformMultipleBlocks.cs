// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.CanTransformMultipleBlocks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.CanTransformMultipleBlocks" /> returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void CanTransformMultipleBlocks_ShouldReturnTrue()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        Assert.IsTrue(transform.CanTransformMultipleBlocks);
    }
}
