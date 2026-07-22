// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
