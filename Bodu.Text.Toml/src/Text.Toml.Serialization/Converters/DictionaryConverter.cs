// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a dictionary of <typeparamref name="TKey" /> to <typeparamref name="TValue" /> to and from a TOML table,
/// where each entry becomes a key/value pair whose table key is the stringified dictionary key.
/// </summary>
/// <typeparam name="TDictionary">
/// The declared dictionary type (a concrete dictionary or a dictionary interface).
/// </typeparam>
/// <typeparam name="TKey">The key type, one of the kinds named by <see cref="DictionaryKeyKind" />.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
/// <remarks>
/// <para>
/// TOML table keys are strings, so a non-string key is converted to round-trippable invariant-culture text on write and
/// parsed back on read: integers use decimal text, enumerations use the member name produced by
/// <see cref="Enum.ToString()" /> (decimal or comma-separated fallback for undefined or combined values, matched
/// case-insensitively on read), <see cref="Guid" /> uses the hyphenated "D" format, <see cref="bool" /> uses
/// <c>True</c>/<c>False</c>, and <see cref="char" /> uses a single-character string. Key stringification ignores naming
/// policies and <see cref="TomlStringEnumMemberNameAttribute" />, mirroring how
/// <see cref="System.Text.Json.JsonSerializer" /> treats dictionary keys independently of value converters. Entries are
/// written in the dictionary's enumeration (insertion) order — TOML output preserves document order, so the stringified
/// keys are <b>not</b> sorted, in contrast to the canonical bytewise key ordering of the Bencode twin. The writer
/// quotes a stringified key only when it does not match the bare-key grammar. A key that cannot be parsed back surfaces
/// as a <see cref="TomlSerializationException" />.
/// </para>
/// <para>
/// TOML has no null, so an entry whose value is <see langword="null" /> is omitted from the output rather than written
/// with a placeholder.
/// </para>
/// </remarks>
internal sealed class DictionaryConverter<TDictionary, TKey, TValue>
    : TomlConverter<TDictionary>
    where TDictionary : class
    where TKey : notnull
{
    /// <summary>
    /// The converter for the value type.
    /// </summary>
    private readonly TomlConverter _valueConverter;

    /// <summary>
    /// The kind of key conversion applied between <typeparamref name="TKey" /> and the TOML table key.
    /// </summary>
    private readonly DictionaryKeyKind _keyKind;

    /// <summary>
    /// Indicates whether the declared type is a concrete dictionary that must be instantiated and populated.
    /// </summary>
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
    public DictionaryConverter(TomlConverter valueConverter, DictionaryKeyKind keyKind, bool concrete)
    {
        ThrowHelper.ThrowIfNull(valueConverter);

        _valueConverter = valueConverter;
        _keyKind = keyKind;
        _concrete = concrete;
    }

    /// <inheritdoc />
    public override TDictionary Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.StartTable)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedTable, reader.TokenType));
        }

        Dictionary<TKey, TValue> entries = [];
        while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
        {
            TKey key = ParseKey(reader.GetString());
            reader.Read();
            entries[key] = (TValue)_valueConverter.ReadAsObject(ref reader, typeof(TValue), options) !;
        }

        return Materialize(entries);
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, TDictionary value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        writer.WriteStartTable();
        foreach (KeyValuePair<TKey, TValue> entry in (IEnumerable<KeyValuePair<TKey, TValue>>)value)
        {
            if (entry.Value is null)
                continue;

            writer.WritePropertyName(FormatKey(entry.Key));
            _valueConverter.WriteAsObject(writer, entry.Value, options);
        }

        writer.WriteEndTable();
    }

    /// <summary>
    /// Converts a dictionary key to its table-key text form.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The round-trippable invariant-culture text for the key.</returns>
    private string FormatKey(TKey key) =>
        _keyKind switch
        {
            DictionaryKeyKind.String => (string)(object)key,
            DictionaryKeyKind.Integer => ((IFormattable)key).ToString(null, CultureInfo.InvariantCulture),
            DictionaryKeyKind.Guid => ((Guid)(object)key).ToString("D", CultureInfo.InvariantCulture),
            _ => key.ToString() !,
        };

    /// <summary>
    /// Parses a table key back to <typeparamref name="TKey" />.
    /// </summary>
    /// <param name="text">The key text read from the document.</param>
    /// <returns>The parsed key.</returns>
    /// <exception cref="TomlSerializationException">
    /// Thrown when <paramref name="text" /> is not a valid representation of <typeparamref name="TKey" />.
    /// </exception>
    private TKey ParseKey(string text)
    {
        try
        {
            switch (_keyKind)
            {
                case DictionaryKeyKind.String:
                    return (TKey)(object)text;

                case DictionaryKeyKind.Integer:
                    return (TKey)Convert.ChangeType(text, typeof(TKey), CultureInfo.InvariantCulture);

                case DictionaryKeyKind.Enum:
                    return (TKey)Enum.Parse(typeof(TKey), text, ignoreCase: true);

                case DictionaryKeyKind.Guid:
                    return (TKey)(object)Guid.ParseExact(text, "D");

                case DictionaryKeyKind.Boolean:
                    return (TKey)(object)bool.Parse(text);

                default:
                    if (text.Length != 1)
                        throw new FormatException();

                    return (TKey)(object)text[0];
            }
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_DictionaryKeyParse, text, typeof(TKey)),
                ex);
        }
    }

    /// <summary>
    /// Materializes the declared dictionary type from the read entries.
    /// </summary>
    /// <param name="entries">The read entries.</param>
    /// <returns>The materialized dictionary.</returns>
    private TDictionary Materialize(Dictionary<TKey, TValue> entries)
    {
        if (!_concrete)
            return (TDictionary)(object)entries;

        var instance = (TDictionary)Activator.CreateInstance(typeof(TDictionary)) !;
        var dictionary = (IDictionary<TKey, TValue>)instance;
        foreach (KeyValuePair<TKey, TValue> entry in entries)
            dictionary[entry.Key] = entry.Value;

        return instance;
    }
}
