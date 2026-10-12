# Bodu: Multi-Solution Architecture Recommendations

**Status:** Proposed architecture and implementation requirements; steps 2-4 of the adoption sequence (section 9) implemented, and the guidance half of step 6, as section 10 records; steps 1 (partly), 5 and 7 open  
**Scope:** Repository solution organisation, project selection, build configuration invariants, build/test orchestration  
**Primary constraint:** An `.slnx` file ONLY selects projects. No build behaviour or policy may depend on the solution in which a project is included.

## 1. Design principles

1. **One repository; one shared build policy.** Retain `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`, `.editorconfig` and Bodu's existing `bld/Bodu.props` aggregator and concern-specific imports.
2. **Solutions are selection manifests, not build policy.** Each `.slnx` contains solely `<Solution>` and `<Project Path="..." />` entries. No project-level build enable/disable flags, solution-specific build configurations, property overrides, or solution-item files.
3. **Project identity is stable.** A project must use the same `.csproj`, package references, source files, dependency graph and evaluated policy in every solution containing it.
4. **Intentional project-specific exceptions remain valid.** For example, tool projects may target an additional framework; exceptions must live in shared configuration or in the project itself, never in an `.slnx`.
5. **Both `net8.0` and `net10.0` remain validated.** Other supported target frameworks (such as `netstandard2.0` where relevant) are preserved from the existing shared framework policy.
6. **The authoritative inventory is explicit.** The existing `bodu.slnx` remains the master solution, and a generator/validator establishes consistent memberships across narrower solutions.
7. **Fast local development does not replace full release validation.** Narrow solutions reduce daily work; full library, test, sample, tooling and packaging coverage remains CI/release responsibility.

## 2. Proposed solution inventory

| File | Purpose | Membership rule |
|---|---|---|
| `bodu.slnx` | Master project inventory | Every supported project in the repository, subject to validating buildability in the required configurations |
| `bodu.libraries.slnx` | Production library build | Shipping library projects and explicitly required source dependencies; no tests, benchmarks, docs or samples |
| `bodu.tests.slnx` | Standard test suite | Runnable test projects, relevant sources, shared test harness and requisite test fixtures |
| `bodu.benchmarks.slnx` | Performance investigations | Benchmark projects and required source dependencies |
| `bodu.samples.slnx` | Samples and example verification | Sample projects, sample tests and required source dependencies |
| `bodu.tooling.slnx` | Specialist utilities | Build tooling, generators, tool tests, AOT smoke and other designated tooling |
| `bodu.core.slnx` | Core foundations | `Bodu.Core`, `Bodu.Collections`, `Bodu.Collections.Concurrent` and their ordinary tests |
| `bodu.security.slnx` | Security and hashing | `Bodu.Security.Cryptography`, `Bodu.IO.Hashing` and ordinary tests |
| `bodu.globalization.slnx` | Calendar and recurrence | `Bodu.Globalization.Calendar.*`, calendar regional data and `Bodu.Globalization.Recurrence` with appropriate tests/fixtures |
| `bodu.financial.slnx` | Numerics and finance | `Bodu.Numerics.*`, `Bodu.Financial.*`, exchange rate providers and their tests |
| `bodu.text.slnx` | Text and configuration | `Bodu.Text.*`, `Bodu.Extensions.Configuration.Text` and tests |
| `bodu.formats.slnx` | Binary formats and file I/O | `Bodu.Formats.*`, `Bodu.IO.Biff`, `Bodu.IO.Compound`, `Bodu.IO.Pst` and tests |

Introduce additional domain splits only after measuring the cost of each domain solution. Keep the initial inventory manageable.

## 3. Minimal `.slnx` format

Example `bodu.core.slnx`:

