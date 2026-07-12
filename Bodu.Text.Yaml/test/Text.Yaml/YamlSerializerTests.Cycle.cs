// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Cycle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that serializing an object graph containing a reference cycle is rejected with
/// <see cref="YamlSerializationException" /> that identifies the cycle and the member path, rather than recursing to
/// the depth ceiling or overflowing the stack. YAML — like the other Bodu text serializers — writes by value and does
/// not preserve reference identity, so a cycle cannot be represented.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that serializing a directly self-referential object throws <see cref="YamlSerializationException" />
    /// whose message identifies the cycle and whose path names the offending member.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectGraphHasCycle_ShouldThrowWithCyclePath()
    {
        var node = new CycleNode { Id = 1 };
        node.Next = node;

        var ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Serialize(node);
        });

        Assert.IsTrue(ex.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
        Assert.IsNotNull(ex.Path);
        Assert.IsTrue(ex.Path!.Contains("Next", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that serializing an indirect reference cycle — a parent reachable from itself through a child's
    /// back-reference — throws <see cref="YamlSerializationException" /> identifying the cycle.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectGraphHasIndirectCycle_ShouldThrowWithCyclePath()
    {
        var parent = new CycleNode { Id = 1 };
        var child = new CycleNode { Id = 2, Next = parent };
        parent.Next = child;

        var ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Serialize(parent);
        });

        Assert.IsTrue(ex.Message.Contains("cycle", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A self-referential node used to build reference cycles.
    /// </summary>
    private sealed class CycleNode
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the next node, which may reference back into the graph.
        /// </summary>
        /// <value>The next node, or <see langword="null" />.</value>
        public CycleNode? Next { get; set; }
    }
}
