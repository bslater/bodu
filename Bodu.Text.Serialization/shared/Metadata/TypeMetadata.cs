// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TypeMetadata.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Linq.Expressions;
using System.Reflection;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Metadata;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Metadata;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Metadata;
#endif

/// <summary>
/// Describes how a type is mapped to and from the format's keyed structure: the serializable members in write order, a
/// name lookup for reading, and the plan used to construct an instance during deserialization.
/// </summary>
/// <remarks>
/// Instances are produced by <see cref="MetadataResolver" /> and cached on the serializer options. A type is built
/// either through a public parameterless constructor followed by member assignment, or through a parameterized
/// constructor (for records and immutable types) whose arguments come from the read members.
/// </remarks>
internal sealed class TypeMetadata
{
    /// <summary>The serializable members, in write order.</summary>
    private readonly PropertyMetadata[] _properties;

    /// <summary>The member lookup used when reading, keyed by wire name with the options' configured case sensitivity.</summary>
    private readonly Dictionary<string, PropertyMetadata> _byWireName;

    /// <summary>The constructor invoked during deserialization, or <see langword="null" /> when a parameterless constructor is used.</summary>
    private readonly ConstructorInfo? _constructor;

    /// <summary>The member bound to each constructor parameter by position, or <see langword="null" /> for an unmapped parameter.</summary>
    private readonly PropertyMetadata?[] _constructorParameters;

    /// <summary>The default value supplied for each constructor parameter when its member is absent.</summary>
    private readonly object?[] _constructorDefaults;

    /// <summary>The compiled construction plan, or <see langword="null" /> when the type cannot be constructed.</summary>
    private readonly Func<object?[]?, object>? _factory;

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
    /// <param name="extensionData">
    /// The member that captures unmatched entries, or <see langword="null" /> when the type declares none.
    /// </param>
    internal TypeMetadata(
        Type type,
        PropertyMetadata[] properties,
        Dictionary<string, PropertyMetadata> byWireName,
        ConstructorInfo? constructor,
        PropertyMetadata?[] constructorParameters,
        object?[] constructorDefaults,
        PropertyMetadata? extensionData)
    {
        Type = type;
        _properties = properties;
        _byWireName = byWireName;
        _constructor = constructor;
        _constructorParameters = constructorParameters;
        _constructorDefaults = constructorDefaults;
        ExtensionData = extensionData;
        _factory = BuildFactory();

        for (int i = 0; i < properties.Length; i++)
            properties[i].SlotIndex = i;
    }

    /// <summary>
    /// Gets the described type.
    /// </summary>
    /// <value>The type.</value>
    internal Type Type { get; }

    /// <summary>
    /// Gets the serializable members, in write order.
    /// </summary>
    /// <value>The ordered members.</value>
    internal IReadOnlyList<PropertyMetadata> Properties => _properties;

    /// <summary>
    /// Gets the number of serializable members, which is also the length a slot-indexed read buffer requires.
    /// </summary>
    /// <value>The member count.</value>
    internal int PropertyCount => _properties.Length;

    /// <summary>
    /// Gets the member that captures entries with no matching property, or <see langword="null" /> when the type
    /// declares none.
    /// </summary>
    /// <value>The extension-data member, or <see langword="null" />.</value>
    internal PropertyMetadata? ExtensionData { get; }

    /// <summary>
    /// Gets the type-level handling for a key that maps to no member, sourced from a
    /// <see cref="UnmappedMemberHandlingAttribute" /> on the type, or <see langword="null" /> when the type declares
    /// none.
    /// </summary>
    /// <value>The type-level unmapped-member handling, or <see langword="null" />.</value>
    internal UnmappedMemberHandling? UnmappedMemberHandling { get; init; }

    /// <summary>
    /// Gets the type-level object-creation handling, sourced from a <see cref="ObjectCreationHandlingAttribute" /> on
    /// the type, or <see langword="null" /> when the type declares none.
    /// </summary>
    /// <value>The type-level object-creation handling, or <see langword="null" />.</value>
    internal ObjectCreationHandling? CreationHandling { get; init; }

    /// <summary>
    /// Gets a value indicating whether the type is constructed through a parameterized constructor.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when a parameterized constructor is used; otherwise <see langword="false" />.
    /// </value>
    internal bool UsesParameterizedConstructor => _constructor is not null && _constructorParameters.Length > 0;

    /// <summary>
    /// Gets the number of parameters of the deserialization constructor.
    /// </summary>
    /// <value>The constructor parameter count, or zero when a parameterless construction is used.</value>
    internal int ConstructorParameterCount => _constructorParameters.Length;

    /// <summary>
    /// Gets a value indicating whether the type can be instantiated during deserialization.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when a construction plan is available; otherwise <see langword="false" />.
    /// </value>
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
        _factory is not null ? _factory(arguments) : Activator.CreateInstance(Type)!;

    /// <summary>
    /// Determines whether the type declares a public parameterless constructor.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a public parameterless constructor exists; otherwise <see langword="false" />.
    /// </returns>
    private bool HasParameterlessConstructor() =>
        Type.GetConstructor(Type.EmptyTypes) is not null;

    /// <summary>
    /// Compiles the construction plan, replacing per-instance <see cref="ConstructorInfo.Invoke(object?[])" /> and
    /// <see cref="Activator.CreateInstance(Type)" /> on the deserialization hot path.
    /// </summary>
    /// <returns>
    /// A delegate constructing an instance from a boxed argument array, or <see langword="null" /> when the type has
    /// no construction plan (the <see cref="CanConstruct" /> guard rejects such types before construction).
    /// </returns>
    /// <remarks>
    /// A <see langword="null" /> argument assigns the parameter type's default for a non-nullable value-type
    /// parameter, matching the documented coercion of reflection invocation. Compiled expression trees fall back to
    /// the expression interpreter where runtime code generation is unavailable.
    /// </remarks>
    private Func<object?[]?, object>? BuildFactory()
    {
        ParameterExpression args = Expression.Parameter(typeof(object?[]), "arguments");

        if (_constructor is not null && _constructorParameters.Length > 0)
        {
            ParameterInfo[] parameters = _constructor.GetParameters();
            var arguments = new Expression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Expression item = Expression.ArrayIndex(args, Expression.Constant(i));
                Type parameterType = parameters[i].ParameterType;
                arguments[i] = parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null
                    ? Expression.Condition(
                        Expression.ReferenceEqual(item, Expression.Constant(null)),
                        Expression.Default(parameterType),
                        Expression.Convert(item, parameterType))
                    : Expression.Convert(item, parameterType);
            }

            return Expression.Lambda<Func<object?[]?, object>>(
                Expression.Convert(Expression.New(_constructor, arguments), typeof(object)), args).Compile();
        }

        // Prefer the declared parameterless constructor so a struct that declares one has it invoked (Expression.New
        // over the bare value type produces default(T) without calling it, unlike Activator.CreateInstance).
        ConstructorInfo? parameterless = Type.GetConstructor(Type.EmptyTypes);
        NewExpression? construct = _constructor is not null
            ? Expression.New(_constructor)
            : parameterless is not null ? Expression.New(parameterless)
            : Type.IsValueType ? Expression.New(Type)
            : null;

        return construct is null
            ? null
            : Expression.Lambda<Func<object?[]?, object>>(Expression.Convert(construct, typeof(object)), args).Compile();
    }
}
