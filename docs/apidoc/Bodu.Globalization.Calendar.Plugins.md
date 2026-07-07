---
uid: Bodu.Globalization.Calendar.Plugins
---

![Bodu.Globalization.Calendar.Plugins](~/images/hero-calendar-plugins.svg)

# Bodu.Globalization.Calendar.Plugins

## Purpose

**Bodu.Globalization.Calendar.Plugins** loads external assemblies that contribute custom date-calculation algorithms to [`Bodu.Globalization.Calendar`](Bodu.Globalization.Calendar.md), behind an explicit, **deny-by-default** trust gate.

A plugin assembly advertises itself with an assembly-level <xref:Bodu.Globalization.Calendar.Plugins.NotableDatePluginAttribute>. The host evaluates the assembly against an <xref:Bodu.Globalization.Calendar.Plugins.IPluginTrustPolicy> *before* activating any plugin type; a rejected assembly is never instantiated. Trusted plugins surface their <xref:Bodu.Globalization.Calendar.Algorithms.INotableDateAlgorithm> implementations, which are then registered into a <xref:Bodu.Globalization.Calendar.Algorithms.NotableDateAlgorithmRegistry> for use by `<Algorithm key="…">` rules.

## Static documentation

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
using System.Reflection;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Plugins;

// Trust only assemblies whose strong-name public-key token is on the allow-list.
IPluginTrustPolicy trust = new StrongNamePluginTrustPolicy(allowedPublicKeyTokens);

Assembly pluginAssembly = Assembly.LoadFrom("Contoso.Holidays.dll");
INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(pluginAssembly, trust); // throws if untrusted

var registry = new NotableDateAlgorithmRegistry();
int registered = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
// registry can now back <Algorithm key="…"> rules in a loaded resource.
```

> [!WARNING]
> <xref:Bodu.Globalization.Calendar.Plugins.AllowAllPluginTrustPolicy> trusts every assembly and is intended for development and tests only. Use a strong-name, file-hash, or composite policy in production.
