// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConstructorAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Marks the constructor the serializer uses to instantiate a type during deserialization, resolving the ambiguity when
/// a type declares more than one constructor.
/// </summary>
/// <remarks>
/// When no constructor carries this attribute, the serializer uses a public parameterless constructor when one exists,
/// otherwise the single declared constructor, otherwise the constructor with the most parameters.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Endpoint
/// {
///     [Constructor]
///     public Endpoint(string host, int port) => (Host, Port) = (host, port);
///
///     public Endpoint(Uri uri) : this(uri.Host, uri.Port) { }
///
///     public string Host { get; }
///     public int Port { get; }
/// }
///
/// // Deserialization instantiates Endpoint through the marked constructor.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class ConstructorAttribute
    : SerializationAttribute
{
}
