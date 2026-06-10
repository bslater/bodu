// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxDiagnostic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Describes a single problem detected while lexing or parsing a source document, pairing a human-readable message with
/// the <see cref="TextLocation" /> at which the problem was found.
/// </summary>
/// <remarks>
/// Diagnostics are collected in a <see cref="SyntaxDiagnosticBag" />. The public parse surfaces report the first error
/// by throwing a format-specific exception; the bag exists so a future non-throwing parse path can surface the full set
/// of problems without changing the tree model.
/// </remarks>
public sealed class SyntaxDiagnostic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxDiagnostic" /> class.
    /// </summary>
    /// <param name="location">The location at which the problem was detected.</param>
    /// <param name="message">The human-readable description of the problem.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    public SyntaxDiagnostic(TextLocation location, string message)
    {
        ThrowHelper.ThrowIfNull(message);

        Location = location;
        Message = message;
    }

    /// <summary>
    /// Gets the location at which the problem was detected.
    /// </summary>
    /// <returns>The source location.</returns>
    public TextLocation Location { get; }

    /// <summary>
    /// Gets the human-readable description of the problem.
    /// </summary>
    /// <returns>The diagnostic message.</returns>
    public string Message { get; }

    /// <summary>
    /// Returns a string combining the location and message.
    /// </summary>
    /// <returns>A diagnostic string of the form <c>location: message</c>.</returns>
    public override string ToString() =>
        $"{Location}: {Message}";
}
