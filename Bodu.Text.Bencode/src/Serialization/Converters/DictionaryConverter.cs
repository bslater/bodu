// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts a string-keyed dictionary of <typeparamref name="TValue" /> to and from a Bencode dictionary, where each
/// entry becomes a key/value pair whose key is the dictionary key.
/// </summary>
/// <typeparam name="TDictionary">
/// The declared dictionary type (a concrete dictionary or a dictionary interface).
/// </typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// Bencode has no null token, so an entry whose value is <see langword="null" /> is omitted from the output rather than
/// written with a placeholder.
/// </remarks>
internal sealed class DictionaryConverter<TDictionary, TValue>
    : BencodeConverter<TDictionary>
    where TDictionary : class
{
    /// <summary>
    /// The converter for the value type.
    /// </summary>
    private readonly BencodeConverter _valueConverter;

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
    public DictionaryConverter(BencodeConverter valueConverter, bool concrete)
    {
        ThrowHelper.ThrowIfNull(valueConverter);
        _valueConverter = valueConverter;
        _concrete = concrete;
    }

    /// <inheritdoc />
    public override TDictionary Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        if (reader.TokenType != BencodeTokenType.StartDictionary)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedDictionary, reader.TokenType),
                reader.BytesConsumed);
        }

        Dictionary<string, TValue> entries = new(StringComparer.Ordinal);
        while (reader.Read() && reader.TokenType != BencodeTokenType.EndDictionary)
        {
            var key = reader.GetString();
            reader.Read();
            entries[key] = (TValue)_valueConverter.ReadAsObject(ref reader, typeof(TValue), options) !;
        }

        return Materialize(entries);
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, TDictionary value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        writer.WriteStartDictionary();
        foreach (KeyValuePair<string, TValue> entry in (IEnumerable<KeyValuePair<string, TValue>>)value)
        {
            if (entry.Value is null)
                continue;

            writer.WritePropertyName(entry.Key);
            _valueConverter.WriteAsObject(writer, entry.Value, options);
        }

        writer.WriteEndDictionary();
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
