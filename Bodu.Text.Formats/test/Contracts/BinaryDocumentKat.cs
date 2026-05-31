// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryDocumentKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Formats.Contracts;

/// <summary>
/// Represents a known-answer test row for a decode/encode binary-document format such as Bencode. A row pairs the raw
/// byte payload with the expected decoded document and (optionally) the parser/formatter options that produced it.
/// </summary>
/// <typeparam name="TDocument">The decoded document type.</typeparam>
/// <typeparam name="TOptions">
/// The parser/formatter options type, or a placeholder when the format has no options.
/// </typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Payload">The raw byte payload supplied to the decoder (or produced by the encoder).</param>
/// <param name="ExpectedDocument">The expected decoded document.</param>
/// <param name="Options">
/// The options to apply, or <see langword="default" /> to select the format's default options.
/// </param>
public sealed record BinaryDocumentKat<TDocument, TOptions>(
    string Name,
    byte[] Payload,
    TDocument ExpectedDocument,
    TOptions? Options = default) : IKat;
