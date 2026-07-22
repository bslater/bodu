// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializer.Member.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

public static partial class DotEnvSerializer
{
    /// <summary>
    /// Describes a single mapped POCO member — its serialized name, type, accessors, and the attribute-derived rules
    /// that govern its inclusion and ordering.
    /// </summary>
    private sealed class Member
    {
        /// <summary>The reflected property, or <see langword="null" /> for a field member.</summary>
        private readonly PropertyInfo? _property;

        /// <summary>The reflected field, or <see langword="null" /> for a property member.</summary>
        private readonly FieldInfo? _field;

        /// <summary>
        /// Initializes a new instance of the <see cref="Member" /> class.
        /// </summary>
        /// <param name="property">The reflected property, or <see langword="null" /> for a field.</param>
        /// <param name="field">The reflected field, or <see langword="null" /> for a property.</param>
        private Member(PropertyInfo? property, FieldInfo? field)
        {
            _property = property;
            _field = field;
        }

        /// <summary>
        /// Gets the serialized key name.
        /// </summary>
        /// <value>The key name.</value>
        public string Name { get; private init; } = string.Empty;

        /// <summary>
        /// Gets the member's declared type.
        /// </summary>
        /// <value>The member type.</value>
        public Type MemberType { get; private init; } = typeof(object);

        /// <summary>
        /// Gets a value indicating whether the member can be read.
        /// </summary>
        /// <value><see langword="true" /> when readable.</value>
        public bool CanRead { get; private init; }

        /// <summary>
        /// Gets a value indicating whether the member can be written.
        /// </summary>
        /// <value><see langword="true" /> when writable.</value>
        public bool CanWrite { get; private init; }

        /// <summary>
        /// Gets a value indicating whether the member is required on deserialization.
        /// </summary>
        /// <value><see langword="true" /> when required.</value>
        public bool Required { get; private init; }

        /// <summary>
        /// Gets the member-level ignore condition, or <see langword="null" /> to use the options default.
        /// </summary>
        /// <value>The ignore condition, or <see langword="null" />.</value>
        public IgnoreCondition? IgnoreCondition { get; private init; }

        /// <summary>
        /// Gets the serialization order key.
        /// </summary>
        /// <value>The order; zero when unspecified.</value>
        public int Order { get; private init; }

        /// <summary>
        /// Creates a member descriptor from a property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The member descriptor.</returns>
        public static Member FromProperty(PropertyInfo property, DotEnvSerializerOptions options) =>
            new(property, null)
            {
                Name = ResolveName(property, property.Name, options),
                MemberType = property.PropertyType,
                CanRead = property.GetMethod is { IsPublic: true },
                CanWrite = property.SetMethod is { IsPublic: true },
                Required = property.GetCustomAttribute<RequiredAttribute>() is not null,
                IgnoreCondition = ResolveIgnore(property),
                Order = property.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0,
            };

        /// <summary>
        /// Creates a member descriptor from a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The member descriptor.</returns>
        public static Member FromField(FieldInfo field, DotEnvSerializerOptions options) =>
            new(null, field)
            {
                Name = ResolveName(field, field.Name, options),
                MemberType = field.FieldType,
                CanRead = true,
                CanWrite = !field.IsInitOnly,
                Required = field.GetCustomAttribute<RequiredAttribute>() is not null,
                IgnoreCondition = ResolveIgnore(field),
                Order = field.GetCustomAttribute<PropertyOrderAttribute>()?.Order ?? 0,
            };

        /// <summary>
        /// Gets the member's value from the supplied instance.
        /// </summary>
        /// <param name="instance">The owning instance.</param>
        /// <returns>The member value.</returns>
        public object? GetValue(object instance) =>
            _property is not null ? _property.GetValue(instance) : _field!.GetValue(instance);

        /// <summary>
        /// Sets the member's value on the supplied instance.
        /// </summary>
        /// <param name="instance">The owning instance.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(object instance, object? value)
        {
            if (_property is not null)
                _property.SetValue(instance, value);
            else
                _field!.SetValue(instance, value);
        }

        /// <summary>
        /// Resolves the serialized name for a member, honouring the property-name attribute and the naming policy.
        /// </summary>
        /// <param name="member">The reflected member.</param>
        /// <param name="clrName">The member's CLR name.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The serialized name.</returns>
        private static string ResolveName(MemberInfo member, string clrName, DotEnvSerializerOptions options) =>
            member.GetCustomAttribute<PropertyNameAttribute>()?.Name ?? options.ConvertName(clrName);

        /// <summary>
        /// Resolves the member-level ignore condition, treating a conditional <see cref="IgnoreAttribute" /> as an
        /// override and <see cref="Serialization.IgnoreCondition.Always" /> as already filtered upstream.
        /// </summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The ignore condition, or <see langword="null" />.</returns>
        private static IgnoreCondition? ResolveIgnore(MemberInfo member)
        {
            IgnoreAttribute? attribute = member.GetCustomAttribute<IgnoreAttribute>();
            return attribute is null || attribute.Condition == Serialization.IgnoreCondition.Always ? null : attribute.Condition;
        }
    }
}
