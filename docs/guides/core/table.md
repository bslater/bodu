---
title: Table (two-key map with projections)
---

# Table (two-key map with projections)

`Table<TRow, TColumn, TValue>` is the .NET analogue of Guava's `Table`: a map keyed by two independent keys — a row and a column — that associates each `(row, column)` pair with a single value.

## When the type is justified

Be honest about the baseline first: **a plain `Dictionary<(TRow, TColumn), TValue>` already covers flat two-key lookup.** Tuple keys hash and compare fine, and if all you ever do is get/set/remove by the full pair, the BCL dictionary is the right choice — `Table` adds nothing there.

The reason `Table` exists is the **projections**:

- `Row(row)` — all cells of one row as a live `IReadOnlyDictionary<TColumn, TValue>`.
- `Column(column)` — one column across all rows as a live `IReadOnlyDictionary<TRow, TValue>`.
- `RowKeys` / `ColumnKeys` — the key sets of each axis.
- `RowMap()` — per-row iteration pairing each row key with its live row view.

With a tuple-keyed dictionary, "all cells of row *r*" is an O(n) LINQ scan over every entry, allocated fresh each time; with `Table` it is an O(1) dictionary handoff. Adopt `Table` only when you need those views — that is the roadmap's own adoption caveat for this type.

```csharp
using Bodu.Collections.Generic;

var sales = new Table<string, int, decimal>();
sales.Add("Widgets", 2024, 1200m);
sales.Add("Widgets", 2025, 1350m);
sales.Add("Gadgets", 2025, 800m);

decimal w2025 = sales["Widgets", 2025];              // flat two-key lookup

IReadOnlyDictionary<int, decimal> widgets = sales.Row("Widgets");   // 2024 → 1200, 2025 → 1350
IReadOnlyDictionary<string, decimal> in2025 = sales.Column(2025);   // Widgets → 1350, Gadgets → 800

sales["Gadgets", 2025] = 850m;                       // upsert through the indexer…
decimal revised = in2025["Gadgets"];                 // 850 — the held view is live
```

## The flat surface

The two-key surface mirrors the familiar dictionary contract:

- `this[row, column]` — get throws `KeyNotFoundException` for a missing cell; set upserts (creating the row when absent).
- `Add` throws on a duplicate cell; `TryAdd` returns `false` instead.
- `TryGetValue`, `Contains(row, column)`, `ContainsRow`, `ContainsColumn`, `ContainsValue`.
- `Remove(row, column)`, `RemoveRow(row)` (all cells of the row), `RemoveColumn(column)` (the column's cell in every row), `Clear`.
- `Count` is the total cell count, maintained as an O(1) counter; `IsEmpty` is its zero check.
- Enumeration yields the flat cells as `KeyValuePair<(TRow Row, TColumn Column), TValue>` in row-major order (per-row order is the unspecified `Dictionary` order — do not rely on it).

Row and column comparers are injectable at construction and exposed through `RowComparer` / `ColumnComparer`.

## Contract points

- **The views are live.** `Row` and `Column` hold the table and the key and resolve on every access — mutations made after the view was created are visible through it. A row view created *before* the row exists reports empty and starts reporting cells once the row appears; it reverts to empty when the row's last cell is removed.
- **The column axis is honest about its cost.** The backing store is row-major (`Dictionary<TRow, Dictionary<TColumn, TValue>>`) with **no second index**: `Column(...)` enumeration and its `Count`, `ContainsColumn`, and `RemoveColumn` scan every row and are O(rows) per call; `ColumnKeys` walks every cell. Per-row lookups through a column view (`ContainsKey`, `TryGetValue`, the indexer) stay O(1). Put the axis you project by most often on the row side.
- **Empty rows never leak.** Removing a row's last cell — via `Remove` or `RemoveColumn` — also removes the row, so `ContainsRow`, `RowKeys`, and `RowMap()` only ever report rows holding at least one cell.
- **`RowKeys` is live; `ColumnKeys` is a snapshot.** `RowKeys` is the backing dictionary's key collection; `ColumnKeys` is recomputed (distinct under `ColumnComparer`) on each read.
- **Fail-fast enumeration.** The views delegate to the live backing dictionaries, so mutating the table while enumerating the table or a view surfaces the standard `Dictionary` fail-fast `InvalidOperationException`.

> [!NOTE]
> `Table<TRow, TColumn, TValue>` is not thread-safe. Concurrent reads and writes, including through the row and column views, require external synchronization.

## Where to go next

- <xref:Bodu.Collections.Generic.Table`3> — the full API surface.
- [Choosing a collection](choosing-a-collection.md) — the full decision guide across the namespace.
- [Multi-value dictionary](multi-value-dictionary.md) — one key to *many* values, when the second axis is not a key.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for the collections packages.
