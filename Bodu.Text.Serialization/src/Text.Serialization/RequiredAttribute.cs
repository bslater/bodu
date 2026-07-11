// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RequiredAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Marks a property or field as required, so deserialization fails when the corresponding key is absent from the input.
/// </summary>
/// <remarks>
/// Applying this attribute has the same effect as declaring the member with the <see langword="required" /> keyword:
/// the member must appear in the input being read, otherwise the serializer throws a serialization exception.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class ServerConfig
/// {
///     [Required]
///     public string Host { get; set; } = string.Empty;
/// }
///
/// // Deserializing input without a "Host" key throws a serialization exception.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class RequiredAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredAttribute" /> class.
    /// </summary>
    public RequiredAttribute()
    {
    }
}
