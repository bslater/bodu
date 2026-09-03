// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiEncodingResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the behavior of <see cref="MapiEncodingResolver" />, the code-page resolver.
/// </summary>
[TestClass]
public partial class MapiEncodingResolverTests
{
    /// <summary>
    /// Verifies that a declared Windows code page resolves — proving the code-pages provider registration — and that
    /// the message code page is preferred over the internet code page.
    /// </summary>
    [TestMethod]
    public void GetEncoding_WhenMessageCodePageDeclared_ShouldResolveAndPreferIt()
    {
        Assert.AreEqual(932, MapiEncodingResolver.GetEncoding(932, 1251).CodePage);
        Assert.AreEqual(1251, MapiEncodingResolver.GetEncoding(null, 1251).CodePage);
    }

    /// <summary>
    /// Verifies that Windows-1252 is the fallback when nothing is declared.
    /// </summary>
    [TestMethod]
    public void GetEncoding_WhenNothingDeclared_ShouldFallBackToWindows1252()
    {
        Assert.AreEqual(1252, MapiEncodingResolver.GetEncoding(null, null).CodePage);
    }

    /// <summary>
    /// Verifies that an unknown or out-of-range code page falls through to the next candidate or the fallback.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="messageCodePage">The declared message code page.</param>
    [TestMethod]
    [DataRow("Unknown", 12345)]
    [DataRow("Zero", 0)]
    [DataRow("Negative", -1)]
    [DataRow("TooLarge", 70000)]
    public void GetEncoding_WhenCodePageUnusable_ShouldFallBack(string testName, int messageCodePage)
    {
        _ = testName;

        Assert.AreEqual(1252, MapiEncodingResolver.GetEncoding(messageCodePage, null).CodePage);
        Assert.AreEqual(932, MapiEncodingResolver.GetEncoding(messageCodePage, 932).CodePage);
    }

    /// <summary>
    /// Verifies that a UTF-16 code page (1200 or 1201) is not a usable encoding for code-page strings — a writer that
    /// declares it means "this message is Unicode" — so resolution falls through to the next candidate.
    /// </summary>
    /// <param name="messageCodePage">The declared message code page.</param>
    [TestMethod]
    [DataRow(1200)]
    [DataRow(1201)]
    public void GetEncoding_WhenMessageCodePageIsUtf16_ShouldFallThroughToNextCandidate(int messageCodePage)
    {
        Assert.AreEqual(1251, MapiEncodingResolver.GetEncoding(messageCodePage, 1251).CodePage);
        Assert.AreEqual(1252, MapiEncodingResolver.GetEncoding(messageCodePage, null).CodePage);
    }

    /// <summary>
    /// Verifies that the HTML-body resolution prefers the internet code page over the message code page — the
    /// reverse of the precedence code-page strings use — and falls back the same way.
    /// </summary>
    [TestMethod]
    public void GetHtmlEncoding_WhenBothDeclared_ShouldPreferInternetCodePage()
    {
        Assert.AreEqual(932, MapiEncodingResolver.GetHtmlEncoding(932, 1251).CodePage);
        Assert.AreEqual(1251, MapiEncodingResolver.GetHtmlEncoding(null, 1251).CodePage);
        Assert.AreEqual(1252, MapiEncodingResolver.GetHtmlEncoding(null, null).CodePage);
    }
}
