// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedDictionary.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a bencoded dictionary with raw byte-string keys.
/// </summary>
public sealed class BencodedDictionary
    : BencodedValue
{
    private readonly SortedDictionary<BencodedString, BencodedValue> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedDictionary" /> class.
    /// </summary>
    /// <param name="items">The dictionary items.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the dictionary contains duplicate keys or null keys or values.
    /// </exception>
    public BencodedDictionary(IEnumerable<KeyValuePair<BencodedString, BencodedValue>> items)
    {
        ThrowHelper.ThrowIfNull(items);

        this._items = new SortedDictionary<BencodedString, BencodedValue>(BencodedStringComparer.Ordinal);

        foreach (KeyValuePair<BencodedString, BencodedValue> pair in items)
        {
            if (pair.Key is null)
                throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_NullDictionaryKey, nameof(items));

            if (pair.Value is null)
                throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_NullDictionaryValue, nameof(items));

            if (!this._items.TryAdd(pair.Key, pair.Value))
                throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(items));
        }
    }

    /// <summary>
    /// Gets the number of key-value pairs in the dictionary.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets the dictionary items sorted by raw byte-string key order.
    /// </summary>
    public IReadOnlyDictionary<BencodedString, BencodedValue> Items => _items;

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.Dictionary;

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The raw byte-string key.</param>
    /// <returns>The value associated with <paramref name="key" />.</returns>
    public BencodedValue this[BencodedString key] => _items[key];

    /// <summary>
    /// Gets the key-value pairs in encoded dictionary order.
    /// </summary>
    /// <returns>The ordered key-value pairs.</returns>
    public IEnumerable<KeyValuePair<BencodedString, BencodedValue>> GetOrderedItems() =>
        _items;

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The raw byte-string key.</param>
    /// <param name="value">When this method returns, contains the matching value, when found.</param>
    /// <returns><see langword="true" /> when the key exists; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(BencodedString key, out BencodedValue value) =>
        _items.TryGetValue(key, out value!);

    /// <summary>
    /// Attempts to get the value associated with a UTF-8 key.
    /// </summary>
    /// <param name="key">The UTF-8 key text.</param>
    /// <param name="value">When this method returns, contains the matching value, when found.</param>
    /// <returns><see langword="true" /> when the key exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />.
    /// </exception>
    public bool TryGetValue(string key, out BencodedValue value)
    {
        ThrowHelper.ThrowIfNull(key);

        return _items.TryGetValue(BencodedString.FromUtf8(key), out value!);
    }
}
