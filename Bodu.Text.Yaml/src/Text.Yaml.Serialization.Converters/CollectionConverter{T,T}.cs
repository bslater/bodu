// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionConverter{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a collection of <typeparamref name="TElement" /> to and from a YAML sequence, materializing the declared
/// collection type <typeparamref name="TCollection" /> according to a <see cref="CollectionStrategy" />.
/// </summary>
/// <typeparam name="TCollection">
/// The declared collection type (an array, a list, a list-compatible interface, a concrete collection, or a queue-,
/// stack-, or bag-shaped collection).
/// </typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
/// <remarks>
/// <para>
/// Writing always enumerates the collection in its natural enumeration order; a null element writes the YAML null
/// scalar rather than being rejected, because YAML — unlike its sibling formats — has a native null form. For
/// <see cref="Queue{T}" /> and <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" /> enumeration is dequeue
/// (first-in) order, so a queue round-trips unchanged. For <see cref="Stack{T}" /> and
/// <see cref="System.Collections.Concurrent.ConcurrentStack{T}" /> enumeration yields pop order (most recently pushed
/// first), while reading pushes the document's elements in document order — so a serialize/deserialize round-trip
/// reverses a stack. <see cref="System.Collections.Concurrent.ConcurrentBag{T}" /> makes no enumeration-order guarantee
/// in either direction.
/// </para>
/// </remarks>
internal sealed class CollectionConverter<TCollection, TElement>
    : YamlConverter<TCollection>
    where TCollection : class
{
    /// <summary>The converter for the element type.</summary>
    private readonly YamlConverter _elementConverter;

    /// <summary>The strategy used to materialize the collection when reading.</summary>
    private readonly CollectionStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionConverter{TCollection, TElement}" /> class.
    /// </summary>
    /// <param name="elementConverter">The converter for the element type.</param>
    /// <param name="strategy">The strategy used to materialize the collection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="elementConverter" /> is <see langword="null" />.
    /// </exception>
    public CollectionConverter(YamlConverter elementConverter, CollectionStrategy strategy)
    {
        ThrowHelper.ThrowIfNull(elementConverter);
        _elementConverter = elementConverter;
        _strategy = strategy;
    }

    /// <inheritdoc />
    public override TCollection Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return null!;

        if (reader.TokenType != YamlTokenType.StartSequence)
            throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedSequence);

        List<TElement> items = [];
        int index = 0;
        while (reader.Read() && reader.TokenType != YamlTokenType.EndSequence)
        {
            items.Add((TElement)ChildBinder.Read(ref reader, _elementConverter, typeof(TElement), options, index)!);
            index++;
        }

        return Materialize(items);
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, TCollection value, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        YamlWriteStack? state = writer.WriteStack;
        if (state is not null && !state.TryEnterReference(value))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCycleDetected, value.GetType()));
            return;
        }

        try
        {
            writer.WriteStartSequence();
            int index = 0;
            foreach (TElement element in (IEnumerable<TElement>)value)
            {
                if (state is { HasFailure: true })
                    break;

                state?.PushPath(index);
                _elementConverter.WriteAsObject(writer, element, options);
                state?.PopPath();

                index++;
            }

            if (state is { HasFailure: true })
                return;

            writer.WriteEndSequence();
        }
        finally
        {
            if (state is not null)
                state.ExitReference(value);
        }
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
        TCollection instance = Activator.CreateInstance<TCollection>()!;
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
        TCollection instance = Activator.CreateInstance<TCollection>()!;
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
        TCollection instance = Activator.CreateInstance<TCollection>()!;
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
        TCollection instance = Activator.CreateInstance<TCollection>()!;
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
        TCollection instance = Activator.CreateInstance<TCollection>()!;
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
        TCollection instance = Activator.CreateInstance<TCollection>()!;
        var bag = (System.Collections.Concurrent.ConcurrentBag<TElement>)(object)instance;
        foreach (TElement element in items)
            bag.Add(element);

        return instance;
    }
}
