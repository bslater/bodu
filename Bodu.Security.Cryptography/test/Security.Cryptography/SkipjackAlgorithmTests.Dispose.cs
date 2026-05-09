// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SkipjackAlgorithmTests
{
    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Skipjack" /> instance — one that has
    /// never had its key, IV, or any other property accessed — completes without throwing.
    /// Regression guard for disposal paths that touch lazily-initialised state without null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = CreateAlgorithm();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Skipjack instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="Skipjack.Dispose" /> twice is idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Skipjack threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
