# Bodu Samples

Runnable, self-contained sample applications demonstrating the Bodu packages the way a
consumer would actually compose them. Every sample:

- **runs fully offline** — no network access, no accounts, no API keys. Exchange-rate
  samples read committed static data files instead of calling live feeds, and each one
  carries a clearly fenced comment block showing exactly how to switch to the real
  web-based provider.
- **is deterministic** — running a sample twice prints the same output, so the samples
  double as executable documentation and as CI smoke tests.
- references the library projects directly via `ProjectReference`, so the samples always
  compile against the current source. Each sample's README lists the equivalent
  `dotnet add package` commands for NuGet consumers.

## Running a sample

```bash
dotnet run --project samples/financial/Bodu.Financial.Samples.OfflineRates
```

All samples are members of `bodu.slnx`, so `dotnet build bodu.slnx` builds them and any
API drift breaks the build immediately.

## Conventions

Sample code is intentionally held to a *different* standard than the shipping libraries.
The samples keep the parts of the repository conventions that make them readable as
documentation, and drop the parts that only matter for shipped binaries:

**Kept:** the standard file header banner, file-scoped namespaces, one public type per
file, an XML `<summary>` on every type and method, and generous inline `//` commentary
explaining *why* each step exists.

**Dropped:** resx-backed exception messages (samples don't ship — plain string literals
are fine), the analyzer/style build gates (`RunAnalyzers=false`), and exhaustive
`<remarks>`/`<exception>` documentation.

Test projects that accompany a sample (for example
`Bodu.Financial.Samples.CustomProvider.Test`) follow the full repository test
conventions — they run in CI alongside the library test suites.

## Index

| Domain | Samples |
|---|---|
| Financial | [`samples/financial/`](financial/README.md) — money arithmetic, offline exchange rates, caching, aggregation, DI, custom providers |
