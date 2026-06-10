// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Identifies how a <see cref="CollectionConverter{TCollection, TElement}" /> materializes the collection it produces
/// when reading.
/// </summary>
internal enum CollectionStrategy
{
    /// <summary>
    /// The target is a single-dimensional array; the read elements are copied into a new array.
    /// </summary>
    Array,

    /// <summary>
    /// The target is <see cref="System.Collections.Generic.List{T}" /> or an interface it satisfies; the read list is
    /// returned directly.
    /// </summary>
    ListAssignable,

    /// <summary>
    /// The target is a concrete collection type with a public parameterless constructor; an instance is created and the
    /// read elements are added through <see cref="System.Collections.Generic.ICollection{T}" />.
    /// </summary>
    ConcreteCollection,
}
