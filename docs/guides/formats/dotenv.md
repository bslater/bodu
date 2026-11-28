---
title: Using DotEnv
---

# Using DotEnv

`Bodu.Text.DotEnv` reads and writes `.env` files - a flat object of `KEY=value` entries with `export` prefixes, quoting, and comments. Values are deliberately **literal**: no `${VAR}` interpolation happens at parse time.

## Pattern 1 - query a document

<!-- compile -->
```csharp
using Bodu.Text.DotEnv.Document;

using DotEnvDocument env = DotEnvDocument.Parse(File.ReadAllBytes(".env"));
DotEnvElement root = env.RootElement;

string url = root.GetProperty("DATABASE_URL").GetString();
bool hasOptional = root.TryGetProperty("OPTIONAL_KEY", out DotEnvElement value);

foreach (DotEnvProperty property in root.EnumerateObject())
{
    Console.WriteLine($"{property.Name} = {property.Value.GetString()}");
}
```

## Pattern 2 - typed settings via the serializer

The `Web` defaults apply the SCREAMING_SNAKE_CASE naming policy with case-insensitive matching, so conventional env keys bind onto PascalCase members:

```csharp
using Bodu.Text.DotEnv;

sealed class Settings
{
    public string? AppEnv { get; set; }       // APP_ENV
    public int AppPort { get; set; }          // APP_PORT
    public string? DatabaseUrl { get; set; }  // DATABASE_URL
}

Settings settings = DotEnvSerializer.Deserialize<Settings>(
    envText, new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
```

The root may be a class or a structure, nullable or not: a nullable structure is read into the structure and written as the structure is. `Deserialize<Dictionary<string, string>>` binds the whole file as a dictionary. Any type that implements `IDictionary<string, TValue>` binds the same way in both directions, whether or not it also implements the non-generic `IDictionary`. A dictionary whose type is not keyed by `string`, such as `Hashtable`, `SortedList` or `Dictionary<int, string>`, is a collection rather than an object: `Serialize` and `Deserialize` reject it with `DotEnvSerializationException`, as they reject a `List<T>`. Each member or dictionary entry is one value, so its type must be a scalar (a string or `char`, a number, `bool`, an enum, a date, time or time span, `Guid` or `Uri`) or `object` holding one. A type with a member of any other type, such as a nested dictionary or a class, is rejected whole by `Serialize` and `Deserialize` alike, with `DotEnvSerializationException` naming the key, before anything is written or read and whatever the member holds or the ignore condition; a dictionary entry of such a type, or an `object` member holding such a value, is rejected when it is written. No such value is written as its `ToString()` text, which does not read back. The write direction (`Serialize`) emits `KEY=value` lines; `DotEnvSerializerOptions.WriteExportPrefix` adds the `export` keyword.

## Pattern 3 - author and round-trip with the mutable DOM

<!-- compile -->
```csharp
using Bodu.Text.DotEnv.Nodes;

DotEnvObject root = DotEnvNode.Parse(File.ReadAllBytes(".env"));
root["APP_PORT"] = new DotEnvValue("9090");
root.SetExport("APP_PORT", true);              // emit "export APP_PORT=9090"
File.WriteAllBytes(".env", root.ToUtf8Bytes());
```

The mutable DOM preserves each entry's `export` flag through the round trip.

## Pattern 4 - read tokens in blocks

For input that arrives in pieces, build a `Utf8DotEnvReader` over each block with `isFinalBlock: false` and a <xref:Bodu.Text.DotEnv.Reader.DotEnvReaderState>. `Read()` returns `false` at an entry or a comment line that the block cuts short, before it reports any of its tokens, and `BytesConsumed` stops before it. Carry the bytes from there into the next buffer, add the data that has arrived, and build the next reader from that buffer and the previous reader's `CurrentState`; the last block is read with `isFinalBlock: true`.

<!-- compile -->
```csharp
using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Reader;

static void PrintEntries(Stream stream)
{
    var state = new DotEnvReaderState(DotEnvReaderOptions.Default);
    byte[] buffer = new byte[4096];
    int carried = 0;
    bool isFinalBlock = false;

    while (!isFinalBlock)
    {
        if (carried == buffer.Length)
            Array.Resize(ref buffer, buffer.Length * 2);   // an entry longer than the buffer

        int read = stream.Read(buffer, carried, buffer.Length - carried);
        isFinalBlock = read == 0;
        int length = carried + read;

        var reader = new Utf8DotEnvReader(buffer.AsSpan(0, length), isFinalBlock, state);
        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.PropertyName)
                Console.Write($"{reader.GetString()}=");
            else if (reader.TokenType == DotEnvTokenType.String)
                Console.WriteLine(reader.GetString());
        }

        state = reader.CurrentState;
        carried = length - reader.BytesConsumed;
        buffer.AsSpan(reader.BytesConsumed, carried).CopyTo(buffer);   // the unread bytes start the next block
    }
}
```

Over a block that is not the last, an entry counts as read once its line ending has arrived, after the closing quote of a double-quoted value that runs across lines, so the buffer must grow to hold the longest entry. A carriage return that ends a block waits for the byte after it, which may be the line feed of a CRLF; a multi-byte whitespace character, or a word that may yet be the `export` prefix, waits for the bytes that settle it. The state carries the options, and `LineNumber` and the line, column and byte offset of a `DotEnvFormatException` count from the start of the whole input, a column from the start of its line even when that line began in an earlier block, while `BytesConsumed` and `ValueSpan` stay relative to the block. A byte-order mark is skipped only at the start of the input. A reader built without a state reads its bytes as the whole input, as before.

## Quoting rules

- **Double quotes** delimit values and may span lines. They resolve the escapes `\\`, `\'`, `\"`, `\a`, `\b`, `\f`, `\n`, `\r`, `\t`, `\v` and `\$`, and a backslash before a line break continues the value on the next line; any other escape keeps its backslash, and octal escapes are not supported.
- **Single quotes** delimit literal values.
- **Unquoted** values are trimmed and end at an inline `#` comment, a `#` with whitespace before it: `KEY= # note` reads as an empty value, while `KEY=#note` and `URL=http://host/#top` keep their `#`.
- An empty value (`KEY=`) is a real value, distinct from an absent key.
- Whitespace is any Unicode whitespace character other than CR and LF, which end a line, so a form feed or a no-break space counts as well as a space or a tab. It may precede a key or a comment, follow `export`, and surround `=`, and it is trimmed from both ends of an unquoted value.
- After a closing quote, only whitespace and a `#` comment may follow on the line; any other text, as in `KEY="a"b` or `KEY='a' b`, is malformed input.

`DotEnvReaderOptions` can disable the `export` prefix or inline comments (`DisallowExportPrefix`, `DisallowInlineComments`).

## Exceptions

`DotEnvFormatException` for malformed input (unterminated quote, missing `=`), with line/offset. `DotEnvSerializationException` for binding failures.

## When *not* to use it

Anything with sections or nesting (INI or TOML), and layered configuration (`Bodu.Text.Configuration`).

## See also

- [Streams and token-level I/O](streaming.md) for the `Utf8DotEnvReader` token loop.
- [Using INI](ini.md) for sectioned configuration.
