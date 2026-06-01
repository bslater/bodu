---
uid: Bodu.Globalization.Calendar.Plugins
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar.Plugins** is the external-plugin loading and trust-policy surface. It enables third-party assemblies to contribute `INotableDateRuleProvider` instances and `INotableDateAlgorithm` registrations to a host `NotableDateService` without the host having to reference them at build time, while keeping load-time isolation and admission policies under the host's control.

Reach for this namespace when you need to load notable-date rules and algorithms from a side-loaded assembly (e.g. a region-specific rule pack discovered at runtime), and you want a declarative trust policy gating which assemblies are admitted.

## Key types

**Plugin contracts**

- <xref:Bodu.Globalization.Calendar.INotableDatePlugin> — the minimum surface every external plugin must expose: `string Name`, `System.Version Version`. Plugins additionally implement one or both of the contributor interfaces below.
- <xref:Bodu.Globalization.Calendar.INotableDateRulePlugin> — contributes rule providers via `IEnumerable<INotableDateRuleProvider> GetRuleProviders()`.
- <xref:Bodu.Globalization.Calendar.INotableDateAlgorithmPlugin> — contributes algorithm registrations via `IEnumerable<(string key, INotableDateAlgorithm algorithm)> GetAlgorithms()`.

**Plugin declaration**

- <xref:Bodu.Globalization.Calendar.NotableDatePluginAttribute> — assembly-level attribute declaring the plugin entry-point type. Exactly one attribute is required per plugin assembly. Constructor: `NotableDatePluginAttribute(Type pluginType)` where `pluginType` implements `INotableDatePlugin`.

**Plugin loader**

- <xref:Bodu.Globalization.Calendar.ExternalPluginLoader> — static loader: `Load(string filePath, IPluginTrustPolicy trustPolicy)` → `INotableDatePlugin`. Reflects only the `NotableDatePluginAttribute`, evaluates the supplied trust policy, and instantiates the declared entry-point type in its own non-collectible `AssemblyLoadContext`.

**Trust policies**

- <xref:Bodu.Globalization.Calendar.IPluginTrustPolicy> — admission gate: `Evaluate(PluginTrustContext) → PluginTrustResult`.
- <xref:Bodu.Globalization.Calendar.PluginTrustContext> — record struct carrying `AssemblyPath`, `AssemblyName`, and `FileHash` (SHA-256).
- <xref:Bodu.Globalization.Calendar.PluginTrustResult> — record struct: `Trusted` plus an optional `Reason`.
- <xref:Bodu.Globalization.Calendar.FileHashPluginTrustPolicy> — admits assemblies whose SHA-256 file hash matches a pinned value keyed by assembly name. Tamper-resistant when combined with a strong-name policy.
- <xref:Bodu.Globalization.Calendar.StrongNamePluginTrustPolicy> — admits assemblies whose strong-name public-key token is in a consumer-supplied allow-list. Tokens are compared case-insensitively as hex. Does **not** cryptographically verify the signature on its own; compose with `FileHashPluginTrustPolicy` for full tamper resistance.
- <xref:Bodu.Globalization.Calendar.CompositePluginTrustPolicy> — AND-composes two or more policies; every child must return trusted.
- <xref:Bodu.Globalization.Calendar.DelegatingPluginTrustPolicy> — adapts an arbitrary callback `Func<PluginTrustContext, PluginTrustResult>` into a policy. Use for runtime-driven decisions (configuration, remote attestation, logged-in user).
- <xref:Bodu.Globalization.Calendar.AllowAllPluginTrustPolicy> — dev/test only; marks every plugin as trusted. Intentionally easy to spot in code review.

**Exception hierarchy**

- <xref:Bodu.Globalization.Calendar.NotableDatePluginException> — abstract base.
- <xref:Bodu.Globalization.Calendar.PluginNotTrustedException> — thrown when the trust policy rejects the candidate.
- <xref:Bodu.Globalization.Calendar.PluginMissingAttributeException> — thrown when the assembly lacks a valid `NotableDatePluginAttribute` or the declared type does not implement `INotableDatePlugin`.
- <xref:Bodu.Globalization.Calendar.PluginActivationException> — thrown when the plugin type lacks a public parameterless constructor or the constructor throws.

## Example

```csharp
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Plugins;

// Production: pin the assembly hash and the strong-name token together.
var trust = new CompositePluginTrustPolicy(
    new StrongNamePluginTrustPolicy(allowedTokens: new[] { "31bf3856ad364e35" }),
    new FileHashPluginTrustPolicy(allowedHashes: new()
    {
        ["Contoso.Calendar.Plugin"] = expectedSha256,
    }));

INotableDatePlugin plugin = ExternalPluginLoader.Load("plugins/Contoso.Calendar.Plugin.dll", trust);

// Wire its contributions into the service.
var ruleProviders   = ((INotableDateRulePlugin)plugin).GetRuleProviders().ToArray();
var algorithmRegistry = new NotableDateAlgorithmRegistry();
foreach (var (key, alg) in ((INotableDateAlgorithmPlugin)plugin).GetAlgorithms())
    algorithmRegistry.Register(key, alg);

var service = new NotableDateService(
    ruleProviders,
    algorithmRegistry);
```

## Notes

- **Isolated load context.** Every external plugin is loaded into its own non-collectible `AssemblyLoadContext`, so its dependency graph does not conflict with the host's. Plugins cannot, however, be unloaded — this is a deliberate trade-off for predictable lifetimes.
- **Trust is the host's responsibility.** `ExternalPluginLoader` evaluates only the supplied policy; it does not consult signing certificates, EFS / SELinux labels, or operating-system policy. Compose with operating-system controls (mark plugins read-only, install in a privileged folder) for defence in depth.
- **Reflect-only attribute scan.** The loader inspects only the `NotableDatePluginAttribute` and does not enumerate types or invoke module initializers, so a malformed plugin cannot execute code merely by being scanned.
- **Strong-name policy caveat.** `StrongNamePluginTrustPolicy` checks the token, not the signature. Pair it with `FileHashPluginTrustPolicy` when the threat model includes a sophisticated attacker who could re-sign with a stolen private key.
- **See also:** the [`NotableDateAlgorithmRegistry` reference](xref:Bodu.Globalization.Calendar.NotableDateAlgorithmRegistry), the [Building and extending the service](~/guides/calendar/building-the-service.md) guide.
