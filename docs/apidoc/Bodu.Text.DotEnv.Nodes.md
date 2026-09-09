---
uid: Bodu.Text.DotEnv.Nodes
---

![Bodu.Text.DotEnv.Nodes](~/images/hero-dotenv.svg)

## Purpose

**Bodu.Text.DotEnv.Nodes** is the mutable document object model of <xref:Bodu.Text.DotEnv>: parse a `.env` file into an editable, insertion-ordered object, add or change entries, toggle each entry's `export` flag, and write it back through the <xref:Bodu.Text.DotEnv.Writer.Utf8DotEnvWriter>. It is the tier for programmatic authoring and rewrites; for read-only lookup use the sibling <xref:Bodu.Text.DotEnv.Document> tier, and for typed settings use the <xref:Bodu.Text.DotEnv.DotEnvSerializer>.

## Key types

- <xref:Bodu.Text.DotEnv.Nodes.DotEnvNode> — abstract base: `Parse` (from `string` or UTF-8 bytes, returning the root object), `AsObject` / `AsValue`, `ValueKind`, `DeepClone`, `WriteTo`, `ToUtf8Bytes`, and an implicit conversion from `string`.
- <xref:Bodu.Text.DotEnv.Nodes.DotEnvObject> — the root: `Keys`, `Count`, `ContainsKey` / `TryGetValue` / `Remove`, a string indexer that adds or replaces, and the per-entry `IsExport` / `SetExport`.
- <xref:Bodu.Text.DotEnv.Nodes.DotEnvValue> — a single entry with a settable `Value`.

## Example

```csharp
using Bodu.Text.DotEnv.Nodes;

DotEnvObject env = DotEnvNode.Parse("export APP_ENV=production\nAPP_PORT=8080\n");
env["APP_PORT"].Value = "9090";
env["APP_DEBUG"] = new DotEnvValue("false");
env.SetExport("APP_DEBUG", true);

byte[] back = env.ToUtf8Bytes();   // export flags survive the round trip
```

## Notes

- **Export flag preserved.** Unlike the other line-format node DOMs, each entry carries its `export` prefix through parse → edit → write.
- **Comments are not retained.** The DOM is otherwise trivia-free; the writer re-quotes values minimally on output.
- **See also:** the [line-formats introduction](~/docs/formats/index.md) and the [DotEnv guide](~/guides/formats/dotenv.md) (Pattern 3 — author and round-trip with the mutable DOM).
