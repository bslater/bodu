// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization.Metadata;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts an object of type <typeparamref name="T" /> to and from a YAML mapping, mapping each serializable member to
/// a key/value pair. Members are read into a buffer and then bound either through a parameterless constructor and
/// setters or through a parameterized constructor, according to the type's resolved metadata.
/// </summary>
/// <typeparam name="T">The object type.</typeparam>
/// <remarks>
/// Writing dispatches on the value's runtime type: a value whose runtime type differs from <typeparamref name="T" />
/// re-enters converter resolution so a polymorphic member serializes exactly as its concrete instance would. A member
/// whose value is <see langword="null" /> writes the YAML null scalar unless the member's ignore condition — or the
/// serializer-wide <see cref="YamlSerializerOptions.IgnoreNullValues" /> — omits it.
/// </remarks>
internal sealed class ObjectConverter<T>
    : YamlConverter<T>
{
    /// <inheritdoc />
    public override T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (reader.TokenType == YamlTokenType.Null)
            return default!;

        if (reader.TokenType != YamlTokenType.StartMapping)
            throw new YamlSerializationException(YamlResourceStrings.Op_Invalid_YamlExpectedMapping);

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));
        if (!metadata.CanConstruct)
            throw new YamlSerializationException(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlTypeNotInstantiable, typeof(T)));

        // Slot-indexed flat buffers hold each member's read value at its metadata slot; present distinguishes an
        // absent member from a read null. Duplicate keys are governed by the reader's duplicate-key policy, so a key
        // that survives it (last-wins) overwrites the earlier read value rather than failing here.
        object?[] values = new object?[metadata.PropertyCount];
        bool[] present = new bool[metadata.PropertyCount];
        Dictionary<string, object?>? extensionEntries = null;
        while (reader.Read() && reader.TokenType != YamlTokenType.EndMapping)
        {
            string name = reader.GetString();
            reader.Read();

            if (metadata.TryGetProperty(name, out PropertyMetadata? property) && property is not null)
            {
                values[property.SlotIndex] = ChildBinder.Read(ref reader, property.Converter, property.PropertyType, options, property.WireName);
                present[property.SlotIndex] = true;
            }
            else if (metadata.ExtensionData is not null)
            {
                extensionEntries ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                extensionEntries[name] = ChildBinder.Read(ref reader, options.GetConverter(typeof(object)), typeof(object), options, name);
            }
            else if ((metadata.UnmappedMemberHandling ?? options.UnmappedMemberHandling) == UnmappedMemberHandling.Disallow)
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlUnmappedMember, name, typeof(T)));
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
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlMissingRequiredMember, property.WireName, typeof(T)));
            }
        }

        // Keep the constructed instance boxed through every mutation. For a value type, unboxing to a local T and
        // then passing it to the assignment helpers re-boxes a throwaway copy, so all settable-member and
        // extension-data writes (and any mutating deserialization callback) would be silently lost. Threading the
        // single box through and unboxing only at the return preserves those writes.
        object boxed = BareConstruct(metadata, values, present);
        (boxed as IOnDeserializing)?.OnDeserializing();
        AssignSettableMembers(metadata, values, present, boxed, options);
        PopulateExtensionData(metadata, boxed, extensionEntries);
        (boxed as IOnDeserialized)?.OnDeserialized();
        return (T)boxed;
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        // Dispatch on the runtime type so a derived instance held by a base-typed member serializes its own members,
        // mirroring the runtime-first converter resolution of the former write walker.
        Type runtimeType = value!.GetType();
        if (runtimeType != typeof(T))
        {
            options.GetConverter(runtimeType).WriteAsObject(writer, value, options);
            return;
        }

        YamlWriteStack? state = writer.WriteStack;
        if (state is { HasFailure: true })
            return;

        // A value type cannot participate in a reference cycle, so only reference instances are tracked.
        bool tracked = !typeof(T).IsValueType;
        if (tracked && state is not null && !state.TryEnterReference(value!))
        {
            state.SetFailure(string.Format(CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlCycleDetected, typeof(T)));
            return;
        }

        try
        {
            (value as IOnSerializing)?.OnSerializing();

            TypeMetadata metadata = options.GetTypeMetadata(typeof(T));

            writer.WriteStartMapping();
            foreach (PropertyMetadata property in metadata.Properties)
            {
                if (state is { HasFailure: true })
                    break;

                object? memberValue = property.GetValue(value!);
                if (ShouldOmit(property, memberValue, options))
                    continue;

                state?.PushPath(property.WireName);
                writer.WritePropertyName(property.WireName);
                property.Converter.WriteAsObject(writer, memberValue, options);
                state?.PopPath();
            }

            if (state is { HasFailure: true })
                return;

            WriteExtensionData(writer, metadata, value!, options);

            if (state is { HasFailure: true })
                return;

            writer.WriteEndMapping();

            (value as IOnSerialized)?.OnSerialized();
        }
        finally
        {
            if (tracked && state is not null)
                state.ExitReference(value!);
        }
    }

    /// <summary>
    /// Writes the entries held by the type's extension-data member after its declared members, rejecting an entry whose
    /// key collides with a declared member's wire name.
    /// </summary>
    /// <param name="writer">The destination writer, positioned inside the open mapping.</param>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="value">The instance being written.</param>
    /// <param name="options">The serializer options.</param>
    /// <exception cref="YamlSerializationException">
    /// Thrown when an extension-data key collides with a declared member's wire name.
    /// </exception>
    private static void WriteExtensionData(Utf8YamlWriter writer, TypeMetadata metadata, T value, YamlSerializerOptions options)
    {
        if (metadata.ExtensionData is not { } member)
            return;

        if (member.GetValue(value!) is not IDictionary<string, object?> entries || entries.Count == 0)
            return;

        YamlWriteStack? state = writer.WriteStack;

        // The collision check covers every declared wire name — including members omitted from this document by an
        // ignore condition — matching the walker's contract that extension data never shadows a declared member.
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (PropertyMetadata property in metadata.Properties)
            declared.Add(property.WireName);

        foreach (KeyValuePair<string, object?> entry in entries)
        {
            if (state is { HasFailure: true })
                return;

            if (declared.Contains(entry.Key))
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlExtensionDataKeyCollision, entry.Key, value!.GetType()));
            }

            state?.PushPath(entry.Key);
            writer.WritePropertyName(entry.Key);

            if (entry.Value is null)
                writer.WriteNull();
            else
                options.GetConverter(entry.Value.GetType()).WriteAsObject(writer, entry.Value, options);

            state?.PopPath();
        }
    }

    /// <summary>
    /// Assigns the captured unmatched entries to the type's extension-data member, reusing the dictionary the member
    /// already holds or assigning a fresh one through the setter.
    /// </summary>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="instance">The constructed instance.</param>
    /// <param name="entries">The captured unmatched entries, or <see langword="null" /> when none were read.</param>
    private static void PopulateExtensionData(TypeMetadata metadata, object instance, Dictionary<string, object?>? entries)
    {
        if (metadata.ExtensionData is not { } member)
            return;

        if (member.GetValue(instance) is not IDictionary<string, object?> dictionary)
        {
            if (!member.CanSet)
                return;

            dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
            member.SetValue(instance, dictionary);
        }

        if (entries is null)
            return;

        foreach (KeyValuePair<string, object?> entry in entries)
            dictionary[entry.Key] = entry.Value;
    }

    /// <summary>
    /// Determines whether a member is omitted from the output for the supplied value, honoring the member's per-member
    /// <see cref="IgnoreCondition" /> and, in its absence, the serializer-wide null handling.
    /// </summary>
    /// <param name="property">The member metadata.</param>
    /// <param name="value">The member value.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>
    /// <see langword="true" /> when the member should be skipped; otherwise <see langword="false" />.
    /// </returns>
    private static bool ShouldOmit(PropertyMetadata property, object? value, YamlSerializerOptions options) =>
        property.ConditionalIgnore switch
        {
            IgnoreCondition.WhenWritingNull => value is null,
            IgnoreCondition.WhenWritingDefault => value is null || Equals(value, property.DefaultTypeValue),
            IgnoreCondition.Never => false,
            _ => options.IgnoreNullValues && value is null,
        };

    /// <summary>
    /// Constructs the instance using the type's construction plan, invoking the chosen constructor only. Settable
    /// members are assigned in a separate step so that an <see cref="IOnDeserializing" /> callback can run between
    /// construction and member population.
    /// </summary>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="values">The read member values, indexed by member slot.</param>
    /// <param name="present">Whether each member slot was read from the input.</param>
    /// <returns>The constructed instance, before any settable member is assigned.</returns>
    private static object BareConstruct(TypeMetadata metadata, object?[] values, bool[] present)
    {
        if (metadata.UsesParameterizedConstructor)
        {
            object?[] arguments = new object?[metadata.ConstructorParameterCount];
            for (int i = 0; i < arguments.Length; i++)
            {
                PropertyMetadata? parameter = metadata.GetConstructorParameter(i);
                arguments[i] = parameter is not null && present[parameter.SlotIndex]
                    ? values[parameter.SlotIndex]
                    : metadata.GetConstructorDefault(i);
            }

            return metadata.Construct(arguments);
        }

        return metadata.Construct(null);
    }

    /// <summary>
    /// Assigns the read values to the members of a constructed instance: a settable member is assigned through its
    /// setter, and a get-only member merges into the collection it already holds only under
    /// <see cref="ObjectCreationHandling.Populate" />, mirroring the former binding walker.
    /// </summary>
    /// <param name="metadata">The type metadata, used to determine constructor binding and effective handling.</param>
    /// <param name="values">The read member values, indexed by member slot.</param>
    /// <param name="present">Whether each member slot was read from the input.</param>
    /// <param name="instance">The instance to assign on.</param>
    /// <param name="options">The serializer options that supply the default object-creation handling.</param>
    private static void AssignSettableMembers(TypeMetadata metadata, object?[] values, bool[] present, object instance, YamlSerializerOptions options)
    {
        bool skipConstructorBound = metadata.UsesParameterizedConstructor;
        foreach (PropertyMetadata property in metadata.Properties)
        {
            if (!present[property.SlotIndex])
                continue;

            if (skipConstructorBound && property.ConstructorParameterIndex >= 0)
                continue;

            object? value = values[property.SlotIndex];
            if (property.CanSet)
            {
                property.SetValue(instance, value);
                continue;
            }

            ObjectCreationHandling handling = property.CreationHandling ?? metadata.CreationHandling ?? options.PreferredObjectCreationHandling;
            if (handling == ObjectCreationHandling.Populate)
                TryPopulate(property, instance, value);
        }
    }

    /// <summary>
    /// Merges a freshly read sequence into the list instance a get-only member already holds, so a get-only collection
    /// property round-trips under <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    /// <param name="property">The member whose existing value is populated.</param>
    /// <param name="instance">The instance that owns the member.</param>
    /// <param name="bufferedValue">The value read into a new collection for the member.</param>
    private static void TryPopulate(PropertyMetadata property, object instance, object? bufferedValue)
    {
        if (property.GetValue(instance) is not System.Collections.IList existing || bufferedValue is not System.Collections.IEnumerable items || bufferedValue is string)
            return;

        foreach (object? item in items)
            existing.Add(item);
    }
}
