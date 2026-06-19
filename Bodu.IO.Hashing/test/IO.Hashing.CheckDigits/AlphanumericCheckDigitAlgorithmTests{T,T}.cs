// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// Invokes the algorithm's static <c>Compute</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="body">The body characters.</param>
    /// <returns>The check character.</returns>
    protected abstract char ComputeStatic(ReadOnlySpan<char> body);

    /// <summary>
    /// Creates a new instance of <typeparamref name="TAlgorithm" /> in its initial state.
    /// </summary>
    /// <returns>A fresh algorithm instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();
    /// <summary>
    /// Returns the <see cref="AlphanumericCheckDigitAlgorithmSpecification" /> describing the algorithm's expected
    /// properties and known-answer vectors.
    /// </summary>
    /// <returns>A non-null specification; its <c>KnownAnswers</c> must be non-empty.</returns>
    protected abstract AlphanumericCheckDigitAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Invokes the algorithm's static <c>IsValid</c> helper. Derived classes forward to the concrete type.
    /// </summary>
    /// <param name="valueIncludingCheck">The full sequence including the trailing check character.</param>
    /// <returns><see langword="true" /> if the sequence is valid under the algorithm; otherwise, <see langword="false" />.</returns>
    protected abstract bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck);

}
