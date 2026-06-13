// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Documents.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the read-only document object model bridge of <see cref="TomlSerializer" />: <see cref="TomlElement" />
/// and <see cref="TomlDocument" /> as members and document roots, the caller-owned disposal contract of a
/// deserialized document, and the guards on default and disposed elements.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a <see cref="TomlElement" /> member deserializes from each TOML value kind with the matching
    /// <see cref="TomlValueKind" /> and re-serializes to the identical text.
    /// </summary>
    /// <param name="toml">The TOML document line carrying the element value.</param>
    /// <param name="kind">The expected value kind of the deserialized element.</param>
    [TestMethod]
    [DataRow("Value = \"x\"\n", TomlValueKind.String, DisplayName = "string")]
    [DataRow("Value = 5\n", TomlValueKind.Integer, DisplayName = "integer")]
    [DataRow("Value = 1.5\n", TomlValueKind.Float, DisplayName = "float")]
    [DataRow("Value = true\n", TomlValueKind.Boolean, DisplayName = "boolean")]
    [DataRow("Value = [1, 2, 3]\n", TomlValueKind.Array, DisplayName = "array")]
    [DataRow("Value = { A = 1 }\n", TomlValueKind.Table, DisplayName = "table")]
    public void SerializeDeserialize_WhenTomlElementMember_ShouldPreserveKindAndRoundTrip(string toml, TomlValueKind kind)
    {
        TomlElement element = TomlSerializer.Deserialize<ValueModel<TomlElement>>(toml).Value;

        Assert.AreEqual(kind, element.ValueKind);

        var text = TomlSerializer.Serialize(new ValueModel<TomlElement> { Value = element });
        TomlElement again = TomlSerializer.Deserialize<ValueModel<TomlElement>>(text).Value;
        Assert.AreEqual(kind, again.ValueKind);
    }

    /// <summary>
    /// Verifies that a deserialized <see cref="TomlElement" /> remains readable after deserialization completes,
    /// because the serializer's internal backing document is never disposed.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTomlElementMember_ShouldRemainReadableAfterwards()
    {
        TomlElement element = TomlSerializer.Deserialize<ValueModel<TomlElement>>("Value = [1, 2, 3]\n").Value;

        Assert.AreEqual(3, element.GetArrayLength());
        Assert.AreEqual(2L, element[1].GetInt64());
    }

    /// <summary>
    /// Verifies that an element obtained from a parsed document serializes as a member, re-emitting the value it views.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTomlElementFromParsedDocument_ShouldEmitViewedValue()
    {
        using var document = TomlDocument.Parse("Inner = 5\n");
        TomlElement element = document.RootElement.GetProperty("Inner");

        var text = TomlSerializer.Serialize(new ValueModel<TomlElement> { Value = element });

        Assert.AreEqual("Value = 5\n", text);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlDocument" /> deserializes at the document root, is owned by the caller, and
    /// re-serializes to the identical text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTomlDocumentRoot_ShouldRoundTripText()
    {
        const string toml = "A = 1\nB = \"x\"\n";

        using TomlDocument document = TomlSerializer.Deserialize<TomlDocument>(toml);

        Assert.AreEqual(TomlValueKind.Table, document.RootElement.ValueKind);
        Assert.AreEqual(toml, TomlSerializer.Serialize(document));
    }

    /// <summary>
    /// Verifies that a <see cref="TomlElement" /> whose kind is a table deserializes at the document root and
    /// re-serializes to the identical text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTomlElementRoot_ShouldRoundTripText()
    {
        const string toml = "A = 1\n";

        TomlElement element = TomlSerializer.Deserialize<TomlElement>(toml);

        Assert.AreEqual(TomlValueKind.Table, element.ValueKind);
        Assert.AreEqual(toml, TomlSerializer.Serialize(element));
    }

    /// <summary>
    /// Verifies that serializing a scalar <see cref="TomlElement" /> at the document root throws
    /// <see cref="InvalidOperationException" /> from the writer's root state machine, because a TOML document root must
    /// be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenScalarTomlElementAtRoot_ShouldThrowInvalidOperationException()
    {
        TomlElement scalar = TomlSerializer.Deserialize<ValueModel<TomlElement>>("Value = 5\n").Value;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Serialize(scalar);
        });
    }

    /// <summary>
    /// Verifies that serializing a member whose <see cref="TomlElement" /> is the default value throws
    /// <see cref="InvalidOperationException" />, because the element belongs to no document.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultTomlElementMember_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Serialize(new ValueModel<TomlElement> { Value = default });
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.WriteTo" /> on an element of a disposed document throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTomlElementFromDisposedDocument_ShouldThrowObjectDisposedException()
    {
        var document = TomlDocument.Parse("A = 1\n");
        TomlElement element = document.RootElement;
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = TomlSerializer.Serialize(element);
        });
    }

    /// <summary>
    /// Verifies that serializing a <see langword="null" /> <see cref="TomlDocument" /> at the root throws
    /// <see cref="TomlSerializationException" />, because TOML has no null form.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenNullTomlDocument_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize<TomlDocument>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlElement.WriteTo" /> re-emits every value kind through a standalone writer,
    /// producing the writer's canonical text — a nested table surfaces as a <c>[T]</c> header section rather than the
    /// inline form it was parsed from.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenEveryValueKind_ShouldReEmitCanonicalText()
    {
        const string toml = "S = \"x\"\nI = 5\nF = 1.5\nB = true\nOdt = 2026-06-10T09:30:00Z\nLdt = 2026-06-10T09:30:00\nLd = 2026-06-10\nLt = 09:30:00\nA = [1, 2]\nT = { N = 1 }\n";
        const string expected = "S = \"x\"\nI = 5\nF = 1.5\nB = true\nOdt = 2026-06-10T09:30:00Z\nLdt = 2026-06-10T09:30:00\nLd = 2026-06-10\nLt = 09:30:00\nA = [1, 2]\n\n[T]\nN = 1\n";
        using var document = TomlDocument.Parse(toml);

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8TomlWriter(buffer);
        document.RootElement.WriteTo(writer);

        Assert.AreEqual(expected, System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
