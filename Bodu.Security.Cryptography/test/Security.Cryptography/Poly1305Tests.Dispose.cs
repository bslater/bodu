// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that calling <see cref="Poly1305.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var poly = new Poly1305();
        poly.Dispose();

        try
        {
            poly.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Poly1305 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Poly1305" /> instance — one whose
    /// key was set by the constructor but never used — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var poly = new Poly1305();

        try
        {
            poly.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Poly1305 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
