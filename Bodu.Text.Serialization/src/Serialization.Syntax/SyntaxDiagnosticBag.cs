// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxDiagnosticBag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Collects the <see cref="SyntaxDiagnostic" /> instances produced while lexing or parsing a source document.
/// </summary>
/// <remarks>
/// The bag is an internal accumulation point. Current parse surfaces throw on the first error, so a populated bag is
/// equivalent to a single reported failure; the type exists to keep the tree model independent of error-reporting
/// policy and to support a future best-effort parse that returns a partial tree alongside the full diagnostic set.
/// </remarks>
public sealed class SyntaxDiagnosticBag
    : IReadOnlyList<SyntaxDiagnostic>
{
    /// <summary>
    /// The accumulated diagnostics, in the order they were reported.
    /// </summary>
    private readonly List<SyntaxDiagnostic> _items = [];

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the bag contains at least one diagnostic.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when one or more diagnostics have been reported; otherwise <see langword="false" />.
    /// </returns>
    public bool HasErrors => _items.Count > 0;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public SyntaxDiagnostic this[int index] => _items[index];

    /// <summary>
    /// Adds a diagnostic to the bag.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="diagnostic" /> is <see langword="null" />.
    /// </exception>
    public void Add(SyntaxDiagnostic diagnostic)
    {
        ThrowHelper.ThrowIfNull(diagnostic);

        _items.Add(diagnostic);
    }

    /// <summary>
    /// Reports a diagnostic with the supplied location and message.
    /// </summary>
    /// <param name="location">The location at which the problem was detected.</param>
    /// <param name="message">The human-readable description of the problem.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message" /> is <see langword="null" />.
    /// </exception>
    public void Report(TextLocation location, string message) =>
        Add(new SyntaxDiagnostic(location, message));

    /// <inheritdoc />
    public IEnumerator<SyntaxDiagnostic> GetEnumerator() =>
        _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _items.GetEnumerator();
}
