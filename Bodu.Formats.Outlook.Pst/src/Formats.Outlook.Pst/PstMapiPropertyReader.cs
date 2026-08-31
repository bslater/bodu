// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstMapiPropertyReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Translates the container's wire-typed LTP values into the shared MAPI value model: a
/// <see cref="PstPropertyContext" /> becomes a <see cref="MapiPropertyCollection" />.
/// </summary>
/// <remarks>
/// <para>
/// Decoding is two-pass, mirroring the <c>.msg</c> reader: the code-page properties are read first so
/// <c>PT_STRING8</c> payloads decode with the object's declared encoding (inheriting the parent's when it declares
/// none, falling back to Windows-1252). Scalars, packed fixed-width multi-values, and FILETIME conversion ride the
/// shared <see cref="MapiValueDecoder" />; the PST-specific count-plus-offset-table layout of variable-size
/// multi-values (MS-PST §2.3.3.4.2) is decoded here.
/// </para>
/// <para>
/// Under strict validation an undecodable value throws <see cref="OutlookPstFormatException" />; under the tolerant
/// levels the property is omitted and decoding continues.
/// </para>
/// </remarks>
internal static class PstMapiPropertyReader
{
    /// <summary>
    /// Decodes a property context into the shared value model.
    /// </summary>
    /// <param name="context">The container's property context.</param>
    /// <param name="inheritedEncoding">
    /// The parent object's string encoding, inherited when this object declares no code page of its own;
    /// <see langword="null" /> at the store root.
    /// </param>
    /// <param name="strict">Whether undecodable values throw instead of being skipped.</param>
    /// <param name="encoding">When this method returns, the encoding the object's code-page strings decoded with.</param>
    /// <returns>The decoded property collection.</returns>
    /// <exception cref="OutlookPstFormatException">A value is undecodable and <paramref name="strict" /> is set.</exception>
    internal static MapiPropertyCollection Read(
        PstPropertyContext context,
        Encoding? inheritedEncoding,
        bool strict,
        out Encoding encoding)
    {
        encoding = ResolveEncoding(context, inheritedEncoding);

        var properties = new List<MapiProperty>(context.Count);
        foreach (PstPropertyValue value in context)
        {
            if (TryDecodeValue(value, encoding, strict, out MapiProperty? property))
                properties.Add(property);
        }

        return new MapiPropertyCollection(properties);
    }

