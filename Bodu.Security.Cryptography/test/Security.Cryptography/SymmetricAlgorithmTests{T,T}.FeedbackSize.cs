// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests{T,T}.FeedbackSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that setting an invalid feedback size throws a CryptographicException.
    /// </summary>
    /// <param name="feedbackSize">The feedback size to test.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(null)]
    public void FeedbackSize_WhenInvalid_ShouldThrowExactly(int? feedbackSize)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        int sizeToTest = feedbackSize ?? algorithm.BlockSize + 1;
        Assert.ThrowsExactly<CryptographicException>(() => algorithm.FeedbackSize = sizeToTest);
    }
}
