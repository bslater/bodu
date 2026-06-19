// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseFormatContractTests{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a value-object type with a parse/format pair driven by a
/// format string and an <see cref="IFormatProvider" /> (the standard .NET text-conversion shape used by
/// <see cref="System.IParsable{TSelf}" /> / <see cref="System.IFormattable" /> types). Concrete subclasses
/// plug their type in by overriding the four parse/format adapter methods plus <see cref="ValidCases" />
/// and <see cref="InvalidCases" />.
/// </summary>
/// <typeparam name="T">The value-object type under test.</typeparam>
public abstract class ParseFormatContractTests<T>
{
    /// <summary>
    /// Parses <paramref name="text" /> with the supplied format and provider. Implementations must throw
    /// <see cref="FormatException" /> (or another documented exception type) when the text is malformed.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="format">The format string, or <see langword="null" /> for the default format.</param>
    /// <param name="provider">The format provider, or <see langword="null" /> for the default provider.</param>
    /// <returns>The parsed value.</returns>
    protected abstract T Parse(string text, string? format = null, IFormatProvider? provider = null);

    /// <summary>
    /// Attempts to parse <paramref name="text" /> with the supplied format and provider. Returns
    /// <see langword="false" /> for malformed text rather than throwing.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="format">The format string, or <see langword="null" /> for the default format.</param>
    /// <param name="provider">The format provider, or <see langword="null" /> for the default provider.</param>
    /// <param name="value">Receives the parsed value when the call returns <see langword="true" />.</param>
    /// <returns><see langword="true" /> when parsing succeeded.</returns>
    protected abstract bool TryParse(string text, string? format, IFormatProvider? provider, out T value);

    /// <summary>
    /// Formats <paramref name="value" /> with the supplied format and provider.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format string, or <see langword="null" /> for the default format.</param>
    /// <param name="provider">The format provider, or <see langword="null" /> for the default provider.</param>
    /// <returns>The formatted text.</returns>
    protected abstract string Format(T value, string? format = null, IFormatProvider? provider = null);

    /// <summary>
    /// Returns the positive parse vectors for the value type under test.
    /// </summary>
    protected abstract IReadOnlyList<ValidKat<string, T>> ValidCases { get; }

    /// <summary>
    /// Returns the negative parse vectors that <see cref="Parse" /> must reject and <see cref="TryParse" />
    /// must return <see langword="false" /> for.
    /// </summary>
    protected abstract IReadOnlyList<InvalidKat<string>> InvalidCases { get; }

    /// <summary>
    /// Verifies that <see cref="Parse" /> returns the expected value for every <see cref="ValidKat{TInput, TExpected}" /> row.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsValid_ShouldReturnExpectedValue()
    {
        foreach (ValidKat<string, T> kat in ValidCases)
        {
            T actual = Parse(kat.Input);

            Assert.AreEqual(kat.Expected, actual, $"KAT '{kat.Name}': parse produced unexpected value.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Parse" /> throws the expected exception type for every
    /// <see cref="InvalidKat{TInput}" /> row.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsInvalid_ShouldThrowExpectedException()
    {
        foreach (InvalidKat<string> kat in InvalidCases)
        {
            Exception? captured = null;
            try
            {
                _ = Parse(kat.Input);
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            Assert.IsNotNull(captured, $"Invalid KAT '{kat.Name}': parser must throw.");
            Assert.AreEqual(kat.ExceptionType, captured.GetType(), $"Invalid KAT '{kat.Name}': wrong exception type.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="TryParse" /> returns <see langword="false" /> for every
    /// <see cref="InvalidKat{TInput}" /> row without throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsInvalid_ShouldReturnFalse()
    {
        foreach (InvalidKat<string> kat in InvalidCases)
        {
            bool success = TryParse(kat.Input, format: null, provider: null, out _);

            Assert.IsFalse(success, $"Invalid KAT '{kat.Name}': TryParse must return false.");
        }
    }

    /// <summary>
    /// Verifies that every valid row round-trips parse → format → parse to the same expected value.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValid_ShouldYieldOriginalValue()
    {
        foreach (ValidKat<string, T> kat in ValidCases)
        {
            T parsed = Parse(kat.Input);
            string text = Format(parsed);
            T again = Parse(text);

            Assert.AreEqual(parsed, again, $"KAT '{kat.Name}': round-trip lost value identity.");
        }
    }
}
