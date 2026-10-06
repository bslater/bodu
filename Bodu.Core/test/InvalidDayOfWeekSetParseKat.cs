// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidDayOfWeekSetParseKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Represents a known-answer test row for a <see cref="DayOfWeekSet" /> parse that must fail, carrying the input text,
/// optional format specifier, and the exception type that the throwing parse overload must raise.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The text supplied to the parser.</param>
/// <param name="Format">The optional format specifier; <see langword="null" /> selects auto-detect.</param>
/// <param name="ExceptionType">
/// The exact exception type that <c>Parse</c> / <c>ParseExact</c> must throw; ignored by <c>TryParse</c> /
/// <c>TryParseExact</c> tests because they return <see langword="false" /> instead of throwing.
/// </param>
public sealed record InvalidDayOfWeekSetParseKat(
    string Name,
    string Input,
    string? Format,
    Type ExceptionType) : IKat;
