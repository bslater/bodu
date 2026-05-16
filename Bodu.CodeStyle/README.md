# Bodu.CodeStyle

Roslyn analyzer and code fix package that enforces Bodu's XML documentation formatting policy.

## Projects

| Project | Role |
|---|---|
| `Bodu.CodeStyle.XmlDocumentation.Core` | Roslyn-free formatter engine. Token model, layout, wrapper, JSON config reader. |
| `Bodu.CodeStyle.XmlDocumentation.Analyzers` | Reports `BODU1xxx` diagnostics on misformatted documentation comments. |
| `Bodu.CodeStyle.XmlDocumentation.CodeFixes` | Provides the "Format XML documentation comment" code fix, including Fix All. |
| `Bodu.CodeStyle.XmlDocumentation` | NuGet packaging project producing `Bodu.CodeStyle.XmlDocumentation.nupkg`. |

## Diagnostic IDs

Bodu diagnostics use a 4-digit `BODU####` identifier. The thousands digit denotes the analyzer family; the
remaining digits identify a specific rule. The scheme mirrors how Roslyn's own `CAxxxx` / `IDExxxx` IDs are
organised, so suppressing or re-targeting a family in `.editorconfig` stays straightforward.

| Range | Family | Status |
|---|---|---|
| `BODU0xxx` | Reserved — analyzer infrastructure (config errors, internal diagnostics) | reserved |
| `BODU1xxx` | **XML documentation** | shipping |
| `BODU2xxx` | Member ordering | deferred |
| `BODU3xxx` | Naming conventions | deferred |
| `BODU4xxx` | Performance | deferred |
| `BODU5xxx` | Security | deferred |
| `BODU6xxx` | Design / API shape | deferred |
| `BODU9xxx` | Whitespace / brace style | deferred |

### BODU1xxx — XML documentation

Each documented XML tag has its own diagnostic ID so individual tags can be silenced or re-targeted in
`.editorconfig` independently. All BODU1xxx descriptors share the `Documentation` category, so a single
`dotnet_analyzer_diagnostic.category-Documentation.severity = …` entry silences or re-targets the entire
XML-doc family in one line.

| ID | Tag | Family |
|---|---|---|
| `BODU1001` | `<summary>` | block (forced multiline) |
| `BODU1002` | `<remarks>` | block |
| `BODU1003` | `<para>` | block |
| `BODU1004` | `<example>` | block |
| `BODU1005` | `<code>` | block |
| `BODU1006` | `<list>` | block |
| `BODU1007` | `<item>` | block |
| `BODU1008` | `<description>` | block |
| `BODU1009` | `<term>` | block |
| `BODU1010` | `<param>` | single-line-when-short |
| `BODU1011` | `<typeparam>` | single-line-when-short |
| `BODU1012` | `<returns>` | single-line-when-short |
| `BODU1013` | `<exception>` | single-line-when-short |
| `BODU1014` | `<value>` | single-line-when-short |
| `BODU1015` | `<c>` | inline atomic |
| `BODU1016` | `<see>` | inline atomic |
| `BODU1017` | `<paramref>` | inline atomic |
| `BODU1018` | `<typeparamref>` | inline atomic |
| `BODU1040` | _(none — cross-cutting)_ | prose / prefix / indent changes outside any tag |

Reserved future ranges within BODU1xxx:

| Range | Purpose |
|---|---|
| `BODU1019` – `BODU1039` | Reserved for additional documented tags (e.g. `<seealso>`, `<note>`). |
| `BODU1041` – `BODU1099` | Reserved for granular cross-cutting splits (separate IDs for prefix vs indent vs line length). |
| `BODU1100` – `BODU1199` | Required tags (e.g. missing `<summary>` on a public method). |
| `BODU1200` – `BODU1299` | Tag / element ordering inside doc comments. |
| `BODU1300` – `BODU1399` | `cref` / `paramref` / `typeparamref` reference validity. |
| `BODU1400` – `BODU1499` | Content quality (empty tags, redundant prose). |

## Formatting policy

Defaults match the Bodu codebase conventions:

- `<summary>`, `<remarks>`, `<para>`, `<example>`, and `<list>` are block tags that emit on their own lines.
- `<param>`, `<typeparam>`, `<returns>`, `<exception>`, and `<value>` stay single-line when short enough.
- `<c>`, `<see>`, `<paramref>`, and `<typeparamref>` are inline atomic tokens and are never split across lines.
- Lines wrap at `120` characters by default, breaking only between tokens.
- If a single atomic token exceeds the configured maximum, the line is allowed to exceed it rather than corrupt
  the content.

