// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatConstructorAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Marks the constructor a serializer uses to instantiate a type during deserialization, resolving the ambiguity when a
/// type declares more than one constructor.
/// </summary>
/// <remarks>
/// When no constructor carries this attribute, the serializer uses a public parameterless constructor when one exists,
/// otherwise the single declared constructor, otherwise the constructor with the most parameters. This mirrors
/// <see cref="System.Text.Json.Serialization.JsonConstructorAttribute" />.
/// </remarks>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class FormatConstructorAttribute
    : Attribute
{
}
