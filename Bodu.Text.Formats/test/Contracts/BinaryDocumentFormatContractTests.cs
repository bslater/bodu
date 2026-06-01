// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryDocumentFormatContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Contracts;

/// <summary>
/// Reusable behavioural contract test base for a binary-document format with a decode/encode pair
/// (Bencode and other byte-oriented document formats). Mirrors
/// <see cref="TextDocumentFormatContractTests{TDocument, TOptions}" /> but operates on byte payloads
/// rather than character text.
/// </summary>
/// <typeparam name="TDocument">The parsed document type.</typeparam>
/// <typeparam name="TOptions">The parser/formatter options type.</typeparam>
public abstract class BinaryDocumentFormatContractTests<TDocument, TOptions>
{
    /// <summary>
    /// Decodes <paramref name="payload" /> under the supplied options. Implementations must throw a
    /// format-specific exception when the payload is malformed.
    /// </summary>
    /// <param name="payload">The raw byte payload.</param>
    /// <param name="options">The parser options, or <see langword="default" /> to use the format's default options.</param>
    /// <returns>The decoded document.</returns>
    protected abstract TDocument Decode(byte[] payload, TOptions options = default!);

    /// <summary>
    /// Encodes <paramref name="document" /> back to its canonical byte representation under the supplied
    /// options.
    /// </summary>
    /// <param name="document">The document to serialise.</param>
    /// <param name="options">The formatter options, or <see langword="default" /> to use the format's default options.</param>
    /// <returns>The canonical byte representation.</returns>
    protected abstract byte[] Encode(TDocument document, TOptions options = default!);

    /// <summary>
    /// Returns the positive known-answer vectors for the format under test.
    /// </summary>
    protected abstract IReadOnlyList<BinaryDocumentKat<TDocument, TOptions>> ValidCases { get; }

    /// <summary>
    /// Returns the negative-vector rows the parser must reject by throwing.
    /// </summary>
    protected abstract IReadOnlyList<InvalidBinaryDocumentKat<TOptions>> InvalidCases { get; }

    /// <summary>
    /// Returns an equality comparer used by the contract tests to compare parsed documents against
    /// expected documents. Default implementation uses the type's default equality semantics.
    /// </summary>
    protected virtual IEqualityComparer<TDocument> DocumentComparer => EqualityComparer<TDocument>.Default;

    /// <summary>
    /// Verifies that every <see cref="BinaryDocumentKat{TDocument, TOptions}" /> row decodes to its
    /// expected document.
    /// </summary>
    [TestMethod]
    public void Decode_WhenKnownAnswer_ShouldReturnExpectedDocument()
    {
        foreach (BinaryDocumentKat<TDocument, TOptions> kat in ValidCases)
        {
            TDocument actual = Decode(kat.Payload, kat.Options!);

            Assert.IsTrue(
                DocumentComparer.Equals(kat.ExpectedDocument, actual),
                $"KAT '{kat.Name}': decode produced unexpected document.");
        }
    }

    /// <summary>
    /// Verifies that every <see cref="InvalidBinaryDocumentKat{TOptions}" /> row causes the decoder to
    /// throw the documented exception type.
    /// </summary>
    [TestMethod]
    public void Decode_WhenPayloadIsInvalid_ShouldThrowExpectedException()
    {
        foreach (InvalidBinaryDocumentKat<TOptions> kat in InvalidCases)
        {
            Exception? captured = null;
            try
            {
                _ = Decode(kat.Payload, kat.Options!);
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            Assert.IsNotNull(captured, $"Invalid KAT '{kat.Name}': decoder must throw.");
            Assert.AreEqual(kat.ExceptionType, captured.GetType(), $"Invalid KAT '{kat.Name}': wrong exception type.");
        }
    }

    /// <summary>
    /// Verifies that every valid row round-trips decode → encode → decode with the same expected
    /// document, proving that the decoder and encoder are inverse on the accepted domain.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValid_ShouldProduceEquivalentDocument()
    {
        foreach (BinaryDocumentKat<TDocument, TOptions> kat in ValidCases)
        {
            TDocument first = Decode(kat.Payload, kat.Options!);
            var reEncoded = Encode(first, kat.Options!);
            TDocument again = Decode(reEncoded, kat.Options!);

            Assert.IsTrue(
                DocumentComparer.Equals(first, again),
                $"KAT '{kat.Name}': decode → encode → decode did not yield an equivalent document.");
        }
    }
}
