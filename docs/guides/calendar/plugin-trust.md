---
title: Calendar plugin trust
---

# Calendar plugin trust

`Bodu.Globalization.Calendar.Plugins` loads external assemblies that contribute custom <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> implementations. Loading a plugin executes third-party code inside your process, so every load runs through an explicit, deny-by-default trust gate. This page states precisely what that gate validates, what each bundled policy does and does not guarantee, and where the security boundary actually sits.

## The security boundary, stated plainly

The trust gate is an **admission check, not a sandbox**. It answers one question — *should this assembly be allowed to run?* — before any plugin code executes. Once admitted, a plugin runs with the **full trust of your process**: it can use the file system, the network, reflection, and every capability your application has. There is no capability restriction, no resource limit, and no isolation of failures beyond what the loader itself guards (activation and registration faults surface as typed exceptions rather than crashing the host).

Consequences:

- Only load plugins whose *publisher* you trust. The gate verifies *which bytes* run, not *what they do*.
- The strongest guarantee available is byte-level integrity pinning via <xref:Bodu.Globalization.Calendar.Plugins.FileHashPluginTrustPolicy> — combine it with provenance controls (where the file came from) rather than treating any policy as a substitute for them.
- For trimmed/AOT deployments the loader is annotated `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`: runtime plugin loading is inherently incompatible with native AOT, and the data-driven rule-pack path is the alternative.

## What the gate evaluates

<xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginLoader> builds a <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustContext> — assembly name, file path, SHA-256 hash, and strong-name public-key token — and passes it to your <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy> **before** the plugin's entry-point type is activated. A rejected assembly's constructor never runs.

The two load entry points offer different strengths:

| Entry point | Guarantee |
|---|---|
| `LoadFrom(string assemblyPath, …)` / `LoadFromFile(string assemblyPath, …)` | **Strong.** The file is read once; the hash the policy verifies is the digest of the exact bytes that are loaded (no time-of-check/time-of-use gap), and the assembly loads into a dedicated collectible `AssemblyLoadContext` whose dependencies resolve from the plugin's own directory. `LoadFromFile` additionally returns a disposable <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginHandle> that owns the context, so the plugin can later be unloaded. |
| `LoadFrom(Assembly, …)` | **Weak — trusted input only.** The assembly is *already loaded* when the policy runs: module initializers may already have executed, and the hash is re-read from disk, which does not close the swap window. Use it only for assemblies you already trust for other reasons (for example, ones you compiled). |

## The bundled policies

| Policy | Validates | Guarantee |
|---|---|---|
| <xref:Bodu.Globalization.Calendar.Plugins.FileHashPluginTrustPolicy> | The SHA-256 digest of the loaded bytes against a pinned allow-list (constant-time comparison). | **Integrity.** The strongest bundled policy: the exact reviewed bytes, or nothing. Re-pinning is required on every legitimate update. |
| <xref:Bodu.Globalization.Calendar.Plugins.StrongNamePluginTrustPolicy> | The assembly's *declared* strong-name public-key token against an allow-list. | **Identity claim only.** .NET (Core) does not verify strong-name signatures at load time, and a token is plain metadata an attacker can copy into a hostile assembly. Treat it as a labelling convention — useful for routing and defense in depth, **never sufficient alone** for untrusted input. Combine it with the file-hash policy. |
| <xref:Bodu.Globalization.Calendar.Plugins.CompositePluginTrustPolicy> | All constituent policies (AND). | Fails closed: an empty composite trusts nothing, and every constituent must approve. |
| <xref:Bodu.Globalization.Calendar.Plugins.DelegatingPluginTrustPolicy> | Whatever your delegate decides. | As strong as your delegate. |
| <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy> | Nothing. | **Development and tests only.** |

A production-grade gate for third-party plugins:

```csharp
IPluginTrustPolicy trust = new CompositePluginTrustPolicy(
[
    new StrongNamePluginTrustPolicy(["c0ffee1234567890"]),          // publisher labelling
    new FileHashPluginTrustPolicy(new Dictionary<string, byte[]>    // byte-level integrity
    {
        ["Contoso.Holidays"] = reviewedDigest,
    }),
]);

using NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile("plugins/Contoso.Holidays.dll", trust);
```

## After admission: registration is also gated

<xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginLoader>.`RegisterAlgorithms` treats the plugin's contribution as untrusted:

- Registration is **atomic** — the contribution is fully staged and validated before the shared registry is touched, so a faulting or malformed plugin never leaves it partially mutated.
- A contributed key that collides with a **built-in algorithm key or an existing registration is rejected by default**, so a plugin cannot silently take over an existing resolution path. Overriding is an explicit opt-in via <xref:Bodu.Globalization.Calendar.Plugins.PluginAlgorithmRegistrationOptions>.`AllowOverride`, and each replacement is logged at warning level.

## Unloading

Disposing the <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginHandle> initiates the unload of the plugin's collectible load context. The runtime reclaims it only once nothing references the plugin's types — a registry still holding the plugin's algorithms, or a service over that registry, keeps the context alive, so tear consumers down before or promptly after disposing the handle.

## Where to go next

- [Building and extending the service](building-the-service.md) — wiring a plugin-populated registry into the loader and the service.
- [Date calculation algorithms](algorithms.md) — the `<Algorithm key="…">` rules a plugin's algorithms back.
- [`Bodu.Globalization.Calendar.Plugins` API reference](xref:Bodu.Globalization.Calendar.Plugins) — the full type list.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
