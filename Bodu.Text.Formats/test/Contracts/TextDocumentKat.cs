// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextDocumentKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Formats.Contracts;

/// <summary>
/// Represents a known-answer test row for a parse/format text-document format such as Bencode, CSV, DotEnv, or INI. A
/// row pairs the raw source text with the expected parsed document and (optionally) the parser/formatter options that
/// produced it.
/// </summary>
/// <typeparam name="TDocument">The parsed document type.</typeparam>
/// <typeparam name="TOptions">
/// The parser/formatter options type, or a placeholder when the format has no options.
/// </typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Text">The raw source text supplied to the parser (or produced by the formatter).</param>
/// <param name="ExpectedDocument">The expected parsed document.</param>
/// <param name="Options">
/// The options to apply, or <see langword="default" /> to select the format's default options.
/// </param>
public sealed record TextDocumentKat<TDocument, TOptions>(
    string Name,
    string Text,
    TDocument ExpectedDocument,
    TOptions? Options = default) : IKat;
