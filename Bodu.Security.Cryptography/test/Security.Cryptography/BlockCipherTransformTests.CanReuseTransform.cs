// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.CanReuseTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.CanReuseTransform" /> returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void CanReuseTransform_ShouldReturnFalse()
    {
        using var transform = CreateAlgorithm();
        Assert.IsFalse(transform.CanReuseTransform);
    }
}
