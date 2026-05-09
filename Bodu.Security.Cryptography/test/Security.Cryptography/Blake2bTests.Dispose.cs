// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake2bTests
{
    /// <summary>
    /// Verifies that calling <see cref="Blake2b.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new Blake2b();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Blake2b threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Blake2b" /> instance — one that
    /// has never had its key, hash size, or any other property accessed — completes without
    /// throwing. Regression guard for disposal paths that touch lazily-initialised state without
    /// null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new Blake2b();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Blake2b instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
