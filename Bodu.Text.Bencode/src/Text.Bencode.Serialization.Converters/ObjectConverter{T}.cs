// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Unicode;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization.Metadata;
using Bodu.Text.Bencode.Writer;

using Bodu.Text.Serialization;

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
                reader.TokenStartIndex);
        }

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        if (!metadata.CanConstruct)
            throw new BencodeSerializationException(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_NotSupported_Deserialize, typeof(T)));

        // Slot-indexed flat buffers replace a per-object Dictionary<PropertyMetadata, object?>: values holds each
        // member's read value at its metadata slot, and present distinguishes an absent member from a read null.
        object?[] values = new object?[metadata.PropertyCount];
        bool[] present = new bool[metadata.PropertyCount];
        Dictionary<string, BencodeNode?>? extensionEntries = null;
        while (reader.Read() && reader.TokenType != BencodeTokenType.EndDictionary)
        {
            // The key's bytes stay addressable after the reader moves on, because they are a slice of the input.
            ReadOnlySpan<byte> nameBytes = reader.ValueSpan;
            int nameOffset = reader.TokenStartIndex;
            string name = reader.GetString();
            reader.Read();

            if (metadata.TryGetProperty(name, out PropertyMetadata? property) && property is not null)
            {
                object? converted = property.Converter.ReadAsObject(ref reader, property.PropertyType, options);

                // Lenient duplicate handling binds last-wins, matching the dictionary converter's indexer assignment.
                if (!options.AllowDuplicateKeys && present[property.SlotIndex])
                {
                    throw new BencodeSerializationException(
                        string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_DuplicateProperty, name),
                        reader.TokenStartIndex);
                }

                values[property.SlotIndex] = converted;
                present[property.SlotIndex] = true;
            }
            else if (metadata.ExtensionData is not null)
            {
                // An extension-data key is kept as text, so a key that is not valid UTF-8 is refused rather than altered.
                if (!Utf8.IsValid(nameBytes))
                {
                    throw new BencodeSerializationException(
                        string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_DictionaryKeyNotUtf8, nameOffset, typeof(T)),
                        nameOffset);
                }

                extensionEntries ??= new Dictionary<string, BencodeNode?>(StringComparer.Ordinal);
                extensionEntries[name] = BencodeNode.ReadFrom(ref reader);
            }
            else if ((metadata.UnmappedMemberHandling ?? options.UnmappedMemberHandling) == UnmappedMemberHandling.Disallow)
            {
                throw new BencodeSerializationException(
                    string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_UnmappedMember, name, typeof(T)),
                    reader.TokenStartIndex);
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
                throw new BencodeSerializationException(
                    string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_MissingRequiredMember, property.WireName, typeof(T)),
                    reader.TokenStartIndex);
            }
        }

        // Keep the instance boxed for the whole assignment phase. For a value type each member assignment must target
        // the same box, so unboxing to T before assignment would mutate a throwaway copy and lose the values.
        object instance = ObjectBinder.Construct(metadata, values, present);
        (instance as IOnDeserializing)?.OnDeserializing();
        ObjectBinder.AssignMembers(metadata, values, present, instance, options.PreferredObjectCreationHandling);
        PopulateExtensionData(metadata, instance, extensionEntries);
        (instance as IOnDeserialized)?.OnDeserialized();
        return (T)instance;
    }

    /// <inheritdoc />
    public override void Write(Utf8BencodeWriter writer, T value, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
            return;

        BencodeWriteStack? state = writer.WriteStack;
        if (state is { HasFailure: true })
            return;

        (value as IOnSerializing)?.OnSerializing();

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));

        // Refuse to descend past the ceiling before opening the dictionary, so the failure is recorded cooperatively
        // and the recursion unwinds through returns rather than throwing from the deepest writer frame.
        if (state is not null && writer.CurrentDepth >= writer.EffectiveMaxDepth)
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_WriterMaxDepthExceeded, writer.EffectiveMaxDepth));
            return;
        }

        writer.WriteStartDictionary();
        foreach (PropertyMetadata property in metadata.Properties)
        {
            object? memberValue = property.GetValue(value);
            if (ShouldSkip(property, memberValue, options))
                continue;

            writer.WritePropertyName(property.WireName);
            property.Converter.WriteAsObject(writer, memberValue, options);
            if (state is { HasFailure: true })
                return;
        }

        WriteExtensionData(writer, metadata, value);

        writer.WriteEndDictionary();

        (value as IOnSerialized)?.OnSerialized();
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
    /// <param name="instance">The constructed instance, boxed.</param>
    /// <param name="entries">The captured unmatched entries, or <see langword="null" /> when none were read.</param>
    private static void PopulateExtensionData(TypeMetadata metadata, object instance, Dictionary<string, BencodeNode?>? entries)
    {
        if (entries is null || entries.Count == 0 || metadata.ExtensionData is not { } member)
            return;

        if (member.CanSet)
        {
            object materialized = member.PropertyType == typeof(BencodeObject)
                ? new BencodeObject(entries)
                : entries;
            member.SetValue(instance, materialized);
            return;
        }

        if (member.GetValue(instance) is IDictionary<string, BencodeNode?> existing)
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
    /// is <see cref="IgnoreCondition.WhenWritingDefault" /> and the value equals the member's default-type value.
    /// </remarks>
    private static bool ShouldSkip(PropertyMetadata property, object? value, BencodeSerializerOptions options)
    {
        if (value is null)
            return true;

        IgnoreCondition effective = property.ConditionalIgnore ?? options.DefaultIgnoreCondition;
        return effective == IgnoreCondition.WhenWritingDefault && Equals(value, property.DefaultTypeValue);
    }
}
