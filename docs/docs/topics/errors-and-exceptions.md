---
title: Errors and exceptions
---

# Errors and exceptions

Bodu packages report failures the way the .NET class libraries do: a bad argument throws an `ArgumentException`, malformed input throws a `FormatException`, and a missing item throws a `KeyNotFoundException`. Where a package can say more, it throws a derived type that carries the position, the category, or the diagnostics you need to report or recover. This page lists those types across the suite, what each tells you, and which ones to catch.

## Three rules that hold everywhere

1. **Invalid arguments throw the standard argument exceptions.** Public members validate their parameters before doing any work, through `Bodu.Core`'s `ThrowHelper`, and throw `ArgumentNullException`, `ArgumentOutOfRangeException`, or `ArgumentException` before doing any work. `ParamName` is the name of the parameter at fault. These are programming errors: fix the call rather than catching them.
2. **Malformed input throws a `FormatException`.** Every format exception in the suite derives from `FormatException`, so `catch (FormatException)` handles malformed input from any reader. Catch the derived type when you want its position or category.
3. **Do not parse exception messages.** Messages come from each package's resource file and are formatted with the current culture; their wording can change in any release. Read the typed properties instead - they are part of the API contract.

## Text formats and serializers

Each format throws one exception for input that is not valid text in the format, and the serializers throw a second for a document that is valid but cannot be mapped onto your type.

| Package | Malformed input | Position | Cannot map to your type |
|---|---|---|---|
| `Bodu.Text.Toml` | `TomlFormatException` | `LineNumber`, `ColumnNumber`, `Offset` | `TomlSerializationException` - `LineNumber`, `ColumnNumber`, `Offset`, `Path` |
| `Bodu.Text.Yaml` | `YamlFormatException` | `LineNumber`, `ColumnNumber`, `Offset` | `YamlSerializationException` - `LineNumber`, `ColumnNumber`, `Offset`, `Path` |
| `Bodu.Text.Bencode` | `BencodeFormatException` | `Offset` | `BencodeSerializationException` - `BytesOffset` |
| `Bodu.Text.Delimited` | `DelimitedFormatException` | `LineNumber`, `Offset` | `DelimitedSerializationException` |
| `Bodu.Text.DotEnv` | `DotEnvFormatException` | `LineNumber`, `ColumnNumber`, `Offset` | `DotEnvSerializationException` |
| `Bodu.Text.Ini` | `IniFormatException` | `LineNumber`, `Offset` | `IniSerializationException` |

The position properties are nullable, and are `null` when no line, column, or offset is associated with the error. The line formats can also tolerate bad input instead of throwing - a Delimited reader can skip a malformed record, for example - through their reader options; the [error-handling guide](../../guides/formats/error-handling.md) shows both families and the dialect policies.

