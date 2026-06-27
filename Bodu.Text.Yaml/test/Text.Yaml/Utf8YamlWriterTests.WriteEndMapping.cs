// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WriteEndMapping.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WriteEndMapping" /> emits the expected output and enforces its contract.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>
    /// Verifies that closing a mapping while a property name awaits its value throws
    /// <see cref="InvalidOperationException" /> rather than silently dropping the key.
    /// </summary>
    [TestMethod]
    public void WriteEndMapping_WhenPropertyNamePending_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteStartMapping();
            writer.WritePropertyName("a");
            writer.WriteEndMapping();
        });
    }

}
