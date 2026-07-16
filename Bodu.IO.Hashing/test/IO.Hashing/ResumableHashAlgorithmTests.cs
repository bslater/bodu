// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResumableHashAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing;

/// <summary>
/// Validates the <see cref="IResumableHashAlgorithm" /> contract across every implementing family (FNV, Adler,
/// Fletcher, CRC): resuming from a previously emitted digest must produce the same result as hashing the concatenated
/// input in one session, the instance's own incremental state must survive the call, and the argument guards must hold.
/// </summary>
[TestClass]
public sealed class ResumableHashAlgorithmTests
{
    /// <summary>
    /// Supplies one instance of every <see cref="IResumableHashAlgorithm" /> implementer, labelled for display.
    /// </summary>
    private static IEnumerable<object[]> Implementations =>
    [
        ["FNV-1-32", (Func<IResumableHashAlgorithm>)(() => new Fnv132())],
        ["FNV-1a-32", (Func<IResumableHashAlgorithm>)(() => new Fnv1a32())],
        ["FNV-1-64", (Func<IResumableHashAlgorithm>)(() => new Fnv164())],
        ["FNV-1a-64", (Func<IResumableHashAlgorithm>)(() => new Fnv1a64())],
        ["Adler-32", (Func<IResumableHashAlgorithm>)(() => new Adler32())],
        ["Adler-32C", (Func<IResumableHashAlgorithm>)(() => new Adler32C())],
        ["Adler-64", (Func<IResumableHashAlgorithm>)(() => new Adler64())],
        ["Fletcher-16", (Func<IResumableHashAlgorithm>)(() => new Fletcher16())],
        ["Fletcher-32", (Func<IResumableHashAlgorithm>)(() => new Fletcher32())],
        ["Fletcher-64", (Func<IResumableHashAlgorithm>)(() => new Fletcher64())],
        ["CRC-32/ISO-HDLC", (Func<IResumableHashAlgorithm>)(() => new Crc(CrcStandard.CRC32_ISOHDLC))],
    ];

    /// <summary>
    /// Verifies that resuming from the digest of a prefix and folding in the suffix produces the same digest as hashing
    /// the concatenated message in a single session, for every implementing algorithm family.
    /// </summary>
    /// <param name="name">The display label for the algorithm under test.</param>
    /// <param name="factory">Creates the algorithm instance.</param>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public void ComputeHashFrom_WhenResumingFromPrefixDigest_ShouldMatchSingleSessionDigest(
        string name, Func<IResumableHashAlgorithm> factory)
    {
        byte[] prefix = "The quick brown fox "u8.ToArray();
        byte[] suffix = "jumps over the lazy dog"u8.ToArray();

        var oneShot = (System.IO.Hashing.NonCryptographicHashAlgorithm)factory();
        oneShot.Append(prefix);
        oneShot.Append(suffix);
        byte[] expected = oneShot.GetCurrentHash();

        var prefixHasher = (System.IO.Hashing.NonCryptographicHashAlgorithm)factory();
        prefixHasher.Append(prefix);
        byte[] prefixDigest = prefixHasher.GetCurrentHash();

        IResumableHashAlgorithm resumable = factory();
        byte[] resumed = resumable.ComputeHashFrom(prefixDigest, suffix);

        CollectionAssert.AreEqual(expected, resumed, $"[{name}] Resumed digest diverged from the single-session digest.");
    }

    /// <summary>
    /// Verifies that <see cref="IResumableHashAlgorithm.TryComputeHashFrom" /> leaves the instance's own incremental
    /// state untouched, so an in-progress computation continues correctly after a resume call on the same instance.
    /// </summary>
    /// <param name="name">The display label for the algorithm under test.</param>
    /// <param name="factory">Creates the algorithm instance.</param>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public void TryComputeHashFrom_WhenInstanceHasPendingState_ShouldPreserveThatState(
        string name, Func<IResumableHashAlgorithm> factory)
    {
        byte[] partA = "in-progress "u8.ToArray();
        byte[] partB = "message"u8.ToArray();
        byte[] unrelated = "unrelated resume input"u8.ToArray();

        var reference = (System.IO.Hashing.NonCryptographicHashAlgorithm)factory();
        reference.Append(partA);
        reference.Append(partB);
        byte[] expected = reference.GetCurrentHash();

        IResumableHashAlgorithm resumable = factory();
        var instance = (System.IO.Hashing.NonCryptographicHashAlgorithm)resumable;
        instance.Append(partA);

        // Interleave a resume from an unrelated digest; the pending Append state must survive.
        byte[] unrelatedDigest = new byte[resumable.HashLengthInBytes];
        Span<byte> scratch = stackalloc byte[resumable.HashLengthInBytes];
        Assert.IsTrue(resumable.TryComputeHashFrom(unrelatedDigest, unrelated, scratch, out int written));
        Assert.AreEqual(resumable.HashLengthInBytes, written);

        instance.Append(partB);
        CollectionAssert.AreEqual(expected, instance.GetCurrentHash(),
            $"[{name}] The interleaved resume call disturbed the instance's incremental state.");
    }

    /// <summary>
    /// Verifies that <see cref="IResumableHashAlgorithm.TryComputeHashFrom" /> throws
    /// <see cref="ArgumentException" /> when the previous digest length does not match the algorithm's digest length.
    /// </summary>
    /// <param name="name">The display label for the algorithm under test.</param>
    /// <param name="factory">Creates the algorithm instance.</param>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public void TryComputeHashFrom_WhenPreviousHashLengthMismatched_ShouldThrowArgumentException(
        string name, Func<IResumableHashAlgorithm> factory)
    {
        IResumableHashAlgorithm resumable = factory();
        byte[] wrongLength = new byte[resumable.HashLengthInBytes + 1];
        byte[] destination = new byte[resumable.HashLengthInBytes];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = resumable.TryComputeHashFrom(wrongLength, "data"u8, destination, out _);
        });

        Assert.AreEqual("previousHash", ex.ParamName, $"[{name}] Unexpected ParamName.");
    }

    /// <summary>
    /// Verifies that <see cref="IResumableHashAlgorithm.TryComputeHashFrom" /> returns <see langword="false" /> with
    /// zero bytes written when the destination span is too small.
    /// </summary>
    /// <param name="name">The display label for the algorithm under test.</param>
    /// <param name="factory">Creates the algorithm instance.</param>
    [TestMethod]
    [DynamicData(nameof(Implementations))]
    public void TryComputeHashFrom_WhenDestinationTooSmall_ShouldReturnFalse(
        string name, Func<IResumableHashAlgorithm> factory)
    {
        IResumableHashAlgorithm resumable = factory();
        byte[] previous = new byte[resumable.HashLengthInBytes];
        byte[] destination = new byte[resumable.HashLengthInBytes - 1];

        bool result = resumable.TryComputeHashFrom(previous, "data"u8, destination, out int written);

        Assert.IsFalse(result, $"[{name}] Expected false for an undersized destination.");
        Assert.AreEqual(0, written, $"[{name}] Expected zero bytes written for an undersized destination.");
    }
}
