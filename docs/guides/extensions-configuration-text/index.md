---
title: Bodu.Extensions.Configuration.Text guides
---

# Bodu.Extensions.Configuration.Text guides

Recipe-style walk-throughs for **Bodu.Extensions.Configuration.Text** — the bridge between
[`Bodu.Text.Configuration`](../text-configuration/index.md) and `Microsoft.Extensions.Configuration`, built on
<xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource>,
<xref:Bodu.Extensions.Configuration.Text.TextStreamConfigurationSource>, and the
<xref:Bodu.Extensions.Configuration.Text.ConfigurationOptionsExtensions> options-binding helpers.

If you are new to the library, start with the [introduction](../../docs/extensions-configuration-text/index.md), the
[Core concepts](../../docs/extensions-configuration-text/concepts.md) glossary, and the
[getting-started page](../../docs/extensions-configuration-text/getting-started.md). The guides below assume you know
the vocabulary (source, provider, target path, parse / resolve option propagation, reload-on-change, options binding).

## How the library works

![IConfigurationBuilder to IConfiguration to IOptions](../../images/diagrams/extensions-configuration-text-flow.svg)

`AddTextConfigurationFile(...)` registers a <xref:Bodu.Extensions.Configuration.Text.TextConfigurationSource> on the
builder. When the builder calls `Build`, the source instantiates a
<xref:Bodu.Extensions.Configuration.Text.TextConfigurationProvider> that parses the file via
<xref:Bodu.Text.Configuration.ConfigurationDocument>, resolves it for the source's `TargetPath`, and copies the
flattened view into the inherited `Data` dictionary as colon-delimited keys. The DI options helper binds named
sections to typed POCO classes through the standard `Microsoft.Extensions.DependencyInjection` shape.

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="configuration-sources.md">Configuration sources</a></h3>
  <p>Every <code>AddTextConfiguration*</code> overload — file, stream, pre-parsed document, convention-based discovery, fluent source configuration — plus the read-only <code>AddTomlFile</code> / <code>AddTomlStream</code> bridge, reload-on-change, <code>IOptions&lt;T&gt;</code> binding, and layering alongside JSON and environment-variable sources.</p>
</div>

<div class="bodu-card">
  <h3><a href="../../docs/extensions-configuration-text/index.md">Introduction</a></h3>
  <p>Package overview — what the bridge adds on top of <code>Bodu.Text.Configuration</code>, headline types, and the scenarios it was built for.</p>
</div>

<div class="bodu-card">
  <h3><a href="../../docs/extensions-configuration-text/concepts.md">Core concepts</a></h3>
  <p>The shared vocabulary — source, provider, target path, option propagation, reload token, options binding.</p>
</div>

<div class="bodu-card">
  <h3><a href="../../docs/extensions-configuration-text/getting-started.md">Getting started</a></h3>
  <p>Install the package and run the minimal samples — a file source, a typed options class, and a Generic Host wiring.</p>
</div>

</div>

## Reading path

1. **[Getting started](../../docs/extensions-configuration-text/getting-started.md)** — install and confirm the minimal file-source sample runs.
2. **[Configuration sources](configuration-sources.md)** — pick the right `AddTextConfiguration*` overload for your scenario and wire up options binding and reload.
3. **[Views and resolution](../text-configuration/views-and-resolution.md)** — when you need to understand *which* value the bridge surfaced, drop down to the underlying resolve layer.

The bridge propagates `ParseOptions` and `ResolveOptions` verbatim to the underlying library, so everything in the
[Bodu.Text.Configuration guides](../text-configuration/index.md) — profiles, glob anchoring, key mapping, diagnostics —
applies unchanged to values consumed through `IConfiguration`.

## Namespace map

| Namespace | What lives here | Static docs |
|---|---|---|
| `Bodu.Extensions.Configuration.Text` | Builder extensions (`TextConfigurationExtensions`), file source / provider (`TextConfigurationSource`, `TextConfigurationProvider`), stream source / provider (`TextStreamConfigurationSource`, `TextStreamConfigurationProvider`), the read-only TOML bridge (`TomlConfigurationExtensions`, `TomlConfigurationSource`, `TomlConfigurationProvider`), DI options helpers (`ConfigurationOptionsExtensions`). | [Introduction](../../docs/extensions-configuration-text/index.md) · [Core concepts](../../docs/extensions-configuration-text/concepts.md) · [Getting started](../../docs/extensions-configuration-text/getting-started.md) |

## Where to go next

- **[Runnable samples](../../samples/text-configuration.md)** — the offline BridgeHosting sample under `samples/Text.Configuration/`: `AddTextConfigurationFile` with `targetPath`, `AddTomlFile`, and `IOptions<T>` binding.
- **[Introduction](../../docs/extensions-configuration-text/index.md)** — namespaces, headline types, scenarios.
- **[Core concepts](../../docs/extensions-configuration-text/concepts.md)** — full vocabulary.
- **[Getting started](../../docs/extensions-configuration-text/getting-started.md)** — install + runnable minimal samples.
- **[Configuration topic guides](../topics/configuration.md)** — every guide in the Configuration topic on one page.
- **[Configuration topic overview](../../docs/topics/configuration.md)** — the pipeline and package boundaries across both packages.
- **[Bodu.Extensions.Configuration.Text API reference](xref:Bodu.Extensions.Configuration.Text)** — full type-by-type docs.
- **[Bodu.Text.Configuration](../text-configuration/index.md)** — the underlying parser, resolver, and view.
