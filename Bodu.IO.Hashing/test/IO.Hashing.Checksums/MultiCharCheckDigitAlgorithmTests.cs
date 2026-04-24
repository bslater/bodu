// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Provides a reusable base class for verifying correctness and consistency of
/// <see cref="MultiCharCheckDigitAlgorithm" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The multi-character check-digit algorithm under test.</typeparam>
/// <remarks>
/// The harness drives assertions that every such algorithm must satisfy — algorithm name / alphabet /
/// check-length exposure, streaming-equivalence with the static <c>Compute</c> helper at every prefix, idempotent
/// <c>GetCurrentCheckDigits</c> reads, <c>Reset</c> semantics, round-trip between <c>Compute</c> and
/// <c>IsValid</c>, rejection of out-of-alphabet input, and a data-driven set of known-answer vectors supplied
/// through <see cref="GetSpecification" />.
/// </remarks>
public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
    where TTest : MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : MultiCharCheckDigitAlgorithm, new()
{
    /// <summary>
    /// Returns the <see cref="MultiCharCheckDigitAlgorithmSpecification" /> describing the algorithm's expected
    /// properties and known-answer vectors.
    /// </summary>
    /// <returns>A non-null specification; its <c>KnownAnswers</c> must be non-empty.</returns>
    protected abstract MultiCharCheckDigitAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Creates a new instance of <typeparamref name="TAlgorithm" /> in its initial state.
    /// </summary>
    /// <returns>A fresh algorithm instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Invokes the algorithm's static <c>Compute</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="body">The body characters.</param>
    /// <returns>The expected check code as a string of <c>CheckLength</c> decimal digits.</returns>
    protected abstract string ComputeStatic(ReadOnlySpan<char> body);

    /// <summary>
    /// Invokes the algorithm's static <c>IsValid</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="valueIncludingCheck">The full sequence including the trailing check code.</param>
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
        MultiCharCheckDigitAlgorithmSpecification spec = new TTest().GetSpecification();
        foreach (MultiCharCheckDigitKnownAnswer vector in spec.KnownAnswers)
            yield return new object[] { vector.Name, vector.Body, vector.ExpectedCheck };
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm exposes the algorithm name, input alphabet, and check
    /// length declared in the specification.
    /// </summary>
    [TestMethod]
    public void Properties_WhenQueried_ShouldMatchSpecification()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();

        Assert.AreEqual(spec.AlgorithmName, algorithm.AlgorithmName);
        Assert.AreEqual(spec.InputAlphabet, algorithm.InputAlphabet);
        Assert.AreEqual(spec.CheckLength, algorithm.CheckLength);
    }

    /// <summary>
    /// Verifies that a freshly constructed algorithm reports the empty-body check code declared in the
    /// specification, when one is declared.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigits_WhenJustConstructed_ShouldReturnEmptyCheckDigits()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigits is not string expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that appending a body in a single call produces the check code recorded for that known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Append_WhenKnownAnswerIsAppendedInFull_ShouldProduceExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that appending a body one character at a time produces the same check code as a single
    /// span-based call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Append_WhenKnownAnswerIsAppendedOneCharAtATime_ShouldProduceExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        foreach (char ch in body)
            algorithm.Append(ch);

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that appending a body in two chunks produces the same check code as appending it in one call.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Append_WhenKnownAnswerIsSplitAcrossTwoChunks_ShouldProduceExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        if (body.Length < 2) return;

        TAlgorithm algorithm = CreateAlgorithm();
        int split = body.Length / 2;

        algorithm.Append(body.AsSpan(0, split));
        algorithm.Append(body.AsSpan(split));

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that reading the current check code twice in succession — with no intervening appends — yields
    /// the same value, proving the getter is non-destructive.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigits_WhenReadRepeatedly_ShouldReturnSameValue()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("12345".AsSpan());

        string first = algorithm.GetCurrentCheckDigits();
        string second = algorithm.GetCurrentCheckDigits();
        string third = algorithm.GetCurrentCheckDigits();

        Assert.AreEqual(first, second);
        Assert.AreEqual(second, third);
    }

    /// <summary>
    /// Verifies that the span and string overloads of <c>GetCurrentCheckDigits</c> agree.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (unused).</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check code (unused in this cross-check).</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void GetCurrentCheckDigits_SpanAndStringOverloads_ShouldAgree(string name, string body, string expectedCheck)
    {
        _ = name;
        _ = expectedCheck;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append(body.AsSpan());

        Span<char> buffer = stackalloc char[algorithm.CheckLength];
        int written = algorithm.GetCurrentCheckDigits(buffer);

        Assert.AreEqual(algorithm.CheckLength, written);
        Assert.AreEqual(algorithm.GetCurrentCheckDigits(), new string(buffer));
    }

    /// <summary>
    /// Verifies that the static <c>Compute</c> helper returns the expected check code for every known-answer
    /// vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check code.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Compute_WhenKnownAnswer_ShouldReturnExpectedCheckDigits(string name, string body, string expectedCheck)
    {
        _ = name;
        Assert.AreEqual(expectedCheck, ComputeStatic(body.AsSpan()));
    }

    /// <summary>
    /// Verifies that appending the algorithm's own computed check code to the body always yields a sequence
    /// that <c>IsValid</c> accepts.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check code.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void IsValid_WhenSequenceIncludesComputedCheckDigits_ShouldReturnTrue(string name, string body, string expectedCheck)
    {
        _ = name;
        string full = body + expectedCheck;
        Assert.IsTrue(IsValidStatic(full.AsSpan()), $"Expected '{full}' to be valid.");
    }

    /// <summary>
    /// Verifies that substituting one character of the trailing check code for a different digit makes
    /// <c>IsValid</c> reject the sequence.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The correct check code.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void IsValid_WhenLastCheckDigitIsTampered_ShouldReturnFalse(string name, string body, string expectedCheck)
    {
        _ = name;
        char lastExpected = expectedCheck[^1];
        for (char c = '0'; c <= '9'; c++)
        {
            if (c == lastExpected) continue;
            string bad = body + expectedCheck[..^1] + c;
            Assert.IsFalse(IsValidStatic(bad.AsSpan()), $"Expected '{bad}' to be invalid (swap of last check digit).");
        }
    }

    /// <summary>
    /// Verifies that <c>Reset</c> after an Append restores the algorithm to the empty-body check code declared
    /// in the specification (when one is declared).
    /// </summary>
    [TestMethod]
    public void Reset_AfterAppend_ShouldRestoreEmptyCheckDigits()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        if (spec.EmptyCheckDigits is not string expected) return;

        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Append("1234".AsSpan());
        algorithm.Reset();

        Assert.AreEqual(expected, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that Reset between two append runs discards the first run's accumulated state so that only the
    /// second run contributes to the final check code.
    /// </summary>
    /// <param name="name">A descriptive name for the vector (used in test output).</param>
    /// <param name="body">The body characters to append in the second run.</param>
    /// <param name="expectedCheck">The check code the algorithm is expected to emit for the second run.</param>
    [TestMethod]
    [DynamicData(nameof(KnownAnswerData), DynamicDataSourceType.Method)]
    public void Reset_BetweenAppends_ShouldDiscardPriorState(string name, string body, string expectedCheck)
    {
        _ = name;
        TAlgorithm algorithm = CreateAlgorithm();

        algorithm.Append("123456".AsSpan());
        algorithm.Reset();
        algorithm.Append(body.AsSpan());

        Assert.AreEqual(expectedCheck, algorithm.GetCurrentCheckDigits());
    }

    /// <summary>
    /// Verifies that <c>Append</c> rejects characters that fall outside the declared input alphabet.
    /// </summary>
    [TestMethod]
    public void Append_WhenCharacterIsOutsideInputAlphabet_ShouldThrowArgumentOutOfRangeException()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        char invalid = spec.InputAlphabet == CheckDigitInputAlphabet.DecimalDigits ? 'A' : '!';

        TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append(new[] { '1', invalid, '2' }.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <c>GetCurrentCheckDigits</c> throws <see cref="ArgumentException" /> when the destination
    /// span is shorter than <c>CheckLength</c>.
    /// </summary>
    [TestMethod]
    public void GetCurrentCheckDigits_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        char[] tooSmall = new char[algorithm.CheckLength - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            algorithm.GetCurrentCheckDigits(tooSmall.AsSpan());
        });
    }
}
