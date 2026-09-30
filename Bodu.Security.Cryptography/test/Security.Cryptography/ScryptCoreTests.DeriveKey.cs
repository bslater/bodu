// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.DeriveKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that a derivation returns its workspace - <c>N + 1</c> units of 128·r bytes - to the pool it came
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
    /// Verifies that a derivation with a pool that retains nothing - as with the reuse switch set - produces RFC 7914's
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

    /// <summary>
    /// Verifies that dividing a derivation's units among threads - at every bound from two to one per processor -
    /// reproduces each OpenSSL corpus row with more than one unit.
    /// </summary>
    /// <param name="vector">The corpus row under test.</param>
    [TestMethod]
    [DynamicData(nameof(OpenSslCorpusWithSeveralUnits), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveKey_WhenUnitsRunOnThreads_ShouldMatchOpenSslCorpusRow(KdfKnownAnswer vector)
    {
        foreach (int bound in new[] { 2, 3, 4, -1 })
        {
            byte[] key = new byte[vector.OutputLength];
            var options = new ScryptCore.MixOptions(bound, minimumParallelUnitBytes: 0, pool: CreatePool());

            ScryptCore.DeriveKey(vector.Password, vector.Salt, vector.CostN, vector.BlockSizeR, vector.Parallelism, key, options);

            Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(key).ToLowerInvariant(), $"bound {bound}");
        }
    }

    /// <summary>
    /// Verifies that a derivation divided among threads by default - sixteen units of 1 MiB, the threshold - produces
    /// RFC 7914's key.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenUnitsReachTheParallelThreshold_ShouldMatchRfc7914Key()
    {
        string key = DeriveSecondRfc7914Key(new ScryptCore.MixOptions(4, pool: CreatePool()));

        Assert.AreEqual(Rfc7914SecondKeyHex, key);
    }

    /// <summary>
    /// Verifies that a derivation divided among threads returns each thread's workspace to the pool - never more than
    /// the bound - and leaves every one of them all zero.
    /// </summary>
    [TestMethod]
    public void DeriveKey_WhenUnitsRunOnThreads_ShouldReturnEveryWorkspaceAllZero()
    {
        NativeBufferPool pool = CreatePool(maxRetainedBuffers: 16);

        _ = DeriveSecondRfc7914Key(new ScryptCore.MixOptions(4, pool: pool));

        int retained = pool.RetainedCount;
        Assert.IsTrue(retained is >= 1 and <= 4, $"retained {retained}");

        var reused = new List<ScryptCore.Workspace>();
        try
        {
            for (int i = 0; i < retained; i++)
                reused.Add(ScryptCore.Workspace.Rent(1024, 32 * 8, pool));

            foreach (ScryptCore.Workspace workspace in reused)
            {
                Assert.IsFalse(workspace.Chain.ContainsAnyExcept(0u));
                Assert.IsFalse(workspace.Scratch.ContainsAnyExcept(0u));
            }
        }
        finally
        {
            foreach (ScryptCore.Workspace workspace in reused)
                workspace.Dispose();
        }
    }

    /// <summary>
    /// Verifies that a derivation through each kernel reproduces each light OpenSSL corpus row.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void DeriveKey_WhenGivenLightCorpusRows_ForEachKernel_ShouldMatchOpenSslCorpus(string kernel)
    {
        var options = new ScryptCore.MixOptions(1, kernel: ParseSupportedKernel(kernel), pool: CreatePool());

        foreach (object[] row in ScryptTests.OpenSslCorpusLight())
        {
            var vector = (KdfKnownAnswer)row[0];
            byte[] key = new byte[vector.OutputLength];

            ScryptCore.DeriveKey(vector.Password, vector.Salt, vector.CostN, vector.BlockSizeR, vector.Parallelism, key, options);

            Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(key).ToLowerInvariant(), vector.Name);
        }
    }
}
