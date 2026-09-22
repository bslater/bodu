# Outlook Format Samples

Console applications demonstrating the Outlook format readers — `Bodu.Formats.Outlook.Msg` for a
single `.msg` message, over the shared `Bodu.Formats.Outlook` MAPI value model. Run one with:

```bash
dotnet run --project samples/Formats.Outlook/<SampleName>
```

Every sample is offline and deterministic. The readers are read-only by design, so the message
sample authors its own input with `Bodu.IO.Compound` rather than shipping a binary `.msg` fixture —
which also makes the container layout visible instead of opaque.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Formats.Outlook.Msg.Samples.MessageReading` | Opening a `.msg` with `OutlookMessage`: subject, sender, and body conveniences; the recipient table with `RecipientType` resolved to `To`/`Cc`; attachments read through `OpenContentStream`; and the tag-addressed `MapiPropertyCollection` the conveniences are shorthand for | `Bodu.Formats.Outlook.Msg`, `Bodu.IO.Compound` |

## Related

- `samples/IO.Pst` — the `.pst` mail-store reader (`OutlookMailStore`) and the low-level
  `Bodu.IO.Pst` container layer beneath it.
- `samples/IO.Compound` — the OLE2 container reader, editor, and writer a `.msg` is built on.
