// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedList.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a bencoded list.
/// </summary>
public sealed class BencodedList
    : BencodedValue
{

    private readonly BencodedValue[] items;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedList" /> class.
    /// </summary>
    /// <param name="items">The list items.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="items" /> contains a <see langword="null" /> value.
    /// </exception>
    public BencodedList(IEnumerable<BencodedValue> items)
    {
        ThrowHelper.ThrowIfNull(items);

        this.items = items.ToArray();

        if (this.items.Any(item => item is null))
            ThrowHelper.ThrowArgumentException_NullListElement(nameof(items));
    }

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    public int Count => items.Length;

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<BencodedValue> Items => items;

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.List;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The item at the specified index.</returns>
    public BencodedValue this[int index] => items[index];

}
