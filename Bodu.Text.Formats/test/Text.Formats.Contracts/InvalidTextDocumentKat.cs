// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidTextDocumentKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Formats.Contracts;

/// <summary>
/// Represents a negative-vector known-answer test row for a parse/format text-document format: a raw source string that
/// the parser must reject, paired with the exception type the throwing overload must raise.
/// </summary>
/// <typeparam name="TOptions">
/// The parser/formatter options type, or a placeholder when the format has no options.
/// </typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Text">The malformed source text supplied to the parser.</param>
/// <param name="Options">
/// The options to apply, or <see langword="default" /> to select the format's default options.
/// </param>
/// <param name="ExceptionType">The exact exception type the throwing parser overload must raise.</param>
/// <param name="MessageContains">An optional substring the exception message must contain.</param>
public sealed record InvalidTextDocumentKat<TOptions>(
    string Name,
    string Text,
    TOptions? Options,
    Type ExceptionType,
    string? MessageContains = null) : IKat;
