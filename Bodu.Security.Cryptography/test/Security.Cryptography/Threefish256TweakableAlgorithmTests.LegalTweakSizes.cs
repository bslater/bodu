// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.LegalTweakSizes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that reading <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> on a
    /// disposed instance does not throw <see cref="NullReferenceException" /> from a cleared
    /// backing field.
    /// </summary>
    [TestMethod]
    public void LegalTweakSizes_WhenAccessedAfterDispose_ShouldNotThrowUnexpected()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            var sizes = algorithm.LegalTweakSizes;
            Assert.IsNotNull(sizes);
        }
        catch (ObjectDisposedException)
        {
            // Acceptable — disposal contract may forbid further reads.
        }
        catch (Exception ex) when (ex is not ObjectDisposedException)
        {
            Assert.Fail(
                $"Reading LegalTweakSizes after Dispose threw an unexpected exception: " +
                $"{ex.GetType().Name}: {ex.Message}");
        }
    }
}