    /// <summary>
    /// Decodes one wire-typed value into a <see cref="MapiProperty" />.
    /// </summary>
    /// <param name="value">The container value.</param>
    /// <param name="encoding">The owning object's code-page string encoding.</param>
    /// <param name="strict">Whether an undecodable value throws instead of being skipped.</param>
    /// <param name="property">When this method returns <see langword="true" />, the decoded property.</param>
    /// <returns><see langword="true" /> when the value decodes; <see langword="false" /> when it is skipped.</returns>
    /// <exception cref="OutlookPstFormatException">The value is undecodable and <paramref name="strict" /> is set.</exception>
    internal static bool TryDecodeValue(
        PstPropertyValue value,
        Encoding encoding,
        bool strict,
        [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out MapiProperty property)
    {
        var tag = new MapiPropertyTag(value.PropertyId, (MapiPropertyType)value.WireType);
        property = null;

        object? decoded;
        if (tag.IsMultiValued)
        {
            if (!TryDecodeMultiValue(tag, value.RawData.Span, encoding, strict, out decoded))
                return false;
        }
        else if (tag.Type == MapiPropertyType.Object)
        {
            // A PT_OBJECT value references a subnode (an embedded message or OLE payload); the property surfaces as
            // a marker and the payload is reached through the owning node's subnode tree, mirroring the .msg reader.
            decoded = null;
        }
        else if (MapiValueDecoder.IsVariableLength(tag.Type))
        {
            if (!MapiValueDecoder.TryDecodeVariableValue(tag.Type, value.RawData.ToArray(), encoding, strict, out decoded))
                return SkipOrThrowValue(tag, strict);
        }
        else
        {
            if (!MapiValueDecoder.TryDecodeFixedValue(tag.Type, WidenInline(value.RawData.Span), out decoded))
                return SkipOrThrowValue(tag, strict);
        }

        property = new MapiProperty(tag, decoded);
        return true;
    }

    /// <summary>
    /// Resolves the string encoding for an object from its code-page properties, the inherited encoding, or the
    /// fallback.
    /// </summary>
    /// <param name="context">The container's property context.</param>
    /// <param name="inheritedEncoding">The parent's encoding, or <see langword="null" /> at the store root.</param>
    /// <returns>The encoding used to decode the object's code-page strings.</returns>
    private static Encoding ResolveEncoding(PstPropertyContext context, Encoding? inheritedEncoding)
    {
        int? messageCodePage = TryReadInt32(context, MapiPropertyIds.MessageCodepage);
        int? internetCodePage = TryReadInt32(context, MapiPropertyIds.InternetCodepage);

        if (messageCodePage is null && internetCodePage is null && inheritedEncoding is not null)
            return inheritedEncoding;

        return MapiEncodingResolver.GetEncoding(messageCodePage, internetCodePage);
    }

    /// <summary>
    /// Reads an <c>Int32</c>-typed value from the context, ignoring an absent or differently typed entry.
    /// </summary>
    /// <param name="context">The container's property context.</param>
    /// <param name="propertyId">The property identifier.</param>
    /// <returns>The value, or <see langword="null" />.</returns>
    private static int? TryReadInt32(PstPropertyContext context, ushort propertyId) =>
        context.TryGetValue(propertyId, out PstPropertyValue value)
            && value.WireType == (ushort)MapiPropertyType.Int32
            && value.RawData.Length >= 4
            ? BinaryPrimitives.ReadInt32LittleEndian(value.RawData.Span)
            : null;

    /// <summary>
    /// Widens an inline little-endian payload of at most eight bytes into the 8-byte slot the shared fixed-value
    /// decoder consumes.
    /// </summary>
    /// <param name="raw">The payload bytes, in their natural width.</param>
    /// <returns>The widened value, or <c>0</c> when the payload exceeds eight bytes.</returns>
    private static ulong WidenInline(ReadOnlySpan<byte> raw)
    {
        if (raw.Length > 8)
            return 0;

        ulong value = 0;
        for (int i = 0; i < raw.Length; i++)
            value |= (ulong)raw[i] << (8 * i);

        return value;
    }

    /// <summary>
    /// Decodes a multi-valued payload: packed elements for the fixed-width types, and the MS-PST §2.3.3.4.2
    /// count-plus-offset-table layout for the variable-size string and binary types.
    /// </summary>
    /// <param name="tag">The multi-valued property tag.</param>
    /// <param name="payload">The complete multi-value payload.</param>
    /// <param name="encoding">The owning object's code-page string encoding.</param>
    /// <param name="strict">Whether malformed content throws instead of being skipped.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded array value.</param>
    /// <returns><see langword="true" /> when the payload decodes; <see langword="false" /> when it is skipped.</returns>
    /// <exception cref="OutlookPstFormatException">The payload is malformed and <paramref name="strict" /> is set.</exception>
    private static bool TryDecodeMultiValue(
        MapiPropertyTag tag,
        ReadOnlySpan<byte> payload,
        Encoding encoding,
        bool strict,
        out object? value)
    {
        value = null;
        switch (tag.Type)
        {
            case MapiPropertyType.Unicode:
            case MapiPropertyType.String8:
            case MapiPropertyType.Binary:
                if (!TryDecodeVariableMultiValue(tag.Type, payload, encoding, strict, out value))
                    return SkipOrThrow(tag, strict);

                return true;
            default:
                if (!MapiValueDecoder.TryDecodePackedMultiValue(tag.Type, payload, out value))
                    return SkipOrThrow(tag, strict);

                return true;
        }
    }

    /// <summary>
    /// Decodes the variable-size multi-value layout: <c>ulCount</c>, then <c>ulCount</c> offsets from the payload
    /// start, then the element data — element <c>i</c> spans its offset to the next offset (the last to the payload
    /// end).
    /// </summary>
    /// <param name="type">The base element type.</param>
    /// <param name="payload">The complete multi-value payload.</param>
    /// <param name="encoding">The owning object's code-page string encoding.</param>
    /// <param name="strict">Whether a malformed element rejects the payload.</param>
    /// <param name="value">When this method returns <see langword="true" />, the decoded array value.</param>
    /// <returns><see langword="true" /> when the payload decodes.</returns>
    private static bool TryDecodeVariableMultiValue(
        MapiPropertyType type,
        ReadOnlySpan<byte> payload,
        Encoding encoding,
        bool strict,
        out object? value)
    {
        value = null;
        if (payload.Length < 4)
            return false;

        uint count = BinaryPrimitives.ReadUInt32LittleEndian(payload);
        long headerLength = 4 + ((long)count * 4);
        if (headerLength > payload.Length)
            return false;

        var elements = new object[count];
        int previousOffset = (int)headerLength;
        for (int i = 0; i < count; i++)
        {
            int start = BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4 + (i * 4)));
            int end = i + 1 < count
                ? BinaryPrimitives.ReadInt32LittleEndian(payload.Slice(4 + ((i + 1) * 4)))
                : payload.Length;

            // Offsets must stay within the payload and never run backwards.
            if (start < previousOffset || end < start || end > payload.Length)
                return false;

            previousOffset = start;
            if (!MapiValueDecoder.TryDecodeVariableValue(type, payload[start..end].ToArray(), encoding, strict, out object? element)
                || element is null)
            {
                return false;
            }

            elements[i] = element;
        }

        value = type == MapiPropertyType.Binary
            ? elements.Cast<byte[]>().ToArray()
            : elements.Cast<string>().ToArray();
        return true;
    }

    /// <summary>
    /// Skips an undecodable multi-valued property, or throws under strict validation.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <param name="strict">Whether to throw.</param>
    /// <returns>Always <see langword="false" /> when not throwing.</returns>
    /// <exception cref="OutlookPstFormatException">Thrown under strict validation.</exception>
    private static bool SkipOrThrow(MapiPropertyTag tag, bool strict) =>
        !strict
            ? false
            : throw new OutlookPstFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookPstResourceStrings.Format_Invalid_PstMultiValue, tag.Id));

    /// <summary>
    /// Skips an undecodable scalar property, or throws under strict validation.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <param name="strict">Whether to throw.</param>
    /// <returns>Always <see langword="false" /> when not throwing.</returns>
    /// <exception cref="OutlookPstFormatException">Thrown under strict validation.</exception>
    private static bool SkipOrThrowValue(MapiPropertyTag tag, bool strict) =>
        !strict
            ? false
            : throw new OutlookPstFormatException(string.Format(
                CultureInfo.CurrentCulture, OutlookPstResourceStrings.Format_Invalid_PstPropertyValue, tag.Id));
}
