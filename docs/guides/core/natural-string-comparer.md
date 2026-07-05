---
title: Natural string comparer
---

# Natural string comparer

*Natural* (numeric-aware) ordering compares the digit runs embedded in strings by numeric value instead of character by character, so `file2` sorts before `file10` — the ordering Windows Explorer (`StrCmpLogicalW`) and Python's `natsort` produce. <xref:Bodu.Extensions.NaturalStringComparer> packages that behaviour as a stateless, thread-safe comparer with the same factory shape as `StringComparer`: `Ordinal`, `OrdinalIgnoreCase`, `CurrentCulture`, `CurrentCultureIgnoreCase`, and `Create(CultureInfo, bool)`.

A few contract points worth keeping in mind:

- **Digit runs never overflow.** Runs are compared by trimmed length and then digit by digit — they are never parsed into a bounded integer — so a 30-digit serial number compares correctly against a 29-digit one.
- **The order is total.** Two runs with equal numeric value but different zero padding are *not* equal: when the strings are otherwise equal, the run with fewer leading zeros sorts first (`7` before `07`), keeping sorts deterministic.
- **Only ASCII digits `0`–`9` are numeric.** Other Unicode digit classes are ordinary text, a `-` is just a character (no sign parsing), and there is no decimal, thousands-separator, or version-sort dotted-tuple handling — `.` merely separates adjacent runs.
- **Null and prefix ordering follow BCL conventions.** `null` sorts before any string, the empty string before any non-empty one, and a strict prefix before the longer string (`file` before `file1`). At the same position a digit run sorts before non-digit text.
- **Equality and hashing agree.** `Equals(x, y)` is exactly `Compare(x, y) == 0`, and `GetHashCode` assigns equal hashes to equal strings (culture-aware modes trade some hash distribution for that guarantee).

## Pattern 1 — Sort file-style names

Use the `Ordinal` instance wherever an `IComparer<string>` is accepted:

```csharp
using Bodu.Extensions;

var names = new[] { "file10", "file2", "file1" };

Array.Sort(names, NaturalStringComparer.Ordinal);
// names is now ["file1", "file2", "file10"]

var ordered = names.OrderBy(n => n, NaturalStringComparer.OrdinalIgnoreCase);
```

`OrdinalIgnoreCase` folds case the way `StringComparer.OrdinalIgnoreCase` does while still comparing digit runs numerically, so `FILE2` sorts before `file10`.

## Pattern 2 — Culture-aware text, numeric digits

The culture modes compare the non-digit segments with the culture's collation rules and leave digit-run handling unchanged. `CurrentCulture` and `CurrentCultureIgnoreCase` read `CultureInfo.CurrentCulture` at compare time (mirroring `StringComparer.CurrentCulture`); `Create` captures a specific culture:

```csharp
using System.Globalization;
using Bodu.Extensions;

var german = NaturalStringComparer.Create(CultureInfo.GetCultureInfo("de-DE"), ignoreCase: false);

german.Compare("ä2", "b10");                       // < 0 — "ä" collates with "a", and 2 < 10
NaturalStringComparer.Ordinal.Compare("ä2", "b10"); // > 0 — ordinal compares code points
```

Because the comparer also implements `IEqualityComparer<string?>`, any instance can key a `HashSet<string>` or `Dictionary<string, T>` — for example to deduplicate names that differ only by case under `OrdinalIgnoreCase`.

## Where to go next

- <xref:Bodu.Extensions.NaturalStringComparer> — the full API surface, including the exact tiebreak and hashing contracts.
- [WeekPattern](week-pattern.md) — another small, framework-style value primitive in `Bodu.Core`.
- [Core foundations](../topics/core-foundations.md) — the wider `Bodu.Core` toolbox.
- [Core documentation](../../docs/core/index.md) — concepts and getting started for `Bodu.Core`.
