// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlIncludeAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Forces a member to participate in serialization: a property's non-public accessors are bound (allowing members such
/// as <c>{ get; private set; }</c> or <c>{ get; init; }</c> with a non-public setter to be read and written), and a
/// public field is surfaced even when <see cref="TomlSerializerOptions.IncludeFields" /> is disabled. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" />.
/// </summary>
/// <remarks>
/// Without this attribute a property is included only when it exposes a public getter, and is assigned only through a
/// public setter; when the attribute is present the serializer binds through the declared accessors regardless of their
/// visibility. Public fields participate only when this attribute is applied or
/// <see cref="TomlSerializerOptions.IncludeFields" /> is enabled; non-public fields are never surfaced.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlIncludeAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlIncludeAttribute" /> class.
    /// </summary>
    public TomlIncludeAttribute()
    {
    }
}
