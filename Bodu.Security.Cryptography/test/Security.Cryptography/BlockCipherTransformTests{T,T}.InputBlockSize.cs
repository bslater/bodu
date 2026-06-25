// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests{T,T}.InputBlockSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
{
    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.InputBlockSize" /> is greater than zero.
    /// </summary>
    [TestMethod]
    public void InputBlockSize_ShouldBeGreaterThanZero()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        Assert.IsGreaterThan(0, transform.InputBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="BlockCipherTransform.InputBlockSize" /> equals
    /// <see cref="BlockCipherTransform.OutputBlockSize" />.
    /// </summary>
    [TestMethod]
    public void InputBlockSize_ShouldEqualOutputBlockSize()
    {
        using TCryptoTransform transform = CreateAlgorithm();
        Assert.AreEqual(transform.InputBlockSize, transform.OutputBlockSize);
    }
}
