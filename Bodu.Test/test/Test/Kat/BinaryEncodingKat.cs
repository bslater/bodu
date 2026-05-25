// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodingKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a binary encoding (Base16, Base32, Base58, Base64, Base85, hex,
/// etc.) that pairs a raw byte payload with the canonical encoded string form, optionally tagged with a
/// variant selector and an opaque options bag.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Bytes">The raw binary payload.</param>
/// <param name="Encoded">The expected encoded string form.</param>
/// <param name="Variant">An optional variant tag (for example a <c>Base32Variant</c> or <c>Base64Variant</c> enum value) consumed by encoders that expose multiple alphabets or padding rules.</param>
/// <param name="Options">An optional options object consumed by encoders that accept formatting flags or settings beyond the variant.</param>
/// <remarks>
/// <para>
/// Lives in <c>Bodu.Test</c> so that any test project that consumes byte-string encoding KATs can adopt the
/// shared shape. Projects that already ship their own domain-specific record (for example
/// <c>EncodingKnownAnswerVector</c> in <c>Bodu.Text.Encoding</c>) may continue to use those — both shapes
/// implement <see cref="IKat" /> so the choice is one of convenience rather than capability.
/// </para>
/// </remarks>
public sealed record BinaryEncodingKat(
    string Name,
    byte[] Bytes,
    string Encoded,
    object? Variant = null,
    object? Options = null) : IKat;
