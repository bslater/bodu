// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IncludeAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Forces a member to participate in serialization: a property's non-public accessors are bound (allowing members such
/// as <c>{ get; private set; }</c> or <c>{ get; init; }</c> with a non-public setter to be read and written), and a
/// public field is surfaced even when the serializer options' <c>IncludeFields</c> setting is disabled.
/// </summary>
/// <remarks>
/// Without this attribute a property is included only when it exposes a public getter, and is assigned only through a
/// public setter; when the attribute is present the serializer binds through the declared accessors regardless of their
/// visibility. Public fields participate only when this attribute is applied or the serializer options' <c>IncludeFields</c>
/// setting is enabled; non-public fields are never surfaced.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Counter
/// {
///     [Include]
///     public int Total { get; private set; }
///
///     [Include]
///     public int Retries;
/// }
///
/// // Both members round-trip, despite the non-public setter and the field.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class IncludeAttribute
    : SerializationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncludeAttribute" /> class.
    /// </summary>
    public IncludeAttribute()
    {
    }
}
