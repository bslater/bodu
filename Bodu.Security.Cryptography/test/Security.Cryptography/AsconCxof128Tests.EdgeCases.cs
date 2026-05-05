// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.EdgeCases.cs" company="PlaceholderCompany">
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
    /// Verifies that a failed <see cref="AsconXof{T}.Absorb" /> call (after <see cref="AsconXof{T}.Squeeze" />
    /// has begun) leaves the customisation state corrupted so that a subsequent
    /// <see cref="AsconCxof128.Customize" /> call throws <see cref="InvalidOperationException" />
    /// citing prior absorption — even though no absorption actually succeeded. Documents the
    /// observable behaviour of <c>_absorbed = true</c> being assigned before <c>base.Absorb</c>'s
    /// state guard runs.
    /// </summary>
    [TestMethod]
    public void Customize_AfterFailedAbsorbCall_ShouldThrowInvalidOperationException()
    {
        using AsconCxof128 sut = new AsconCxof128();
        sut.Squeeze(new byte[8]);

        // The Absorb-after-Squeeze path is documented to throw — but it sets _absorbed = true
        // before the base guard runs, so a later Customize() observes a "post-absorb" state.
        try
        {
            sut.Absorb([0x01]);
        }
        catch (InvalidOperationException)
        {
            // Expected — Absorb after Squeeze is rejected.
        }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Customize([0x02]);
        });
    }

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

    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.Initialize" /> on a disposed
    /// <see cref="AsconCxof128" /> instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconCxof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Initialize();
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="AsconXof{T}.AlgorithmName" /> on a disposed
    /// <see cref="AsconCxof128" /> instance throws <see cref="ObjectDisposedException" /> rather
    /// than returning the cached identifier from cleared state.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconCxof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.AlgorithmName;
        });
    }
}
