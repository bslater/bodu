---
uid: Bodu.Text.Bencode.Writer
---

![Bodu.Text.Bencode.Writer](~/images/hero-bencode.svg)

## Purpose

**Bodu.Text.Bencode.Writer** is the emitting counterpart of <xref:Bodu.Text.Bencode.Reader> in <xref:Bodu.Text.Bencode>: a forward-only token writer that produces canonical Bencode (BEP 3) bytes into an `IBufferWriter<byte>`. The <xref:Bodu.Text.Bencode.BencodeSerializer> and both DOMs (`WriteTo`) emit through it, and a custom <xref:Bodu.Text.Bencode.Serialization.BencodeConverter`1> writes its value through it.

## Key types

- <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> — the `ref struct` writer: `WriteStartList` / `WriteEndList`, `WriteStartDictionary` / `WriteEndDictionary`, `WritePropertyName`, `WriteInteger`, `WriteString` / `WriteByteString`, the name-plus-value conveniences, and `WriteRawValue`.
- <xref:Bodu.Text.Bencode.Writer.BencodeWriterOptions> — `MaxDepth` and `AllowMultipleRootValues`.

## Example

```csharp
using System.Buffers;
using Bodu.Text.Bencode.Writer;

var output = new ArrayBufferWriter<byte>();
var writer = new Utf8BencodeWriter(output);

writer.WriteStartDictionary();
writer.WriteString("cow", "moo");   // keys are sorted when the dictionary closes
writer.WriteInteger("age", 42);
writer.WriteEndDictionary();

// output.WrittenSpan now holds canonical "d3:agei42e3:cow3:mooe".
```

## Notes

- **Canonical ordering is automatic.** Each dictionary's entries are buffered and sorted bytewise when it is closed; root values and list items stream straight to the destination. There is no `Flush` — bytes are committed as they are emitted.
- **Grammar is enforced.** A property name outside a dictionary, a mismatched container end, a duplicate key, or a second root value (without `AllowMultipleRootValues`) throws <xref:System.InvalidOperationException>.
- **No booleans, floats, or date-times.** Reduce such values to an integer or byte string in a converter before writing.
- **See also:** the [Bodu.Text.Bencode introduction](~/docs/serialization/bencode/index.md) and the [Using Bencode](~/guides/serialization/bencode/using.md) guide (Pattern 9 — Process tokens by hand).
