---
title: Configuration guides
---

# Configuration guides

Recipe-style walk-throughs for the **Configuration** topic — [`Bodu.Text.Configuration`](../text-configuration/index.md), the layered EditorConfig-style parser and resolver, and [`Bodu.Extensions.Configuration.Text`](../extensions-configuration-text/index.md), its bridge into `Microsoft.Extensions.Configuration`.

If you are new to the topic, start with the [Configuration overview](../../docs/topics/configuration.md) for the pipeline and package boundaries, and the [Configuration concepts](../../docs/topics/configuration-concepts.md) glossary for the shared vocabulary (profile, preamble, glob-anchored section, layered resolution, view, source, provider, reload token).

## Bodu.Text.Configuration

Parse a configuration document under one of the four profiles (`Bodu`, `EditorConfigCompatible`, `Strict`, `Relaxed`), resolve it for a target path, and read typed values back out.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../text-configuration/index.md">Overview</a></h3>
  <p>Namespace map, the parse → resolve → view pipeline, and where each guide fits.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-configuration/parsing-and-profiles.md">Parsing and profiles</a></h3>
  <p><code>ConfigurationDocument.Parse</code>, <code>ConfigurationParseOptions</code>, and the four profile presets — inline comments, duplicate handling, length limits.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-configuration/views-and-resolution.md">Views and resolution</a></h3>
  <p><code>Resolve</code> → <code>ConfigurationView</code>: glob matching against a target path, key projection, typed getters, missing-key fallbacks.</p>
</div>

<div class="bodu-card">
  <h3><a href="../text-configuration/diagnostics.md">Diagnostics</a></h3>
  <p>The structured diagnostic surface — modes, severities, and the full <code>ConfigurationDiagnosticCode</code> catalogue.</p>
</div>

</div>

[Bodu.Text.Configuration API reference](xref:Bodu.Text.Configuration)

## Bodu.Extensions.Configuration.Text

Surface a parsed and resolved document through the standard `IConfiguration` pipeline that ASP.NET Core and Generic Host already consume.

<div class="bodu-cards">

<div class="bodu-card">
  <h3><a href="../extensions-configuration-text/index.md">Overview</a></h3>
  <p>Namespace map — builder extensions, file and stream sources and providers, DI options helpers.</p>
</div>

<div class="bodu-card">
  <h3><a href="../extensions-configuration-text/configuration-sources.md">Configuration sources</a></h3>
  <p><code>AddBoduConfigurationFile</code> / <code>AddBoduConfigurationStream</code>, the conventional file probe, reload-on-change, target-path anchoring, and <code>IOptions&lt;T&gt;</code> binding.</p>
</div>

</div>

[Bodu.Extensions.Configuration.Text API reference](xref:Bodu.Extensions.Configuration.Text)

## Suggested reading path

1. **[Parsing and profiles](../text-configuration/parsing-and-profiles.md)** — get a document out of source text and pick the right profile.
2. **[Views and resolution](../text-configuration/views-and-resolution.md)** — project the document for a target path and read typed values.
3. **[Diagnostics](../text-configuration/diagnostics.md)** — handle user-authored input that may not be canonical.
4. **[Configuration sources](../extensions-configuration-text/configuration-sources.md)** — hand the result to `Microsoft.Extensions.Configuration` when you host there.

## See also

- **[Configuration overview](../../docs/topics/configuration.md)** — the topic landing page: pipeline, package table, decision table, install commands.
- **[Configuration concepts](../../docs/topics/configuration-concepts.md)** — the cross-package vocabulary.
- **[Bodu.Text.Configuration getting started](../../docs/text-configuration/getting-started.md)** and **[Bodu.Extensions.Configuration.Text getting started](../../docs/extensions-configuration-text/getting-started.md)** — install + minimal runnable samples.
