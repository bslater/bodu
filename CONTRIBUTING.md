# Contributing to Bodu

Thank you for helping improve Bodu. Bug reports, documentation fixes, test vectors, and code changes are
all welcome. This guide covers how to report a problem, how to build and test the solution, and what a
pull request is reviewed against.

Everyone taking part is expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Reporting a problem

- **Security vulnerabilities:** do not open a public issue. Follow [SECURITY.md](SECURITY.md) instead.
- **Bugs:** open an issue with the **Bug report** form. The most useful reports name the package and
  version, give the smallest call sequence that reproduces the problem, and say what you expected
  instead. Documentation that contradicts the library's behaviour is a bug too.
- **Features and API changes:** open an issue with the **Feature request** form before writing code, so
  the shape of the API can be agreed first. Each package's `README.md` states its API-stability tier: a
  **Stable** package does not take breaking changes outside a major version, so a proposal that breaks
  one needs a strong case.

Search the [existing issues](https://github.com/bslater/bodu/issues) first; a comment on an existing
issue is better than a duplicate.

## Setting up

The repository builds with the exact .NET SDK pinned in [`global.json`](global.json), a .NET 10 SDK: the
sources use C# 14 and the `.slnx` solution format, which earlier SDKs do not support. Every library
targets `net8.0` and `net10.0`, so install the **.NET 8 runtime** as well. Without it the `net8.0` test
legs roll forward onto .NET 10 and say nothing about .NET 8.

Microsoft's `dotnet-install` script installs the pinned SDK beside any others:

```bash
./dotnet-install.sh --jsonfile global.json --install-dir <dir>
./dotnet-install.sh --runtime dotnet --channel 8.0 --install-dir <dir>
```

Then enable the repository's pre-push hook, which runs the same deterministic policy checks as CI before
anything leaves your machine:

```bash
git config core.hooksPath bld/hooks
```

## Building and testing

```bash
dotnet build bodu.slnx
dotnet test  bodu.slnx --settings bvt.runsettings      # the fast default tier
dotnet test  bodu.slnx --settings test.runsettings     # what CI runs
```

Tests are partitioned into tiers with `[TestCategory]`, and each `*.runsettings` file at the repository
root selects a set of them:

| Tier | Category | Contents | Run with |
|---|---|---|---|
| Smoke | `Smoke` | One happy-path test per primary type | `smoke.runsettings` |
| BVT | *(none)* | Structural, exception, property, and contract tests | `bvt.runsettings` |
| Regression | `Regression` | Published vector tables, catalogues, wide parameter sweeps | `test.runsettings`, `regression.runsettings` |
| Stress | `Stress` | Long, high-iteration loops | `stress.runsettings` |

A local run is a weaker signal than CI unless it matches CI. To match it:

- **Run `test.runsettings`, not `bvt.runsettings`.** CI includes the Regression tier.
- **Build with warnings as errors.** CI fails on any compiler or analyzer warning:
  `dotnet build <project> -c Release -p:TreatWarningsAsErrors=true`.
- **Install both runtimes** (see *Setting up*), and leave `DOTNET_ROLL_FORWARD` unset.
- **Use the pinned SDK.** A different SDK ships different analyzers and can disagree with CI in either
  direction. `dotnet --version` at the repository root names the one in use.

Some code runs only on ARM64, and CI runs the cryptography tests on an ARM64 runner as well; on x64 those
tests report inconclusive.

If you restore and build as separate steps, pass the same configuration to both
(`dotnet restore bodu.slnx -p:Configuration=Release`): a few projects, such as the benchmarks, are
excluded from the Debug configuration and are otherwise skipped by the restore.

## Making a change

[`CLAUDE.md`](CLAUDE.md) is the complete convention guide for this repository, for human and AI
contributors alike, and [`bld/policy/POLICIES.md`](bld/policy/POLICIES.md) lists the policies CI enforces.
The points that most often need attention in review:

- **Source files.** Every `.cs` file starts with the copyright banner from
  [`Bodu.sln.licenseheader`](Bodu.sln.licenseheader), uses a file-scoped namespace, and declares one
  top-level type. Folders under `src/` and `test/` map one to one to namespaces
  (`bld/check-folder-namespace-alignment.sh` checks this).
- **Documentation.** Every member, private ones included, carries XML documentation in US English in
  the style of the .NET base class library. Missing documentation fails the build.
- **Validation and messages.** Public members validate their arguments through the `ThrowHelper.ThrowIf…`
  helpers, and exception messages come from the project's `*ResourceStrings.resx`, never a string
  literal.
- **Punctuation.** Do not use an em-dash or an en-dash anywhere: code, comments, Markdown, or commit
  messages. Use a hyphen.
- **Tests.** Tests use MSTest, live in `<Project>/test/` in partial classes named after the member under
  test, follow `<Member>_When<Condition>_Should<Result>` naming with a `Verifies that ...` summary, and
  assert exceptions with `Assert.ThrowsExactly<T>`.
- **Bug fixes are test-first.** Add a regression test, see it fail against the current code, then fix it.
  Commit the failing test before the fix so the history shows the reproduction.

Keep each pull request to one logical change. A defect you notice along the way that is outside the
change's scope belongs in its own issue and its own pull request.

## Opening a pull request

1. Branch from `master` and make your change in focused commits.
2. Run the build and the tests the way CI does (see *Building and testing*).
3. Open the pull request against `master` and complete the template. Link the issue it resolves with
   `Fixes #<number>`.

Every pull request runs:

- **Build and Test** - every test project on Linux x64, and the cryptography tests on Linux ARM64, with
  warnings as errors.
- **Policy Gate** - the deterministic checks in `bld/check-policy.sh` and the folder and namespace
  alignment check.
- **Claude Policy Review** - an automated review against the judgment-based conventions in `CLAUDE.md`.
- **Build and Publish DocFX Sites** - the API reference and documentation site, with its link checks.

A change to a published package's public API is also compared against its last release by package
validation when the package is packed. An intended breaking change needs a reviewed reason and, for a
Stable package, a major version.

## License

Bodu is released under the [MIT License](LICENSE). By submitting a contribution you confirm that you
have the right to license it under the MIT License, and you agree that it is licensed under those terms.
