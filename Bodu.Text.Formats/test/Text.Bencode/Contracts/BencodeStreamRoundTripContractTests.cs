// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeStreamRoundTripContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Formats.Contracts;

namespace Bodu.Text.Bencode.Contracts;

/// <summary>
/// Drives <see cref="StreamRoundTripContractTests{T}" /> against <see cref="Bencode" />'s synchronous
/// and asynchronous <see cref="Stream" /> surface. Verifies that integer and byte-string values
/// round-trip losslessly through both <c>Encode</c>/<c>Decode</c> and <c>EncodeAsync</c>/<c>DecodeAsync</c>.
/// Bespoke Bencode coverage (lists, dictionaries, malformed-input rejection, depth limits) lives in
/// <c>BencodeTests.*</c>.
/// </summary>
[TestClass]
public sealed class BencodeStreamRoundTripContractTests
    : StreamRoundTripContractTests<BencodedValue>
{
    /// <inheritdoc />
    protected override void Write(Stream destination, BencodedValue value) =>
        Bencode.Encode(value, destination);

    /// <inheritdoc />
    protected override BencodedValue Read(Stream source) =>
        Bencode.Decode(source);

    /// <inheritdoc />
    protected override async Task WriteAsync(Stream destination, BencodedValue value, CancellationToken cancellationToken) =>
        await Bencode.EncodeAsync(value, destination, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    protected override async Task<BencodedValue> ReadAsync(Stream source, CancellationToken cancellationToken) =>
        await Bencode.DecodeAsync(source, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    protected override IReadOnlyList<BinaryKat<BencodedValue, BencodedValue>> ValidCases { get; } =
        [
        new(
            Name: "integer 0",
            Input: new BencodedInteger(0),
            Expected: new BencodedInteger(0)),

        new(
            Name: "integer -1",
            Input: new BencodedInteger(-1),
            Expected: new BencodedInteger(-1)),

        new(
            Name: "integer 42",
            Input: new BencodedInteger(42),
            Expected: new BencodedInteger(42)),

        new(
            Name: "byte string 'hello'",
            Input: new BencodedString("hello"u8.ToArray()),
            Expected: new BencodedString("hello"u8.ToArray())),

        new(
            Name: "empty byte string",
            Input: new BencodedString(Array.Empty<byte>()),
            Expected: new BencodedString(Array.Empty<byte>())),
    ];
}
