// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidRuleParseKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a negative-vector known-answer test row for a calendar rule-document parser: a malformed source string
/// the parser must reject, the format identifier, and the exception type the throwing overload must raise.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The malformed source text.</param>
/// <param name="Format">The format identifier, for example <c>"xml"</c> or <c>"json"</c>.</param>
/// <param name="ExceptionType">The exact exception type the throwing parser overload must raise.</param>
/// <param name="MessageContains">An optional substring the exception message must contain.</param>
public sealed record InvalidRuleParseKat(
    string Name,
    string Input,
    string Format,
    Type ExceptionType,
    string? MessageContains = null) : IKat;
