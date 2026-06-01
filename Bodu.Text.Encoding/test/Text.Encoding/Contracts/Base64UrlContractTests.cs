// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64UrlContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base64Url" /> with
/// RFC 4648 §5 URL-safe vectors. Verifies that the URL-safe alphabet swaps (<c>+</c>→<c>-</c>,
/// <c>/</c>→<c>_</c>) and unpadded output round-trip end-to-end through the shared contract surface.
/// Bespoke Base64Url tests (padded/unpadded inputs, JWT-shape acceptance, UTF-8 surface) live in the
/// existing <c>Base64UrlTests.*</c> partials.
/// </summary>
[TestClass]
public sealed class Base64UrlContractTests : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base64Url.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base64Url.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base64Url.TryEncode(input.AsSpan(), destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base64Url.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("empty", [], ""),
        new("0xFB 0xFF byte produces hyphen", [0xFB, 0xFF], "-_8"),
        new("0xFF 0xFF 0xFF byte produces underscore", [0xFF, 0xFF, 0xFF], "____"),
        new("ASCII 'foo'", "foo"u8.ToArray(), "Zm9v"),
        new("ASCII 'foobar'", "foobar"u8.ToArray(), "Zm9vYmFy"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("plus is not in URL-safe alphabet", "Zm9+"),
        new("forward-slash is not in URL-safe alphabet", "Zm9/"),
        new("invalid character", "Zm9v!"),
    ];
}
