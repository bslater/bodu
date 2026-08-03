# Bodu.Globalization.Calendar.Build

MSBuild integration for Bodu notable-date rule packs: `NotableDatePack` items compile XML/JSON
documents to sealed `.bcal` binary packs during build, incrementally, via the `bodu-calendar` tool.

```xml
<ItemGroup>
  <NotableDatePack Include="rules\holidays.xml" />
  <NotableDatePack Include="rules\corporate.json" ResolverDir="rules\shared" />
</ItemGroup>
```

Each item compiles to `<Filename>.bcal` under the intermediate output path and is copied beside the
application output, ready for `NotableDateResourceLoader.LoadBinary`. Compilation is incremental —
an unchanged document is never recompiled, and the format's byte-stable output keeps downstream
up-to-date checks sound. An invalid document fails the build with the tool's stable `BODU-CAL-*`
diagnostic lines in the log.

| Override | Purpose |
|---|---|
| `NotableDatePackOutputPath` | The directory receiving compiled packs (default: `$(IntermediateOutputPath)bcal\`). |
| `NotableDatePackCopyToOutput` | Set `false` to keep packs out of the project output directory. |
| `ResolverDir` (item metadata) | A directory whose `<name>.xml` / `<name>.json` files satisfy the document's imports. |
| `BoduCalendarTaskAssembly` / `BoduCalendarToolDll` | Advanced: repoint the task or tool assemblies (used by the repository's own integration tests). |

The package ships as build tooling only (`DevelopmentDependency`): the task assembly under
`tasks/`, the `.targets` under `build/`, and the framework-dependent `bodu-calendar` binaries under
`tools/`, invoked through `dotnet exec --roll-forward Major` so any newer installed runtime works.
See the [binary rule packs guide](../docs/guides/calendar/binary-rule-packs.md) for the format
contract.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Build/test/Bodu.Globalization.Calendar.Build.Test.csproj --settings regression.runsettings
```

The tests drive real `dotnet build` invocations over a fixture project consuming the `.targets`.

## License

MIT. © Bodu Pty. Ltd.
