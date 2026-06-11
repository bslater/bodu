// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeStringEnumMemberNameAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies the byte-string name used for an individual enumeration member when the enumeration is serialized to
/// Bencode by name, overriding both the member's CLR name and any naming policy applied to the enumeration.
/// </summary>
/// <remarks>
/// <para>
/// The attribute applies only when the enumeration is converted to a byte string holding its member name — for example
/// through <see cref="BencodeStringEnumConverter" /> or the built-in by-name enum converter — and has no effect when
/// the enumeration is written as an integer.
/// </para>
/// <para>
/// This attribute derives from <see cref="BencodeAttribute" /> so that it is discoverable alongside the rest of the
/// Bencode serialization attribute family.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [BencodeConverter(typeof(BencodeStringEnumConverter<Status>))]
/// public enum Status
/// {
///     Active,
///
///     [BencodeStringEnumMemberName("on-hold")]
///     OnHold,
/// }
///
/// // Status.OnHold serializes as the byte string 7:on-hold.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class BencodeStringEnumMemberNameAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeStringEnumMemberNameAttribute" /> class.
    /// </summary>
    /// <param name="name">The byte-string name used for the annotated enumeration member in serialized output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public BencodeStringEnumMemberNameAttribute(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>
    /// Gets the byte-string name used for the annotated enumeration member in serialized output.
    /// </summary>
    /// <returns>The serialized member name.</returns>
    public string Name { get; }
}
