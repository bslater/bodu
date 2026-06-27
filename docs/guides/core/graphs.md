---
title: Graphs and graph algorithms
---

# Graphs and graph algorithms

The `Bodu.Collections.Generic.Graphs` namespace is a compact graph toolkit: an adjacency-list container, `Graph<T>`; a static catalogue of classic algorithms, `GraphAlgorithms`; and two union-find structures, `DisjointSet` and `DisjointSet<T>`. The container holds vertices of any non-nullable type connected by optionally weighted edges, and the algorithms — breadth-first and depth-first traversal, Dijkstra shortest path, Kahn topological sort, and connected components — run over read-only interfaces, so they are decoupled from how the graph is stored.

Edge directedness is fixed at construction by `GraphKind`, weights are finite and non-negative `double`s defaulting to `1.0`, and vertex identity follows an optional `IEqualityComparer<T>`. The algorithms reuse the library's own `Deque<T>` and `IndexedPriorityQueue<TElement, TPriority>` primitives and evaluate iteratively, so they do not overflow the stack on deep graphs. See the full API surface at <xref:Bodu.Collections.Generic.Graphs>.

## Pattern 1 — Build a graph and enumerate neighbors

`Graph<T>` is directed or undirected for its whole life, chosen with `GraphKind` at construction. Vertices referenced by `AddEdge` are created on demand; you only call `AddVertex` for an isolated vertex with no edges.

```csharp
using Bodu.Collections.Generic.Graphs;

// Undirected: one AddEdge makes both vertices mutual neighbors.
var undirected = new Graph<int>(GraphKind.Undirected);
undirected.AddEdge(1, 2);
undirected.AddEdge(2, 3);
undirected.AddVertex(20);            // isolated vertex, no edges

foreach (int neighbor in undirected.Neighbors(2))
    Console.WriteLine(neighbor);     // 1, 3 (2 is adjacent to both)

Console.WriteLine(undirected.Degree(2));       // 2
Console.WriteLine(undirected.VertexCount);     // 4  -> 1, 2, 3, 20
Console.WriteLine(undirected.EdgeCount);       // 2  -> each undirected edge counts once
```

```csharp
// Directed: an edge does not imply its reverse.
var directed = new Graph<string>(GraphKind.Directed);
directed.AddEdge("A", "B");
directed.AddEdge("A", "C");

Console.WriteLine(directed.ContainsEdge("A", "B"));   // true
Console.WriteLine(directed.ContainsEdge("B", "A"));   // false (directed)
Console.WriteLine(directed.IsDirected);               // true
```

Weighted edges carry a finite, non-negative `double`. `AddEdge` adds or updates; `TryAddEdge` refuses to overwrite an existing edge.

```csharp
var weighted = new Graph<string>(GraphKind.Directed);
weighted.AddEdge("A", "B", 1.5);
weighted.AddEdge("B", "C", 2.0);

if (weighted.TryGetEdgeWeight("A", "B", out double w))
    Console.WriteLine(w);            // 1.5

foreach ((string neighbor, double weight) in weighted.WeightedNeighbors("A"))
    Console.WriteLine($"{neighbor} @ {weight}");   // B @ 1.5

bool added = weighted.TryAddEdge("A", "B", 99.0);  // false: edge already exists
```

Use a comparer to control vertex identity — for example, case-insensitive string vertices:

```csharp
var graph = new Graph<string>(GraphKind.Undirected, StringComparer.OrdinalIgnoreCase);
graph.AddEdge("Node", "Other");
Console.WriteLine(graph.ContainsVertex("NODE"));   // true
```

## Pattern 2 — Traverse with BFS and DFS

`GraphAlgorithms.BreadthFirstSearch` and `DepthFirstSearch` return lazily evaluated sequences of the vertices reachable from a source, beginning with the source itself. They accept any `IReadOnlyGraph<T>`, which `Graph<T>` implements.

