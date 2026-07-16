// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryConverter{T,T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a dictionary of <typeparamref name="TKey" /> to <typeparamref name="TValue" /> to and from a YAML
/// mapping, where each entry becomes a key/value pair whose mapping key is the entry key's invariant text.
/// </summary>
/// <typeparam name="TDictionary">
/// The declared dictionary type (a concrete dictionary or a dictionary interface).
/// </typeparam>
/// <typeparam name="TKey">The key type, one of the kinds named by <see cref="DictionaryKeyKind" />.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// <para>
/// YAML mapping keys are strings, so a non-string key is converted to its round-trippable invariant-culture text on
/// write and parsed back on read. Distinct keys whose string forms collide are rejected with
/// <see cref="YamlSerializationException" />, so the serializer never emits a document its own parser would reject as
/// a duplicate key. An entry whose value is <see langword="null" /> writes the YAML null scalar — YAML, unlike its
/// sibling formats, has a native null form.
/// </para>
/// </remarks>
internal sealed class DictionaryConverter<TDictionary, TKey, TValue>
    : YamlConverter<TDictionary>
    where TDictionary : class
    where TKey : notnull
{
    /// <summary>The converter for the value type.</summary>
    private readonly YamlConverter _valueConverter;

    /// <summary>The kind of key conversion applied between <typeparamref name="TKey" /> and the mapping key.</summary>
    private readonly DictionaryKeyKind _keyKind;

    /// <summary>Indicates whether the declared type is a concrete dictionary that must be instantiated and populated.</summary>
    private readonly bool _concrete;

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryConverter{TDictionary, TKey, TValue}" /> class.
    /// </summary>
    /// <param name="valueConverter">The converter for the value type.</param>
    /// <param name="keyKind">The kind of key conversion to apply.</param>
    /// <param name="concrete">Whether the declared type is a concrete dictionary.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="valueConverter" /> is <see langword="null" />.
    /// </exception>
    public DictionaryConverter(YamlConverter valueConverter, DictionaryKeyKind keyKind, bool concrete)
    {
        ThrowHelper.ThrowIfNull(valueConverter);

        _valueConverter = valueConverter;
        _keyKind = keyKind;
        _concrete = concrete;
    }

    /// <inheritdoc />
    public override TDictionary Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return null!;

        if (reader.TokenType != YamlTokenType.StartMapping)
            throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedMapping);

        Dictionary<TKey, TValue> entries = [];
        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string keyText = reader.GetString();
            TKey key = ParseKey(keyText);
            reader.Read();
            entries[key] = (TValue)ChildBinder.Read(ref reader, _valueConverter, typeof(TValue), options, keyText)!;
        }

        return Materialize(entries);
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, TDictionary value, YamlSerializerOptions options)
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
            writer.WriteStartMapping();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<TKey, TValue> entry in (IEnumerable<KeyValuePair<TKey, TValue>>)value)
            {
                if (state is { HasFailure: true })
                    break;

                string keyText = Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                if (!seen.Add(keyText))
                {
                    throw new YamlSerializationException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlDuplicateDictionaryKey, keyText));
                }

                state?.PushPath(keyText);
                writer.WritePropertyName(keyText);
                _valueConverter.WriteAsObject(writer, entry.Value, options);
                state?.PopPath();
            }

            if (state is { HasFailure: true })
                return;

            writer.WriteEndMapping();
        }
        finally
        {
            if (state is not null)
                state.ExitReference(value);
        }
    }

    /// <summary>
    /// Parses a mapping key back to <typeparamref name="TKey" />, mirroring the coercions of the former binding
    /// walker: strings pass through, enumeration names match case-insensitively, and every other supported kind
    /// converts from its invariant text.
    /// </summary>
    /// <param name="text">The key text read from the document.</param>
    /// <returns>The parsed key.</returns>
    private TKey ParseKey(string text) =>
        _keyKind switch
        {
            DictionaryKeyKind.String => (TKey)(object)text,
            DictionaryKeyKind.Enum => (TKey)Enum.Parse(typeof(TKey), text, ignoreCase: true),
            _ => (TKey)Convert.ChangeType(text, typeof(TKey), CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Materializes the declared dictionary type from the read entries.
    /// </summary>
    /// <param name="entries">The read entries.</param>
    /// <returns>The materialized dictionary.</returns>
    private TDictionary Materialize(Dictionary<TKey, TValue> entries)
    {
        if (!_concrete)
            return (TDictionary)(object)entries;

        TDictionary instance = Activator.CreateInstance<TDictionary>()!;
        var dictionary = (IDictionary<TKey, TValue>)instance;
        foreach (KeyValuePair<TKey, TValue> entry in entries)
            dictionary[entry.Key] = entry.Value;

        return instance;
    }
}