`Bodu.Text.Configuration` throws `ConfigurationParseException` (a `FormatException`) only in its default `Throw` diagnostic mode. Its `Diagnostic` property is the error that stopped the parse, with its `Location`; in `Collect` mode the parser gathers every diagnostic and returns them instead of throwing. See [diagnostic mode](../text-configuration/concepts.md#diagnostic-mode).

## Containers and binary formats

The container readers sort their failures into categories, so you can tell a truncated download from a corrupt file without reading the message.

| Package | Base type | Derived types | What to read |
|---|---|---|---|
| `Bodu.IO.Compound` | `CompoundFileException` | `CompoundFileFormatException`, `CompoundStreamNotFoundException`, `CompoundFileSerializationException` | `CompoundFileFormatException.Category`, a `CompoundFileError` such as `TruncatedFile`, `FatCycle`, or `InvalidDirectory`; `CompoundStreamNotFoundException.StreamName`. |
| `Bodu.IO.Biff` | `FormatException`, `NotSupportedException` | `BiffFormatException`, `BiffUnsupportedVersionException` | `BiffFormatException.Offset`, the position in the record stream; `BiffUnsupportedVersionException.RawVersion`. |
| `Bodu.Formats.Excel.Binary` | `FormatException`, `NotSupportedException`, `KeyNotFoundException` | `ExcelBinaryFormatException`, `ExcelBinaryUnsupportedException`, `ExcelBinaryEncryptedWorkbookException`, `ExcelBinaryWorkbookStreamNotFoundException` | The type itself. A failure in the BIFF codec is rethrown as the Excel type with the codec's exception as `InnerException`; a file that is not a valid compound file throws the container's `CompoundFileFormatException`, unwrapped. |
| `Bodu.IO.Pst` | `PstFileException` | `PstFileFormatException`, `PstUnsupportedFormatException`, `PstNodeNotFoundException` | `PstFileException.Error`, a `PstFileError` such as `InvalidHeader`, `InvalidBlock`, or `LimitExceeded`. |
| `Bodu.Formats.Outlook.Msg` | `OutlookFormatException` | `OutlookMsgFormatException` | A container fault (`CompoundFileException`) is wrapped, as `InnerException`. |
| `Bodu.Formats.Outlook.Pst` | `OutlookFormatException` | `OutlookPstFormatException` | Every failure to read the store is wrapped: the underlying `PstFileException`, with its `Error`, is the `InnerException`. Argument, I/O, and disposal exceptions pass through unwrapped. |

`OutlookFormatException` is a `FormatException`, and `catch (OutlookFormatException)` handles both Outlook readers. A file that is merely damaged can often still be read in part: see [reading untrusted input](untrusted-input.md) for the validation levels and the option that omits an unreadable value instead of failing.

## Calendars

| Exception | Thrown when | What to read |
|---|---|---|
| `NotableDateValidationException` | A rule document fails validation while it loads. | `Diagnostics`: every error, each with a stable `BODU-CAL-*` code. Load in collect mode to get them without an exception. |
| `NotableDateBinaryFormatException` (a `FormatException`) | A compiled `.bcal` pack cannot be read: it is not a pack, declares an unsupported format version, is truncated or corrupted, or carries a value outside the format's tables. | The type itself. |
| `PluginNotTrustedException` | The trust policy rejects a plugin assembly. | `AssemblyName` and `Reason`. |
| `PluginMissingAttributeException`, `PluginActivationException` | A candidate assembly declares no `NotableDatePluginAttribute`, or the plugin type it declares cannot be activated or does not implement `INotableDatePlugin`. | `AssemblyName`; `PluginType`. |

The three plugin exceptions derive from `NotableDatePluginException`. The [validation diagnostics guide](../../guides/calendar/validation-diagnostics.md) lists every `BODU-CAL-*` code and its condition, and the same codes are what the `bodu-calendar` tool and the MSBuild integration print.

## Exchange rates

The providers use the standard types: a date the loaded data cannot resolve throws `KeyNotFoundException` from `GetRate` (use `TryGetRate` to avoid it), a pair a single-base feed does not quote throws `RateSeriesNotFoundException` (a `KeyNotFoundException`), a downloaded payload that cannot be parsed throws `ExchangeRateFormatException` (a `FormatException`), and a transport failure surfaces as `HttpRequestException` after the resilience pipeline's retries. The [failure modes table](../exchange-rates/concepts.md#failure-modes) has every case.

## Cryptography

The cryptographic types follow the BCL: an AEAD tag or Poly1305 check that fails throws `CryptographicException`, while signature and one-time-password verification return `false` instead of throwing. An AEAD transform is single-use, so a second `Encrypt` or `Decrypt`, even after a failed one, throws `InvalidOperationException`. The [security posture guide](../../guides/cryptography/security-posture.md#the-exception-contract) has the full exception contract.

## Where to go next

- **[Reading untrusted input](untrusted-input.md)** - the limits each reader enforces, and the exception each one throws when a limit trips.
- **[Text formats error handling](../../guides/formats/error-handling.md)** - catching and tolerating malformed Delimited, DotEnv, and INI input.
- **[Calendar validation diagnostics](../../guides/calendar/validation-diagnostics.md)** - the `BODU-CAL-*` codes.
