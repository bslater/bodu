// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.WriteEndTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlWriter.WriteEndTable" /> emits the expected output and enforces its contract.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that closing a table while the innermost open container is an array throws
    /// <see cref="InvalidOperationException" /> rather than an implementation-detail cast failure.
    /// </summary>
    [TestMethod]
    public void WriteEndTable_WhenCurrentContainerIsArray_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            ArrayBufferWriter<byte> buffer = new();
            Utf8TomlWriter writer = new(buffer);

            writer.WriteStartTable();
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            writer.WriteEndTable();
        });
    }

}
