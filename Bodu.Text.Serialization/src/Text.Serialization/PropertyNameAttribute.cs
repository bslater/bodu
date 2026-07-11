// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the key used for a property or field when it is serialized, overriding the member's CLR name and any
/// configured naming policy.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Profile
/// {
///     [PropertyName("display-name")]
///     public string DisplayName { get; set; } = "Ada";
/// }
///
/// // Serializes under the key "display-name".
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class PropertyNameAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The name to use for the member in serialized output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public PropertyNameAttribute(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// Gets the name used for the member in serialized output.
    /// </summary>
    /// <value>The serialized member name.</value>
    public string Name { get; }
}
