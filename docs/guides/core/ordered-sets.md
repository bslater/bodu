---
title: Indexed and ordered sets
---

# Indexed and ordered sets

`Bodu.Collections.Generic` ships two insertion-ordered, uniqueness-enforcing sets built on the same open-addressing hash engine. They differ only in the surface they expose:

- <xref:Bodu.Collections.Generic.IndexedSet`1> implements `IList<T>` — a list that silently refuses duplicates, with O(1) `Contains`, `IndexOf`, and indexed read **and write**.
- <xref:Bodu.Collections.Generic.OrderedSet`1> implements `ISet<T>` — the full set-algebra surface (`UnionWith`, `IntersectWith`, …) while preserving insertion order and exposing position only as a read-only index.

Both keep elements in the order they were first added, both reject `null` only when the element type and comparer reject it, and both compare with an `IEqualityComparer<T>` you can supply at construction.

## When to reach for which

| You need… | Reach for |
|---|---|
| A `List<T>` that also guarantees uniqueness and O(1) `Contains` | <xref:Bodu.Collections.Generic.IndexedSet`1> |
| Positional insert / remove / move while staying unique | <xref:Bodu.Collections.Generic.IndexedSet`1> |
| Set algebra (union / intersect / except) over an ordered set | <xref:Bodu.Collections.Generic.OrderedSet`1> |
| Subset / superset / overlap relationship tests | <xref:Bodu.Collections.Generic.OrderedSet`1> |
| An unordered unique set | <xref:System.Collections.Generic.HashSet`1> (BCL) |
| A thread-safe unique set | <xref:Bodu.Collections.Generic.Concurrent.ConcurrentHashSet`1> |

## IndexedSet&lt;T&gt; — a unique, indexable list

### Pattern 1 — add, reject duplicates, look up by index

```csharp
using Bodu.Collections.Generic;

var tags = new IndexedSet<string>();

bool addedFirst  = tags.Add("alpha");   // true
bool addedSecond = tags.Add("beta");    // true
bool addedDup    = tags.Add("alpha");   // false — already present, order unchanged

int index = tags.IndexOf("beta");       // 1, O(1)
string at = tags[0];                    // "alpha", O(1)
bool has  = tags.Contains("alpha");     // true, O(1)
```

`Add` returns `false` rather than throwing when the element is already present. To add several at once, `AddRange` returns the number of elements actually inserted (duplicates skipped):

```csharp
int inserted = tags.AddRange(new[] { "beta", "gamma", "delta" }); // 2 — "beta" skipped
```

### Pattern 2 — positional editing

Because `IndexedSet<T>` is an `IList<T>`, it supports positional mutation. Inserting a value that already exists throws (use `TryInsert` for the non-throwing form), and the indexer setter replaces the element at a position:

```csharp
var order = new IndexedSet<string> { "first", "third" };

order.Insert(1, "second");          // first, second, third
bool ok = order.TryInsert(0, "second"); // false — "second" already present
order.Move(2, 0);                   // third, first, second
order[0] = "head";                  // replaces "third" at position 0
order.RemoveAt(2);                  // removes "second"
```

### Pattern 3 — capacity management

```csharp
var set = new IndexedSet<int>(capacity: 1024);
set.EnsureCapacity(4096);   // pre-grow before a known burst
// … fill …
set.TrimExcess();           // release unused slots
```

## OrderedSet&lt;T&gt; — an ordered set with full set algebra

### Pattern 4 — set operations preserve insertion order

```csharp
using Bodu.Collections.Generic;

var a = new OrderedSet<int> { 1, 2, 3, 4 };
var b = new OrderedSet<int> { 3, 4, 5, 6 };

a.UnionWith(b);             // 1, 2, 3, 4, 5, 6 (new members appended in b's order)
a.IntersectWith(b);         // keeps only members also in b
a.ExceptWith(b);            // removes members found in b
a.SymmetricExceptWith(b);   // keeps members in exactly one set
```

### Pattern 5 — relationship tests

```csharp
var roles  = new OrderedSet<string> { "reader", "writer" };
var grant  = new OrderedSet<string> { "reader", "writer", "admin" };

bool subset   = roles.IsSubsetOf(grant);        // true
bool proper   = roles.IsProperSubsetOf(grant);  // true
bool superset = grant.IsSupersetOf(roles);      // true
bool overlaps = roles.Overlaps(grant);          // true
bool equal    = roles.SetEquals(grant);         // false
```

### Pattern 6 — read-only positional view

`OrderedSet<T>` records insertion order and exposes it through a **read-only** indexer and `IndexOf`; unlike `IndexedSet<T>`, there is no indexer setter or positional `Insert`:

```csharp
var ordered = new OrderedSet<string> { "x", "y", "z" };
int pos = ordered.IndexOf("y");   // 1
string first = ordered[0];        // "x" — read only
```

## Custom equality

Both types accept an `IEqualityComparer<T>` so uniqueness can be case-insensitive or structural:

```csharp
var ci = new IndexedSet<string>(StringComparer.OrdinalIgnoreCase);
ci.Add("Alpha");
bool dup = ci.Add("ALPHA");   // false — same key under the comparer
```

## API summary

| Member | `IndexedSet<T>` | `OrderedSet<T>` | Description |
|---|:--:|:--:|---|
| `Add(T)` | ✓ | ✓ | Adds; returns `false` if already present. |
| `AddRange(IEnumerable<T>)` | ✓ | ✓ | Adds many; returns the count actually inserted. |
| `Contains(T)` | ✓ | ✓ | O(1) membership test. |
| `IndexOf(T)` | ✓ | ✓ | O(1) position lookup. |
| `this[int]` | get / set | get | Indexed access (set on `IndexedSet<T>` only). |
| `Insert` / `TryInsert` / `Move` / `RemoveAt` | ✓ | — | Positional editing. |
| `Remove(T)` | ✓ | ✓ | Removes by value. |
| `UnionWith` / `IntersectWith` / `ExceptWith` / `SymmetricExceptWith` | — | ✓ | In-place set algebra. |
| `IsSubsetOf` / `IsSupersetOf` / `IsProperSubsetOf` / `IsProperSupersetOf` / `Overlaps` / `SetEquals` | — | ✓ | Relationship tests. |
| `Capacity` / `EnsureCapacity` / `TrimExcess` | ✓ | ✓ | Capacity management. |
| `Comparer` | ✓ | ✓ | The active `IEqualityComparer<T>`. |
| `CopyTo` / `ToArray` / `Clear` / `Count` | ✓ | ✓ | Standard collection surface. |

## Where to go next

- [Choosing a collection](choosing-a-collection.md) — the full decision guide.
- [Multiset](multiset.md) — when duplicates should be *retained* as multiplicity rather than rejected.
- [Concurrent collections](concurrent-collections.md) — `ConcurrentHashSet<T>` for thread-safe set membership.
- [Bodu.Collections.Generic API reference](xref:Bodu.Collections.Generic) — full namespace overview.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
