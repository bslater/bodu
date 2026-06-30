// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WritePropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WritePropertyName" /> emits the expected output and enforces its contract.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>
    /// Verifies that writing a property name outside a mapping throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WritePropertyName_WhenNotInMapping_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteStartSequence();
            writer.WritePropertyName("a");
        });
    }

}
