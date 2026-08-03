---
uid: Bodu.Globalization.Calendar.Plugins
---

![Bodu.Globalization.Calendar.Plugins](~/images/hero-calendar-plugins.svg)

# Bodu.Globalization.Calendar.Plugins

## Purpose

**Bodu.Globalization.Calendar.Plugins** loads external assemblies that contribute custom date-calculation algorithms to [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md), behind an explicit, **deny-by-default** trust gate.

A plugin assembly advertises itself with an assembly-level <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginAttribute>. The host evaluates the assembly against an <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy> *before* activating any plugin type; a rejected assembly is never instantiated. Trusted plugins surface their <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> implementations, which are then registered into a <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> for use by `<Algorithm key="…">` rules.

## Static documentation

- **[Calendar plugin trust](~/guides/calendar/plugin-trust.md)** — the trust-gate contract: what each policy guarantees, entry-point strength, registration collision policy, and unloading.
- **[Building and extending the service](~/guides/calendar/building-the-service.md)** — the plugin model, trust policies, and end-to-end loading.

## Key types

**Plugin contracts**

- <xref:Bodu.Globalization.Calendar.Plugins.INotableDatePlugin> — the base contract (`Name`, `Version`).
- <xref:Bodu.Globalization.Calendar.Plugins.INotableDateAlgorithmPlugin> — `GetAlgorithms()` returns the `(key, INotableDateAlgorithm)` pairs the plugin contributes.
- <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginAttribute> — the assembly-level attribute naming the plugin type, e.g. `[assembly: NotableDatePlugin(typeof(MyPlugin))]`.

**Loader**

- <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginLoader> — `LoadFrom(Assembly, IPluginTrustPolicy)` and `LoadFrom(string assemblyPath, IPluginTrustPolicy)` (the path overload loads into a dedicated `AssemblyLoadContext`); `RegisterAlgorithms(plugin, registry)` registers the plugin's algorithms and returns the count. Trust is evaluated before activation.

**Trust policies**

- <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy> — `Evaluate(PluginTrustContext)` returns a <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustResult>; the inputs are carried by <xref:Bodu.Globalization.Calendar.Plugins.PluginTrustContext> (assembly name, path, file hash, public-key token).
- Bundled policies: <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy> (development / tests only), <xref:Bodu.Globalization.Calendar.Plugins.StrongNamePluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.FileHashPluginTrustPolicy>, <xref:Bodu.Globalization.Calendar.Plugins.CompositePluginTrustPolicy> (AND / short-circuit), and <xref:Bodu.Globalization.Calendar.Plugins.DelegatingPluginTrustPolicy> (decide with a delegate).

**Exceptions**

- <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginException> (base), <xref:Bodu.Globalization.Calendar.Plugins.PluginNotTrustedException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginMissingAttributeException>, <xref:Bodu.Globalization.Calendar.Plugins.PluginActivationException>.

## Minimal sample

```csharp
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Plugins;

// Pin the exact reviewed bytes: the file-hash policy is the strongest bundled guarantee.
IPluginTrustPolicy trust = new FileHashPluginTrustPolicy(new Dictionary<string, byte[]>
{
    ["Contoso.Holidays"] = reviewedSha256Digest,
});

// The path overload hashes the same bytes it loads (no swap window) and isolates the plugin
// in a dedicated collectible AssemblyLoadContext; throws if untrusted.
INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom("plugins/Contoso.Holidays.dll", trust);

var registry = new NotableDateAlgorithmRegistry();
int registered = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
// registry can now back <Algorithm key="…"> rules in a loaded resource.
```

> [!WARNING]
> The trust gate is an admission check, not a sandbox — an admitted plugin runs with the full trust of the process. <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy> is for development and tests only, and <xref:Bodu.Globalization.Calendar.Plugins.StrongNamePluginTrustPolicy> validates a copyable manifest token rather than a verified signature — for untrusted input, always combine it with the file-hash policy. The `LoadFrom(Assembly, …)` overload evaluates trust *after* the assembly is already loaded and must not be used for untrusted input; prefer the path overloads, and `LoadFromFile` when the plugin should be unloadable. See the [plugin trust guide](~/guides/calendar/plugin-trust.md).
