// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlExtensionDataAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Nodes;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Designates a property or field that captures table entries which do not map to any other member during
/// deserialization, and whose entries are written back out during serialization.
/// </summary>
/// <remarks>
/// <para>
/// The member must be of type <see cref="TomlObject" />,
/// <see cref="System.Collections.Generic.IDictionary{TKey, TValue}" />, or
/// <see cref="System.Collections.Generic.Dictionary{TKey, TValue}" /> with a key of <see cref="string" /> and a value
/// of <see cref="TomlNode" />. A type may declare at most one extension-data member.
/// </para>
/// <para>
/// On serialization the captured entries are written alongside the type's other members in document order.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class ServerConfig
/// {
///     public int Port { get; set; }
///
///     [TomlExtensionData]
///     public Dictionary<string, TomlNode>? Extra { get; set; }
/// }
///
/// // Keys that map to no member land in Extra and are written back on serialization.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class TomlExtensionDataAttribute
    : TomlAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlExtensionDataAttribute" /> class.
    /// </summary>
    public TomlExtensionDataAttribute()
    {
    }
}
