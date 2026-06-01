// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.ToUtf8Bytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToUtf8Bytes_WhenInvoked_ShouldMatchBclUtf8" /> spanning
    /// empty, ASCII, multi-byte and surrogate-pair payloads.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object[]> GetToUtf8BytesCases() =>
    [
        [string.Empty],
        ["hello, world"],
        ["héllo — Привет — 你好 — \U0001F600"],
        ["\U0001F4A9"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToUtf8Bytes(string)" /> matches the BCL UTF-8 byte
    /// sequence for representative input strings.
    /// </summary>
    /// <param name="text">The string under test.</param>
    [TestMethod]
    [DynamicData(nameof(GetToUtf8BytesCases))]
    public void ToUtf8Bytes_WhenInvoked_ShouldMatchBclUtf8(string text)
    {
        var expected = System.Text.Encoding.UTF8.GetBytes(text);

        var actual = text.ToUtf8Bytes();

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToUtf8Bytes(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToUtf8Bytes_WhenTextIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.ToUtf8Bytes(null!);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
