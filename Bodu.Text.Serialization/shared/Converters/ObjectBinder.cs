// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectBinder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Reflection;
using Bodu.Text.Serialization;
#if BENCODE
using Bodu.Text.Bencode.Serialization.Metadata;
#elif TOML
using Bodu.Text.Toml.Serialization.Metadata;
#elif YAML
using Bodu.Text.Yaml.Serialization.Metadata;
#endif

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Binds the members an object converter has read to an instance: it constructs the instance through the type's
/// construction plan, then assigns each read member through its setter or, under
/// <see cref="ObjectCreationHandling.Populate" />, adds the read entries into the value the member already holds.
/// </summary>
/// <remarks>
/// The object converters of every format share these steps, so they apply one object-creation rule; each converter
/// keeps its own reading, extension data, and error reporting.
/// </remarks>
internal static class ObjectBinder
{
    /// <summary>
    /// Constructs an instance using the type's construction plan, invoking the chosen constructor only. Settable
    /// members are assigned in a separate step, so that an <see cref="IOnDeserializing" /> callback can run between
    /// construction and member assignment.
    /// </summary>
    /// <param name="metadata">The type metadata.</param>
    /// <param name="values">The read member values, indexed by member slot.</param>
    /// <param name="present">Whether each member slot was read from the input.</param>
    /// <returns>The constructed instance, boxed when the type is a value type, before any member is assigned.</returns>
    /// <remarks>
    /// For a parameterized constructor the bound arguments are gathered from <paramref name="values" /> (falling back
    /// to each parameter's default), so an <see cref="IOnDeserializing" /> callback necessarily observes those
    /// arguments already applied; for a parameterless constructor the instance is created empty.
    /// </remarks>
    internal static object Construct(TypeMetadata metadata, object?[] values, bool[] present)
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
    /// Assigns the read values to the members of a constructed instance, honoring each member's effective
    /// object-creation handling so that a <see cref="ObjectCreationHandling.Populate" /> member merges its read entries
    /// into the existing collection or dictionary instead of replacing it.
    /// </summary>
    /// <param name="metadata">The type metadata, used to determine constructor binding and effective handling.</param>
    /// <param name="values">The read member values, indexed by member slot.</param>
    /// <param name="present">Whether each member slot was read from the input.</param>
    /// <param name="instance">The instance to assign on, boxed when the type is a value type.</param>
    /// <param name="preferredHandling">The serializer-wide object-creation handling.</param>
    /// <remarks>
    /// Members bound to a constructor parameter are skipped when the type is built through a parameterized constructor,
    /// since their values were already supplied to the constructor. For every other member the effective handling is
    /// the member's <see cref="PropertyMetadata.CreationHandling" />, then the type's
    /// <see cref="TypeMetadata.CreationHandling" />, then <paramref name="preferredHandling" />;
    /// <see cref="ObjectCreationHandling.Populate" /> is applied only when the member already holds a populatable
    /// collection or dictionary, otherwise the value is set through the member's setter.
    /// </remarks>
    internal static void AssignMembers(TypeMetadata metadata, object?[] values, bool[] present, object instance, ObjectCreationHandling preferredHandling)
    {
        bool skipConstructorBound = metadata.UsesParameterizedConstructor;
        foreach (PropertyMetadata property in metadata.Properties)
        {
            if (!present[property.SlotIndex])
                continue;

            if (skipConstructorBound && property.ConstructorParameterIndex >= 0)
                continue;

            object? value = values[property.SlotIndex];
            ObjectCreationHandling handling = property.CreationHandling ?? metadata.CreationHandling ?? preferredHandling;
            if (handling == ObjectCreationHandling.Populate && TryPopulate(property, instance, value))
                continue;

            if (property.CanSet)
                property.SetValue(instance, value);
        }
    }

    /// <summary>
    /// Attempts to merge a freshly read collection or dictionary value into the instance already held by a member,
    /// rather than replacing it. This lets a get-only collection or dictionary property round-trip under
    /// <see cref="ObjectCreationHandling.Populate" />.
    /// </summary>
    /// <param name="property">The member whose existing value is populated.</param>
    /// <param name="instance">The instance that owns the member.</param>
    /// <param name="bufferedValue">The value read into a new collection or dictionary for the member.</param>
    /// <returns>
    /// <see langword="true" /> when the member's existing value was populated from <paramref name="bufferedValue" />;
    /// otherwise <see langword="false" />, indicating the caller should set the value normally.
    /// </returns>
    /// <remarks>
    /// The existing value must be non-<see langword="null" /> and shaped as a non-generic <see cref="IDictionary" /> or
    /// <see cref="IList" />, or a closed <c>ICollection&lt;T&gt;</c>, and the buffered value must be enumerable. A
    /// dictionary copies key/value pairs; a list or collection adds elements in order. Any other shape returns
    /// <see langword="false" />.
    /// </remarks>
    private static bool TryPopulate(PropertyMetadata property, object instance, object? bufferedValue)
    {
        object? existing = property.GetValue(instance);
        if (existing is null || bufferedValue is null)
            return false;

        if (existing is IDictionary existingDictionary && bufferedValue is IDictionary bufferedDictionary)
        {
            foreach (DictionaryEntry pair in bufferedDictionary)
                existingDictionary[pair.Key] = pair.Value;

            return true;
        }

        if (bufferedValue is not IEnumerable bufferedItems)
            return false;

        if (existing is IList existingList)
        {
            foreach (object? item in bufferedItems)
                existingList.Add(item);

            return true;
        }

        return TryPopulateGenericCollection(existing, bufferedItems);
    }

    /// <summary>
    /// Attempts to add the buffered items into an existing value that implements a closed <c>ICollection&lt;T&gt;</c>
    /// but is not a non-generic <see cref="IList" />, invoking the interface's <c>Add</c> method through reflection.
    /// </summary>
    /// <param name="existing">The existing collection instance.</param>
    /// <param name="bufferedItems">The items read for the member.</param>
    /// <returns>
    /// <see langword="true" /> when an <c>ICollection&lt;T&gt;</c> was found and the items were added; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryPopulateGenericCollection(object existing, IEnumerable bufferedItems)
    {
        Type? collectionInterface = Array.Find(
            existing.GetType().GetInterfaces(),
            static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

        if (collectionInterface is null)
            return false;

        MethodInfo? add = collectionInterface.GetMethod("Add");
        if (add is null)
            return false;

        object?[] argument = new object?[1];
        foreach (object? item in bufferedItems)
        {
            argument[0] = item;
            add.Invoke(existing, argument);
        }

        return true;
    }
}
