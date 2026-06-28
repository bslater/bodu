---
title: Choosing a text format
---

# Choosing a text format

`Bodu.Text.Formats` ships three document codecs — [Delimited
(CSV/TSV)](delimited.md), [DotEnv](dotenv.md), and [INI](ini.md). They
overlap enough that the first question is usually *which one*. This guide is
the decision aid: a shape-based matrix, then the two cross-cutting topics
that affect the choice — round-tripping / mutation, and parser error
recovery.

For the shared vocabulary (document, row, header, field, entry, section,
parse vs format options) see [Core concepts](../../docs/formats/concepts.md).

## Decision matrix

Pick by the *shape of your data* first; the format follows.

| Concern | Delimited (CSV/TSV) | DotEnv | INI |
|---|---|---|---|
| Data shape | Tabular — many rows, one column schema | Flat — a single list of `KEY=value` pairs | Hierarchical — keys grouped into named `[section]`s |
| Nesting | None (rectangular grid) | None (one level) | One level (section → key); no deeper nesting |
| Repetition | Many rows of the same columns | One value per key | One value per key per section |
| Comments | Optional (`#`), discarded on parse unless enabled | `#` full-line (preservable as trivia) and inline (truncates an unquoted value) | `#` / `;` full-line, preservable as trivia (no inline-comment parsing) |
| Mutation | Immutable document | Read-only document | Mutable document (add / remove sections and entries) |
| Round-trip fidelity | Values round-trip; layout does not | Values plus leading comments round-trip | Values, sections, and leading comments round-trip |
| Typical use | Exports, datasets, spreadsheets, bulk records | Environment / `.env` config for a process | Application or tool configuration with grouped settings |

Read it as three questions:

1. **Is the data a grid of records with a fixed column schema?** Use
   [Delimited](delimited.md). It is the only one of the three that models
   *rows*; the other two model a *map* of keys to values.
2. **Is it a flat bag of `KEY=value` settings for one process — the
   twelve-factor `.env` pattern?** Use [DotEnv](dotenv.md).
3. **Is it configuration that wants to be grouped under named headings?**
   Use [INI](ini.md). Its `[section]` layer is the one feature DotEnv lacks.

If you need *deeper* nesting than INI's single section level, none of these
three is the right tool — reach for the hierarchical serializers
([TOML](../serialization/toml/index.md), [YAML](../serialization/yaml/index.md), or
[Bencode](../serialization/bencode/index.md)) instead.

## Round-tripping and mutation

The three formats differ sharply in how faithfully a parse-then-format
cycle reproduces the input, and in whether the parsed document can be
edited in place.

### Round-trip fidelity

None of the three is byte-for-byte stable, but they preserve different
things:

- **Delimited** preserves field values, field order, and header order.
  Parse *discards* blank lines, comment lines, and whitespace inside
  unquoted fields, so the layout is not reproduced. `Delimited.Format`
  re-emits RFC 4180-conformant output: fields that need quoting are quoted,
  embedded quotes doubled, empty fields quoted.
- **DotEnv** preserves key/value pairs and — when `PreserveComments` is on
  (the default) — the full-line comments that lead each entry, so a
  comment-annotated `.env` file round-trips with its structure intact.
- **INI** preserves sections, entries, and (again under `PreserveComments`)
  the leading comments attached to each section and entry, so an annotated
  INI file round-trips its grouping and documentation.

In all three, set `PreserveComments: false` (DotEnv / INI) to treat the
input strictly as data and drop trivia.

```csharp
using Bodu.Text.Delimited;
using Bodu.Text.DotEnv;
using Bodu.Text.Ini;

// Delimited: values round-trip, layout does not.
DelimitedDocument records = Delimited.Parse(csv);
string csvOut = Delimited.Format(records);

// DotEnv: values plus leading comments round-trip.
DotEnvDocument env = DotEnv.Parse(dotenv);
string envOut = DotEnv.Format(env);

// INI: values, sections, and leading comments round-trip.
IniDocument config = Ini.Parse(ini);
string iniOut = Ini.Format(config);
```

### Mutation

The document types differ in mutability, and this often settles the choice
on its own:

| Format | Document mutability | How to produce edited output |
|---|---|---|
| Delimited | `DelimitedDocument` is immutable (`Headers` / `Rows` are read-only). | Build new rows and write with `DelimitedWriter`, or transform during a streaming read/write. |
| DotEnv | `DotEnvDocument` is read-only (`Entries` is `IReadOnlyList<DotEnvEntry>`). | Project to your own model, or re-emit with `DotEnvWriter`. |
| INI | `IniDocument` is mutable — add and remove sections and entries in place. | Edit the document, then `Ini.Format`. |

