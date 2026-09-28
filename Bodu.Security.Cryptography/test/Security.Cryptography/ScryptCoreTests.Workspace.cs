// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.Workspace.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that a rented workspace spans <c>N</c> units of <c>V</c> and one unit of scratch.
    /// </summary>
    [TestMethod]
    public void Rent_WhenGivenShape_ShouldSpanChainAndScratch()
    {
        using ScryptCore.Workspace workspace = ScryptCore.Workspace.Rent(16, 32 * 3, CreatePool());

        Assert.AreEqual(16 * 32 * 3, workspace.Chain.Length);
        Assert.AreEqual(32 * 3, workspace.Scratch.Length);
    }

    /// <summary>
    /// Verifies that the chain and the scratch of a workspace do not overlap: filling one leaves the other as it was.
    /// </summary>
    [TestMethod]
    public void Chain_WhenFilled_ShouldLeaveScratchUnchanged()
    {
        using ScryptCore.Workspace workspace = ScryptCore.Workspace.Rent(16, 32, CreatePool());
        workspace.Scratch.Fill(0x5A5A_5A5Au);

        workspace.Chain.Fill(0xA5A5_A5A5u);

        Assert.IsFalse(workspace.Scratch.ContainsAnyExcept(0x5A5A_5A5Au));
    }

    /// <summary>
    /// Verifies that reading the chain of a released workspace throws <see cref="ObjectDisposedException" /> rather
    /// than exposing memory the workspace no longer owns.
    /// </summary>
    [TestMethod]
    public void Chain_WhenWorkspaceIsReleased_ShouldThrowObjectDisposedException()
    {
        ScryptCore.Workspace workspace = ScryptCore.Workspace.Rent(16, 32, CreatePool());
        workspace.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = workspace.Chain;
        });
    }

    /// <summary>
    /// Verifies that reading the scratch of a released workspace throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Scratch_WhenWorkspaceIsReleased_ShouldThrowObjectDisposedException()
    {
        ScryptCore.Workspace workspace = ScryptCore.Workspace.Rent(16, 32, CreatePool());
        workspace.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = workspace.Scratch;
        });
    }

    /// <summary>
    /// Verifies that disposing a workspace twice returns its buffer to the pool once.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldReturnBufferOnce()
    {
        NativeBufferPool pool = CreatePool();
        ScryptCore.Workspace workspace = ScryptCore.Workspace.Rent(16, 32, pool);

        workspace.Dispose();
        workspace.Dispose();

        Assert.AreEqual(1, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that a released workspace leaves every word it spanned zero in the buffer the pool keeps.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenWorkspaceWasWritten_ShouldLeaveBufferAllZero()
    {
        NativeBufferPool pool = CreatePool();
        using (ScryptCore.Workspace written = ScryptCore.Workspace.Rent(16, 32, pool))
        {
            written.Chain.Fill(0xDEAD_BEEFu);
            written.Scratch.Fill(0xDEAD_BEEFu);
        }

        using ScryptCore.Workspace reused = ScryptCore.Workspace.Rent(16, 32, pool);

        Assert.IsFalse(reused.Chain.ContainsAnyExcept(0u));
        Assert.IsFalse(reused.Scratch.ContainsAnyExcept(0u));
    }
}
