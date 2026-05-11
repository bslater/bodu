// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.GenerateTweak.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Threefish.GenerateTweak" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateTweak_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        Threefish256 algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.GenerateTweak();
        });
    }
}
