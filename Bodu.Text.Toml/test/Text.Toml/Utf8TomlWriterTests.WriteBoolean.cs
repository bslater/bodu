// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteBoolean.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteBoolean" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that boolean values are emitted as the lowercase keywords <c>true</c> and <c>false</c>.
    /// </summary>
    /// <param name="value">The boolean value to write.</param>
    /// <param name="expected">The expected emitted keyword.</param>
    [TestMethod]
    [DataRow(true, "true", DisplayName = "true")]
    [DataRow(false, "false", DisplayName = "false")]
    public void WriteBoolean_WhenValue_ShouldEmitKeyword(bool value, string expected)
    {
        string actual = WriteDocument((ref Utf8TomlWriter writer) =>
        {
            writer.WriteStartTable();
            writer.WritePropertyName("v");
            writer.WriteBoolean(value);
            writer.WriteEndTable();
        });

        Assert.AreEqual($"v = {expected}\n", actual);
    }

}
