---
title: The bodu-calendar CLI
---

# The `bodu-calendar` CLI

`Bodu.Globalization.Calendar.Tool` is a .NET tool that validates notable-date XML / JSON documents with the stable `BODU-CAL-*` diagnostics and compiles them to sealed `.bcal` binary packs. It runs the *same* load pipeline as the runtime — `NotableDateResourceLoader.TryLoad` / `TryLoadJson` in collect mode — so a document that passes `lint` loads at run time and vice versa, and the `Bodu.Globalization.Calendar.Build` MSBuild task is a thin wrapper over its `compile` verb. This page is the verb-by-verb reference; the [tooling introduction](../../../docs/calendar-tooling/index.md) covers installation and positioning, and [Binary rule packs](../binary-rule-packs.md) covers when a pack is worth it.

```bash
dotnet tool install --global Bodu.Globalization.Calendar.Tool
bodu-calendar --help
```

## Verbs and options

```text
bodu-calendar lint    <file.xml|file.json> [--resolver-dir <dir>]
bodu-calendar compile <file.xml|file.json> [-o <out.bcal>] [--resolver-dir <dir>]
bodu-calendar info    <file.bcal>
```

| Verb | What it does | Writes |
|---|---|---|
| `lint` | Loads and validates the document, printing every diagnostic, without compiling. | stdout: diagnostics, then a one-line summary. |
| `compile` | Validates and, when clean, writes the sealed pack. | stdout: the compile summary; diagnostics and the failure summary go to **stderr**. |
| `info` | Reads a compiled pack's header and content summary. | stdout: one summary line. |

| Option | Applies to | Meaning |
|---|---|---|
| `-o`, `--output <file>` | `compile` | The pack path. **Default:** the input path with its extension changed to `.bcal` (`rules/holidays.xml` → `rules/holidays.bcal`). Missing directories are created. |
| `--resolver-dir <dir>` | `lint`, `compile` | A directory whose `<name>.xml` / `<name>.json` files satisfy the document's imports, consulted **before** the bundled common catalogues. Without it, imports resolve against the bundled catalogues only. |
| `-h`, `--help`, `help` | — | Prints usage to stderr and exits 2. |

Verbs are case-insensitive (`LINT` works). The document format is chosen by extension — `.xml` or `.json`; anything else is a usage error. Exactly one input file is accepted; an unknown option or a second positional argument prints usage.

## Exit codes

| Code | Constant | When |
|---|---|---|
| `0` | `CalendarTool.ExitSuccess` | The document is valid (`lint`), the pack was written (`compile`), or the pack was read (`info`). |
| `1` | `CalendarTool.ExitFailure` | Validation produced at least one error, the input file does not exist, or (`info`) the pack is malformed. |
| `2` | `CalendarTool.ExitUsage` | No verb, an unknown verb, a missing argument, an unknown option, or an unsupported document extension. |

A document with only warnings is valid: `lint` prints the warnings and exits `0`.

## The diagnostic line format

Each diagnostic prints on its own line as `[Severity] CODE: message`, where `CODE` is the stable `BODU-CAL-*` identifier from the [validation diagnostics catalogue](../validation-diagnostics.md). The runs below are real output from `CalendarTool.Run` against documents in the repository's test fixtures:

```text
$ bodu-calendar lint easter.xml
easter.xml: OK — resource 'fixture.easter' is valid (0 diagnostic(s)).
[exit 0]

$ bodu-calendar lint invalid-unknown-algorithm.xml
[Error] BODU-CAL-ALGORITHM: Notable date 'x', rule 'r': algorithm key 'not-a-real-algorithm' is not recognized.
invalid-unknown-algorithm.xml: FAILED — 1 error(s), 1 total diagnostic(s).
[exit 1]

$ bodu-calendar lint invalid-unknown-policy-ref.xml
[Error] BODU-CAL-ADJREF: Rule 'r' references undefined adjustment policy 'missing-policy'.
invalid-unknown-policy-ref.xml: FAILED — 1 error(s), 1 total diagnostic(s).
[exit 1]

$ bodu-calendar lint missing.xml
File not found: missing.xml          (stderr)
[exit 1]

$ bodu-calendar lint holidays.bcal
Unsupported document extension '.bcal': expected .xml or .json.          (stderr)
[exit 2]
```

The summary line's shape is fixed: `<file>: OK — resource '<id>' is valid (<n> diagnostic(s)).` on success and `<file>: FAILED — <errors> error(s), <total> total diagnostic(s).` on failure, so a log scraper can match on `: OK —` / `: FAILED —` and an editor can match `^\[(Error|Warning)\] (BODU-CAL-[A-Z]+): `.

## Compiling to `.bcal`

