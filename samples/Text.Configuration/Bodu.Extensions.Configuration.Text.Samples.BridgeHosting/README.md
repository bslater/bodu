# Bodu.Extensions.Configuration.Text.Samples.BridgeHosting

The bridge package `Bodu.Extensions.Configuration.Text` in action: Bodu text formats flowing
into the standard `Microsoft.Extensions.Configuration` pipeline. Three scenarios cover a
`.boduconfig` cascade as an `IConfiguration` source (resolved per target path), a TOML file
flattened to configuration keys, and the final hop into strongly typed `IOptions<T>` via
dependency injection. Everything runs offline against the committed `Data/` files.

```bash
dotnet run --project samples/Text.Configuration/Bodu.Extensions.Configuration.Text.Samples.BridgeHosting
```

> Path handling differs by source: `AddTextConfigurationFile` resolves relative paths through
> the builder's file provider, so the sample calls `SetBasePath(AppContext.BaseDirectory)`;
> `AddTomlFile` reads its path directly (absolute, or relative to the working directory), so
> the TOML scenarios anchor with `Path.Combine(AppContext.BaseDirectory, ...)`. The csproj
> copies `Data/**` next to the built binary either way.

## Scenario 1 — TextConfigurationFileSource

**Intent.** Show `AddTextConfigurationFile`: instead of calling the
`Bodu.Text.Configuration` document/resolver API directly, the `.boduconfig` file plugs into
`ConfigurationBuilder` like `AddJsonFile` would. The EditorConfig-style cascade is resolved
for the supplied `targetPath` when the source loads, and the resolved view's dotted keys
(`logging.level`) surface as the standard colon-separated keys (`logging:level`).

**What it does.** Builds configuration twice from the same `Data/settings.boduconfig` —
once with `targetPath: "dev/web"` (only `[*]` matches) and once with
`targetPath: "production/web"` (the `[production/**]` section overrides `logging.level`) —
and reads the same two keys from each.

**What to expect.** Identical `app:name`, different `logging:level` — the cascade decided by
the target path, not by which file you loaded:

```text
targetPath 'dev/web       ': app:name = bridge-sample, logging:level = information
targetPath 'production/web': app:name = bridge-sample, logging:level = warning
```

**APIs demonstrated.** `AddTextConfigurationFile(path, targetPath:)`,
`SetBasePath(AppContext.BaseDirectory)`, dotted-key → colon-key mapping
(`ConfigurationKeyOptions.Default`, the `Microsoft.Extensions.Configuration` shape).

## Scenario 2 — TomlFileSource

**Intent.** Show `AddTomlFile`: TOML's nested tables flatten onto the configuration key
tree — `[server.limits]` becomes `server:limits:*` — so a TOML settings file composes with
every other provider (JSON, environment variables, command line) in one builder.

**What it does.** Builds configuration from `Data/settings.toml` (anchored with an absolute
path — `AddTomlFile` does not consult the builder's file provider), reads a root key, two
`[server]` keys, and a nested `[server.limits]` key; shows `GetSection("server")`
enumerating its children like any provider's tree; and builds a second configuration from a
missing file with `optional: true` to show it is skipped rather than thrown.

**What to expect.**

```text
title                    : bridge-sample
server:host              : localhost
server:port              : 8080
server:limits:max_connections : 100
GetSection("server")     : 4 children
optional missing file    : builds clean (0 keys)
```

**APIs demonstrated.** `AddTomlFile(path, optional:)`, TOML table → configuration-section
flattening, `IConfiguration.GetSection` / `GetChildren` composition.

## Scenario 3 — OptionsBinding

**Intent.** Complete the bridge: services should depend on typed options, not on
`IConfiguration` strings. `AddConfigurationOptions<TOptions>` binds a section to a POCO and
registers it with DI in one call, so the consuming service takes `IOptions<ServerOptions>`
and never learns the values came from TOML.

**What it does.** Builds configuration from the same TOML file, registers
`AddConfigurationOptions<ServerOptions>(configuration, "server")` on a `ServiceCollection`,
resolves `IOptions<ServerOptions>` from the built provider, and prints the bound values —
`host`/`port`/`tls` matched to `Host`/`Port`/`Tls` by the binder's case-insensitive name
matching.

**What to expect.**

```text
ServerOptions: localhost:8080 (tls: False)
```

**APIs demonstrated.** `AddConfigurationOptions<TOptions>(IConfiguration, sectionName)`,
`IOptions<T>` resolution through `ServiceProvider`, section-to-POCO binding.

## Layout

```text
Bodu.Extensions.Configuration.Text.Samples.BridgeHosting/
  Program.cs                              # runs the scenarios in order
  Data/settings.boduconfig                # cascade input ([*] + [production/**])
  Data/settings.toml                      # TOML input (root + [server] + [server.limits])
  Scenarios/TextConfigurationFileSource.cs
  Scenarios/TomlFileSource.cs
  Scenarios/OptionsBinding.cs
```

## Related

- `Bodu.Text.Configuration.Samples.ConfigCascade` — the same `.boduconfig` model consumed
  directly through the document/resolver API, without the Microsoft.Extensions bridge.
- `Bodu.Text.Toml` samples (`samples/Text.Toml/`) — the full TOML library the `AddTomlFile`
  source parses with.
- Guides: `docs/guides/extensions-configuration-text/`.
