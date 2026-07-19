# Bodu.Collections.Samples.RangesGraphsTrees

The interval-, graph-, and tree-shaped members of `Bodu.Collections.Generic`: coalescing range sets and
dictionaries plus the interval tree (`Bodu.Collections.Generic`), the graph algorithms and disjoint-set
union-find (`Bodu.Collections.Generic.Graphs`), and the tree / trie family with the Aho-Corasick
multi-pattern scanner (`Bodu.Collections.Generic.Trees`). Five scenarios.

Everything runs offline with fixed inputs. String-keyed graph vertices use a supplied `StableStringComparer`
so adjacency iteration — and therefore traversal order — is identical on every run.

```bash
dotnet run --project samples/Collections/Bodu.Collections.Samples.RangesGraphsTrees
```

## Scenario 1 — RangesAndIntervalTree

**Intent.** Distinguish the two interval models. `RangeSet<T>` / `RangeDictionary<,>` store *disjoint*
half-open ranges and coalesce adjacent ones on insertion; `IntervalTree<,>` stores *arbitrary overlapping*
intervals and answers point-stabbing and window-overlap queries.

**What it does.** Adds `[0,10)` and `[10,20)` to a range set (which merge) plus a disjoint `[30,40)`; maps
score bands to grade names in a range dictionary and looks up three scores; then indexes four overlapping
meeting intervals and asks which are active at 10:00 and which overlap the window `[12,13]`.

**What to expect.** The two touching ranges coalesce to `[0,20)`; the grade lookups land in the right band;
the interval tree returns every interval whose inclusive span covers the query (sorted for stable output):

```text
  coalesced ranges : [0,20), [30,40)
  contains 15 / 25 : True / False
  score 60 -> Pass
  score 90 -> Distinction
  score 47 -> Fail
  active at 10:00       : design, standup
  overlapping [12..13]  : design, lunch, review
```

**APIs demonstrated.** `RangeSet<T>.Add` / `.Contains` / `.Count` / indexer (`.StartInclusive` /
`.EndExclusive`); `RangeDictionary<,>.Add` / indexer / `.TryGetValue`; `IntervalTree<,>.Add` / `.QueryPoint` /
`.QueryOverlaps`.

## Scenario 2 — GraphAlgorithms

**Intent.** Show `Graph<T>` together with the static `GraphAlgorithms` helpers over a single weighted,
directed acyclic graph: breadth-first traversal, Dijkstra shortest path by summed weight, and a topological
ordering that respects every dependency edge.

**What it does.** Builds a seven-edge build-pipeline DAG (weights are per-stage effort), constructed with a
`StableStringComparer` so the internal adjacency order is deterministic. It runs `BreadthFirstSearch`,
`TryShortestPath` from `clone` to `deploy`, and `TopologicalSort`.

**What to expect.** All three agree on the natural pipeline order; the cheapest route from clone to deploy
costs 12 (via `restore`, beating the direct `clone→build` edge of weight 9):

```text
  vertices / edges : 6 / 7
  BFS from clone   : clone -> restore -> build -> test -> package -> deploy
  cheapest path    : clone -> restore -> build -> test -> package -> deploy
  path cost        : 12 (found: True)
  topological order: clone -> restore -> build -> test -> package -> deploy
```

**APIs demonstrated.** `Graph<T>(GraphKind, IEqualityComparer<T>)`, `.AddEdge(from, to, weight)`,
`.VertexCount` / `.EdgeCount`; `GraphAlgorithms.BreadthFirstSearch`, `.TryShortestPath` (returns
`ShortestPathResult<T>` with `.Path` / `.Distance` / `.Found`), `.TopologicalSort`.

## Scenario 3 — DisjointSetUnionFind

**Intent.** Show `DisjointSet<T>` (union-find): a partition of elements into disjoint groups that merges two
groups in near-constant amortized time and answers same-group membership queries.

**What it does.** Seeds six singleton sets, merges them along a fixed friendship edge list with `Union`, then
asks two connectivity questions and prints the resulting components (each grouped by its `Find`
representative and sorted for stable output).

