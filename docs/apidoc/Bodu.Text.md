---
uid: Bodu.Text
---

![Bodu.Text](~/images/hero-text.svg)

# Bodu.Text

## Purpose

The **Bodu.Text** namespace serves two complementary roles:

- It is the home of the **`Bodu.Text` library** — allocation-conscious helpers for the BCL <xref:System.Text.Encoding> itself: zero-ceremony `string`↔`byte[]` conversion, pooled / owned-memory surfaces, byte-order-mark (BOM / preamble) handling, UTF classification, fallback configuration, and chunked transcoding. (These are distinct from the binary-to-text *radix* encodings, which live in [`Bodu.Text.Encoding`](Bodu.Text.Encoding.md), and from the include/exclude pattern filtering in [`Bodu.Text.Filtering`](Bodu.Text.Filtering.md).)
- It sits alongside the **line-format libraries** ([`Bodu.Text.Delimited`](Bodu.Text.Delimited.md), [`Bodu.Text.DotEnv`](Bodu.Text.DotEnv.md), [`Bodu.Text.Ini`](Bodu.Text.Ini.md) — see the [line-formats introduction](~/docs/formats/index.md)), each of which throws its own precise `*FormatException` derived from <xref:System.FormatException>.

## Static documentation

- **[Encoding helpers and BOM detection](~/guides/text-encoding/encoding-helpers.md)** — the `System.Text.Encoding` convenience surface.
- **[Bodu.Text.Formats overview](~/guides/formats/index.md)** — the document-format codecs and their shared pipeline.

## Key types

**`System.Text.Encoding` helpers**

- <xref:Bodu.Text.StringEncodingExtensions> — extension methods on `string` (UTF-8 fast paths, pooled / preamble-aware conversion).
- <xref:Bodu.Text.EncodingExtensions> — extension methods on `System.Text.Encoding`, `Encoder`, and `Decoder` (BOM handling, classification, fallback configuration, chunked transcoding).
- <xref:Bodu.Text.EncodingDetection> — static BOM-sniffing.

## Example

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Document;

try
{
    using IniDocument doc = IniDocument.Parse(bytes);
}
catch (IniFormatException ex)
{
    // Each line-format library throws its own FormatException subtype with line/offset attached.
    Console.Error.WriteLine(ex.Message);
}
```

## Notes

- **Catch per format.** Each line-format library throws its own `*FormatException` (all derive from <xref:System.FormatException>) carrying the source position.
- **See also:** the per-format guides — [delimited](~/guides/formats/delimited.md), [DotEnv](~/guides/formats/dotenv.md), [INI](~/guides/formats/ini.md).
