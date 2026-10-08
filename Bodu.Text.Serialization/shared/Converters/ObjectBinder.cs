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
/// <see cref="ObjectCreationHandling.Populate" />, reads the member into the value it already holds: the entries of a
/// collection or dictionary are added to it, and the members of an object are set on it.
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
    /// Determines whether a member is read into an <see cref="ObjectPopulation" />, so that the members read for it are
    /// set on the object it holds rather than on a new instance.
    /// </summary>
    /// <param name="owner">The metadata of the type that declares the member.</param>
    /// <param name="property">The member being read.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>
    /// <see langword="true" /> when the member's effective handling is <see cref="ObjectCreationHandling.Populate" />
    /// and its value is an object the serializer can populate; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The member's converter must read objects (implement <see cref="IPopulatingConverter" />), and the member's type
    /// must be built through a parameterless constructor: a member set only by a parameterized constructor could not be
    /// set on an existing instance. A member bound to a parameter of its owner's constructor is never populated, since
    /// its value is passed to that constructor, and a member of a value type is populated only when it has a setter to
    /// store the populated copy. Every other member is read as usual.
    /// </remarks>
    internal static bool PopulatesObject(TypeMetadata owner, PropertyMetadata property, FormatOptions options)
    {
        ObjectCreationHandling handling = property.CreationHandling ?? owner.CreationHandling ?? options.PreferredObjectCreationHandling;
        if (handling != ObjectCreationHandling.Populate || property.Converter is not IPopulatingConverter)
            return false;

        if (owner.UsesParameterizedConstructor && property.ConstructorParameterIndex >= 0)
            return false;

        if (property.PropertyType.IsValueType && !property.CanSet)
            return false;

        TypeMetadata metadata = options.GetTypeMetadata(property.PropertyType);
        return metadata.CanConstruct && !metadata.UsesParameterizedConstructor;
    }

    /// <summary>
    /// Assigns the read values to the members of an instance, honoring each member's effective object-creation
    /// handling, so that a <see cref="ObjectCreationHandling.Populate" /> member is read into the value it already
    /// holds instead of being replaced.
    /// </summary>
    /// <param name="metadata">The type metadata, used to determine constructor binding and effective handling.</param>
    /// <param name="values">The read member values, indexed by member slot.</param>
    /// <param name="present">Whether each member slot was read from the input.</param>
    /// <param name="instance">The instance to assign on, boxed when the type is a value type.</param>
    /// <param name="preferredHandling">The serializer-wide object-creation handling.</param>
    /// <remarks>
    /// <para>
    /// Members bound to a constructor parameter are skipped when the type is built through a parameterized constructor,
    /// since their values were already supplied to the constructor. For every other member the effective handling is
    /// the member's <see cref="PropertyMetadata.CreationHandling" />, then the type's
    /// <see cref="TypeMetadata.CreationHandling" />, then <paramref name="preferredHandling" />.
    /// </para>
    /// <para>
    /// A member read into an <see cref="ObjectPopulation" /> has its members set on the object it holds; a value of a
    /// value type is populated on the copy the getter returns, which the setter then stores. When the member holds
    /// <see langword="null" />, a new instance is built from the same read members instead. A collection or dictionary
    /// read under <see cref="ObjectCreationHandling.Populate" /> is merged into the one the member holds when that can
    /// grow. In every other case the value is set through the member's setter, and a member without one keeps its
    /// value.
    /// </para>
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
            if (value is ObjectPopulation population)
            {
                object? existing = property.GetValue(instance);
                if (existing is not null)
                {
                    population.Populate(existing);

                    // A value-type member returned a boxed copy, which now holds the read members and must be stored.
                    if (property.PropertyType.IsValueType)
                        property.SetValue(instance, existing);

                    continue;
                }

                value = population.Create();
            }
            else
            {
                ObjectCreationHandling handling = property.CreationHandling ?? metadata.CreationHandling ?? preferredHandling;
                if (handling == ObjectCreationHandling.Populate && TryPopulate(property, instance, value))
                    continue;
            }

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
    /// <see cref="IList" />, or a closed <c>ICollection&lt;T&gt;</c>, that can grow: an array, or any read-only or
    /// fixed-size collection, cannot take the read entries. The buffered value must be enumerable, and not a string. A
    /// dictionary copies key/value pairs; a list or collection adds elements in order. Any other shape returns
    /// <see langword="false" />.
    /// </remarks>
    private static bool TryPopulate(PropertyMetadata property, object instance, object? bufferedValue)
    {
        // A string is enumerable, but a scalar read for a member is never spread into the collection it holds.
        object? existing = property.GetValue(instance);
        if (existing is null || bufferedValue is null || bufferedValue is string)
            return false;

        if (existing is IDictionary existingDictionary && bufferedValue is IDictionary bufferedDictionary)
        {
            if (existingDictionary.IsReadOnly || existingDictionary.IsFixedSize)
                return false;

            foreach (DictionaryEntry pair in bufferedDictionary)
                existingDictionary[pair.Key] = pair.Value;

            return true;
        }

        if (bufferedValue is not IEnumerable bufferedItems)
            return false;

        if (existing is IList existingList)
        {
            if (existingList.IsReadOnly || existingList.IsFixedSize)
                return false;

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
    /// <see langword="true" /> when an <c>ICollection&lt;T&gt;</c> that is not read-only was found and the items were
    /// added; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryPopulateGenericCollection(object existing, IEnumerable bufferedItems)
    {
        Type? collectionInterface = Array.Find(
            existing.GetType().GetInterfaces(),
            static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));

        if (collectionInterface is null || collectionInterface.GetProperty("IsReadOnly")?.GetValue(existing) is true)
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
