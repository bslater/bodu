# Bodu.Globalization.Calendar.Samples.RulePackToolchain

The rule-pack toolchain: `Bodu.Globalization.Calendar.Build` (the `CompileNotableDatePack` MSBuild
task and `NotableDatePack` items) driving the `bodu-calendar` tool from
`Bodu.Globalization.Calendar.Tool`.

**Unlike every other sample, the interesting part happens at build time.** The csproj declares
`rules/company-holidays.xml` as a `NotableDatePack` item; the task lints and compiles it with the
tool and copies the sealed `.bcal` pack beside the application. What the program does is only the
consumer side — loading that pack with `NotableDateResourceLoader.LoadBinary`. If you want to see the
toolchain work, watch the build, or delete `company-holidays.bcal` from the output directory and
rebuild.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.RulePackToolchain
```

Offline and deterministic, and the compile step is incremental — a rebuild with no edit to the
document does no work.

## The one thing a consumer writes

```xml
<ItemGroup>
  <NotableDatePack Include="rules\company-holidays.xml" />
</ItemGroup>
```

A NuGet consumer writes exactly that and nothing else: the package's `build/*.targets` is imported
automatically and finds the task and the tool inside the package. This sample carries extra
plumbing — an explicit `<Import>` of the `.targets` and overrides for `BoduCalendarTaskAssembly` and
`BoduCalendarToolDll` — because inside this repository the task and tool are *projects* rather than a
restored package. That plumbing is repository-specific and is commented as such in the csproj.

## Scenarios

### Loading the .bcal pack the build compiled

**Intent.** Show why the toolchain exists: the authoring format and the deployment format are
different jobs.

**What it does.** Loads the compiled pack, resolves 2026 from it, then parses the same XML at runtime
and compares the two. Prints the size of each representation.

**Expected output.** Both resolve the same three holidays and the comparison reports `True`. The pack
is far smaller (255 bytes against 1,946) and needs no schema validation to read. The pack's presence
is itself the evidence that the task ran during the build — if it were missing the program says so
and stops.

**APIs.** `NotableDateResourceLoader.LoadBinary`, `NotableDateResourceLoader.Load`,
`NotableDateService.Resolve`.

## Layout

```
  Program.cs                           # locates the compiled pack, runs the scenario
  SampleConsole.cs                     # the what/why/expect banner every scenario opens with
  Scenarios/LoadingACompiledPack.cs    # pack versus XML, same answer
  rules/company-holidays.xml           # the NotableDatePack input the build compiles
```

## Equivalent NuGet references

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Build      # development dependency; brings the tool
dotnet tool install --global Bodu.Globalization.Calendar.Tool   # only if you want the CLI directly
```

## Related

- `Bodu.Globalization.Calendar.Samples.ValidationLint` — the diagnostics the tool reports, reached
  through the library's validation API instead of the CLI.
- `Bodu.Globalization.Calendar.Samples.CustomCalendar` — authoring the same kind of document in code
  with `NotableDateDocumentBuilder`, which is how this sample's XML was generated.
