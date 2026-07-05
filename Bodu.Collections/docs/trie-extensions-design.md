# Trie-family extensions — design note

**Date:** 2026-07-05
**Status:** Executed alongside implementation — `AhoCorasickAutomaton` (+ `<TValue>`) and `RadixTrie` (+ `<TValue>`) ship with this note.
**Relates to:** [`roadmap-implementation-plan.md`](../../Bodu.Core/docs/roadmap-implementation-plan.md) §8 T6.c; [`ROADMAP.md`](../../ROADMAP.md) — *Per-project roadmap → `Bodu.Core`*

This note records the build model, node representation, match semantics, and compression contracts for the
two siblings of the existing `Trie` / `Trie<TValue>` in `Bodu.Collections.Generic.Trees`.

## 1. Aho-Corasick build model: two-phase, immutable after build

Construction is a **single factory call** — `AhoCorasickAutomaton.Build(IEnumerable<string> patterns)` (and the
keyed `Build(IEnumerable<KeyValuePair<string,TValue>>)`) — not a mutable add-then-freeze builder. The factory
materializes the pattern collection, builds the goto trie, then runs the classic breadth-first pass that assigns
every node its **failure link** (the node for the longest proper suffix of its path that is also a path in the
trie) and its **output link** (the nearest failure-chain node that terminates a pattern). The automaton is
**immutable once built**, and this is a load-bearing contract, not a convenience: failure and output links are
*global* invariants — every link in the automaton is defined relative to the complete pattern set, so a pattern
added later would invalidate links wholesale (any existing node whose longest matchable suffix changes must be
re-linked, which is the full BFS pass again). Re-running `Build` with the new set is the honest, equally cheap
operation, so no incremental mutation surface is offered.

Pattern-set contracts: the collection must be non-null and non-empty; individual patterns must be non-null and
non-empty (`ArgumentException` — an empty pattern would "match" at every position and carries no information).
The unkeyed automaton **deduplicates** repeated patterns (`Patterns` reports each once, in first-seen order);
the keyed automaton **throws** on a duplicate pattern key (`Arg_Invalid_DuplicateDictionaryKey`), mirroring the
`Trie<TValue>` versus `Trie` add contracts.

## 2. Node representation and case sensitivity

Goto transitions are `Dictionary<char, Node>` per node — the same representation `TrieCore`'s `TrieNode<TValue>`
uses, chosen for the same reasons (sparse alphabets, O(1) expected transition, no per-node 64K arrays). The
automaton is **ordinal-only**: `Trie` defaults to ordinal (`EqualityComparer<char>.Default`) and merely *allows*
a custom `IEqualityComparer<char>`; the automaton pins the default and does not accept a comparer, because the
comparer would have to govern failure-link construction as well as matching, and a fold-while-matching automaton
is better built by normalizing patterns and text up front. `RadixTrie` keeps the comparer constructor for
surface parity with `Trie` (§4).

## 3. Match semantics and ordering

`EnumerateMatches(string text)` reports **all occurrences of all patterns — including overlapping and nested
matches** — as `AhoCorasickMatch(string Pattern, int Start)`; the exclusive end index is derivable
(`End => Start + Pattern.Length`). Matches are yielded in a **pinned deterministic order: ascending end index,
then ascending pattern length** — the scan visits text positions left to right, and at each end position the
output chain (which naturally surfaces the longest suffix first) is buffered and reversed so shorter patterns
precede longer ones. The order is documented and tested; for `he/she/his/hers` over `"ushers"` the sequence is
`he@2, she@1, hers@2`.

**The span split:** a lazy `IEnumerable<T>` cannot capture a `ReadOnlySpan<char>` (ref structs cannot live in
iterator state machines), so the surface is split — `EnumerateMatches(string)` is the lazy, allocation-per-match
enumeration, while `CountMatches(ReadOnlySpan<char>)` and `HasMatch(ReadOnlySpan<char>)` are span-friendly
**eager** conveniences that run the same scan without materializing matches (`HasMatch` exits at the first hit).
String overloads of the eager pair are unnecessary — `string` converts implicitly to span.

## 4. RadixTrie edge-compression model and Trie parity

`RadixTrie` is a PATRICIA-style path-compressed trie: each child edge carries a **string label** (one or more
characters), keyed in the parent's `Dictionary<char, Node>` by the label's first character under the
construction-time `IEqualityComparer<char>`. Structural invariant: **no non-root, non-terminal node has exactly
one child** — such a node is always fused with its child, so node count is O(number of keys), not O(total key
length).

- **Split on insert:** walking an edge whose label diverges from the key mid-label splits the edge — an
  intermediate node takes the common label prefix, the old child keeps the remainder, and the key's tail (if
  any) becomes a new leaf (`"team"` then `"tea"`: the `team` edge splits into `tea` → `m`).
- **Merge on remove:** after a removal detaches a leaf or clears a terminal flag, any node left as a non-root,
  non-terminal single-child pass-through is re-fused with its child, restoring the invariant.

The public surface **mirrors `Trie` / `Trie<TValue>` member-for-member** (constructors incl. the
`IEqualityComparer<char>` overloads, `Count`, `Comparer`, `Add`/`TryAdd`/`Set`/indexer, `Contains`/`ContainsKey`,
`TryGetValue`, `StartsWith`, `Remove`, `KeysWithPrefix`/`ItemsWithPrefix`, `Clear`, the fail-fast snapshot
struct enumerator, and the debugger proxies), including the span overloads, so consumers can swap types without
code changes. The compressed representation is intentionally not observable through the API — no node/edge
introspection is exposed — and compression is pinned indirectly by structure-sensitive behavioural tests
(split/merge sequences must preserve every membership, prefix, and enumeration answer).

## 5. Differential test strategies

- **Aho-Corasick** (`AhoCorasickAutomatonTests.DifferentialSweep`, Regression tier): seeded random corpora —
  5,000-char texts over a 4-char alphabet with 50 random patterns of lengths 1..8 per seed — with the full
  `(pattern, start)` match set compared against a brute-force per-pattern `string.IndexOf` scan oracle, plus a
  verification that the enumerated sequence obeys the pinned (end, length) order. The failure-link BFS is the
  classic subtle spot; the oracle cannot be wrong.
- **RadixTrie** (`RadixTrieTests.DifferentialSweep` / `RadixTrieGenericTests.DifferentialSweep`, Regression
  tier): 10,000 seeded add/remove/contains/prefix operations on random short strings mirrored against the
  existing uncompressed `Trie` / `Trie<TValue>` as the oracle — every per-operation result and periodic
  full-content checkpoints must agree, which exercises every split and merge path the compressed representation
  can take.
