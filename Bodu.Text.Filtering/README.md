# Bodu.Text.Filtering

A high-performance include/exclude filtering engine for lists of text values. A set of glob
(wildcard) and regex patterns compiles once into a `TextFilter`, which then classifies each
pattern by evaluation cost and runs the cheapest matchers first — so filtering 100k+ items
against 10–100+ patterns stays fast even when some patterns are regexes.

```csharp
using Bodu.Text.Filtering;

var filter = TextFilter.Parse(new[]
{
    "error*",        // include everything starting with "error"
    "warn*",         // ... or "warn"
    "!*debug*",      // but exclude anything containing "debug"
});

foreach (var line in filter.Filter(lines))
    Console.WriteLine(line);
```

## Design lineage

This library deliberately adopts the best-established designs from well-known filtering and
globbing engines rather than inventing new semantics:

| Design | Borrowed from |
|---|---|
| Unordered include/exclude sets: empty includes ⇒ include-all; excludes always veto | Ant `DirectoryScanner`, MSBuild item globs, `Microsoft.Extensions.FileSystemGlobbing` |
| Ordered rules where the **last matching rule wins**, `!` negation, `#` comments | gitignore / ESLint ignore files |
| Compile many patterns at once and extract literal/prefix/suffix strategies so cheap matchers run before regex; report *which* patterns matched | Rust `globset` (ripgrep) |
| Fluent builder (`AddInclude` / `AddExclude`) over a compiled matcher | `Microsoft.Extensions.FileSystemGlobbing.Matcher` |
| Glob grammar: `*`, `?`, `[abc]` / `[a-z]` / `[!abc]`, `{a,b}` alternation, `\` escape | Java `PathMatcher`, minimatch, shell glob |
| Filtering statistics surface | ripgrep `--stats` |

## Semantics

An item is evaluated against the compiled pattern set according to the configured
`TextFilterEvaluationMode`:

- **`AnyMatch`** (default — Ant/MSBuild-style sets): an item is accepted iff *(the include set
  is empty OR at least one include matches)* AND *no exclude matches*. Group matching is an
  order-independent OR, so the engine is free to evaluate patterns cheapest-first.
- **`LastMatchWins`** (gitignore-style ordered rules): the last matching rule's action decides;
  an item that matches no rule is **included** (gitignore-faithful). Allowlists are expressed
  with a leading exclude-everything rule: `["!*", "error*", "!*debug*"]` keeps only `error*`
  items except those containing `debug`.

Matching is whole-string (use `*abc*` for contains-style matching) and, by default, ordinal
and case-insensitive; case sensitivity is configurable per filter and overridable per pattern.

## Glob grammar

| Syntax | Meaning |
|---|---|
| `*` | zero or more characters |
| `?` | exactly one character |
| `[abc]`, `[a-z]` | one character from the set / range |
| `[!abc]` | one character *not* in the set |
| `{a,b}` | alternation, expanded at build time (`{error,warn}*` compiles into two prefix matchers) |
| `\x` | literal `x` (escapes `*?[]{}\!#` metacharacters) |

Anything richer is expressed as a `TextFilterPatternKind.Regex` pattern. Regexes prefer the
linear-time `RegexOptions.NonBacktracking` engine and always carry a match timeout; a timed-out
pattern fails safe (a timed-out include does not admit the item; a timed-out exclude still
vetoes it).

## Cost tiers

At build time each glob is classified so evaluation runs cheapest-first (the `globset` idea):

`MatchAll` → `Literal` → `Prefix` / `Suffix` / `PrefixAndSuffix` → `Contains` → general
wildcard (iterative two-pointer matcher) → `Regex`.

## Indicative performance

BenchmarkDotNet (short job) on the development container, filtering a 100,000-value synthetic
corpus per invocation; the baseline evaluates one compiled `Regex` per pattern per value:

| Pattern count | Mixed-tier `TextFilter` | Per-pattern compiled regex | Speed-up |
|---:|---:|---:|---:|
| 10 | ~15 ms | ~36 ms | ~2.4× |
| 50 | ~73 ms | ~170 ms | ~2.3× |
| 100 | ~132 ms | ~443 ms | ~3.4× |

All `TextFilter` passes allocate nothing per value, and attaching a no-op
`ITextFilterObserver` was within measurement noise. Reproduce with:

```bash
dotnet run -c Release --project Bodu.Text.Filtering/bench/Bodu.Text.Filtering.Benchmarks.csproj -- --filter '*TextFilter*'
```

## Telemetry

`TextFilter` keeps always-on counters (items evaluated / accepted / excluded / not-included,
per-pattern hit counts, regex timeouts, opt-in timing) exposed via `GetStatistics()`, and an
optional `ITextFilterObserver` invoked per decision with the deciding pattern — a single null
check when unattached.
