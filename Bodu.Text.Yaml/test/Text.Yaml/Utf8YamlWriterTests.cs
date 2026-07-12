// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="Utf8YamlWriter" />. Test methods live in the subject-specific partial files (emission,
/// writer-state, and options); this root holds the shared emission helper.
/// </summary>
[TestClass]
public partial class Utf8YamlWriterTests
{
    /// <summary>A writer callback used to drive emission within a test; copies share the writer state.</summary>
    /// <param name="writer">The writer to emit into.</param>
    private delegate void WriterAction(Utf8YamlWriter writer);

    /// <summary>Writes via the supplied callback and returns the produced UTF-8 text.</summary>
    /// <param name="write">The emission callback.</param>
    /// <returns>The written YAML text.</returns>
    private static string Write(WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);
        write(writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

}
