# Bodu.CodeStyle

Roslyn analyzer and code fix package that enforces Bodu's XML documentation formatting policy and ships the
configuration model for future member-ordering rules.

## Projects

| Project | Role |
|---|---|
| `Bodu.CodeStyle.XmlDocumentation.Core` | Roslyn-free formatter engine. Token model, layout, wrapper, JSON config reader. |
| `Bodu.CodeStyle.XmlDocumentation.Analyzers` | Reports `BODUXML001` on misformatted documentation comments. |
| `Bodu.CodeStyle.XmlDocumentation.CodeFixes` | Provides the "Format XML documentation comment" code fix, including Fix All. |
| `Bodu.CodeStyle.XmlDocumentation` | NuGet packaging project producing `Bodu.CodeStyle.XmlDocumentation.nupkg`. |
| `Bodu.CodeStyle.Ordering.Core` | Configuration model for the (future) member-ordering analyzer. |

## BODUXML001

The analyzer reports `BODUXML001` when an XML documentation comment's layout differs from the active policy.
Defaults match the Bodu codebase conventions:

- `<summary>`, `<remarks>`, `<para>`, `<example>`, and `<list>` are block tags that emit on their own lines.
- `<param>`, `<typeparam>`, `<returns>`, `<exception>`, and `<value>` stay single-line when short enough.
- `<c>`, `<see>`, `<paramref>`, and `<typeparamref>` are inline atomic tokens and are never split across lines.
- Lines wrap at `120` characters by default, breaking only between tokens.
- If a single atomic token exceeds the configured maximum, the line is allowed to exceed it rather than corrupt
  the content.

## Configuration

The analyzer reads policy from three layers, applied in order:

1. **Defaults in code** — `XmlDocFormatPolicyDefaults.CreateBoduDefaults()`.
2. **JSON additional file** — `bodu.xmldocstyle.json` added as `<AdditionalFiles>` in the consumer csproj. The
   JSON shape mirrors `XmlDocFormatOptions` (`maxLineLength`, `documentationPrefix`, `blockTags`, `inlineTags`,
   `forceMultilineTags`, `singleLineWhenShort`, `neverSplitTagContent`, `tagPolicies`).
3. **`.editorconfig` scalar overrides** — keys such as `bodu_xmldoc_max_line_length`, plus the standard
   `dotnet_diagnostic.BODUXML001.severity` and `end_of_line`.

## Building

```bash
cd Bodu.CodeStyle
dotnet build Bodu.CodeStyle.sln -c Release
dotnet test Bodu.CodeStyle.sln -c Release
dotnet pack Bodu.CodeStyle.XmlDocumentation/Bodu.CodeStyle.XmlDocumentation.csproj -c Release -o ./artifacts
```

The packaging project produces a NuGet that lays out the analyzer, code fix, and Core DLLs under
`analyzers/dotnet/cs/`, matching Microsoft's documented analyzer NuGet layout.

## Status

- Milestone 1 — Core formatter: shipped.
- Milestone 2 — `BODUXML001` analyzer + code fix + Fix All: shipped.
- Milestone 3 — Configuration (defaults + JSON + `.editorconfig`): shipped.
- Milestone 4 — Member ordering: configuration model shipped. Analyzer / code fix deferred.
- Milestone 5 — CLI: deferred.
- Milestone 6 — VSIX: deferred. The architecture keeps formatting logic in
  `Bodu.CodeStyle.XmlDocumentation.Core` so a future VSIX can reuse the same engine.
