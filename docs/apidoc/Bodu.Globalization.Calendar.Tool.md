---
uid: Bodu.Globalization.Calendar.Tool
---

![Bodu.Globalization.Calendar.Tool](~/images/hero-calendar-tool.svg)

## Purpose

**Bodu.Globalization.Calendar.Tool** is the `bodu-calendar` command-line tool (`dotnet tool install --global Bodu.Globalization.Calendar.Tool`): it validates notable-date XML / JSON documents with the stable `BODU-CAL-*` diagnostics (`lint`), compiles them to sealed `.bcal` binary rule packs (`compile`), and inspects compiled packs (`info`). The namespace holds the tool's in-process entry point, <xref:Bodu.Globalization.Calendar.Tool.CalendarTool>, which the console host, the `Bodu.Globalization.Calendar.Build` MSBuild task, and the test suite all share — so the build-time compiler and the command line are one code path.

The package is published as **Preview**. It depends on `Bodu.Globalization.Calendar` and performs every load through <xref:Bodu.Globalization.Calendar.NotableDateResourceLoader> in collect mode, resolving imports first from an optional `--resolver-dir` and then from the bundled <xref:Bodu.Globalization.Calendar.CommonNotableDateResources>.

## Static documentation

- **[Calendar tooling introduction](~/docs/calendar-tooling/index.md)** — the verbs, options, exit codes, and diagnostic line format, and the companion `Bodu.Globalization.Calendar.Build` MSBuild integration.
- **[Binary rule packs](~/guides/calendar/binary-rule-packs.md)** — when to compile a pack, the format guarantees and layout, and loading packs with `LoadBinary`.
- **[Calendar validation diagnostics](~/guides/calendar/validation-diagnostics.md)** — every `BODU-CAL-*` code the tool prints.
- **[Globalization & Calendars topic overview](~/docs/topics/globalization-and-calendars.md)** — where the toolchain sits beside the runtime, its companions, and the data packs.

## Key types

- <xref:Bodu.Globalization.Calendar.Tool.CalendarTool> — the static command surface. `Run(string[] args, TextWriter output, TextWriter error)` dispatches the `lint` / `compile` / `info` verbs (case-insensitive), writes normal output to `output` and errors plus usage help to `error`, and returns the process exit code. The exit codes are exposed as constants: `ExitSuccess` (`0`), `ExitFailure` (`1` — a validation or input failure), and `ExitUsage` (`2` — a usage error). Diagnostics print one per line as `[Severity] CODE: message`.

## Example

```csharp
using Bodu.Globalization.Calendar.Tool;

// Drive the tool in process — the same path the console host and the MSBuild task use.
int exitCode = CalendarTool.Run(
    ["compile", "rules/holidays.xml", "-o", "packs/holidays.bcal", "--resolver-dir", "rules/shared"],
    Console.Out,
    Console.Error);

if (exitCode != CalendarTool.ExitSuccess)
{
    // ExitFailure: the document reported BODU-CAL-* errors (printed to the error writer) or the input was missing.
    // ExitUsage:   a bad verb, option, or extension; usage help was printed to the error writer.
    Console.Error.WriteLine($"bodu-calendar exited with {exitCode}");
}
```

## Notes

- **Preview.** The verb set and option names may still change between releases without a major-version bump.
- **No public API beyond `Run`.** The tool is meant to be installed and invoked, or hosted through the `Bodu.Globalization.Calendar.Build` package; consumers who want to compile packs from code should use <xref:Bodu.Globalization.Calendar.Builder.NotableDateDocumentBuilder>`.SaveBinary` or <xref:Bodu.Globalization.Calendar.NotableDateBinaryResource>`.Write` directly rather than referencing this assembly.