## Configuration

The analyzer reads policy from three layers, applied in order:

1. **Defaults in code** — `XmlDocFormatPolicyDefaults.CreateBoduDefaults()`.
2. **JSON additional file** — `bodu.xmldocstyle.json` added as `<AdditionalFiles>` in the consumer csproj. The
   JSON shape mirrors `XmlDocFormatOptions` (`maxLineLength`, `documentationPrefix`, `blockTags`, `inlineTags`,
   `forceMultilineTags`, `singleLineWhenShort`, `neverSplitTagContent`, `tagPolicies`).
3. **`.editorconfig` scalar overrides** — keys such as `bodu_xmldoc_max_line_length`, plus the standard
   `dotnet_diagnostic.BODU####.severity` per individual rule and `end_of_line`. To silence or re-target the
   whole XML-doc family at once, use
   `dotnet_analyzer_diagnostic.category-Documentation.severity = …` instead of listing each rule.

## Building

```bash
cd Bodu.CodeStyle
dotnet build Bodu.CodeStyle.sln -c Release
dotnet test Bodu.CodeStyle.sln -c Release
dotnet pack Bodu.CodeStyle.XmlDocumentation/Bodu.CodeStyle.XmlDocumentation.csproj -c Release -o ./artifacts
```

The packaging project produces a NuGet that lays out the analyzer, code fix, and Core DLLs under
`analyzers/dotnet/cs/`, matching Microsoft's documented analyzer NuGet layout.

## Consuming from the rest of the Bodu repository

`bld/Bodu.props` adds a `PackageReference` to `Bodu.CodeStyle.XmlDocumentation` for every consuming project
(controlled by the `BoduCodeStyleAnalyzers` MSBuild property, default `true`). The analyzer is restored from the
`bodu-local` source declared in the repo-root `NuGet.config`, which points at `./local-packages/`.

The packed `.nupkg` is committed to `local-packages/` so a fresh clone restores out-of-the-box without an
explicit pack step.

### Updating the analyzer in `Bodu.Core` and the other library projects

```bash
# 1. Make your changes under Bodu.CodeStyle/ and verify them.
cd Bodu.CodeStyle
dotnet test Bodu.CodeStyle.sln -c Release
cd ..

# 2. Repack into local-packages/ and evict NuGet's global cache for the package
#    (because the version stays at 1.0.0, the cache would otherwise serve a stale copy).
bash bld/pack-codestyle-analyzer.sh         # bash / Linux / macOS / WSL / Git Bash
# pwsh bld\pack-codestyle-analyzer.ps1       # PowerShell on Windows or cross-platform
# bld\pack-codestyle-analyzer.cmd            # cmd.exe shim that calls the .ps1

# 3. Force-restore the consumer project so it picks up the freshly extracted DLLs.
dotnet restore Bodu.Core/src/Bodu.Core.csproj --force

# 4. Rebuild the consumer — the new analyzer is now loaded by Roslyn.
dotnet build Bodu.Core/src/Bodu.Core.csproj -c Release

# 5. Commit the updated local-packages/Bodu.CodeStyle.XmlDocumentation.1.0.0.nupkg
#    alongside your analyzer source changes.
git add Bodu.CodeStyle/ local-packages/
git commit
```

If you have Visual Studio open while doing the above, close and reopen the solution after step 2 — VS keeps the
analyzer DLL loaded in memory and will not pick up the new payload until the AppDomain is recycled.

CI runs the equivalent `dotnet pack` step in `.github/workflows/build-test.yml` before restoring the library
projects, so PRs don't need a freshly packed `.nupkg` committed — but committing it after a local change keeps
the IDE / local `dotnet build` experience smooth.

The `Bodu.CodeStyle/Directory.Build.props` sets `BoduCodeStyleAnalyzers=false` so the analyzer projects never
analyse themselves and never form a circular package reference.

## Status

- Milestone 1 — Core formatter: shipped.
- Milestone 2 — `BODU1001` analyzer + code fix + Fix All: shipped.
- Milestone 3 — Configuration (defaults + JSON + `.editorconfig`): shipped.
- Milestone 4 — Member ordering: deferred (analyzer + model).
- Milestone 5 — CLI: deferred.
- Milestone 6 — VSIX: deferred. The architecture keeps formatting logic in
  `Bodu.CodeStyle.XmlDocumentation.Core` so a future VSIX can reuse the same engine.
