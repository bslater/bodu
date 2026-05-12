// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishAlgorithmTests.LegalTweakSizes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class ThreefishAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that reading <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> on a
    /// disposed instance does not throw <see cref="NullReferenceException" /> from a cleared
    /// backing field.
    /// </summary>
    [TestMethod]
    public void LegalTweakSizes_WhenAccessedAfterDispose_ShouldNotThrowUnexpected()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            KeySizes[] sizes = algorithm.LegalTweakSizes;
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
