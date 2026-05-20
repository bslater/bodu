// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.Format.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvTests
{

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> throws <see cref="ArgumentNullException" /> when
    /// the document is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DotEnv.Format(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> produces an empty string for an empty document.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentIsEmpty_ShouldReturnEmptyString()
    {
        DotEnvDocument doc = DotEnv.Parse(string.Empty);

        string formatted = DotEnv.Format(doc);

        Assert.AreEqual(string.Empty, formatted);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> writes an empty value as <c>KEY=</c> without
    /// quotes.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueIsEmpty_ShouldWriteKeyEqualsWithoutQuotes()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=");

        string formatted = DotEnv.Format(doc);

        StringAssert.Contains(formatted, "KEY=");
        Assert.IsFalse(formatted.Contains('"'), "Empty values should not be quoted.");
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> writes a safe value without quotes.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueContainsOnlySafeChars_ShouldWriteUnquoted()
    {
        DotEnvDocument doc = DotEnv.Parse("HOST=localhost");

        string formatted = DotEnv.Format(doc);

        StringAssert.Contains(formatted, "HOST=localhost");
        Assert.IsFalse(formatted.Contains('"'), "Safe values should not be quoted.");
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> double-quotes a value that contains spaces.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueContainsSpaces_ShouldWriteDoubleQuoted()
    {
        DotEnvDocument doc = DotEnv.Parse("GREETING=\"hello world\"");

        string formatted = DotEnv.Format(doc);

        StringAssert.Contains(formatted, "GREETING=\"hello world\"");
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> escapes double quotes inside values.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueContainsDoubleQuote_ShouldEscapeIt()
    {
        DotEnvDocument doc = DotEnv.Parse("KEY=\"say \\\"hi\\\"\"");

        string formatted = DotEnv.Format(doc);

        StringAssert.Contains(formatted, "\\\"");
    }

    /// <summary>
    /// Verifies that a round-trip through <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> and
    /// <see cref="DotEnv.Format(DotEnvDocument)" /> preserves all keys and values.
    /// </summary>
    [TestMethod]
    public void Format_WhenRoundTripped_ShouldPreserveDocumentContent()
    {
        const string source = "HOST=localhost\nPORT=8080\nDEBUG=True";

        DotEnvDocument original = DotEnv.Parse(source);
        string formatted = DotEnv.Format(original);
        DotEnvDocument roundTripped = DotEnv.Parse(formatted);

        Assert.AreEqual("localhost", roundTripped["HOST"]);
        Assert.AreEqual("8080", roundTripped["PORT"]);
        Assert.AreEqual("True", roundTripped["DEBUG"]);
    }

}
