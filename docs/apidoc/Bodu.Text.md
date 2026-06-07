---
uid: Bodu.Text
---

# Bodu.Text

## Purpose

The **Bodu.Text** namespace holds the shared base type for the document-format codecs in [`Bodu.Text.Formats`](Bodu.Text.Bencode.md). It exists so that callers can catch any format-level parse failure — Bencode, delimited (CSV/TSV), DotEnv, or INI — through a single common exception type, while each codec still throws its own precise subtype.

## Static documentation

- **[Bodu.Text.Formats overview](~/guides/formats/index.md)** — the document-format codecs and their shared pipeline.

## Key types

- <xref:Bodu.Text.TextFormatException> — the abstract base for every format-specific parse exception in the formats library. Concrete subtypes include <xref:Bodu.Text.Bencode.BencodeFormatException>, <xref:Bodu.Text.Delimited.DelimitedFormatException>, <xref:Bodu.Text.DotEnv.DotEnvFormatException>, and <xref:Bodu.Text.Ini.IniFormatException>.

## Example

```csharp
using Bodu.Text;
using Bodu.Text.Ini;

try
{
    IniDocument doc = Ini.Decode(text);
}
catch (TextFormatException ex)
{
    // Catches IniFormatException — and any other Bodu.Text.Formats parse failure.
    Console.Error.WriteLine(ex.Message);
}
```

## Notes

- **Catch broad or narrow.** Catch <xref:Bodu.Text.TextFormatException> to handle any document-format failure uniformly, or the concrete subtype when you need format-specific detail.
- **See also:** the per-format guides — [Bencode](~/guides/formats/bencode.md), [delimited](~/guides/formats/delimited.md), [DotEnv](~/guides/formats/dotenv.md), [INI](~/guides/formats/ini.md).
