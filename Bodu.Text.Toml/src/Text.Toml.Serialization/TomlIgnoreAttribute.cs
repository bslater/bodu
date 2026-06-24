// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlIgnoreAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Excludes a property or field from TOML serialization, either unconditionally or under the condition given by
/// <see cref="Condition" />.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Account
/// {
///     [TomlIgnore]
///     public string? Secret { get; set; }
///
///     [TomlIgnore(Condition = TomlIgnoreCondition.WhenWritingNull)]
///     public string? Comment { get; set; }
/// }
///
/// // Secret is never written; Comment is written only when it is non-null.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class TomlIgnoreAttribute
    : TomlAttribute
{
    /// <summary>
    /// Gets or sets the condition under which the member is ignored.
    /// </summary>
    /// <value>The ignore condition; <see cref="TomlIgnoreCondition.Always" /> by default.</value>
    public TomlIgnoreCondition Condition { get; set; } = TomlIgnoreCondition.Always;
}
