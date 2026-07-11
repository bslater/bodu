// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

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

    /// <summary>
    /// The target is <see cref="System.Collections.Generic.Queue{T}" /> (or a subclass); an instance is created and the
    /// read elements are enqueued in document order.
    /// </summary>
    Queue,

    /// <summary>
    /// The target is <see cref="System.Collections.Generic.Stack{T}" /> (or a subclass); an instance is created and the
    /// read elements are pushed in document order, so enumeration yields them in reverse document order.
    /// </summary>
    Stack,

    /// <summary>
    /// The target is <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> (or a subclass); an instance is
    /// created and the read elements are enqueued in document order.
    /// </summary>
    ConcurrentQueue,

    /// <summary>
    /// The target is <see cref="System.Collections.Concurrent.ConcurrentStack{T}" /> (or a subclass); an instance is
    /// created and the read elements are pushed in document order, so enumeration yields them in reverse document
    /// order.
    /// </summary>
    ConcurrentStack,

    /// <summary>
    /// The target is <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> (or a subclass); an instance is
    /// created and the read elements are added, with no enumeration-order guarantee.
    /// </summary>
    ConcurrentBag,
}
