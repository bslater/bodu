// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base64" /> with the
/// canonical RFC 4648 §10 vectors. Bespoke Base64 tests (variants — Standard, UrlSafe, MIME — span
/// overloads, formatting options) live in the existing <c>Base64Tests.*.cs</c> partials.
/// </summary>
[TestClass]
public sealed class Base64ContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base64.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base64.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base64.TryEncode(input.AsSpan(), destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base64.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        // RFC 4648 §10 - Base64 reference vectors.
        new("empty",   Array.Empty<byte>(),    ""),
        new("f",       "f"u8.ToArray(),        "Zg=="),
        new("fo",      "fo"u8.ToArray(),       "Zm8="),
        new("foo",     "foo"u8.ToArray(),      "Zm9v"),
        new("foob",    "foob"u8.ToArray(),     "Zm9vYg=="),
        new("fooba",   "fooba"u8.ToArray(),    "Zm9vYmE="),
        new("foobar",  "foobar"u8.ToArray(),   "Zm9vYmFy"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("contains '!' (not in Standard alphabet)", "Zm9v!Yg=="),
        new("missing padding",                          "Zm9vYg"),
    ];
}
