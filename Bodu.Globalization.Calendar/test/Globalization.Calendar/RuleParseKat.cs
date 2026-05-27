// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleParseKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a known-answer test row for a calendar rule-document parser (XML, JSON, or other). The
/// <typeparamref name="TDocument" /> parameter lets the same record carry domain-specific parsed document shapes
/// without forcing the test infrastructure to know the concrete type.
/// </summary>
/// <typeparam name="TDocument">The parsed document type — for example <c>ParsedNotableDateDocument</c>.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The raw source text supplied to the parser.</param>
/// <param name="Format">The format identifier, for example <c>"xml"</c> or <c>"json"</c>.</param>
/// <param name="Expected">The expected parsed document.</param>
public sealed record RuleParseKat<TDocument>(
    string Name,
    string Input,
    string Format,
    TDocument Expected) : IKat;
