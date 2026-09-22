# Bodu.Formats.Outlook.Msg.Samples.MessageReading

Reading an Outlook `.msg` message with `Bodu.Formats.Outlook.Msg`, over the shared
`Bodu.Formats.Outlook` MAPI value model. Two scenarios: the surface a consumer extracting mail
actually uses, and the tag-addressed property collection underneath it.

The reader is read-only by design, so this sample ships **no binary fixture** — it authors its own
`.msg` with `Bodu.IO.Compound` and reads it back, which keeps the run offline and deterministic and
makes the container layout visible rather than opaque. See `MsgAuthor.cs`.

```bash
dotnet run --project samples/Formats.Outlook/Bodu.Formats.Outlook.Msg.Samples.MessageReading
```

## Scenarios

### Opening a .msg and reading subject, recipients, and attachments

**Intent.** Show the surface a mail-extraction tool needs, and nothing more.

**What it does.** Sniffs the container with `IsMsgFile` rather than trusting the file extension,
opens it with `OutlookMessage.OpenRead`, prints the subject / sender / body conveniences, walks the
recipient table, and reads the single attachment's bytes through `OpenContentStream`.

**Expected output.** The subject, sender, and body come back exactly as authored. Two recipients
print with `RecipientType` resolved to `To` and `Cc` rather than the raw `1` and `2` stored on the
wire, and the attachment's CSV reads back byte for byte.

**APIs.** `OutlookMessage.IsMsgFile` / `OpenRead`, `Subject`, `SenderName`, `SenderEmailAddress`,
`BodyText`, `Recipients` (`OutlookRecipient`), `Attachments` (`OutlookAttachment.FileName`,
`Method`, `Size`, `OpenContentStream`).

### The property model under the conveniences

**Intent.** Show that the conveniences are a lookup into a decoded collection, not a separate
parse — and that any tag the file carries is reachable.

**What it does.** Enumerates every decoded property with its tag, type, and value; reads the
subject both through `message.Subject` and by its raw `0x0037` tag; then asks for a tag the message
does not carry.

**Expected output.** Every property authored by the sample appears with its type resolved. Both
readings of the subject return the same string. The missing tag reports absence through
`TryGetValue` rather than throwing.

**APIs.** `OutlookMessage.Properties` (`MapiPropertyCollection`), `MapiProperty.Tag` / `Value`,
`MapiPropertyTag` (`Id`, `Type`), `MapiPropertyIds`, `MapiPropertyType`.

## Layout

```
  Program.cs                       # authors the .msg, runs both scenarios, deletes it
  MsgAuthor.cs                     # writes a minimal MS-OXMSG container, with the layout documented
  SampleConsole.cs                 # the what/why/expect banner every scenario opens with
  Scenarios/ReadingAMessage.cs     # the consumer-facing surface
  Scenarios/PropertyModel.cs       # the tag-addressed collection underneath
```

## Equivalent NuGet references

```bash
dotnet add package Bodu.Formats.Outlook.Msg
dotnet add package Bodu.IO.Compound
```

## Related

- `samples/IO.Pst` — the `.pst` mail store, where the same value model is reached through
  `OutlookMailStore` over a far larger container.
- `samples/IO.Compound` — the OLE2 container reader, editor, and writer a `.msg` is built on.
