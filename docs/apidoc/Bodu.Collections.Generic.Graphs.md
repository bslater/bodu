---
uid: Bodu.Collections.Generic.Graphs
---

![Bodu.Collections.Generic.Graphs](~/images/hero-collections.svg)

## Purpose

**Bodu.Collections.Generic.Graphs** is a small, self-contained graph toolkit in the `Bodu.Collections` package (which depends on `Bodu.Core`): a vertex-and-edge container, a static catalogue of classic graph algorithms, and two union-find (disjoint-set) structures. It is built for in-memory adjacency-list graphs of arbitrary vertex types — integers, strings, or your own value/reference types — with optional non-negative edge weights.

The container, <xref:Bodu.Collections.Generic.Graphs.Graph`1>, stores an adjacency map and is fixed as either directed or undirected at construction via <xref:Bodu.Collections.Generic.Graphs.GraphKind>. The algorithms in <xref:Bodu.Collections.Generic.Graphs.GraphAlgorithms> — breadth-first and depth-first traversal, Dijkstra shortest path, Kahn topological sort, and connected components — are decoupled from the container: they accept the read-only interfaces <xref:Bodu.Collections.Generic.Graphs.IReadOnlyGraph`1> and <xref:Bodu.Collections.Generic.Graphs.IReadOnlyWeightedGraph`2>, so they run over any conforming representation. The algorithms reuse the library's own primitives — <xref:Bodu.Collections.Generic.Deque`1> for breadth-first frontiers and Kahn's ready queue, and <xref:Bodu.Collections.Generic.IndexedPriorityQueue`2> for Dijkstra relaxation — and evaluate iteratively so they do not overflow the stack on deep graphs.

## Static documentation

- **[Bodu.Collections introduction](~/docs/collections/index.md)** — the headline collections and where the graph types sit among them.
- **[Graphs and graph algorithms](~/guides/core/graphs.md)** — build a graph, run a traversal or shortest path, sort topologically, find components, and use union-find.

## Key types

**Graph container**

- <xref:Bodu.Collections.Generic.Graphs.Graph`1> — the adjacency-list graph. Construct with a <xref:Bodu.Collections.Generic.Graphs.GraphKind> and an optional vertex comparer; mutate with `AddVertex` / `RemoveVertex` / `AddEdge` (unweighted or weighted) / `TryAddEdge` / `RemoveEdge` / `Clear`; query with `Vertices`, `Neighbors`, `WeightedNeighbors`, `ContainsVertex`, `ContainsEdge`, `TryGetEdgeWeight`, `Degree`, `VertexCount`, `EdgeCount`, and `IsDirected`. Implements <xref:Bodu.Collections.Generic.Graphs.IReadOnlyWeightedGraph`2> with `double` weights.
- <xref:Bodu.Collections.Generic.Graphs.GraphKind> — `Undirected` (the default; an edge makes each vertex a neighbor of the other) or `Directed` (an edge does not imply its reverse).

**Read-only views (algorithm inputs)**

- <xref:Bodu.Collections.Generic.Graphs.IReadOnlyGraph`1> — the unweighted topology surface (`IsDirected`, `VertexCount`, `EdgeCount`, `Comparer`, `Vertices`, `ContainsVertex`, `Neighbors`, `Degree`) that traversal, topological sort, and connectivity accept.
- <xref:Bodu.Collections.Generic.Graphs.IReadOnlyWeightedGraph`2> — extends the above with `WeightedNeighbors`, `ContainsEdge`, and `TryGetEdgeWeight`; the input to the shortest-path algorithms.

**Algorithms**

