# IO.Compound Samples

A console application demonstrating the `Bodu.IO.Compound` package — the OLE2 / Compound
File Binary structured-storage container. Run it with:

```bash
dotnet run --project samples/IO.Compound/Bodu.IO.Compound.Samples.CompoundBasics
```

The sample is offline and deterministic: in-memory containers plus two committed fixtures
(`golden-v3.cfb`, 8 KB; `sample1.doc`, 29 KB — a real Word 97-2003 file).

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.IO.Compound.Samples.CompoundBasics` | Bottom-up authoring with `CompoundStorageBuilder` (nested storages, streams) and byte-exact read-back through `CompoundFile`; OLE property sets authored with `SummaryInformationBuilder` and read from a real `.doc` via `TryGetSummaryInformation`; `IsCompoundFile` signature detection plus the `CompoundBuildOptions.Version` v3/v4 sector-size knob; and a recursive walk of the real `.doc`'s storage tree with raw stream reads | `Bodu.IO.Compound` |
