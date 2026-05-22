// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Behavioural tests for <see cref="DelimitedExtensions" />.
/// </summary>
[TestClass]
public sealed class DelimitedExtensionsTests
{
    private const string CanonicalSource = "name,age\nAda,42\nLinus,55";

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.ParseDelimited(ReadOnlySpan{char})" /> produces the same document
    /// as the canonical <see cref="Delimited.Parse(ReadOnlySpan{char})" /> static method.
    /// </summary>
    [TestMethod]
    public void ParseDelimited_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        DelimitedDocument fromExtension = CanonicalSource.AsSpan().ParseDelimited();
        DelimitedDocument fromStatic = Delimited.Parse(CanonicalSource);

        Assert.AreEqual(fromStatic.Rows.Count, fromExtension.Rows.Count);
        Assert.AreEqual(fromStatic.Headers.Count, fromExtension.Headers.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.ParseDelimited(string)" /> produces the same document as the
    /// canonical static method.
    /// </summary>
    [TestMethod]
    public void ParseDelimited_OnString_ShouldMatchStaticCanonical()
    {
        DelimitedDocument fromExtension = CanonicalSource.ParseDelimited();
        DelimitedDocument fromStatic = Delimited.Parse(CanonicalSource);

        Assert.AreEqual(fromStatic.Rows.Count, fromExtension.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.ParseDelimited(string, DelimitedParseOptions)" /> respects the
    /// supplied options.
    /// </summary>
    [TestMethod]
    public void ParseDelimited_OnStringWithOptions_ShouldRespectOptions()
    {
        DelimitedParseOptions options = new() { Delimiter = '\t', HasHeader = false };
        const string tsv = "Ada\t42\nLinus\t55";

        DelimitedDocument fromExtension = tsv.ParseDelimited(options);
        DelimitedDocument fromStatic = Delimited.Parse(tsv, options);

        Assert.AreEqual(fromStatic.Rows.Count, fromExtension.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.TryParseDelimited(string, out DelimitedDocument?)" /> returns
    /// <see langword="true" /> and produces a document for valid input.
    /// </summary>
    [TestMethod]
    public void TryParseDelimited_OnString_WhenInputIsValid_ShouldReturnTrue()
    {
        var success = CanonicalSource.TryParseDelimited(out DelimitedDocument? document);

        Assert.IsTrue(success);
        Assert.IsNotNull(document);
        Assert.AreEqual(2, document.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.TryParseDelimited(ReadOnlySpan{char}, out DelimitedDocument?)" />
    /// matches the canonical static
    /// <see cref="Delimited.TryParse(ReadOnlySpan{char}, out DelimitedDocument?)" /> outcome.
    /// </summary>
    [TestMethod]
    public void TryParseDelimited_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        var extSuccess = CanonicalSource.AsSpan().TryParseDelimited(out DelimitedDocument? extDocument);
        var staticSuccess = Delimited.TryParse(CanonicalSource, out DelimitedDocument? staticDocument);

        Assert.AreEqual(staticSuccess, extSuccess);
        Assert.AreEqual(staticDocument!.Rows.Count, extDocument!.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.FormatDelimited(DelimitedDocument)" /> returns the same text as the
    /// canonical <see cref="Delimited.Format(DelimitedDocument)" /> static method.
    /// </summary>
    [TestMethod]
    public void FormatDelimited_OnDelimitedDocument_ShouldMatchStaticCanonical()
    {
        DelimitedDocument document = Delimited.Parse(CanonicalSource);

        var fromExtension = document.FormatDelimited();
        var fromStatic = Delimited.Format(document);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.FormatDelimited(DelimitedDocument, DelimitedParseOptions)" />
    /// matches the canonical static method when options are supplied.
    /// </summary>
    [TestMethod]
    public void FormatDelimited_OnDelimitedDocumentWithOptions_ShouldMatchStaticCanonical()
    {
        DelimitedDocument document = Delimited.Parse(CanonicalSource);
        DelimitedParseOptions options = new() { Delimiter = '\t' };

        var fromExtension = document.FormatDelimited(options);
        var fromStatic = Delimited.Format(document, options);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <c>ParseDelimited</c> followed by <c>FormatDelimited</c> round-trips a delimited document through
    /// the extension surface.
    /// </summary>
    [TestMethod]
    public void ParseDelimitedThenFormatDelimited_ShouldRoundTrip()
    {
        DelimitedDocument first = CanonicalSource.ParseDelimited();
        var emitted = first.FormatDelimited();
        DelimitedDocument second = emitted.ParseDelimited();

        Assert.AreEqual(first.Rows.Count, second.Rows.Count);
        Assert.AreEqual(first.Headers.Count, second.Headers.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedExtensions.FormatDelimited(DelimitedDocument)" /> throws
    /// <see cref="ArgumentNullException" /> when the receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FormatDelimited_WhenDocumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((DelimitedDocument)null!).FormatDelimited();
        });
    }
}
