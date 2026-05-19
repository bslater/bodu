---
title: Bodu.Extensions.Configuration.Text guides
---

# Bodu.Extensions.Configuration.Text guides

Recipe-style walk-throughs for **Bodu.Extensions.Configuration.Text** — the bridge between
[`Bodu.Text.Configuration`](../text-configuration/index.md) and `Microsoft.Extensions.Configuration`.

If you are new to the library, start with the [introduction](../../docs/extensions-configuration-text/index.md), the
[Core concepts](../../docs/extensions-configuration-text/concepts.md) glossary, and the
[getting-started page](../../docs/extensions-configuration-text/getting-started.md). The guides below assume you know
the vocabulary (source, provider, target path, parse / resolve option propagation, reload-on-change, options binding).

## How the library works

![IConfigurationBuilder to IConfiguration to IOptions](../../images/diagrams/extensions-configuration-text-flow.svg)

`AddBoduConfiguration(...)` registers a <xref:Bodu.Extensions.Configuration.Text.BoduTextConfigurationSource> on the
builder. When the builder calls `Build`, the source instantiates a
<xref:Bodu.Extensions.Configuration.Text.BoduTextConfigurationProvider> that parses the file via
<xref:Bodu.Text.Configuration.BoduConfigurationDocument>, resolves it for the source's `TargetPath`, and copies the
flattened view into the inherited `Data` dictionary as colon-delimited keys. The DI options helper binds named
sections to typed POCO classes through the standard `Microsoft.Extensions.DependencyInjection` shape.

## Namespace map

| Namespace | What lives here | Static docs |
|---|---|---|
| `Bodu.Extensions.Configuration.Text` | Builder extensions (`BoduTextConfigurationExtensions`), file source / provider (`BoduTextConfigurationSource`, `BoduTextConfigurationProvider`), stream source / provider (`BoduTextStreamConfigurationSource`, `BoduTextStreamConfigurationProvider`), DI options helpers (`BoduConfigurationOptionsExtensions`). | [Introduction](../../docs/extensions-configuration-text/index.md) · [Core concepts](../../docs/extensions-configuration-text/concepts.md) · [Getting started](../../docs/extensions-configuration-text/getting-started.md) |

## Where to go next

- **[Introduction](../../docs/extensions-configuration-text/index.md)** — namespaces, headline types, scenarios.
- **[Core concepts](../../docs/extensions-configuration-text/concepts.md)** — full vocabulary.
- **[Getting started](../../docs/extensions-configuration-text/getting-started.md)** — install + runnable minimal samples.
- **[Bodu.Extensions.Configuration.Text API reference](../../apidoc/Bodu.Extensions.Configuration.Text.md)** — full type-by-type docs.
- **[Bodu.Text.Configuration](../text-configuration/index.md)** — the underlying parser, resolver, and view.
