// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85GitContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base85" /> in the Git compact
/// variant. Covers empty input, an all-zero group (no shortcut), the 0xFF boundary, and partial-tail groups; the
/// padded Git binary-patch primitive stays covered by <c>Base85GitTests.Padded</c>.
/// </summary>
[TestClass]
public sealed class Base85GitContractTests
    : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base85.Encode(input, Base85Variant.Git);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base85.Decode(text, Base85Variant.Git);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base85.TryEncode(input.AsSpan(), destination, out charsWritten, Base85Variant.Git);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base85.TryDecode(text.AsSpan(), destination, out bytesWritten, Base85Variant.Git);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("empty", [], ""),
        new("four zero bytes (no shortcut)", [0x00, 0x00, 0x00, 0x00], "00000"),
        new("four 0xFF bytes (boundary)", [0xFF, 0xFF, 0xFF, 0xFF], "|NsC0"),
        new("one byte (partial tail)", [0x01], "0R"),
        new("'hello'", [0x68, 0x65, 0x6C, 0x6C, 0x6F], "Xk~0{Zv"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("single trailing character (length mod 5 == 1)", "0"),
        new("full group plus one trailing character", "0RjUA0"),
        new("character not in Git alphabet (space)", "0R j"),
    ];
}
