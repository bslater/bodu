// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.UbiContract.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Direct-contract tests for the Skein UBI infrastructure exposed via the test-only
/// <c>GetInitialChainingValueWords</c> entry point. The existing
/// <c>SkeinTests.InitialChainingValues.cs</c> verifies that the IV matches the Skein 1.3
/// Appendix B constants for the unkeyed configurations; this file pins down the surrounding
/// contract — caching, defensive copies, key-driven invalidation, and disposed-state
/// behaviour. Together they give the UBI compression path direct, parallel-pipeline-independent
/// regression coverage.
/// </summary>
public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that two consecutive calls to <c>GetInitialChainingValueWords</c> return arrays of
    /// identical content — the IV is cached so repeated reads do not re-execute the CFG UBI phase
    /// and do not drift across calls.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_WhenCalledTwice_ShouldReturnIdenticalContent()
    {
        using var algorithm = new TAlgorithm();
        algorithm.Key = System.Array.Empty<byte>();

        ulong[] first = algorithm.GetInitialChainingValueWords();
        ulong[] second = algorithm.GetInitialChainingValueWords();

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that <c>GetInitialChainingValueWords</c> returns a defensive copy — mutating the
    /// returned array does not corrupt the cached state observed by a later read.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_ShouldReturnDefensiveCopy()
    {
        using var algorithm = new TAlgorithm();
        algorithm.Key = System.Array.Empty<byte>();

        ulong[] first = algorithm.GetInitialChainingValueWords();
        first[0] = 0xDEADBEEFDEADBEEFUL;

        ulong[] second = algorithm.GetInitialChainingValueWords();

        Assert.AreNotEqual(0xDEADBEEFDEADBEEFUL, second[0],
            "GetInitialChainingValueWords must return a defensive copy; caller mutation must not affect the cached state.");
    }

    /// <summary>
    /// Verifies that the chaining-value word count equals the Skein state size in 64-bit words —
    /// 4 for Skein-256, 8 for Skein-512, 16 for Skein-1024.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_ShouldReturnStateSizedWordArray()
    {
        using var algorithm = new TAlgorithm();
        algorithm.Key = System.Array.Empty<byte>();

        ulong[] words = algorithm.GetInitialChainingValueWords();

        // Block size is in bytes; state holds one ulong per 8 bytes of state.
        int expectedWordCount = algorithm.InputBlockSize / sizeof(ulong);
        Assert.AreEqual(expectedWordCount, words.Length);
    }

    /// <summary>
    /// Verifies that switching from the empty-key (plain hash) profile to a non-empty key produces
    /// a different initial chaining value — the KEY UBI phase folds the key into the state before
    /// the CFG phase, so the resulting IV must diverge from the unkeyed default.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_AfterAssigningNonEmptyKey_ShouldDifferFromUnkeyedDefault()
    {
        using var algorithm = new TAlgorithm();

        algorithm.Key = System.Array.Empty<byte>();
        ulong[] unkeyed = algorithm.GetInitialChainingValueWords();

        algorithm.Key = SkeinTestKey;
        ulong[] keyed = algorithm.GetInitialChainingValueWords();

        Assert.IsFalse(unkeyed.SequenceEqual(keyed),
            "Setting a non-empty key must invalidate the cached IV and produce a different KEY-UBI-then-CFG-UBI chaining value.");
    }

    /// <summary>
    /// Verifies that re-assigning the same key value yields the same chaining value words — the
    /// KEY UBI phase is deterministic and the cache invalidation on Key set re-runs the same
    /// derivation rather than introducing entropy.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_WhenKeyReassignedToSameValue_ShouldMatchPreviousReading()
    {
        using var algorithm = new TAlgorithm();

        algorithm.Key = SkeinTestKey;
        ulong[] first = algorithm.GetInitialChainingValueWords();

        algorithm.Key = (byte[])SkeinTestKey.Clone();
        ulong[] second = algorithm.GetInitialChainingValueWords();

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that <c>GetInitialChainingValueWords</c> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than reading the cleared chaining-value
    /// buffer.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new TAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.GetInitialChainingValueWords();
        });
    }
}
