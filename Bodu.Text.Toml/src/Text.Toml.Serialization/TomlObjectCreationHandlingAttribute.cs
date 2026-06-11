// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlObjectCreationHandlingAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Specifies, for the annotated type or member, whether the serializer replaces the value with a freshly created
/// instance or populates the value already held during deserialization, overriding the serializer-wide
/// <see cref="TomlSerializerOptions.PreferredObjectCreationHandling" />. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonObjectCreationHandlingAttribute" />.
/// </summary>
/// <remarks>
/// When applied to a member the attribute governs that member alone; when applied to a type it governs every member of
/// the type that does not carry its own attribute. A member-level attribute takes precedence over a type-level one, and
/// both take precedence over the options-level default.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlObjectCreationHandlingAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlObjectCreationHandlingAttribute" /> class.
    /// </summary>
    /// <param name="handling">The object-creation handling applied to the annotated type or member.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="handling" /> is not a defined <see cref="TomlObjectCreationHandling" /> value.
    /// </exception>
    public TomlObjectCreationHandlingAttribute(TomlObjectCreationHandling handling)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(handling);

        Handling = handling;
    }

    /// <summary>
    /// Gets the object-creation handling applied to the annotated type or member.
    /// </summary>
    /// <returns>The object-creation handling.</returns>
    public TomlObjectCreationHandling Handling { get; }
}
