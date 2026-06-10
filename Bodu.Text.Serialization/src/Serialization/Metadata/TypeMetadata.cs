// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TypeMetadata.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Serialization.Metadata;

/// <summary>
/// Describes how a type is mapped to and from an object structure: the serializable members in write order, a name
/// lookup for reading, and the plan used to construct an instance during deserialization.
/// </summary>
/// <remarks>
/// Instances are produced by <see cref="MetadataResolver" /> and cached on the serializer options. A type is built
/// either through a public parameterless constructor followed by member assignment, or through a parameterized
/// constructor (for records and immutable types) whose arguments come from the read members.
/// </remarks>
internal sealed class TypeMetadata
{
    /// <summary>
    /// The serializable members, in write order.
    /// </summary>
    private readonly PropertyMetadata[] _properties;

    /// <summary>
    /// The member lookup used when reading, keyed by wire name with the options' configured case sensitivity.
    /// </summary>
    private readonly Dictionary<string, PropertyMetadata> _byWireName;

    /// <summary>
    /// The constructor invoked during deserialization, or <see langword="null" /> when a parameterless constructor is
    /// used.
    /// </summary>
    private readonly ConstructorInfo? _constructor;

    /// <summary>
    /// The member bound to each constructor parameter by position, or <see langword="null" /> for an unmapped
    /// parameter.
    /// </summary>
    private readonly PropertyMetadata?[] _constructorParameters;

    /// <summary>
    /// The default value supplied for each constructor parameter when its member is absent.
    /// </summary>
    private readonly object?[] _constructorDefaults;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMetadata" /> class.
    /// </summary>
    /// <param name="type">The described type.</param>
    /// <param name="properties">The serializable members, in write order.</param>
    /// <param name="byWireName">The member lookup keyed by wire name.</param>
    /// <param name="constructor">
    /// The constructor used during deserialization, or <see langword="null" /> for a parameterless construction.
    /// </param>
    /// <param name="constructorParameters">The member bound to each constructor parameter by position.</param>
    /// <param name="constructorDefaults">The default value for each constructor parameter.</param>
    internal TypeMetadata(
        Type type,
        PropertyMetadata[] properties,
        Dictionary<string, PropertyMetadata> byWireName,
        ConstructorInfo? constructor,
        PropertyMetadata?[] constructorParameters,
        object?[] constructorDefaults)
    {
        Type = type;
        _properties = properties;
        _byWireName = byWireName;
        _constructor = constructor;
        _constructorParameters = constructorParameters;
        _constructorDefaults = constructorDefaults;
    }

    /// <summary>
    /// Gets the described type.
    /// </summary>
    /// <returns>The type.</returns>
    internal Type Type { get; }

    /// <summary>
    /// Gets the serializable members, in write order.
    /// </summary>
    /// <returns>The ordered members.</returns>
    internal IReadOnlyList<PropertyMetadata> Properties => _properties;

    /// <summary>
    /// Gets a value indicating whether the type is constructed through a parameterized constructor.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a parameterized constructor is used; otherwise <see langword="false" />.
    /// </returns>
    internal bool UsesParameterizedConstructor => _constructor is not null && _constructorParameters.Length > 0;

    /// <summary>
    /// Gets the number of parameters of the deserialization constructor.
    /// </summary>
    /// <returns>The constructor parameter count, or zero when a parameterless construction is used.</returns>
    internal int ConstructorParameterCount => _constructorParameters.Length;

    /// <summary>
    /// Gets a value indicating whether the type can be instantiated during deserialization.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a construction plan is available; otherwise <see langword="false" />.
    /// </returns>
    internal bool CanConstruct => _constructor is not null || Type.IsValueType || HasParameterlessConstructor();

    /// <summary>
    /// Attempts to find the member that an incoming wire name maps to.
    /// </summary>
    /// <param name="wireName">The wire name read from the input.</param>
    /// <param name="property">When this method returns <see langword="true" />, the matching member.</param>
    /// <returns><see langword="true" /> when a member matches; otherwise <see langword="false" />.</returns>
    internal bool TryGetProperty(string wireName, out PropertyMetadata? property) =>
        _byWireName.TryGetValue(wireName, out property);

    /// <summary>
    /// Gets the member bound to the constructor parameter at the specified position.
    /// </summary>
    /// <param name="index">The constructor parameter position.</param>
    /// <returns>The bound member, or <see langword="null" /> when the parameter is unmapped.</returns>
    internal PropertyMetadata? GetConstructorParameter(int index) =>
        _constructorParameters[index];

    /// <summary>
    /// Gets the default value for the constructor parameter at the specified position.
    /// </summary>
    /// <param name="index">The constructor parameter position.</param>
    /// <returns>The default value.</returns>
    internal object? GetConstructorDefault(int index) =>
        _constructorDefaults[index];

    /// <summary>
    /// Constructs an instance of the type using the supplied constructor arguments.
    /// </summary>
    /// <param name="arguments">
    /// The constructor arguments, or <see langword="null" /> for a parameterless construction.
    /// </param>
    /// <returns>The new instance.</returns>
    internal object Construct(object?[]? arguments) =>
        _constructor is not null && _constructorParameters.Length > 0
            ? _constructor.Invoke(arguments)
            : _constructor?.Invoke(null) ?? Activator.CreateInstance(Type) !;

    /// <summary>
    /// Determines whether the type declares a public parameterless constructor.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a public parameterless constructor exists; otherwise <see langword="false" />.
    /// </returns>
    private bool HasParameterlessConstructor() =>
        Type.GetConstructor(Type.EmptyTypes) is not null;
}
