// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyDecoder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Materializes the decoded <see cref="MapiPropertyCollection" /> of an MS-OXMSG storage from its property stream and
/// the associated <c>__substg1.0_</c> value streams.
/// </summary>
/// <remarks>
/// Decoding is two-pass: the fixed 16-byte records are parsed first so the message code page is known before any
/// code-page (<c>PT_STRING8</c>) payload is decoded, regardless of the order the records appear in. Under
/// <see cref="CompoundValidationLevel.Strict" /> structural problems throw; under the tolerant levels the affected
/// property is omitted and decoding continues.
/// </remarks>
internal static class MsgPropertyDecoder
{
    /// <summary>The full tag of the message code-page property (<c>PidTagMessageCodepage</c>, Int32).</summary>
    private const uint MessageCodepageTag = 0x3FFD0003;

    /// <summary>The full tag of the internet code-page property (<c>PidTagInternetCodepage</c>, Int32).</summary>
    private const uint InternetCodepageTag = 0x3FDE0003;

    /// <summary>
    /// Decodes every property of a storage.
    /// </summary>
    /// <param name="storage">The message, recipient, or attachment storage.</param>
    /// <param name="kind">The storage kind, which determines the property-stream header shape.</param>
    /// <param name="validationLevel">The validation level governing malformed-content handling.</param>
    /// <param name="inheritedEncoding">
    /// The parent's string encoding, inherited when the storage declares no code page of its own;
    /// <see langword="null" /> at the root.
    /// </param>
    /// <param name="header">When this method returns, the decoded property-stream header.</param>
    /// <returns>The decoded property collection.</returns>
    /// <exception cref="OutlookMsgFormatException">
    /// The storage has no property stream, the stream is malformed, the container is corrupt, or — under
    /// <see cref="CompoundValidationLevel.Strict" /> — a property entry or value stream is invalid (including a
    /// declared size that disagrees with the value stream).
    /// </exception>
    internal static MapiPropertyCollection Decode(
        CompoundStorage storage,
        MsgPropertyStreamKind kind,
        CompoundValidationLevel validationLevel,
        Encoding? inheritedEncoding,
        out MsgPropertyStreamHeader header)
    {
        if (!MsgContainer.TryReadStream(storage, MsgStreamNames.PropertiesStreamName, out byte[]? payload))
            throw new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_MsgContainer);

        MsgPropertyEntry[] entries = MsgPropertyStreamReader.Read(payload, kind, out header);
        Encoding encoding = ResolveEncoding(entries, inheritedEncoding);

        var properties = new List<MapiProperty>(entries.Length);
        foreach (MsgPropertyEntry entry in entries)
        {
            if (TryDecodeEntry(storage, entry, encoding, validationLevel, out MapiProperty? property))
                properties.Add(property);
        }

