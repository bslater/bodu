// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base58" /> with a
/// small set of Bitcoin/Flickr-variant vectors. Validates encode/decode parity and round-trip on the
/// shared contract surface; bespoke Base58 tests (variants, formatting options, span overloads) live in
/// the existing <c>Base58Tests.*.cs</c> partials.
/// </summary>
/// <remarks>
/// The <c>TEncoding</c> type parameter on <see cref="BinaryEncodingContractTests{TEncoding}" /> is a
/// documentation marker — the contract is exercised through the protected adapter methods. Because
/// <see cref="Base58" /> is a <c>static class</c> it cannot be passed as a type argument; this subclass
/// uses <see cref="object" /> as the placeholder.
/// </remarks>
[TestClass]
public sealed class Base58ContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base58.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base58.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base58.TryEncode(input.AsSpan(), destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base58.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } = new BinaryEncodingKat[]
    {
        new("single zero byte", [0x00], "1"),
        new("two zero bytes", [0x00, 0x00], "11"),
        new("zero-prefixed payload", [0x00, 0x01], "12"),
        new("single 0x39 byte", [0x39], "z"),
        new("ASCII 'Hello'", "Hello"u8.ToArray(), "9Ajdvzr"),
    };

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } = new InvalidEncodedTextKat[]
    {
        new("zero digit not in alphabet", "10"),
        new("capital O not in alphabet", "9OAjdvzr"),
        new("capital I not in alphabet", "9Ajdvzr0I"),
        new("lowercase l not in alphabet", "9Ajldvzr"),
    };
}
