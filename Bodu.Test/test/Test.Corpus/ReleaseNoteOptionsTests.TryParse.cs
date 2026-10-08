// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteOptionsTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteOptionsTests
{
    /// <summary>
    /// Verifies that an empty field parses to no options.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenFieldIsEmpty_ShouldReturnNoOptions()
    {
        bool parsed = ReleaseNoteOptions.TryParse(string.Empty, out IReadOnlyList<KeyValuePair<string, string>>? options, out string? error);

        Assert.IsTrue(parsed, error);
        Assert.IsNotNull(options);
        Assert.AreEqual(0, options.Count);
    }

    /// <summary>
    /// Verifies that options parse in the order written, with each value's escapes decoded.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenFieldHoldsSeveralOptions_ShouldReturnThemInOrderWithDecodedValues()
    {
        bool parsed = ReleaseNoteOptions.TryParse(@"Delimiter=\x3B;Quote=';MaxDepth=12;Empty=", out IReadOnlyList<KeyValuePair<string, string>>? options, out string? error);

        Assert.IsTrue(parsed, error);
        Assert.IsNotNull(options);
        CollectionAssert.AreEqual(
            new[]
            {
                new KeyValuePair<string, string>("Delimiter", ";"),
                new KeyValuePair<string, string>("Quote", "'"),
                new KeyValuePair<string, string>("MaxDepth", "12"),
                new KeyValuePair<string, string>("Empty", string.Empty),
            },
            options.ToArray());
    }

    /// <summary>
    /// Verifies that a malformed field fails to parse with an error.
    /// </summary>
    /// <param name="text">The malformed field.</param>
    [TestMethod]
    [DataRow("MaxDepth")]
    [DataRow("=1")]
    [DataRow("1Name=1")]
    [DataRow("Max Depth=1")]
    [DataRow("MaxDepth=1;MaxDepth=2")]
    [DataRow("MaxDepth=1;")]
    [DataRow(@"Delimiter=\q")]
    [DataRow(@"Delimiter=\xFF")]
    public void TryParse_WhenFieldIsMalformed_ShouldReturnFalseWithAnError(string text)
    {
        bool parsed = ReleaseNoteOptions.TryParse(text, out IReadOnlyList<KeyValuePair<string, string>>? options, out string? error);

        Assert.IsFalse(parsed);
        Assert.IsNull(options);
        Assert.IsFalse(string.IsNullOrEmpty(error));
    }

    /// <summary>
    /// Verifies that parsing a <see langword="null" /> field throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenFieldIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ReleaseNoteOptions.TryParse(null!, out _, out _);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
