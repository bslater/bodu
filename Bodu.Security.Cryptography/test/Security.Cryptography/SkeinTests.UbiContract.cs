// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.UbiContract.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

        var first = algorithm.GetInitialChainingValueWords();
        var second = algorithm.GetInitialChainingValueWords();

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

        var first = algorithm.GetInitialChainingValueWords();
        first[0] = 0xDEADBEEFDEADBEEFUL;

        var second = algorithm.GetInitialChainingValueWords();

        Assert.AreNotEqual(0xDEADBEEFDEADBEEFUL, second[0],
            "GetInitialChainingValueWords must return a defensive copy; caller mutation must not affect the cached state.");
    }

    /// <summary>
    /// Verifies that the chaining-value word count matches one of the Skein state sizes — 4 ulongs
    /// for Skein-256 (32-byte state), 8 for Skein-512 (64-byte state), or 16 for Skein-1024
    /// (128-byte state). The framework's <see cref="HashAlgorithm.InputBlockSize" /> is not
    /// overridden by Skein, so the size is asserted as membership in the Skein-permitted set
    /// rather than derived from a public property.
    /// </summary>
    [TestMethod]
    public void GetInitialChainingValueWords_ShouldReturnStateSizedWordArray()
    {
        using var algorithm = new TAlgorithm();
        algorithm.Key = System.Array.Empty<byte>();

        var words = algorithm.GetInitialChainingValueWords();

        int[] permitted = { 4, 8, 16 };
        Assert.IsTrue(System.Array.IndexOf(permitted, words.Length) >= 0,
            $"Skein chaining value must be 4, 8, or 16 ulongs (state size in 64-bit words); got {words.Length}.");
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
        var unkeyed = algorithm.GetInitialChainingValueWords();

        algorithm.Key = SkeinTestKey;
        var keyed = algorithm.GetInitialChainingValueWords();

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
        var first = algorithm.GetInitialChainingValueWords();

        algorithm.Key = (byte[])SkeinTestKey.Clone();
        var second = algorithm.GetInitialChainingValueWords();

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
