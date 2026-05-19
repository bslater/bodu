// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Behavioural tests for <see cref="IniExtensions" />.
/// </summary>
[TestClass]
public sealed class IniExtensionsTests
{
    private const string CanonicalSource = "[database]\nhost=localhost\nport=5432";

    /// <summary>
    /// Verifies that <see cref="IniExtensions.ParseIni(ReadOnlySpan{char})" /> produces the same document as the
    /// canonical <see cref="Ini.Parse(ReadOnlySpan{char})" /> static method.
    /// </summary>
    [TestMethod]
    public void ParseIni_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        IniDocument fromExtension = CanonicalSource.AsSpan().ParseIni();
        IniDocument fromStatic = Ini.Parse(CanonicalSource);

        Assert.AreEqual(fromStatic.Sections.Count, fromExtension.Sections.Count);
        Assert.AreEqual(fromStatic.GetSection("database")!["host"], fromExtension.GetSection("database")!["host"]);
        Assert.AreEqual(fromStatic.GetSection("database")!["port"], fromExtension.GetSection("database")!["port"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniExtensions.ParseIni(string)" /> produces the same document as the canonical static
    /// method.
    /// </summary>
    [TestMethod]
    public void ParseIni_OnString_ShouldMatchStaticCanonical()
    {
        IniDocument fromExtension = CanonicalSource.ParseIni();
        IniDocument fromStatic = Ini.Parse(CanonicalSource);

        Assert.AreEqual(fromStatic.GetSection("database")!["host"], fromExtension.GetSection("database")!["host"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniExtensions.ParseIni(string, IniParseOptions)" /> respects the supplied options.
    /// </summary>
    [TestMethod]
    public void ParseIni_OnStringWithOptions_ShouldRespectOptions()
    {
        IniParseOptions options = new() { CaseSensitiveSections = true };

        IniDocument fromExtension = CanonicalSource.ParseIni(options);
        IniDocument fromStatic = Ini.Parse(CanonicalSource, options);

        Assert.AreEqual(fromStatic.Sections.Count, fromExtension.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniExtensions.TryParseIni(string, out IniDocument?)" /> returns <see langword="true" />
    /// and produces a document for valid input.
    /// </summary>
    [TestMethod]
    public void TryParseIni_OnString_WhenInputIsValid_ShouldReturnTrue()
    {
        var success = CanonicalSource.TryParseIni(out IniDocument? document);

        Assert.IsTrue(success);
        Assert.IsNotNull(document);
        Assert.AreEqual("localhost", document.GetSection("database")!["host"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniExtensions.TryParseIni(ReadOnlySpan{char}, out IniDocument?)" /> matches the
    /// canonical static <see cref="Ini.TryParse(ReadOnlySpan{char}, out IniDocument?)" /> outcome.
    /// </summary>
    [TestMethod]
    public void TryParseIni_OnReadOnlySpan_ShouldMatchStaticCanonical()
    {
        var extSuccess = CanonicalSource.AsSpan().TryParseIni(out IniDocument? extDocument);
        var staticSuccess = Ini.TryParse(CanonicalSource, out IniDocument? staticDocument);

        Assert.AreEqual(staticSuccess, extSuccess);
        Assert.AreEqual(
            staticDocument!.GetSection("database")!["host"],
            extDocument!.GetSection("database")!["host"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniExtensions.FormatIni(IniDocument)" /> returns the same text as the canonical
    /// <see cref="Ini.Format(IniDocument)" /> static method.
    /// </summary>
    [TestMethod]
    public void FormatIni_OnIniDocument_ShouldMatchStaticCanonical()
    {
        IniDocument document = Ini.Parse(CanonicalSource);

        var fromExtension = document.FormatIni();
        var fromStatic = Ini.Format(document);

        Assert.AreEqual(fromStatic, fromExtension);
    }

    /// <summary>
    /// Verifies that <c>ParseIni</c> followed by <c>FormatIni</c> round-trips an INI document through the extension
    /// surface.
    /// </summary>
    [TestMethod]
    public void ParseIniThenFormatIni_ShouldRoundTrip()
    {
        IniDocument first = CanonicalSource.ParseIni();
        var emitted = first.FormatIni();
        IniDocument second = emitted.ParseIni();

        Assert.AreEqual(first.GetSection("database")!["host"], second.GetSection("database")!["host"]);
        Assert.AreEqual(first.GetSection("database")!["port"], second.GetSection("database")!["port"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniExtensions.FormatIni(IniDocument)" /> throws
    /// <see cref="ArgumentNullException" /> when the receiver is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FormatIni_WhenDocumentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((IniDocument)null!).FormatIni();
        });
    }
}
