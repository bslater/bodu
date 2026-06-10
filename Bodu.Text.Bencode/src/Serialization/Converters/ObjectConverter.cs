// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Serialization.Metadata;

namespace Bodu.Text.Bencode.Serialization.Converters;

/// <summary>
/// Converts an object of type <typeparamref name="T" /> to and from a Bencode dictionary, mapping each serializable
/// member to a key/value pair. Members are read into a buffer and then bound either through a parameterless constructor
/// and setters or through a parameterized constructor, according to the type's resolved metadata.
/// </summary>
/// <typeparam name="T">The object type.</typeparam>
/// <remarks>
/// Bencode has no null token, so a member whose value is <see langword="null" /> is omitted from the output rather than
/// written with a placeholder.
/// </remarks>
internal sealed class ObjectConverter<T>
    : BencodeConverter<T>
{
    /// <inheritdoc />
    public override T Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (reader.TokenType != BencodeTokenType.StartDictionary)
        {
            throw new BencodeSerializationException(
                string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ExpectedDictionary, reader.TokenType),
                reader.BytesConsumed);
        }

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        if (!metadata.CanConstruct)
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_NotSupported_Deserialize, typeof(T)));

        Dictionary<PropertyMetadata, object?> values = [];
        Dictionary<string, BencodeNode?>? extensionEntries = null;
        while (reader.Read() && reader.TokenType != BencodeTokenType.EndDictionary)
        {
            var name = reader.GetString();
            reader.Read();

            if (metadata.TryGetProperty(name, out PropertyMetadata? property) && property is not null)
            {
                if (!values.TryAdd(property, property.Converter.ReadAsObject(ref reader, property.PropertyType, options)))
                {
                    throw new BencodeSerializationException(
                        string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_DuplicateProperty, name),
                        reader.BytesConsumed);
                }
            }
            else if (metadata.ExtensionData is not null)
            {
                extensionEntries ??= new Dictionary<string, BencodeNode?>(StringComparer.Ordinal);
                extensionEntries[name] = BencodeNode.ReadFrom(ref reader);
            }
            else
            {
                reader.Skip();
            }
        }

        foreach (PropertyMetadata property in metadata.Properties)
        {
            if (property.IsRequired && !values.ContainsKey(property))
            {
                throw new BencodeSerializationException(
                    string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_MissingRequiredMember, property.WireName, typeof(T)),
                    reader.BytesConsumed);
            }
        }

        var instance = (T)Construct(metadata, values);
        PopulateExtensionData(metadata, instance, extensionEntries);
        return instance;
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
            return;

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        writer.WriteStartDictionary();
        foreach (PropertyMetadata property in metadata.Properties)
        {
            object? memberValue = property.GetValue(value);
            if (ShouldSkip(property, memberValue, options))
                continue;

            writer.WritePropertyName(property.WireName);
            property.Converter.WriteAsObject(writer, memberValue, options);
        }

        WriteExtensionData(writer, metadata, value);

        writer.WriteEndDictionary();
    }

    /// <summary>
    /// Writes the entries held by the type's extension-data member, when one is declared and populated. The writer
    /// re-sorts dictionary keys on close, so the entries merge into canonical key order alongside the type's other
    /// members.
    /// </summary>
    /// <param name="writer">The destination writer, positioned inside the open dictionary.</param>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="value">The instance being written.</param>
    private static void WriteExtensionData(Utf8BencodeWriter writer, TypeMetadata metadata, T value)
    {
        if (metadata.ExtensionData is not { } member)
            return;

        if (member.GetValue(value!) is not IEnumerable<KeyValuePair<string, BencodeNode?>> entries)
            return;

        foreach (KeyValuePair<string, BencodeNode?> entry in entries)
        {
            if (entry.Value is null)
                continue;

            writer.WritePropertyName(entry.Key);
            entry.Value.WriteTo(writer);
        }
    }

    /// <summary>
    /// Assigns the captured unmatched entries to the type's extension-data member, materializing the member's declared
    /// type or adding into a pre-initialized instance when the member is get-only.
    /// </summary>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="instance">The constructed instance.</param>
    /// <param name="entries">The captured unmatched entries, or <see langword="null" /> when none were read.</param>
    private static void PopulateExtensionData(TypeMetadata metadata, T instance, Dictionary<string, BencodeNode?>? entries)
    {
        if (entries is null || entries.Count == 0 || metadata.ExtensionData is not { } member)
            return;

        if (member.CanSet)
        {
            object materialized = member.PropertyType == typeof(BencodeObject)
                ? new BencodeObject(entries)
                : entries;
            member.SetValue(instance!, materialized);
            return;
        }

        if (member.GetValue(instance!) is IDictionary<string, BencodeNode?> existing)
        {
            foreach (KeyValuePair<string, BencodeNode?> entry in entries)
                existing[entry.Key] = entry.Value;
        }
    }

    /// <summary>
    /// Determines whether a member is omitted from the output for the supplied value, applying the member's own ignore
    /// condition when present and otherwise the serializer-wide default.
    /// </summary>
    /// <param name="property">The member metadata.</param>
    /// <param name="value">The member value.</param>
    /// <param name="options">The serializer options that supply the default ignore condition.</param>
    /// <returns>
    /// <see langword="true" /> when the member should be skipped; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// A <see langword="null" /> value is always skipped because Bencode cannot represent it. For a non-null value, the
    /// effective condition is the member's <see cref="PropertyMetadata.ConditionalIgnore" /> when set, otherwise
    /// <see cref="BencodeSerializerOptions.DefaultIgnoreCondition" />: a value is skipped when the effective condition
    /// is <see cref="BencodeIgnoreCondition.WhenWritingDefault" /> and the value equals the member's default-type
    /// value.
    /// </remarks>
    private static bool ShouldSkip(PropertyMetadata property, object? value, BencodeSerializerOptions options)
    {
        if (value is null)
            return true;

        BencodeIgnoreCondition effective = property.ConditionalIgnore ?? options.DefaultIgnoreCondition;
        return effective == BencodeIgnoreCondition.WhenWritingDefault && Equals(value, property.DefaultTypeValue);
    }

    /// <summary>
    /// Constructs an instance from the read member values, using the type's construction plan.
    /// </summary>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="values">The read member values.</param>
    /// <returns>The constructed instance.</returns>
    private static object Construct(TypeMetadata metadata, Dictionary<PropertyMetadata, object?> values)
    {
        if (metadata.UsesParameterizedConstructor)
        {
            var arguments = new object?[metadata.ConstructorParameterCount];
            for (var i = 0; i < arguments.Length; i++)
            {
                PropertyMetadata? parameter = metadata.GetConstructorParameter(i);
                arguments[i] = parameter is not null && values.TryGetValue(parameter, out object? value)
                    ? value
                    : metadata.GetConstructorDefault(i);
            }

            object instance = metadata.Construct(arguments);
            AssignSettableMembers(values, instance, skipConstructorBound: true);
            return instance;
        }

        object created = metadata.Construct(null);
        AssignSettableMembers(values, created, skipConstructorBound: false);
        return created;
    }

    /// <summary>
    /// Assigns the read values to the settable members of an instance.
    /// </summary>
    /// <param name="values">The read member values.</param>
    /// <param name="instance">The instance to assign on.</param>
    /// <param name="skipConstructorBound">Whether members bound to a constructor parameter are skipped.</param>
    private static void AssignSettableMembers(Dictionary<PropertyMetadata, object?> values, object instance, bool skipConstructorBound)
    {
        foreach (KeyValuePair<PropertyMetadata, object?> entry in values)
        {
            PropertyMetadata property = entry.Key;
            if (skipConstructorBound && property.ConstructorParameterIndex >= 0)
                continue;

            if (property.CanSet)
                property.SetValue(instance, entry.Value);
        }
    }
}
