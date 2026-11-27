// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PositionedDiagnosticCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Collects the diagnostics of one document load and gives each the position, in the loaded document, of the node or
/// declaration under examination when it is added.
/// </summary>
/// <remarks>
/// The loader wraps its diagnostic list in an instance when it loads a document from XML text. The reader, the import
/// resolution, the override applier and the validator open a <see cref="Scope" /> for each node or declaration they
/// examine, and a diagnostic added inside the scope takes its position, unless it carries one of its own. A node or
/// declaration with no known position leaves the enclosing scope in force, which is how a diagnostic about an imported
/// document takes the position of the <c>Import</c> element that brought it in. Code that receives any other collection
/// opens scopes that do nothing, so a JSON load and a binary pack validation add their diagnostics unchanged.
/// </remarks>
internal sealed partial class PositionedDiagnosticCollection
    : ICollection<NotableDateValidationDiagnostic>
{
    /// <summary>The collection that receives the diagnostics.</summary>
    private readonly ICollection<NotableDateValidationDiagnostic> _inner;

    /// <summary>The position of each declaration of the loaded document, keyed by the declaration's identity.</summary>
    private readonly Dictionary<object, NotableDateSourcePosition> _declarations = new(ReferenceEqualityComparer.Instance);

    /// <summary>The position of the innermost open scope, or <see langword="null" /> outside every scope.</summary>
    private NotableDateSourcePosition? _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionedDiagnosticCollection" /> class over a diagnostic list.
    /// </summary>
    /// <param name="inner">The collection that receives the diagnostics.</param>
    public PositionedDiagnosticCollection(ICollection<NotableDateValidationDiagnostic> inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public int Count => _inner.Count;

    /// <inheritdoc />
    public bool IsReadOnly => _inner.IsReadOnly;

    /// <summary>
    /// Opens a scope at the position of <paramref name="node" />, when <paramref name="diagnostics" /> records
    /// positions and the node has one.
    /// </summary>
    /// <param name="diagnostics">The collection the diagnostics are added to.</param>
    /// <param name="node">The node under examination, or <see langword="null" /> when it is absent.</param>
    /// <returns>A scope that restores the enclosing position when it is disposed.</returns>
    public static Scope Enter(ICollection<NotableDateValidationDiagnostic> diagnostics, IDocumentNode? node) =>
        diagnostics is PositionedDiagnosticCollection positioned ? positioned.Enter(node?.Position) : default;

    /// <summary>
    /// Opens a scope at the position recorded for <paramref name="declaration" />, when <paramref name="diagnostics" />
    /// records positions and has one for it.
    /// </summary>
    /// <param name="diagnostics">The collection the diagnostics are added to.</param>
    /// <param name="declaration">The policy, notable date, rule, override, import or use under examination.</param>
    /// <returns>A scope that restores the enclosing position when it is disposed.</returns>
    public static Scope EnterDeclaration(ICollection<NotableDateValidationDiagnostic> diagnostics, object declaration) =>
        diagnostics is PositionedDiagnosticCollection positioned
            ? positioned.Enter(positioned._declarations.TryGetValue(declaration, out NotableDateSourcePosition position) ? position : null)
            : default;

    /// <summary>
    /// Records <paramref name="declaration" /> as declared at the position of <paramref name="node" />, so that a later
    /// scope opened for it takes that position.
    /// </summary>
    /// <param name="diagnostics">The collection the diagnostics are added to.</param>
    /// <param name="declaration">The declaration read from <paramref name="node" />.</param>
    /// <param name="node">The node the declaration was read from.</param>
    public static void Record(ICollection<NotableDateValidationDiagnostic> diagnostics, object declaration, IDocumentNode node)
    {
        if (diagnostics is PositionedDiagnosticCollection positioned && node.Position is NotableDateSourcePosition position)
            positioned._declarations[declaration] = position;
    }

    /// <summary>
    /// Records <paramref name="declaration" /> at the position recorded for <paramref name="source" />, or else at the
    /// position of the innermost open scope, unless it has a position already.
    /// </summary>
    /// <param name="diagnostics">The collection the diagnostics are added to.</param>
    /// <param name="declaration">The declaration to record, such as one copied or rewritten from another.</param>
    /// <param name="source">
    /// The declaration whose position it takes, or <see langword="null" /> to take the innermost scope's.
    /// </param>
    public static void RecordFrom(ICollection<NotableDateValidationDiagnostic> diagnostics, object declaration, object? source)
    {
        if (diagnostics is not PositionedDiagnosticCollection positioned)
            return;

        if (source is not null && positioned._declarations.TryGetValue(source, out NotableDateSourcePosition position))
            positioned._declarations.TryAdd(declaration, position);
        else if (positioned._current is NotableDateSourcePosition current)
            positioned._declarations.TryAdd(declaration, current);
    }

    /// <summary>
    /// Adds a diagnostic, giving it the position of the innermost open scope when it carries none of its own.
    /// </summary>
    /// <param name="item">The diagnostic to add.</param>
    public void Add(NotableDateValidationDiagnostic item) =>
        _inner.Add(item.LineNumber is null && _current is NotableDateSourcePosition position
            ? item with { LineNumber = position.LineNumber, LinePosition = position.LinePosition }
            : item);

    /// <inheritdoc />
    public void Clear() => _inner.Clear();

    /// <inheritdoc />
    public bool Contains(NotableDateValidationDiagnostic item) => _inner.Contains(item);

    /// <inheritdoc />
    public void CopyTo(NotableDateValidationDiagnostic[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(NotableDateValidationDiagnostic item) => _inner.Remove(item);

    /// <inheritdoc />
    public IEnumerator<NotableDateValidationDiagnostic> GetEnumerator() => _inner.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Opens a scope at <paramref name="position" />, or one that keeps the enclosing position in force when it is
    /// <see langword="null" />.
    /// </summary>
    /// <param name="position">The position of the node or declaration under examination.</param>
    /// <returns>A scope that restores the enclosing position when it is disposed.</returns>
    private Scope Enter(NotableDateSourcePosition? position)
    {
        var scope = new Scope(this, _current);
        if (position is not null)
            _current = position;

        return scope;
    }
}
