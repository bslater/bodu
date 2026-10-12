// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvWriterTests.WritePropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Contains the member backbone tests for <see cref="Utf8DotEnvWriter.WritePropertyName(string)" /> and
/// <see cref="Utf8DotEnvWriter.WritePropertyName(string, bool)" />.
/// </summary>
public partial class Utf8DotEnvWriterTests
{
    /// <summary>
    /// Verifies that a name the reader would not read back as a key, one that is empty or does not match
    /// <c>[A-Za-z_][A-Za-z0-9_]*</c>, throws <see cref="ArgumentException" /> for <c>name</c> from both overloads and
    /// writes nothing.
    /// </summary>
    /// <param name="testName">The name of the case.</param>
    /// <param name="name">The name to write.</param>
    [TestMethod]
    [DataRow("empty", "")]
    [DataRow("equals sign", "A=B")]
    [DataRow("hash", "#A")]
    [DataRow("inner space", "A B")]
    [DataRow("leading space", " A")]
    [DataRow("trailing tab", "A\t")]
    [DataRow("line feed", "A\nB=1")]
    [DataRow("carriage return", "A\rB")]
    [DataRow("leading digit", "1A")]
    [DataRow("period", "A.B")]
    [DataRow("hyphen", "A-B")]
    [DataRow("non-ASCII letter", "ÉTÉ")]
    [DataRow("byte order mark", "﻿A")]
    public void WritePropertyName_WhenTheNameCannotBeReadBack_ShouldThrowArgumentException(string testName, string name)
    {
        var buffer = new ArrayBufferWriter<byte>();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var writer = new Utf8DotEnvWriter(buffer);
            writer.WritePropertyName(name);
        });
        var exportEx = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var writer = new Utf8DotEnvWriter(buffer);
            writer.WritePropertyName(name, export: true);
        });

        Assert.AreEqual("name", ex.ParamName, testName);
        Assert.AreEqual("name", exportEx.ParamName, testName);
        Assert.AreEqual(0, buffer.WrittenCount, testName);
    }
}
