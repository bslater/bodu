// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationDiagnostic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a single diagnostic produced while loading and validating a notable-date document resource.
/// </summary>
/// <param name="Severity">The severity of the diagnostic.</param>
/// <param name="Code">A short, stable code identifying the kind of diagnostic.</param>
/// <param name="Message">A human-readable description of the diagnostic.</param>
/// <remarks>
/// <para>
/// A diagnostic about a document loaded from XML text carries the place in that text that it concerns, through
/// <see cref="LineNumber" /> and <see cref="LinePosition" />, both counted from one. A schema or well-formedness
/// diagnostic points at the character the XML reader or the schema validation reports; a diagnostic about a value
/// points at the element that holds it; a validation diagnostic about an adjustment policy, a notable date or a rule
/// points at that declaration's element; and an import or override diagnostic points at the <c>Import</c>, <c>Use</c>
/// or override element that it concerns. A diagnostic that an imported document produces, or that concerns a
/// declaration an import brought in, points at the <c>Import</c> or <c>Use</c> element of the loaded document that
/// brought it in, never at a line of the imported document.
/// </para>
/// <para>
/// A diagnostic carries no position when the loader read no XML text of its own for it: a JSON document gives positions
/// only to its syntax errors, a compiled <c>.bcal</c> pack gives none, and nor does a document that
/// <c>NotableDateDocumentBuilder</c> validates, whose XML is the builder's own serialization rather than text a reader
/// could look up.
/// </para>
/// <para>
/// Two diagnostics are equal when their severity, code, message, line number and line position are all equal, so a
/// diagnostic that carries a position is not equal to one that carries the same text without it.
/// </para>
/// </remarks>
public sealed record NotableDateValidationDiagnostic(
    NotableDateValidationSeverity Severity,
    string Code,
    string Message)
{
    /// <summary>The value of <see cref="LineNumber" />.</summary>
    private readonly int? _lineNumber;

    /// <summary>The value of <see cref="LinePosition" />.</summary>
    private readonly int? _linePosition;

    /// <summary>
    /// Gets the line of the document text that the diagnostic concerns, counted from one.
    /// </summary>
    /// <value>The line number, or <see langword="null" /> when the diagnostic carries no position.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int? LineNumber
    {
        get => _lineNumber;
        init
        {
            ThrowHelper.ThrowIfLessThan(value, 1);
            _lineNumber = value;
        }
    }

    /// <summary>
    /// Gets the position on <see cref="LineNumber" /> of the character that the diagnostic concerns, counted from one.
    /// </summary>
    /// <value>The character position, or <see langword="null" /> when the diagnostic carries no position.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than one.</exception>
    public int? LinePosition
    {
        get => _linePosition;
        init
        {
            ThrowHelper.ThrowIfLessThan(value, 1);
            _linePosition = value;
        }
    }

    /// <summary>
    /// Returns a readable rendering of the diagnostic, combining its severity, code, and message, and its position when
    /// it carries one.
    /// </summary>
    /// <returns>
    /// The diagnostic formatted as <c>[Severity] Code: Message</c>, followed in parentheses by its line and its
    /// position on the line when it carries them.
    /// </returns>
    public override string ToString()
    {
        string text = $"[{Severity}] {Code}: {Message}";

        if (LineNumber is not int line)
            return text;

        return LinePosition is int position
            ? string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DiagnosticAtLineAndPosition, text, line, position)
            : string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Validation_DiagnosticAtLine, text, line);
    }
}
