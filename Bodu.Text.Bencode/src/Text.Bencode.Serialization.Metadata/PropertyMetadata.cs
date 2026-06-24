// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyMetadata.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Bencode.Serialization.Metadata;

/// <summary>
/// Describes a single serializable member (a property, or a public field surfaced through
/// <see cref="BencodeSerializerOptions.IncludeFields" /> or <see cref="BencodeIncludeAttribute" />) of a type: its wire
/// name, the converter that handles its value, how it is read and written, and how it binds to a constructor parameter.
/// </summary>
/// <remarks>
/// Instances are produced by <see cref="MetadataResolver" /> and cached on the serializer options, so the reflection
/// cost is paid once per type.
/// </remarks>
internal sealed class PropertyMetadata
{
    /// <summary>The reflected property used to read and write the member value, or <see langword="null" /> when the member is a field.</summary>
    private readonly PropertyInfo? _property;

    /// <summary>The reflected field used to read and write the member value, or <see langword="null" /> when the member is a property.</summary>
    private readonly FieldInfo? _field;

    /// <summary>Whether the member is opted into binding through non-public accessors by <see cref="BencodeIncludeAttribute" />.</summary>
    private readonly bool _included;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMetadata" /> class.
    /// </summary>
    /// <param name="member">The reflected member, a <see cref="PropertyInfo" /> or a <see cref="FieldInfo" />.</param>
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
    /// <param name="included">
    /// Whether the member is opted into binding through non-public accessors by <see cref="BencodeIncludeAttribute" />.
    /// </param>
    internal PropertyMetadata(
        MemberInfo member,
        string wireName,
        BencodeConverter converter,
        BencodeIgnoreCondition? conditionalIgnore,
        int order,
        int constructorParameterIndex,
        bool isRequired,
        object? defaultValue,
        bool included)
    {
        _property = member as PropertyInfo;
        _field = member as FieldInfo;
        _included = included;
        WireName = wireName;
        Converter = converter;
        ConditionalIgnore = conditionalIgnore;
        Order = order;
        ConstructorParameterIndex = constructorParameterIndex;
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        DefaultTypeValue = PropertyType.IsValueType ? Activator.CreateInstance(PropertyType) : null;
    }

    /// <summary>
    /// Gets the CLR name of the member.
    /// </summary>
    /// <value>The declared member name.</value>
    internal string ClrName => _property?.Name ?? _field!.Name;

    /// <summary>
    /// Gets the declared type of the member.
    /// </summary>
    /// <value>The member type.</value>
    internal Type PropertyType => _property?.PropertyType ?? _field!.FieldType;

    /// <summary>
    /// Gets the name used for the member in serialized output.
    /// </summary>
    /// <value>The wire name.</value>
    internal string WireName { get; }

    /// <summary>
    /// Gets the converter that handles the member value.
    /// </summary>
    /// <value>The member converter.</value>
    internal BencodeConverter Converter { get; }

    /// <summary>
    /// Gets the condition under which the member is omitted on write, or <see langword="null" /> when it is always
    /// written.
    /// </summary>
    /// <value>The conditional-ignore setting, or <see langword="null" />.</value>
    internal BencodeIgnoreCondition? ConditionalIgnore { get; }

    /// <summary>
    /// Gets the member-level object-creation handling, sourced from a
    /// <see cref="BencodeObjectCreationHandlingAttribute" /> on the member, or <see langword="null" /> when the member
    /// declares none.
    /// </summary>
    /// <value>The member-level object-creation handling, or <see langword="null" />.</value>
    internal BencodeObjectCreationHandling? CreationHandling { get; init; }

    /// <summary>
    /// Gets the relative write order of the member.
    /// </summary>
    /// <value>The order value.</value>
    internal int Order { get; }

    /// <summary>
    /// Gets the index of the constructor parameter the member binds to, or -1 when the member is set through its
    /// setter.
    /// </summary>
    /// <value>The constructor parameter index, or -1.</value>
    internal int ConstructorParameterIndex { get; }

    /// <summary>
    /// Gets a value indicating whether the member must be present in the input.
    /// </summary>
    /// <value><see langword="true" /> when the member is required; otherwise <see langword="false" />.</value>
    internal bool IsRequired { get; }

    /// <summary>
    /// Gets the default value supplied for the member's constructor parameter when the member is absent from the input.
    /// </summary>
    /// <value>The default value, or <see langword="null" />.</value>
    internal object? DefaultValue { get; }

    /// <summary>
    /// Gets the default value of the member's type, used to evaluate
    /// <see cref="BencodeIgnoreCondition.WhenWritingDefault" />.
    /// </summary>
    /// <value>The boxed default value of the member type; <see langword="null" /> for reference types.</value>
    internal object? DefaultTypeValue { get; }

    /// <summary>
    /// Gets a value indicating whether the member can be assigned during deserialization.
    /// </summary>
    /// <value>
    /// For a property, <see langword="true" /> when it has a public setter (which includes an init-only setter) or a
    /// non-public setter opted in by <see cref="BencodeIncludeAttribute" />; a property exposed only through a
    /// non-public setter is therefore not assigned on read unless it carries that attribute. For a field,
    /// <see langword="true" /> unless the field is <see langword="readonly" />.
    /// </value>
    internal bool CanSet =>
        _property is not null
            ? _property.SetMethod is not null && (_property.SetMethod.IsPublic || _included)
            : !_field!.IsInitOnly;

    /// <summary>
    /// Reads the member value from the specified target.
    /// </summary>
    /// <param name="target">The object to read from.</param>
    /// <returns>The member value.</returns>
    internal object? GetValue(object target) =>
        _property is not null ? _property.GetValue(target) : _field!.GetValue(target);

    /// <summary>
    /// Assigns the member value on the specified target.
    /// </summary>
    /// <param name="target">The object to assign on.</param>
    /// <param name="value">The value to assign.</param>
    internal void SetValue(object target, object? value)
    {
        if (_property is not null)
            _property.SetValue(target, value);
        else
            _field!.SetValue(target, value);
    }
}
