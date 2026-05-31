// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.GetUtf8ByteCount.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="GetUtf8ByteCount_WhenInvoked_ShouldMatchBclUtf8" /> spanning
    /// empty, ASCII, multi-byte and surrogate-pair payloads.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object[]> GetUtf8ByteCountCases() =>
    [
        [string.Empty],
        ["hello, world"],
        ["héllo — Привет — 你好 — \U0001F600"],
        ["\U0001F4A9"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetUtf8ByteCount(string)" /> matches the BCL UTF-8
    /// byte count for representative input strings.
    /// </summary>
    /// <param name="text">The string under test.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetUtf8ByteCountCases), DynamicDataSourceType.Method)]
    public void GetUtf8ByteCount_WhenInvoked_ShouldMatchBclUtf8(string text)
    {
        Assert.AreEqual(System.Text.Encoding.UTF8.GetByteCount(text), text.GetUtf8ByteCount());
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetUtf8ByteCount(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetUtf8ByteCount_WhenTextIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.GetUtf8ByteCount(null!);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
