// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base16" /> with a
/// minimal set of canonical RFC 4648 vectors. Validates that the contract base — which expects
/// <c>Encode</c>, <c>Decode</c>, <c>TryEncode</c>, and <c>TryDecode</c> adapter methods — wires up to
/// the static <see cref="Base16" /> surface and round-trips bytes through every code path. Bespoke
/// Base16 tests (variants, formatting options, span overloads) live in the existing
/// <c>Base16Tests.*.cs</c> partials.
/// </summary>
/// <remarks>
/// The <c>TEncoding</c> type parameter on <see cref="BinaryEncodingContractTests{TEncoding}" /> is a
/// documentation marker — the contract is exercised through the protected adapter methods. Because
/// <see cref="Base16" /> is a <c>static class</c> it cannot be passed as a type argument; this subclass
/// uses <see cref="object" /> as the placeholder.
/// </remarks>
[TestClass]
public sealed class Base16ContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base16.Encode(input, BaseFormattingOptions.UpperCase);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base16.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base16.TryEncode(input.AsSpan(), destination, out charsWritten, BaseFormattingOptions.UpperCase);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base16.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("empty", [], ""),
        new("single zero", [0x00], "00"),
        new("0xDE 0xAD", [0xDE, 0xAD], "DEAD"),
        new("0xDE 0xAD 0xBE 0xEF", [0xDE, 0xAD, 0xBE, 0xEF], "DEADBEEF"),
        new("ASCII 'foo'", "foo"u8.ToArray(), "666F6F"),
        new("ASCII 'foobar'", "foobar"u8.ToArray(), "666F6F626172"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("odd-length input", "DEA"),
        new("non-hex character", "DEAG"),
        new("space inside", "DE AD"),
    ];
}
