---
uid: Bodu.Text.DotEnv
---

![Bodu.Text.DotEnv](~/images/hero-dotenv.svg)

## Purpose

**Bodu.Text.DotEnv** parses and emits **.env**-style environment files — `KEY=VALUE` lines with optional quoting, escapes, and comments — using a strongly-typed document model and a static codec with `Parse` / `Format` / `Try*` overloads over `ReadOnlySpan<char>` / `string` / `Stream` / `TextReader` / `TextWriter`. It is one of four format namespaces shipped by the **Bodu.Text.Formats** package; see also <xref:Bodu.Text.Bencode>, <xref:Bodu.Text.Delimited>, and <xref:Bodu.Text.Ini>.

## Key types

- <xref:Bodu.Text.DotEnv.DotEnv> — static codec exposing `Parse`, `Format`, `TryParse`, and the streaming variants.
- <xref:Bodu.Text.DotEnv.DotEnvDocument> — parsed document: ordered <xref:Bodu.Text.DotEnv.DotEnvEntry> values with indexer access by key.
- <xref:Bodu.Text.DotEnv.DotEnvEntry> — a single `key=value` line with optional comment, quoting state, and source position.
- <xref:Bodu.Text.DotEnv.DotEnvParseOptions> — duplicate-key, comment-preservation, variable-expansion, and quote-handling options.
- <xref:Bodu.Text.DotEnv.DotEnvDuplicateKeyBehavior> — `LastWins`, `FirstWins`, `Disallowed`.
- <xref:Bodu.Text.DotEnv.DotEnvFormatException> — derives from <xref:System.FormatException>; thrown on malformed input.

## Example

```csharp
using Bodu.Text.DotEnv;

DotEnvDocument env = DotEnv.Parse("""
HOST=localhost
PORT=8080
DEBUG=True
""");

Assert.AreEqual("localhost", env["HOST"]);
Assert.AreEqual("8080",      env["PORT"]);

string formatted = DotEnv.Format(env);
```

## Notes

- **Quoting rules.** Single-quoted values are taken verbatim; double-quoted values honour the standard backslash escapes (`\n`, `\t`, `\\`, `\"`); unquoted values are trimmed and have no escape processing.
- **Order preservation.** Entries are stored in source order; round-tripping under default options reproduces the original layout.
- **Configuration consumers.** When you need EditorConfig-style profile layering or Microsoft.Extensions.Configuration integration, prefer <xref:Bodu.Text.Configuration> (INI-backed) over a raw `DotEnvDocument`.
- **See also:** the [Bodu.Text.Formats introduction](~/docs/formats/index.md) and [getting-started](~/docs/formats/getting-started.md) pages.
