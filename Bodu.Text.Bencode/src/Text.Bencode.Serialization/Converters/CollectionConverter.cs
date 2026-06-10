// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a collection of <typeparamref name="TElement" /> to and from a Bencode list, materializing the declared
/// collection type <typeparamref name="TCollection" /> according to a <see cref="CollectionStrategy" />.
/// </summary>
/// <typeparam name="TCollection">
/// The declared collection type (an array, a list, a list-compatible interface, or a concrete collection).
/// </typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
internal sealed class CollectionConverter<TCollection, TElement>
    : BencodeConverter<TCollection>
    where TCollection : class
{
    /// <summary>
    /// The converter for the element type.
    /// </summary>
    private readonly BencodeConverter _elementConverter;

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
    public CollectionConverter(BencodeConverter elementConverter, CollectionStrategy strategy)
    {
        ThrowHelper.ThrowIfNull(elementConverter);
        _elementConverter = elementConverter;
        _strategy = strategy;
    }

    /// <inheritdoc />
    public override TCollection Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.StartList)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedList, reader.TokenType),
                reader.BytesConsumed);
        }

        List<TElement> items = [];
        while (reader.Read() && reader.TokenType != BencodeTokenType.EndList)
            items.Add((TElement)_elementConverter.ReadAsObject(ref reader, typeof(TElement), options) !);

        return Materialize(items);
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, TCollection value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        writer.WriteStartList();
        foreach (TElement element in (IEnumerable<TElement>)value)
        {
            if (element is null)
                throw new BencodeSerializationException(BencodeResourceStrings.Op_NotSupported_NullCollectionElement);

            _elementConverter.WriteAsObject(writer, element, options);
        }

        writer.WriteEndList();
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
}
