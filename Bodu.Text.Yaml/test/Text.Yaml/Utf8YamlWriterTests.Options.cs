// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.Options.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter" /> validates its <see cref="YamlWriterOptions" /> at construction,
/// rejecting an unsupported newline string or an out-of-range indentation width.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>Verifies that an unsupported newline string is rejected with <see cref="ArgumentException" />.</summary>
    [TestMethod]
    public void Ctor_WhenNewLineIsUnsupported_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            _ = new Utf8YamlWriter(buffer, new YamlWriterOptions { NewLine = "not-a-newline" });
        });
    }

    /// <summary>Verifies that an indentation width above the maximum is rejected with
    /// <see cref="ArgumentOutOfRangeException" />.</summary>
    [TestMethod]
    public void Ctor_WhenIndentSizeAboveMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            _ = new Utf8YamlWriter(buffer, new YamlWriterOptions { IndentSize = int.MaxValue });
        });
    }

    /// <summary>Verifies that a carriage-return line feed is accepted as a newline string.</summary>
    [TestMethod]
    public void Ctor_WhenNewLineIsCarriageReturnLineFeed_ShouldSucceed()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer, new YamlWriterOptions { NewLine = "\r\n" });
        writer.WriteString("ok");

        Assert.IsTrue(buffer.WrittenCount > 0);
    }
}
