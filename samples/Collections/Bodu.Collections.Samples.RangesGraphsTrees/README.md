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
--- RangeSet / RangeDictionary / IntervalTree ---
  What   : Adds touching and overlapping ranges to a set and watches them coalesce, maps score bands to grades
           through a range dictionary, then indexes a day of overlapping meetings and asks what is active at an
           instant and what overlaps a window.
  Why    : Range keys turn a chain of if-else boundary comparisons into a lookup, and boundary comparisons written
           by hand are where off-by-one bugs live. The critical choice is coalescing versus not: a range set merges
           adjacent and overlapping ranges, which is right for a partition like a grade band and wrong for anything
           that legitimately overlaps. An interval tree keeps every interval distinct and answers point-stabbing and
           window queries in log time rather than by scanning.
  Expect : [0,10) and [10,20) coalesce into [0,20) because half-open ranges that touch have no gap between them,
           while [30,40) stays separate. 15 is inside the merged range and 25 falls in the gap. The meeting queries
           return several overlapping entries at once, which is exactly what the coalescing containers could not
           represent.

  coalesced ranges : [0,20), [30,40)  (expected [0,20) and [30,40) - the touching pair merged, since half-open ranges that meet leave no gap; [30,40) is disjoint and survives)
  contains 15 / 25 : True / False  (expected True / False - 15 sits inside the merged range, 25 in the gap between them)
  score 60 -> Pass  (a band lookup, not a chain of >= comparisons - the boundary lives in the data)
  score 90 -> Distinction  (the top band; note each band is half-open, so 90 belongs to this one and not the one below)
  score 47 -> Fail  (TryGetValue is the safe form when a score might fall outside every band)
  active at 10:00       : design, standup  (point stabbing - two meetings overlap this instant, which a coalescing range set would have merged into one)
  overlapping [12..13]  : design, lunch, review  (a window query returns every interval touching it, however they overlap each other)
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
--- Graph<T> + GraphAlgorithms - BFS, shortest path, topological sort ---
  What   : Builds a weighted build-pipeline graph, walks it breadth-first, finds the cheapest route by edge weight,
           then produces a dependency-respecting execution order.
  Why    : The three answers differ and are easy to confuse. BFS gives fewest hops, ignoring weight - the right
           answer for degrees of separation, the wrong one for cost. Dijkstra weighs the edges, so its route may
           take more steps to spend less. A topological order answers neither question: it produces any sequence in
           which no step precedes something it depends on, which is what a build scheduler needs and what a cycle
           would make impossible.
  Expect : All three agree here because the pipeline is a chain - which is itself worth noticing, since on a
           branching graph they routinely disagree. The path costs 12 and Found is True; a stable comparer pins the
           adjacency iteration so the traversal order is identical on every run.

  vertices / edges : 6 / 7  (expected 6 / 7 - more edges than a plain chain, so there is a genuine choice of route)
  BFS from clone   : clone -> restore -> build -> test -> package -> deploy  (visit order by hop count, weights ignored entirely)
  cheapest path    : clone -> restore -> build -> test -> package -> deploy  (Dijkstra minimises total weight, so on a branching graph this can take more hops than BFS to cost less)
  path cost        : 12  (found: True - check Found before trusting Distance; an unreachable target is not an exception)
  topological order: clone -> restore -> build -> test -> package -> deploy  (an order in which nothing precedes its dependency - this exists only because the graph is acyclic)
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
--- DisjointSet<T> - union-find connectivity ---
  What   : Starts with six elements in six groups, merges a few pairs, asks whether two elements are connected, and
           enumerates the resulting components.
  Why    : Connectivity by traversal costs a search per question. Union-find answers it in near-constant amortised
           time by storing only which group each element belongs to - never the edges - so it scales to questions
           asked far more often than the structure changes. The catch is the other side of that trade: it can tell
           you two elements are connected but not by what route, and it cannot un-merge. It is the right tool for
           Kruskal, for cycle detection while building, and for incremental clustering; the wrong one if you ever
           need the path or a split.
  Expect : Six singletons collapse to three components after the merges. amy and cara test connected through ben
           without any edge between them being stored, while amy and dan remain apart. The components print as {amy,
           ben, cara}, {dan, eve} and the untouched {finn}.

  initial groups : 6  (expected 6 - every element starts in a group of its own)
  amy ~ cara?    : True  (expected True - connected through ben; transitivity comes free, no path is stored or walked)
  amy ~ dan?     : False  (expected False - different components, answered without searching either of them)
  groups now     : 3  (expected 3 - each Union lowers the count by one, and merging an already-joined pair is a no-op)
  component      : {amy, ben, cara}  (materialising the groups is the expensive direction - the structure is built to answer membership, not to list it)
  component      : {dan, eve}  (materialising the groups is the expensive direction - the structure is built to answer membership, not to list it)
  component      : {finn}  (materialising the groups is the expensive direction - the structure is built to answer membership, not to list it)
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
--- Tree<T> / Trie<TValue> / RadixTrie ---
  What   : Walks an n-ary tree in pre-order and level-order, looks up keys by prefix in a trie, and asks a radix
           trie about a word that is only a waypoint rather than a member.
  Why    : The two traversals answer different questions: pre-order follows each branch to its end, which is what
           you want for rendering a hierarchy; level-order sweeps by depth, which is what you want for breadth-first
           work or for anything sensitive to distance from the root. The tries exist because prefix is the index -
           completing "http" is a walk to one node, where a dictionary would have to examine every key. The
           membership trap is the thing to remember: passing through a node does not make it a key.
  Expect : The same six nodes print in two different orders, and only the traversal differs. The trie returns three
           keys under http and answers a prefix-existence question without materialising the matches. In the radix
           trie, ten is a member and te is not, despite te being on the path to it.

  pre-order   : root, src, app.cs, util.cs, docs, readme.md  (depth first - each branch is finished before the next begins, which is the order a directory listing reads in)
  level-order : root, src, docs, app.cs, util.cs, readme.md  (breadth first - every node at one depth before any at the next; same six nodes, different question)
  leaf count  : 3  (expected 3 - the files; traversal order does not affect a count, only the sequence)
  keys under 'http' : http, https, httpx  (expected http, https, httpx - one walk to the http node, then everything below it; http is both a key and a prefix)
  https -> port     : 443  (expected 443 - an exact lookup still works; the prefix index does not cost you the dictionary behaviour)
  any key 'ft*'?    : True  (expected True - answers whether anything shares the prefix without enumerating the matches)
  radix keys 'tea*' : tea, team, teapot  (a radix trie compresses single-child chains into one edge, so it stores the same keys in fewer nodes)
  contains 'ten'    : True  (expected True - added as a key)
  contains 'te'     : False  (expected False - te is on the path to ten and tea, but reaching a node is not membership)
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
--- AhoCorasickAutomaton<TValue> - multi-pattern scan ---
  What   : Registers four patterns with associated values and scans a short string that makes three of them overlap.
  Why    : Searching a document for many terms by looping over the terms costs one pass per term, so the work grows
           with the dictionary - untenable once the dictionary is thousands of terms. This automaton makes one pass
           regardless of how many patterns are registered. It also finds overlapping matches, which the
           loop-per-term approach quietly drops: once a search advances past a hit, any match starting inside that
           hit is gone.
  Expect : Three matches in a six-character string, deliberately overlapping: he at [2..4), she at [1..4) and hers
           at [2..6). Every one shares characters with another, and a naive scan that skipped past the first hit
           would have reported only one of them.

  patterns : 4  (expected 4 - the scan below costs one pass whether this is 4 or 40,000)
  text     : "ushers"  (six characters chosen so three patterns overlap inside them)
  match    : 'he' (pronoun) at [2..4)  (half-open span; the value rides along, so the automaton doubles as a lookup)
  match    : 'she' (pronoun) at [1..4)  (half-open span; the value rides along, so the automaton doubles as a lookup)
  match    : 'hers' (possessive) at [2..6)  (half-open span; the value rides along, so the automaton doubles as a lookup)
  total    : 3 match(es)  (expected 3 - overlapping hits all count; a loop-per-pattern scan would have missed some)
```

**APIs demonstrated.** `AhoCorasickAutomaton<TValue>.Build(IEnumerable<KeyValuePair<string, TValue>>)`,
`.Patterns`, `.EnumerateMatches` (yielding `AhoCorasickMatch<TValue>` with `.Pattern` / `.Value` / `.Start` /
`.End`), `.CountMatches`.

## Layout

```text
Bodu.Collections.Samples.RangesGraphsTrees/
  Program.cs                           # runs the scenarios in order
  SampleConsole.cs                     # the what/why/expect banner every scenario opens with
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
