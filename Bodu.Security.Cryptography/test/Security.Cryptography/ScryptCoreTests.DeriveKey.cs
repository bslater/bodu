// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.DeriveKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that a derivation returns its workspace — <c>N + 1</c> units of 128·r bytes — to the pool it came
    /// from.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenDerivationEnds_ShouldReturnWorkspaceToPool()
    {
        NativeBufferPool pool = CreatePool();

        _ = DeriveSecondRfc7914Key(pool);

        Assert.AreEqual(1, pool.RetainedCount);
        Assert.AreEqual((1024 + 1) * 128 * 8, pool.RetainedBytes);
    }

    /// <summary>
    /// Verifies that the workspace a derivation returns to the pool is all zero, so no password-derived word outlives
    /// the derivation.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenDerivationEnds_ShouldLeaveReturnedWorkspaceAllZero()
    {
        NativeBufferPool pool = CreatePool();
        _ = DeriveSecondRfc7914Key(pool);

        using ScryptCore.Workspace reused = ScryptCore.Workspace.Rent(1024, 32 * 8, pool);

        Assert.IsFalse(reused.Chain.ContainsAnyExcept(0u));
        Assert.IsFalse(reused.Scratch.ContainsAnyExcept(0u));
    }

    /// <summary>
    /// Verifies that derivations produce RFC 7914's key both on a fresh buffer and on the workspace the first one
    /// returned to the pool.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenWorkspaceIsReused_ShouldMatchRfc7914Key()
    {
        NativeBufferPool pool = CreatePool();

        string first = DeriveSecondRfc7914Key(pool);
        string second = DeriveSecondRfc7914Key(pool);

        Assert.AreEqual(Rfc7914SecondKeyHex, first);
        Assert.AreEqual(Rfc7914SecondKeyHex, second);
    }

    /// <summary>
    /// Verifies that a derivation with a pool that retains nothing — as with the reuse switch set — produces RFC 7914's
    /// key and leaves nothing retained.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenPoolRetainsNothing_ShouldMatchRfc7914KeyAndRetainNothing()
    {
        NativeBufferPool pool = CreatePool(maxRetainedBuffers: 0);

        string key = DeriveSecondRfc7914Key(pool);

        Assert.AreEqual(Rfc7914SecondKeyHex, key);
        Assert.AreEqual(0, pool.RetainedCount);
    }
}