```xml
<Solution>
  <Project Path="Bodu.Core/src/Bodu.Core.csproj" />
  <Project Path="Bodu.Core/test/Bodu.Core.Test.csproj" />
  <Project Path="Bodu.Collections/src/Bodu.Collections.csproj" />
  <Project Path="Bodu.Collections/test/Bodu.Collections.Test.csproj" />
  <Project Path="Bodu.Collections.Concurrent/src/Bodu.Collections.Concurrent.csproj" />
  <Project Path="Bodu.Collections.Concurrent/test/Bodu.Collections.Concurrent.Test.csproj" />
</Solution>
```

No `<Build>` entries, configurations, target frameworks, solution items or build settings may appear. Use normalised repository-relative paths and deterministic ordering. Solution names never act as build inputs.

**Important:** MSBuild can follow `<ProjectReference>` dependencies even when those projects are not explicitly listed in the opened `.slnx`. Prefer explicitly listing meaningful dependencies for IDE navigation and validate transitive closure where practical. Project references themselves remain authoritative and must not be duplicated as dependencies in solution policy.

## 4. Shared build-policy invariants

- Root `Directory.Build.props` remains the common early import and imports `bld/Bodu.props`.
- Root `Directory.Build.targets` remains the common late import for targets, test dependency conventions, version handling and package validation.
- `bld/Bodu.props` retains its existing concern-scoped imports (`CompanyDetails`, `Copyright`, `Versioning`, `TargetFrameworks`, `Compiler`, `Packaging`, `SourceLink`, `Signing`, `Analyzers`).
- `Directory.Packages.props` remains the authority for NuGet dependency versions.
- `global.json` remains the authority for SDK selection, and CI must explicitly provision all required test runtimes.
- `.editorconfig` and centrally managed analyzer rules remain independent of solution membership.
- Runsettings are selected at execution time, never by solution contents or solution name.
- Project-local exceptions are permitted only when justified and must be identical in every solution. The existing `Bodu.CodeStyle` subtree is an example of a controlled, project-area exception that imports the shared root policy.
- Avoid solution-dependent conditions such as `$(SolutionName)`, `$(SolutionPath)` or `$(SolutionDir)` when they affect compilation, output paths, signing, packaging or validation.
- Do not introduce different `Directory.Build.props` files beside individual solutions to create solution-specific configurations.
- Do not rely on `Directory.Solution.props` / `.targets` for compilation policy; these affect solution-level command-line builds and are not a substitute for project-level imports in Visual Studio.

**Invariant:** Given the same project, Git revision, SDK, framework, configuration and invocation properties, the project's evaluated compilation policy and result must not vary with `.slnx` membership.

## 5. Generate and validate memberships

Maintain a single project inventory and an explicit project-classification manifest under `tools/solutions/`, with a deterministic generator that reads the master solution and project files. The manifest supplies matching rules and explicit exceptions; `.slnx` files never contain matching logic.

The generator must:

1. Discover projects using the master `.slnx` and scan for additional supported `.csproj` files so omitted new projects are detected.
2. Classify source, runnable test, test support, test fixture, benchmark, sample, tooling, documentation and AOT/smoke projects; `src/` and `.Test.` naming are useful hints but **not** sufficient by themselves.
3. Apply domain ownership and known cross-domain dependencies.
4. Emit deterministically sorted `.slnx` files in the repository root.
5. Fail on duplicates, unresolved project paths, unknown classifications, missing production projects, missing expected test projects and inconsistent membership.
6. Check that generated files match committed files without modifying the working tree in CI.
7. Reject any `.slnx` with a `<Build>` element, configuration override, or non-project entry.

Special cases in the current tree include Calendar build/tool projects, Calendar plug-in test fixtures, `Bodu.Test`, financial test-support projects, sample `.Test.` projects, benchmark projects, `docs/doc.csproj`, and `Bodu.Numerics.AotSmoke`.

## 6. Address legacy master-solution build exceptions

The current `bodu.slnx` uses `<Build Solution="Debug|*" Project="false" />` for some Calendar tooling, benchmarks, AOT and sample projects. This is incompatible with the new strict project-only model.

