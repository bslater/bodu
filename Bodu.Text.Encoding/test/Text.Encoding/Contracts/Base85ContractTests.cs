// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base85" /> in the
/// canonical Adobe Ascii85 variant. Covers the empty input, the all-zero <c>z</c> shortcut, the
/// Wikipedia-documented "Man " sample, and a 0xFF boundary case; Z85 variant tests stay covered by
/// <c>Base85Tests.*</c> partials.
/// </summary>
[TestClass]
public sealed class Base85ContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base85.Encode(input, Base85Variant.Ascii85);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base85.Decode(text, Base85Variant.Ascii85);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base85.TryEncode(input.AsSpan(), destination, out charsWritten, Base85Variant.Ascii85);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base85.TryDecode(text.AsSpan(), destination, out bytesWritten, Base85Variant.Ascii85);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("empty", [], ""),
        new("four zero bytes (z shortcut)", [0x00, 0x00, 0x00, 0x00], "z"),
        new("Wikipedia 'Man '", [0x4D, 0x61, 0x6E, 0x20], "9jqo^"),
        new("four 0xFF bytes (boundary)", [0xFF, 0xFF, 0xFF, 0xFF], "s8W-!"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("character above alphabet upper bound", "9jqov"),
        new("misplaced 'z' shortcut", "9jz"),
        new("single trailing character", "9"),
    ];
}
