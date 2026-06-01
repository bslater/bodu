// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidBinaryDocumentKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Formats.Contracts;

/// <summary>
/// Represents a known-failure row for a binary-document decoder. The decoder must reject <see cref="Payload" /> by
/// throwing <see cref="ExceptionType" />; the optional <see cref="MessageContains" /> substring lets tests assert a
/// specific diagnostic phrase.
/// </summary>
/// <typeparam name="TOptions">The parser options type.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Payload">The malformed byte payload.</param>
/// <param name="Options">The options under which the payload should be rejected.</param>
/// <param name="ExceptionType">The exact exception type the decoder must throw.</param>
/// <param name="MessageContains">
/// An optional substring the exception message must contain; <see langword="null" /> skips the message check.
/// </param>
public sealed record InvalidBinaryDocumentKat<TOptions>(
    string Name,
    byte[] Payload,
    TOptions? Options,
    Type ExceptionType,
    string? MessageContains = null) : IKat;
