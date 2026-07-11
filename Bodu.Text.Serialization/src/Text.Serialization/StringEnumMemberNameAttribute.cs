// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEnumMemberNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the string name used for an individual enumeration member when the enumeration is serialized by name,
/// overriding both the member's CLR name and any naming policy applied to the enumeration.
/// </summary>
/// <remarks>
/// <para>
/// The attribute applies only when the enumeration is converted to a string holding its member name — for example
/// through the by-name enum converter — and has no effect when the enumeration is written as an integer.
/// </para>
/// <para>
/// This attribute derives from <see cref="SerializationAttribute" /> so that it is discoverable alongside the rest of
/// the serialization attribute family.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public enum Status
/// {
///     Active,
///
///     [StringEnumMemberName("on-hold")]
///     OnHold,
/// }
///
/// // Serialized by name, Status.OnHold is written as "on-hold".
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class StringEnumMemberNameAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringEnumMemberNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The string name used for the annotated enumeration member in serialized output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public StringEnumMemberNameAttribute(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// Gets the string name used for the annotated enumeration member in serialized output.
    /// </summary>
    /// <value>The serialized member name.</value>
    public string Name { get; }
}
