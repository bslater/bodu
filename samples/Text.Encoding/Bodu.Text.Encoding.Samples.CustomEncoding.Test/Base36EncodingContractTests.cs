// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base36EncodingContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding.Contracts;

namespace Bodu.Samples.Text.Encoding.CustomEncoding;

/// <summary>
/// Drives the library's shared <see cref="BinaryEncodingContractTests{TEncoding}" /> against the
/// sample's <see cref="Base36Encoding" />. This is the pattern for any custom
/// <c>IBinaryEncoding</c>: implement the adapter members, supply known-answer and invalid-input
/// vectors, and inherit the encode/decode/round-trip/Try-pattern contract for free.
/// </summary>
[TestClass]
public sealed class Base36EncodingContractTests
    : BinaryEncodingContractTests<Base36Encoding>
{
    private static readonly Base36Encoding Encoding = new();

    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Encoding.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Encoding.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Encoding.TryEncode(input, destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Encoding.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("ascii 'Bodu!'", "Bodu!"u8.ToArray(), "3N2Y3NS1"),
        new("ascii 'hello'", "hello"u8.ToArray(), "5PZCSZU7"),
        new("leading zero bytes preserved", [0x00, 0x00, 0xFF], "0073"),
        new("single zero byte", [0x00], "0"),
        new("max two-byte value 0xFFFF", [0xFF, 0xFF], "1EKF"),
        new("four bytes DE AD BE EF", [0xDE, 0xAD, 0xBE, 0xEF], "1PS9WXB"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("lowercase letter outside alphabet", "abc"),
        new("punctuation outside alphabet", "AB-CD"),
        new("whitespace not tolerated", "12 34"),
        new("non-ASCII character", "12É4"),
    ];
}
