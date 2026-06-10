// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxTriviaList.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Represents an immutable, ordered sequence of <see cref="SyntaxTrivia" /> attached to one side of a
/// <see cref="SyntaxToken" />.
/// </summary>
/// <remarks>
/// The list is a lightweight value type over an optional backing array; the default value and <see cref="Empty" /> both
/// represent an empty list, which is the form every token in a binary format carries.
/// </remarks>
public readonly struct SyntaxTriviaList
    : IReadOnlyList<SyntaxTrivia>
{
    /// <summary>
    /// The backing trivia array, or <see langword="null" /> when the list is empty.
    /// </summary>
    private readonly SyntaxTrivia[]? _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxTriviaList" /> struct.
    /// </summary>
    /// <param name="items">The trivia, in order.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    public SyntaxTriviaList(IEnumerable<SyntaxTrivia> items)
    {
        ThrowHelper.ThrowIfNull(items);

        SyntaxTrivia[] array = [.. items];
        _items = array.Length == 0 ? null : array;
    }

    /// <summary>
    /// Gets an empty trivia list.
    /// </summary>
    /// <returns>A list with no trivia.</returns>
    public static SyntaxTriviaList Empty => default;

    /// <inheritdoc />
    public int Count => _items?.Length ?? 0;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public SyntaxTrivia this[int index]
    {
        get
        {
            if (_items is null || (uint)index >= (uint)_items.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }
    }

    /// <summary>
    /// Appends the concatenated text of every trivia in the list to the supplied writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public void WriteTo(TextWriter writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        if (_items is null)
            return;

        foreach (SyntaxTrivia trivia in _items)
            writer.Write(trivia.Text);
    }

    /// <inheritdoc />
    public IEnumerator<SyntaxTrivia> GetEnumerator()
    {
        if (_items is null)
            yield break;

        foreach (SyntaxTrivia trivia in _items)
            yield return trivia;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}
