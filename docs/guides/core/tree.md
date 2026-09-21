---
title: N-ary tree
---

# N-ary tree

<xref:Bodu.Collections.Generic.Trees.Tree`1> is a mutable **n-ary tree node**: every instance carries a `Value`, an optional `Parent`, and an ordered list of `Children`, and is at the same time the root of the subtree beneath it. There is no separate "tree" container — a root is simply a node whose `Parent` is `null` — so the same type models an org chart, a file system, a parsed outline, or a scene graph.

The type lives in `Bodu.Collections.Generic.Trees` alongside the trie family, but it is not a trie: it has no keys, no lookup, and no ordering constraint. Use it when the *shape* of the hierarchy is the data. For keyed prefix structures see [Tries and text search](trie.md); for flattening an existing object graph without building nodes, see `RecursiveSelect` in [Sequence operators and generators](sequence-operators.md#pattern-7--recursiveselect-and-recursiveselectcontrol).

All examples below were run; the comments show the actual results.

## Pattern 1 — build a tree

```csharp
using Bodu.Collections.Generic.Trees;

var company = new Tree<string>("Company");
Tree<string> engineering = company.AddChild("Engineering");   // AddChild(T) returns the new node
Tree<string> sales = company.AddChild("Sales");
engineering.AddChild("Platform");
engineering.AddChild("Apps");
sales.AddChild("EMEA");

var support = new Tree<string>("Support", new[] { "Tier 1", "Tier 2" });   // a root seeded with child values
company.AddChild(support);                                                  // attach a detached subtree

int direct   = company.ChildCount;              // 3
bool isRoot  = company.IsRoot;                  // true
bool wasRoot = support.IsRoot;                  // false — attached now
int depth    = engineering.Depth;               // 1 — edges from the root
int height   = company.Height;                  // 2 — edges on the longest downward path
string owner = support.Parent?.Value ?? "";     // "Company"
```

`AddChild(T value)` creates and appends a child and returns it, so a tree can be built fluently from the top down. `AddChild(Tree<T> child)` attaches an existing **detached** node (one whose `Parent` is `null`) with its whole subtree; the node must not already have a parent and must not be an ancestor of the target, or `InvalidOperationException` is thrown. `Children` is a live read-only view — it reflects later adds and removes but cannot be mutated directly, which is how the parent and acyclicity invariants stay intact.

## Pattern 2 — traversals

Six enumerations are available on every node and apply to the subtree rooted at that node:

```csharp
using Bodu.Collections.Generic.Trees;

var root = new Tree<int>(1);
var two = root.AddChild(2);
var three = root.AddChild(3);
two.AddChild(4);
two.AddChild(5);
three.AddChild(6);

var pre    = root.PreOrder().Select(n => n.Value);      // 1 2 4 5 3 6
var post   = root.PostOrder().Select(n => n.Value);     // 4 5 2 6 3 1
var level  = root.LevelOrder().Select(n => n.Value);    // 1 2 3 4 5 6
var desc   = root.Descendants().Select(n => n.Value);   // 2 4 5 3 6  — pre-order minus the node itself
var leaves = root.Leaves().Select(n => n.Value);        // 4 5 6
var anc    = two.Children[1].Ancestors().Select(n => n.Value);   // 2 1 — parent first, root last
var sub    = two.PreOrder().Select(n => n.Value);       // 2 4 5 — traversals are per subtree
Tree<int> top = two.Children[1].Root();                 // the node holding 1
```

| Member | Order | Includes the receiver? |
|---|---|---|
| `PreOrder()` | node, then each child's subtree left to right | yes |
| `PostOrder()` | each child's subtree left to right, then the node | yes |
| `LevelOrder()` | breadth-first, left to right within a level | yes |
| `Descendants()` | pre-order | no |
| `Leaves()` | pre-order, nodes with `IsLeaf == true` only | yes, if it is itself a leaf |
| `Ancestors()` | parent, grandparent, …, root | no |
| `Root()` | — | returns the topmost ancestor (the node itself when it is a root) |

Every traversal is **lazy** (`yield`-based) and **iterative** — an explicit stack or queue rather than recursion — so arbitrarily deep trees can be walked without a stack overflow:

```csharp
using Bodu.Collections.Generic.Trees;

var root = new Tree<int>(0);
var cursor = root;
for (int i = 1; i <= 200_000; i++)
    cursor = cursor.AddChild(i);            // a 200 000-deep chain

int nodes  = root.PostOrder().Count();      // 200001
int height = root.Height;                   // 200000
int depth  = cursor.Depth;                  // 200000
```

`Depth` and `Height` are computed on demand — `Depth` walks up to the root (O(depth)), `Height` walks the subtree (O(subtree size)) — so cache them if you need them in a hot loop.

## Pattern 3 — mutate: detach, re-attach, replace values, clear

```csharp
using Bodu.Collections.Generic.Trees;