        return new MapiPropertyCollection(properties);
    }

    /// <summary>
    /// Resolves the string encoding for a storage from its own code-page entries, the inherited encoding, or the
    /// fallback.
    /// </summary>
    /// <param name="entries">The parsed property records.</param>
    /// <param name="inheritedEncoding">The parent's encoding, or <see langword="null" /> at the root.</param>
    /// <returns>The encoding used to decode the storage's code-page strings.</returns>
    private static Encoding ResolveEncoding(MsgPropertyEntry[] entries, Encoding? inheritedEncoding)
    {
        int? messageCodePage = FindInt32(entries, MessageCodepageTag);
        int? internetCodePage = FindInt32(entries, InternetCodepageTag);

        if (messageCodePage is null && internetCodePage is null && inheritedEncoding is not null)
            return inheritedEncoding;

        return MapiEncodingResolver.GetEncoding(messageCodePage, internetCodePage);
    }

    /// <summary>
    /// Finds the inline 32-bit value of a fixed property record.
    /// </summary>
    /// <param name="entries">The parsed property records.</param>
    /// <param name="tag">The full tag to find.</param>
    /// <returns>The value, or <see langword="null" /> when the tag is absent.</returns>
    private static int? FindInt32(MsgPropertyEntry[] entries, uint tag)
    {
        foreach (MsgPropertyEntry entry in entries)
        {
            if (entry.Tag == tag)
                return (int)(uint)entry.Raw;
        }

        return null;
    }

    /// <summary>
    /// Decodes one property record into a <see cref="MapiProperty" />.
    /// </summary>
    /// <param name="storage">The owning storage, used to open value streams.</param>
    /// <param name="entry">The property record.</param>
    /// <param name="encoding">The storage's code-page string encoding.</param>
    /// <param name="validationLevel">The validation level governing malformed-content handling.</param>
    /// <param name="property">When this method returns <see langword="true" />, the decoded property.</param>
    /// <returns><see langword="true" /> when the record decodes; <see langword="false" /> when it is skipped.</returns>
    private static bool TryDecodeEntry(
        CompoundStorage storage,
        MsgPropertyEntry entry,
        Encoding encoding,
        CompoundValidationLevel validationLevel,
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out MapiProperty property)
    {
        var tag = new MapiPropertyTag(entry.Tag);
        bool strict = validationLevel == CompoundValidationLevel.Strict;
        property = null;

        object? value;
        if (tag.IsMultiValued)
        {
            if (!TryDecodeMultiValue(storage, tag, encoding, strict, out value))
                return false;
        }
        else if (tag.Type == MapiPropertyType.Object)
        {
            // The payload of PT_OBJECT is a child storage (for example, an embedded message), never a stream —
            // surface the entry as a marker so callers know the property exists without touching the payload here.
            value = null;
        }
        else if (IsVariableLength(tag.Type))
        {
            if (!TryReadSubstg(storage, tag.Value, strict, out byte[]? bytes))
                return false;

            // The record's size field is the stream length (string sizes count their terminator, which the stream
            // omits); a disagreement is a structural fault under strict validation and ignored otherwise.
            if (strict && !DeclaredSizeMatches(tag.Type, entry.Size, bytes.Length))
                throw MalformedEntry(tag);

            if (!TryDecodeVariableValue(tag, bytes, encoding, strict, out value))
                return false;
        }
        else
        {
            if (!TryDecodeFixedValue(tag, entry.Raw, strict, out value))
                return false;
        }

        property = new MapiProperty(tag, value);
        return true;
    }

    /// <summary>
    /// Determines whether a variable-length record's declared size agrees with its value stream.
    /// </summary>
    /// <param name="type">The base property type.</param>
    /// <param name="declared">The size the property record declares.</param>
    /// <param name="actual">The value stream's length.</param>
    /// <returns>
    /// <see langword="true" /> when the sizes match exactly, or differ by the string terminator (two bytes for a
    /// Unicode string, one for a code-page string) that the size counts and the stream omits.
    /// </returns>
    private static bool DeclaredSizeMatches(MapiPropertyType type, uint declared, int actual) =>
        declared == (uint)actual
            || (type == MapiPropertyType.Unicode && declared == (uint)actual + 2)
            || (type == MapiPropertyType.String8 && declared == (uint)actual + 1);

    /// <summary>
    /// Determines whether a base type stores its payload in a value stream.
    /// </summary>
    /// <param name="type">The base property type.</param>
    /// <returns><see langword="true" /> for the string, binary, and GUID types.</returns>
    private static bool IsVariableLength(MapiPropertyType type) =>
        MapiValueDecoder.IsVariableLength(type);

    /// <summary>
    /// Decodes a fixed-length value stored inline in the property record, mapping a shared-decoder rejection onto this
    /// format's skip-or-throw policy.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <param name="raw">The 8-byte inline value.</param>
    /// <param name="strict">Whether unsupported types throw instead of being skipped.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded value.</param>
    /// <returns>
    /// <see langword="true" /> when the value decodes; <see langword="false" /> when the entry is skipped.
    /// </returns>
    private static bool TryDecodeFixedValue(MapiPropertyTag tag, ulong raw, bool strict, out object? value)
    {
        if (MapiValueDecoder.TryDecodeFixedValue(tag.Type, raw, out value))
            return true;

        return !strict
            ? false
            : throw MalformedEntry(tag);
    }

    /// <summary>
    /// Decodes a variable-length payload read from a value stream, mapping a shared-decoder rejection onto this
    /// format's skip-or-throw policy.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <param name="bytes">The value-stream payload.</param>
    /// <param name="encoding">The storage's code-page string encoding.</param>
    /// <param name="strict">Whether malformed payloads throw instead of being skipped.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded value.</param>
    /// <returns>
    /// <see langword="true" /> when the payload decodes; <see langword="false" /> when it is skipped.
    /// </returns>
    private static bool TryDecodeVariableValue(MapiPropertyTag tag, byte[] bytes, Encoding encoding, bool strict, out object? value)
    {
        // Preserve the distinct strict-mode diagnostic for a structurally odd Unicode payload before the shared
        // decoder folds it into a generic rejection.
        if (strict && tag.Type == MapiPropertyType.Unicode && (bytes.Length & 1) != 0)
        {
            throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgStringPayload, tag));
        }

        if (MapiValueDecoder.TryDecodeVariableValue(tag.Type, bytes, encoding, strict, out value))
            return true;

        return !strict
            ? false
            : throw MalformedEntry(tag);
    }

    /// <summary>
    /// Decodes a multi-valued property from its length and element streams (variable-length element types) or its
    /// packed single stream (fixed-length element types).
    /// </summary>
    /// <param name="storage">The owning storage.</param>
    /// <param name="tag">The multi-valued property tag.</param>
    /// <param name="encoding">The storage's code-page string encoding.</param>
    /// <param name="strict">Whether malformed content throws instead of being skipped.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded array value.</param>
    /// <returns>
    /// <see langword="true" /> when the property decodes; <see langword="false" /> when it is skipped.
    /// </returns>
    private static bool TryDecodeMultiValue(CompoundStorage storage, MapiPropertyTag tag, Encoding encoding, bool strict, out object? value)
    {
        value = null;
        if (!TryReadSubstg(storage, tag.Value, strict, out byte[]? baseBytes))
            return false;

        switch (tag.Type)
        {
            case MapiPropertyType.Unicode:
            case MapiPropertyType.String8:
            case MapiPropertyType.Binary:
                // MS-OXMSG §2.1.3: the base stream is a length stream — 4-byte entries for the string types,
                // 8-byte entries for binary — and each element's payload lives in its own "-XXXXXXXX" stream.
                int entryWidth = tag.Type == MapiPropertyType.Binary ? 8 : 4;
                if (baseBytes.Length % entryWidth != 0)
                    return SkipOrThrowMultiValue(tag, strict);

                int count = baseBytes.Length / entryWidth;
                var elements = new object[count];
                for (int i = 0; i < count; i++)
                {
                    if (!TryReadElementStream(storage, tag.Value, i, strict, out byte[]? elementBytes)
                        || !TryDecodeVariableValue(
                            new MapiPropertyTag(tag.Id, tag.Type), elementBytes, encoding, strict, out object? element)
                        || element is null)
                        return SkipOrThrowMultiValue(tag, strict);

                    elements[i] = element;
                }

                value = tag.Type == MapiPropertyType.Binary
                    ? elements.Cast<byte[]>().ToArray()
                    : elements.Cast<string>().ToArray();
                return true;
            default:
                return TryDecodePackedMultiValue(tag, baseBytes, strict, out value);
        }
    }

    /// <summary>
    /// Decodes a multi-valued fixed-length property whose elements are packed contiguously in the base stream.
    /// </summary>
    /// <param name="tag">The multi-valued property tag.</param>
    /// <param name="bytes">The packed payload.</param>
    /// <param name="strict">Whether malformed content throws instead of being skipped.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded array value.</param>
    /// <returns>
    /// <see langword="true" /> when the property decodes; <see langword="false" /> when it is skipped.
    /// </returns>
    private static bool TryDecodePackedMultiValue(MapiPropertyTag tag, byte[] bytes, bool strict, out object? value)
    {
        if (MapiValueDecoder.TryDecodePackedMultiValue(tag.Type, bytes, out value))
            return true;

        return SkipOrThrowMultiValue(tag, strict);
    }

    /// <summary>
    /// Reads the value stream a variable-length or multi-valued entry references.
    /// </summary>
    /// <param name="storage">The owning storage.</param>
    /// <param name="tagValue">The raw property tag naming the stream.</param>
    /// <param name="strict">Whether a missing stream throws instead of skipping the property.</param>
    /// <param name="bytes">When this method returns <see langword="true" />, the stream payload.</param>
    /// <returns><see langword="true" /> when the stream exists.</returns>
    private static bool TryReadSubstg(
        CompoundStorage storage,
        uint tagValue,
        bool strict,
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out byte[] bytes)
    {
        string name = MsgStreamNames.GetSubstgStreamName(tagValue);
        if (MsgContainer.TryReadStream(storage, name, out bytes))
            return true;

        return !strict
            ? false
            : throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.IO_KeyNotFound_MsgSubstgStream, new MapiPropertyTag(tagValue), name));
    }

    /// <summary>
    /// Reads one element stream of a multi-valued variable-length property.
    /// </summary>
    /// <param name="storage">The owning storage.</param>
    /// <param name="tagValue">The raw multi-valued property tag.</param>
    /// <param name="index">The zero-based element index.</param>
    /// <param name="strict">Whether a missing stream throws instead of skipping the property.</param>
    /// <param name="bytes">When this method returns <see langword="true" />, the element payload.</param>
    /// <returns><see langword="true" /> when the element stream exists.</returns>
    private static bool TryReadElementStream(
        CompoundStorage storage,
        uint tagValue,
        int index,
        bool strict,
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out byte[] bytes)
    {
        string name = MsgStreamNames.GetMultiValueElementStreamName(tagValue, index);
        if (MsgContainer.TryReadStream(storage, name, out bytes))
            return true;

        return !strict
            ? false
            : throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.IO_KeyNotFound_MsgSubstgStream, new MapiPropertyTag(tagValue), name));
    }

    /// <summary>
    /// Skips a malformed multi-valued property, or throws under strict validation.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <param name="strict">Whether to throw.</param>
    /// <returns>Always <see langword="false" /> when not throwing.</returns>
    /// <exception cref="OutlookMsgFormatException">Thrown under strict validation.</exception>
    private static bool SkipOrThrowMultiValue(MapiPropertyTag tag, bool strict) =>
        !strict
            ? false
            : throw new OutlookMsgFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgMultiValueLength, tag));

    /// <summary>
    /// Creates the malformed-entry exception for a tag.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <returns>The exception to throw.</returns>
    private static OutlookMsgFormatException MalformedEntry(MapiPropertyTag tag) =>
        new(string.Format(CultureInfo.CurrentCulture, OutlookMsgResourceStrings.Format_Invalid_MsgPropertyEntry, tag));
}
