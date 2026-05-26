// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Text.Bencode.Contracts;

/// <summary>
/// Drives <see cref="BinaryDocumentFormatContractTests{TDocument, TOptions}" /> against
/// <see cref="Bencode" />, the byte-oriented document format used by BitTorrent. Bespoke Bencode
/// coverage (lists, dictionaries, depth limits, malformed-string-length rejection) remains in
/// <c>BencodeTests.*</c>.
/// </summary>
[TestClass]
public sealed class BencodeContractTests
    : BinaryDocumentFormatContractTests<BencodedValue, BencodeParseOptions>
{
    /// <inheritdoc />
    protected override BencodedValue Decode(byte[] payload, BencodeParseOptions options = default!) =>
        Bencode.Decode(payload.AsSpan(), options);

    /// <inheritdoc />
    protected override byte[] Encode(BencodedValue document, BencodeParseOptions options = default!) =>
        Bencode.Encode(document);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryDocumentKat<BencodedValue, BencodeParseOptions>> ValidCases { get; } =
        [
        new(
            Name: "integer 42",
            Payload: "i42e"u8.ToArray(),
            ExpectedDocument: new BencodedInteger(42),
            Options: BencodeParseOptions.Default),

        new(
            Name: "integer -1",
            Payload: "i-1e"u8.ToArray(),
            ExpectedDocument: new BencodedInteger(-1),
            Options: BencodeParseOptions.Default),

        new(
            Name: "integer 0",
            Payload: "i0e"u8.ToArray(),
            ExpectedDocument: new BencodedInteger(0),
            Options: BencodeParseOptions.Default),

        new(
            Name: "byte string 'hello'",
            Payload: "5:hello"u8.ToArray(),
            ExpectedDocument: new BencodedString("hello"u8.ToArray()),
            Options: BencodeParseOptions.Default),

        new(
            Name: "empty byte string",
            Payload: "0:"u8.ToArray(),
            ExpectedDocument: new BencodedString(Array.Empty<byte>()),
            Options: BencodeParseOptions.Default),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidBinaryDocumentKat<BencodeParseOptions>> InvalidCases { get; } =
        [
        new(
            Name: "integer missing terminator",
            Payload: "i42"u8.ToArray(),
            Options: BencodeParseOptions.Default,
            ExceptionType: typeof(BencodeFormatException)),

        new(
            Name: "integer with leading zero",
            Payload: "i01e"u8.ToArray(),
            Options: BencodeParseOptions.Default,
            ExceptionType: typeof(BencodeFormatException)),

        new(
            Name: "string declared length exceeds payload",
            Payload: "10:abc"u8.ToArray(),
            Options: BencodeParseOptions.Default,
            ExceptionType: typeof(BencodeFormatException)),
    ];
}
