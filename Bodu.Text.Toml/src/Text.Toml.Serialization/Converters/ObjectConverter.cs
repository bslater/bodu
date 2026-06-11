// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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
    public override T Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
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

        Dictionary<PropertyMetadata, object?> values = [];
        Dictionary<string, TomlNode?>? extensionEntries = null;
        while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
        {
            var name = reader.GetString();
            reader.Read();

            if (metadata.TryGetProperty(name, out PropertyMetadata? property) && property is not null)
            {
                if (!values.TryAdd(property, property.Converter.ReadAsObject(ref reader, property.PropertyType, options)))
                {
                    throw new TomlSerializationException(
                        string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_DuplicateProperty, name));
                }
            }
            else if (metadata.ExtensionData is not null)
            {
                extensionEntries ??= new Dictionary<string, TomlNode?>(StringComparer.Ordinal);
                extensionEntries[name] = TomlNode.ReadFrom(ref reader);
            }
            else if ((metadata.UnmappedMemberHandling ?? options.UnmappedMemberHandling) == TomlUnmappedMemberHandling.Disallow)
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
            if (property.IsRequired && !values.ContainsKey(property))
            {
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_MissingRequiredMember, property.WireName, typeof(T)));
            }
        }

        var instance = (T)BareConstruct(metadata, values);
        (instance as ITomlOnDeserializing)?.OnDeserializing();
        AssignSettableMembers(metadata, values, instance!, options);
        PopulateExtensionData(metadata, instance, extensionEntries);
        (instance as ITomlOnDeserialized)?.OnDeserialized();
        return instance;
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, T value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (value is null)
            return;

        (value as ITomlOnSerializing)?.OnSerializing();

        TypeMetadata metadata = options.GetTypeMetadata(typeof(T));

        // Track emitted keys only when extension data may follow, so a colliding overflow entry is rejected as a
        // serialization error rather than surfacing as a writer-level duplicate-key failure.
        HashSet<string>? emittedKeys = metadata.ExtensionData is null ? null : new HashSet<string>(StringComparer.Ordinal);

        writer.WriteStartTable();
        foreach (PropertyMetadata property in metadata.Properties)
        {
            object? memberValue = property.GetValue(value);
            if (ShouldSkip(property, memberValue, options))
                continue;

            writer.WritePropertyName(property.WireName);
            property.Converter.WriteAsObject(writer, memberValue, options);
            _ = emittedKeys?.Add(property.WireName);
        }

        WriteExtensionData(writer, metadata, value, emittedKeys);

        writer.WriteEndTable();

        (value as ITomlOnSerialized)?.OnSerialized();
    }

    /// <summary>
    /// Writes the entries held by the type's extension-data member, when one is declared and populated.
    /// </summary>
    /// <param name="writer">The destination writer, positioned inside the open table.</param>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="value">The instance being written.</param>
    /// <param name="emittedKeys">The wire names already written for declared members, or <see langword="null" />.</param>
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
    private static void PopulateExtensionData(TypeMetadata metadata, T instance, Dictionary<string, TomlNode?>? entries)
    {
        if (entries is null || entries.Count == 0 || metadata.ExtensionData is not { } member)
            return;

        if (member.CanSet)
        {
            object materialized = member.PropertyType == typeof(TomlObject)
                ? new TomlObject(entries)
                : entries;
            member.SetValue(instance!, materialized);
            return;
        }

        if (member.GetValue(instance!) is IDictionary<string, TomlNode?> existing)
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
    /// <see cref="TomlIgnoreCondition.WhenWritingDefault" /> and the value equals the member's default-type value.
    /// </remarks>
    private static bool ShouldSkip(PropertyMetadata property, object? value, TomlSerializerOptions options)
    {
        if (value is null)
            return true;

        TomlIgnoreCondition effective = property.ConditionalIgnore ?? options.DefaultIgnoreCondition;
        return effective == TomlIgnoreCondition.WhenWritingDefault && Equals(value, property.DefaultTypeValue);
    }

    /// <summary>
    /// Constructs the instance using the type's construction plan, invoking the chosen constructor only. Settable
    /// members are assigned in a separate step so that an <see cref="ITomlOnDeserializing" /> callback can run between
    /// construction and member population.
    /// </summary>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="values">The read member values, used to bind constructor arguments.</param>
    /// <returns>The constructed instance, before any settable member is assigned.</returns>
    /// <remarks>
    /// For a parameterized constructor the bound arguments are gathered from <paramref name="values" /> (falling back
    /// to each parameter's default), so an <see cref="ITomlOnDeserializing" /> callback necessarily observes those
    /// arguments already applied; for a parameterless constructor the instance is created empty.
    /// </remarks>
    private static object BareConstruct(TypeMetadata metadata, Dictionary<PropertyMetadata, object?> values)
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

            return metadata.Construct(arguments);
        }

        return metadata.Construct(null);
    }

    /// <summary>
    /// Assigns the read values to the settable members of a constructed instance, honoring each member's effective
    /// object-creation handling so that a <see cref="TomlObjectCreationHandling.Populate" /> member merges its read
    /// entries into the existing collection or dictionary instead of replacing it.
    /// </summary>
    /// <param name="metadata">The type metadata, used to determine constructor binding and effective handling.</param>
    /// <param name="values">The read member values.</param>
    /// <param name="instance">The instance to assign on.</param>
    /// <param name="options">The serializer options that supply the default object-creation handling.</param>
    /// <remarks>
    /// Members bound to a constructor parameter are skipped when the type is built through a parameterized constructor,
    /// since their values were already supplied to the constructor. For every other member the effective handling is
    /// the member's <see cref="PropertyMetadata.CreationHandling" />, then the type's
    /// <see cref="TypeMetadata.CreationHandling" />, then
    /// <see cref="TomlSerializerOptions.PreferredObjectCreationHandling" />;
    /// <see cref="TomlObjectCreationHandling.Populate" /> is applied only when the member already holds a populatable
    /// collection or dictionary, otherwise the value is set through the member's setter.
    /// </remarks>
    private static void AssignSettableMembers(TypeMetadata metadata, Dictionary<PropertyMetadata, object?> values, object instance, TomlSerializerOptions options)
    {
        var skipConstructorBound = metadata.UsesParameterizedConstructor;
        foreach (KeyValuePair<PropertyMetadata, object?> entry in values)
        {
            PropertyMetadata property = entry.Key;
            if (skipConstructorBound && property.ConstructorParameterIndex >= 0)
                continue;

            TomlObjectCreationHandling handling = property.CreationHandling ?? metadata.CreationHandling ?? options.PreferredObjectCreationHandling;
            if (handling == TomlObjectCreationHandling.Populate && TryPopulate(property, instance, entry.Value))
                continue;

            if (property.CanSet)
                property.SetValue(instance, entry.Value);
        }
    }

    /// <summary>
    /// Attempts to merge a freshly read collection or dictionary value into the instance already held by a member,
    /// rather than replacing it. This lets a get-only collection or dictionary property round-trip under
    /// <see cref="TomlObjectCreationHandling.Populate" />.
    /// </summary>
    /// <param name="property">The member whose existing value is populated.</param>
    /// <param name="instance">The instance that owns the member.</param>
    /// <param name="bufferedValue">The value read into a new collection or dictionary for the member.</param>
    /// <returns>
    /// <see langword="true" /> when the member's existing value was populated from <paramref name="bufferedValue" />;
    /// otherwise <see langword="false" />, indicating the caller should set the value normally.
    /// </returns>
    /// <remarks>
    /// The existing value must be non-<see langword="null" /> and shaped as a non-generic
    /// <see cref="System.Collections.IDictionary" /> or <see cref="System.Collections.IList" />, or a closed
    /// <c>ICollection&lt;T&gt;</c>, and the buffered value must be enumerable. A dictionary copies key/value pairs; a
    /// list or collection adds elements in order. Any other shape returns <see langword="false" />.
    /// </remarks>
    private static bool TryPopulate(PropertyMetadata property, object instance, object? bufferedValue)
    {
        object? existing = property.GetValue(instance);
        if (existing is null || bufferedValue is null)
            return false;

        if (existing is System.Collections.IDictionary existingDictionary && bufferedValue is System.Collections.IDictionary bufferedDictionary)
        {
            foreach (System.Collections.DictionaryEntry pair in bufferedDictionary)
                existingDictionary[pair.Key] = pair.Value;

            return true;
        }

        if (bufferedValue is not System.Collections.IEnumerable bufferedItems)
            return false;

        if (existing is System.Collections.IList existingList)
        {
            foreach (object? item in bufferedItems)
                existingList.Add(item);

            return true;
        }

        return TryPopulateGenericCollection(existing, bufferedItems);
    }

    /// <summary>
    /// Attempts to add the buffered items into an existing value that implements a closed <c>ICollection&lt;T&gt;</c>
    /// but is not a non-generic <see cref="System.Collections.IList" />, invoking the interface's <c>Add</c> method
    /// through reflection.
    /// </summary>
    /// <param name="existing">The existing collection instance.</param>
    /// <param name="bufferedItems">The items read for the member.</param>
    /// <returns>
    /// <see langword="true" /> when an <c>ICollection&lt;T&gt;</c> was found and the items were added; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryPopulateGenericCollection(object existing, System.Collections.IEnumerable bufferedItems)
    {
        Type? collectionInterface = Array.Find(
            existing.GetType().GetInterfaces(),
            static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

        if (collectionInterface is null)
            return false;

        System.Reflection.MethodInfo? add = collectionInterface.GetMethod("Add");
        if (add is null)
            return false;

        var argument = new object?[1];
        foreach (object? item in bufferedItems)
        {
            argument[0] = item;
            add.Invoke(existing, argument);
        }

        return true;
    }
}
