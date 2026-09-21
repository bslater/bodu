# Bodu.Text.Filtering.Samples.FilteringTour

A four-scenario tour of the `Bodu.Text.Filtering` engine: compiling include/exclude pattern sets,
parsing gitignore-style rule lines with ordered last-match-wins evaluation, the glob grammar with
its cost-tier classification and deciding-pattern diagnostics, and the built-in telemetry with a
per-decision observer.

Everything runs offline against small in-code corpora, so the output is deterministic.

```bash
dotnet run --project samples/Text.Filtering/Bodu.Text.Filtering.Samples.FilteringTour
```

## Scenario 1 — IncludeExcludeBasics

**Intent.** Show the default `AnyMatch` semantics — the Ant / MSBuild include-exclude set model —
and the include-all default when a filter has no include patterns.

**What it does.** Builds a filter from two include globs and one exclude glob, streams a small
log-line corpus through `Filter`, then builds an exclude-only filter to show that everything
passes unless an exclude vetoes it. Matching is ordinal and case-insensitive by default, so
`WARN:` matches `warn*`.

**What to expect.**

```text
--- Include/exclude sets - the AnyMatch model ---
  What   : Compiles two includes and one exclude into a filter, runs a five-line corpus through it with one line per
           decision path, then builds an exclude-only filter to show what happens when no include is declared at
           all.
  Why    : This is the model Ant and MSBuild item groups use, and its two rules are worth stating explicitly because
           getting them backwards is the usual filtering bug. First, includes are an OR-set: matching any one is
           enough, so adding an include widens the filter. Second, an exclude always wins over an include,
           regardless of declaration order - which is what lets a broad include be paired with narrow carve-outs
           instead of being rewritten into a precise pattern. The third rule is the one people trip over: declaring
           zero includes means include everything, so a filter flips from blocklist to allowlist the moment its
           first include appears. Compiling once matters too - Build does the pattern analysis, so applying the
           filter to a million values does not redo it a million times.
  Expect : Three of the five lines are kept. The debug line is dropped although it matched an include, because the
           exclude vetoes; the info line is dropped because nothing included it - two different reasons for the same
           outcome. The upper-case WARN line is kept, since matching is case-insensitive by default. The
           exclude-only filter then accepts a value no pattern mentions, which is the blocklist behaviour that
           declaring an include would have turned off.

  kept    -> error: disk full
  kept    -> warn: retrying request
  kept    -> WARN: cache miss
  (three of five: 'error-debug-trace' matched an include but the exclude vetoed it, and 'info' matched no include at all)

  report.txt  with exclude-only filter -> True  (expected True - with no includes declared the filter is a blocklist, so an unmentioned value passes)
  scratch.tmp with exclude-only filter -> False  (expected False - adding a single include here would flip every unmatched value to rejected)
```

**APIs demonstrated.** `TextFilter.Build`, `TextFilterPattern.Include` / `Exclude`,
`TextFilter.Filter`, `TextFilter.IsMatch`.

## Scenario 2 — ParseAndOrderedRules

**Intent.** Show `TextFilter.Parse` reading raw lines with the gitignore file conventions, and the
`LastMatchWins` mode where the last matching rule decides — so a later include re-admits a value an
earlier exclude rejected, and an allowlist is expressed with a leading exclude-everything rule.

**What it does.** Parses a comment-bearing rule list under
`TextFilterEvaluationMode.LastMatchWins`, probes the re-inclusion and unmatched-default behaviors,
then parses the `["!*", "error*", "!*debug*"]` allowlist shape.

**What to expect.**

```text
--- Parsing gitignore lines and the LastMatchWins ordered model ---
  What   : Parses four gitignore-style lines - comments, a '!' exclude and a later include - in ordered mode and
           tests three values against them, then builds an allowlist out of the same mechanism.
  Why    : LastMatchWins is a different model from the include/exclude set, not a variation on it. There is one
           ordered list rather than two groups, and the last rule that matches decides - so a later include can
           re-admit a value an earlier exclude rejected. That re-inclusion idiom is precisely what the AnyMatch
           model cannot express, because there an exclude is final. The other half of the model is that an unmatched
           value is included, which is why gitignore files list what to ignore rather than what to keep. Together
           those two rules make the rule list read top to bottom like firewall rules, and they are also why an
           allowlist has to start by excluding everything.
  Expect : 'app.log' is rejected by the exclude, but 'important.log' is kept even though the same exclude matches
           it, because the include is declared later - that is the whole point of the mode. 'readme.txt' matches
           nothing and is kept, which is the unmatched-means-included rule. The allowlist then inverts the default
           with a leading '!*' and carves an exception back out, giving three different outcomes decided by three
           different lines.

  app.log       -> False  (expected False - its only matching rule is the exclude)
  important.log -> True  (expected True - it matches the same exclude, but a later include overrides it; an AnyMatch set could not express this)
  readme.txt    -> True  (expected True - unmatched values are included, the default that makes a gitignore file a list of what to ignore)

  error1      -> True  (expected True - last matching rule is the 'error*' include)
  error-debug -> False  (expected False - it matched the include too, but '!*debug*' comes after it)
  info        -> False  (expected False - only the leading '!*' matches, which is how an allowlist inverts the default)
```

**APIs demonstrated.** `TextFilter.Parse`, `TextFilterOptions.Mode`,
`TextFilterEvaluationMode.LastMatchWins`.

## Scenario 3 — GlobsAndCostTiers

