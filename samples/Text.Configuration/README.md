# Text.Configuration Samples

Console applications demonstrating the `Bodu.Text.Configuration` document/resolver pipeline
and its `Microsoft.Extensions.Configuration` bridge. Each sample is a standalone project; run
one with:

```bash
dotnet run --project samples/Text.Configuration/<SampleName>
```

Every sample is offline and deterministic: the inputs are small committed `Data/` files.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Configuration.Samples.ConfigCascade` | `Parse` vs `ParseWithDiagnostics` (Relaxed profile collects instead of throwing), the path-targeted resolve cascade (`[*]` defaults + glob-section overrides, three targets compared), typed view getters, `unset` under `TreatAsLiteral` vs `RemoveEffectiveValue` plus the canonical profile option sets, and `Save` with comments preserved | `Bodu.Text.Configuration` |
| `Bodu.Extensions.Configuration.Text.Samples.BridgeHosting` | `AddTextConfigurationFile` resolving the `.boduconfig` cascade for a `targetPath` at load time, `AddTomlFile` flattening TOML tables to colon-separated keys (with `optional:` handling), and `AddConfigurationOptions<T>` binding a section into DI-resolved `IOptions<T>` | `Bodu.Extensions.Configuration.Text`, `Bodu.Text.Configuration`, `Bodu.Text.Toml` |
