// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.SystemTextJsonParity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
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

        var tomlOptions = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.CamelCase };
        string toml = TomlSerializer.Serialize(model, tomlOptions);

        Assert.IsTrue(json.Contains("\"serverName\"", StringComparison.Ordinal), "System.Text.Json emitted camelCase key.");
        Assert.IsTrue(toml.Contains("serverName = ", StringComparison.Ordinal), "TomlSerializer should emit the same camelCase key.");
        Assert.IsTrue(toml.Contains("maxConnections = ", StringComparison.Ordinal), "TomlSerializer should camel-case every member.");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializerOptions.DefaultIgnoreCondition" /> set to
    /// <see cref="IgnoreCondition.WhenWritingNull" /> omits a null member, matching
    /// <see cref="JsonIgnoreCondition.WhenWritingNull" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIgnoreNullCondition_ShouldOmitMemberLikeSystemTextJson()
    {
        var model = new NullableParityModel { Present = "value", Absent = null };

        var jsonOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        string json = JsonSerializer.Serialize(model, jsonOptions);

        var tomlOptions = new TomlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull };
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

        var tomlOptions = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.CamelCase };
        string toml = TomlSerializer.Serialize(model, tomlOptions);

        Assert.IsTrue(json.Contains("\"explicit_name\"", StringComparison.Ordinal), "System.Text.Json honored the attribute over the policy.");
        Assert.IsTrue(toml.Contains("explicit_name = ", StringComparison.Ordinal), "TomlSerializer should honor the attribute over the policy.");
    }

    /// <summary>
    /// Verifies that a <see cref="Version" /> serializes to the same string content as
    /// <see cref="System.Text.Json.JsonSerializer" /> and that both reject the same padded input on read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenVersion_ShouldMatchSystemTextJsonStringForm()
    {
        var value = Version.Parse("1.2.3.4");

        string json = JsonSerializer.Serialize(new ValueModel<Version> { Value = value });
        string toml = TomlSerializer.Serialize(new ValueModel<Version> { Value = value });

        Assert.IsTrue(json.Contains("\"1.2.3.4\"", StringComparison.Ordinal), "System.Text.Json emitted the component string.");
        Assert.IsTrue(toml.Contains("\"1.2.3.4\"", StringComparison.Ordinal), "TomlSerializer should emit the same component string.");

        _ = Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<ValueModel<Version>>("{\"Value\":\" 1.2.3\"}"));
        _ = Assert.ThrowsExactly<TomlSerializationException>(() => TomlSerializer.Deserialize<ValueModel<Version>>("Value = \" 1.2.3\"\n"));
    }

    /// <summary>
    /// Verifies that a <see cref="TimeSpan" /> serializes to the same constant-format string content as
    /// <see cref="System.Text.Json.JsonSerializer" /> and round-trips through both serializers to the same value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTimeSpan_ShouldMatchSystemTextJsonConstantFormat()
    {
        var value = new TimeSpan(1, 2, 3, 4, 567);

        string json = JsonSerializer.Serialize(new ValueModel<TimeSpan> { Value = value });
        string toml = TomlSerializer.Serialize(new ValueModel<TimeSpan> { Value = value });

        Assert.IsTrue(json.Contains("\"1.02:03:04.5670000\"", StringComparison.Ordinal), "System.Text.Json emitted the constant format.");
        Assert.IsTrue(toml.Contains("\"1.02:03:04.5670000\"", StringComparison.Ordinal), "TomlSerializer should emit the same constant format.");

        Assert.AreEqual(
            JsonSerializer.Deserialize<ValueModel<TimeSpan>>(json)!.Value,
            TomlSerializer.Deserialize<ValueModel<TimeSpan>>(toml).Value);
    }

    /// <summary>
    /// Verifies that a <see cref="Half" /> round-trips through both serializers to the same value, each through its
    /// native numeric form.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenHalf_ShouldRoundTripLikeSystemTextJson()
    {
        var value = (Half)1.5;

        string json = JsonSerializer.Serialize(new ValueModel<Half> { Value = value });
        string toml = TomlSerializer.Serialize(new ValueModel<Half> { Value = value });

        Assert.AreEqual(
            JsonSerializer.Deserialize<ValueModel<Half>>(json)!.Value,
            TomlSerializer.Deserialize<ValueModel<Half>>(toml).Value);
    }

    /// <summary>
    /// Verifies that a full-precision <see cref="decimal" /> under <see cref="TomlDecimalHandling.String" /> preserves
    /// the same digit text <see cref="System.Text.Json.JsonSerializer" /> emits as a JSON number, and that both
    /// round-trip the value exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDecimalStringHandling_ShouldPreservePrecisionLikeSystemTextJson()
    {
        const decimal value = 0.1234567890123456789012345678m;

        string json = JsonSerializer.Serialize(new ValueModel<decimal> { Value = value });
        var tomlOptions = new TomlSerializerOptions { DecimalHandling = TomlDecimalHandling.String };
        string toml = TomlSerializer.Serialize(new ValueModel<decimal> { Value = value }, tomlOptions);

        Assert.IsTrue(json.Contains("0.1234567890123456789012345678", StringComparison.Ordinal), "System.Text.Json preserved the digits as a JSON number.");
        Assert.IsTrue(toml.Contains("0.1234567890123456789012345678", StringComparison.Ordinal), "TomlSerializer should preserve the same digits in its string form.");

        Assert.AreEqual(
            JsonSerializer.Deserialize<ValueModel<decimal>>(json)!.Value,
            TomlSerializer.Deserialize<ValueModel<decimal>>(toml, tomlOptions).Value);
    }

    /// <summary>
    /// Verifies that an <see cref="object" />-typed member dispatches on its runtime type in both serializers, emitting
    /// the boxed value's native form rather than an empty object.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenObjectMemberHoldsBoxedValue_ShouldDispatchLikeSystemTextJson()
    {
        string json = JsonSerializer.Serialize(new ValueModel<object> { Value = 5 });
        string toml = TomlSerializer.Serialize(new ValueModel<object> { Value = 5 });

        Assert.IsTrue(json.Contains(":5", StringComparison.Ordinal), "System.Text.Json wrote the boxed integer through its runtime type.");
        Assert.IsTrue(toml.Contains("Value = 5", StringComparison.Ordinal), "TomlSerializer should write the boxed integer through its runtime type.");
    }

    /// <summary>
    /// A model with two Pascal-cased members used to compare naming-policy output across serializers.
    /// </summary>
    private sealed class ParityModel
    {
        /// <summary>
        /// Gets or sets the server-name member.
        /// </summary>
        /// <value>The server name.</value>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum-connections member.
        /// </summary>
        /// <value>The maximum number of connections.</value>
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
        /// <value>The present value.</value>
        public string? Present { get; set; }

        /// <summary>
        /// Gets or sets the absent member.
        /// </summary>
        /// <value>The absent value, or <see langword="null" />.</value>
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
        /// <value>The value.</value>
        [PropertyName("explicit_name")]
        [JsonPropertyName("explicit_name")]
        public int Value { get; set; }
    }
}
