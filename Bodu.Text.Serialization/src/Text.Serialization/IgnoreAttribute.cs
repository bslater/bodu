// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IgnoreAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Excludes a property or field from serialization, either unconditionally or under the condition given by
/// <see cref="Condition" />.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Account
/// {
///     [Ignore]
///     public string? Secret { get; set; }
///
///     [Ignore(Condition = IgnoreCondition.WhenWritingNull)]
///     public string? Comment { get; set; }
/// }
///
/// // Secret is never written; Comment is written only when it is non-null.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Gets or sets the condition under which the member is ignored.
    /// </summary>
    /// <value>The ignore condition; <see cref="IgnoreCondition.Always" /> by default.</value>
    public IgnoreCondition Condition { get; set; } = IgnoreCondition.Always;
}
