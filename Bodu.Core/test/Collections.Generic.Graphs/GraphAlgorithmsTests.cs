// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithmsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Contains unit tests for the <see cref="GraphAlgorithms" /> catalogue.
/// </summary>
[TestClass]
public sealed class GraphAlgorithmsTests
{
    /// <summary>
    /// Provides known-answer shortest-path scenarios.
    /// </summary>
    public static IEnumerable<object[]> ShortestPathScenarios =>
    [
        [new ShortestPathKat("directed weighted shortcut", [("A", "B", 1), ("B", "C", 2), ("A", "C", 5)], true, "A", "C", ["A", "B", "C"], 3)],
        [new ShortestPathKat("directed cheaper detour", [("A", "B", 4), ("A", "C", 1), ("C", "B", 1)], true, "A", "B", ["A", "C", "B"], 2)],
        [new ShortestPathKat("undirected unit weights", [("A", "B", 1), ("B", "C", 1), ("C", "D", 1)], false, "A", "D", ["A", "B", "C", "D"], 3)],
        [new ShortestPathKat("source equals target", [("A", "B", 1)], true, "A", "A", ["A"], 0)],
        [new ShortestPathKat("unreachable target", [("A", "B", 1), ("C", "D", 1)], true, "A", "D", [], double.NaN)],
    ];

    /// <summary>
    /// Provides known-answer topological-sort scenarios.
    /// </summary>
    public static IEnumerable<object[]> TopologicalScenarios =>
    [
        [new TopologicalSortKat("linear chain", [("A", "B"), ("B", "C")], ["A", "B", "C"], true)],
        [new TopologicalSortKat("diamond", [("A", "B"), ("A", "C"), ("B", "D"), ("C", "D")], ["A", "B", "C", "D"], true)],
        [new TopologicalSortKat("with isolated vertex", [("A", "B")], ["A", "B", "C"], true)],
        [new TopologicalSortKat("simple cycle", [("A", "B"), ("B", "C"), ("C", "A")], ["A", "B", "C"], false)],
        [new TopologicalSortKat("self loop", [("A", "A")], ["A"], false)],
    ];

