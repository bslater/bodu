// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Provides a reusable base class for verifying correctness and consistency of
/// <see cref="AlphanumericCheckDigitAlgorithm" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The alphanumeric check-digit algorithm under test.</typeparam>
/// <remarks>
/// The harness drives assertions that every such algorithm must satisfy — algorithm name / alphabet exposure,
/// streaming-equivalence with the static <c>Compute</c> helper at every prefix, idempotent
/// <c>GetCurrentCheckDigit</c> reads, <c>Reset</c> semantics, round-trip between <c>Compute</c> and
/// <c>IsValid</c>, rejection of out-of-alphabet input, and a data-driven set of known-answer vectors supplied
/// through <see cref="GetSpecification" />.
/// </remarks>
public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
    where TTest : AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : AlphanumericCheckDigitAlgorithm, new()
{
    /// <summary>
    /// Returns the <see cref="AlphanumericCheckDigitAlgorithmSpecification" /> describing the algorithm's expected
    /// properties and known-answer vectors.
    /// </summary>
    /// <returns>A non-null specification; its <c>KnownAnswers</c> must be non-empty.</returns>
    protected abstract AlphanumericCheckDigitAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Creates a new instance of <typeparamref name="TAlgorithm" /> in its initial state.
    /// </summary>
    /// <returns>A fresh algorithm instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Invokes the algorithm's static <c>Compute</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="body">The body characters.</param>
    /// <returns>The check character.</returns>
    protected abstract char ComputeStatic(ReadOnlySpan<char> body);

    /// <summary>
    /// Invokes the algorithm's static <c>IsValid</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="valueIncludingCheck">The full sequence including the trailing check character.</param>
    /// <returns><see langword="true" /> if the sequence is valid under the algorithm; otherwise, <see langword="false" />.</returns>
    protected abstract bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck);

    /// <summary>
    /// Returns an ordered dataset of known-answer vectors for use with MSTest data-driven tests.
    /// </summary>
    /// <returns>
    /// An enumerable sequence of <c>object[]</c> arrays in the form <c>{ name, body, expectedCheck }</c>.
    /// </returns>
    public static IEnumerable<object[]> KnownAnswerData()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = new TTest().GetSpecification();
        foreach (CheckDigitKnownAnswer vector in spec.KnownAnswers)
            yield return new object[] { vector.Name, vector.Body, vector.ExpectedCheck };
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the algorithm name, input alphabet, and output
    /// alphabet declared in the specification.
    /// </summary>
    [TestMethod]
    public void Properties_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.AlgorithmName, algorithm.AlgorithmName);
        Assert.AreEqual(spec.InputAlphabet, algorithm.InputAlphabet);
        Assert.AreEqual(spec.OutputAlphabet, algorithm.OutputAlphabet);
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm reports the empty-body check character declared in the
    /// specification, when one is declared.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigit_WhenJustConstructed_ShouldReturnEmptyCheckDigit()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigit is not char expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body in a single call produces the check character recorded for that
    /// known-answer vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Append_WhenKnownAnswerIsAppendedInFull_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body one character at a time produces the same check character as a single
    /// span-based call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Append_WhenKnownAnswerIsAppendedOneCharAtATime_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        foreach (char ch in body)
            algorithm.Append(ch);

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending a body in two chunks produces the same check character as appending it in one
    /// call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Append_WhenKnownAnswerIsSplitAcrossTwoChunks_ShouldProduceExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        if (body.Length < 2) return;

        TAlgorithm algorithm = CreateAlgorithm();
        int split = body.Length / 2;

        algorithm.Append(body.AsSpan(0, split));
        algorithm.Append(body.AsSpan(split));

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that appending an empty span leaves the running check character unchanged.
    /// </summary>
    [TestMethod]
    public void Append_WhenSpanIsEmpty_ShouldLeaveCheckDigitUnchanged()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("123".AsSpan());
        char initial = algorithm.GetCurrentCheckDigit();

        algorithm.Append(ReadOnlySpan<char>.Empty);

        Assert.AreEqual(initial, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that streaming the body one character at a time and reading <c>GetCurrentCheckDigit</c> matches
    /// the static <c>Compute</c> result at every prefix length.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The full body characters.</param>
    /// <param name="expectedCheck">The expected check character (unused).</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void GetCurrentCheckDigit_AtEveryPrefixLength_ShouldAgreeWithStaticCompute(string name, string body, char expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        for (int i = 0; i < body.Length; i++)
        {
            algorithm.Append(body[i]);
            char streaming = algorithm.GetCurrentCheckDigit();
            char fromCompute = ComputeStatic(body.AsSpan(0, i + 1));
            Assert.AreEqual(fromCompute, streaming, $"Prefix length {i + 1} (\"{body[..(i + 1)]}\").");
        }
    }

    /// <summary>
    /// Verifies that reading the current check character twice in succession — with no intervening appends —
    /// yields the same value, proving the getter is non-destructive.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigit_WhenReadRepeatedly_ShouldReturnSameValue()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("123".AsSpan());

        char first = algorithm.GetCurrentCheckDigit();
        char second = algorithm.GetCurrentCheckDigit();
        char third = algorithm.GetCurrentCheckDigit();

        Assert.AreEqual(first, second);
        Assert.AreEqual(second, third);
    }

    /// <summary>
    /// Verifies that the static <c>Compute</c> helper returns the expected check character for every
    /// known-answer vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check character.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Compute_WhenKnownAnswer_ShouldReturnExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        Assert.AreEqual(expectedCheck, ComputeStatic(body.AsSpan()));
    }

    /// <summary>
    /// Verifies that appending the algorithm's own computed check character to the body always yields a sequence
    /// that <c>IsValid</c> accepts.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check character.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void IsValid_WhenSequenceIncludesComputedCheckDigit_ShouldReturnTrue(string name, string body, char expectedCheck)
    {
        _ = name;
        string full = body + expectedCheck;
        Assert.IsTrue(IsValidStatic(full.AsSpan()), $"Expected '{full}' to be valid.");
    }

    /// <summary>
    /// Verifies that substituting the trailing check character for a different digit (or the sentinel
    /// <c>'X'</c> when applicable) makes <c>IsValid</c> reject the sequence.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The correct check character.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void IsValid_WhenCheckDigitIsTampered_ShouldReturnFalse(string name, string body, char expectedCheck)
    {
        _ = name;
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();

        for (char c = '0'; c <= '9'; c++)
        {
            if (c == expectedCheck) continue;
            string bad = body + c;
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of check digit).");
        }

        if (spec.OutputAlphabet == CheckDigitOutputAlphabet.DecimalDigitsOrX && expectedCheck != 'X')
        {
            string bad = body + 'X';
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of check digit to X).");
        }
    }

    /// <summary>
    /// Verifies that <c>Reset</c> after an Append restores the algorithm to the empty-body check character
    /// declared in the specification (when one is declared).
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldRestoreEmptyCheckDigit()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigit is not char expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("123".AsSpan());
        algorithm.Reset();

        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that Reset between two append runs discards the first run's accumulated state so that only the
    /// second run contributes to the final check character.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append in the second run.</param>
    /// <param name="expectedCheck">The check character the algorithm is expected to emit for the second run.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Reset_BetweenAppends_ShouldDiscardPriorState(string name, string body, char expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append("987".AsSpan());
        algorithm.Reset();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigit());
    }

    /// <summary>
    /// Verifies that <c>Append</c> rejects characters that fall outside the declared input alphabet.
    /// </summary>
    [TestMethod]
    public void Append_WhenCharacterIsOutsideInputAlphabet_ShouldThrowArgumentOutOfRangeException()
    {
        AlphanumericCheckDigitAlgorithmSpecification spec = GetSpecification();
        char invalid = spec.InputAlphabet == CheckDigitInputAlphabet.DecimalDigits ? 'A' : '!';

        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append(new[] { '1', invalid, '2' }.AsSpan());
        });
    }
}
