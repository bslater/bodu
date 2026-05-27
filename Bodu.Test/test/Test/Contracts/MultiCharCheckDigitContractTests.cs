// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a multi-character check-digit algorithm (ISO 7064
/// MOD 97-10, IBAN, LEI, …). Concrete subclasses override <see cref="Compute" />, <see cref="IsValid" />,
/// and <see cref="KnownAnswers" /> to plug their algorithm into the inherited tests.
/// </summary>
/// <typeparam name="TAlgorithm">The algorithm type under test.</typeparam>
/// <remarks>
/// <para>
/// Carried for documentation only.
/// </para>
/// </remarks>
public abstract class MultiCharCheckDigitContractTests<TAlgorithm>
{
    /// <summary>
    /// Computes the multi-character check sequence for <paramref name="payload" /> under the algorithm
    /// under test.
    /// </summary>
    /// <param name="payload">The body characters without the check sequence.</param>
    /// <returns>The computed check sequence as a string of one or more characters.</returns>
    protected abstract string Compute(string payload);

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="fullValue" /> (payload followed by the
    /// algorithm's check sequence) is valid under the algorithm under test.
    /// </summary>
    /// <param name="fullValue">The canonical full value including the check sequence.</param>
    /// <returns><see langword="true" /> when the value is valid; otherwise <see langword="false" />.</returns>
    protected abstract bool IsValid(string fullValue);

    /// <summary>
    /// Returns the known-answer vectors for the algorithm under test.
    /// </summary>
    protected abstract IReadOnlyList<CheckDigitKat> KnownAnswers { get; }

    /// <summary>
    /// Verifies that every <see cref="CheckDigitKat" /> row's payload computes to its expected check
    /// sequence.
    /// </summary>
    [TestMethod]
    public void Compute_WhenPayloadIsKnown_ShouldReturnExpectedCheckSequence()
    {
        foreach (CheckDigitKat kat in KnownAnswers)
        {
            string actual = Compute(kat.Payload);

            Assert.AreEqual(
                kat.CheckDigit,
                actual,
                $"KAT '{kat.Name}': compute produced unexpected check sequence.");
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
    /// Verifies that flipping the final check character of every known-good value causes validation
    /// to return <see langword="false" />. Documents that the check sequence is genuinely load-bearing.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckSequenceCorrupted_ShouldReturnFalse()
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