```text
$ bodu-calendar compile easter.xml -o out/easter.bcal
Compiled 'fixture.easter' -> out/easter.bcal (202 bytes, sha256 f66fff55565b24f511f85fbd18f74667c03fd22b44d251602a96d5aeed8adde3).
[exit 0]

$ bodu-calendar info out/easter.bcal
out/easter.bcal: format v1, resource 'fixture.easter' (schema 1.0), 2 notable date(s), 0 adjustment policy(ies), payload sha256 f66fff55565b24f511f85fbd18f74667c03fd22b44d251602a96d5aeed8adde3.
[exit 0]

$ bodu-calendar compile invalid-unknown-algorithm.xml -o out/bad.bcal
[Error] BODU-CAL-ALGORITHM: Notable date 'x', rule 'r': algorithm key 'not-a-real-algorithm' is not recognized.          (stderr)
invalid-unknown-algorithm.xml: FAILED — 1 error(s), 1 total diagnostic(s).                                               (stderr)
[exit 1]
```

The digest printed by `compile` and `info` is the SHA-256 of the pack payload that the format stores in its header (bytes 8–39, after the `BCAL` magic and the little-endian format version), so the two lines agree by construction and the value is stable for a given document — byte-identical output is what makes the MSBuild integration's up-to-date check sound. Nothing is written when validation fails.

A document that imports shared catalogues by name — `<Use resource="christian-western" />` and the like — resolves them from the catalogues embedded in the runtime. Point `--resolver-dir` at a folder of your own `<name>.xml` / `<name>.json` files to satisfy private imports, or to shadow a bundled catalogue with a local copy of the same name.

## Loading a compiled pack

At run time the pack loads without re-parsing or re-validating, through either of two equivalent entry points:

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource resource;
using (FileStream stream = File.OpenRead("out/easter.bcal"))
    resource = NotableDateResourceLoader.LoadBinary(stream);          // the loader's binary entry point

using (FileStream stream = File.OpenRead("out/easter.bcal"))
    resource = NotableDateBinaryResource.Read(stream);                // the low-level symmetric surface

var service = new NotableDateService(resource);
```

<xref:Bodu.Globalization.Calendar.NotableDateBinaryResource> is the same writer the tool uses — `Write(resource, stream)` produces the identical bytes from an already-built resource, including the bundled catalogues — and `Read(stream)` is its inverse. A truncated or tampered pack fails with <xref:Bodu.Globalization.Calendar.NotableDateBinaryFormatException>, which is also what `info` reports as exit `1`.

## Pattern — fail the build on lint errors

Because errors exit `1` and diagnostics are stable, the tool drops straight into CI. Lint every rule document, and let the non-zero exit fail the job:

```bash
# GitHub Actions / any POSIX shell — lint every document, fail on the first error.
dotnet tool install --global Bodu.Globalization.Calendar.Tool
for doc in rules/*.xml rules/*.json; do
  bodu-calendar lint "$doc" --resolver-dir rules/shared || exit 1
done
```

```yaml
# GitHub Actions step
- name: Lint notable-date rule packs
  run: |
    dotnet tool install --global Bodu.Globalization.Calendar.Tool
    bodu-calendar lint rules/holidays.xml --resolver-dir rules/shared
    bodu-calendar compile rules/holidays.xml -o artifacts/holidays.bcal --resolver-dir rules/shared
```

For a project-integrated alternative that runs on every `dotnet build`, use the MSBuild task — see [Compiling packs in MSBuild](msbuild-integration.md).

## In-process use

The console host is a one-liner over `CalendarTool.Run(string[] args, TextWriter output, TextWriter error)`, which returns the exit code. Reference the tool project and call it directly to lint in a test or a custom build step without spawning a process:

```csharp
using Bodu.Globalization.Calendar.Tool;

var stdout = new StringWriter();
var stderr = new StringWriter();

int exitCode = CalendarTool.Run(["lint", "rules/holidays.xml"], stdout, stderr);

if (exitCode != CalendarTool.ExitSuccess)
    Console.Error.WriteLine(stdout.ToString() + stderr);
```

## Where to go next

- **[Compiling packs in MSBuild](msbuild-integration.md)** — the `NotableDatePack` items and `CompileNotableDatePack` task over the same compiler.
- **[Binary rule packs](../binary-rule-packs.md)** — format guarantees and the version-1 layout.
- **[Calendar validation diagnostics](../validation-diagnostics.md)** — the `BODU-CAL-*` catalogue the lint output uses.
- **[Bodu.Globalization.Calendar tooling introduction](../../../docs/calendar-tooling/index.md)** — installation and positioning of the two tooling packages.
- **[Globalization & Calendars guides](../../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
