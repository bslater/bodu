---
title: Line formats — Introduction
---

# The Bodu line formats

The Bodu line-oriented text formats — **Delimited** (RFC 4180 CSV / TSV), **DotEnv**, and **INI** — ship as three standalone libraries, each shaped after `System.Text.Json`:

- **`Bodu.Text.Delimited`** — tabular records with the RFC 4180 quoting rules and real-world dialect policies.
- **`Bodu.Text.DotEnv`** — flat `KEY=value` environment files with `export` prefixes, quoting, and inline comments.
- **`Bodu.Text.Ini`** — sectioned `[name]` / `key=value` configuration files with a comment-preserving mutable model.

The **`Bodu.Text.Formats`** package is a thin umbrella that references all three; depend on it when you want the whole family, or on a single format package when you do not.

## Key concepts

Every format exposes the same quartet, mirroring `Bodu.Text.Bencode` / `Bodu.Text.Toml` / `Bodu.Text.Yaml`:

| Surface | Delimited | DotEnv | INI |
|---|---|---|---|
| Token reader / writer (ref struct, UTF-8) | `Utf8DelimitedReader` / `Utf8DelimitedWriter` | `Utf8DotEnvReader` / `Utf8DotEnvWriter` | `Utf8IniReader` / `Utf8IniWriter` (+ normalized `IniDocumentReader`) |
| Serializer (POCO ↔ format) | `DelimitedSerializer` | `DotEnvSerializer` | `IniSerializer` |
| Mutable DOM | `DelimitedNode` / `DelimitedArray` / `DelimitedObject` / `DelimitedValue` | `DotEnvNode` / `DotEnvObject` / `DotEnvValue` | `IniNode` / `IniObject` / `IniValue` |
| Read-only DOM (`IDisposable`) | `DelimitedDocument` / `DelimitedElement` | `DotEnvDocument` / `DotEnvElement` | `IniDocument` / `IniElement` |

Two properties distinguish the line formats from the structured-text quartet:

### String-only wire

All three formats carry values as **text**. The serializers convert scalars (numbers, booleans, dates, GUIDs) with `InvariantCulture` at the binding layer; there is no per-format number or date syntax.

### Trivia in the mutable DOMs

The read-only `*Document` DOMs are trivia-free, like the rest of the quartet. The **mutable DotEnv and INI DOMs deliberately preserve trivia** — DotEnv keeps the per-entry `export` flag, and the INI node tree carries leading and trailing comment lines — so tooling can rewrite files a human still owns without destroying their annotations.

## Worked example — a small INI document

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Document;

byte[] ini = File.ReadAllBytes("app.ini");
using IniDocument document = IniDocument.Parse(ini);

IniElement root = document.RootElement;
string environment = root.GetProperty("environment").GetString(); // global key
string host = root.GetProperty("server").GetProperty("host").GetString();
```

## INI primitives vs. configuration layering

`Bodu.Text.Ini` is the general-purpose INI library. `Bodu.Text.Configuration` is the EditorConfig-style configuration engine (profiles, glob resolution, typed views) built over its **own** internal INI document model — it does not depend on the format packages.

## Where to go next

- [Getting started](getting-started.md) — install and first parse/serialize calls per format.
- [Core concepts](concepts.md) — the quartet vocabulary.
- [Parser policies](parser-policies.md) — the dialect and strictness knobs.
- The [format guides](../../guides/formats/index.md) — recipe-style walk-throughs.
