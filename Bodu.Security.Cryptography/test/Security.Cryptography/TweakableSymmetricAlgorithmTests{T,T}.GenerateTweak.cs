// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests{T,T}.GenerateTweak.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.GenerateTweak" /> creates a tweak of expected length.
    /// </summary>
    [TestMethod]
    public void GenerateTweak_WhenCalled_ShouldInitializeTweakCorrectly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        int size = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.TweakSize = size;
        algorithm.GenerateTweak();

        Assert.HasCount(size / 8, algorithm.Tweak);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.GenerateTweak" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateTweak_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(algorithm.GenerateTweak);
    }
}
