// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
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
/// check-length exposure, streaming-equivalence with the static <c>Compute</c> helper, idempotent
/// <c>GetCurrentCheckDigits</c> reads, <c>Reset</c> semantics, rejection of out-of-alphabet input, a data-driven
/// set of known-answer vectors supplied through <see cref="GetSpecification" />, and a separate set of
/// <c>IsValid</c> vectors supplied through <see cref="GetIsValidKnownAnswers" /> for algorithms whose canonical
/// sequence layout differs from the streaming body order.
/// </remarks>
public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
    where TTest : MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : MultiCharCheckDigitAlgorithm, new()
{

    /// <summary>
    /// Gets the display name used by <see cref="DynamicDataAttribute" /> for a test case row.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo" /> for the test under construction (unused).</param>
    /// <param name="data">The test case data row; the first element is expected to be the vector name.</param>
    /// <returns>The vector name for the current test case.</returns>
    public static string GetKnownAnswerTestName(MethodInfo methodInfo, object[] data)
    {
        _ = methodInfo;
        return (string)data[0];
    }

    /// <summary>
    /// Returns an ordered dataset of <c>IsValid</c> known-answer vectors for use with MSTest data-driven tests.
    /// </summary>
    /// <returns>
    /// An enumerable sequence of <c>object[]</c> arrays in the form <c>{ name, value, expectedIsValid }</c>.
    /// </returns>
    public static IEnumerable<object[]> IsValidKnownAnswerData()
    {
        foreach (MultiCharCheckDigitIsValidKnownAnswer vector in new TTest().GetIsValidKnownAnswers())
            yield return new object[] { vector.Name, vector.Value, vector.ExpectedIsValid };
    }

    /// <summary>
    /// Returns an ordered dataset of body / expected-check known-answer vectors for use with MSTest data-driven
    /// tests covering the streaming and <c>Compute</c> surface.
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
    /// Invokes the algorithm's static <c>Compute</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="body">The body characters.</param>
    /// <returns>The expected check code as a string of <c>CheckLength</c> decimal digits.</returns>
    protected abstract string ComputeStatic(ReadOnlySpan<char> body);

    /// <summary>
    /// Creates a new instance of <typeparamref name="TAlgorithm" /> in its initial state.
    /// </summary>
    /// <returns>A fresh algorithm instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Returns the set of explicit known-answer vectors used to exercise the algorithm's <c>IsValid</c> static
    /// helper.
    /// </summary>
    /// <returns>
    /// A non-null sequence of <see cref="MultiCharCheckDigitIsValidKnownAnswer" /> records. The default
    /// implementation synthesises a positive vector from each <see cref="GetSpecification" /> entry by
    /// concatenating <c>Body + ExpectedCheck</c>, then appends one negative vector per entry that flips the final
    /// check character to a different decimal digit. Algorithms whose canonical form rearranges those characters
    /// (for example IBAN) must override to supply the canonical text directly.
    /// </returns>
    protected virtual IEnumerable<MultiCharCheckDigitIsValidKnownAnswer> GetIsValidKnownAnswers()
    {
        MultiCharCheckDigitAlgorithmSpecification spec = GetSpecification();
        foreach (MultiCharCheckDigitKnownAnswer vector in spec.KnownAnswers)
        {
            yield return new MultiCharCheckDigitIsValidKnownAnswer
            {
                Name = vector.Name,
                Value = vector.Body + vector.ExpectedCheck,
                ExpectedIsValid = true,
            };

            var lastExpected = vector.ExpectedCheck[^1];
            var tampered = lastExpected == '0' ? '1' : '0';
            yield return new MultiCharCheckDigitIsValidKnownAnswer
            {
                Name = vector.Name + "_TamperedLastCheckDigit",
                Value = vector.Body + vector.ExpectedCheck[..^1] + tampered,
                ExpectedIsValid = false,
            };
        }
    }
    /// <summary>
    /// Returns the <see cref="MultiCharCheckDigitAlgorithmSpecification" /> describing the algorithm's expected
    /// properties and known-answer vectors used for streaming, <c>Compute</c>, and <c>Reset</c> assertions.
    /// </summary>
    /// <returns>A non-null specification; its <c>KnownAnswers</c> must be non-empty.</returns>
    protected abstract MultiCharCheckDigitAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Invokes the algorithm's static <c>IsValid</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="value">The full sequence in its canonical form.</param>
    /// <returns><see langword="true" /> if the sequence is valid under the algorithm; otherwise, <see langword="false" />.</returns>
    protected abstract bool IsValidStatic(ReadOnlySpan<char> value);

}
