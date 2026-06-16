// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Behavioural tests for <see cref="DotEnvExtensions" />.
/// </summary>
[TestClass]
public sealed class DotEnvExtensionsTests
{
    private const string CanonicalSource = "PORT=8080\nHOST=localhost";

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.ParseDotEnv(ReadOnlySpan{char})" /> produces the same document as the
    /// canonical <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> static method.
    /// </summary>
    [TestMethod]
    public void ParseDotEnv_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        DotEnvDocument fromExtension = CanonicalSource.AsSpan().ParseDotEnv();
        DotEnvDocument fromStatic = DotEnv.Parse(CanonicalSource);

        Assert.AreEqual(fromStatic["PORT"], fromExtension["PORT"]);
        Assert.AreEqual(fromStatic["HOST"], fromExtension["HOST"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.ParseDotEnv(string)" /> produces the same document as the canonical
    /// static method.
    /// </summary>
    [TestMethod]
    public void ParseDotEnv_OnString_ShouldMatchStaticCanonical()
    {
        DotEnvDocument fromExtension = CanonicalSource.ParseDotEnv();
        DotEnvDocument fromStatic = DotEnv.Parse(CanonicalSource);

        Assert.AreEqual(fromStatic["PORT"], fromExtension["PORT"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.ParseDotEnv(string, DotEnvParseOptions)" /> respects the supplied
    /// options.
    /// </summary>
    [TestMethod]
    public void ParseDotEnv_OnStringWithOptions_ShouldRespectOptions()
    {
        DotEnvParseOptions options = new();

        DotEnvDocument fromExtension = CanonicalSource.ParseDotEnv(options);
        DotEnvDocument fromStatic = DotEnv.Parse(CanonicalSource, options);

        Assert.AreEqual(fromStatic.Entries.Count, fromExtension.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.TryParseDotEnv(string, out DotEnvDocument?)" /> returns
    /// <see langword="true" /> and produces a document for valid input.
    /// </summary>
    [TestMethod]
    public void TryParseDotEnv_OnString_WhenInputIsValid_ShouldReturnTrue()
    {
        bool success = CanonicalSource.TryParseDotEnv(out DotEnvDocument? document);

        Assert.IsTrue(success);
        Assert.IsNotNull(document);
        Assert.AreEqual("8080", document["PORT"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.TryParseDotEnv(ReadOnlySpan{char}, out DotEnvDocument?)" /> matches
    /// the canonical static <see cref="DotEnv.TryParse(ReadOnlySpan{char}, out DotEnvDocument?)" /> outcome.
    /// </summary>
    [TestMethod]
    public void TryParseDotEnv_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        bool extSuccess = CanonicalSource.AsSpan().TryParseDotEnv(out DotEnvDocument? extDocument);
        bool staticSuccess = DotEnv.TryParse(CanonicalSource, out DotEnvDocument? staticDocument);

        Assert.AreEqual(staticSuccess, extSuccess);
        Assert.AreEqual(staticDocument!["PORT"], extDocument!["PORT"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.FormatDotEnv(DotEnvDocument)" /> returns the same text as the
    /// canonical <see cref="DotEnv.Format(DotEnvDocument)" /> static method.
    /// </summary>
    [TestMethod]
    public void FormatDotEnv_OnDotEnvDocument_ShouldMatchStaticCanonical()
    {
        DotEnvDocument document = DotEnv.Parse(CanonicalSource);

        string fromExtension = document.FormatDotEnv();
        string fromStatic = DotEnv.Format(document);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <c>ParseDotEnv</c> followed by <c>FormatDotEnv</c> round-trips a DotEnv document through the
    /// extension surface.
    /// </summary>
    [TestMethod]
    public void ParseDotEnvThenFormatDotEnv_ShouldRoundTrip()
    {
        DotEnvDocument first = CanonicalSource.ParseDotEnv();
        string emitted = first.FormatDotEnv();
        DotEnvDocument second = emitted.ParseDotEnv();

        Assert.AreEqual(first["PORT"], second["PORT"]);
        Assert.AreEqual(first["HOST"], second["HOST"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvExtensions.FormatDotEnv(DotEnvDocument)" /> throws
    /// <see cref="ArgumentNullException" /> when the receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FormatDotEnv_WhenDocumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((DotEnvDocument)null!).FormatDotEnv();
        });
    }

    /// <summary>
    /// Verifies that the span overload of <see cref="DotEnvExtensions.ParseDotEnv(ReadOnlySpan{char},
    /// DotEnvParseOptions)" /> parses using the supplied options.
    /// </summary>
    [TestMethod]
    public void ParseDotEnv_OnReadOnlySpanWithOptions_ShouldParse()
    {
        DotEnvDocument doc = CanonicalSource.AsSpan().ParseDotEnv(DotEnvParseOptions.Default);

        Assert.AreEqual("8080", doc["PORT"]);
    }

    /// <summary>
    /// Verifies that the span overload of <see cref="DotEnvExtensions.TryParseDotEnv(ReadOnlySpan{char},
    /// DotEnvParseOptions, out DotEnvDocument)" /> succeeds and yields a document.
    /// </summary>
    [TestMethod]
    public void TryParseDotEnv_OnReadOnlySpanWithOptions_ShouldReturnTrueAndDocument()
    {
        bool parsed = CanonicalSource.AsSpan().TryParseDotEnv(DotEnvParseOptions.Default, out DotEnvDocument? doc);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(doc);
    }

    /// <summary>
    /// Verifies that the string overload of <see cref="DotEnvExtensions.TryParseDotEnv(string, DotEnvParseOptions, out
    /// DotEnvDocument)" /> succeeds and yields a document.
    /// </summary>
    [TestMethod]
    public void TryParseDotEnv_OnStringWithOptions_ShouldReturnTrueAndDocument()
    {
        bool parsed = CanonicalSource.TryParseDotEnv(DotEnvParseOptions.Default, out DotEnvDocument? doc);

        Assert.IsTrue(parsed);
        Assert.IsNotNull(doc);
    }
}
