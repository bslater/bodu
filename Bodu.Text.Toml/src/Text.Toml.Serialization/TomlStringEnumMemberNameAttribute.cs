// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlStringEnumMemberNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Specifies the string name used for an individual enumeration member when the enumeration is serialized to TOML by
/// name, overriding both the member's CLR name and any naming policy applied to the enumeration.
/// </summary>
/// <remarks>
/// <para>
/// The attribute applies only when the enumeration is converted to a string holding its member name — for example
/// through <see cref="TomlStringEnumConverter" /> or the built-in by-name enum converter — and has no effect when the
/// enumeration is written as an integer.
/// </para>
/// <para>
/// This attribute derives from <see cref="TomlAttribute" /> so that it is discoverable alongside the rest of the TOML
/// serialization attribute family.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlStringEnumMemberNameAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlStringEnumMemberNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The string name used for the annotated enumeration member in serialized output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public TomlStringEnumMemberNameAttribute(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// Gets the string name used for the annotated enumeration member in serialized output.
    /// </summary>
    /// <returns>The serialized member name.</returns>
    public string Name { get; }
}
