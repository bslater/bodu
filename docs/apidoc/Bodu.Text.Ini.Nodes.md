---
uid: Bodu.Text.Ini.Nodes
---

![Bodu.Text.Ini.Nodes](~/images/hero-ini.svg)

## Purpose

**Bodu.Text.Ini.Nodes** is the mutable, **comment-preserving** document object model of <xref:Bodu.Text.Ini>: parse an INI file into an editable object-of-objects, change or add sections and entries, and write it back through the <xref:Bodu.Text.Ini.Writer.Utf8IniWriter> with the human-authored comments intact. It is the one sanctioned trivia-bearing DOM among the line formats, intended for faithful rewrites of files people also edit by hand. For read-only lookup use the sibling <xref:Bodu.Text.Ini.Document> tier; for typed binding use the <xref:Bodu.Text.Ini.IniSerializer>.

## Key types

- <xref:Bodu.Text.Ini.Nodes.IniNode> — abstract base: `Parse` (from `string` or UTF-8 bytes, returning the root object), `LeadingComments`, `AsObject` / `AsValue`, `ValueKind`, `DeepClone`, `WriteTo`, `ToUtf8Bytes`, and an implicit conversion from `string`.
- <xref:Bodu.Text.Ini.Nodes.IniObject> — the root or a section: `Keys`, `Count`, `ContainsKey` / `TryGetValue` / `Remove`, a string indexer that adds or replaces, and `TrailingComments`.
- <xref:Bodu.Text.Ini.Nodes.IniValue> — a single entry with a settable `Value`.

## Example

```csharp
using Bodu.Text.Ini.Nodes;

IniObject root = IniNode.Parse("; owned by ops\n[server]\nhost=localhost\n"u8);
IniObject server = root["server"].AsObject();

var port = new IniValue("8080");
port.LeadingComments.Add("listening port");
server["port"] = port;

byte[] back = root.ToUtf8Bytes();   // "; owned by ops" survives
```

## Notes

- **Two levels only.** The root holds global entries and sections; a section holds entries. Nested sections are not modeled.
- **Comment placement.** `LeadingComments` on an entry or section are emitted immediately before it (the root object's own list is never written — document-level comments attach to the first entry or section); `TrailingComments` on an object are emitted after its last entry.
- **Duplicate policies apply at parse.** `Parse` normalizes through <xref:Bodu.Text.Ini.IniDocumentOptions> (merge / last-wins by default), so the tree never contains duplicates.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [INI guide](~/guides/formats/ini.md) (Pattern 3 — comment-preserving edits with the mutable DOM).
