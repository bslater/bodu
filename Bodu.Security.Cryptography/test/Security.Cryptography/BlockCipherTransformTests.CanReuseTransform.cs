// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.CanReuseTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.CanReuseTransform" /> returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void CanReuseTransform_ShouldReturnFalse()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        Assert.IsFalse(transform.CanReuseTransform);
    }
}
