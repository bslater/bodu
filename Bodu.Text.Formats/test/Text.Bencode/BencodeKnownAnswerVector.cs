// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeKnownAnswerVector.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Encoding;

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents a positive Known Answer Test (KAT) vector sourced from a published specification or reference
/// implementation. A vector pairs an encoded bencode byte sequence with the canonical <see cref="BencodedValue" /> it
/// represents so that data-driven tests can verify <see cref="Bencode" /> against authoritative references.
/// </summary>
/// <param name="Description">A short human-readable label identifying the vector.</param>
/// <param name="EncodedBytes">The canonical encoded form.</param>
/// <param name="DecodedValue">The decoded value that round-trips with <paramref name="EncodedBytes" />.</param>
/// <param name="Source">
/// The citation for the vector's origin (specification clause, reference implementation, etc.).
/// </param>
public sealed record BencodeKnownAnswerVector(
    string Description,
    byte[] EncodedBytes,
    BencodedValue DecodedValue,
    string? Source = null) : IKat
{

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see cref="Description" /> so the row participates in <see cref="KatDisplayName" />-driven display
    /// formatting.
    /// </remarks>
    string IKat.Name => Description;

    /// <summary>
    /// Builds a vector for a dictionary value.
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="encoded">The canonical encoded ASCII form.</param>
    /// <param name="value">The decoded dictionary.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="encoded" /> resolved to its ASCII byte representation.</returns>
    public static BencodeKnownAnswerVector Dictionary(string description, string encoded, BencodedValue value, string source) =>
        new(description, System.Text.Encoding.ASCII.GetBytes(encoded), value, source);

    /// <summary>
    /// Builds a vector from hex-encoded bytes, useful when the encoded form contains non-printable bytes that are
    /// awkward to author as ASCII string literals. Reuses <see cref="Base16.Decode(string)" /> from
    /// <c>Bodu.Text.Encoding</c>.
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="encodedHex">
    /// The encoded form expressed as a hex string (e.g. <c>"323aff00"</c> for <c>2:\xFF\x00</c>).
    /// </param>
    /// <param name="value">The decoded value that round-trips with the bytes.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="encodedHex" /> resolved to its byte representation.</returns>
    public static BencodeKnownAnswerVector FromHex(string description, string encodedHex, BencodedValue value, string source) =>
        new(description, Base16.Decode(encodedHex), value, source);
    /// <summary>
    /// Builds a vector for an integer value, e.g. <c>i42e</c>.
    /// </summary>
    /// <param name="value">The decoded integer.</param>
    /// <param name="encoded">The canonical encoded ASCII form, e.g. <c>"i42e"</c>.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="encoded" /> resolved to its ASCII byte representation.</returns>
    public static BencodeKnownAnswerVector Integer(long value, string encoded, string source) =>
        new($"Integer {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}", System.Text.Encoding.ASCII.GetBytes(encoded), new BencodedInteger(value), source);

    /// <summary>
    /// Builds a vector for a list value.
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="encoded">The canonical encoded ASCII form.</param>
    /// <param name="value">The decoded list.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="encoded" /> resolved to its ASCII byte representation.</returns>
    public static BencodeKnownAnswerVector List(string description, string encoded, BencodedValue value, string source) =>
        new(description, System.Text.Encoding.ASCII.GetBytes(encoded), value, source);

    /// <summary>
    /// Builds a vector for a UTF-8 string value, e.g. <c>4:spam</c>.
    /// </summary>
    /// <param name="text">The decoded text.</param>
    /// <param name="encoded">The canonical encoded ASCII form, e.g. <c>"4:spam"</c>.</param>
    /// <param name="source">The citation.</param>
    /// <returns>A KAT vector with <paramref name="text" /> resolved to its UTF-8 byte content.</returns>
    public static BencodeKnownAnswerVector Utf8String(string text, string encoded, string source) =>
        new($"String \"{text}\"", System.Text.Encoding.ASCII.GetBytes(encoded), BencodedString.FromUtf8(text), source);

    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating test
    /// names so each row is identifiable in failure output.
    /// </summary>
    /// <returns>The description, optionally suffixed with the citation in square brackets.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";

}
