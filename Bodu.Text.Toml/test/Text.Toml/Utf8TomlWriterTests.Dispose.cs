// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that disposing a <see cref="Utf8TomlWriter" /> flushes a stream destination.
/// </summary>
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// Verifies that disposing a stream-backed writer flushes the buffered document, enabling the
    /// <see langword="using" /> pattern.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenStreamDestination_ShouldFlush()
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8TomlWriter(stream))
        {
            writer.WriteStartTable();
            writer.WriteString("s", "x");
            writer.WriteEndTable();
        }

        Assert.AreEqual("s = \"x\"\n", Encoding.UTF8.GetString(stream.ToArray()));
    }

}
