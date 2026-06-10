// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedList.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents a bencoded list — an ordered, heterogeneous sequence of <see cref="BencodedValue" /> items.
/// </summary>
/// <remarks>
/// <para>
/// Bencoded lists are encoded as <c>l</c>&lt;values…&gt;<c>e</c>, with each contained value encoded recursively. The
/// items collection is immutable once constructed; the constructor rejects <see langword="null" /> entries to keep the
/// list safely usable in pattern-matched switches over <see cref="BencodedValue" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var list = new BencodedList(new BencodedValue[]
/// {
///     BencodedString.FromUtf8("foo"),
///     new BencodedInteger(42),
/// });
///
/// Console.WriteLine(list.Count);          // 2
/// Console.WriteLine(list[0].Kind);        // String
/// foreach (BencodedValue v in list.Items)
///     Console.WriteLine(v);
///]]>
/// </example>
public sealed class BencodedList
    : BencodedValue
{
    private readonly BencodedValue[] _items;

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

        this._items = [.. items];

        if (this._items.Any(item => item is null))
            throw new ArgumentException(BencodeResourceStrings.Arg_Invalid_NullListElement, nameof(items));
    }

    /// <summary>
    /// Gets the number of items in the list.
    /// </summary>
    public int Count => _items.Length;

    /// <summary>
    /// Gets the list items.
    /// </summary>
    public IReadOnlyList<BencodedValue> Items => _items;

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.List;

    /// <summary>
    /// Gets the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based item index.</param>
    /// <returns>The item at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0 or greater than or equal to <see cref="Count" />.
    /// </exception>
    public BencodedValue this[int index]
    {
        get
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, _items);

            return _items[index];
        }
    }
}
