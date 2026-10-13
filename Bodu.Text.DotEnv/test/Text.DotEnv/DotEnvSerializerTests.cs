// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;

using Bodu.Test.Kat;
using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Hosts the shared model types and <see cref="DynamicData" /> providers for the <see cref="DotEnvSerializer" /> test
/// backbone (<c>.Serialize</c>, <c>.Deserialize</c>, <c>.RoundTrip</c>).
/// </summary>
[TestClass]
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// A simple configuration POCO exercising strings, typed values, and the property-name/ignore attributes.
    /// </summary>
    public sealed class ServerConfig
    {
        /// <summary>Gets or sets the host name, remapped to the <c>HOST</c> key.</summary>
        /// <value>The host.</value>
        [PropertyName("HOST")]
        public string Host { get; set; } = string.Empty;

        /// <summary>Gets or sets the port, remapped to the <c>PORT</c> key.</summary>
        /// <value>The port.</value>
        [PropertyName("PORT")]
        public int Port { get; set; }

        /// <summary>Gets or sets a value indicating whether TLS is enabled, remapped to <c>TLS</c>.</summary>
        /// <value><see langword="true" /> when TLS is enabled.</value>
        [PropertyName("TLS")]
        public bool UseTls { get; set; }

        /// <summary>Gets or sets a secret that is never serialized.</summary>
        /// <value>The secret.</value>
        [Bodu.Text.Serialization.Ignore]
        public string? Secret { get; set; }
    }

    /// <summary>
    /// A POCO with a required member, used to verify the required-key contract.
    /// </summary>
    public sealed class RequiredConfig
    {
        /// <summary>Gets or sets the required token.</summary>
        /// <value>The token.</value>
        [Required]
        public string Token { get; set; } = string.Empty;
    }

    /// <summary>
    /// A POCO with one member whose key a document may define more than once.
    /// </summary>
    public sealed class RepeatedKeyConfig
    {
        /// <summary>Gets or sets the value, bound to the <c>MULTI1</c> key under the <c>Web</c> defaults.</summary>
        /// <value>The value.</value>
        public string Multi1 { get; set; } = string.Empty;
    }

    /// <summary>
    /// A POCO that records the serialization callback invocations.
    /// </summary>
    public sealed class CallbackConfig
        : IOnSerializing, IOnDeserialized
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets a value indicating whether the serializing callback fired.</summary>
        /// <value><see langword="true" /> when the callback fired.</value>
        public bool SerializingCalled { get; private set; }

        /// <summary>Gets a value indicating whether the deserialized callback fired.</summary>
        /// <value><see langword="true" /> when the callback fired.</value>
        public bool DeserializedCalled { get; private set; }

        /// <inheritdoc />
        public void OnSerializing() => SerializingCalled = true;

        /// <inheritdoc />
        public void OnDeserialized() => DeserializedCalled = true;
    }

    /// <summary>
    /// A POCO whose members are the scalars beyond the common set: <see cref="DateOnly" />, <see cref="TimeOnly" />, the
    /// 128-bit and half-precision numbers and <see cref="System.Numerics.BigInteger" />, two of them also in nullable form.
    /// </summary>
    public sealed class ExtendedScalarRecord
    {
        /// <summary>Gets or sets the date.</summary>
        /// <value>The date.</value>
        public DateOnly Day { get; set; }

        /// <summary>Gets or sets the time of day.</summary>
        /// <value>The time of day.</value>
        public TimeOnly Time { get; set; }

        /// <summary>Gets or sets the signed 128-bit number.</summary>
        /// <value>The signed 128-bit number.</value>
        public Int128 Large { get; set; }

        /// <summary>Gets or sets the unsigned 128-bit number.</summary>
        /// <value>The unsigned 128-bit number.</value>
        public UInt128 Unsigned { get; set; }

        /// <summary>Gets or sets the half-precision number.</summary>
        /// <value>The half-precision number.</value>
        public Half Small { get; set; }

        /// <summary>Gets or sets the arbitrary-precision integer.</summary>
        /// <value>The arbitrary-precision integer.</value>
        public System.Numerics.BigInteger Big { get; set; }

        /// <summary>Gets or sets the optional signed 128-bit number.</summary>
        /// <value>The number, or <see langword="null" />.</value>
        public Int128? MaybeLarge { get; set; }

        /// <summary>Gets or sets the optional time of day.</summary>
        /// <value>The time of day, or <see langword="null" />.</value>
        public TimeOnly? MaybeTime { get; set; }
    }

    /// <summary>
    /// A POCO with a key <c>HOST</c> and no public parameterless constructor, which deserialization cannot create.
    /// </summary>
    public sealed class ConstructedConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructedConfig" /> class.
        /// </summary>
        /// <param name="host">The host name.</param>
        public ConstructedConfig(string host) =>
            Host = host;

        /// <summary>Gets or sets the host name.</summary>
        /// <value>The host.</value>
        [PropertyName("HOST")]
        public string Host { get; set; }
    }

    /// <summary>
    /// An abstract POCO with a key <c>HOST</c>, which deserialization cannot create.
    /// </summary>
    public abstract class AbstractConfig
    {
        /// <summary>Gets or sets the host name.</summary>
        /// <value>The host.</value>
        [PropertyName("HOST")]
        public string Host { get; set; } = string.Empty;
    }

    /// <summary>
    /// A base POCO whose text <c>Port</c> property <see cref="PortNumberHidingConfig" /> hides.
    /// </summary>
    public class PortTextBase
    {
        /// <summary>Gets or sets the base port text, which a derived type hides.</summary>
        /// <value>The port text.</value>
        public string? Port { get; set; }
    }

    /// <summary>
    /// A POCO whose <see cref="int" /> <c>Port</c> property hides the base type's text <c>Port</c> with the
    /// <see langword="new" /> modifier.
    /// </summary>
    public sealed class PortNumberHidingConfig
        : PortTextBase
    {
        /// <summary>Gets or sets the port, hiding the base type's.</summary>
        /// <value>The port.</value>
        public new int Port { get; set; }
    }

    /// <summary>
    /// A base POCO whose <see cref="int" /> <c>Port</c> property <see cref="PortTextHidingConfig" /> hides.
    /// </summary>
    public class PortNumberBase
    {
        /// <summary>Gets or sets the base port number, which a derived type hides.</summary>
        /// <value>The port number.</value>
        public int Port { get; set; }
    }

    /// <summary>
    /// A POCO whose text <c>Port</c> property hides the base type's <see cref="int" /> <c>Port</c> with the
    /// <see langword="new" /> modifier.
    /// </summary>
    public sealed class PortTextHidingConfig
        : PortNumberBase
    {
        /// <summary>Gets or sets the port text, hiding the base type's.</summary>
        /// <value>The port text.</value>
        public new string? Port { get; set; }
    }

    /// <summary>
    /// A POCO whose member is declared with the C# <see langword="required" /> keyword rather than
    /// <see cref="RequiredAttribute" />.
    /// </summary>
    public sealed class RequiredKeywordConfig
    {
        /// <summary>Gets or sets the required host name, remapped to the <c>HOST</c> key.</summary>
        /// <value>The host.</value>
        [PropertyName("HOST")]
        public required string Host { get; set; }
    }

    /// <summary>
    /// A POCO whose two members are both mapped to the key <c>X</c>.
    /// </summary>
    public sealed class DuplicateKeyConfig
    {
        /// <summary>Gets or sets the first value mapped to <c>X</c>.</summary>
        /// <value>The first value.</value>
        [PropertyName("X")]
        public string? First { get; set; }

        /// <summary>Gets or sets the second value mapped to <c>X</c>.</summary>
        /// <value>The second value.</value>
        [PropertyName("X")]
        public string? Second { get; set; }
    }

    /// <summary>
    /// A POCO whose member <c>HostName</c> the <see cref="DotEnvSerializerDefaults.Web" /> naming policy maps to
    /// <c>HOST_NAME</c>, the key another member names explicitly.
    /// </summary>
    public sealed class PolicyDuplicateKeyConfig
    {
        /// <summary>Gets or sets the host name, mapped to <c>HOST_NAME</c> by the naming policy.</summary>
        /// <value>The host name.</value>
        public string? HostName { get; set; }

        /// <summary>Gets or sets an alias mapped explicitly to <c>HOST_NAME</c>.</summary>
        /// <value>The alias.</value>
        [PropertyName("HOST_NAME")]
        public string? Alias { get; set; }
    }

    /// <summary>
    /// Gets scalar root values, each of which serialization must reject with <see cref="DotEnvSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named scalar value and the exception it raises.</value>
    public static IEnumerable<object[]> ScalarRootValues =>
    [
        [new InvalidKat<object>("decimal", 1.5m, typeof(DotEnvSerializationException), MessageContains: "System.Decimal")],
        [new InvalidKat<object>("DateTime", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), typeof(DotEnvSerializationException), MessageContains: "System.DateTime")],
        [new InvalidKat<object>("DateTimeOffset", DateTimeOffset.UnixEpoch, typeof(DotEnvSerializationException), MessageContains: "System.DateTimeOffset")],
        [new InvalidKat<object>("TimeSpan", TimeSpan.FromHours(1), typeof(DotEnvSerializationException), MessageContains: "System.TimeSpan")],
        [new InvalidKat<object>("Guid", Guid.Empty, typeof(DotEnvSerializationException), MessageContains: "System.Guid")],
        [new InvalidKat<object>("Uri", new Uri("https://example.com/"), typeof(DotEnvSerializationException), MessageContains: "System.Uri")],
        [new InvalidKat<object>("DateOnly", new DateOnly(2026, 1, 2), typeof(DotEnvSerializationException), MessageContains: "System.DateOnly")],
        [new InvalidKat<object>("TimeOnly", new TimeOnly(1, 2), typeof(DotEnvSerializationException), MessageContains: "System.TimeOnly")],
        [new InvalidKat<object>("Int128", (Int128)5, typeof(DotEnvSerializationException), MessageContains: "System.Int128")],
        [new InvalidKat<object>("UInt128", (UInt128)5, typeof(DotEnvSerializationException), MessageContains: "System.UInt128")],
        [new InvalidKat<object>("Half", (Half)1, typeof(DotEnvSerializationException), MessageContains: "System.Half")],
        [new InvalidKat<object>("BigInteger", new BigInteger(7), typeof(DotEnvSerializationException), MessageContains: "System.Numerics.BigInteger")],
        [new InvalidKat<object>("char", 'c', typeof(DotEnvSerializationException), MessageContains: "System.Char")],
    ];

    /// <summary>
    /// Gets scalar root types, including nullable forms, each of which deserialization must reject with
    /// <see cref="DotEnvSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named scalar type and the exception it raises.</value>
    public static IEnumerable<object[]> ScalarRootTypes =>
    [
        [new InvalidKat<Type>("decimal", typeof(decimal), typeof(DotEnvSerializationException), MessageContains: "System.Decimal")],
        [new InvalidKat<Type>("DateTime", typeof(DateTime), typeof(DotEnvSerializationException), MessageContains: "System.DateTime")],
        [new InvalidKat<Type>("DateTimeOffset", typeof(DateTimeOffset), typeof(DotEnvSerializationException), MessageContains: "System.DateTimeOffset")],
        [new InvalidKat<Type>("TimeSpan", typeof(TimeSpan), typeof(DotEnvSerializationException), MessageContains: "System.TimeSpan")],
        [new InvalidKat<Type>("Guid", typeof(Guid), typeof(DotEnvSerializationException), MessageContains: "System.Guid")],
        [new InvalidKat<Type>("Uri", typeof(Uri), typeof(DotEnvSerializationException), MessageContains: "System.Uri")],
        [new InvalidKat<Type>("DateOnly", typeof(DateOnly), typeof(DotEnvSerializationException), MessageContains: "System.DateOnly")],
        [new InvalidKat<Type>("TimeOnly", typeof(TimeOnly), typeof(DotEnvSerializationException), MessageContains: "System.TimeOnly")],
        [new InvalidKat<Type>("Int128", typeof(Int128), typeof(DotEnvSerializationException), MessageContains: "System.Int128")],
        [new InvalidKat<Type>("UInt128", typeof(UInt128), typeof(DotEnvSerializationException), MessageContains: "System.UInt128")],
        [new InvalidKat<Type>("Half", typeof(Half), typeof(DotEnvSerializationException), MessageContains: "System.Half")],
        [new InvalidKat<Type>("BigInteger", typeof(BigInteger), typeof(DotEnvSerializationException), MessageContains: "System.Numerics.BigInteger")],
        [new InvalidKat<Type>("int?", typeof(int?), typeof(DotEnvSerializationException), MessageContains: "System.Nullable")],
        [new InvalidKat<Type>("Guid?", typeof(Guid?), typeof(DotEnvSerializationException), MessageContains: "System.Nullable")],
        [new InvalidKat<Type>("DateOnly?", typeof(DateOnly?), typeof(DotEnvSerializationException), MessageContains: "System.Nullable")],
    ];

    /// <summary>
    /// Deserializes text into a root of a type known only at run time, through
    /// <see cref="DotEnvSerializer.Deserialize{T}(string, DotEnvSerializerOptions?)" />, rethrowing the serializer's own exception.
    /// </summary>
    /// <param name="rootType">The root type.</param>
    /// <param name="text">The text to deserialize.</param>
    /// <returns>The deserialized root.</returns>
    private static object? DeserializeAs(Type rootType, string text)
    {
        MethodInfo method = typeof(DotEnvSerializer)
            .GetMethod(nameof(DotEnvSerializer.Deserialize), [typeof(string), typeof(DotEnvSerializerOptions)])!
            .MakeGenericMethod(rootType);

        try
        {
            return method.Invoke(null, [text, null]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
