// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodePropertyNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies the dictionary key used for a property or field when it is serialized to Bencode, overriding the member's
/// CLR name and any configured naming policy.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Profile
/// {
///     [BencodePropertyName("display-name")]
///     public string DisplayName { get; set; } = "Ada";
/// }
///
/// // Serializes as the dictionary entry: 12:display-name3:Ada
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class BencodePropertyNameAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodePropertyNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The name to use for the member in serialized output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public BencodePropertyNameAttribute(string name)
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
