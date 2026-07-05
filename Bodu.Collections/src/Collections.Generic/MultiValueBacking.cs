// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueBacking.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Specifies how a <see cref="MultiValueDictionary{TKey, TValue}" /> stores the values associated with each key.
/// </summary>
/// <remarks>
/// <para>
/// The backing is selected at construction time and is immutable for the lifetime of the dictionary. It is reported by
/// <see cref="MultiValueDictionary{TKey, TValue}.Backing" />.
/// </para>
/// <para>
/// <see cref="List" /> corresponds to a list multimap: every added value is retained, including per-key duplicates.
/// <see cref="Set" /> corresponds to an order-preserving set multimap: values are deduplicated per key using the
/// dictionary's <see cref="MultiValueDictionary{TKey, TValue}.ValueComparer" />, and the insertion order of each
/// value's first occurrence is preserved.
/// </para>
/// <para>
/// Under <see cref="Set" />, each add performs a linear scan of the key's existing values to detect duplicates. This
/// trade-off keeps the per-key values in insertion order and preserves the <see cref="IReadOnlyList{T}" /> view
/// contract exposed by the dictionary.
/// </para>
/// </remarks>
public enum MultiValueBacking
{
    /// <summary>
    /// Values for a key are kept in insertion order and per-key duplicates are allowed. This is the default and
    /// preserves the historical behavior of <see cref="MultiValueDictionary{TKey, TValue}" />.
    /// </summary>
    List = 0,

    /// <summary>
    /// Values for a key are deduplicated using the dictionary's
    /// <see cref="MultiValueDictionary{TKey, TValue}.ValueComparer" />; adding a value already associated with the key
    /// leaves the dictionary unchanged. The insertion order of each value's first occurrence is preserved.
    /// </summary>
    Set = 1,
}
