// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyMetadata.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Bencode.Serialization.Metadata;

/// <summary>
/// Describes a single serializable member (property or field surfaced as a property) of a type: its wire name, the
/// converter that handles its value, how it is read and written, and how it binds to a constructor parameter.
/// </summary>
/// <remarks>
/// Instances are produced by <see cref="MetadataResolver" /> and cached on the serializer options, so the reflection
/// cost is paid once per type.
/// </remarks>
internal sealed class PropertyMetadata
{
    /// <summary>
    /// The reflected property used to read and write the member value.
    /// </summary>
    private readonly PropertyInfo _property;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMetadata" /> class.
    /// </summary>
    /// <param name="property">The reflected property.</param>
    /// <param name="wireName">The name used for the member in serialized output.</param>
    /// <param name="converter">The converter that handles the member value.</param>
    /// <param name="conditionalIgnore">
    /// The condition under which the member is omitted on write, or <see langword="null" />.
    /// </param>
    /// <param name="order">The relative write order of the member.</param>
    /// <param name="constructorParameterIndex">
    /// The index of the constructor parameter the member binds to, or -1.
    /// </param>
    /// <param name="isRequired">Whether the member must be present when reading.</param>
    /// <param name="defaultValue">
    /// The default value used when the member binds to a constructor parameter that is absent.
    /// </param>
    internal PropertyMetadata(
        PropertyInfo property,
        string wireName,
        BencodeConverter converter,
        BencodeIgnoreCondition? conditionalIgnore,
        int order,
        int constructorParameterIndex,
        bool isRequired,
        object? defaultValue)
    {
        _property = property;
        WireName = wireName;
        Converter = converter;
        ConditionalIgnore = conditionalIgnore;
        Order = order;
        ConstructorParameterIndex = constructorParameterIndex;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        DefaultTypeValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
    }

    /// <summary>
    /// Gets the CLR name of the member.
    /// </summary>
    /// <returns>The declared member name.</returns>
    internal string ClrName => _property.Name;

    /// <summary>
    /// Gets the declared type of the member.
    /// </summary>
    /// <returns>The member type.</returns>
    internal Type PropertyType => _property.PropertyType;

    /// <summary>
    /// Gets the name used for the member in serialized output.
    /// </summary>
    /// <returns>The wire name.</returns>
    internal string WireName { get; }

    /// <summary>
    /// Gets the converter that handles the member value.
    /// </summary>
    /// <returns>The member converter.</returns>
    internal BencodeConverter Converter { get; }

    /// <summary>
    /// Gets the condition under which the member is omitted on write, or <see langword="null" /> when it is always
    /// written.
    /// </summary>
    /// <returns>The conditional-ignore setting, or <see langword="null" />.</returns>
    internal BencodeIgnoreCondition? ConditionalIgnore { get; }

    /// <summary>
    /// Gets the relative write order of the member.
    /// </summary>
    /// <returns>The order value.</returns>
    internal int Order { get; }

    /// <summary>
    /// Gets the index of the constructor parameter the member binds to, or -1 when the member is set through its
    /// setter.
    /// </summary>
    /// <returns>The constructor parameter index, or -1.</returns>
    internal int ConstructorParameterIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the member must be present in the input.
    /// </summary>
    /// <returns><see langword="true" /> when the member is required; otherwise <see langword="false" />.</returns>
    internal bool IsRequired { get; }

    /// <summary>
    /// Gets the default value supplied for the member's constructor parameter when the member is absent from the input.
    /// </summary>
    /// <returns>The default value, or <see langword="null" />.</returns>
    internal object? DefaultValue { get; }

    /// <summary>
    /// Gets the default value of the member's type, used to evaluate
    /// <see cref="BencodeIgnoreCondition.WhenWritingDefault" />.
    /// </summary>
    /// <returns>The boxed default value of the member type; <see langword="null" /> for reference types.</returns>
    internal object? DefaultTypeValue { get; }

    /// <summary>
    /// Gets a value indicating whether the member can be assigned through its setter.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the member has a setter (including an init-only setter); otherwise
    /// <see langword="false" />.
    /// </returns>
    internal bool CanSet => _property.SetMethod is not null;

    /// <summary>
    /// Reads the member value from the specified target.
    /// </summary>
    /// <param name="target">The object to read from.</param>
    /// <returns>The member value.</returns>
    internal object? GetValue(object target) =>
        _property.GetValue(target);

    /// <summary>
    /// Assigns the member value on the specified target through its setter.
    /// </summary>
    /// <param name="target">The object to assign on.</param>
    /// <param name="value">The value to assign.</param>
    internal void SetValue(object target, object? value) =>
        _property.SetValue(target, value);
}