**Intent.** Tour the glob grammar — `{a,b}` alternation, character classes, escapes — alongside a
regex pattern, and show the diagnostic surfaces that reveal which pattern decided each outcome.

**What it does.** Builds a mixed filter (`{error,warn}*` expands at build time into two cheap
prefix matchers; the class pattern routes through the general matcher; the regex sits in the most
expensive tier), evaluates five values with `Evaluate`, lists every matching pattern for an
overlapping value with `GetMatchingPatterns`, and matches an escaped-metacharacter literal.

**What to expect.**

```text
--- Glob grammar and cost tiers ---
  What   : Builds a filter mixing brace alternation, a character class, a regex and a contains-glob, evaluates five
           values to see which pattern decided each, asks for every pattern matching an overlapping value, and shows
           a backslash escape demoting a glob to a literal.
  Why    : Two things are happening at build time that make this practical at scale. The braces are expanded into
           separate patterns, so alternation costs nothing per value rather than being re-parsed on every call. And
           each pattern is classified into the cheapest strategy its shape allows - a literal equality, a prefix or
           suffix comparison, a substring search, the general wildcard matcher, or a regex - and evaluated in that
           order, so a regex is only consulted after every cheaper pattern has already failed. The diagnostic
           surfaces exist because a filter that gives the wrong answer is nearly impossible to debug from a boolean:
           Evaluate returns which pattern decided, and GetMatchingPatterns returns all of them, which is how you
           find a rule shadowed by another.
  Expect : Three distinct decision kinds across the five values, each naming the pattern responsible. 'job-7x' is
           NotIncluded with no pattern at all, because nothing matched - a different outcome from being vetoed, and
           the distinction matters when tuning rules. GetMatchingPatterns reports two patterns for the overlapping
           value where Evaluate reported only the deciding one, since it deliberately does not short-circuit. The
           escaped pattern matches the literal three characters and nothing else.

  warn: slow disk  -> Included     decided by +wildcard:{error,warn}*
  job-42           -> Included     decided by +wildcard:job-[0-9][0-9]
  job-7x           -> NotIncluded  decided by (no pattern)
  metric.http.p99  -> Included     decided by +regex:^metric\.[a-z]+\.p\d{2}$
  error-retry-8    -> Excluded     decided by -wildcard:*retry*
  (three decision kinds: matched an include, vetoed by an exclude, and NotIncluded with no pattern - nothing matched at all)

  error-retry-8 matches 2 pattern(s): +wildcard:{error,warn}*, -wildcard:*retry*  (Evaluate named only the deciding pattern - this surface does not short-circuit, so it finds rules shadowed by others)
  literal 'a*b' -> True, 'axb' -> False  (expected True then False - escaping the star also demotes the pattern to the cheapest tier, a single equality check)
```

**APIs demonstrated.** `TextFilter.Evaluate`, `TextFilterResult.Decision` / `Pattern`,
`TextFilter.GetMatchingPatterns`, `TextFilterPatternKind.Regex`.

## Scenario 4 — TelemetryAndObserver

**Intent.** Show the always-on statistics counters and the optional per-decision observer hook.

**What it does.** Filters a deterministic 200-value corpus, prints the reconciled decision buckets
(`Evaluated == Accepted + Excluded + NotIncluded`) and the per-pattern hit counts (credited to the
*deciding* pattern), then attaches an `ITextFilterObserver` that logs each value an exclude vetoed.

**What to expect.**

```text
--- Telemetry and the observer hook ---
  What   : Runs a 200-value corpus built from eight fixed shapes through the filter, prints the statistics snapshot
           and the per-pattern hit counts, then resets the counters and attaches an observer that reports each
           vetoed value.
  Why    : A filter that is wrong in production is hard to diagnose after the fact, because the evidence is the
           values that did not arrive. The counters are always on for that reason and cost essentially nothing.
           Their most useful signal is the per-pattern hit count, which credits only the pattern that actually
           decided an outcome: a pattern sitting near zero over a large corpus is either redundant or shadowed by an
           earlier rule, and that is not visible from reading the pattern list. The observer answers the other
           question - not how many, but which - and it is opt-in because seeing every decision is only worth the
           callback when you are actively debugging.
  Expect : The buckets reconcile exactly: evaluated equals accepted plus excluded plus not-included, with 100
           accepted, 50 vetoed and 50 that matched no include. Those last two are counted separately although both
           were rejected, because they call for different fixes - one means an exclude is too broad, the other that
           an include is too narrow. The observer then names the two vetoed values out of three, which the counts
           alone would not have told you.

  evaluated 200, accepted 100, excluded 50, not-included 50 (kept 100)  (the buckets reconcile: 200 == 100 + 50 + 50, and 'excluded' vs 'not-included' distinguishes a veto from no include matching)
    +wildcard:{error,warn}* decided 100 outcomes
    -wildcard:*debug*  decided 50 outcomes
  (only the deciding pattern is credited - one sitting near zero over a large corpus is redundant or shadowed by an earlier rule)

  (the observer below sees every decision and reports only the vetoes - which values, not just how many)
  observer: 'warn-debug-10' vetoed by -wildcard:*debug*
  observer: 'error-debug-11' vetoed by -wildcard:*debug*
```

**APIs demonstrated.** `TextFilter.GetStatistics`, `TextFilterStatistics`,
`TextFilterPatternStatistics.HitCount`, `TextFilter.ResetStatistics`, `TextFilter.Observer`,
`ITextFilterObserver`.
