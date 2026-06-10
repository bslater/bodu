// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatPropertyNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the name used for a property or field when it is serialized, overriding the member's CLR name and any
/// configured naming policy.
/// </summary>
/// <remarks>
/// This attribute is honored by every Bodu format serializer, mirroring the role of
/// <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class FormatPropertyNameAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatPropertyNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The name to use for the member in serialized output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public FormatPropertyNameAttribute(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// Gets the name used for the member in serialized output.
    /// </summary>
    /// <returns>The serialized member name.</returns>
    public string Name { get; }
}
