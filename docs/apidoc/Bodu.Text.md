---
uid: Bodu.Text
---

![Bodu.Text](~/images/hero-text.svg)

# Bodu.Text

## Purpose

The **Bodu.Text** namespace serves two complementary roles:

- It is the home of the **`Bodu.Text` library** — allocation-conscious helpers for the BCL <xref:System.Text.Encoding> itself: zero-ceremony `string`↔`byte[]` conversion, pooled / owned-memory surfaces, byte-order-mark (BOM / preamble) handling, UTF classification, fallback configuration, and chunked transcoding. (These are distinct from the binary-to-text *radix* encodings, which live in [`Bodu.Text.Encoding`](Bodu.Text.Encoding.md).)
- It also holds the **shared base exception** for the document-format codecs in [`Bodu.Text.Formats`](~/docs/formats/index.md), so callers can catch any format-level parse failure — delimited (CSV/TSV), DotEnv, or INI — through a single common type while each codec still throws its own precise subtype.

## Static documentation

- **[Encoding helpers and BOM detection](~/guides/text-encoding/encoding-helpers.md)** — the `System.Text.Encoding` convenience surface.
- **[Bodu.Text.Formats overview](~/guides/formats/index.md)** — the document-format codecs and their shared pipeline.

## Key types

**`System.Text.Encoding` helpers**

- <xref:Bodu.Text.StringEncodingExtensions> — extension methods on `string` (UTF-8 fast paths, pooled / preamble-aware conversion).
- <xref:Bodu.Text.EncodingExtensions> — extension methods on `System.Text.Encoding`, `Encoder`, and `Decoder` (BOM handling, classification, fallback configuration, chunked transcoding).
- <xref:Bodu.Text.EncodingDetection> — static BOM-sniffing.

**Document-format exceptions**

- <xref:Bodu.Text.TextFormatException> — the abstract base for every format-specific parse exception in the formats library. Concrete subtypes are <xref:Bodu.Text.Delimited.DelimitedFormatException>, <xref:Bodu.Text.DotEnv.DotEnvFormatException>, and <xref:Bodu.Text.Ini.IniFormatException>.

## Example

```csharp
using Bodu.Text;
using Bodu.Text.Ini;

try
{
    IniDocument doc = Ini.Parse(text);
}
catch (TextFormatException ex)
{
    // Catches IniFormatException — and any other Bodu.Text.Formats parse failure.
    Console.Error.WriteLine(ex.Message);
}
```

## Notes

- **Catch broad or narrow.** Catch <xref:Bodu.Text.TextFormatException> to handle any document-format failure uniformly, or the concrete subtype when you need format-specific detail.
- **See also:** the per-format guides — [delimited](~/guides/formats/delimited.md), [DotEnv](~/guides/formats/dotenv.md), [INI](~/guides/formats/ini.md).
