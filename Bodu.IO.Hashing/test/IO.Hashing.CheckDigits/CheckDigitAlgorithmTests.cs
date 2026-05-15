// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Provides a reusable base class for verifying correctness and consistency of
/// <see cref="CheckDigitAlgorithm" /> implementations.
/// </summary>
/// <typeparam name="TTest">The concrete test type inheriting this class.</typeparam>
/// <typeparam name="TAlgorithm">The check-digit algorithm under test.</typeparam>
/// <remarks>
/// The harness drives assertions that every check-digit algorithm must satisfy — streaming-equivalence with the
/// static <c>Compute</c> helper, idempotent <c>GetCurrentCheckDigit</c> reads, <c>Reset</c> semantics,
/// round-trip between <c>Compute</c> and <c>IsValid</c>, digit-only input validation, and a data-driven set of
/// known-answer vectors supplied through
/// <see cref="GetSpecification" />.
/// </remarks>
public abstract partial class CheckDigitAlgorithmTests<TTest, TAlgorithm>
    where TTest : CheckDigitAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : CheckDigitAlgorithm, new()
{

    /// <summary>
    /// Gets the display name used by <see cref="DynamicDataAttribute" /> for a test case row.
    /// </summary>
    /// <param name="data">
    /// The test case data row. The first element is expected to contain the human-readable standard name.
    /// </param>
    /// <returns>
    /// The standard name for the current test case as a <see cref="string" />.
    /// </returns>
    public static string GetKnownAnswerTestName(MethodInfo methodInfo, object[] data)
    {
        var name = (string)data[0];
        return $"{name}";
    }

    /// <summary>
    /// Returns an ordered dataset of known-answer vectors for use with MSTest data-driven tests.
    /// </summary>
    /// <returns>
    /// An enumerable sequence of <c>object[]</c> arrays in the form <c>{ name, body, expectedCheck }</c>.
    /// </returns>
    public static IEnumerable<object[]> KnownAnswerData()
    {
        CheckDigitAlgorithmSpecification spec = new TTest().GetSpecification();
        foreach (CheckDigitKnownAnswer vector in spec.KnownAnswers)
            yield return new object[] { vector.Name, vector.Body, vector.ExpectedCheck };
    }

    /// <summary>
    /// Invokes the algorithm's static <c>Compute</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="digits">The body digits.</param>
    /// <returns>The check digit as a character.</returns>
    protected abstract char ComputeStatic(ReadOnlySpan<char> digits);

    /// <summary>
    /// Creates a new instance of <typeparamref name="TAlgorithm" /> in its initial state.
    /// </summary>
    /// <returns>A fresh algorithm instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();
    /// <summary>
    /// Returns the <see cref="CheckDigitAlgorithmSpecification" /> describing the algorithm's expected
    /// properties and known-answer vectors.
    /// </summary>
    /// <returns>A non-null specification; <see cref="CheckDigitAlgorithmSpecification.KnownAnswers" /> must be non-empty.</returns>
    protected abstract CheckDigitAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Invokes the algorithm's static <c>IsValid</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="digitsIncludingCheck">The full sequence including the trailing check digit.</param>
    /// <returns><see langword="true" /> if the sequence is valid under the algorithm; otherwise, <see langword="false" />.</returns>
    protected abstract bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck);

}