    /// <summary>
    /// Verifies that breadth-first search visits every reachable vertex beginning with the source.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void BreadthFirstSearch_WhenTraversed_ShouldVisitReachableVerticesFromSource()
    {
        var graph = new Graph<int>(GraphKind.Directed);
        graph.AddEdge(1, 2);
        graph.AddEdge(1, 3);
        graph.AddEdge(2, 4);
        graph.AddVertex(9); // unreachable

        var order = GraphAlgorithms.BreadthFirstSearch(graph, 1).ToList();

        Assert.AreEqual(1, order[0]);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, order);
    }

    /// <summary>
    /// Verifies that depth-first search visits exactly the vertices reachable from the source.
    /// </summary>
    [TestMethod]
    public void DepthFirstSearch_WhenTraversed_ShouldVisitReachableVertices()
    {
        var graph = new Graph<int>(GraphKind.Directed);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddVertex(8); // unreachable

        var visited = GraphAlgorithms.DepthFirstSearch(graph, 1).ToList();

        Assert.AreEqual(1, visited[0]);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, visited);
    }

    /// <summary>
    /// Verifies that traversal rejects a <see langword="null" /> graph and an absent source.
    /// </summary>
    [TestMethod]
    public void BreadthFirstSearch_WhenArgumentsInvalid_ShouldThrow()
    {
        var graph = new Graph<int>();

        Assert.AreEqual("graph", Assert.ThrowsExactly<ArgumentNullException>(() => GraphAlgorithms.BreadthFirstSearch<int>(null!, 1).ToArray()).ParamName);
        Assert.AreEqual("source", Assert.ThrowsExactly<ArgumentException>(() => GraphAlgorithms.BreadthFirstSearch(graph, 7).ToArray()).ParamName);
    }

    /// <summary>
    /// Verifies that shortest-path queries return the expected path and distance.
    /// </summary>
    /// <param name="kat">The scenario supplying the graph, endpoints, and expected result.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ShortestPathScenarios),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ShortestPath_WhenQueried_ShouldReturnExpectedPath(ShortestPathKat kat)
    {
        var graph = new Graph<string>(kat.Directed ? GraphKind.Directed : GraphKind.Undirected);
        foreach (var (from, to, weight) in kat.Edges)
            graph.AddEdge(from, to, weight);

        var path = GraphAlgorithms.ShortestPath(graph, kat.Source, kat.Target);

        CollectionAssert.AreEqual(kat.ExpectedPath, path.ToArray());

        var lengths = GraphAlgorithms.ShortestPathLengths(graph, kat.Source);
        if (kat.ExpectedPath.Length == 0)
        {
            Assert.IsFalse(lengths.ContainsKey(kat.Target));
        }
        else
        {
            Assert.AreEqual(kat.ExpectedDistance, lengths[kat.Target]);
        }
    }

    /// <summary>
    /// Verifies that <see cref="GraphAlgorithms.TryShortestPath{T}(IReadOnlyWeightedGraph{T, double}, T, T)" /> reports
    /// reachability, distance, and path consistently with the scenario.
    /// </summary>
    /// <param name="kat">The scenario supplying the graph, endpoints, and expected result.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(ShortestPathScenarios),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryShortestPath_WhenQueried_ShouldReportFoundDistanceAndPath(ShortestPathKat kat)
    {
        var graph = new Graph<string>(kat.Directed ? GraphKind.Directed : GraphKind.Undirected);
        foreach (var (from, to, weight) in kat.Edges)
            graph.AddEdge(from, to, weight);

        var result = GraphAlgorithms.TryShortestPath(graph, kat.Source, kat.Target);

        if (kat.ExpectedPath.Length == 0)
        {
            Assert.IsFalse(result.Found);
            Assert.AreEqual(double.PositiveInfinity, result.Distance);
            Assert.IsEmpty(result.Path);
        }
        else
        {
            Assert.IsTrue(result.Found);
            Assert.AreEqual(kat.ExpectedDistance, result.Distance);
            CollectionAssert.AreEqual(kat.ExpectedPath, result.Path.ToArray());
        }
    }

    /// <summary>
    /// Verifies that the traversal algorithms accept a graph through the <see cref="IReadOnlyGraph{TVertex}" />
    /// abstraction rather than the concrete <see cref="Graph{T}" />.
    /// </summary>
    [TestMethod]
    public void BreadthFirstSearch_WhenGraphPassedAsReadOnlyInterface_ShouldTraverse()
    {
        var graph = new Graph<int>(GraphKind.Directed);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);

        IReadOnlyGraph<int> readOnly = graph;
        var visited = GraphAlgorithms.BreadthFirstSearch(readOnly, 1).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, visited);
    }

    /// <summary>
    /// Verifies that topological sorting respects edge direction on acyclic graphs and fails on cyclic graphs.
    /// </summary>
    /// <param name="kat">The scenario supplying the directed graph and its acyclicity.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(TopologicalScenarios),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TopologicalSort_WhenApplied_ShouldRespectEdgesOrReportCycle(TopologicalSortKat kat)
    {
        var graph = new Graph<string>(GraphKind.Directed);
        foreach (var vertex in kat.Vertices)
            graph.AddVertex(vertex);
        foreach (var (from, to) in kat.Edges)
            graph.AddEdge(from, to);

        if (kat.IsAcyclic)
        {
            Assert.IsTrue(GraphAlgorithms.TryTopologicalSort(graph, out var order));
            Assert.HasCount(graph.VertexCount, order);

            var position = order.Select((v, i) => (v, i)).ToDictionary(p => p.v, p => p.i);
            foreach (var (from, to) in kat.Edges)
                Assert.IsLessThan(position[to], position[from], $"Edge {from}->{to} violates ordering");
        }
        else
        {
            Assert.IsFalse(GraphAlgorithms.TryTopologicalSort(graph, out _));
            Assert.ThrowsExactly<InvalidOperationException>(() => GraphAlgorithms.TopologicalSort(graph));
        }
    }

    /// <summary>
    /// Verifies that topological sorting an undirected graph throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void TopologicalSort_WhenGraphUndirected_ShouldThrowInvalidOperationException()
    {
        var graph = new Graph<int>(GraphKind.Undirected);
        graph.AddEdge(1, 2);

        Assert.ThrowsExactly<InvalidOperationException>(() => GraphAlgorithms.TopologicalSort(graph));
    }

    /// <summary>
    /// Verifies that connected components group mutually reachable vertices and separate disjoint ones.
    /// </summary>
    [TestMethod]
    public void ConnectedComponents_WhenGraphHasTwoClusters_ShouldGroupEachCluster()
    {
        var graph = new Graph<int>(GraphKind.Undirected);
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(10, 11);
        graph.AddVertex(20); // isolated

        var components = GraphAlgorithms.ConnectedComponents(graph)
            .Select(c => c.OrderBy(x => x).ToArray())
            .OrderBy(c => c[0])
            .ToArray();

        Assert.HasCount(3, components);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, components[0]);
        CollectionAssert.AreEqual(new[] { 10, 11 }, components[1]);
        CollectionAssert.AreEqual(new[] { 20 }, components[2]);
    }

    /// <summary>
    /// Verifies that connected-component analysis rejects a <see langword="null" /> graph.
    /// </summary>
    [TestMethod]
    public void ConnectedComponents_WhenGraphIsNull_ShouldThrowForGraph()
    {
        Assert.AreEqual(
            "graph",
            Assert.ThrowsExactly<ArgumentNullException>(() => GraphAlgorithms.ConnectedComponents<int>(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a deep chain can be traversed iteratively without overflowing the stack.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void DepthFirstSearch_WhenChainIsVeryLong_ShouldTraverseWithoutStackOverflow()
    {
        var graph = new Graph<int>(GraphKind.Directed);
        const int n = 100_000;
        for (var i = 1; i < n; i++)
            graph.AddEdge(i - 1, i);

        Assert.HasCount(n, GraphAlgorithms.DepthFirstSearch(graph, 0));
    }
}
