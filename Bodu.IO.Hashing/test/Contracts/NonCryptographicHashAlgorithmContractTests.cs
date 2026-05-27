// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a <see cref="NonCryptographicHashAlgorithm" /> implementation.
/// Concrete subclasses override <see cref="Create" />, <see cref="KnownAnswers" />, and
/// <see cref="StreamingCases" /> to plug their algorithm into the inherited contract tests.
/// </summary>
/// <typeparam name="TAlgorithm">The algorithm type under test.</typeparam>
/// <remarks>
/// <para>
/// The base exercises the four core invariants of every non-cryptographic hash algorithm:
/// </para>
/// <list type="bullet">
///   <item><description>Empty-input digest is stable and equal to the freshly-allocated instance's current hash.</description></item>
///   <item><description><c>GetHashAndReset</c> reproduces every known-answer vector when applied to a one-shot <c>Append</c>.</description></item>
///   <item><description>Segmented input via successive <c>Append</c> calls produces the same digest as a one-shot append.</description></item>
///   <item><description><c>Reset</c> restores the algorithm to the empty-input state.</description></item>
/// </list>
/// <para>
/// Each <c>[TestMethod]</c> loops over the supplied <see cref="KnownAnswers" /> or
/// <see cref="StreamingCases" /> collection and tags failures with the row's name, so the runner reports
/// which row failed without needing <c>[DynamicData]</c> (which cannot bind to instance state).
/// </para>
/// </remarks>
public abstract class NonCryptographicHashAlgorithmContractTests<TAlgorithm>
    where TAlgorithm : NonCryptographicHashAlgorithm
{
    /// <summary>
    /// Creates a fresh algorithm instance for use in a single test invocation.
    /// </summary>
    /// <returns>A new algorithm instance.</returns>
    protected abstract TAlgorithm Create();

    /// <summary>
    /// Returns the one-shot known-answer vectors that the algorithm must reproduce.
    /// </summary>
    /// <returns>An ordered list of one-shot vectors.</returns>
    protected abstract IReadOnlyList<HashKat> KnownAnswers { get; }

    /// <summary>
    /// Returns the streaming-parity vectors that the algorithm must reproduce when the same input is
    /// supplied through multiple <c>Append</c> calls in the specified segment sizes.
    /// </summary>
    /// <returns>An ordered list of streaming vectors.</returns>
    protected abstract IReadOnlyList<HashStreamingKat> StreamingCases { get; }

    /// <summary>
    /// Returns the hex-encoded digest the algorithm produces for an empty input. Concrete subclasses
    /// override when they want to assert against the canonical empty-input value documented by the
    /// algorithm specification.
    /// </summary>
    /// <returns>The hex-encoded empty-input digest, or <see langword="null" /> to skip the empty-input check.</returns>
    protected virtual string? EmptyInputExpectedHex => null;

    /// <summary>
    /// Verifies that every <see cref="HashKat" /> row in <see cref="KnownAnswers" /> reproduces its
    /// expected hex digest when fed to a fresh algorithm instance via a single <c>Append</c> call and
    /// finalised with <c>GetHashAndReset</c>.
    /// </summary>
    [TestMethod]
    public void GetHashAndReset_WhenKnownAnswer_ShouldMatchExpectedHex()
    {
        foreach (HashKat kat in KnownAnswers)
        {
            TAlgorithm algorithm = Create();
            algorithm.Append(kat.Input);
            string actual = Convert.ToHexString(algorithm.GetHashAndReset());

            Assert.AreEqual(
                kat.ExpectedHex,
                actual,
                ignoreCase: true,
                $"KAT '{kat.Name}' produced unexpected digest.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="HashStreamingKat" /> row produces the same digest whether the input
    /// is appended in one call or as a series of segmented appends.
    /// </summary>
    [TestMethod]
    public void Append_WhenInputSegmented_ShouldMatchOneShotHash()
    {
        foreach (HashStreamingKat kat in StreamingCases)
        {
            TAlgorithm streamed = Create();
            int offset = 0;
            foreach (int segmentSize in kat.SegmentSizes)
            {
                streamed.Append(kat.Input.AsSpan(offset, segmentSize));
                offset += segmentSize;
            }

            string actual = Convert.ToHexString(streamed.GetHashAndReset());

            Assert.AreEqual(
                kat.ExpectedHex,
                actual,
                ignoreCase: true,
                $"Streaming KAT '{kat.Name}' produced unexpected digest.");
        }
    }

    /// <summary>
    /// Verifies that a fresh algorithm instance's <c>GetCurrentHash</c> equals the documented empty-input
    /// digest when <see cref="EmptyInputExpectedHex" /> is provided.
    /// </summary>
    [TestMethod]
    public void GetCurrentHash_WhenNoInput_ShouldMatchEmptyHash()
    {
        if (EmptyInputExpectedHex is null)
            return;

        TAlgorithm algorithm = Create();
        string actual = Convert.ToHexString(algorithm.GetCurrentHash());

        Assert.AreEqual(EmptyInputExpectedHex, actual, ignoreCase: true);
    }

    /// <summary>
    /// Verifies that <c>Reset</c> restores the algorithm to its empty-input state, so an immediately-
    /// reissued <c>GetHashAndReset</c> matches the empty-input digest.
    /// </summary>
    [TestMethod]
    public void Reset_WhenCalledAfterAppend_ShouldRestoreEmptyState()
    {
        TAlgorithm algorithm = Create();
        TAlgorithm reference = Create();

        if (KnownAnswers.Count == 0)
            return;

        algorithm.Append(KnownAnswers[0].Input);
        algorithm.Reset();

        string after = Convert.ToHexString(algorithm.GetCurrentHash());
        string fresh = Convert.ToHexString(reference.GetCurrentHash());

        Assert.AreEqual(fresh, after);
    }
}