```csharp
using Bodu.Collections.Generic.Graphs;

var graph = new Graph<int>(GraphKind.Directed);
graph.AddEdge(1, 2);
graph.AddEdge(1, 3);
graph.AddEdge(2, 4);

// Breadth-first: source, then its neighbors, then theirs.
var bfs = GraphAlgorithms.BreadthFirstSearch(graph, 1).ToList();   // 1, 2, 3, 4

// Depth-first: dive down one branch before backtracking.
var dfs = GraphAlgorithms.DepthFirstSearch(graph, 1).ToList();     // e.g. 1, 3, 2, 4
```

Both throw `ArgumentException` if the source is not in the graph. Because the sequences are lazy, you can short-circuit a search with LINQ:

```csharp
bool reachable = GraphAlgorithms.BreadthFirstSearch(graph, 1).Contains(4);   // true
```

## Pattern 3 — Shortest path with Dijkstra

The shortest-path family runs Dijkstra's algorithm over non-negative weights and accepts an `IReadOnlyWeightedGraph<T, double>`. There are three entry points:

- `ShortestPath` returns just the vertex list (empty when unreachable).
- `TryShortestPath` returns a `ShortestPathResult<T>` carrying `Found`, `Distance`, and `Path`.
- `ShortestPathLengths` returns the distance from the source to every reachable vertex.

```csharp
using Bodu.Collections.Generic.Graphs;

var graph = new Graph<string>(GraphKind.Directed);
graph.AddEdge("A", "B", 1.0);
graph.AddEdge("B", "C", 2.0);
graph.AddEdge("A", "C", 5.0);

// Cheapest route A -> B -> C (total 3) beats the direct A -> C (5).
IReadOnlyList<string> path = GraphAlgorithms.ShortestPath(graph, "A", "C");   // [A, B, C]

ShortestPathResult<string> result = GraphAlgorithms.TryShortestPath(graph, "A", "C");
Console.WriteLine(result.Found);      // true
Console.WriteLine(result.Distance);   // 3.0
Console.WriteLine(string.Join(" -> ", result.Path));   // A -> B -> C
```

When no path exists, `TryShortestPath` reports it cleanly:

```csharp
graph.AddVertex("Z");   // present but unreachable from A

ShortestPathResult<string> miss = GraphAlgorithms.TryShortestPath(graph, "A", "Z");
Console.WriteLine(miss.Found);                              // false
Console.WriteLine(double.IsPositiveInfinity(miss.Distance)); // true
Console.WriteLine(miss.Path.Count);                        // 0
```

To get every distance from a source in one pass:

```csharp
IReadOnlyDictionary<string, double> distances =
    GraphAlgorithms.ShortestPathLengths(graph, "A");
// { A: 0, B: 1, C: 3 }  — unreachable vertices (e.g. Z) are omitted
```

Dijkstra relaxation here is backed by `IndexedPriorityQueue<TElement, TPriority>`, whose O(log n) decrease-key is exactly what an efficient Dijkstra needs — see the [indexed priority queue guide](indexed-priority-queue.md). Because weights must be non-negative, there is no negative-edge (Bellman-Ford) variant.

## Pattern 4 — Topological sort

`TopologicalSort` orders the vertices of a directed acyclic graph so that every edge points from an earlier vertex to a later one, using Kahn's algorithm. It is directed-only: an undirected graph throws `InvalidOperationException`.

```csharp
using Bodu.Collections.Generic.Graphs;

var graph = new Graph<string>(GraphKind.Directed);
graph.AddEdge("compile", "link");
graph.AddEdge("link", "run");

IReadOnlyList<string> order = GraphAlgorithms.TopologicalSort(graph);
// compile, link, run  — every edge respected
```

`TopologicalSort` throws `InvalidOperationException` on a cycle. When a cycle is possible, prefer the non-throwing `TryTopologicalSort`, which returns `false` and an empty list instead:

