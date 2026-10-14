// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bodu.Text.Serialization;

/// <summary>
/// Describes one property or field that a line-format serializer maps: its serialized name, its type and the scalar it
/// converts as, the attributes that shape it, and access to its value.
/// </summary>
/// <remarks>
/// A descriptor is immutable once created, so <see cref="FlatMemberCache" /> shares it between calls and threads.
/// </remarks>
internal sealed class FlatMemberDescriptor
{
    /// <summary>The underlying property, when the member is a property.</summary>
    private readonly PropertyInfo? _property;

    /// <summary>The underlying field, when the member is a field.</summary>
    private readonly FieldInfo? _field;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlatMemberDescriptor" /> class for a reflected property or field.
    /// </summary>
    /// <param name="member">The reflected property or field.</param>
    /// <param name="namingPolicy">
    /// The naming policy that converts the member's name, or <see langword="null" /> to keep it.
    /// </param>
    /// <remarks>
    /// A <see cref="PropertyNameAttribute" /> names the member as it is, before and instead of the naming policy.
    /// </remarks>
    internal FlatMemberDescriptor(MemberInfo member, NamingPolicy? namingPolicy)
    {
        _property = member as PropertyInfo;
        _field = member as FieldInfo;

        MemberName = member.Name;
        Name = member.GetCustomAttribute<PropertyNameAttribute>()?.Name ?? namingPolicy?.ConvertName(member.Name) ?? member.Name;
        MemberType = _property?.PropertyType ?? _field!.FieldType;
        ScalarType = Nullable.GetUnderlyingType(MemberType) ?? MemberType;
        IsNullable = ScalarType != MemberType;
        ScalarKind = InvariantScalarCodec.Classify(ScalarType);
        CanRead = _property is null || _property.GetMethod is { IsPublic: true };
        CanWrite = _property is not null ? _property.SetMethod is { IsPublic: true } : !_field!.IsInitOnly;
        Required = member.GetCustomAttribute<RequiredAttribute>() is not null
            || member.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
        IgnoreCondition? condition = member.GetCustomAttribute<IgnoreAttribute>()?.Condition;
        IgnoreCondition = condition == Bodu.Text.Serialization.IgnoreCondition.Always ? null : condition;
        Order = member.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0;
    }

    /// <summary>
    /// Gets the name the member is declared with, before any <see cref="PropertyNameAttribute" /> or naming policy.
    /// </summary>
    public string MemberName { get; }

    /// <summary>
    /// Gets the resolved serialized name of the member.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the declared type of the member.
    /// </summary>
    public Type MemberType { get; }

    /// <summary>
    /// Gets the declared type of the member, or the type it makes nullable when it is a <see cref="Nullable{T}" />.
    /// </summary>
    public Type ScalarType { get; }

    /// <summary>
    /// Gets a value indicating whether the declared type of the member is a <see cref="Nullable{T}" />.
    /// </summary>
    public bool IsNullable { get; }

    /// <summary>
    /// Gets the scalar the member's value converts as, or <see cref="FlatScalarKind.None" /> when its type is not a
    /// scalar.
    /// </summary>
    public FlatScalarKind ScalarKind { get; }

    /// <summary>
    /// Gets a value indicating whether the member's type is a scalar, which maps to one value rather than to members.
    /// </summary>
    public bool IsScalar => ScalarKind != FlatScalarKind.None;

    /// <summary>
    /// Gets a value indicating whether the member is publicly readable.
    /// </summary>
    public bool CanRead { get; }

    /// <summary>
    /// Gets a value indicating whether the member is publicly writable.
    /// </summary>
    public bool CanWrite { get; }

    /// <summary>
    /// Gets a value indicating whether the member is required: marked with <see cref="RequiredAttribute" />, or
    /// declared with the C# <see langword="required" /> keyword, which has the same effect.
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
