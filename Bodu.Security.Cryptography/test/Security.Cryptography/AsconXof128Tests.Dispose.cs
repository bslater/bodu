// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconXof128Tests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconXof128Tests
{
    /// <summary>
    /// Verifies that calling <see cref="AsconXof128.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsconXof128();
        sut.Dispose();

        try
        {
            sut.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on AsconXof128 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="AsconXof128" /> instance — one
    /// that has never been absorbed, squeezed, or had any property accessed — completes without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var sut = new AsconXof128();

        try
        {
            sut.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched AsconXof128 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