**Migration rule:** Remove those entries rather than carrying them into new solutions. First determine whether each affected project can build consistently in all required configurations. Fix such projects or classify them as specialised projects in purpose-specific solutions. Do not remove an exception from the master until the resulting Debug and Release build behaviour is understood. If a master solution is required to contain every project and build unconditionally, every included project must be made buildable for the configurations claimed to be supported.

## 7. Build and test orchestration

From the repository root:

```powershell
# Fast local domain build
 dotnet build bodu.core.slnx -c Debug

# Fast local domain tests (both target frameworks by default)
 dotnet test bodu.core.slnx -c Debug --settings bvt.runsettings

# All production libraries
 dotnet build bodu.libraries.slnx -c Release

# Full standard test set
 dotnet test bodu.tests.slnx -c Release --settings test.runsettings

# Full master inventory (when required)
 dotnet build bodu.slnx -c Release
```

A selected solution controls project membership only. Runsettings decide test categories; CLI options decide build configuration. Development-only `-f net10.0` may shorten a loop, but both supported TFM legs must be validated in CI/release. Where execution follows a separate successful build, `dotnet test ... --no-build` may avoid redundant build work.

Do not run parallel CI jobs that write to the same checkout's `obj` / `bin` paths. Use separate workspaces, or deliberately isolate output paths in centrally governed orchestration.

## 8. Acceptance criteria

- [ ] Every `.slnx` contains only `<Solution>` and `<Project Path="..." />` nodes. *Met by the eleven views, which the check enforces; `bodu.slnx` keeps its folders, solution items and legacy exclusions (section 10.2).*
- [x] Every listed project path resolves to a real `.csproj`.
- [x] Every supported project is represented in the authoritative inventory or a documented explicit exclusion.
- [x] Every shipping library appears in `bodu.libraries.slnx`.
- [x] Every runnable standard test appears in `bodu.tests.slnx`, with requisite helper projects and fixtures.
- [x] Domain solutions contain the intended project families and required references.
- [ ] No `.slnx` adds or changes MSBuild properties, target frameworks, configurations or package versions. *Met by the views; `bodu.slnx`'s fifteen Debug exclusions are configuration and remain until step 5.*
- [x] `Directory.Build.props`, `Directory.Build.targets`, `bld/` modules and package management remain the single shared policy.
- [x] The same project produces equivalent evaluated build settings from each containing solution under identical invocation inputs. *The check rejects any MSBuild file that reads a solution property, and `Bodu.Core` rebuilt through three solutions is byte-identical (section 10.4).*
- [x] Tests are validated under both .NET 8 and .NET 10; additional applicable framework targets remain supported. *The views change no framework; `dotnet test` through two views ran both legs (section 10.4), and CI's per-project matrix is unchanged.*
- [x] The generator is deterministic; CI fails if regenerated solution files differ from committed files.
- [ ] Existing master-solution Debug build exclusions are resolved or intentionally segregated without hidden overrides. *Segregated: no view carries them, and the master's are recorded exactly in `legacyMasterBuildExclusions`; not yet removed (section 10.5).*
- [x] Full regression, samples, tools, documentation and package validation remain assigned to appropriate CI/release workflows.
- [x] Build and test timing is recorded before and after migration, with no unsupported speedup claims. *Section 10.4, for local builds; CI is unchanged, so no CI speedup is claimed.*

## 9. Suggested adoption sequence

1. Baseline current master build and test duration by category, framework and project family; generate an MSBuild binlog where necessary.
2. Add the project classification manifest and generator, plus tests asserting the project-only `.slnx` contract.
3. Generate `bodu.libraries.slnx`, `bodu.tests.slnx`, and `bodu.core.slnx` first; validate policy parity.
4. Generate the remaining domain and purpose views, initially without changing full CI coverage.
5. Remove legacy solution-level `<Build>` exceptions after addressing intentionally excluded projects.
6. Switch local development guidance to domain solutions; retain full and release gates in CI.
7. Optimise CI partitions using measured bottlenecks and cross-domain dependency graph, rather than assuming solution splitting alone will reduce total CPU time.

