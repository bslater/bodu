# Bodu.Globalization.Calendar.Tool

`bodu-calendar` — the command-line compiler and lint for Bodu notable-date rule packs.

```bash
dotnet tool install -g Bodu.Globalization.Calendar.Tool

bodu-calendar lint    holidays.xml                    # print every BODU-CAL-* diagnostic
bodu-calendar compile holidays.xml -o holidays.bcal   # compile to a sealed binary pack
bodu-calendar info    holidays.bcal                   # summarize a compiled pack
```

## Commands

| Command | Purpose | Exit codes |
|---|---|---|
| `lint <file.xml\|file.json> [--resolver-dir <dir>]` | Validate a document through the collect-mode loader and print every diagnostic (`[Severity] BODU-CAL-*: message`) without compiling. | `0` clean, `1` errors, `2` usage |
| `compile <file> [-o <out.bcal>] [--resolver-dir <dir>]` | Validate and compile to a sealed `.bcal` binary pack; nothing invalid can reach a pack. The output defaults to the input path with a `.bcal` extension. | `0` compiled, `1` failure, `2` usage |
| `info <pack.bcal>` | Print the pack's format version, resource identity, content counts, and payload digest. | `0`, `1` on a bad pack |

`--resolver-dir` supplies a directory whose `<name>.xml` / `<name>.json` files satisfy document
imports; the bundled common catalogues (`global-core`, `christian-western`, …) always resolve as a
fallback.

Packs load at run time with `NotableDateResourceLoader.LoadBinary` — no parsing, no validation,
trim/AOT-friendly. See the [binary rule packs guide](../docs/guides/calendar/binary-rule-packs.md)
for the format contract and the [validation diagnostics guide](../docs/guides/calendar/validation-diagnostics.md)
for the complete code catalogue.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Tool/test/Bodu.Globalization.Calendar.Tool.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
