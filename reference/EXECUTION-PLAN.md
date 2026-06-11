# Execution plan — finish the Bodu Bencode & TOML test suites (new-session entry point)

**Branch: `claude/relaxed-rubin-m62s2x`.** Goal: bring `Bodu.Text.Bencode` and `Bodu.Text.Toml` to System.Text.Json-parity test coverage. This file is the short "how to start"; the full detail is in [`REMAINING-TEST-PLAN.md`](REMAINING-TEST-PLAN.md).

## Use these three inputs together
1. **The committed test projects** — `Bodu.Text.Bencode/test/` and `Bodu.Text.Toml/test/`. The existing files (B1, B2a, T1, B2b, T2a) are the **canonical style/pattern exemplars**: copy their conventions (file banner, file-scoped namespace, `Verifies that …` summaries, `Assert.ThrowsExactly<T>` block lambdas, `[DynamicData]`+KAT with `ArgumentNullException.ThrowIfNull(kat);`, Regression tagging). Mirror Bencode's `BencodeSerializerTests.*.cs` when writing the TOML equivalents.
2. **[`REMAINING-TEST-PLAN.md`](REMAINING-TEST-PLAN.md)** — the detailed spec: per-pass scenario catalogs (Bodu-named test methods), the **probed library contracts** (the expected outputs/oracle — §4), conventions (§2), and the workflow + hard-won lessons (§1).
3. **[`system-text-json-tests/`](system-text-json-tests/)** — the full S.T.J test corpus (reference only). Use `Common/` for the real `[Fact]`/`[Theory]` bodies and exact `[InlineData]` values; `System.Text.Json.Tests/` for reader/writer + DOM + feature tests. Map S.T.J scenarios → Bodu via plan §3.

## Remaining passes (in order)
| Pass | Scope | Spec |
|---|---|---|

## Per-pass workflow (do this every time)
```bash
# build + run (per-project csproj; SDK 8 pinned, slnx needs SDK 9+)
dotnet test  Bodu.Text.<Lib>/test/Bodu.Text.<Lib>.Test.csproj --settings bvt.runsettings        -v q --nologo
dotnet test  Bodu.Text.<Lib>/test/Bodu.Text.<Lib>.Test.csproj --settings regression.runsettings -v q --nologo
# WARNINGS: only a CLEAN rebuild surfaces analyzer warnings — incremental builds hide them
dotnet build Bodu.Text.<Lib>/test/Bodu.Text.<Lib>.Test.csproj -v q --nologo --no-incremental 2>&1 \
  | grep -E "Bodu\.Text\.<Lib>/(src|test)/[^/]+\.cs.*: warning"   # MUST be empty (ignore the repo baseline elsewhere)
```
1. Read the src types + existing tests for the pass; **don't duplicate**.
2. **Probe before asserting:** when a contract is unclear, write a throwaway `[TestMethod]` that logs actual output to `/tmp`, run it, read it, delete it. Never invent a contract.
3. If the impl violates its own documented contract, fix the src (note it). Don't add new features except in RICH.
4. Green BVT + Regression, 0 net-new warnings on a clean rebuild, no scratch files left.
5. Commit scoped to the one project; push `git push -u origin claude/relaxed-rubin-m62s2x` (backoff retry). Update the plan §0 status table.

## Where we are now
Committed & green: B1, B2a, T1, B2b (p1+p2), T2a, B3, T2b, T3, RICH, GUARD.
- Bencode: **BVT 542 / Regression 759** (complete).
- TOML: **BVT 765 / Regression 1103** (complete).
All passes are done. The S.T.J → Bodu translation is documented in [`STJ-TRACEABILITY.md`](STJ-TRACEABILITY.md).

## Watch-outs (full detail in plan §1)
- **Incremental builds hide analyzer warnings** — always do the final check with `--no-incremental`.
- **`ArgumentNullException.ThrowIfNull(kat);`** must be the first line of any KAT-parameter test method (else CA1062).
- The container **suspends during idle and kills background subagents** — keep work on disk progressively; prefer foreground or small fast passes; salvage + re-verify after any interruption.
- Commit messages end with the session URL; **never** put a model identifier in commits/PRs/code. Don't push to `master`; no PR unless asked.
