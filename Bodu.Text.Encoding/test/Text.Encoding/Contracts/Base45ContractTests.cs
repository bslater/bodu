// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base45" /> with the RFC 9285
/// worked-example vectors and the specification's mandatory rejection cases. Validates encode/decode parity and
/// round-trip on the shared contract surface; bespoke Base45 tests (span overloads, whitespace, registry lookup) live
/// in the <c>Base45Tests.*.cs</c> partials.
/// </summary>
/// <remarks>
/// The <c>TEncoding</c> type parameter is a documentation marker — the contract is exercised through the protected
/// adapter methods. Because <see cref="Base45" /> is a <c>static class</c> it cannot be passed as a type argument; this
/// subclass uses <see cref="object" /> as the placeholder.
/// </remarks>
[TestClass]
public sealed class Base45ContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base45.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base45.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base45.TryEncode(input.AsSpan(), destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base45.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("RFC 9285 example 1 'AB'", "AB"u8.ToArray(), "BB8"),
        new("RFC 9285 example 2 'Hello!!'", "Hello!!"u8.ToArray(), "%69 VD92EX0"),
        new("RFC 9285 example 3 'base-45'", "base-45"u8.ToArray(), "UJCLQE7W581"),
        new("RFC 9285 decode example 'ietf!'", "ietf!"u8.ToArray(), "QED8WEX0"),
        new("single zero byte", [0x00], "00"),
        new("two zero bytes", [0x00, 0x00], "000"),
        new("max byte pair 0xFFFF", [0xFF, 0xFF], "FGW"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("triplet overflow 'GGW' (65536)", "GGW"),
        new("length modulo 3 equals one", "A"),
        new("lowercase letter not in alphabet", "ab"),
        new("two-character group overflow '::' (2024)", "::"),
    ];
}
