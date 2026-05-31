// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a single-character check-digit algorithm (Luhn, Damm,
/// ABA, EAN, GTIN, ISBN, …). Concrete subclasses override <see cref="Compute" />,
/// <see cref="IsValid" />, and <see cref="KnownAnswers" /> to plug their algorithm into the inherited
/// tests.
/// </summary>
/// <typeparam name="TAlgorithm">The algorithm type under test.</typeparam>
/// <remarks>
/// <para>
/// The base targets the static <c>Compute</c> / <c>IsValid</c> surface most check-digit algorithms
/// expose. Append/streaming and Reset coverage stays in each algorithm's bespoke partials because the
/// state-machine semantics differ between algorithms.
/// </para>
/// <para>
/// Carried for documentation only.
/// </para>
/// </remarks>
public abstract class CheckDigitContractTests<TAlgorithm>
{
    /// <summary>
    /// Computes the check digit for <paramref name="payload" /> under the algorithm under test.
    /// </summary>
    /// <param name="payload">The body characters/digits without the check digit.</param>
    /// <returns>The computed check digit as a single character.</returns>
    protected abstract char Compute(string payload);

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="fullValue" /> (payload followed by the
    /// algorithm's check digit) is valid under the algorithm under test.
    /// </summary>
    /// <param name="fullValue">The canonical full value including the check digit.</param>
    /// <returns><see langword="true" /> when the value is valid; otherwise <see langword="false" />.</returns>
    protected abstract bool IsValid(string fullValue);

    /// <summary>
    /// Returns the known-answer vectors for the algorithm under test.
    /// </summary>
    protected abstract IReadOnlyList<CheckDigitKat> KnownAnswers { get; }

    /// <summary>
    /// Verifies that every <see cref="CheckDigitKat" /> row's payload computes to its expected check
    /// digit.
    /// </summary>
    [TestMethod]
    public void Compute_WhenPayloadIsKnown_ShouldReturnExpectedCheckDigit()
    {
        foreach (CheckDigitKat kat in KnownAnswers)
        {
            char actual = Compute(kat.Payload);

            Assert.AreEqual(
                kat.CheckDigit[0],
                actual,
                $"KAT '{kat.Name}': compute produced unexpected check digit.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="CheckDigitKat" /> row's canonical full value validates true.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenFullValueIsKnownValid_ShouldReturnTrue()
    {
        foreach (CheckDigitKat kat in KnownAnswers)
        {
            bool actual = IsValid(kat.FullValue);

            Assert.IsTrue(actual, $"KAT '{kat.Name}': validation rejected the canonical full value.");
        }
    }

    /// <summary>
    /// Verifies that flipping the final check digit of every known-good value causes validation to
    /// return <see langword="false" />. Documents that the check digit is genuinely load-bearing.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckDigitIsCorrupted_ShouldReturnFalse()
    {
        foreach (CheckDigitKat kat in KnownAnswers)
        {
            if (kat.FullValue.Length == 0)
                continue;

            char original = kat.FullValue[^1];
            char swapped = original switch
            {
                '0' => '1',
                _ => '0',
            };

            string corrupted = kat.FullValue[..^1] + swapped;
            bool actual = IsValid(corrupted);

            Assert.IsFalse(actual, $"KAT '{kat.Name}': validation accepted a corrupted value.");
        }
    }
}
