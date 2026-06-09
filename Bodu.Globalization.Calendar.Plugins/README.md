# Bodu.Globalization.Calendar.Plugins

Trust-gated loading of external `Bodu.Globalization.Calendar` algorithm plugins. Third-party assemblies can contribute custom `INotableDateAlgorithm` implementations, but only after passing an explicit trust policy — loading executes attacker-controlled code, so the gate is opt-in and fails closed.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.Plugins
```

Targets `net8.0`. All types live in the `Bodu.Globalization.Calendar.Plugins` namespace.

## Trust model

`NotableDatePluginLoader` evaluates a candidate assembly against an `IPluginTrustPolicy` before activating any plugin. The policy receives a `PluginTrustContext` (assembly name, file path, SHA-256 hash, strong-name public-key token) and returns a `PluginTrustResult`.

| Policy | Trust basis |
|---|---|
| `StrongNamePluginTrustPolicy` | Strong-name public-key token allow-list |
| `FileHashPluginTrustPolicy` | SHA-256 file-hash allow-list |
| `DelegatingPluginTrustPolicy` | Custom delegate |
| `CompositePluginTrustPolicy` | Combine policies (AND / OR) |
| `AllowAllPluginTrustPolicy` | **Development only** — accepts everything; unsafe |

## Plugin contract

A plugin assembly declares its entry point with `NotableDatePluginAttribute` and implements `INotableDateAlgorithmPlugin` (deriving from `INotableDatePlugin`, which carries `Name` and `Version`). On load, contributed algorithms are registered into the algorithm registry. Failures surface through a typed exception hierarchy rooted at `NotableDatePluginException`: `PluginNotTrustedException`, `PluginActivationException`, and `PluginMissingAttributeException`.

Construct a policy (e.g. `StrongNamePluginTrustPolicy` over a trusted public-key-token allow-list) and pass it to `NotableDatePluginLoader` along with the candidate assembly and the target algorithm registry; untrusted assemblies are rejected before any plugin type is instantiated.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.Plugins/test/Bodu.Globalization.Calendar.Plugins.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
