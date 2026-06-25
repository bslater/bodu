// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8YamlWriterTests.State.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="Utf8YamlWriter" /> enforces its write-state machine, rejecting invalid sequences instead of
/// silently emitting corrupt YAML.
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

    /// <summary>
    /// Verifies that ending a sequence while a mapping is open throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteEndSequence_WhenMappingIsOpen_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteStartMapping();
            writer.WriteEndSequence();
        });
    }

    /// <summary>
    /// Verifies that writing a second root value throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenSecondRootValue_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteString("first");
            writer.WriteString("second");
        });
    }

    /// <summary>
    /// Verifies that writing a string containing an unpaired surrogate throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenUnpairedSurrogate_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8YamlWriter(buffer);
            writer.WriteString("\uD800");
        });
    }

    /// <summary>
    /// Verifies that a non-printable control character is emitted as a hex escape within a double-quoted scalar.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenControlCharacter_ShouldEscapeAsHex()
    {
        var value = "a" + (char)0x01 + "b";
        var yaml = Write((ref Utf8YamlWriter w) =>
        {
            w.WriteStartMapping();
            w.WritePropertyName("c");
            w.WriteString(value);
            w.WriteEndMapping();
        });

        Assert.AreEqual("c: \"a\\x01b\"\n", yaml);
    }
}