## 10. Implementation

This section records what was built against sections 1-9, the decisions they left open, and what remains.

### 10.1 Where things live

| Path | Contents |
|---|---|
| `bodu.<view>.slnx` (repository root) | The eleven generated views of section 2. |
| `tools/solutions/solutions.json` | The classification manifest of section 5: kinds, domains, the views and what each selects, the matching rules, and every explicit exception with its reason. |
| `tools/solutions/generate-solutions.py` | The generator and validator (Python 3, standard library only): writes the views, or with `--check` verifies them without writing; `--list` prints each project's kind, domain and views. |
| `tools/solutions/tests/` | The generator's unit tests, over synthetic repositories (section 9, step 2). |
| `tools/solutions/README.md` | How to use it, and what it checks. |
| `.github/workflows/policy-gate.yml` | Runs the tests and `--check` on every pull request, as policy BODU-P016 (`bld/policy/POLICIES.md`). |

`tools/` already held the repository's hand-run generators; `bld/` holds build configuration and its gates. The manifest and generator follow section 5 into `tools/solutions/`, and the plan joins the other implementation plans in `plans/`.

### 10.2 Decisions

- **Classification.** A project's kind comes from an explicit exception if one matches, else from the first matching path rule (`samples/**/*.Test.csproj`, `samples/**`, `**/bench/**`, `**/test/*.Test.csproj`, `**/src/*.csproj`). The kind is then checked against the project file, as section 5 asks: a test must set `IsTestProject` and be named `*.Test.csproj`, a library must be packable, not an executable and not a test, and a `*.Test.csproj` must be a test kind. Twelve exceptions cover the special cases section 5 names: the calendar tool and MSBuild task and the source generator (`tooling`) and their tests (`tooling-test`), `Bodu.Financial.ExchangeRates.Testing` and `Bodu.Globalization.Calendar.Data.Test.Common` (`test-support`), the six plugin fixtures (`test-fixture`, one glob), `Bodu.Numerics.AotSmoke` (`aot-smoke`), `docs/doc.csproj` (`docs`), and `Bodu.Test`, which is a runnable test suite (it has tests of its own and CI runs it) but belongs to no domain.
- **Domains** are assigned by the longest matching project-name prefix, segment-aligned (`Bodu.Collections` claims `Bodu.Collections.Concurrent`, not `Bodu.CollectionsX`).
- **Reference closure.** Every view holds the transitive `ProjectReference` closure of what it selects, including references with `ReferenceOutputAssembly="false"` and item expansions such as the plugin test's `@(PluginFixture)`. Visual Studio builds only the projects in the opened solution, so this is what makes a view build in the IDE. The generated `bodu.core.slnx` therefore also holds `Bodu.Test`, which section 3's illustrative example omits, and every domain view holds the libraries of other domains it depends on (`bodu.formats.slnx` holds `Bodu.Collections`).
- **Domain views** hold every project their domain owns except its benchmarks and samples, which have views of their own: libraries, tests, test support, fixtures, and the domain's tooling (so `bodu.globalization.slnx` holds the rule-pack toolchain and `bodu.text.slnx` the source generator, as the `Bodu.Globalization.Calendar.*` and `Bodu.Text.*` rows of section 2 imply).
- **The tests view** holds every `*.Test.csproj` in the master, sample tests included, which is exactly the set `build-test.yml` and `release.yml` discover and run, so `dotnet test bodu.tests.slnx -c Release --settings test.runsettings` runs what CI runs. Sample tests are also in the samples view.
- **The tooling view** holds the `tooling`, `tooling-test`, `aot-smoke` and `docs` kinds.
- **`.slnx`, not `.slnf`.** A solution filter selects from a parent solution and inherits its configuration. Over today's master, `dotnet msbuild` on a filter reports "The project "Bodu.Text.Encoding.Benchmarks" is not selected for building in solution configuration "Debug|Any CPU"", so a benchmarks or tooling filter would build none of its own projects in Debug. Standalone views carry no configuration.
- **Master inventory.** The generator scans the tree for `*.csproj` and fails on any not in `bodu.slnx` and not in `notInMaster`. The scan found one: `Bodu.Core/bench/Bodu.Collections.Generic.Concurrent.Benchmarks`, which builds cleanly and was added to `bodu.slnx` (without a Debug exclusion). `notInMaster` records the `Bodu.CodeStyle/` subtree (its own solution and SDK) and the two hand-run generators under `tools/`, which stay out of the master so the release pack, which builds `bodu.slnx`, never builds them.
- **External references.** `Bodu.Core` loads the in-repo XML-documentation analyzer from source through `ProjectReference`s into `Bodu.CodeStyle/`. The master does not list those projects, so the manifest records the reference in `externalReferences`, with its reason, and the views do not follow it; any other reference out of the master fails the check.
- **The master is not yet minimal.** `bodu.slnx` keeps its folders and solution items for IDE navigation, and its fifteen Debug exclusions (section 6). The exclusions are recorded exactly in `legacyMasterBuildExclusions`, so the master cannot gain one and a removed one must leave the list; the views never carry them.
- **Solution-independent policy** (section 4) is enforced, not only stated: the check fails if any `.csproj`, `.props` or `.targets` reads `$(SolutionDir)`, `$(SolutionPath)`, `$(SolutionName)`, `$(SolutionFileName)` or `$(SolutionExt)`, or if a `Directory.Solution.props` or `.targets` appears. None does today.
- **Format.** Paths use forward slashes and sort segment by segment, case-insensitively, so a folder precedes its dotted siblings; files are UTF-8 without a BOM, LF line endings, two-space indent, and carry no comment (a comment is not a project node).

