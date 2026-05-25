// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base32" /> with the
/// canonical RFC 4648 §10 vectors. Validates that the contract base wires up to <see cref="Base32" />'s
/// static surface and round-trips bytes through every code path. Bespoke Base32 tests (variants —
/// Crockford, ExtendedHex, ZBase32 — formatting options, span overloads) live in the existing
/// <c>Base32Tests.*.cs</c> partials.
/// </summary>
[TestClass]
public sealed class Base32ContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base32.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base32.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base32.TryEncode(input.AsSpan(), destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base32.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } = new BinaryEncodingKat[]
    {
        // RFC 4648 §10 - Base32 reference vectors.
        new("empty",   Array.Empty<byte>(),    ""),
        new("f",       "f"u8.ToArray(),        "MY======"),
        new("fo",      "fo"u8.ToArray(),       "MZXQ===="),
        new("foo",     "foo"u8.ToArray(),      "MZXW6==="),
        new("foob",    "foob"u8.ToArray(),     "MZXW6YQ="),
        new("fooba",   "fooba"u8.ToArray(),    "MZXW6YTB"),
        new("foobar",  "foobar"u8.ToArray(),   "MZXW6YTBOI======"),
    };

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } = new InvalidEncodedTextKat[]
    {
        // Characters '8' and '9' fall outside the RFC 4648 Base32 alphabet (which uses A-Z and 2-7).
        new("contains '8'", "M8YYY==="),
        new("contains '9'", "MZ9Q===="),
        new("contains '$'", "MZX$===="),
    };
}
