// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization.Metadata;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts an object of type <typeparamref name="T" /> to and from a TOML table, mapping each serializable member to a
/// key/value pair. Members are read into a buffer and then bound either through a parameterless constructor and setters
/// or through a parameterized constructor, according to the type's resolved metadata.
/// </summary>
/// <typeparam name="T">The object type.</typeparam>
/// <remarks>
/// TOML has no null, so a member whose value is <see langword="null" /> is omitted from the output rather than written
/// with a placeholder.
/// </remarks>
internal sealed class ObjectConverter<T>
    : TomlConverter<T>
{
    /// <inheritdoc />
    public override T Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (reader.TokenType != TomlTokenType.StartTable)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedTable, reader.TokenType));
        }

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        if (!metadata.CanConstruct)
            throw new TomlSerializationException(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_NotSupported_Deserialize, typeof(T)));

        // Slot-indexed flat buffers replace a per-object Dictionary<PropertyMetadata, object?>: values holds each
        // member's read value at its metadata slot, and present distinguishes an absent member from a read null.
        object?[] values = new object?[metadata.PropertyCount];
        bool[] present = new bool[metadata.PropertyCount];
        Dictionary<string, TomlNode?>? extensionEntries = null;
        while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
        {
            string name = reader.GetString();
            reader.Read();

            if (metadata.TryGetProperty(name, out PropertyMetadata? property) && property is not null)
            {
                object? converted;
                try
                {
                    converted = property.Converter.ReadAsObject(ref reader, property.PropertyType, options);
                }
                catch (TomlSerializationException ex)
                {
                    ex.Path = TomlSerializationException.CombinePath(name, ex.Path);
                    reader.StampPosition(ex);
                    throw;
                }

                if (present[property.SlotIndex])
                {
                    throw new TomlSerializationException(
                        string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_DuplicateProperty, name));
                }

                values[property.SlotIndex] = converted;
                present[property.SlotIndex] = true;
            }
            else if (metadata.ExtensionData is not null)
            {
                extensionEntries ??= new Dictionary<string, TomlNode?>(StringComparer.Ordinal);
                extensionEntries[name] = TomlNode.ReadFrom(ref reader);
            }
            else if ((metadata.UnmappedMemberHandling ?? options.UnmappedMemberHandling) == UnmappedMemberHandling.Disallow)
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_UnmappedMember, name, typeof(T)));
            }
            else
            {
                reader.Skip();
            }
        }

        foreach (PropertyMetadata property in metadata.Properties)
        {
            if (property.IsRequired && !present[property.SlotIndex])
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_MissingRequiredMember, property.WireName, typeof(T)));
            }
        }

        // Keep the constructed instance boxed through every mutation. For a value type, unboxing to a local T and
        // then passing it to the reflection-based assignment helpers re-boxes a throwaway copy, so all settable-member
        // and extension-data writes (and any mutating deserialization callback) would be silently lost. Threading the
        // single box through and unboxing only at the return preserves those writes.
        object boxed = ObjectBinder.Construct(metadata, values, present);
        (boxed as IOnDeserializing)?.OnDeserializing();
        ObjectBinder.AssignMembers(metadata, values, present, boxed, options.PreferredObjectCreationHandling);
        PopulateExtensionData(metadata, boxed, extensionEntries);
        (boxed as IOnDeserialized)?.OnDeserialized();
        return (T)boxed;
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, T value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
            return;

        TomlWriteStack? state = writer.WriteStack;
        if (state is { HasFailure: true })
            return;

        // A value type cannot participate in a reference cycle, so only reference instances are tracked.
        bool tracked = !typeof(T).IsValueType;
        if (tracked && state is not null && !state.TryEnterReference(value!))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_CycleDetected, typeof(T)));
            return;
        }

        try
        {
            (value as IOnSerializing)?.OnSerializing();

            TypeMetadata metadata = options.GetTypeMetadata(typeof(T));

            // Track emitted keys only when extension data may follow, so a colliding overflow entry is rejected as a
            // serialization error rather than surfacing as a writer-level duplicate-key failure.
            HashSet<string>? emittedKeys = metadata.ExtensionData is null ? null : new HashSet<string>(StringComparer.Ordinal);

            // Refuse to descend past the ceiling before opening the table, so the failure is recorded cooperatively and
            // the recursion unwinds through returns rather than throwing from the deepest writer frame.
            if (state is not null && writer.Depth >= writer.EffectiveMaxDepth)
            {
                state.SetFailure(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_WriterMaxDepthExceeded, writer.EffectiveMaxDepth));
                return;
            }

            writer.WriteStartTable();
            foreach (PropertyMetadata property in metadata.Properties)
            {
                object? memberValue = property.GetValue(value);
                if (ShouldSkip(property, memberValue, options))
                    continue;

                writer.WritePropertyName(property.WireName);

                state?.PushPath(property.WireName);
                property.Converter.WriteAsObject(writer, memberValue, options);
                if (state is { HasFailure: true })
                    return;
                state?.PopPath();

                _ = emittedKeys?.Add(property.WireName);
            }

            WriteExtensionData(writer, metadata, value, emittedKeys);

            writer.WriteEndTable();

            (value as IOnSerialized)?.OnSerialized();
        }
        finally
        {
            if (tracked && state is not null)
                state.ExitReference(value!);
        }
    }

    /// <summary>
    /// Writes the entries held by the type's extension-data member, when one is declared and populated.
    /// </summary>
    /// <param name="writer">The destination writer, positioned inside the open table.</param>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="value">The instance being written.</param>
    /// <param name="emittedKeys">
    /// The wire names already written for declared members, or <see langword="null" />.
    /// </param>
    /// <exception cref="TomlSerializationException">
    /// Thrown when an extension-data key collides with a key already written to the table.
    /// </exception>
    private static void WriteExtensionData(Utf8TomlWriter writer, TypeMetadata metadata, T value, HashSet<string>? emittedKeys)
    {
        if (metadata.ExtensionData is not { } member)
            return;

        if (member.GetValue(value!) is not IEnumerable<KeyValuePair<string, TomlNode?>> entries)
            return;

        foreach (KeyValuePair<string, TomlNode?> entry in entries)
        {
            if (entry.Value is null)
                continue;

            if (emittedKeys is not null && !emittedKeys.Add(entry.Key))
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExtensionDataKeyCollision, entry.Key, typeof(T)));
            }

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
    private static void PopulateExtensionData(TypeMetadata metadata, object instance, Dictionary<string, TomlNode?>? entries)
    {
        if (entries is null || entries.Count == 0 || metadata.ExtensionData is not { } member)
            return;

        if (member.CanSet)
        {
            object materialized = member.PropertyType == typeof(TomlObject)
                ? new TomlObject(entries)
                : entries;
            member.SetValue(instance, materialized);
            return;
        }

        if (member.GetValue(instance) is IDictionary<string, TomlNode?> existing)
        {
            foreach (KeyValuePair<string, TomlNode?> entry in entries)
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
    /// A <see langword="null" /> value is always skipped because TOML cannot represent it. For a non-null value, the
    /// effective condition is the member's <see cref="PropertyMetadata.ConditionalIgnore" /> when set, otherwise
    /// <see cref="TomlSerializerOptions.DefaultIgnoreCondition" />: a value is skipped when the effective condition is
    /// <see cref="IgnoreCondition.WhenWritingDefault" /> and the value equals the member's default-type value.
    /// </remarks>
    private static bool ShouldSkip(PropertyMetadata property, object? value, TomlSerializerOptions options)
    {
        if (value is null)
            return true;

        IgnoreCondition effective = property.ConditionalIgnore ?? options.DefaultIgnoreCondition;
        return effective == IgnoreCondition.WhenWritingDefault && Equals(value, property.DefaultTypeValue);
    }
}
