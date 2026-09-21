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
--- AddTextConfigurationFile - a resolved cascade as an IConfiguration source ---
  What   : Builds configuration twice from the same .boduconfig file, once for a development target path and once
           for a production one, and reads the same two keys from each.
  Why    : A .boduconfig file is not a flat key/value document - a value only means something once you name the path
           it applies to. IConfiguration has no notion of that, so the bridge resolves the cascade when the source
           loads and publishes the resulting view as ordinary keys. The consequence is worth being explicit about:
           the target path is fixed at build time, so two target paths need two builds rather than two lookups. In a
           host that is not a limitation, since the environment is known before configuration is built - but it is
           why the target path is a source parameter rather than an argument to the key lookup.
  Expect : Two builds of one file give different values for logging:level and the same value for app:name, because
           only the production section overrides the former. The dotted keys in the file arrive as the
           colon-separated keys every other provider uses, so a consumer binding these cannot tell a cascade was
           involved.

  targetPath 'dev/web       ': app:name = bridge-sample, logging:level = information
  targetPath 'production/web': app:name = bridge-sample, logging:level = warning
  (same file, same keys, different values - the production section overrides logging:level, and the cascade is resolved at load)
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
--- AddTomlFile - a TOML document as an IConfiguration source ---
  What   : Registers a TOML file with a standard ConfigurationBuilder, reads a top-level key and two levels of
           nested table, composes a section the way any provider's tree composes, and builds again over a file that
           does not exist while marked optional.
  Why    : Microsoft.Extensions.Configuration is a key/value abstraction with one shape - a flat map of
           colon-separated string keys, layered by provider order. A format only has to answer one question to join
           it: how does a nested structure flatten into that key space. Here a TOML table becomes a key prefix,
           which is the same rule the built-in JSON provider uses, so a TOML file layers with appsettings.json,
           environment variables and command-line arguments without anything downstream knowing which provider a
           value came from. That is the whole value of the bridge: the format choice stops being an architectural
           decision.
  Expect : Nested tables surface as colon-separated keys, so [server.limits] is read as
           'server:limits:max_connections' - the same shape a JSON provider would produce for the same structure.
           GetSection returns a normal section over that tree. The optional missing file builds cleanly with no keys
           instead of throwing, which is what makes an optional local override file safe to register
           unconditionally.

  title                    : bridge-sample  (a top-level TOML key becomes a top-level configuration key)
  server:host              : localhost
  server:port              : 8080
  server:limits:max_connections : 100  (the nested [server.limits] table flattens to a colon path - the same rule the built-in JSON provider applies)
  GetSection("server")     : 4 children  (an ordinary section over an ordinary tree - nothing downstream can tell the values came from TOML)
  optional missing file    : builds clean (0 keys)  (expected 0 keys and no exception - what makes a local override file safe to register unconditionally)
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
--- AddConfigurationOptions<T> - from a TOML table to an injected options type ---
  What   : Builds configuration from the TOML file, registers the [server] table as a strongly typed options class,
           resolves it from the service provider, and prints the bound values.
  Why    : This is where the bridge stops being visible, which is the goal. A service that takes
           IOptions<ServerOptions> depends on a shape it owns, not on a configuration key, a file path or a format -
           so the file can move to JSON, to environment variables, or to a secret store without touching it. Binding
           also does the type conversion in one place: the wire values are all text, and Port arriving as an int
           rather than a string is the difference between parsing once at startup and parsing at every call site,
           differently.
  Expect : The options instance carries values from the TOML table with their types already resolved - an int port
           and a bool flag, not strings. Nothing in ServerOptions or in the code consuming it refers to TOML, which
           is the entire point of registering it this way.

  ServerOptions: localhost:8080 (tls: False)  (bound from the [server] table with types resolved once at startup - the consuming code never mentions TOML)
```

**APIs demonstrated.** `AddConfigurationOptions<TOptions>(IConfiguration, sectionName)`,
`IOptions<T>` resolution through `ServiceProvider`, section-to-POCO binding.

## Layout

```text
Bodu.Extensions.Configuration.Text.Samples.BridgeHosting/
  Program.cs                              # runs the scenarios in order
  SampleConsole.cs                        # the What / Why / Expect scenario banner
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
