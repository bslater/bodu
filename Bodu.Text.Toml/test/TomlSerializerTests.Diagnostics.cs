// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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

        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(node);
        });

        Assert.IsTrue(ex.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("Child", ex.Path);
    }

    /// <summary>
    /// Verifies that serializing an object graph with an indirect reference cycle — a parent reachable from itself
    /// through a child's back-reference — throws <see cref="TomlSerializationException" /> identifying the cycle rather
    /// than recursing until the stack is exhausted.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectGraphHasIndirectCycle_ShouldThrowWithCyclePath()
    {
        var parent = new TreeNode();
        var child = new TreeNode();
        parent.Child = child;
        child.Parent = parent;

        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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

        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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

        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
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
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DiagnosticsArrayHolder>("Values = [1, \"x\"]\n");
        });

        Assert.AreEqual("Values[1]", ex.Path);
    }

    /// <summary>
    /// An object whose single member is itself an object, used to exercise a two-level binding path.
    /// </summary>
    private sealed class DiagnosticsOuter
    {
        /// <summary>
        /// Gets or sets the nested object.
        /// </summary>
        /// <returns>The nested object, or <see langword="null" />.</returns>
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
        /// <returns>The value.</returns>
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
        /// <returns>The values, or <see langword="null" />.</returns>
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
        /// <returns>The child, or <see langword="null" />.</returns>
        public TreeNode? Child { get; set; }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        /// <returns>The parent, or <see langword="null" />.</returns>
        public TreeNode? Parent { get; set; }
    }
}
