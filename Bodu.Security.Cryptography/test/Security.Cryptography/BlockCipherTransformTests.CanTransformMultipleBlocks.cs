// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.CanTransformMultipleBlocks.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

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
