---
title: Compiling packs in MSBuild
---

# Compiling packs in MSBuild

`Bodu.Globalization.Calendar.Build` compiles notable-date documents to sealed `.bcal` packs as part of `dotnet build`, incrementally, using the bundled `bodu-calendar` compiler. It is a **development dependency**: the package contributes a `.targets` file, an MSBuild task assembly, and the tool binaries, and adds no runtime reference to the consuming project. Validation failures surface as build errors carrying the same stable `BODU-CAL-*` lines the [command-line tool](bodu-calendar-cli.md) prints.

## Pattern 1 — reference the package and declare packs

```xml
<ItemGroup>
  <PackageReference Include="Bodu.Globalization.Calendar.Build" Version="…" PrivateAssets="all" />
</ItemGroup>

<ItemGroup>
  <NotableDatePack Include="rules\holidays.xml" />
  <NotableDatePack Include="rules\corporate.json" ResolverDir="rules\shared" />
</ItemGroup>
```

`PrivateAssets="all"` keeps the build package out of your own package's dependency graph (the package is also marked as a development dependency, so NuGet applies this by default; stating it makes the intent explicit). Each `NotableDatePack` item is one document; the only item metadata the targets read is:

| Metadata | Meaning |
|---|---|
| `ResolverDir` | Optional directory whose `<name>.xml` / `<name>.json` files satisfy the document's imports, consulted before the bundled catalogues — the same semantics as the tool's `--resolver-dir`. |

Each item compiles to `$(NotableDatePackOutputPath)<Filename>.bcal` and, by default, is copied into the project output directory beside the application.

## The `CompileNotableDatePack` task

The `.targets` file wires every item to <xref:Bodu.Globalization.Calendar.Build.CompileNotableDatePack>, a `ToolTask` that runs `dotnet exec --roll-forward Major <tool.dll> compile <Input> -o <Output> [--resolver-dir <ResolverDir>]`:

| Parameter | Required | Value the targets pass |
|---|---|---|
| `Input` | yes | `%(NotableDatePack.Identity)` — the document path. |
| `Output` | yes | `$(NotableDatePackOutputPath)%(NotableDatePack.Filename).bcal`. The task creates the output directory before running the tool. |
| `ToolDll` | yes | `$(BoduCalendarToolDll)` — the `bodu-calendar` assembly executed through the `dotnet` host. |
| `ResolverDir` | no | `%(NotableDatePack.ResolverDir)`; omitted when empty. |

The `dotnet` host is resolved from the `PATH`; `--roll-forward Major` lets the net8.0 tool run on any newer installed runtime. Standard output and standard error are logged at high importance, so the compile summary and the diagnostic lines appear even at minimal verbosity, and a non-zero exit fails the build.

## Properties and override points

| Property | Default | Purpose |
|---|---|---|
| `BoduCalendarTaskAssembly` | `<package>/tasks/netstandard2.0/Bodu.Globalization.Calendar.Build.dll` | The task assembly loaded by `UsingTask`. Override to run a locally built task (the repository's own integration test does this). |
| `BoduCalendarToolDll` | `<package>/tools/Bodu.Globalization.Calendar.Tool.dll` | The compiler assembly. Override to pin a different tool build. |
| `NotableDatePackOutputPath` | `$(MSBuildProjectDirectory)\$(IntermediateOutputPath)bcal\` | Directory receiving the compiled packs. Its default is computed at target time (in `_InitNotableDatePackProperties`) because `IntermediateOutputPath` is defined after the package targets are imported. |
| `NotableDatePackCopyToOutput` | `true` | When `true`, each compiled pack is added as a `None` item with `CopyToOutputDirectory=PreserveNewest` and `Link=<Filename>.bcal`, so it lands in the output directory root. Set `false` to keep packs in the intermediate folder only. |

```xml
<PropertyGroup>
  <NotableDatePackOutputPath>$(MSBuildProjectDirectory)\obj\packs\</NotableDatePackOutputPath>
  <NotableDatePackCopyToOutput>false</NotableDatePackCopyToOutput>
</PropertyGroup>
```

## Incremental build behaviour

The `CompileNotableDatePacks` target declares `Inputs="@(NotableDatePack)"` and `Outputs="@(NotableDatePack->'$(NotableDatePackOutputPath)%(Filename).bcal')"`, so MSBuild skips each `(document, pack)` pair whose output is newer than its input — a rebuild with an unchanged document does not invoke the tool at all. Because the compiler's output is byte-stable for a given document (the pack embeds a SHA-256 of its payload and no timestamps), downstream up-to-date checks stay sound after a recompile.

Two consequences to know:

- **Changing an import is not tracked.** Only the document itself is an input. If a file under `ResolverDir` changes, touch the document or delete the `.bcal` to force a recompile.
- **Renaming a document orphans its pack.** The old `.bcal` stays in the output path until a clean; the copy-to-output step only copies packs that correspond to current items.

The target runs `BeforeTargets="AssignTargetPaths"` so the compiled packs can join the normal content copy in the same build. The repository's `CompileNotableDatePackIntegrationTests` drives a fixture project through this pipeline and asserts both the incremental skip and that an invalid document fails the build with its diagnostic code.

## Pattern 2 — consume the compiled pack at run time

With `NotableDatePackCopyToOutput` left at `true`, the pack sits beside the executable under its document's file name:

```csharp
using Bodu.Globalization.Calendar;

string packPath = Path.Combine(AppContext.BaseDirectory, "holidays.bcal");

NotableDateResource resource;
using (FileStream stream = File.OpenRead(packPath))
    resource = NotableDateResourceLoader.LoadBinary(stream);

var service = new NotableDateService(resource);
IReadOnlyList<NotableDate> thisYear = service.Resolve(2026, "US");
```

Loading a pack performs no parsing and no validation — the build already did both — which is the trim- and AOT-friendly path described in [Binary rule packs](../binary-rule-packs.md). To embed the pack instead of copying it, add the compiled file as an `EmbeddedResource` from `$(NotableDatePackOutputPath)` in a target that runs `AfterTargets="CompileNotableDatePacks"`, and load it from a manifest resource stream.

## Troubleshooting

**`error MSB4062: The task "Bodu.Globalization.Calendar.Build.CompileNotableDatePack" could not be loaded`.** `BoduCalendarTaskAssembly` does not point at the task DLL — usually a stale override left in a `Directory.Build.props`.

**The build fails with a `BODU-CAL-*` line.** The document is invalid; run `bodu-calendar lint <document>` for the same diagnostics interactively, then rebuild.

**A pack is not recompiled after editing a shared catalogue.** See the incremental caveat above — imports are not inputs.

**`dotnet` not found.** The task launches the `dotnet` host from the `PATH`; on a build agent that installs the SDK to a private location, add it to `PATH` for the build step.

## Where to go next

- **[The bodu-calendar CLI](bodu-calendar-cli.md)** — the compiler the task invokes, its exit codes, and the diagnostic line format.
- **[Binary rule packs](../binary-rule-packs.md)** — what the format guarantees.
- **[Calendar validation diagnostics](../validation-diagnostics.md)** — the `BODU-CAL-*` catalogue.
- **[Bodu.Globalization.Calendar tooling introduction](../../../docs/calendar-tooling/index.md)**
- **[Globalization & Calendars guides](../../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