var root = new Tree<string>("root");
var a = root.AddChild("a");
var b = root.AddChild("b");
var a1 = a.AddChild("a1");

bool removed = root.RemoveChild(b);        // true — b is now a detached root with its own subtree intact
bool bIsRoot = b.IsRoot;                   // true
bool first   = a1.Remove();                // true  — detaches a1 from a
bool second  = a1.Remove();                // false — already a root
a.AddChild(a1);                            // re-attach the detached node
string parent = a1.Parent!.Value;          // "a"

a1.Value = "A1";                           // Value is a settable property; the structure is untouched
var walk = root.PreOrder().Select(n => n.Value);   // root a A1

try { root.AddChild(a1); }                 // a1 already has a parent
catch (InvalidOperationException) { /* "already has a parent" */ }

try { a1.AddChild(root); }                 // root is an ancestor of a1
catch (InvalidOperationException) { /* "would create a cycle" */ }

root.Clear();                              // detaches every child; each becomes a root
bool empty = root.IsLeaf;                  // true
bool aRoot = a.IsRoot;                     // true
```

| Member | Effect | Returns |
|---|---|---|
| `AddChild(T value)` | Appends a new child. | the new node |
| `AddChild(Tree<T> child)` | Appends a detached subtree. Throws `ArgumentNullException` for `null`, `InvalidOperationException` if `child` has a parent or is an ancestor of this node. | — |
| `RemoveChild(Tree<T> child)` | Detaches a direct child (subtree preserved). | `true` if found |
| `Remove()` | Detaches this node from its parent. | `true` if it had one |
| `Clear()` | Detaches every child. | — |
| `Value` (get/set) | The payload. | — |

Removal never destroys a subtree — a removed node keeps its children and can be re-attached anywhere, including under a different root.

## Pattern 4 — mutation during traversal

Traversals do **not** carry a version counter, so there is no fail-fast exception: mutating the tree while one of its enumerations is live produces *undefined results* (skipped or repeated nodes are possible). Snapshot first, then mutate:

```csharp
using Bodu.Collections.Generic.Trees;

var root = new Tree<int>(0);
root.AddChild(1);
root.AddChild(2);

foreach (var node in root.Descendants().ToList())   // materialize before touching the structure
    if (node.Value == 1) node.Remove();

int remaining = root.ChildCount;                    // 1
```

`Tree<T>` is not thread-safe for concurrent mutation either; readers on other threads must be excluded while a writer is active. Reading a fully built tree from several threads is safe, because traversals allocate their own stack state.

## When to use `Tree<T>` versus the alternatives

| You have | Reach for |
|---|---|
| A hierarchy whose *shape* is the data, edited over time (outline, org chart, scene graph) | `Tree<T>` |
| An existing object graph with a child collection you only need to *walk* | `RecursiveSelect` (no node allocation) — see [Sequence operators](sequence-operators.md) |
| String keys with prefix lookup or autocomplete | `Trie` / `Trie<TValue>` / `RadixTrie` — see [Tries and text search](trie.md) |
| Arbitrary edges (cycles, multiple parents, weights) | `Graph<T>` — see [Graphs and graph algorithms](graphs.md) |
| Disjoint groups merged over time | `DisjointSet<T>` — see [Graphs and graph algorithms](graphs.md) |

## API summary

| Member | Description |
|---|---|
| `Tree(T value)` / `Tree(T value, IEnumerable<T> childValues)` | Creates a root, optionally with one child per value. |
| `Value` | The payload; settable. |
| `Parent` | The parent node, or `null` for a root. |
| `Children` | Live, read-only, ordered view of the direct children. |
| `ChildCount`, `IsRoot`, `IsLeaf` | Structural facts, O(1). |
| `Depth`, `Height` | Edges to the root / to the deepest descendant leaf; computed on demand. |
| `AddChild(T)`, `AddChild(Tree<T>)`, `RemoveChild(Tree<T>)`, `Remove()`, `Clear()` | Structural mutation; the parent and acyclicity invariants are enforced. |
| `PreOrder()`, `PostOrder()`, `LevelOrder()`, `Descendants()`, `Leaves()`, `Ancestors()`, `Root()` | Lazy, iterative traversals of the subtree (or the ancestor chain). |

## Where to go next

- [Tries and text search](trie.md) — the keyed structures that share this namespace.
- [Sequence operators and generators](sequence-operators.md) — `RecursiveSelect` for walking hierarchies you do not own.
- [Graphs and graph algorithms](graphs.md) — when the structure is not a tree.
- [`Bodu.Collections.Generic.Trees` API reference](xref:Bodu.Collections.Generic.Trees) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