### 10.3 Inventory at adoption

The master lists 213 projects: 58 libraries, 58 tests, 3 tooling, 3 tooling tests, 2 test-support, 6 test fixtures, 1 AOT smoke app, 9 benchmarks, 67 samples, 5 sample tests and the docs project. The 58 libraries are the 56 packages `bld/release-manifest.txt` publishes plus the two it withholds (`Bodu.Financial.ExchangeRates.Xe` and `.Oanda`).

| View | Projects | Selected by |
|---|---:|---|
| `bodu.libraries.slnx` | 58 | kind `library` (the check fails if a library references a non-library) |
| `bodu.tests.slnx` | 139 | kinds `test`, `tooling-test`, `sample-test` (66 test projects) |
| `bodu.benchmarks.slnx` | 26 | kind `benchmark` (9 harnesses) |
| `bodu.samples.slnx` | 138 | kinds `sample`, `sample-test` |
| `bodu.tooling.slnx` | 16 | kinds `tooling`, `tooling-test`, `aot-smoke`, `docs` |
| `bodu.core.slnx` | 7 | domain `core` |
| `bodu.security.slnx` | 7 | domain `security` |
| `bodu.globalization.slnx` | 41 | domain `globalization` |
| `bodu.financial.slnx` | 52 | domain `financial` |
| `bodu.text.slnx` | 26 | domain `text` |
| `bodu.formats.slnx` | 17 | domain `formats` |

### 10.4 Measurements

Measured once each, at commit `3d6c11f`, in the Claude Code cloud container the views were built in: 4 vCPUs, 16 GB, Linux, the pinned SDK 10.0.401 with the .NET 8.0.31 runtime, NuGet cache warm. A cold build starts with no `obj` or `bin` (`git clean -fdX`) and no build servers running. Single runs, so the figures are indicative; they are not a CI baseline, and CI is unchanged.

