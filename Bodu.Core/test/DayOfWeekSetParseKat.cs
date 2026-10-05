// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetParseKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Represents a known-answer test row for a successful <see cref="DayOfWeekSet" /> parse, carrying the input text,
/// optional format specifier, and the expected set.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The text supplied to the parser.</param>
/// <param name="Format">The optional format specifier; <see langword="null" /> selects auto-detect.</param>
/// <param name="Expected">
/// The expected set, as a seven-bit mask written Sunday first, most significant bit first, the way the binary text
/// form reads.
/// </param>
public sealed record DayOfWeekSetParseKat(
    string Name,
    string Input,
    string? Format,
    byte Expected) : IKat;