- <xref:Bodu.Collections.Generic.Graphs.GraphAlgorithms> — a static class with `BreadthFirstSearch` / `DepthFirstSearch` (lazy reachability sequences), `ShortestPath` / `TryShortestPath` / `ShortestPathLengths` (Dijkstra over non-negative weights), `TopologicalSort` / `TryTopologicalSort` (Kahn's algorithm, directed graphs only), and `ConnectedComponents` (weakly connected components via union-find).
- <xref:Bodu.Collections.Generic.Graphs.ShortestPathResult`1> — the readonly record struct returned by `TryShortestPath`: `Found`, `Distance`, and `Path`.

**Union-find (disjoint set)**

- <xref:Bodu.Collections.Generic.Graphs.DisjointSet> — a fixed-size, integer-indexed union-find over the range `[0, count)`: `Union`, `Find`, `AreConnected`, `SizeOf`, `Reset`, `Count`, `SetCount`. Path-halving compression with union by size, amortized near-constant per operation.
- <xref:Bodu.Collections.Generic.Graphs.DisjointSet`1> — the element-keyed equivalent for arbitrary non-nullable keys: `MakeSet` / `Add`, `Contains`, `Union`, `Find` / `TryFind`, `AreConnected`, `SizeOf`, `Clear`, with an optional <xref:System.Collections.Generic.IEqualityComparer`1>.

## Example

```csharp
using Bodu.Collections.Generic.Graphs;

// Build a small weighted directed graph; referenced vertices are created on demand.
var graph = new Graph<string>(GraphKind.Directed);
graph.AddEdge("A", "B", 1.0);
graph.AddEdge("B", "C", 2.0);
graph.AddEdge("A", "C", 5.0);

// Breadth-first reachability from A.
var reachable = GraphAlgorithms.BreadthFirstSearch(graph, "A").ToList();

// Cheapest route A -> B -> C (total 3) beats the direct A -> C (5).
ShortestPathResult<string> result = GraphAlgorithms.TryShortestPath(graph, "A", "C");
// result.Found is true, result.Distance is 3.0, result.Path is [A, B, C]
```

## Notes

- **Directedness is fixed at construction.** Choose <xref:Bodu.Collections.Generic.Graphs.GraphKind> when you create the graph. In an undirected graph each edge is stored symmetrically, so one `AddEdge` makes both vertices mutual neighbors and `EdgeCount` counts the connection once.
- **Weights are finite and non-negative.** Edges default to weight `1.0`; `AddEdge` and `TryAddEdge` reject `NaN`, infinity, and negative weights with <xref:System.ArgumentOutOfRangeException>. Algorithms that ignore weight treat the graph as unweighted. Dijkstra requires non-negative weights — there is no Bellman-Ford for negative edges.
- **Vertex identity follows the comparer.** Pass an <xref:System.Collections.Generic.IEqualityComparer`1> at construction (for example, `StringComparer.OrdinalIgnoreCase`) to control how vertices are deduplicated; the same comparer flows into the algorithms' internal sets.
- **Algorithms take the interfaces, not the concrete graph.** Because they accept <xref:Bodu.Collections.Generic.Graphs.IReadOnlyGraph`1> / <xref:Bodu.Collections.Generic.Graphs.IReadOnlyWeightedGraph`2>, you can run them over a custom graph representation as well as the built-in <xref:Bodu.Collections.Generic.Graphs.Graph`1>.
- **Topological sort is directed-only.** `TopologicalSort` / `TryTopologicalSort` throw <xref:System.InvalidOperationException> on an undirected graph; `TopologicalSort` additionally throws on a cycle, while `TryTopologicalSort` returns `false` and an empty list.
- **Connected components are weakly connected.** `ConnectedComponents` treats every edge as undirected and partitions with a <xref:Bodu.Collections.Generic.Graphs.DisjointSet`1>; for a directed graph the result is the weakly connected components.
- **Not thread-safe.** A <xref:Bodu.Collections.Generic.Graphs.Graph`1> is not safe for concurrent mutation; coordinate writes externally.
- **See also:** the [Bodu.Collections introduction](~/docs/collections/index.md) and the [graphs guide](~/guides/core/graphs.md).
