// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that calling <see cref="Blake3.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new Blake3();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Blake3 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Blake3" /> instance — one that
    /// has never had any property accessed or hashing performed — completes without throwing.
    /// Regression guard for disposal paths that touch lazily-initialised state without null
    /// checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new Blake3();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Blake3 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
