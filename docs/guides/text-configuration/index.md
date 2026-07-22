---
title: Bodu.Text.Configuration guides
---

# Bodu.Text.Configuration guides

Recipe-style walk-throughs for **Bodu.Text.Configuration** — the layered, EditorConfig-style parser, resolver, and typed view built on <xref:Bodu.Text.Configuration.ConfigurationDocument>, <xref:Bodu.Text.Configuration.ConfigurationView>, and <xref:Bodu.Text.Configuration.ConfigurationParseOptions>.

If you are new to the library, start with the [introduction](../../docs/text-configuration/index.md), the
[Core concepts](../../docs/text-configuration/concepts.md) glossary, and the
[getting-started page](../../docs/text-configuration/getting-started.md). The guides below assume you know the
vocabulary (document, view, profile, target path, preamble, glob pattern, key mapping, unset, diagnostic mode).

## How the library works

![Bodu Text Configuration pipeline](../../images/diagrams/text-configuration-pipeline.svg)

A configuration document is parsed once and then projected — through a target path — into a flat
<xref:Bodu.Text.Configuration.ConfigurationView>. The reader produces an immutable
<xref:Bodu.Text.Configuration.IniDocument>; the resolver layers the preamble plus matching glob-anchored sections in source
order; the view exposes typed accessors that return the effective value for each colon-delimited key.

## Guides

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="parsing-and-profiles.md">Parsing and profiles</a></h3>
  <p>The parse half — <code>ConfigurationDocument.Parse</code>, non-throwing and diagnostic-collecting parses, <code>ConfigurationParseOptions</code> field by field, and the four profiles (<code>Bodu</code>, <code>EditorConfigCompatible</code>, <code>Strict</code>, <code>Relaxed</code>).</p>
</div>

<div class="bodu-card">
  <h3><a href="views-and-resolution.md">Views and resolution</a></h3>
  <p>The resolve half — <code>Resolve()</code> → <code>ConfigurationView</code>, EditorConfig glob anchoring against a target path, typed value getters, key projection via <code>ConfigurationKey</code> / <code>ConfigurationKeyOptions</code>, and <code>ConfigurationPattern</code> glob matching.</p>
</div>

<div class="bodu-card">
  <h3><a href="diagnostics.md">Diagnostics</a></h3>
  <p>The structured <code>ConfigurationDiagnostic</code> record, diagnostic modes and severities, and the full <code>ConfigurationDiagnosticCode</code> catalogue — what triggers each code and how to build IDE-style validation.</p>
</div>

</div>

## Reading path

1. **[Parsing and profiles](parsing-and-profiles.md)** — parse text into a `ConfigurationDocument` and pick the profile that matches your file's dialect.
2. **[Views and resolution](views-and-resolution.md)** — resolve the document into the flat, typed `ConfigurationView` your application code consumes.
3. **[Diagnostics](diagnostics.md)** — when a file does not parse cleanly, read the structured diagnostics instead of guessing from an exception message.

Hosting the resolved view inside `Microsoft.Extensions.Configuration` (ASP.NET Core, Generic Host, `IOptions<T>` binding)? Continue with the bridge package guides at **[Bodu.Extensions.Configuration.Text](../extensions-configuration-text/index.md)**.

## Namespace map

| Namespace | What lives here | Static docs |
|---|---|---|
| `Bodu.Text.Configuration` | Static façade (`ConfigurationDocument`), resolved view (`ConfigurationView`), profile and option types (`ConfigurationParseOptions`, `ConfigurationResolveOptions`, `ConfigurationWriteOptions`, `ConfigurationKeyOptions`), key model (`ConfigurationKey`), diagnostics (`ConfigurationDiagnostic`, `ConfigurationParseResult`), and the resolver pattern engine (`ConfigurationPattern`). | [Introduction](../../docs/text-configuration/index.md) · [Core concepts](../../docs/text-configuration/concepts.md) · [Getting started](../../docs/text-configuration/getting-started.md) |

## Where to go next

- **[Runnable samples](../../samples/text-configuration.md)** — offline sample projects under `samples/Text.Configuration/` covering the resolve cascade, diagnostics, `unset` dialects, save, and the Microsoft.Extensions bridge.
- **[Introduction](../../docs/text-configuration/index.md)** — namespaces, headline types, scenarios.
- **[Core concepts](../../docs/text-configuration/concepts.md)** — full vocabulary.
- **[Getting started](../../docs/text-configuration/getting-started.md)** — install + runnable minimal samples.
- **[Configuration topic guides](../topics/configuration.md)** — every guide in the Configuration topic on one page.
- **[Configuration topic overview](../../docs/topics/configuration.md)** — the pipeline and package boundaries across both packages.
- **[Bodu.Text.Configuration API reference](xref:Bodu.Text.Configuration)** — full type-by-type docs.
- **[Bodu.Extensions.Configuration.Text](../extensions-configuration-text/index.md)** — the `Microsoft.Extensions.Configuration` bridge.
- **[Bodu.Text.Ini](../formats/index.md)** — the standalone INI library, for codec-only INI reading and editing.
