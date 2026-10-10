// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Common reflection descriptor for the simple POCO record serializers.
/// </summary>
/// <remarks>
/// Discovery and format-specific mapping remain owned by the caller.
/// </remarks>
internal class FlatMemberDescriptor
{
    /// <summary>The underlying property, when the member is a property.</summary>
    private readonly PropertyInfo? _property;

    /// <summary>The underlying field, when the member is a field.</summary>
    private readonly FieldInfo? _field;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlatMemberDescriptor" /> class for a reflected property or field.
    /// </summary>
    /// <param name="member">The reflected property or field.</param>
    /// <param name="convertName">The member-naming policy.</param>
    protected FlatMemberDescriptor(MemberInfo member, Func<string, string> convertName)
    {
        _property = member as PropertyInfo;
        _field = member as FieldInfo;

        Name = member.GetCustomAttribute<PropertyNameAttribute>()?.Name ?? convertName(member.Name);
        MemberType = _property?.PropertyType ?? _field!.FieldType;
        CanRead = _property is null || _property.GetMethod is { IsPublic: true };
        CanWrite = _property is not null ? _property.SetMethod is { IsPublic: true } : !_field!.IsInitOnly;
        Required = member.GetCustomAttribute<RequiredAttribute>() is not null;
        IgnoreCondition? condition = member.GetCustomAttribute<IgnoreAttribute>()?.Condition;
        IgnoreCondition = condition == Bodu.Text.Serialization.IgnoreCondition.Always ? null : condition;
        Order = member.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0;
    }

    /// <summary>
    /// Gets the resolved serialized name of the member.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the declared type of the member.
    /// </summary>
    public Type MemberType { get; }

    /// <summary>
    /// Gets a value indicating whether the member is publicly readable.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Gets a value indicating whether the member is publicly writable.
    /// </summary>
    public bool CanWrite { get; }

    /// <summary>
    /// Gets a value indicating whether the member is required.
    /// </summary>
    public bool Required { get; }

    /// <summary>
    /// Gets the member's conditional omission rule, when applicable.
    /// </summary>
    public IgnoreCondition? IgnoreCondition { get; }

    /// <summary>
    /// Gets the serialized ordering hint.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Gets the reflected member's value from an object instance.
    /// </summary>
    /// <param name="instance">The instance containing the member.</param>
    /// <returns>The member's current value.</returns>
    public object? GetValue(object instance) => _property is not null ? _property.GetValue(instance) : _field!.GetValue(instance);

    /// <summary>
    /// Assigns the reflected member's value on an object instance.
    /// </summary>
    /// <param name="instance">The instance containing the member.</param>
    /// <param name="value">The value to assign.</param>
    public void SetValue(object instance, object? value)
    {
        if (_property is not null)
            _property.SetValue(instance, value);
        else
            _field!.SetValue(instance, value);
    }
}
