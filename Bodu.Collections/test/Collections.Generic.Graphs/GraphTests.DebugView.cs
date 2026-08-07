// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

public sealed partial class GraphTests
{
    /// <summary>
    /// Verifies that <see cref="GraphDebugView{T}.Items" /> renders one adjacency line per vertex, carrying the
    /// neighbor and its edge weight.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_ShouldRenderOneAdjacencyLinePerVertex()
    {
        var sut = new Graph<int>();
        sut.AddEdge(1, 2);
        sut.AddEdge(1, 3);

        var view = new GraphDebugView<int>(sut);

        Assert.AreEqual(sut.VertexCount, view.Items.Length);
        Assert.Contains("1 -> [2(1), 3(1)]", view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="GraphDebugView{T}.Items" /> renders an isolated vertex with an empty neighbor
    /// list rather than omitting it.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenVertexIsolated_ShouldRenderEmptyNeighborList()
    {
        var sut = new Graph<int>();
        sut.AddVertex(7);

        var view = new GraphDebugView<int>(sut);

        Assert.AreEqual(1, view.Items.Length);
        Assert.AreEqual("7 -> []", view.Items[0]);
    }

    /// <summary>
    /// Verifies that <see cref="GraphDebugView{T}.Items" /> is empty for a graph with no vertices.
    /// </summary>
    [TestMethod]
    public void DebugView_Items_WhenGraphIsEmpty_ShouldBeEmpty()
    {
        var view = new GraphDebugView<int>(new Graph<int>());

        Assert.AreEqual(0, view.Items.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="GraphDebugView{T}" /> with a <see langword="null" /> graph throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenGraphIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new GraphDebugView<int>(null!);
        });

        Assert.AreEqual("graph", ex.ParamName);
    }
}
