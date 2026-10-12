// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;

using Bodu.Test.Kat;
using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains the shared model types for the <see cref="IniSerializer" /> test suite; the member backbone lives in the
/// <c>.Serialize</c>, <c>.Deserialize</c>, <c>.SerializeAsync</c>, <c>.DeserializeAsync</c>, and <c>.RoundTrip</c>
/// partials.
/// </summary>
[TestClass]
public partial class IniSerializerTests
{
    /// <summary>
    /// A root model with global scalar members, a section POCO, and a section dictionary.
    /// </summary>
    private sealed class ServerConfig
    {
        /// <summary>Gets or sets the server name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the retry count (a global key).</summary>
        public int Retries { get; set; }

        /// <summary>Gets or sets the database section.</summary>
        public DatabaseSection? Database { get; set; }

        /// <summary>Gets or sets the logging section as a dictionary.</summary>
        public Dictionary<string, string>? Logging { get; set; }
    }

    /// <summary>
    /// A section model of scalar members.
    /// </summary>
    private sealed class DatabaseSection
    {
        /// <summary>Gets or sets the host name.</summary>
        public string? Host { get; set; }

        /// <summary>Gets or sets the port number.</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose only member is required.
    /// </summary>
    private sealed class RequiredConfig
    {
        /// <summary>Gets or sets the required name.</summary>
        [Required]
        public string? Name { get; set; }
    }

    /// <summary>
    /// A root model with a section nested one level too deep.
    /// </summary>
    private sealed class DeepConfig
    {
        /// <summary>Gets or sets the outer section.</summary>
        public OuterSection? Outer { get; set; }
    }

    /// <summary>
    /// A section model that illegally contains a further section.
    /// </summary>
    private sealed class OuterSection
    {
        /// <summary>Gets or sets the nested section, which INI cannot represent.</summary>
        public DatabaseSection? Inner { get; set; }
    }

    /// <summary>
    /// A root model with a public global key and a private property, which is not part of its serialized form.
    /// </summary>
    private sealed class PrivateMemberConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets a value the serializer must not write, because the property is private.</summary>
        private string Ignored { get; set; } = "ignored";
    }

    /// <summary>
    /// A root model whose only member is an integer.
    /// </summary>
    private sealed class PortConfig
    {
        /// <summary>Gets or sets the port number (a global key).</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose only member is a <see cref="float" />.
    /// </summary>
    private sealed class RatioConfig
    {
        /// <summary>Gets or sets the ratio (a global key).</summary>
        public float Ratio { get; set; }
    }

