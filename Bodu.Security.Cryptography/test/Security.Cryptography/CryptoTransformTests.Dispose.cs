// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class CryptoTransformTests<TCryptoTransform>
{
    /// <summary>
    /// Verifies that disposing the transform multiple times is safe and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        using TCryptoTransform transform = CreateAlgorithm();

        transform.Dispose();
        transform.Dispose();
    }
}