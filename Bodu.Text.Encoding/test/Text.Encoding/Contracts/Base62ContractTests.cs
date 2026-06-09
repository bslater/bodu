// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62ContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding.Contracts;

/// <summary>
/// Drives <see cref="BinaryEncodingContractTests{TEncoding}" /> against <see cref="Base62" /> with hand-verified
/// GMP-alphabet vectors. Validates encode/decode parity and round-trip on the shared contract surface; bespoke Base62
/// tests live in the <c>Base62Tests.*.cs</c> partials.
/// </summary>
/// <remarks>
/// The <c>TEncoding</c> type parameter is a documentation marker — the contract is exercised through the protected
/// adapter methods. Because <see cref="Base62" /> is a <c>static class</c> it cannot be passed as a type argument; this
/// subclass uses <see cref="object" /> as the placeholder.
/// </remarks>
[TestClass]
public sealed class Base62ContractTests
    : BinaryEncodingContractTests<object>
{
    /// <inheritdoc />
    protected override string Encode(byte[] input) =>
        Base62.Encode(input);

    /// <inheritdoc />
    protected override byte[] Decode(string text) =>
        Base62.Decode(text);

    /// <inheritdoc />
    protected override bool TryEncode(byte[] input, Span<char> destination, out int charsWritten) =>
        Base62.TryEncode(input.AsSpan(), destination, out charsWritten);

    /// <inheritdoc />
    protected override bool TryDecode(string text, Span<byte> destination, out int bytesWritten) =>
        Base62.TryDecode(text.AsSpan(), destination, out bytesWritten);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryEncodingKat> KnownAnswers { get; } =
    [
        new("single zero byte", [0x00], "0"),
        new("two zero bytes", [0x00, 0x00], "00"),
        new("zero-prefixed payload", [0x00, 0x01], "01"),
        new("value 61 -> last single digit", [0x3D], "z"),
        new("value 62 -> two digits", [0x3E], "10"),
        new("value 255", [0xFF], "47"),
        new("value 256", [0x01, 0x00], "48"),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidEncodedTextKat> InvalidInputs { get; } =
    [
        new("hyphen not in alphabet", "ab-cd"),
        new("plus not in alphabet", "ab+cd"),
        new("space not in alphabet", "ab cd"),
    ];
}
