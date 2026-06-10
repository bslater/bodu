// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization.Metadata;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts an object of type <typeparamref name="T" /> to and from an object token, mapping each serializable member
/// to a property. Members are read into a buffer and then bound either through a parameterless constructor and setters
/// or through a parameterized constructor, according to the type's resolved metadata.
/// </summary>
/// <typeparam name="T">The object type.</typeparam>
internal sealed class ObjectConverter<T>
    : FormatConverter<T>
{
    /// <inheritdoc />
    public override T Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);
        ThrowHelper.ThrowIfNull(options);

        if (reader.TokenKind != SerializationValueKind.StartObject)
        {
            throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_ExpectedObject, reader.TokenKind),
                reader.Location);
        }

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        if (!metadata.CanConstruct)
            throw new FormatSerializationException(string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_NotSupported_Deserialize, typeof(T)));

        Dictionary<PropertyMetadata, object?> values = [];
        while (reader.Read() && reader.TokenKind != SerializationValueKind.EndObject)
        {
            var name = reader.GetPropertyName();
            reader.Read();

            if (metadata.TryGetProperty(name, out PropertyMetadata? property) && property is not null)
            {
                if (!values.TryAdd(property, property.Converter.ReadAsObject(reader, property.PropertyType, options)))
                {
                    throw new FormatSerializationException(
                        string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_DuplicateProperty, name),
                        reader.Location);
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
                throw new FormatSerializationException(
                    string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_MissingRequiredMember, property.WireName, typeof(T)),
                    reader.Location);
            }
        }

        return (T)Construct(metadata, values);
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, T value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        writer.WriteStartObject();
        foreach (PropertyMetadata property in metadata.Properties)
        {
            object? memberValue = property.GetValue(value);
            if (ShouldSkip(property, memberValue, options))
                continue;

            writer.WritePropertyName(property.WireName);
            if (memberValue is null)
                writer.WriteNull();
            else
                property.Converter.WriteAsObject(writer, memberValue, options);
        }

        writer.WriteEndObject();
    }

    /// <summary>
    /// Determines whether a member is omitted from the output for the supplied value.
    /// </summary>
    /// <param name="property">The member metadata.</param>
    /// <param name="value">The member value.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>
    /// <see langword="true" /> when the member should be skipped; otherwise <see langword="false" />.
    /// </returns>
    private static bool ShouldSkip(PropertyMetadata property, object? value, FormatSerializerOptions options)
    {
        if (property.ConditionalIgnore == FormatIgnoreCondition.WhenWritingNull && value is null)
            return true;

        if (property.ConditionalIgnore == FormatIgnoreCondition.WhenWritingDefault && Equals(value, property.DefaultTypeValue))
            return true;

        return value is null && options.NullHandling == FormatNullHandling.IgnoreOnWrite;
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
