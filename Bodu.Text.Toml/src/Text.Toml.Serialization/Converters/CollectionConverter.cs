// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a collection of <typeparamref name="TElement" /> to and from a TOML array, materializing the declared
/// collection type <typeparamref name="TCollection" /> according to a <see cref="CollectionStrategy" />.
/// </summary>
/// <typeparam name="TCollection">
/// The declared collection type (an array, a list, a list-compatible interface, a concrete collection, or a queue-,
/// stack-, or bag-shaped collection).
/// </typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <remarks>
/// <para>
/// Writing always enumerates the collection in its natural enumeration order. For <see cref="Queue{T}" /> and
/// <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> that is dequeue (first-in) order, so a queue
/// round-trips unchanged. For <see cref="Stack{T}" /> and
/// <see cref="System.Collections.Concurrent.ConcurrentStack{T}" /> enumeration yields pop order (most recently pushed
/// first), while reading pushes the document's elements in document order — so a serialize/deserialize round-trip
/// reverses a stack. <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> makes no enumeration-order guarantee
/// in either direction.
/// </para>
/// </remarks>
internal sealed class CollectionConverter<TCollection, TElement>
    : TomlConverter<TCollection>
    where TCollection : class
{
    /// <summary>
    /// The converter for the element type.
    /// </summary>
    private readonly TomlConverter _elementConverter;

    /// <summary>
    /// The strategy used to materialize the collection when reading.
    /// </summary>
    private readonly CollectionStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionConverter{TCollection, TElement}" /> class.
    /// </summary>
    /// <param name="elementConverter">The converter for the element type.</param>
    /// <param name="strategy">The strategy used to materialize the collection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="elementConverter" /> is <see langword="null" />.
    /// </exception>
    public CollectionConverter(TomlConverter elementConverter, CollectionStrategy strategy)
    {
        ThrowHelper.ThrowIfNull(elementConverter);
        _elementConverter = elementConverter;
        _strategy = strategy;
    }

    /// <inheritdoc />
    public override TCollection Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.StartArray)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedArray, reader.TokenType));
        }

        List<TElement> items = [];
        while (reader.Read() && reader.TokenType != TomlTokenType.EndArray)
            items.Add((TElement)_elementConverter.ReadAsObject(ref reader, typeof(TElement), options) !);

        return Materialize(items);
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TCollection value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        writer.WriteStartArray();
        foreach (TElement element in (IEnumerable<TElement>)value)
        {
            if (element is null)
                throw new TomlSerializationException(TomlResourceStrings.Op_NotSupported_NullCollectionElement);

            _elementConverter.WriteAsObject(writer, element, options);
        }

        writer.WriteEndArray();
    }

    /// <summary>
    /// Materializes the declared collection type from the read elements.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized collection.</returns>
    private TCollection Materialize(List<TElement> items) =>
        _strategy switch
        {
            CollectionStrategy.Array => (TCollection)(object)items.ToArray(),
            CollectionStrategy.ListAssignable => (TCollection)(object)items,
            CollectionStrategy.Queue => MaterializeQueue(items),
            CollectionStrategy.Stack => MaterializeStack(items),
            CollectionStrategy.ConcurrentQueue => MaterializeConcurrentQueue(items),
            CollectionStrategy.ConcurrentStack => MaterializeConcurrentStack(items),
            CollectionStrategy.ConcurrentBag => MaterializeConcurrentBag(items),
            _ => MaterializeConcrete(items),
        };

    /// <summary>
    /// Materializes a concrete collection by creating an instance and adding each element.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized collection.</returns>
    private static TCollection MaterializeConcrete(List<TElement> items)
    {
        var instance = (TCollection)Activator.CreateInstance(typeof(TCollection)) !;
        var collection = (ICollection<TElement>)instance;
        foreach (TElement element in items)
            collection.Add(element);

        return instance;
    }

    /// <summary>
    /// Materializes a <see cref="Queue{T}" /> (or subclass) by enqueuing each element in document order.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized queue.</returns>
    private static TCollection MaterializeQueue(List<TElement> items)
    {
        var instance = (TCollection)Activator.CreateInstance(typeof(TCollection)) !;
        var queue = (Queue<TElement>)(object)instance;
        foreach (TElement element in items)
            queue.Enqueue(element);

        return instance;
    }

    /// <summary>
    /// Materializes a <see cref="Stack{T}" /> (or subclass) by pushing each element in document order, so enumeration
    /// yields the elements in reverse document order.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized stack.</returns>
    private static TCollection MaterializeStack(List<TElement> items)
    {
        var instance = (TCollection)Activator.CreateInstance(typeof(TCollection)) !;
        var stack = (Stack<TElement>)(object)instance;
        foreach (TElement element in items)
            stack.Push(element);

        return instance;
    }

    /// <summary>
    /// Materializes a <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> (or subclass) by enqueuing each
    /// element in document order.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized queue.</returns>
    private static TCollection MaterializeConcurrentQueue(List<TElement> items)
    {
        var instance = (TCollection)Activator.CreateInstance(typeof(TCollection)) !;
        var queue = (System.Collections.Concurrent.ConcurrentQueue<TElement>)(object)instance;
        foreach (TElement element in items)
            queue.Enqueue(element);

        return instance;
    }

    /// <summary>
    /// Materializes a <see cref="System.Collections.Concurrent.ConcurrentStack{T}" /> (or subclass) by pushing each
    /// element in document order, so enumeration yields the elements in reverse document order.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized stack.</returns>
    private static TCollection MaterializeConcurrentStack(List<TElement> items)
    {
        var instance = (TCollection)Activator.CreateInstance(typeof(TCollection)) !;
        var stack = (System.Collections.Concurrent.ConcurrentStack<TElement>)(object)instance;
        foreach (TElement element in items)
            stack.Push(element);

        return instance;
    }

    /// <summary>
    /// Materializes a <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> (or subclass) by adding each
    /// element.
    /// </summary>
    /// <param name="items">The read elements.</param>
    /// <returns>The materialized bag.</returns>
    private static TCollection MaterializeConcurrentBag(List<TElement> items)
    {
        var instance = (TCollection)Activator.CreateInstance(typeof(TCollection)) !;
        var bag = (System.Collections.Concurrent.ConcurrentBag<TElement>)(object)instance;
        foreach (TElement element in items)
            bag.Add(element);

        return instance;
    }
}