So INI is the natural choice when the workflow is *load, change a few
settings, save*. For Delimited and DotEnv, plan to write fresh output
through the format's writer rather than mutating the parsed document:

```csharp
using Bodu.Text.Ini;

// INI supports in-place edits.
IniDocument config = Ini.Parse(source);
IniSection database = config.GetOrAddSection("database");
database.AddEntry(new IniEntry("timeout", "30"));
database.RemoveEntry("legacy_flag");

string updated = Ini.Format(config);
```

## Parser error recovery

Each format exposes parse-options behavior enums that decide whether a
structural anomaly throws or is tolerated. The defaults are strict — they
surface problems — and you opt into leniency per anomaly. The exceptions
all derive from `TextFormatException` and carry a 1-based `LineNumber`.

### Delimited

Delimited has the richest set of recovery knobs, because tabular data has
the most ways to be malformed.

| Anomaly | Option | Default | Lenient choices |
|---|---|---|---|
| Row field count differs from the header | `FieldCountBehavior` (`DelimitedFieldCountBehavior`) | `Strict` — throws | `Ragged` — missing fields read as empty |
| Repeated header name | `DuplicateHeaderBehavior` (`DelimitedDuplicateHeaderBehavior`) | `Throw` | `FirstWins`, `LastWins`, `AllowDuplicates` |
| Text after a closing quote | `MalformedRecordBehavior` (`DelimitedMalformedRecordBehavior`) | `Throw` | `SkipRecord` — discard the record remainder |

```csharp
using Bodu.Text.Delimited;

var lenient = new DelimitedParseOptions
{
    FieldCountBehavior      = DelimitedFieldCountBehavior.Ragged,
    DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.LastWins,
    MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
};

DelimitedDocument document = Delimited.Parse(messyExport, lenient);
```

### DotEnv

DotEnv's only structural ambiguity is a repeated key, governed by the
shared `DuplicateKeyPolicy`. The default is `LastWins`, matching the
prevailing `.env` convention where a later assignment overrides an earlier
one.

| Anomaly | Option | Default | Other choices |
|---|---|---|---|
| Repeated key | `DuplicateKeyBehavior` (`DuplicateKeyPolicy`) | `LastWins` | `FirstWins`, `Disallowed` (throws) |

```csharp
using Bodu.Text;
using Bodu.Text.DotEnv;

var strict = new DotEnvParseOptions
{
    DuplicateKeyBehavior = DuplicateKeyPolicy.Disallowed,
};

DotEnvDocument env = DotEnv.Parse(source, strict);   // throws on any duplicate key
```

### INI

INI has both a key-level and a section-level ambiguity. Keys share the same
`DuplicateKeyPolicy` as DotEnv; sections use `IniDuplicateSectionBehavior`.

| Anomaly | Option | Default | Other choices |
|---|---|---|---|
| Repeated key within a section | `DuplicateKeyBehavior` (`DuplicateKeyPolicy`) | `LastWins` | `FirstWins`, `Disallowed` |
| Repeated section name | `DuplicateSectionBehavior` (`IniDuplicateSectionBehavior`) | `Merge` | `MergeAdjacent`, `Preserve`, `Disallowed` |
| Keys before the first section | `AllowGlobalSection` | `true` — collected into the global section | `false` — throws on the first such key |

```csharp
using Bodu.Text;
using Bodu.Text.Ini;

var options = new IniParseOptions
{
    DuplicateKeyBehavior     = DuplicateKeyPolicy.FirstWins,
    DuplicateSectionBehavior = IniDuplicateSectionBehavior.Preserve,
    AllowGlobalSection       = false,
};

IniDocument config = Ini.Parse(source, options);
```

The default `Merge` folds later occurrences of a section into the first —
the historical INI behavior. `Preserve` keeps repeated sections as separate
entries in source order so a downstream consumer can apply its own
precedence (this is the mode `Bodu.Text.Configuration` uses for
EditorConfig-style last-section-wins).

## See also

- [Using delimited (CSV / TSV)](delimited.md), [Using DotEnv](dotenv.md), [Using INI](ini.md) — the per-format walk-throughs.
- [Core concepts](../../docs/formats/concepts.md) — document model and parse vs format options.
- API reference — <xref:Bodu.Text.Delimited>, <xref:Bodu.Text.DotEnv>, <xref:Bodu.Text.Ini>.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
