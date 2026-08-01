---
title: Bodu.Text.Filtering — Getting started
---

# Bodu.Text.Filtering — Getting started

Unfamiliar with terms like *action*, *evaluation mode*, or *deciding pattern*? Read
[Core concepts](concepts.md) first.

## Install

```bash
dotnet add package Bodu.Text.Filtering
```

Targets `net8.0`. The package has a single dependency on `Bodu.Core`.

## Minimal samples

### Filter a list with include/exclude globs

```csharp
using Bodu.Text.Filtering;

var filter = TextFilter.Build(
[
    TextFilterPattern.Include("error*"),
    TextFilterPattern.Include("warn*"),
    TextFilterPattern.Exclude("*debug*"),
]);

string[] lines = ["error: disk full", "warn: retrying", "error-debug-trace", "info: started"];
var kept = filter.FilterToList(lines);
// → "error: disk full", "warn: retrying"
```

### Parse gitignore-style lines

```csharp
var filter = TextFilter.Parse(
[
    "# log selection",
    "error*",
    "!*debug*",       // '!' flips the pattern to an exclude
]);

bool match = filter.IsMatch("error-7");        // → true
bool vetoed = filter.IsMatch("error-debug");   // → false
```

### Ordered rules with re-inclusion (gitignore semantics)

```csharp
var ordered = new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins };
var filter = TextFilter.Parse(["!*.log", "important.log"], ordered);

filter.IsMatch("app.log");        // → false — excluded by "!*.log"
filter.IsMatch("important.log");  // → true  — re-included by the later rule
filter.IsMatch("readme.txt");     // → true  — unmatched values are included
```

### Ask which pattern decided

```csharp
var result = filter.Evaluate("app.log");
// → result.Decision == TextFilterDecision.Excluded, result.Pattern is the "!*.log" rule
```

### Read the telemetry

```csharp
var stats = filter.GetStatistics();
// → stats.ItemsEvaluated, stats.ItemsAccepted, stats.ItemsExcluded,
//   stats.Patterns[i].HitCount — decisions credited to each pattern
```

## Fluent assembly with the builder

```csharp
var filter = new TextFilterBuilder()
    .AddInclude("{error,warn}*")                 // brace alternation → two cheap prefix matchers
    .AddIncludeRegex(@"^audit-\d+$")
    .AddExclude("*retry*")
    .Build();
```

## Where to go next

- **[Bodu.Text.Filtering guides](../../guides/text-filtering/index.md)** — the grammar, the modes, and telemetry in depth.
- **[Core concepts](concepts.md)** — vocabulary refresher.
- **[Introduction](index.md)** — type map and scenario index.
- **[Runnable samples](../../samples/text-filtering.md)** — the FilteringTour console sample.
- **[Bodu.Text.Filtering API reference](xref:Bodu.Text.Filtering)** — full type-by-type docs.
