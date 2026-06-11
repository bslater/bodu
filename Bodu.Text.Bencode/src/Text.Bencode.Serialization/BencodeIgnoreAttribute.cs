// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeIgnoreAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Excludes a property or field from Bencode serialization, either unconditionally or under the condition given by
/// <see cref="Condition" />.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Account
/// {
///     [BencodeIgnore]
///     public string? Secret { get; set; }
///
///     [BencodeIgnore(Condition = BencodeIgnoreCondition.WhenWritingNull)]
///     public string? Comment { get; set; }
/// }
///
/// // Secret is never written; Comment is written only when it is non-null.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class BencodeIgnoreAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Gets or sets the condition under which the member is ignored.
    /// </summary>
    /// <value>The ignore condition; <see cref="BencodeIgnoreCondition.Always" /> by default.</value>
    /// <returns>The configured ignore condition.</returns>
    public BencodeIgnoreCondition Condition { get; set; } = BencodeIgnoreCondition.Always;
}
