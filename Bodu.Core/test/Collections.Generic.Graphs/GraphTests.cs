// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Contains unit tests for the <see cref="Graph{T}" /> structural surface.
/// </summary>
[TestClass]
public sealed class GraphTests
{
    /// <summary>
    /// Verifies that adding an edge connects two vertices and records them as neighbors.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddEdge_WhenVerticesNew_ShouldConnectAndRecordNeighbors()
    {
        var sut = new Graph<int>();

        sut.AddEdge(1, 2);

        Assert.IsTrue(sut.ContainsEdge(1, 2));
        Assert.AreEqual(2, sut.VertexCount);
        Assert.AreEqual(1, sut.EdgeCount);
        CollectionAssert.AreEquivalent(new[] { 2 }, sut.Neighbors(1).ToArray());
    }

    /// <summary>
    /// Verifies that adding an existing vertex returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void AddVertex_WhenAlreadyPresent_ShouldReturnFalse()
    {
        var sut = new Graph<int>();

        Assert.IsTrue(sut.AddVertex(1));
        Assert.IsFalse(sut.AddVertex(1));
        Assert.AreEqual(1, sut.VertexCount);
    }

    /// <summary>
    /// Verifies that an undirected edge is navigable in both directions.
    /// </summary>
    [TestMethod]
    public void AddEdge_WhenUndirected_ShouldBeSymmetric()
    {
        var sut = new Graph<int>(GraphKind.Undirected);

        sut.AddEdge(1, 2);

        Assert.IsTrue(sut.ContainsEdge(1, 2));
        Assert.IsTrue(sut.ContainsEdge(2, 1));
        Assert.AreEqual(1, sut.Degree(1));
        Assert.AreEqual(1, sut.Degree(2));
        Assert.AreEqual(1, sut.EdgeCount);
    }

    /// <summary>
    /// Verifies that a directed edge is navigable only in its declared direction.
    /// </summary>
    [TestMethod]
    public void AddEdge_WhenDirected_ShouldBeAsymmetric()
    {
        var sut = new Graph<int>(GraphKind.Directed);

        sut.AddEdge(1, 2);

        Assert.IsTrue(sut.ContainsEdge(1, 2));
        Assert.IsFalse(sut.ContainsEdge(2, 1));
        Assert.AreEqual(1, sut.Degree(1));
        Assert.AreEqual(0, sut.Degree(2));
    }

    /// <summary>
    /// Verifies that re-adding an edge updates its weight without changing the edge count.
    /// </summary>
    [TestMethod]
    public void AddEdge_WhenEdgeExists_ShouldUpdateWeightAndKeepCount()
    {
        var sut = new Graph<int>(GraphKind.Directed);
        sut.AddEdge(1, 2, 2.5);

        sut.AddEdge(1, 2, 9.0);

        Assert.IsTrue(sut.TryGetEdgeWeight(1, 2, out var weight));
        Assert.AreEqual(9.0, weight);
        Assert.AreEqual(1, sut.EdgeCount);
    }

    /// <summary>
    /// Verifies that <see cref="Graph{T}.TryAddEdge(T, T, double)" /> declines to overwrite an existing edge.
    /// </summary>
    [TestMethod]
    public void TryAddEdge_WhenEdgeExists_ShouldReturnFalse()
    {
        var sut = new Graph<int>(GraphKind.Directed);
        sut.AddEdge(1, 2, 1.0);

        Assert.IsFalse(sut.TryAddEdge(1, 2, 5.0));
        Assert.IsTrue(sut.TryGetEdgeWeight(1, 2, out var weight));
        Assert.AreEqual(1.0, weight);
    }

    /// <summary>
    /// Verifies that removing an undirected edge clears both directions and decrements the count.
    /// </summary>
    [TestMethod]
    public void RemoveEdge_WhenUndirected_ShouldClearBothDirections()
    {
        var sut = new Graph<int>(GraphKind.Undirected);
        sut.AddEdge(1, 2);

        Assert.IsTrue(sut.RemoveEdge(1, 2));
        Assert.IsFalse(sut.ContainsEdge(1, 2));
        Assert.IsFalse(sut.ContainsEdge(2, 1));
        Assert.AreEqual(0, sut.EdgeCount);
        Assert.IsFalse(sut.RemoveEdge(1, 2));
    }

    /// <summary>
    /// Verifies that removing a vertex deletes its incoming and outgoing edges in a directed graph.
    /// </summary>
    [TestMethod]
    public void RemoveVertex_WhenDirected_ShouldRemoveIncidentEdges()
    {
        var sut = new Graph<int>(GraphKind.Directed);
        sut.AddEdge(1, 2);
        sut.AddEdge(2, 3);
        sut.AddEdge(1, 3);

        Assert.IsTrue(sut.RemoveVertex(2));

        Assert.AreEqual(2, sut.VertexCount);
        Assert.AreEqual(1, sut.EdgeCount);
        Assert.IsTrue(sut.ContainsEdge(1, 3));
        Assert.IsFalse(sut.ContainsVertex(2));
    }

