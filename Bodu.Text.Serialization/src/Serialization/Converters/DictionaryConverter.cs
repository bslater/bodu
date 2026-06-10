// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts a string-keyed dictionary of <typeparamref name="TValue" /> to and from an object token, where each entry
/// becomes a property whose name is the key.
/// </summary>
/// <typeparam name="TDictionary">
/// The declared dictionary type (a concrete dictionary or a dictionary interface).
/// </typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
internal sealed class DictionaryConverter<TDictionary, TValue>
    : FormatConverter<TDictionary>
    where TDictionary : class
{
    /// <summary>
    /// The converter for the value type.
    /// </summary>
    private readonly FormatConverter _valueConverter;

    /// <summary>
    /// Indicates whether the declared type is a concrete dictionary that must be instantiated and populated.
    /// </summary>
    private readonly bool _concrete;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryConverter{TDictionary, TValue}" /> class.
    /// </summary>
    /// <param name="valueConverter">The converter for the value type.</param>
    /// <param name="concrete">Whether the declared type is a concrete dictionary.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="valueConverter" /> is <see langword="null" />.
    /// </exception>
    public DictionaryConverter(FormatConverter valueConverter, bool concrete)
    {
        ThrowHelper.ThrowIfNull(valueConverter);
        _valueConverter = valueConverter;
        _concrete = concrete;
    }

    /// <inheritdoc />
    public override TDictionary Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        if (reader.TokenKind != SerializationValueKind.StartObject)
        {
            throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_ExpectedObject, reader.TokenKind),
                reader.Location);
        }

        Dictionary<string, TValue> entries = new(StringComparer.Ordinal);
        while (reader.Read() && reader.TokenKind != SerializationValueKind.EndObject)
        {
            var key = reader.GetPropertyName();
            reader.Read();
            entries[key] = (TValue)_valueConverter.ReadAsObject(reader, typeof(TValue), options) !;
        }

        return Materialize(entries);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, TDictionary value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(value);

        writer.WriteStartObject();
        foreach (KeyValuePair<string, TValue> entry in (IEnumerable<KeyValuePair<string, TValue>>)value)
        {
            writer.WritePropertyName(entry.Key);
            if (entry.Value is null)
                writer.WriteNull();
            else
                _valueConverter.WriteAsObject(writer, entry.Value, options);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Materializes the declared dictionary type from the read entries.
    /// </summary>
    /// <param name="entries">The read entries.</param>
    /// <returns>The materialized dictionary.</returns>
    private TDictionary Materialize(Dictionary<string, TValue> entries)
    {
        if (!_concrete)
            return (TDictionary)(object)entries;

        var instance = (TDictionary)Activator.CreateInstance(typeof(TDictionary)) !;
        var dictionary = (IDictionary<string, TValue>)instance;
        foreach (KeyValuePair<string, TValue> entry in entries)
            dictionary[entry.Key] = entry.Value;

        return instance;
    }
}
