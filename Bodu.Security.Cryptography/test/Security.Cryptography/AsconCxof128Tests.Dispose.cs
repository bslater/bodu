// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="AsconCxof128" /> for unexpected exceptions and contract violations when its
/// public surface is exercised outside of expected usage. Complements the existing well-formed
/// state-machine coverage with edge cases around failed-call state corruption and disposal.
/// </summary>
public partial class AsconCxof128Tests
{
    /// <summary>
    /// Verifies that calling <see cref="AsconCxof128.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsconCxof128();
        sut.Dispose();

        try
        {
            sut.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on AsconCxof128 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="AsconCxof128" /> instance — one
    /// that has never been customised, absorbed, or squeezed — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var sut = new AsconCxof128();

        try
        {
            sut.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched AsconCxof128 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

}