**What to expect.** Three unions leave three components; `amy` and `cara` end up connected transitively
through `ben`, while `amy` and `dan` remain in different components:

```text
  initial groups : 6 (one per element)
  amy ~ cara?    : True
  amy ~ dan?     : False
  groups now     : 3
  component      : {amy, ben, cara}
  component      : {dan, eve}
  component      : {finn}
```

**APIs demonstrated.** `DisjointSet<T>(IEnumerable<T>)`, `.SetCount`, `.Union`, `.AreConnected`, `.Find`.

## Scenario 4 — TreesAndTries

**Intent.** Cover the tree and trie family: `Tree<T>` (an n-ary tree with multiple traversal orders),
`Trie<TValue>` (a character trie mapping string keys to values with prefix search), and `RadixTrie` (a
compressed prefix set).

**What it does.** Assembles a small filesystem-shaped tree and walks it in pre-order and level-order, counting
leaves; loads a value trie of protocol→port mappings and lists the keys under `http`; loads a radix trie of
words and runs prefix and membership queries (prefix results sorted for stable output).

**What to expect.** Pre-order is depth-first (`root, src, app.cs, …`) while level-order is breadth-first
(`root, src, docs, …`); the trie lists all three `http*` keys and resolves an exact key; the radix trie
distinguishes a stored key (`ten`) from a mere prefix (`te`):

```text
  pre-order   : root, src, app.cs, util.cs, docs, readme.md
  level-order : root, src, docs, app.cs, util.cs, readme.md
  leaf count  : 3
  keys under 'http' : http, https, httpx
  https -> port     : 443
  any key 'ft*'?    : True
  radix keys 'tea*' : tea, team, teapot
  contains 'ten'    : True
  contains 'te'     : False
```

**APIs demonstrated.** `Tree<T>.AddChild`, `.PreOrder` / `.LevelOrder`, `.IsLeaf`; `Trie<TValue>.Add` /
indexer / `.KeysWithPrefix` / `.StartsWith`; `RadixTrie(IEnumerable<string>)`, `.KeysWithPrefix` /
`.Contains`.

## Scenario 5 — MultiPatternSearch

**Intent.** Show `AhoCorasickAutomaton<TValue>`: a finite-state machine that scans text once and reports every
occurrence of any registered pattern — including overlapping matches — in a single linear pass regardless of
how many patterns there are.

**What it does.** Builds an automaton from a keyword→category dictionary of four words, then enumerates all
matches over the text `"ushers"` and counts them.

**What to expect.** Three patterns hide inside the six-letter word — `she`, `he`, and `hers` — and the scanner
finds all of them (with their categories and half-open spans) in one pass:

```text
  patterns : 4
  text     : "ushers"
  match    : 'he' (pronoun) at [2..4)
  match    : 'she' (pronoun) at [1..4)
  match    : 'hers' (possessive) at [2..6)
  total    : 3 match(es)
```

**APIs demonstrated.** `AhoCorasickAutomaton<TValue>.Build(IEnumerable<KeyValuePair<string, TValue>>)`,
`.Patterns`, `.EnumerateMatches` (yielding `AhoCorasickMatch<TValue>` with `.Pattern` / `.Value` / `.Start` /
`.End`), `.CountMatches`.

## Layout

```text
Bodu.Collections.Samples.RangesGraphsTrees/
  Program.cs                           # runs the scenarios in order
  StableStringComparer.cs              # process-stable string hashing for deterministic graph output
  Scenarios/RangesAndIntervalTree.cs
  Scenarios/GraphAlgorithms.cs
  Scenarios/DisjointSetUnionFind.cs
  Scenarios/TreesAndTries.cs
  Scenarios/MultiPatternSearch.cs
```

## Related

- `Bodu.Collections.Samples.CollectionCatalogue` — the ring, deque, evicting cache, multi-maps and sets, the
  bidirectional and navigable dictionaries, and the indexed priority queue.
- `Bodu.Collections.Samples.ProbabilisticSketches` — the Bloom filter, count-min sketch, and HyperLogLog
  approximate sketches.
