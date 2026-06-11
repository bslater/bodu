// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.SystemTextJsonParity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that the <see cref="TomlSerializer" /> reproduces the observable behavior of
/// <see cref="System.Text.Json.JsonSerializer" /> for the options and attributes both libraries share, running the
/// same model through both serializers and comparing the outcome.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that the camelCase naming policy lowercases the leading character of a Pascal-cased member name in the
    /// same way <see cref="System.Text.Json.JsonNamingPolicy.CamelCase" /> does.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCamelCasePolicy_ShouldMatchSystemTextJsonKeyCasing()
    {
        var model = new ParityModel { ServerName = "alpha", MaxConnections = 5 };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string json = JsonSerializer.Serialize(model, jsonOptions);

        var tomlOptions = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.CamelCase };
        string toml = TomlSerializer.Serialize(model, tomlOptions);

        Assert.IsTrue(json.Contains("\"serverName\"", StringComparison.Ordinal), "System.Text.Json emitted camelCase key.");
        Assert.IsTrue(toml.Contains("serverName = ", StringComparison.Ordinal), "TomlSerializer should emit the same camelCase key.");
        Assert.IsTrue(toml.Contains("maxConnections = ", StringComparison.Ordinal), "TomlSerializer should camel-case every member.");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions.DefaultIgnoreCondition" /> set to
    /// <see cref="TomlIgnoreCondition.WhenWritingNull" /> omits a null member, matching
    /// <see cref="JsonIgnoreCondition.WhenWritingNull" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIgnoreNullCondition_ShouldOmitMemberLikeSystemTextJson()
    {
        var model = new NullableParityModel { Present = "value", Absent = null };

        var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        string json = JsonSerializer.Serialize(model, jsonOptions);

        var tomlOptions = new TomlSerializerOptions { DefaultIgnoreCondition = TomlIgnoreCondition.WhenWritingNull };
        string toml = TomlSerializer.Serialize(model, tomlOptions);

        Assert.IsFalse(json.Contains("Absent", StringComparison.Ordinal), "System.Text.Json omitted the null member.");
        Assert.IsFalse(toml.Contains("Absent", StringComparison.Ordinal), "TomlSerializer should omit the null member as well.");
        Assert.IsTrue(toml.Contains("Present = ", StringComparison.Ordinal), "TomlSerializer should retain the non-null member.");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions" /> becomes read-only after the first serialization and rejects
    /// further mutation with <see cref="InvalidOperationException" />, matching the freeze-after-first-use contract of
    /// <see cref="JsonSerializerOptions" />.
    /// </summary>
    [TestMethod]
    public void Options_WhenMutatedAfterFirstUse_ShouldThrowInvalidOperationExceptionLikeSystemTextJson()
    {
        var jsonOptions = new JsonSerializerOptions();
        _ = JsonSerializer.Serialize(new ParityModel(), jsonOptions);
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => jsonOptions.PropertyNameCaseInsensitive = true);

        var tomlOptions = new TomlSerializerOptions();
        _ = TomlSerializer.Serialize(new ParityModel(), tomlOptions);
        Assert.IsTrue(tomlOptions.IsReadOnly, "TomlSerializerOptions should be read-only after first use.");
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => tomlOptions.PropertyNameCaseInsensitive = true);
    }

    /// <summary>
    /// Verifies that a member-level property-name attribute overrides the active naming policy in the same way
    /// <see cref="JsonPropertyNameAttribute" /> overrides <see cref="JsonSerializerOptions.PropertyNamingPolicy" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenPropertyNameAttributePresent_ShouldOverridePolicyLikeSystemTextJson()
    {
        var model = new AttributeOverrideModel { Value = 7 };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string json = JsonSerializer.Serialize(model, jsonOptions);

        var tomlOptions = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.CamelCase };
        string toml = TomlSerializer.Serialize(model, tomlOptions);

        Assert.IsTrue(json.Contains("\"explicit_name\"", StringComparison.Ordinal), "System.Text.Json honored the attribute over the policy.");
        Assert.IsTrue(toml.Contains("explicit_name = ", StringComparison.Ordinal), "TomlSerializer should honor the attribute over the policy.");
    }

    /// <summary>
    /// A model with two Pascal-cased members used to compare naming-policy output across serializers.
    /// </summary>
    private sealed class ParityModel
    {
        /// <summary>
        /// Gets or sets the server-name member.
        /// </summary>
        /// <returns>The server name.</returns>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum-connections member.
        /// </summary>
        /// <returns>The maximum number of connections.</returns>
        public int MaxConnections { get; set; }
    }

    /// <summary>
    /// A model carrying a present and an absent (null) member used to compare null-ignore behavior.
    /// </summary>
    private sealed class NullableParityModel
    {
        /// <summary>
        /// Gets or sets the present member.
        /// </summary>
        /// <returns>The present value.</returns>
        public string? Present { get; set; }

        /// <summary>
        /// Gets or sets the absent member.
        /// </summary>
        /// <returns>The absent value, or <see langword="null" />.</returns>
        public string? Absent { get; set; }
    }

    /// <summary>
    /// A model whose member name is fixed by an attribute that must override any naming policy.
    /// </summary>
    private sealed class AttributeOverrideModel
    {
        /// <summary>
        /// Gets or sets the value member, whose wire name is fixed by attribute.
        /// </summary>
        /// <returns>The value.</returns>
        [TomlPropertyName("explicit_name")]
        [JsonPropertyName("explicit_name")]
        public int Value { get; set; }
    }
}
