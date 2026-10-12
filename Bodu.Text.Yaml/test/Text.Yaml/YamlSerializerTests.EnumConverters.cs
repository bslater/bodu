// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.EnumConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the System.Text.Json-style enum-converter surface for YAML: the built-in by-name default, the
/// <see cref="StringEnumMemberNameAttribute" /> override, the <see cref="YamlStringEnumConverter" />,
/// <see cref="YamlStringEnumConverter{TEnum}" />, and <see cref="YamlNumberEnumConverter{TEnum}" /> factories, and the
/// interplay with <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that an enumeration with no explicit converter serializes as its member-name scalar by default and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDefaultEnum_ShouldWriteMemberNameScalarAndRoundTrip()
    {
        var model = new StatusModel { Status = Status.Active };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Status: Active\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text)!;
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that an enumeration serializes as its numeric value when
    /// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> is disabled, and round-trips through the integer path.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenWriteEnumsAsStringsDisabled_ShouldWriteIntegerAndRoundTrip()
    {
        var options = new YamlSerializerOptions { WriteEnumsAsStrings = false };

        var model = new StatusModel { Status = Status.Archived };
        string text = YamlSerializer.Serialize(model, options);

        Assert.AreEqual("Status: 2\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options)!;
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a bare enumeration value at the document root, which YAML permits as a root scalar, serializes as
    /// its member-name scalar and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRootIsEnum_ShouldWriteRootScalarAndRoundTrip()
    {
        string text = YamlSerializer.Serialize(Status.Pending);

        Assert.AreEqual("Pending\n", text);

        Status roundTripped = YamlSerializer.Deserialize<Status>(text);
        Assert.AreEqual(Status.Pending, roundTripped);
    }

    /// <summary>
    /// Verifies that the default by-name converter accepts a YAML integer scalar on read and converts it to the
    /// corresponding numeric enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDefaultEnumReadsInteger_ShouldReadIntegerAsEnum()
    {
        StatusModel model = YamlSerializer.Deserialize<StatusModel>("Status: 2\n")!;

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that <see cref="StringEnumMemberNameAttribute" /> overrides the string used for an individual
    /// enumeration member on both write and read, even with no converter explicitly registered.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemberNameAttributePresent_ShouldUseOverriddenName()
    {
        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Status: not-found\n", text);

        RenamedStatusModel roundTripped = YamlSerializer.Deserialize<RenamedStatusModel>(text)!;
        Assert.AreEqual(RenamedStatus.NotFound, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> registered with a camel-case naming policy writes a
    /// Pascal-case member as its camel-cased name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterCamelCasePolicy_ShouldCamelCaseAndRoundTrip()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        string text = YamlSerializer.Serialize(model, options);

        Assert.AreEqual("Status: active\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options)!;
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> throws <see cref="YamlSerializationException" /> when it
    /// reads a string that matches no member name and is not otherwise parseable.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterReadsUnknownName_ShouldThrowYamlSerializationException()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<StatusModel>("Status: unknown\n", options);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="YamlSerializationException" /> when it reads a YAML integer scalar for the enumeration.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegers_ShouldThrowYamlSerializationException()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<StatusModel>("Status: 2\n", options);
        });
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="YamlNumberEnumConverter{TEnum}" /> serializes the enumeration as a YAML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterAttribute_ShouldWriteIntegerAndRoundTrip()
    {
        var model = new NumberEnumModel { Status = Status.Archived };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Status: 2\n", text);

        NumberEnumModel roundTripped = YamlSerializer.Deserialize<NumberEnumModel>(text)!;
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="YamlStringEnumConverter{TEnum}" /> serializes the enumeration as its member-name scalar and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterAttribute_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new StringEnumModel { Status = Status.Pending };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Status: Pending\n", text);

        StringEnumModel roundTripped = YamlSerializer.Deserialize<StringEnumModel>(text)!;
        Assert.AreEqual(Status.Pending, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a non-generic <see cref="YamlStringEnumConverter" /> registered without a naming policy writes a
    /// member as its unchanged CLR name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterNoPolicy_ShouldUseMemberName()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter());

        var model = new StatusModel { Status = Status.Active };
        string text = YamlSerializer.Serialize(model, options);

        Assert.AreEqual("Status: Active\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options)!;
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter matches a member name case-insensitively on read, so an upper-cased wire
    /// scalar binds to a Pascal-case member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumStringCaseDiffers_ShouldMatchCaseInsensitively()
    {
        StatusModel model = YamlSerializer.Deserialize<StatusModel>("Status: ACTIVE\n")!;

        Assert.AreEqual(Status.Active, model.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter reads a numeric string by parsing it through the runtime, so a member
    /// stored as its quoted decimal text still binds to the enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumReadsNumericString_ShouldParseToValue()
    {
        StatusModel model = YamlSerializer.Deserialize<StatusModel>("Status: \"2\"\n")!;

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="YamlSerializationException" /> at the scalar, with its line, column, offset and path, when the scalar
    /// is not an integer but its text holds one, alone or as one of its comma-separated parts, rather than binding the
    /// integer it holds.
    /// </summary>
    /// <param name="scalar">The scalar, as the document spells it.</param>
    /// <param name="offset">
    /// The zero-based offset of the scalar on its line, which is also its offset in the document: after
    /// <c>Flags: </c>, and after the tag of a tagged scalar.
    /// </param>
    [TestMethod]
    [DataRow("\"2\"", 7, DisplayName = "the double-quoted digits of a defined value")]
    [DataRow("'99'", 7, DisplayName = "the single-quoted digits of an undefined value")]
    [DataRow("!!str 2", 13, DisplayName = "digits tagged as a string")]
    [DataRow("\" +2 \"", 7, DisplayName = "a signed integer between spaces")]
    [DataRow("\"-1\"", 7, DisplayName = "a negative integer")]
    [DataRow("\"2\\0\"", 7, DisplayName = "an integer followed by a null character")]
    [DataRow("1, 2", 7, DisplayName = "a list of integers")]
    [DataRow("Read, 2", 7, DisplayName = "a member name and an integer")]
    [DataRow("18446744073709551616", 7, DisplayName = "a plain integer beyond 64 bits")]
    [DataRow("2.0", 7, DisplayName = "an integral float")]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndScalarHoldsInteger_ShouldThrowYamlSerializationException(
        string scalar,
        int offset)
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        string document = $"Flags: {scalar}\n";

        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<FlagsModel>(document, options);
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(offset + 1, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(offset, ex.Offset, "Offset");
        Assert.AreEqual("Flags", ex.Path, "Path");
        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter{TEnum}" /> configured to reject integer values throws
    /// <see cref="YamlSerializationException" /> when the scalar's text holds an integer, as the converter the
    /// non-generic factory produces does.
    /// </summary>
    /// <param name="scalar">The scalar, as the document spells it.</param>
    [TestMethod]
    [DataRow("\"2\"", DisplayName = "the double-quoted digits of a defined value")]
    [DataRow("Read, 2", DisplayName = "a member name and an integer")]
    public void Deserialize_WhenGenericStringEnumConverterDisallowsIntegersAndScalarHoldsInteger_ShouldThrowYamlSerializationException(
        string scalar)
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter<PermissionFlags>(namingPolicy: null, allowIntegerValues: false));
        string document = $"Flags: {scalar}\n";

        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<FlagsModel>(document, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> configured to reject integer values still binds a string
    /// scalar that is a member's wire name when that name is an integer's text, because the wire names are matched
    /// before the text is checked for an integer.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndWireNameIsInteger_ShouldBindMember()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        MedalModel model = YamlSerializer.Deserialize<MedalModel>("Medal: '1'\n", options)!;

        Assert.AreEqual(Medal.Gold, model.Medal);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> configured to reject integer values writes a member whose
    /// wire name is an integer's text as a quoted string scalar, which reads back as that member rather than as an
    /// integer.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterDisallowsIntegersAndWireNameIsInteger_ShouldRoundTripMember()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        string text = YamlSerializer.Serialize(new MedalModel { Medal = Medal.Gold }, options);

        Assert.AreEqual("Medal: \"1\"\n", text);

        MedalModel roundTripped = YamlSerializer.Deserialize<MedalModel>(text, options)!;
        Assert.AreEqual(Medal.Gold, roundTripped.Medal);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlStringEnumConverter" /> configured to allow integer values binds a scalar whose
    /// text holds an integer as the value it holds, as the default handling does.
    /// </summary>
    /// <param name="scalar">The scalar, as the document spells it.</param>
    /// <param name="expected">The underlying value the scalar holds.</param>
    [TestMethod]
    [DataRow("\"2\"", 2, DisplayName = "the double-quoted digits of a defined value")]
    [DataRow("'99'", 99, DisplayName = "the single-quoted digits of an undefined value")]
    [DataRow("\" +2 \"", 2, DisplayName = "a signed integer between spaces")]
    [DataRow("2.0", 2, DisplayName = "an integral float")]
    public void Deserialize_WhenStringEnumConverterAllowsIntegersAndScalarHoldsInteger_ShouldBindValue(string scalar, int expected)
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(namingPolicy: null, allowIntegerValues: true));

        FlagsModel model = YamlSerializer.Deserialize<FlagsModel>($"Flags: {scalar}\n", options)!;

        Assert.AreEqual((PermissionFlags)expected, model.Flags);
    }

    /// <summary>
    /// Verifies that the generic <see cref="YamlStringEnumConverter{TEnum}" /> registered with a camel-case naming
    /// policy on the options writes a Pascal-case member as its camel-cased name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGenericStringEnumConverterWithPolicy_ShouldCamelCaseAndRoundTrip()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter<Status>(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        string text = YamlSerializer.Serialize(model, options);

        Assert.AreEqual("Status: active\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options)!;
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="YamlNumberEnumConverter{TEnum}" /> registered on the options writes the enumeration
    /// as a YAML integer regardless of <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />, and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterRegistered_ShouldWriteIntegerAndRoundTrip()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlNumberEnumConverter<Status>());

        var model = new StatusModel { Status = Status.Archived };
        string text = YamlSerializer.Serialize(model, options);

        Assert.AreEqual("Status: 2\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options)!;
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property whose <see cref="YamlNumberEnumConverter{TEnum}" /> reads a non-integer scalar throws
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNumberEnumConverterReadsString_ShouldThrowYamlSerializationException()
    {
        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<NumberEnumModel>("Status: Archived\n");
        });
    }

    /// <summary>
    /// Verifies that a null scalar read by a <see cref="YamlNumberEnumConverter{TEnum}" /> yields the enumeration's
    /// default value, matching YAML's null-scalar handling across the scalar converters.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNumberEnumConverterReadsNull_ShouldReturnDefault()
    {
        NumberEnumModel model = YamlSerializer.Deserialize<NumberEnumModel>("Status: null\n")!;

        Assert.AreEqual(Status.Pending, model.Status);
    }

    /// <summary>
    /// Verifies that an undefined enumeration value, which corresponds to no member, is written by the by-name
    /// converter as its quoted decimal text and read back to the same undefined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUndefinedEnumValue_ShouldWriteDecimalTextAndRoundTrip()
    {
        var model = new StatusModel { Status = (Status)99 };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Status: \"99\"\n", text);

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text)!;
        Assert.AreEqual((Status)99, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a combination of <see cref="PermissionFlags" /> values, which corresponds to no single member, is
    /// written by the by-name converter as its comma-separated member list and read back to the same combined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCombinedFlags_ShouldWriteFlagListAndRoundTrip()
    {
        var model = new FlagsModel { Flags = PermissionFlags.Read | PermissionFlags.Write };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Flags: Read, Write\n", text);

        FlagsModel roundTripped = YamlSerializer.Deserialize<FlagsModel>(text)!;
        Assert.AreEqual(PermissionFlags.Read | PermissionFlags.Write, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that a single <see cref="PermissionFlags" /> member is written as its member name and round-trips,
    /// confirming the by-name path applies to flags enumerations whose value maps to one member.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSingleFlag_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new FlagsModel { Flags = PermissionFlags.Write };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Flags: Write\n", text);

        FlagsModel roundTripped = YamlSerializer.Deserialize<FlagsModel>(text)!;
        Assert.AreEqual(PermissionFlags.Write, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that <see cref="StringEnumMemberNameAttribute" /> takes precedence over a naming policy applied by
    /// a <see cref="YamlStringEnumConverter" />, so the explicit name is used unchanged.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberNameAttributeAndNamingPolicy_ShouldPreferAttribute()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };
        string text = YamlSerializer.Serialize(model, options);

        Assert.AreEqual("Status: not-found\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="YamlStringEnumConverter.CanConvert(Type)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var converter = new YamlStringEnumConverter();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = converter.CanConvert(null!);
        }, "typeToConvert");
    }

    /// <summary>
    /// Verifies that <see cref="YamlStringEnumConverter.CreateConverter(Type, YamlSerializerOptions)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateConverter_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var converter = new YamlStringEnumConverter();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = converter.CreateConverter(null!, new YamlSerializerOptions());
        }, "typeToConvert");
    }

    /// <summary>
    /// Verifies that an enumeration member whose <see cref="StringEnumMemberNameAttribute" /> name is empty is written as
    /// the empty string, quoted so it does not read as null, and that the empty string reads back as that member.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumMemberNameIsEmpty_ShouldWriteEmptyString()
    {
        string text = YamlSerializer.Serialize(BlankNamedOption.Blank);

        Assert.AreEqual("\"\"\n", text);
        Assert.AreEqual(BlankNamedOption.Blank, YamlSerializer.Deserialize<BlankNamedOption>(text));
    }

    /// <summary>
    /// Verifies that a root enumeration value binds its member name case-insensitively, so <c>onhold</c> and
    /// <c>ONHOLD</c> both read as <see cref="HoldStatus.OnHold" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumNameCaseDiffers_ShouldBindMember()
    {
        Assert.AreEqual(HoldStatus.OnHold, YamlSerializer.Deserialize<HoldStatus>("onhold"));
        Assert.AreEqual(HoldStatus.OnHold, YamlSerializer.Deserialize<HoldStatus>("ONHOLD"));
    }

    /// <summary>
    /// Verifies that an integer outside the range of an enumeration's underlying type is rejected with
    /// <see cref="YamlSerializationException" /> at the integer, with its line, column, offset and path, through the
    /// by-name default and the number converter alike, rather than bound as the value it truncates to.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, one step outside the underlying type's range.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="YamlNumberEnumConverter{TEnum}" />.
    /// </param>
    [TestMethod]
    [DataRow("SByte", "128", false, DisplayName = "sbyte, one above the range, by name")]
    [DataRow("SByte", "-129", false, DisplayName = "sbyte, one below the range, by name")]
    [DataRow("Byte", "256", false, DisplayName = "byte, one above the range, by name")]
    [DataRow("Byte", "-1", false, DisplayName = "byte, one below the range, by name")]
    [DataRow("Int16", "32768", false, DisplayName = "short, one above the range, by name")]
    [DataRow("Int16", "-32769", false, DisplayName = "short, one below the range, by name")]
    [DataRow("Int32", "2147483648", false, DisplayName = "int, one above the range, by name")]
    [DataRow("Int32", "-2147483649", false, DisplayName = "int, one below the range, by name")]
    [DataRow("Int64", "9223372036854775808", false, DisplayName = "long, one above the range, by name")]
    [DataRow("Int64", "-9223372036854775809", false, DisplayName = "long, one below the range, by name")]
    [DataRow("UInt64", "18446744073709551616", false, DisplayName = "ulong, one above the range, by name")]
    [DataRow("UInt64", "-1", false, DisplayName = "ulong, one below the range, by name")]
    [DataRow("SByte", "128", true, DisplayName = "sbyte, one above the range, number converter")]
    [DataRow("SByte", "-129", true, DisplayName = "sbyte, one below the range, number converter")]
    [DataRow("Byte", "256", true, DisplayName = "byte, one above the range, number converter")]
    [DataRow("Byte", "-1", true, DisplayName = "byte, one below the range, number converter")]
    [DataRow("Int16", "32768", true, DisplayName = "short, one above the range, number converter")]
    [DataRow("Int16", "-32769", true, DisplayName = "short, one below the range, number converter")]
    [DataRow("Int32", "2147483648", true, DisplayName = "int, one above the range, number converter")]
    [DataRow("Int32", "-2147483649", true, DisplayName = "int, one below the range, number converter")]
    [DataRow("UInt64", "-1", true, DisplayName = "ulong, one below the range, number converter")]
    public void Deserialize_WhenIntegerIsOutsideEnumUnderlyingRange_ShouldThrowYamlSerializationException(
        string member,
        string literal,
        bool numberConverter)
    {
        string document = $"{member}: {literal}\n";

        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = DeserializeRangeModel(document, numberConverter);
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(member.Length + 3, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(member.Length + 2, ex.Offset, "Offset");
        Assert.AreEqual(member, ex.Path, "Path");
    }

    /// <summary>
    /// Verifies that an integer at either end of the range of an enumeration's underlying type binds as that value,
    /// through the by-name default and the number converter alike.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">
    /// The integer, at one end of the underlying type's range, or for <see cref="ulong" /> through the number converter
    /// the largest integer token.
    /// </param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="YamlNumberEnumConverter{TEnum}" />.
    /// </param>
    [TestMethod]
    [DataRow("SByte", "127", false, DisplayName = "sbyte, the maximum, by name")]
    [DataRow("SByte", "-128", false, DisplayName = "sbyte, the minimum, by name")]
    [DataRow("Byte", "255", false, DisplayName = "byte, the maximum, by name")]
    [DataRow("Byte", "0", false, DisplayName = "byte, the minimum, by name")]
    [DataRow("Int16", "32767", false, DisplayName = "short, the maximum, by name")]
    [DataRow("Int16", "-32768", false, DisplayName = "short, the minimum, by name")]
    [DataRow("Int32", "2147483647", false, DisplayName = "int, the maximum, by name")]
    [DataRow("Int32", "-2147483648", false, DisplayName = "int, the minimum, by name")]
    [DataRow("Int64", "9223372036854775807", false, DisplayName = "long, the maximum, by name")]
    [DataRow("Int64", "-9223372036854775808", false, DisplayName = "long, the minimum, by name")]
    [DataRow("UInt64", "18446744073709551615", false, DisplayName = "ulong, the maximum, by name")]
    [DataRow("UInt64", "0", false, DisplayName = "ulong, the minimum, by name")]
    [DataRow("SByte", "127", true, DisplayName = "sbyte, the maximum, number converter")]
    [DataRow("SByte", "-128", true, DisplayName = "sbyte, the minimum, number converter")]
    [DataRow("Byte", "255", true, DisplayName = "byte, the maximum, number converter")]
    [DataRow("Byte", "0", true, DisplayName = "byte, the minimum, number converter")]
    [DataRow("Int16", "32767", true, DisplayName = "short, the maximum, number converter")]
    [DataRow("Int16", "-32768", true, DisplayName = "short, the minimum, number converter")]
    [DataRow("Int32", "2147483647", true, DisplayName = "int, the maximum, number converter")]
    [DataRow("Int32", "-2147483648", true, DisplayName = "int, the minimum, number converter")]
    [DataRow("Int64", "9223372036854775807", true, DisplayName = "long, the maximum, number converter")]
    [DataRow("Int64", "-9223372036854775808", true, DisplayName = "long, the minimum, number converter")]
    [DataRow("UInt64", "9223372036854775807", true, DisplayName = "ulong, the format's maximum, number converter")]
    [DataRow("UInt64", "0", true, DisplayName = "ulong, the minimum, number converter")]
    public void Deserialize_WhenIntegerIsAtEnumUnderlyingRangeLimit_ShouldBindIt(
        string member,
        string literal,
        bool numberConverter)
    {
        object model = DeserializeRangeModel($"{member}: {literal}\n", numberConverter);

        Assert.AreEqual(literal, UnderlyingText(model, member));
    }

    /// <summary>
    /// Verifies that an integer inside the range of an enumeration's underlying type that names no member binds as that
    /// undefined value, through the by-name default and the number converter alike.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, inside the underlying type's range and naming no member.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="YamlNumberEnumConverter{TEnum}" />.
    /// </param>
    [TestMethod]
    [DataRow("Byte", "99", false, DisplayName = "byte, by name")]
    [DataRow("SByte", "-5", false, DisplayName = "sbyte, by name")]
    [DataRow("Int64", "-123456789012", false, DisplayName = "long, by name")]
    [DataRow("UInt64", "7", false, DisplayName = "ulong, by name")]
    [DataRow("Byte", "99", true, DisplayName = "byte, number converter")]
    [DataRow("SByte", "-5", true, DisplayName = "sbyte, number converter")]
    [DataRow("Int64", "-123456789012", true, DisplayName = "long, number converter")]
    [DataRow("UInt64", "7", true, DisplayName = "ulong, number converter")]
    public void Deserialize_WhenIntegerInRangeNamesNoMember_ShouldBindUndefinedValue(
        string member,
        string literal,
        bool numberConverter)
    {
        object model = DeserializeRangeModel($"{member}: {literal}\n", numberConverter);

        Assert.AreEqual(literal, UnderlyingText(model, member));
    }

    /// <summary>
    /// Verifies that a plain integer above <see cref="long.MaxValue" />, which YAML resolves as a string, binds an
    /// enumeration based on <see cref="ulong" /> through <see cref="YamlNumberEnumConverter{TEnum}" />, as a
    /// <see cref="ulong" /> member reads it.
    /// </summary>
    /// <param name="literal">The plain integer, above <see cref="long.MaxValue" />.</param>
    [TestMethod]
    [DataRow("9223372036854775808", DisplayName = "long.MaxValue + 1")]
    [DataRow("18446744073709551615", DisplayName = "ulong.MaxValue")]
    public void Deserialize_WhenPlainIntegerExceedsInt64_ForUInt64BasedNumberEnum_ShouldBindIt(string literal)
    {
        object model = DeserializeRangeModel($"UInt64: {literal}\n", numberConverter: true);

        Assert.AreEqual(literal, UnderlyingText(model, "UInt64"));
    }

    /// <summary>
    /// Verifies that <see cref="YamlNumberEnumConverter{TEnum}" /> rejects a scalar that is not a plain, untagged
    /// integer with <see cref="YamlSerializationException" />, even for an enumeration based on <see cref="ulong" />
    /// and digits above <see cref="long.MaxValue" />: a quoted string, a string tagged as one, or a plain string that
    /// is not an integer.
    /// </summary>
    /// <param name="scalar">The scalar, as the document spells it.</param>
    [TestMethod]
    [DataRow("\"18446744073709551615\"", DisplayName = "double-quoted digits")]
    [DataRow("'18446744073709551615'", DisplayName = "single-quoted digits")]
    [DataRow("!!str 18446744073709551615", DisplayName = "digits tagged as a string")]
    [DataRow("18446744073709551615x", DisplayName = "plain, not an integer")]
    public void Deserialize_WhenScalarIsNotPlainInteger_ForUInt64BasedNumberEnum_ShouldThrowYamlSerializationException(
        string scalar)
    {
        string document = $"UInt64: {scalar}\n";

        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = DeserializeRangeModel(document, numberConverter: true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="YamlNumberEnumConverter{TEnum}" /> rejects a plain integer above
    /// <see cref="long.MaxValue" /> that is outside the range of the enumeration's underlying type with
    /// <see cref="YamlSerializationException" /> at the scalar, with its line, column, offset and path: one above
    /// <see cref="ulong.MaxValue" /> for an enumeration based on <see cref="ulong" />, and one above
    /// <see cref="long.MaxValue" /> for one based on <see cref="long" />.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The plain integer, outside the underlying type's range.</param>
    [TestMethod]
    [DataRow("UInt64", "18446744073709551616", DisplayName = "ulong, one above the range")]
    [DataRow("Int64", "9223372036854775808", DisplayName = "long, one above the range")]
    public void Deserialize_WhenPlainIntegerExceedsUnderlyingRange_ForNumberEnum_ShouldThrowYamlSerializationException(
        string member,
        string literal)
    {
        string document = $"{member}: {literal}\n";

        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = DeserializeRangeModel(document, numberConverter: true);
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(member.Length + 3, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(member.Length + 2, ex.Offset, "Offset");
        Assert.AreEqual(member, ex.Path, "Path");
    }

    /// <summary>
    /// Verifies that a value of an enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" /> is
    /// written as its plain invariant text, as a <see cref="ulong" /> member writes one, through
    /// <see cref="YamlNumberEnumConverter{TEnum}" /> and through the default handling with
    /// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> off.
    /// </summary>
    /// <param name="value">The underlying value, above <see cref="long.MaxValue" />.</param>
    /// <param name="digits">The value's decimal digits.</param>
    /// <param name="numberConverter">
    /// Whether the member writes through <see cref="YamlNumberEnumConverter{TEnum}" /> rather than the default.
    /// </param>
    [TestMethod]
    [DataRow(9_223_372_036_854_775_808UL, "9223372036854775808", false, DisplayName = "long.MaxValue + 1, default")]
    [DataRow(ulong.MaxValue, "18446744073709551615", false, DisplayName = "ulong.MaxValue, default")]
    [DataRow(9_223_372_036_854_775_808UL, "9223372036854775808", true, DisplayName = "long.MaxValue + 1, number")]
    [DataRow(ulong.MaxValue, "18446744073709551615", true, DisplayName = "ulong.MaxValue, number")]
    public void Serialize_WhenUInt64BasedValueExceedsInt64_ShouldWritePlainInvariantText(
        ulong value,
        string digits,
        bool numberConverter)
    {
        string text = SerializeUnsignedValue((UInt64BasedLevel)value, numberConverter);

        Assert.AreEqual("Value: " + digits + "\n", text);
    }

    /// <summary>
    /// Verifies that a value of an enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" />
    /// round-trips through <see cref="YamlNumberEnumConverter{TEnum}" /> and through the default handling with
    /// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> off.
    /// </summary>
    /// <param name="value">The underlying value, above <see cref="long.MaxValue" />.</param>
    /// <param name="numberConverter">
    /// Whether the member reads and writes through <see cref="YamlNumberEnumConverter{TEnum}" /> rather than the
    /// default.
    /// </param>
    [TestMethod]
    [DataRow(9_223_372_036_854_775_808UL, false, DisplayName = "long.MaxValue + 1, default")]
    [DataRow(ulong.MaxValue, false, DisplayName = "ulong.MaxValue, default")]
    [DataRow(9_223_372_036_854_775_808UL, true, DisplayName = "long.MaxValue + 1, number")]
    [DataRow(ulong.MaxValue, true, DisplayName = "ulong.MaxValue, number")]
    public void SerializeDeserialize_WhenUInt64BasedValueExceedsInt64_ShouldRoundTrip(ulong value, bool numberConverter)
    {
        string text = SerializeUnsignedValue((UInt64BasedLevel)value, numberConverter);

        Assert.AreEqual((UInt64BasedLevel)value, DeserializeUnsignedValue(text, numberConverter));
    }

    /// <summary>
    /// Serializes a value of the enumeration based on <see cref="ulong" /> as a member, through the number converter or
    /// through the default handling with <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> off.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="numberConverter">
    /// Whether the member writes through <see cref="YamlNumberEnumConverter{TEnum}" /> rather than the default.
    /// </param>
    /// <returns>The YAML text.</returns>
    private static string SerializeUnsignedValue(UInt64BasedLevel value, bool numberConverter) =>
        numberConverter
            ? YamlSerializer.Serialize(new NumberUnsignedValueModel { Value = value })
            : YamlSerializer.Serialize(
                new UnsignedValueModel { Value = value },
                new YamlSerializerOptions { WriteEnumsAsStrings = false });

    /// <summary>
    /// Deserializes a member of the enumeration based on <see cref="ulong" />, through the number converter or through
    /// the default handling with <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> off.
    /// </summary>
    /// <param name="text">The YAML text.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="YamlNumberEnumConverter{TEnum}" /> rather than the default.
    /// </param>
    /// <returns>The member's value.</returns>
    private static UInt64BasedLevel DeserializeUnsignedValue(string text, bool numberConverter) =>
        numberConverter
            ? YamlSerializer.Deserialize<NumberUnsignedValueModel>(text)!.Value
            : YamlSerializer.Deserialize<UnsignedValueModel>(
                text,
                new YamlSerializerOptions { WriteEnumsAsStrings = false })!.Value;

    /// <summary>
    /// Deserializes a range model, through the by-name default or the number converter.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="numberConverter">
    /// Whether the members read through <see cref="YamlNumberEnumConverter{TEnum}" />.
    /// </param>
    /// <returns>The bound model.</returns>
    private static object DeserializeRangeModel(string document, bool numberConverter) =>
        numberConverter
            ? YamlSerializer.Deserialize<NumberUnderlyingRangeModel>(document)!
            : YamlSerializer.Deserialize<UnderlyingRangeModel>(document)!;

    /// <summary>
    /// Reads a member of a range model as the decimal text of its underlying integer.
    /// </summary>
    /// <param name="model">The bound model.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>The member's value as the decimal text of its underlying integer.</returns>
    private static string UnderlyingText(object model, string member) =>
        ((Enum)model.GetType().GetProperty(member)!.GetValue(model)!).ToString("D");

    /// <summary>
    /// An enumeration whose members map to member-name scalars by default.
    /// </summary>
    private enum Status
    {
        /// <summary>
        /// The pending state, with underlying value <c>0</c>.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// The active state, with underlying value <c>1</c>.
        /// </summary>
        Active = 1,

        /// <summary>
        /// The archived state, with underlying value <c>2</c>.
        /// </summary>
        Archived = 2,
    }

    /// <summary>
    /// An enumeration whose members carry explicit wire names through
    /// <see cref="StringEnumMemberNameAttribute" />.
    /// </summary>
    private enum RenamedStatus
    {
        /// <summary>
        /// The found state.
        /// </summary>
        Found = 0,

        /// <summary>
        /// The not-found state, written under the name <c>not-found</c>.
        /// </summary>
        [StringEnumMemberName("not-found")]
        NotFound = 1,
    }

    /// <summary>
    /// A flags enumeration used to exercise the by-name converter's combined-value fallback.
    /// </summary>
    [Flags]
    private enum PermissionFlags
    {
        /// <summary>
        /// No flags, with underlying value <c>0</c>.
        /// </summary>
        None = 0,

        /// <summary>
        /// The read flag, with underlying value <c>1</c>.
        /// </summary>
        Read = 1,

        /// <summary>
        /// The write flag, with underlying value <c>2</c>.
        /// </summary>
        Write = 2,
    }

    /// <summary>
    /// A model carrying a <see cref="Status" /> enumeration value.
    /// </summary>
    private sealed class StatusModel
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="RenamedStatus" /> enumeration value.
    /// </summary>
    private sealed class RenamedStatusModel
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public RenamedStatus Status { get; set; }
    }

    /// <summary>
    /// A model whose status is mapped to a YAML integer through a converter attribute.
    /// </summary>
    private sealed class NumberEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as a YAML integer.
        /// </summary>
        /// <value>The status.</value>
        [Converter(typeof(YamlNumberEnumConverter<Status>))]
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model whose status is mapped to a member-name scalar through a converter attribute.
    /// </summary>
    private sealed class StringEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as its member-name scalar.
        /// </summary>
        /// <value>The status.</value>
        [Converter(typeof(YamlStringEnumConverter<Status>))]
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="PermissionFlags" /> value mapped to a member-name scalar by default.
    /// </summary>
    private sealed class FlagsModel
    {
        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>The flags.</value>
        public PermissionFlags Flags { get; set; }
    }

    /// <summary>
    /// An enumeration one of whose members is written under a name that is an integer's text.
    /// </summary>
    private enum Medal
    {
        /// <summary>
        /// The gold medal, written under the name <c>1</c>, with underlying value <c>5</c>.
        /// </summary>
        [StringEnumMemberName("1")]
        Gold = 5,

        /// <summary>
        /// The silver medal, with underlying value <c>6</c>.
        /// </summary>
        Silver = 6,
    }

    /// <summary>
    /// A model carrying a <see cref="Medal" /> value.
    /// </summary>
    private sealed class MedalModel
    {
        /// <summary>
        /// Gets or sets the medal.
        /// </summary>
        /// <value>The medal.</value>
        public Medal Medal { get; set; }
    }

    /// <summary>
    /// An enumeration one of whose members is written under the empty name.
    /// </summary>
    private enum BlankNamedOption
    {
        /// <summary>
        /// The member written as the empty string.
        /// </summary>
        [StringEnumMemberName("")]
        Blank = 0,

        /// <summary>
        /// A member written under its own name.
        /// </summary>
        Named = 1,
    }

    /// <summary>
    /// An enumeration with a two-word member, read back from names whose case differs.
    /// </summary>
    private enum HoldStatus
    {
        /// <summary>
        /// The active state.
        /// </summary>
        Active = 0,

        /// <summary>
        /// The on-hold state.
        /// </summary>
        OnHold = 1,
    }

    /// <summary>
    /// An enumeration whose underlying type is <see cref="sbyte" />.
    /// </summary>
    private enum SByteBasedLevel : sbyte
    {
        /// <summary>
        /// The low level, with underlying value <c>1</c>.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The high level, with underlying value <c>2</c>.
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// An enumeration whose underlying type is <see cref="byte" />.
    /// </summary>
    private enum ByteBasedLevel : byte
    {
        /// <summary>
        /// The low level, with underlying value <c>1</c>.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The high level, with underlying value <c>2</c>.
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// An enumeration whose underlying type is <see cref="short" />.
    /// </summary>
    private enum Int16BasedLevel : short
    {
        /// <summary>
        /// The low level, with underlying value <c>1</c>.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The high level, with underlying value <c>2</c>.
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// An enumeration whose underlying type is <see cref="int" />.
    /// </summary>
    private enum Int32BasedLevel : int
    {
        /// <summary>
        /// The low level, with underlying value <c>1</c>.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The high level, with underlying value <c>2</c>.
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// An enumeration whose underlying type is <see cref="long" />.
    /// </summary>
    private enum Int64BasedLevel : long
    {
        /// <summary>
        /// The low level, with underlying value <c>1</c>.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The high level, with underlying value <c>2</c>.
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// An enumeration whose underlying type is <see cref="ulong" />.
    /// </summary>
    private enum UInt64BasedLevel : ulong
    {
        /// <summary>
        /// The low level, with underlying value <c>1</c>.
        /// </summary>
        Low = 1,

        /// <summary>
        /// The high level, with underlying value <c>2</c>.
        /// </summary>
        High = 2,
    }

    /// <summary>
    /// A model with a member of each enumeration, read by name with integers accepted, as the default does.
    /// </summary>
    private sealed class UnderlyingRangeModel
    {
        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="sbyte" />.
        /// </summary>
        /// <value>The value.</value>
        public SByteBasedLevel SByte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="byte" />.
        /// </summary>
        /// <value>The value.</value>
        public ByteBasedLevel Byte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="short" />.
        /// </summary>
        /// <value>The value.</value>
        public Int16BasedLevel Int16 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="int" />.
        /// </summary>
        /// <value>The value.</value>
        public Int32BasedLevel Int32 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="long" />.
        /// </summary>
        /// <value>The value.</value>
        public Int64BasedLevel Int64 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="ulong" />.
        /// </summary>
        /// <value>The value.</value>
        public UInt64BasedLevel UInt64 { get; set; }
    }

    /// <summary>
    /// A model with a member of each enumeration, each read through <see cref="YamlNumberEnumConverter{TEnum}" />.
    /// </summary>
    private sealed class NumberUnderlyingRangeModel
    {
        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="sbyte" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<SByteBasedLevel>))]
        public SByteBasedLevel SByte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="byte" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<ByteBasedLevel>))]
        public ByteBasedLevel Byte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="short" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<Int16BasedLevel>))]
        public Int16BasedLevel Int16 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="int" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<Int32BasedLevel>))]
        public Int32BasedLevel Int32 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="long" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<Int64BasedLevel>))]
        public Int64BasedLevel Int64 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="ulong" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<UInt64BasedLevel>))]
        public UInt64BasedLevel UInt64 { get; set; }
    }

    /// <summary>
    /// A model with one member of the enumeration based on <see cref="ulong" />, under the default handling.
    /// </summary>
    private sealed class UnsignedValueModel
    {
        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        /// <value>The value.</value>
        public UInt64BasedLevel Value { get; set; }
    }

    /// <summary>
    /// A model with one member of the enumeration based on <see cref="ulong" />, read and written through
    /// <see cref="YamlNumberEnumConverter{TEnum}" />.
    /// </summary>
    private sealed class NumberUnsignedValueModel
    {
        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(YamlNumberEnumConverter<UInt64BasedLevel>))]
        public UInt64BasedLevel Value { get; set; }
    }
}
