// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectCreationHandlingAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies, for the annotated type or member, whether the serializer replaces the value with a freshly created
/// instance or populates the value already held during deserialization, overriding the serializer-wide preferred
/// object-creation handling.
/// </summary>
/// <remarks>
/// When applied to a member the attribute governs that member alone; when applied to a type it governs every member of
/// the type that does not carry its own attribute. A member-level attribute takes precedence over a type-level one, and
/// both take precedence over the options-level default.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Pipeline
/// {
///     [ObjectCreationHandling(ObjectCreationHandling.Populate)]
///     public List<string> Steps { get; } = new() { "restore" };
/// }
///
/// // Deserialized entries are appended to the existing list instead of replacing it.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ObjectCreationHandlingAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectCreationHandlingAttribute" /> class.
    /// </summary>
    /// <param name="handling">The object-creation handling applied to the annotated type or member.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="handling" /> is not a defined <see cref="ObjectCreationHandling" /> value.
    /// </exception>
    public ObjectCreationHandlingAttribute(ObjectCreationHandling handling)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(handling);

        Handling = handling;
    }

    /// <summary>
    /// Gets the object-creation handling applied to the annotated type or member.
    /// </summary>
    /// <value>The object-creation handling.</value>
    public ObjectCreationHandling Handling { get; }
}
