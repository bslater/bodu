// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextDocumentFormatContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a text-document format with a parse/format pair (Bencode,
/// Delimited, DotEnv, INI, etc.). Concrete subclasses override <see cref="Parse" />, <see cref="Format" />,
/// <see cref="ValidCases" />, and <see cref="InvalidCases" /> to plug the format into the inherited tests.
/// </summary>
/// <typeparam name="TDocument">The parsed document type.</typeparam>
/// <typeparam name="TOptions">The parser/formatter options type.</typeparam>
public abstract class TextDocumentFormatContractTests<TDocument, TOptions>
{
    /// <summary>
    /// Parses <paramref name="text" /> under the supplied options. Implementations must throw a format-
    /// specific exception when the text is malformed.
    /// </summary>
    /// <param name="text">The raw source text.</param>
    /// <param name="options">The parser options, or <see langword="default" /> to use the format's default options.</param>
    /// <returns>The parsed document.</returns>
    protected abstract TDocument Parse(string text, TOptions? options = default);

    /// <summary>
    /// Formats <paramref name="document" /> back to its canonical text representation under the supplied
    /// options.
    /// </summary>
    /// <param name="document">The document to serialise.</param>
    /// <param name="options">The formatter options, or <see langword="default" /> to use the format's default options.</param>
    /// <returns>The canonical text representation.</returns>
    protected abstract string Format(TDocument document, TOptions? options = default);

    /// <summary>
    /// Returns the positive known-answer vectors for the format under test.
    /// </summary>
    protected abstract IReadOnlyList<TextDocumentKat<TDocument, TOptions>> ValidCases { get; }

    /// <summary>
    /// Returns the negative-vector rows the parser must reject by throwing.
    /// </summary>
    protected abstract IReadOnlyList<InvalidTextDocumentKat<TOptions>> InvalidCases { get; }

    /// <summary>
    /// Returns an equality comparer used by the contract tests to compare parsed documents against
    /// expected documents. Default implementation uses the type's default equality semantics.
    /// </summary>
    protected virtual IEqualityComparer<TDocument> DocumentComparer => EqualityComparer<TDocument>.Default;

    /// <summary>
    /// Verifies that every <see cref="TextDocumentKat{TDocument, TOptions}" /> row parses to its expected
    /// document.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKnownAnswer_ShouldReturnExpectedDocument()
    {
        foreach (TextDocumentKat<TDocument, TOptions> kat in ValidCases)
        {
            TDocument actual = Parse(kat.Text, kat.Options);

            Assert.IsTrue(
                DocumentComparer.Equals(kat.ExpectedDocument, actual),
                $"KAT '{kat.Name}': parse produced unexpected document.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="InvalidTextDocumentKat{TOptions}" /> row causes the parser to throw
    /// the documented exception type, with an optional message-substring check.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsInvalid_ShouldThrowExpectedException()
    {
        foreach (InvalidTextDocumentKat<TOptions> kat in InvalidCases)
        {
            Exception? captured = null;
            try
            {
                _ = Parse(kat.Text, kat.Options);
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            Assert.IsNotNull(captured, $"Invalid KAT '{kat.Name}': parser must throw.");
            Assert.AreEqual(kat.ExceptionType, captured.GetType(), $"Invalid KAT '{kat.Name}': wrong exception type.");

            if (kat.MessageContains is not null)
            {
                StringAssert.Contains(captured.Message, kat.MessageContains, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// Verifies that every valid row round-trips parse → format → parse with the same expected document.
    /// Documents that the parser and formatter are inverse on the accepted domain.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValid_ShouldProduceEquivalentDocument()
    {
        foreach (TextDocumentKat<TDocument, TOptions> kat in ValidCases)
        {
            TDocument first = Parse(kat.Text, kat.Options);
            string serialised = Format(first, kat.Options);
            TDocument again = Parse(serialised, kat.Options);

            Assert.IsTrue(
                DocumentComparer.Equals(first, again),
                $"KAT '{kat.Name}': parse → format → parse did not yield an equivalent document.");
        }
    }
}
