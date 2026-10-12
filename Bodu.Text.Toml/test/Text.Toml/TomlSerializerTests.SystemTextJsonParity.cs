// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.SystemTextJsonParity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Globalization;
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

#if NET9_0_OR_GREATER
    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values rejects a string that
    /// holds an integer, as <see cref="JsonStringEnumConverter" /> configured the same way does from .NET 9.
    /// </summary>
    /// <param name="text">The string's text.</param>
    [TestMethod]
    [DataRow("2", DisplayName = "the digits of a defined value")]
    [DataRow(" +2 ", DisplayName = "a signed integer between spaces")]
    [DataRow("-1", DisplayName = "a negative integer")]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndTextHoldsInteger_ShouldRejectLikeSystemTextJson(
        string text)
    {
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        var tomlOptions = new TomlSerializerOptions();
        tomlOptions.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<StatusModel>($"{{\"Status\":\"{text}\"}}", jsonOptions);
        });
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<StatusModel>($"Status = \"{text}\"\n", tomlOptions);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> with a naming policy writes a member, and a combination of
    /// flags one of which a <see cref="StringEnumMemberNameAttribute" /> renames, as the text
    /// <see cref="JsonStringEnumConverter" /> writes for the same value under the same policy.
    /// </summary>
    /// <param name="access">The underlying value of the <see cref="ParityAccessFlags" /> member.</param>
    /// <param name="renamed">The underlying value of the <see cref="ParityRenamedFlags" /> member.</param>
    /// <param name="toggle">The underlying value of the <see cref="ParityToggle" /> member.</param>
    [TestMethod]
    [DataRow(5, 3, 1, DisplayName = "two single flags, a renamed flag with another, and a renamed member")]
    [DataRow(7, 1, 0, DisplayName = "a composite member and a flag, a renamed flag, and an unrenamed member")]
    [DataRow(6, 2, 1, DisplayName = "two other single flags, an unrenamed flag, and a renamed member")]
    [DataRow(3, 0, 0, DisplayName = "a composite member, and two zero values")]
    public void Serialize_WhenStringEnumConverterHasNamingPolicy_ShouldWriteNamesLikeSystemTextJson(int access, int renamed, int toggle)
    {
        var model = new EnumParityModel { Access = (ParityAccessFlags)access, Renamed = (ParityRenamedFlags)renamed, Toggle = (ParityToggle)toggle };
        using JsonDocument json = JsonDocument.Parse(JsonSerializer.Serialize(model, SnakeCaseJsonEnumOptions()));
        string expected = string.Concat(
            json.RootElement.EnumerateObject().Select(static property => $"{property.Name} = \"{property.Value.GetString()}\"\n"));

        string toml = TomlSerializer.Serialize(model, SnakeCaseEnumOptions(allowIntegerValues: true));

        Assert.AreEqual(expected, toml);
    }

    /// <summary>
    /// Verifies that text <see cref="JsonStringEnumConverter" /> binds under a naming policy binds to the same value
    /// through a <see cref="TomlStringEnumConverter" /> with the same policy.
    /// </summary>
    /// <param name="member">The model member the text is read into.</param>
    /// <param name="text">The string's text.</param>
    [TestMethod]
    [DataRow(nameof(EnumParityModel.Access), "can_read, can_execute", DisplayName = "two wire names")]
    [DataRow(nameof(EnumParityModel.Access), "can_execute, can_read", DisplayName = "two wire names in descending order")]
    [DataRow(nameof(EnumParityModel.Access), " can_read , can_execute ", DisplayName = "two wire names between spaces")]
    [DataRow(nameof(EnumParityModel.Access), "can_read_write, can_execute", DisplayName = "a composite member's wire name and a flag")]
    [DataRow(nameof(EnumParityModel.Access), "can_read,", DisplayName = "a wire name and a trailing comma")]
    [DataRow(nameof(EnumParityModel.Access), "CanRead", DisplayName = "an unrenamed member's CLR name")]
    [DataRow(nameof(EnumParityModel.Access), "canread", DisplayName = "an unrenamed member's CLR name in lower case")]
    [DataRow(nameof(EnumParityModel.Access), "CanRead, can_execute", DisplayName = "a CLR name and a wire name")]
    [DataRow(nameof(EnumParityModel.Access), " +1 ", DisplayName = "a whole-text integer")]
    [DataRow(nameof(EnumParityModel.Renamed), "r, write", DisplayName = "a configured name and a wire name")]
    [DataRow(nameof(EnumParityModel.Renamed), "write, r", DisplayName = "a wire name and a configured name")]
    [DataRow(nameof(EnumParityModel.Toggle), "on", DisplayName = "a renamed member's configured name")]
    [DataRow(nameof(EnumParityModel.Toggle), "Disabled", DisplayName = "an unrenamed member's CLR name beside a renamed member")]
    public void Deserialize_WhenSystemTextJsonBindsEnumText_ShouldBindSameValue(string member, string text)
    {
        EnumParityModel json = JsonSerializer.Deserialize<EnumParityModel>(
            $"{{\"{member}\":{JsonSerializer.Serialize(text)}}}", SnakeCaseJsonEnumOptions())!;

        EnumParityModel toml = TomlSerializer.Deserialize<EnumParityModel>(
            $"{member} = \"{text}\"\n", SnakeCaseEnumOptions(allowIntegerValues: true))!;

        Assert.AreEqual(json.ValueOf(member), toml.ValueOf(member));
    }

    /// <summary>
    /// Verifies that text <see cref="JsonStringEnumConverter" /> rejects under a naming policy, with integer values
    /// allowed, is rejected through a <see cref="TomlStringEnumConverter" /> configured the same way.
    /// </summary>
    /// <param name="member">The model member the text is read into.</param>
    /// <param name="text">The string's text.</param>
    [TestMethod]
    [DataRow(nameof(EnumParityModel.Access), "CAN_READ", DisplayName = "a policy name in another case")]
    [DataRow(nameof(EnumParityModel.Access), "can_read, , can_execute", DisplayName = "an empty name between two names")]
    [DataRow(nameof(EnumParityModel.Access), ", can_read", DisplayName = "an empty name before a name")]
    [DataRow(nameof(EnumParityModel.Access), "can_read,,", DisplayName = "a name and two trailing commas")]
    [DataRow(nameof(EnumParityModel.Access), "", DisplayName = "the empty string")]
    [DataRow(nameof(EnumParityModel.Access), "1, 4", DisplayName = "a list of integers")]
    [DataRow(nameof(EnumParityModel.Access), "can_read, 4", DisplayName = "a wire name and an integer")]
    [DataRow(nameof(EnumParityModel.Renamed), "Read", DisplayName = "a renamed flag's CLR name")]
    [DataRow(nameof(EnumParityModel.Renamed), "Read, write", DisplayName = "a renamed flag's CLR name in a list")]
    [DataRow(nameof(EnumParityModel.Toggle), "ON", DisplayName = "a configured name in another case")]
    [DataRow(nameof(EnumParityModel.Toggle), "Enabled", DisplayName = "a renamed member's CLR name")]
    [DataRow(nameof(EnumParityModel.Toggle), "enabled", DisplayName = "a renamed member's CLR name in lower case")]
    public void Deserialize_WhenSystemTextJsonRejectsEnumText_ShouldRejectLikeSystemTextJson(string member, string text)
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<EnumParityModel>($"{{\"{member}\":{JsonSerializer.Serialize(text)}}}", SnakeCaseJsonEnumOptions());
        });
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<EnumParityModel>($"{member} = \"{text}\"\n", SnakeCaseEnumOptions(allowIntegerValues: true));
        });
    }

    /// <summary>
    /// Verifies that an enumeration used as a dictionary key is written with the key text
    /// <see cref="System.Text.Json.JsonSerializer" /> writes for it: a renamed member's configured name, a combination
    /// of flags as its members' names, an undefined value as its decimal text, and, under a string enum converter with a
    /// naming policy, the policy's names.
    /// </summary>
    /// <param name="mode">The enumeration handling, as <see cref="EnumKeyOptions(string)" /> names it.</param>
    [TestMethod]
    [DataRow("default", DisplayName = "default enumeration handling")]
    [DataRow("snake", DisplayName = "a string enum converter with a naming policy")]
    [DataRow("noint", DisplayName = "a string enum converter that disallows integer values")]
    public void Serialize_WhenEnumKeyedDictionary_ShouldWriteKeysLikeSystemTextJson(string mode)
    {
        (JsonSerializerOptions jsonOptions, TomlSerializerOptions tomlOptions) = EnumKeyOptions(mode);
        var toggles = new Dictionary<ParityToggle, int> { [ParityToggle.Enabled] = 1, [ParityToggle.Disabled] = 2 };
        var access = new Dictionary<ParityAccessFlags, int>
        {
            [ParityAccessFlags.CanRead | ParityAccessFlags.CanExecute] = 1,
            [ParityAccessFlags.CanReadWrite] = 2,
            [(ParityAccessFlags)64] = 3,
        };

        CollectionAssert.AreEqual(JsonKeys(toggles, jsonOptions), TomlKeys(toggles, tomlOptions));
        CollectionAssert.AreEqual(JsonKeys(access, jsonOptions), TomlKeys(access, tomlOptions));
    }

    /// <summary>
    /// Verifies that a dictionary key <see cref="System.Text.Json.JsonSerializer" /> binds to an enumeration binds to
    /// the same value through the <see cref="TomlSerializer" /> under the same enumeration handling.
    /// </summary>
    /// <param name="mode">The enumeration handling, as <see cref="EnumKeyOptions(string)" /> names it.</param>
    /// <param name="type">The key enumeration, <see cref="ParityToggle" /> or <see cref="ParityAccessFlags" />.</param>
    /// <param name="key">The key's text.</param>
    [TestMethod]
    [DataRow("default", nameof(ParityToggle), "on", DisplayName = "a renamed member's configured name")]
    [DataRow("default", nameof(ParityToggle), "Disabled", DisplayName = "an unrenamed member's CLR name")]
    [DataRow("default", nameof(ParityToggle), "disabled", DisplayName = "an unrenamed member's CLR name in lower case")]
    [DataRow("default", nameof(ParityToggle), "1", DisplayName = "integer text")]
    [DataRow("default", nameof(ParityAccessFlags), "CanExecute, CanRead", DisplayName = "two CLR names in descending order")]
    [DataRow("default", nameof(ParityAccessFlags), "64", DisplayName = "an undefined value's decimal text")]
    [DataRow("snake", nameof(ParityToggle), "on", DisplayName = "a configured name under a naming policy")]
    [DataRow("snake", nameof(ParityAccessFlags), "can_read, can_execute", DisplayName = "two policy names")]
    [DataRow("snake", nameof(ParityAccessFlags), "can_read_write", DisplayName = "a composite member's policy name")]
    [DataRow("snake", nameof(ParityAccessFlags), "CanRead", DisplayName = "an unrenamed member's CLR name under a naming policy")]
    public void Deserialize_WhenSystemTextJsonBindsEnumKey_ShouldBindSameKey(string mode, string type, string key)
    {
        (JsonSerializerOptions jsonOptions, TomlSerializerOptions tomlOptions) = EnumKeyOptions(mode);

        long json = type == nameof(ParityToggle)
            ? JsonKeyValue<ParityToggle>(key, jsonOptions)
            : JsonKeyValue<ParityAccessFlags>(key, jsonOptions);
        long toml = type == nameof(ParityToggle)
            ? TomlKeyValue<ParityToggle>(key, tomlOptions)
            : TomlKeyValue<ParityAccessFlags>(key, tomlOptions);

        Assert.AreEqual(json, toml);
    }

    /// <summary>
    /// Verifies that a dictionary key <see cref="System.Text.Json.JsonSerializer" /> rejects for an enumeration is
    /// rejected by the <see cref="TomlSerializer" /> under the same enumeration handling.
    /// </summary>
    /// <param name="mode">The enumeration handling, as <see cref="EnumKeyOptions(string)" /> names it.</param>
    /// <param name="type">The key enumeration, <see cref="ParityToggle" /> or <see cref="ParityAccessFlags" />.</param>
    /// <param name="key">The key's text.</param>
    [TestMethod]
    [DataRow("default", nameof(ParityToggle), "Enabled", DisplayName = "a renamed member's CLR name")]
    [DataRow("default", nameof(ParityToggle), "ON", DisplayName = "a configured name in another case")]
    [DataRow("default", nameof(ParityToggle), "Purple", DisplayName = "a name no member has")]
    [DataRow("snake", nameof(ParityAccessFlags), "CAN_READ", DisplayName = "a policy name in another case")]
    [DataRow("noint", nameof(ParityToggle), "1", DisplayName = "integer text when integer values are not allowed")]
    public void Deserialize_WhenSystemTextJsonRejectsEnumKey_ShouldRejectLikeSystemTextJson(string mode, string type, string key)
    {
        (JsonSerializerOptions jsonOptions, TomlSerializerOptions tomlOptions) = EnumKeyOptions(mode);

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = type == nameof(ParityToggle)
                ? JsonKeyValue<ParityToggle>(key, jsonOptions)
                : JsonKeyValue<ParityAccessFlags>(key, jsonOptions);
        });
        _ = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = type == nameof(ParityToggle)
                ? TomlKeyValue<ParityToggle>(key, tomlOptions)
                : TomlKeyValue<ParityAccessFlags>(key, tomlOptions);
        });
    }

    /// <summary>
    /// Creates the matching options of the two serializers for an enumeration handling.
    /// </summary>
    /// <param name="mode">
    /// <c>default</c> for no enumeration converter, <c>snake</c> for a string enum converter with the lower snake-case
    /// naming policy that allows integer values, or <c>noint</c> for a string enum converter without a naming policy that
    /// disallows them.
    /// </param>
    /// <returns>The System.Text.Json and TOML options.</returns>
    private static (JsonSerializerOptions Json, TomlSerializerOptions Toml) EnumKeyOptions(string mode)
    {
        var json = new JsonSerializerOptions();
        var toml = new TomlSerializerOptions();
        if (mode == "snake")
        {
            json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: true));
            toml.Converters.Add(new TomlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues: true));
        }
        else if (mode == "noint")
        {
            json.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
            toml.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        }

        return (json, toml);
    }

    /// <summary>
    /// Returns the keys <see cref="System.Text.Json.JsonSerializer" /> writes for a dictionary, in order.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The written keys.</returns>
    private static string[] JsonKeys<TKey>(Dictionary<TKey, int> dictionary, JsonSerializerOptions options)
        where TKey : notnull
    {
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(dictionary, options));
        return document.RootElement.EnumerateObject().Select(static property => property.Name).ToArray();
    }

    /// <summary>
    /// Returns the table keys the <see cref="TomlSerializer" /> writes for a dictionary, in order.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The written keys.</returns>
    private static string[] TomlKeys<TKey>(Dictionary<TKey, int> dictionary, TomlSerializerOptions options)
        where TKey : notnull =>
        TomlSerializer.Deserialize<Dictionary<string, int>>(TomlSerializer.Serialize(dictionary, options)).Keys.ToArray();

    /// <summary>
    /// Reads a one-entry object through <see cref="System.Text.Json.JsonSerializer" /> and returns its key's underlying
    /// value.
    /// </summary>
    /// <typeparam name="TEnum">The key enumeration.</typeparam>
    /// <param name="key">The key's text.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound key's underlying value.</returns>
    private static long JsonKeyValue<TEnum>(string key, JsonSerializerOptions options)
        where TEnum : struct, Enum =>
        Convert.ToInt64(
            JsonSerializer.Deserialize<Dictionary<TEnum, int>>($"{{{JsonSerializer.Serialize(key)}:1}}", options)!.Keys.Single(),
            CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a one-entry table through the <see cref="TomlSerializer" /> and returns its key's underlying value.
    /// </summary>
    /// <typeparam name="TEnum">The key enumeration.</typeparam>
    /// <param name="key">The key's text, written as a quoted key.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The bound key's underlying value.</returns>
    private static long TomlKeyValue<TEnum>(string key, TomlSerializerOptions options)
        where TEnum : struct, Enum =>
        Convert.ToInt64(
            TomlSerializer.Deserialize<Dictionary<TEnum, int>>($"\"{key}\" = 1\n", options).Keys.Single(),
            CultureInfo.InvariantCulture);

    /// <summary>
    /// Creates options that convert enumerations by name through a <see cref="JsonStringEnumConverter" /> with the lower
    /// snake-case naming policy, allowing integer values, the counterpart of
    /// <see cref="SnakeCaseEnumOptions(bool)" />.
    /// </summary>
    /// <returns>The options.</returns>
    private static JsonSerializerOptions SnakeCaseJsonEnumOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: true));
        return options;
    }

    /// <summary>
    /// A flags enumeration whose members' names change under a naming policy, with a composite member, for comparing the
    /// two serializers.
    /// </summary>
    [Flags]
    private enum ParityAccessFlags
    {
        /// <summary>
        /// No access.
        /// </summary>
        None = 0,

        /// <summary>
        /// Read access.
        /// </summary>
        CanRead = 1,

        /// <summary>
        /// Write access.
        /// </summary>
        CanWrite = 2,

        /// <summary>
        /// Read and write access, the composite of <see cref="CanRead" /> and <see cref="CanWrite" />.
        /// </summary>
        CanReadWrite = 3,

        /// <summary>
        /// Execute access.
        /// </summary>
        CanExecute = 4,
    }

    /// <summary>
    /// A flags enumeration one of whose members both serializers' attributes rename.
    /// </summary>
    [Flags]
    private enum ParityRenamedFlags
    {
        /// <summary>
        /// No flag.
        /// </summary>
        None = 0,

        /// <summary>
        /// The read flag, written under the name <c>r</c>.
        /// </summary>
        [StringEnumMemberName("r")]
        [JsonStringEnumMemberName("r")]
        Read = 1,

        /// <summary>
        /// The write flag.
        /// </summary>
        Write = 2,
    }

    /// <summary>
    /// An enumeration one of whose members both serializers' attributes rename.
    /// </summary>
    private enum ParityToggle
    {
        /// <summary>
        /// The disabled state.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// The enabled state, written under the name <c>on</c>.
        /// </summary>
        [StringEnumMemberName("on")]
        [JsonStringEnumMemberName("on")]
        Enabled = 1,
    }

    /// <summary>
    /// A model carrying one member of each parity enumeration.
    /// </summary>
    private sealed class EnumParityModel
    {
        /// <summary>
        /// Gets or sets the access flags.
        /// </summary>
        /// <value>The access flags.</value>
        public ParityAccessFlags Access { get; set; }

        /// <summary>
        /// Gets or sets the renamed flags.
        /// </summary>
        /// <value>The renamed flags.</value>
        public ParityRenamedFlags Renamed { get; set; }

        /// <summary>
        /// Gets or sets the toggle.
        /// </summary>
        /// <value>The toggle.</value>
        public ParityToggle Toggle { get; set; }

        /// <summary>
        /// Returns the underlying value of the member with the given name.
        /// </summary>
        /// <param name="member">The member's name.</param>
        /// <returns>The member's underlying value.</returns>
        public long ValueOf(string member) => member switch
        {
            nameof(Access) => (long)Access,
            nameof(Renamed) => (long)Renamed,
            _ => (long)Toggle,
        };
    }
#endif

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