**Cold Debug builds.** A domain view restores and compiles only its own projects and their references, each library for both frameworks.

| Solution | Projects | Cold Debug build |
|---|---:|---:|
| `bodu.slnx` (198 built; 15 excluded from Debug) | 213 | 497 s |
| `bodu.core.slnx` | 7 | 128 s |
| `bodu.security.slnx` | 7 | 170 s |
| `bodu.formats.slnx` | 17 | 145 s |
| `bodu.text.slnx` | 26 | 164 s |

**Every view builds in Debug.** Over the master's Debug outputs, each of the eleven views built with 0 warnings and 0 errors. That includes the first Debug build of all fifteen projects the master excludes from Debug: the eight excluded benchmarks (`bodu.benchmarks.slnx`), the calendar tool and MSBuild task, their tests and the AOT smoke app (`bodu.tooling.slnx`), and the three calendar samples (`bodu.samples.slnx`), among them `RulePackToolchain`, which runs the task it builds. Builds with nothing to compile took 9-45 s per view (`bodu.security.slnx` 9 s, `bodu.tests.slnx` 45 s).

Any commit changes every project's `AssemblyInformationalVersion`, because Source Link appends the commit SHA, so the first build after a commit recompiles every project, whichever solution builds it. Commits landed during the tooling and samples builds, and their times (97 s and 335 s) include that recompilation, so they say nothing about the views.

**Tests through a view.** `dotnet test bodu.core.slnx --settings bvt.runsettings` ran both frameworks in 33 s, all passing (`Bodu.Core.Test` 24,340 on net8.0 and 24,336 on net10.0, one skipped on each; `Bodu.Collections.Test` 3,599; `Bodu.Collections.Concurrent.Test` 912; `Bodu.Test` 130). `dotnet test bodu.globalization.slnx --settings bvt.runsettings` passed on both frameworks in 38 s, among them the 58 plugin-loader tests, which load the fixtures the view builds. The MiddleEast data pack's tests and the MSBuild task's tests are all in the Regression tier, so the BVT filter selected none of them.

**Output parity.** `Bodu.Core` rebuilt in Release through `bodu.slnx`, `bodu.core.slnx` and `bodu.libraries.slnx` (`-t:Rebuild` on its solution target) produced byte-identical assemblies: SHA-256 `802e6e34…` for net8.0 and `2da4c10c…` for net10.0 from all three. With `Deterministic` builds, identical bytes mean identical compiler inputs.

**Not measured.** Release builds of every view, the full `test.runsettings` tier through a view, ARM64, and Visual Studio's behaviour with each view open (the closure rule is what makes that work, and the check enforces it). Baselines by test category and framework (step 1) were not recorded.

### 10.5 What remains

- **Step 1.** Section 10.4 records local build timings; baselines by test category and framework are still to be taken.
- **Step 5.** Remove the master's fifteen Debug exclusions. Section 10.4 shows each excluded project builds in Debug through a view; what is left to confirm is Visual Studio's behaviour with the whole master loaded (the exclusions were made to keep the benchmarks and the toolchain out of ordinary Debug builds), then delete the `<Build>` elements and the `legacyMasterBuildExclusions` list together. Making the master itself selection-only (no folders or solution items) is a further, separate decision.
- **Step 6.** `CLAUDE.md` and `CONTRIBUTING.md` now name the views for day-to-day work; CI's build, test, coverage and release gates are unchanged and still derive their project lists from `bodu.slnx`.
- **Step 7.** CI partitioning is not changed. The views give it ready-made partitions, but splitting the test run across jobs that share no `obj`/`bin` would rebuild the shared libraries in each, so any change should follow measured job times.

## References

- Bodu repository: https://github.com/bslater/bodu
- MSBuild folder-wide configuration: https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory
- MSBuild solution customisation caveats: https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-solution-build
- `dotnet sln` command: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln
