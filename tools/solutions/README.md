# tools/solutions/

Generates the solution views at the repository root from the master solution, `bodu.slnx`. The
design, and the requirements it meets, are in
[`plans/multi-solution-architecture.md`](../../plans/multi-solution-architecture.md).

| File | Purpose |
| --- | --- |
| `solutions.json` | The classification: each project's kind and domain, the views and what each selects, and every deliberate exception, each with its reason. |
| `generate-solutions.py` | Reads `bodu.slnx`, every project file, and `solutions.json`; writes the views, or checks them. Python 3 standard library only. |
| `tests/` | The script's unit tests (`unittest`), run over synthetic repositories. |

## The views

A view is a selection manifest and nothing else: a `<Solution>` of `<Project Path="..." />` entries,
sorted, with no folders, configurations, build flags or solution items. A project builds exactly the
same way whichever solution it is built through, because nothing in a view can change how it builds,
and the script fails if any MSBuild file reads `$(SolutionName)` or its siblings.

| Solution | Holds |
| --- | --- |
| `bodu.slnx` | The master inventory: every project. Hand-maintained; keeps its folders, solution items and legacy Debug exclusions. |
| `bodu.libraries.slnx` | Every production library (shipping, or withheld from nuget.org), and nothing else. |
| `bodu.tests.slnx` | Every runnable test project CI runs (`*.Test.csproj`), with what they reference. |
| `bodu.benchmarks.slnx` | The BenchmarkDotNet harnesses, with the libraries they measure. |
| `bodu.samples.slnx` | The samples and the sample tests, with what they reference. |
| `bodu.tooling.slnx` | The calendar rule-pack tool and MSBuild task, the source generator, their tests, the AOT smoke app and the docs project. |
| `bodu.core.slnx` | Core foundations: `Bodu.Core`, `Bodu.Collections`, `Bodu.Collections.Concurrent`. |
| `bodu.security.slnx` | Security and hashing: `Bodu.Security.Cryptography`, `Bodu.IO.Hashing`. |
| `bodu.globalization.slnx` | Calendar and recurrence: `Bodu.Globalization.*`, the regional data packs, the plugin test fixtures and the rule-pack toolchain. |
| `bodu.financial.slnx` | Numerics and finance: `Bodu.Numerics.*`, `Bodu.Financial.*` and the exchange-rate providers. |
| `bodu.text.slnx` | Text and configuration: `Bodu.Text.*` (including the source generator) and `Bodu.Extensions.Configuration.Text`. |
| `bodu.formats.slnx` | Binary formats and file I/O: `Bodu.Formats.*`, `Bodu.IO.Biff`, `Bodu.IO.Compound`, `Bodu.IO.Pst`. |

A domain view holds every project its domain owns except the benchmarks and samples, which have
views of their own. Every view also holds every project its selection references, directly or
transitively, because Visual Studio builds only the projects in the opened solution: a domain view
therefore includes `Bodu.Test` and the libraries of other domains it depends on.

## Usage

```bash
python3 tools/solutions/generate-solutions.py           # write the views
python3 tools/solutions/generate-solutions.py --check   # write nothing; exit 1 if a view is stale or a rule is broken
python3 tools/solutions/generate-solutions.py --list    # each project's kind, domain and views
python3 -m unittest discover -s tools/solutions/tests -t tools/solutions/tests
```

The Policy Gate runs the tests and `--check` on every pull request (policy BODU-P016 in
`bld/policy/POLICIES.md`).

## Adding a project

1. Add it to `bodu.slnx`, as today.
2. Run the script. A project that follows the layout conventions (`<Project>/src/*.csproj`,
   `<Project>/test/*.Test.csproj`, `bench/`, `samples/`) and whose name starts with a domain prefix
   is classified by the rules; the script lists anything else, and you give it an `exceptions` entry
   with the reason. A project deliberately left out of the master goes in `notInMaster`, with the
   reason.
3. Commit the regenerated views with the project.

Never edit a view by hand, or through Visual Studio's Solution Explorer: change `bodu.slnx` or
`solutions.json` and regenerate. The check fails on any difference and names the projects that
differ.

## What the script checks

Every run, write or check, refuses to proceed past any of these:

- **Inventory.** Every project `bodu.slnx` lists exists, once. Every `*.csproj` in the tree is in
  `bodu.slnx` or matches a `notInMaster` entry, and no such entry is stale.
- **Classification.** Every project matches exactly one kind, and the kind agrees with the project
  file: a test sets `IsTestProject` and is named `*.Test.csproj`; a library is packable, is not an
  executable and is not a test. Every package `bld/release-manifest.txt` publishes is a library.
- **Domains.** Every project has one domain, from its longest matching name prefix or an exception.
- **References.** Every `ProjectReference` (including item expansions such as `@(PluginFixture)`)
  resolves to a project in `bodu.slnx`, or matches an `externalReferences` entry.
- **Views.** The libraries view holds only libraries; every master project is in at least one view;
  with `--check`, every committed view equals the generated text and holds only project entries.
- **Legacy.** The master's `<Build Solution="Debug|*" Project="false" />` exclusions are exactly
  the list in `legacyMasterBuildExclusions`, so none is added silently.
- **Policy.** No `.csproj`, `.props` or `.targets` reads `$(SolutionDir)`, `$(SolutionPath)`,
  `$(SolutionName)`, `$(SolutionFileName)` or `$(SolutionExt)`, and no `Directory.Solution.props` or
  `Directory.Solution.targets` exists.

## Why `.slnx` views rather than `.slnf` filters

A solution filter selects projects from a parent solution and inherits the parent's configuration,
including `bodu.slnx`'s Debug exclusions. `dotnet msbuild` over a filter of `bodu.slnx` reports
"The project "Bodu.Text.Encoding.Benchmarks" is not selected for building in solution configuration
"Debug|Any CPU"", so a benchmarks filter would build none of its benchmarks in Debug, and a tooling
filter none of its tooling. A standalone `.slnx` carries no configuration, so every project builds in
every configuration.
