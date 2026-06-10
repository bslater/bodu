// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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

        return (T)Construct(metadata, values);
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
            if (ShouldSkip(property, memberValue))
                continue;

            writer.WritePropertyName(property.WireName);
            property.Converter.WriteAsObject(writer, memberValue, options);
        }

        writer.WriteEndDictionary();
    }

    /// <summary>
    /// Determines whether a member is omitted from the output for the supplied value.
    /// </summary>
    /// <param name="property">The member metadata.</param>
    /// <param name="value">The member value.</param>
    /// <returns>
    /// <see langword="true" /> when the member should be skipped; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// A <see langword="null" /> value is always skipped because Bencode cannot represent it; the conditional-ignore
    /// settings refine that for non-null default values.
    /// </remarks>
    private static bool ShouldSkip(PropertyMetadata property, object? value)
    {
        if (value is null)
            return true;

        return property.ConditionalIgnore == BencodeIgnoreCondition.WhenWritingDefault && Equals(value, property.DefaultTypeValue);
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
