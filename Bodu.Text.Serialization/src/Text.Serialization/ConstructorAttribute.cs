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
/// <para>
/// The serializer considers public instance constructors only, so the marked constructor must be public: on an
/// internal, protected, or private constructor the attribute is ignored, and the constructor is chosen as though no
/// constructor carried it.
/// </para>
/// <para>
/// When no public constructor carries this attribute, the serializer uses a public parameterless constructor when one
/// exists, otherwise the single public constructor, otherwise the public constructor with the most parameters.
/// </para>
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