```csharp
var cyclic = new Graph<string>(GraphKind.Directed);
cyclic.AddEdge("a", "b");
cyclic.AddEdge("b", "c");
cyclic.AddEdge("c", "a");   // closes a cycle

if (GraphAlgorithms.TryTopologicalSort(cyclic, out IReadOnlyList<string> sorted))
{
    // not reached: graph is cyclic
}
else
{
    Console.WriteLine(sorted.Count);   // 0
}
```

## Pattern 5 — Connected components

`ConnectedComponents` partitions the vertices into groups that are mutually reachable, treating every edge as undirected. For a directed graph this yields the weakly connected components.

```csharp
using Bodu.Collections.Generic.Graphs;

var graph = new Graph<int>(GraphKind.Undirected);
graph.AddEdge(1, 2);
graph.AddEdge(2, 3);
graph.AddEdge(10, 11);
graph.AddVertex(20);   // isolated vertex

IReadOnlyList<IReadOnlyList<int>> components = GraphAlgorithms.ConnectedComponents(graph);
// Three components: { 1, 2, 3 }, { 10, 11 }, { 20 }
Console.WriteLine(components.Count);   // 3
```

## Pattern 6 — Union-find with DisjointSet

Sometimes you want the connectivity machinery without building a graph at all. `DisjointSet<T>` is an element-keyed union-find: each element starts in its own singleton set, `Union` merges two sets, and `Find` returns a set's canonical representative. Two elements are connected exactly when they share a representative.

```csharp
using Bodu.Collections.Generic.Graphs;

var groups = new DisjointSet<string>(new[] { "a", "b", "c", "d" });
groups.Union("a", "b");
groups.Union("c", "d");
groups.Union("b", "c");                 // merges both pairs into one set

Console.WriteLine(groups.AreConnected("a", "d"));   // true
Console.WriteLine(groups.SetCount);                 // 1
Console.WriteLine(groups.SizeOf("a"));              // 4

// Same representative => same set.
Console.WriteLine(groups.Find("a").Equals(groups.Find("d")));   // true
```

Add elements incrementally with `MakeSet` (or its alias `Add`), and probe membership with `Contains` / `TryFind`:

```csharp
var ds = new DisjointSet<int>();
ds.MakeSet(1);
ds.Add(2);                              // alias for MakeSet
bool isNew = ds.MakeSet(1);             // false: already present

if (ds.TryFind(2, out int root))
    Console.WriteLine(root);            // 2 (its own representative)

Console.WriteLine(ds.Contains(99));     // false
```

When your elements are already a contiguous block of integers `[0, count)`, the non-generic `DisjointSet` is the allocation-light form — it skips the dictionary and indexes arrays directly:

```csharp
var ds = new DisjointSet(5);            // elements 0..4, each in its own set
ds.Union(0, 1);
ds.Union(2, 3);

Console.WriteLine(ds.AreConnected(0, 1));   // true
Console.WriteLine(ds.AreConnected(0, 3));   // false
Console.WriteLine(ds.SetCount);             // 3  -> {0,1} {2,3} {4}

ds.Reset();                                 // back to five singletons
Console.WriteLine(ds.SetCount);             // 5
```

Both variants combine path-halving compression with union by size, giving amortized near-constant cost per operation. `ConnectedComponents` (Pattern 5) is itself built on `DisjointSet<T>`.

## Where to go next

- [Indexed priority queue](indexed-priority-queue.md) — the decrease-key heap that powers Dijkstra in Pattern 3.
- [Choosing a collection](choosing-a-collection.md) — where the graph types sit among the other `Bodu.Core` collections.
- [Core foundations overview](../../docs/core/index.md) — the headline `Bodu.Core` building blocks.
- [Core foundations topic](../topics/core-foundations.md) — the wider tour of buffers, collections, and extensions.
- API reference: <xref:Bodu.Collections.Generic.Graphs>.
