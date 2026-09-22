# Bodu.Globalization.Calendar.Samples.Plugins

Trust-gated plugin loading with `Bodu.Globalization.Calendar.Plugins`: a date-calculation algorithm
loaded out of a separate assembly at runtime, under a policy that decides whether that assembly is
allowed to run at all.

The plugin lives in the sibling `Bodu.Globalization.Calendar.Samples.Plugin.Contoso` project. This
host references it with `ReferenceOutputAssembly="false"`, so the DLL is built and copied into a
`plugins/` folder beside the host while its **types stay unavailable at compile time** — the host can
only reach it through the loader, which is the arrangement a real plugin host has.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.Plugins
```

Offline and deterministic.

## Scenarios

### Loading a plugin and using the algorithm it contributes

**Intent.** Show the whole round trip: discover an assembly, load it, register what it contributes,
and resolve a date that depends on it.

**What it does.** Loads the plugin by file path, prints the name and version it reports, registers
its algorithms into a `NotableDateAlgorithmRegistry`, and resolves a year against a rule document
that references one of them by the key `contoso.founding-day`.

**Expected output.** `Contoso Calendar 1.0.0`, one algorithm registered, and Founding Day resolving
to Friday 13 March 2026 — the algorithm rolls 12 March to the Friday of its week. New Year's Day
resolves from an ordinary fixed rule beside it, so plugin-fed and built-in rules coexist in one
document.

**APIs.** `NotableDatePluginLoader.LoadFromFile` / `RegisterAlgorithms`, `NotableDatePluginHandle`
(`Plugin`, `IsUnloadable`, `Dispose`), `INotableDateAlgorithmPlugin`, `AllowAllPluginTrustPolicy`.

### The trust gate — why loading a plugin takes a policy

**Intent.** Show that the policy is not optional, and what the useful policies are.

**What it does.** Pins the plugin with a SHA-256 allow-list and loads it; retries with a deliberately
wrong hash; then shows a `DelegatingPluginTrustPolicy` expressing a host's own rule, and a
`CompositePluginTrustPolicy` that requires its members to agree.

**Expected output.** The matching hash loads. The wrong hash raises `PluginNotTrustedException`
carrying the reason, **before any plugin code runs**. The composite refuses because one member
refuses — composition can only narrow trust.

**APIs.** `FileHashPluginTrustPolicy`, `DelegatingPluginTrustPolicy`, `CompositePluginTrustPolicy`,
`PluginTrustResult`, `PluginTrustContext`, `PluginNotTrustedException`.

## Layout

```
  Program.cs                      # locates the built plugin, runs both scenarios
  SampleConsole.cs                # the what/why/expect banner every scenario opens with
  Scenarios/LoadingAPlugin.cs     # load, register, resolve
  Scenarios/TrustPolicies.cs      # the four policies and a refusal
```

## Equivalent NuGet references

```bash
dotnet add package Bodu.Globalization.Calendar
dotnet add package Bodu.Globalization.Calendar.Plugins
dotnet add package Bodu.Globalization.Calendar.Builder
```

## Related

- `Bodu.Globalization.Calendar.Samples.CustomAlgorithm` — the same extension point reached by
  registering an algorithm in-process, when you control the host and do not need an external
  assembly.
