// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the diagnostic context carried by <see cref="TomlSerializationException" />: an object cycle is reported as
/// a cycle rather than as an opaque depth-exceeded failure, and a binding failure carries the member path and source
/// offset of the offending value.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that serializing an object graph that contains a reference cycle throws
    /// <see cref="TomlSerializationException" /> whose message identifies the cycle and whose path names the member that
    /// closed it, rather than surfacing as a maximum-depth failure.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectGraphHasCycle_ShouldThrowWithCyclePath()
    {
        var node = new RecursiveModel();
        node.Child = node;

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(node);
        });

        Assert.IsTrue(ex.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("Child", ex.Path);
    }

    /// <summary>
    /// Verifies that serializing an object graph with an indirect reference cycle - a parent reachable from itself
    /// through a child's back-reference - throws <see cref="TomlSerializationException" /> identifying the cycle rather
    /// than recursing until the stack is exhausted.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectGraphHasIndirectCycle_ShouldThrowWithCyclePath()
    {
        var parent = new TreeNode();
        var child = new TreeNode();
        parent.Child = child;
        child.Parent = parent;

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(parent);
        });

        Assert.IsTrue(ex.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("Child.Parent", ex.Path);
    }

    /// <summary>
    /// Verifies that a reference cycle through a collection element is reported as a cycle whose path combines the
    /// containing dictionary key and the array index, confirming the cooperative path is built from the serializer state
    /// across the dictionary and collection converters.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCollectionContainsItself_ShouldThrowWithIndexedCyclePath()
    {
        var items = new List<object>();
        items.Add(items);
        var root = new Dictionary<string, object> { ["items"] = items };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(root);
        });

        Assert.IsTrue(ex.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("items[0]", ex.Path);
    }

    /// <summary>
    /// Verifies that exceeding the configured maximum depth reports the dotted path to the level at which the limit was
    /// reached, confirming the cooperative depth failure captures the path from the serializer state.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenGraphExceedsMaxDepth_ShouldReportPathToFailingLevel()
    {
        var options = new TomlSerializerOptions { MaxDepth = 3 };
        var deep = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel { Child = new RecursiveModel() } } } };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(deep, options);
        });

        Assert.AreEqual("Child.Child.Child", ex.Path);
    }

    /// <summary>
    /// Verifies that a binding failure on a nested member reports the dotted path from the document root to the
    /// offending member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedMemberTypeMismatches_ShouldReportPropertyPath()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DiagnosticsOuter>("[Inner]\nValue = \"x\"\n");
        });

        Assert.AreEqual("Inner.Value", ex.Path);
    }

    /// <summary>
    /// Verifies that a binding failure on a nested member reports the byte offset of the offending value in the source.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedMemberTypeMismatches_ShouldReportOffset()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DiagnosticsOuter>("[Inner]\nValue = \"x\"\n");
        });

        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that a binding failure on a nested member reports the 1-based line and byte column of the offending
    /// value in the source.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNestedMemberTypeMismatches_ShouldReportLineAndColumn()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DiagnosticsOuter>("[Inner]\nValue = \"x\"\n");
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual(9, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a binding failure on a nested element of an array reports a path that includes the array index.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenArrayElementTypeMismatches_ShouldReportIndexedPath()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DiagnosticsArrayHolder>("Values = [1, \"x\"]\n");
        });

        Assert.AreEqual("Values[1]", ex.Path);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlSerializationException" /> a member's converter throws for a value it rejects
    /// reaches the caller with the line and the path of that value, and still carries the converter's message.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConverterRejectsValue_ShouldReportItsPosition()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<OkCodesModel>("k1 = 'asd'\nk2 = 'ok'\nk3 = 'invalid'\nk4 = 'ok'");
        });

        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual("k3", ex.Path);
        Assert.IsTrue(ex.Message.Contains(OkCodeConverter.RejectionMessage, StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a table, or an array of tables, given for a string member is reported with the member's path and
    /// at the line of the header that opens it, not at the start of the document.
    /// </summary>
    /// <param name="toml">The document, whose second line opens a table or an array of tables for the member.</param>
    [TestMethod]
    [DataRow("X = 1\n[A]\n", DisplayName = "table")]
    [DataRow("X = 1\n[[A]]\n", DisplayName = "array of tables")]
    public void Deserialize_WhenTableIsGivenForStringMember_ShouldReportHeaderPosition(string toml)
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<HeaderTargetModel>(toml);
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.AreEqual("A", ex.Path);
    }

    /// <summary>
    /// Verifies that a string given for an <see cref="int" /> member of a nested table is reported with a path that
    /// names both the table and the member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringIsReadIntoInt32Member_ShouldReportPath()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ServerSectionHostModel>("[server]\npath = \"/my/path\"\nport = \"bad\"\n");
        });

        Assert.AreEqual("server.port", ex.Path);
    }

    /// <summary>
    /// Verifies that a string given for an <see cref="int" /> member of a nested table is reported at the string: its
    /// line and its column.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringIsReadIntoInt32Member_ShouldReportValuePosition()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<ServerSectionHostModel>("[server]\npath = \"/my/path\"\nport = \"bad\"\n");
        });

        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(8, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a table given for an array member is reported with the member's path and at the line of the
    /// header that opens the table, rather than at the start of the document or at no position.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenArrayMemberIsGivenTable_ShouldReportTablePosition()
    {
        const string toml = "\n[package]\nname = \"foo\"\nversion = \"0.1.0\"\nedition = \"2021\"\n[[bench.foo]]\n";

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<PackageManifestModel>(toml);
        });

        Assert.AreEqual("bench", ex.Path);
        Assert.AreEqual(6, ex.LineNumber);
    }

    /// <summary>
    /// A model with a string member and two code members read by <see cref="OkCodeConverter" />.
    /// </summary>
    private sealed class OkCodesModel
    {
        /// <summary>
        /// Gets or sets the string member.
        /// </summary>
        /// <value>The string.</value>
        [PropertyName("k1")]
        public string K1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the first code.
        /// </summary>
        /// <value>The code.</value>
        [PropertyName("k2")]
        public OkCode K2 { get; set; }

        /// <summary>
        /// Gets or sets the second code.
        /// </summary>
        /// <value>The code.</value>
        [PropertyName("k3")]
        public OkCode K3 { get; set; }
    }

    /// <summary>
    /// A code whose only valid spelling is <c>ok</c>, read and written by <see cref="OkCodeConverter" />.
    /// </summary>
    [Converter(typeof(OkCodeConverter))]
    private enum OkCode
    {
        /// <summary>
        /// The only code.
        /// </summary>
        Ok,
    }

    /// <summary>
    /// A converter that reads <c>ok</c> as <see cref="OkCode.Ok" /> and rejects every other string with
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    private sealed class OkCodeConverter
        : TomlConverter<OkCode>
    {
        /// <summary>
        /// The message of the exception the converter throws for a string other than <c>ok</c>.
        /// </summary>
        public const string RejectionMessage = "The code is not 'ok'.";

        /// <inheritdoc />
        public override OkCode Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options) =>
            reader.GetString() == "ok" ? OkCode.Ok : throw new TomlSerializationException(RejectionMessage);

        /// <inheritdoc />
        public override void Write(Utf8TomlWriter writer, OkCode value, TomlSerializerOptions options) =>
            writer.WriteString("ok");
    }

    /// <summary>
    /// A model with an integer member and a string member that a document gives a table.
    /// </summary>
    private sealed class HeaderTargetModel
    {
        /// <summary>
        /// Gets or sets the integer member.
        /// </summary>
        /// <value>The integer.</value>
        public long X { get; set; }

        /// <summary>
        /// Gets or sets the string member.
        /// </summary>
        /// <value>The string, or <see langword="null" />.</value>
        public string? A { get; set; }
    }

    /// <summary>
    /// A model whose only member is a server table.
    /// </summary>
    private sealed class ServerSectionHostModel
    {
        /// <summary>
        /// Gets or sets the server table.
        /// </summary>
        /// <value>The server table, or <see langword="null" />.</value>
        [PropertyName("server")]
        public ServerSectionModel? Server { get; set; }
    }

    /// <summary>
    /// A server table with a string member and an integer member.
    /// </summary>
    private sealed class ServerSectionModel
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [PropertyName("path")]
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        [PropertyName("port")]
        public int Port { get; set; }
    }

    /// <summary>
    /// A package manifest with a package table and an array of bench tables, both of types with no members.
    /// </summary>
    private sealed class PackageManifestModel
    {
        /// <summary>
        /// Gets or sets the package table.
        /// </summary>
        /// <value>The package table, or <see langword="null" />.</value>
        [PropertyName("package")]
        public EmptyPackageModel? Package { get; set; }

        /// <summary>
        /// Gets or sets the bench tables.
        /// </summary>
        /// <value>The bench tables, or <see langword="null" />.</value>
        [PropertyName("bench")]
        public List<EmptyBenchModel>? Bench { get; set; }
    }

    /// <summary>
    /// A package table with no members, so every key in it is unmapped.
    /// </summary>
    private sealed class EmptyPackageModel
    {
    }

    /// <summary>
    /// A bench table with no members.
    /// </summary>
    private sealed class EmptyBenchModel
    {
    }

    /// <summary>
    /// An object whose single member is itself an object, used to exercise a two-level binding path.
    /// </summary>
    private sealed class DiagnosticsOuter
    {
        /// <summary>
        /// Gets or sets the nested object.
        /// </summary>
        /// <value>The nested object, or <see langword="null" />.</value>
        public DiagnosticsInner? Inner { get; set; }
    }

    /// <summary>
    /// A nested object with a single integer member, used as the leaf of a binding path.
    /// </summary>
    private sealed class DiagnosticsInner
    {
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// An object whose single member is an integer list, used to exercise an indexed binding path.
    /// </summary>
    private sealed class DiagnosticsArrayHolder
    {
        /// <summary>
        /// Gets or sets the integer values.
        /// </summary>
        /// <value>The values, or <see langword="null" />.</value>
        public List<int>? Values { get; set; }
    }

    /// <summary>
    /// A node with both a child and a parent back-reference, used to build an indirect reference cycle.
    /// </summary>
    private sealed class TreeNode
    {
        /// <summary>
        /// Gets or sets the child node.
        /// </summary>
        /// <value>The child, or <see langword="null" />.</value>
        public TreeNode? Child { get; set; }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        /// <value>The parent, or <see langword="null" />.</value>
        public TreeNode? Parent { get; set; }
    }
}
