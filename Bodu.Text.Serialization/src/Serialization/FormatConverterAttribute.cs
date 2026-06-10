// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatConverterAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the <see cref="FormatConverter" /> to use for a property, field, or type, taking precedence over the
/// converters configured on the serializer options.
/// </summary>
/// <remarks>
/// The referenced type must derive from <see cref="FormatConverter" /> and declare a public parameterless constructor.
/// Applied to a member, the converter governs that member only; applied to a type, it governs every use of the type.
/// This mirrors <see cref="System.Text.Json.Serialization.JsonConverterAttribute" />.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum,
    AllowMultiple = false,
    Inherited = true)]
public sealed class FormatConverterAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatConverterAttribute" /> class.
    /// </summary>
    /// <param name="converterType">The converter type to use.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="converterType" /> is <see langword="null" />.
    /// </exception>
    public FormatConverterAttribute(Type converterType)
    {
        ThrowHelper.ThrowIfNull(converterType);

        ConverterType = converterType;
    }

    /// <summary>
    /// Gets the converter type to use.
    /// </summary>
    /// <returns>The converter type.</returns>
    public Type ConverterType { get; }
}
