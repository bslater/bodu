# Bodu.Globalization.Calendar.Plugins

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Trust-gated loading of external `Bodu.Globalization.Calendar` algorithm plugins. Third-party assemblies can contribute custom `INotableDateAlgorithm` implementations, but only after passing an explicit trust policy — loading executes attacker-controlled code, so the gate is opt-in and fails closed.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Plugins
```

Targets `net8.0`. All types live in the `Bodu.Globalization.Calendar.Plugins` namespace.

## Trust model

`NotableDatePluginLoader` evaluates a candidate assembly against an `IPluginTrustPolicy` before activating any plugin. The policy receives a `PluginTrustContext` (assembly name, file path, SHA-256 hash, strong-name public-key token) and returns a `PluginTrustResult`.

| Policy | Trust basis | Guarantee |
|---|---|---|
| `FileHashPluginTrustPolicy` | SHA-256 file-hash allow-list | **Integrity** — the exact reviewed bytes, or nothing; the strongest bundled policy |
| `StrongNamePluginTrustPolicy` | Strong-name public-key token allow-list | **Identity claim only** — .NET (Core) does not verify strong-name signatures at load, and the token is copyable metadata; never sufficient alone for untrusted input — combine with the hash policy |
| `CompositePluginTrustPolicy` | Every constituent policy (AND) | Fails closed; an empty composite trusts nothing |
| `DelegatingPluginTrustPolicy` | Custom delegate | As strong as the delegate |
| `AllowAllPluginTrustPolicy` | Nothing | **Development only** — accepts everything; unsafe |

The gate is an admission check, not a sandbox: an admitted plugin runs with the full trust of the process. The complete contract — entry-point strength, registration collision policy, unloading — is documented in the [plugin trust guide](../docs/guides/calendar/plugin-trust.md).

## Plugin contract

A plugin assembly declares its entry point with `NotableDatePluginAttribute` and implements `INotableDateAlgorithmPlugin` (deriving from `INotableDatePlugin`, which carries `Name` and `Version`). On load, contributed algorithms are registered into the algorithm registry. Failures surface through a typed exception hierarchy rooted at `NotableDatePluginException`: `PluginNotTrustedException`, `PluginActivationException`, and `PluginMissingAttributeException`.

Construct a policy (for untrusted input, a `CompositePluginTrustPolicy` combining the file-hash pin with the strong-name label) and pass it to `NotableDatePluginLoader` with the plugin's file path; untrusted assemblies are rejected before any plugin type is instantiated. `LoadFromFile` additionally returns a disposable `NotableDatePluginHandle` owning the plugin's collectible load context, so the plugin can later be unloaded.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Plugins/test/Bodu.Globalization.Calendar.Plugins.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