    /// <summary>
    /// Verifies that a directed self-loop is counted once and reported as an out-edge.
    /// </summary>
    [TestMethod]
    public void AddEdge_WhenDirectedSelfLoop_ShouldCountOnce()
    {
        var sut = new Graph<int>(GraphKind.Directed);

        sut.AddEdge(1, 1);

        Assert.AreEqual(1, sut.EdgeCount);
        Assert.IsTrue(sut.ContainsEdge(1, 1));
        CollectionAssert.AreEquivalent(new[] { 1 }, sut.Neighbors(1).ToArray());
    }

    /// <summary>
    /// Verifies that a negative weight throws <see cref="ArgumentOutOfRangeException" /> naming <c>weight</c>.
    /// </summary>
    [TestMethod]
    public void AddEdge_WhenWeightIsNegative_ShouldThrowForWeight()
    {
        var sut = new Graph<int>();

        Assert.AreEqual(
            "weight",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.AddEdge(1, 2, -0.5)).ParamName);
    }

    /// <summary>
    /// Verifies that operations on an absent vertex throw <see cref="ArgumentException" /> naming <c>vertex</c>.
    /// </summary>
    [TestMethod]
    public void Neighbors_WhenVertexAbsent_ShouldThrowForVertex()
    {
        var sut = new Graph<int>();

        Assert.AreEqual("vertex", Assert.ThrowsExactly<ArgumentException>(() => sut.Neighbors(9).ToArray()).ParamName);
        Assert.AreEqual("vertex", Assert.ThrowsExactly<ArgumentException>(() => sut.Degree(9)).ParamName);
        Assert.AreEqual("vertex", Assert.ThrowsExactly<ArgumentException>(() => sut.WeightedNeighbors(9).ToArray()).ParamName);
    }

    /// <summary>
    /// Verifies that the value-checking members reject a <see langword="null" /> vertex with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Members_WhenVertexIsNull_ShouldThrowForVertex()
    {
        var sut = new Graph<string>();

        Assert.AreEqual("vertex", Assert.ThrowsExactly<ArgumentNullException>(() => sut.AddVertex(null!)).ParamName);
        Assert.AreEqual("vertex", Assert.ThrowsExactly<ArgumentNullException>(() => sut.ContainsVertex(null!)).ParamName);
        Assert.AreEqual("vertex", Assert.ThrowsExactly<ArgumentNullException>(() => sut.RemoveVertex(null!)).ParamName);
        Assert.AreEqual("from", Assert.ThrowsExactly<ArgumentNullException>(() => sut.AddEdge(null!, "b")).ParamName);
    }

    /// <summary>
    /// Verifies that a custom vertex comparer governs vertex identity.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCaseInsensitive_ShouldUnifyVertices()
    {
        var sut = new Graph<string>(GraphKind.Directed, StringComparer.OrdinalIgnoreCase);

        sut.AddEdge("A", "b");

        Assert.IsTrue(sut.ContainsVertex("a"));
        Assert.IsTrue(sut.ContainsEdge("a", "B"));
        Assert.AreEqual(2, sut.VertexCount);
    }

    /// <summary>
    /// Verifies that <see cref="Graph{T}.Clear" /> empties the graph.
    /// </summary>
    [TestMethod]
    public void Clear_WhenPopulated_ShouldEmptyGraph()
    {
        var sut = new Graph<int>();
        sut.AddEdge(1, 2);
        sut.AddEdge(2, 3);

        sut.Clear();

        Assert.AreEqual(0, sut.VertexCount);
        Assert.AreEqual(0, sut.EdgeCount);
    }

    /// <summary>
    /// Verifies that <see cref="Graph{T}.AddEdge(T, T, double)" /> rejects a weight that is not finite and non-negative.
    /// </summary>
    /// <param name="weight">The invalid weight under test.</param>
    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(-1.0)]
    public void AddEdge_WhenWeightIsNotFiniteNonNegative_ShouldThrowForWeight(double weight)
    {
        var sut = new Graph<int>();

        Assert.AreEqual(
            "weight",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.AddEdge(1, 2, weight)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Graph{T}.TryAddEdge(T, T, double)" /> rejects a weight that is not finite and non-negative.
    /// </summary>
    /// <param name="weight">The invalid weight under test.</param>
    [TestMethod]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    [DataRow(-1.0)]
    public void TryAddEdge_WhenWeightIsNotFiniteNonNegative_ShouldThrowForWeight(double weight)
    {
        var sut = new Graph<int>();

        Assert.AreEqual(
            "weight",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => sut.TryAddEdge(1, 2, weight)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Graph{T}.AddEdge(T, T, double)" /> accepts a zero weight.
    /// </summary>
    [TestMethod]
    public void AddEdge_WhenWeightIsZero_ShouldSucceed()
    {
        var sut = new Graph<int>();

        sut.AddEdge(1, 2, 0.0);

        Assert.IsTrue(sut.TryGetEdgeWeight(1, 2, out var weight));
        Assert.AreEqual(0.0, weight);
    }
}
