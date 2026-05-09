// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that calling <see cref="Skein{T}.Dispose" /> twice on the same instance is
    /// idempotent and does not throw — the constructor takes ownership of an internal Threefish
    /// cipher, and a double-dispose path could otherwise propagate an inner
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var skein = new TAlgorithm();
        skein.Dispose();

        try
        {
            skein.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on {typeof(TAlgorithm).Name} threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed Skein instance — one that has never had any
    /// property accessed or hashing performed — completes without throwing. Regression guard for
    /// disposal paths that touch the internal Threefish cipher without null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var skein = new TAlgorithm();

        try
        {
            skein.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched {typeof(TAlgorithm).Name} instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
