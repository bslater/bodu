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
/// <remarks>
/// This attribute mirrors the role of <see cref="System.Text.Json.Serialization.JsonIgnoreAttribute" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class BencodeIgnoreAttribute
    : Attribute
{
    /// <summary>
    /// Gets or sets the condition under which the member is ignored.
    /// </summary>
    /// <value>The ignore condition; <see cref="BencodeIgnoreCondition.Always" /> by default.</value>
    /// <returns>The configured ignore condition.</returns>
    public BencodeIgnoreCondition Condition { get; set; } = BencodeIgnoreCondition.Always;
}
