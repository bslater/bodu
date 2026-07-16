// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.WriteStartSequence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter.WriteStartSequence" /> emits the expected output and enforces its contract.
/// </summary>
public partial class Utf8YamlWriterTests
{
    /// <summary>Verifies that a sequence value is emitted with dash entries.</summary>
    [TestMethod]
    public void WriteStartSequence_WhenSequence_ShouldEmitDashes()
    {
        string yaml = Write((Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("items");
            w.WriteStartSequence();
            w.WriteString("a");
            w.WriteInteger(2);
            w.WriteEndSequence();
            w.WriteEndMapping();
        });

        Assert.AreEqual("items:\n  - a\n  - 2\n", yaml);
    }

}