    /// <summary>
    /// A hand-written <see cref="IIniSectionFactory{TSection}" /> for <see cref="DatabaseSection" />, standing in for
    /// the generated factory in the reflection-free section-overload tests.
    /// </summary>
    private sealed class DatabaseSectionFactory : IIniSectionFactory<DatabaseSection>
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Keys { get; } = new[] { "Host", "Port" };

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, string>> GetEntries(DatabaseSection section)
        {
            yield return new KeyValuePair<string, string>("Host", section.Host ?? string.Empty);
            yield return new KeyValuePair<string, string>("Port", section.Port.ToString(CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public DatabaseSection Create(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var section = new DatabaseSection();
            foreach (KeyValuePair<string, string> entry in entries)
            {
                if (string.Equals(entry.Key, "Host", StringComparison.Ordinal))
                    section.Host = entry.Value;
                else if (string.Equals(entry.Key, "Port", StringComparison.Ordinal))
                    section.Port = int.Parse(entry.Value, CultureInfo.InvariantCulture);
            }

            return section;
        }
    }

    /// <summary>
    /// Gets scalar root values, each of which serialization must reject with <see cref="IniSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named scalar value and the exception it raises.</value>
    public static IEnumerable<object[]> ScalarRootValues =>
    [
        [new InvalidKat<object>("decimal", 1.5m, typeof(IniSerializationException), MessageContains: "System.Decimal")],
        [new InvalidKat<object>("DateTime", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), typeof(IniSerializationException), MessageContains: "System.DateTime")],
        [new InvalidKat<object>("DateTimeOffset", DateTimeOffset.UnixEpoch, typeof(IniSerializationException), MessageContains: "System.DateTimeOffset")],
        [new InvalidKat<object>("TimeSpan", TimeSpan.FromHours(1), typeof(IniSerializationException), MessageContains: "System.TimeSpan")],
        [new InvalidKat<object>("Guid", Guid.Empty, typeof(IniSerializationException), MessageContains: "System.Guid")],
        [new InvalidKat<object>("Uri", new Uri("https://example.com/"), typeof(IniSerializationException), MessageContains: "System.Uri")],
        [new InvalidKat<object>("DateOnly", new DateOnly(2026, 1, 2), typeof(IniSerializationException), MessageContains: "System.DateOnly")],
        [new InvalidKat<object>("TimeOnly", new TimeOnly(1, 2), typeof(IniSerializationException), MessageContains: "System.TimeOnly")],
        [new InvalidKat<object>("Int128", (Int128)5, typeof(IniSerializationException), MessageContains: "System.Int128")],
        [new InvalidKat<object>("UInt128", (UInt128)5, typeof(IniSerializationException), MessageContains: "System.UInt128")],
        [new InvalidKat<object>("Half", (Half)1, typeof(IniSerializationException), MessageContains: "System.Half")],
        [new InvalidKat<object>("BigInteger", new BigInteger(7), typeof(IniSerializationException), MessageContains: "System.Numerics.BigInteger")],
        [new InvalidKat<object>("char", 'c', typeof(IniSerializationException), MessageContains: "System.Char")],
    ];

    /// <summary>
    /// Gets scalar root types, including nullable forms, each of which deserialization must reject with
    /// <see cref="IniSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named scalar type and the exception it raises.</value>
    public static IEnumerable<object[]> ScalarRootTypes =>
    [
        [new InvalidKat<Type>("decimal", typeof(decimal), typeof(IniSerializationException), MessageContains: "System.Decimal")],
        [new InvalidKat<Type>("DateTime", typeof(DateTime), typeof(IniSerializationException), MessageContains: "System.DateTime")],
        [new InvalidKat<Type>("DateTimeOffset", typeof(DateTimeOffset), typeof(IniSerializationException), MessageContains: "System.DateTimeOffset")],
        [new InvalidKat<Type>("TimeSpan", typeof(TimeSpan), typeof(IniSerializationException), MessageContains: "System.TimeSpan")],
        [new InvalidKat<Type>("Guid", typeof(Guid), typeof(IniSerializationException), MessageContains: "System.Guid")],
        [new InvalidKat<Type>("Uri", typeof(Uri), typeof(IniSerializationException), MessageContains: "System.Uri")],
        [new InvalidKat<Type>("DateOnly", typeof(DateOnly), typeof(IniSerializationException), MessageContains: "System.DateOnly")],
        [new InvalidKat<Type>("TimeOnly", typeof(TimeOnly), typeof(IniSerializationException), MessageContains: "System.TimeOnly")],
        [new InvalidKat<Type>("Int128", typeof(Int128), typeof(IniSerializationException), MessageContains: "System.Int128")],
        [new InvalidKat<Type>("UInt128", typeof(UInt128), typeof(IniSerializationException), MessageContains: "System.UInt128")],
        [new InvalidKat<Type>("Half", typeof(Half), typeof(IniSerializationException), MessageContains: "System.Half")],
        [new InvalidKat<Type>("BigInteger", typeof(BigInteger), typeof(IniSerializationException), MessageContains: "System.Numerics.BigInteger")],
        [new InvalidKat<Type>("int?", typeof(int?), typeof(IniSerializationException), MessageContains: "System.Nullable")],
        [new InvalidKat<Type>("Guid?", typeof(Guid?), typeof(IniSerializationException), MessageContains: "System.Nullable")],
        [new InvalidKat<Type>("DateOnly?", typeof(DateOnly?), typeof(IniSerializationException), MessageContains: "System.Nullable")],
    ];

    /// <summary>
    /// Deserializes text into a root of a type known only at run time, through
    /// <see cref="IniSerializer.Deserialize{T}(string, IniSerializerOptions?)" />, rethrowing the serializer's own exception.
    /// </summary>
    /// <param name="rootType">The root type.</param>
    /// <param name="text">The text to deserialize.</param>
    /// <returns>The deserialized root.</returns>
    private static object? DeserializeAs(Type rootType, string text)
    {
        MethodInfo method = typeof(IniSerializer)
            .GetMethod(nameof(IniSerializer.Deserialize), [typeof(string), typeof(IniSerializerOptions)])!
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
