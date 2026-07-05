// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionary{T,T}.IDictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Globalization;

namespace Bodu.Collections.Generic;

public sealed partial class BiDictionary<TKey, TValue> :
    System.Collections.Generic.IDictionary<TKey, TValue>,
    System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>,
    System.Collections.IDictionary
{
    /// <inheritdoc />
    public int Count => _forward.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    /// <remarks>
    /// Returns the forward index's live key collection. The collection is read-only through this surface and its
    /// enumeration order matches the dictionary's (unspecified) enumeration order.
    /// </remarks>
    public ICollection<TKey> Keys => _forward.Keys;

    /// <inheritdoc />
    /// <remarks>
    /// Returns the forward index's live value collection. Because the mapping is one-to-one, the values are distinct
    /// under <see cref="ValueComparer" />. The collection is read-only through this surface and its enumeration order
    /// matches the dictionary's (unspecified) enumeration order.
    /// </remarks>
    public ICollection<TValue> Values => _forward.Values;

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <inheritdoc />
    bool IDictionary.IsFixedSize => false;

    /// <inheritdoc />
    bool IDictionary.IsReadOnly => false;

    /// <inheritdoc />
    ICollection IDictionary.Keys => _forward.Keys;

    /// <inheritdoc />
    ICollection IDictionary.Values => _forward.Values;

    /// <inheritdoc cref="System.Collections.Generic.IDictionary{TKey, TValue}.this" />
    /// <remarks>
    /// <para>
    /// Assigning a new key adds the pair; assigning an existing key re-binds it — the key's previous value is unbound
    /// from the inverse index so it no longer resolves through <see cref="TryGetKey(TValue, out TKey)" /> or
    /// <see cref="ContainsValue(TValue)" />.
    /// </para>
    /// <para>
    /// When the assigned value is already bound to a <i>different</i> key, the conflict is resolved by
    /// <see cref="DuplicateValuePolicy" />: <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> rejects the
    /// assignment with <see cref="ArgumentException" />, while <see cref="BiDictionaryDuplicateValuePolicy.Replace" />
    /// evicts the previous binding. Re-assigning a key its own current value is never a conflict.
    /// </para>
    /// </remarks>
    public TValue this[TKey key]
    {
        get => _forward.TryGetValue(key, out TValue? value)
            ? value
            : throw new KeyNotFoundException(string.Format(CultureInfo.CurrentCulture, CollectionsResourceStrings.KeyNotFound_Dictionary, key));

        set
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(value);

            if (_backward.TryGetValue(value, out TKey? boundKey) && !_forward.Comparer.Equals(boundKey, key))
            {
                if (_duplicateValuePolicy == BiDictionaryDuplicateValuePolicy.Throw)
                    throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateBiDictionaryValue, nameof(value));

                _forward.Remove(boundKey);
            }

            // Re-binding an existing key must unbind its previous value from the inverse index first.
            if (_forward.TryGetValue(key, out TValue? oldValue))
                _backward.Remove(oldValue);

            _forward[key] = value;
            _backward[value] = key;
        }
    }

    /// <inheritdoc />
    object? IDictionary.this[object key]
    {
        get => key is TKey typedKey && _forward.TryGetValue(typedKey, out TValue? value) ? value : null;

        set
        {
            ThrowHelper.ThrowIfNotOfType<TKey>(key);
            ThrowHelper.ThrowIfNotOfType<TValue>(value);

            this[(TKey)key] = (TValue)value!;
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add. Must not be <see langword="null" />.</param>
    /// <param name="value">The value to bind to <paramref name="key" />. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary, or <paramref name="value" /> is already bound to
    /// a different key and <see cref="DuplicateValuePolicy" /> is <see cref="BiDictionaryDuplicateValuePolicy.Throw" />.
    /// </exception>
    /// <remarks>
    /// Duplicate keys follow the strict <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Add(TKey,
    /// TValue)" /> contract and always throw. A duplicate value is resolved by <see cref="DuplicateValuePolicy" />:
    /// under <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> the previous binding holding the value is evicted
    /// — its key is removed — before the new pair is added.
    /// </remarks>
    public void Add(TKey key, TValue value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);
        if (_forward.ContainsKey(key)) throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateDictionaryKey, nameof(key));
        if (_duplicateValuePolicy == BiDictionaryDuplicateValuePolicy.Throw && _backward.ContainsKey(value))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_DuplicateBiDictionaryValue, nameof(value));

        if (_backward.TryGetValue(value, out TKey? displacedKey))
            _forward.Remove(displacedKey);

        _forward.Add(key, value);
        _backward[value] = key;
    }

    /// <summary>
    /// Adds the specified key/value pair to the dictionary.
    /// </summary>
    /// <param name="item">The key/value pair to add to the dictionary.</param>
    /// <exception cref="ArgumentNullException">
    /// <c>item.Key</c> or <c>item.Value</c> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// An element with the same key already exists in the dictionary, or the pair's value is already bound to a
    /// different key and <see cref="DuplicateValuePolicy" /> is <see cref="BiDictionaryDuplicateValuePolicy.Throw" />.
    /// </exception>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Removes all entries from the dictionary.
    /// </summary>
    /// <remarks>
    /// Clears both the forward and inverse indexes, so the <see cref="Inverse" /> view is emptied as well.
    /// </remarks>
    public void Clear()
    {
        _forward.Clear();
        _backward.Clear();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Value equality is determined by <see cref="ValueComparer" />, matching the comparer used by the inverse index.
    /// </remarks>
    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        _forward.TryGetValue(item.Key, out TValue? value) && _backward.Comparer.Equals(value, item.Value);

    /// <inheritdoc />
    public bool ContainsKey(TKey key) => _forward.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(arrayIndex, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, arrayIndex + Count);

        foreach (KeyValuePair<TKey, TValue> kvp in _forward)
            array[arrayIndex++] = kvp;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Enumeration follows the forward index's unspecified, insertion-biased order. As with
    /// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" />, adding an entry — through either view —
    /// invalidates the enumerator, which then throws <see cref="InvalidOperationException" />; do not mutate the
    /// dictionary while enumerating it.
    /// </remarks>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _forward.GetEnumerator();

    /// <inheritdoc />
    /// <remarks>
    /// Removing a key also unbinds its value from the inverse index, so the value no longer resolves through
    /// <see cref="TryGetKey(TValue, out TKey)" /> or <see cref="ContainsValue(TValue)" />.
    /// </remarks>
    public bool Remove(TKey key)
    {
        if (_forward.Remove(key, out TValue? value))
        {
            _backward.Remove(value);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Remove(KeyValuePair<TKey, TValue> item) => Contains(item) && Remove(item.Key);

    /// <summary>
    /// Attempts to retrieve the value bound to the specified key.
    /// </summary>
    /// <param name="key">The key of the value to retrieve. Must not be <see langword="null" />.</param>
    /// <param name="value">
    /// When this method returns, contains the value bound to the specified key, if the key is found; otherwise, the
    /// default value for the type of the value parameter.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the dictionary contains an element with the specified key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGetValue(TKey key, out TValue value) => _forward.TryGetValue(key, out value!);

    /// <inheritdoc />
    void IDictionary.Add(object key, object? value)
    {
        ThrowHelper.ThrowIfNotOfType<TKey>(key);
        ThrowHelper.ThrowIfNotOfType<TValue>(value);

        Add((TKey)key, (TValue)value!);
    }

    /// <inheritdoc />
    bool IDictionary.Contains(object key) => key is TKey typedKey && ContainsKey(typedKey);

    /// <inheritdoc />
    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)_forward).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    void IDictionary.Remove(object key)
    {
        ThrowHelper.ThrowIfNotOfType<TKey>(key);

        Remove((TKey)key);
    }
}
