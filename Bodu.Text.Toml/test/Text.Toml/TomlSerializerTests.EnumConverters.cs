// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.EnumConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Assertions;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the System.Text.Json-style enum-converter surface for TOML: the built-in by-name default, the
/// <see cref="StringEnumMemberNameAttribute" /> override, and the <see cref="TomlStringEnumConverter" />,
/// <see cref="TomlStringEnumConverter{TEnum}" />, and <see cref="TomlNumberEnumConverter{TEnum}" /> factories.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that an enumeration with no explicit converter serializes as its member-name string by default and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDefaultEnum_ShouldWriteMemberNameStringAndRoundTrip()
    {
        var model = new StatusModel { Status = Status.Active };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"Active\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that serializing a bare enumeration value at the document root throws
    /// <see cref="TomlSerializationException" />, because a TOML document's root must be a table.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootIsEnum_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(Status.Pending);
        });
    }

    /// <summary>
    /// Verifies that the default by-name converter accepts a TOML integer on read and converts it to the corresponding
    /// numeric enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDefaultEnumReadsInteger_ShouldReadIntegerAsEnum()
    {
        StatusModel model = TomlSerializer.Deserialize<StatusModel>("Status = 2\n");

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

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"not-found\"\n", text);

        RenamedStatusModel roundTripped = TomlSerializer.Deserialize<RenamedStatusModel>(text);
        Assert.AreEqual(RenamedStatus.NotFound, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> registered with a camel-case naming policy writes a
    /// Pascal-case member as its camel-cased name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterCamelCasePolicy_ShouldCamelCaseAndRoundTrip()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"active\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to allow integer values reads a TOML integer
    /// as the corresponding numeric enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterAllowsIntegers_ShouldReadIntegerAsEnum()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        StatusModel model = TomlSerializer.Deserialize<StatusModel>("Status = 2\n", options);

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> throws <see cref="TomlSerializationException" /> when it
    /// reads a string that matches no member name and is not otherwise parseable.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterReadsUnknownName_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<StatusModel>("Status = \"unknown\"\n", options);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="TomlSerializationException" /> when it reads a TOML integer for the enumeration.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegers_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<StatusModel>("Status = 2\n", options);
        });
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="TomlNumberEnumConverter{TEnum}" /> serializes the enumeration as a TOML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterAttribute_ShouldWriteIntegerAndRoundTrip()
    {
        var model = new NumberEnumModel { Status = Status.Archived };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = 2\n", text);

        NumberEnumModel roundTripped = TomlSerializer.Deserialize<NumberEnumModel>(text);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="TomlStringEnumConverter{TEnum}" /> serializes the enumeration as its member-name string and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterAttribute_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new StringEnumModel { Status = Status.Pending };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"Pending\"\n", text);

        StringEnumModel roundTripped = TomlSerializer.Deserialize<StringEnumModel>(text);
        Assert.AreEqual(Status.Pending, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a non-generic <see cref="TomlStringEnumConverter" /> registered without a naming policy writes a
    /// member as its unchanged CLR name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterNoPolicy_ShouldUseMemberName()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter());

        var model = new StatusModel { Status = Status.Active };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"Active\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter matches a member name case-insensitively on read, so an upper-cased wire
    /// string binds to a Pascal-case member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumStringCaseDiffers_ShouldMatchCaseInsensitively()
    {
        StatusModel model = TomlSerializer.Deserialize<StatusModel>("Status = \"ACTIVE\"\n");

        Assert.AreEqual(Status.Active, model.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter reads a numeric string by parsing it through the runtime, so a member
    /// stored as its decimal text still binds to the enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumReadsNumericString_ShouldParseToValue()
    {
        StatusModel model = TomlSerializer.Deserialize<StatusModel>("Status = \"2\"\n");

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="TomlSerializationException" /> at the string, with its line, column, offset and path, when the string
    /// holds an integer, alone or as one of its comma-separated parts, rather than binding the integer it holds.
    /// </summary>
    /// <param name="literal">The string, as the document spells it.</param>
    [TestMethod]
    [DataRow("\"2\"", DisplayName = "the digits of a defined value")]
    [DataRow("\"99\"", DisplayName = "the digits of an undefined value")]
    [DataRow("\" +2 \"", DisplayName = "a signed integer between spaces")]
    [DataRow("'-1'", DisplayName = "a negative integer in a literal string")]
    [DataRow("\"2\\u0000\"", DisplayName = "an integer followed by a null character")]
    [DataRow("\"1, 2\"", DisplayName = "a list of integers")]
    [DataRow("\"Read, 2\"", DisplayName = "a member name and an integer")]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndTextHoldsInteger_ShouldThrowTomlSerializationException(
        string literal)
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        string document = $"Flags = {literal}\n";

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<FlagsModel>(document, options);
        });

        // The string begins after "Flags = ", at the zero-based offset 8 and the one-based column 9.
        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(9, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(8, ex.Offset, "Offset");
        Assert.AreEqual("Flags", ex.Path, "Path");
        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter{TEnum}" /> configured to reject integer values throws
    /// <see cref="TomlSerializationException" /> when the string holds an integer, as the converter the non-generic
    /// factory produces does.
    /// </summary>
    /// <param name="literal">The string, as the document spells it.</param>
    [TestMethod]
    [DataRow("\"2\"", DisplayName = "the digits of a defined value")]
    [DataRow("\"Read, 2\"", DisplayName = "a member name and an integer")]
    public void Deserialize_WhenGenericStringEnumConverterDisallowsIntegersAndTextHoldsInteger_ShouldThrowTomlSerializationException(
        string literal)
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter<PermissionFlags>(namingPolicy: null, allowIntegerValues: false));
        string document = $"Flags = {literal}\n";

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<FlagsModel>(document, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values still binds a string
    /// that is a member's wire name when that name is an integer's text, because the wire names are matched before the
    /// text is checked for an integer.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndWireNameIsInteger_ShouldBindMember()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        MedalModel model = TomlSerializer.Deserialize<MedalModel>("Medal = \"1\"\n", options);

        Assert.AreEqual(Medal.Gold, model.Medal);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to allow integer values binds a string that
    /// holds an integer as the value it holds, as the default handling does.
    /// </summary>
    /// <param name="literal">The string, as the document spells it.</param>
    /// <param name="expected">The underlying value the string holds.</param>
    [TestMethod]
    [DataRow("\"2\"", 2, DisplayName = "the digits of a defined value")]
    [DataRow("\"99\"", 99, DisplayName = "the digits of an undefined value")]
    [DataRow("\" +2 \"", 2, DisplayName = "a signed integer between spaces")]
    public void Deserialize_WhenStringEnumConverterAllowsIntegersAndTextHoldsInteger_ShouldBindValue(string literal, int expected)
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: true));

        FlagsModel model = TomlSerializer.Deserialize<FlagsModel>($"Flags = {literal}\n", options);

        Assert.AreEqual((PermissionFlags)expected, model.Flags);
    }

    /// <summary>
    /// Verifies that the generic <see cref="TomlStringEnumConverter{TEnum}" /> registered with a camel-case naming
    /// policy on the options writes a Pascal-case member as its camel-cased name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGenericStringEnumConverterWithPolicy_ShouldCamelCaseAndRoundTrip()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter<Status>(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"active\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlNumberEnumConverter{TEnum}" /> registered on the options writes the enumeration
    /// as a TOML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterRegistered_ShouldWriteIntegerAndRoundTrip()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlNumberEnumConverter<Status>());

        var model = new StatusModel { Status = Status.Archived };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = 2\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property whose <see cref="TomlNumberEnumConverter{TEnum}" /> reads a non-integer token throws
    /// <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNumberEnumConverterReadsString_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<NumberEnumModel>("Status = \"Archived\"\n");
        });
    }

    /// <summary>
    /// Verifies that an undefined enumeration value, which corresponds to no member, is written by the by-name
    /// converter as its decimal text and read back to the same undefined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUndefinedEnumValue_ShouldWriteDecimalTextAndRoundTrip()
    {
        var model = new StatusModel { Status = (Status)99 };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"99\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text);
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

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Flags = \"Read, Write\"\n", text);

        FlagsModel roundTripped = TomlSerializer.Deserialize<FlagsModel>(text);
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

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Flags = \"Write\"\n", text);

        FlagsModel roundTripped = TomlSerializer.Deserialize<FlagsModel>(text);
        Assert.AreEqual(PermissionFlags.Write, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="TomlSerializationException" />, naming the value and the enumeration, when it writes a value that is
    /// not a member, rather than writing the value's decimal text, which it would reject on read.
    /// </summary>
    /// <param name="value">The underlying value, which no member of <see cref="Status" /> has.</param>
    [TestMethod]
    [DataRow(99, DisplayName = "an undefined value")]
    [DataRow(-1, DisplayName = "a negative undefined value")]
    public void Serialize_WhenStringEnumConverterDisallowsIntegersAndValueIsUndefined_ShouldThrowTomlSerializationException(
        int value)
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        var model = new StatusModel { Status = (Status)value };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(Status).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="TomlSerializationException" />, naming the value and the enumeration, when it writes a flags value
    /// with a bit that no member defines, which is not a combination of defined flags.
    /// </summary>
    /// <param name="value">
    /// The underlying value, with a bit that no member of <see cref="PermissionFlags" /> defines.
    /// </param>
    [TestMethod]
    [DataRow(4, DisplayName = "an undefined flag")]
    [DataRow(7, DisplayName = "defined flags with an undefined flag")]
    public void Serialize_WhenStringEnumConverterDisallowsIntegersAndFlagIsUndefined_ShouldThrowTomlSerializationException(
        int value)
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter<PermissionFlags>(namingPolicy: null, allowIntegerValues: false));
        var model = new FlagsModel { Flags = (PermissionFlags)value };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(PermissionFlags).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to reject integer values still writes a
    /// combination of defined flags as its comma-separated member list, and the zero-valued member as its name.
    /// </summary>
    /// <param name="value">The underlying value, a combination of defined flags.</param>
    /// <param name="expected">The expected TOML document.</param>
    [TestMethod]
    [DataRow(3, "Flags = \"Read, Write\"\n", DisplayName = "two defined flags")]
    [DataRow(0, "Flags = \"None\"\n", DisplayName = "the zero-valued member")]
    public void Serialize_WhenStringEnumConverterDisallowsIntegersAndFlagsAreDefined_ShouldWriteNames(int value, string expected)
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        string text = TomlSerializer.Serialize(new FlagsModel { Flags = (PermissionFlags)value }, options);

        Assert.AreEqual(expected, text);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> configured to allow integer values writes a value that is
    /// not a member as its decimal text, as the default handling does.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringEnumConverterAllowsIntegersAndValueIsUndefined_ShouldWriteDecimalText()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(namingPolicy: null, allowIntegerValues: true));

        string text = TomlSerializer.Serialize(new StatusModel { Status = (Status)99 }, options);

        Assert.AreEqual("Status = \"99\"\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="StringEnumMemberNameAttribute" /> takes precedence over a naming policy applied by
    /// a <see cref="TomlStringEnumConverter" />, so the explicit name is used unchanged.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberNameAttributeAndNamingPolicy_ShouldPreferAttribute()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"not-found\"\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="TomlStringEnumConverter.CanConvert(Type)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var converter = new TomlStringEnumConverter();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = converter.CanConvert(null!);
        }, "typeToConvert");
    }

    /// <summary>
    /// Verifies that <see cref="TomlStringEnumConverter.CreateConverter(Type, TomlSerializerOptions)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateConverter_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var converter = new TomlStringEnumConverter();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = converter.CreateConverter(null!, new TomlSerializerOptions());
        }, "typeToConvert");
    }

    /// <summary>
    /// Verifies that constructing <see cref="StringEnumMemberNameAttribute" /> with a <see langword="null" />
    /// name throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>name</c>.
    /// </summary>
    [TestMethod]
    public void StringEnumMemberNameAttribute_WhenNameNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new StringEnumMemberNameAttribute(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that, under the snake-case policy, an enumeration member reads its value from the member's name and is
    /// written back as that name on the same line.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemberIsEnum_ShouldUseMemberName()
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };

        EnumValueModel model = TomlSerializer.Deserialize<EnumValueModel>("enum_value = \"EnumValue1\"\n", options);
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual(ReflectedEnum.EnumValue1, model.EnumValue);
        Assert.AreEqual("enum_value = \"EnumValue1\"\n", text);
    }

    /// <summary>
    /// Verifies that an integer outside the range of an enumeration's underlying type is rejected with
    /// <see cref="TomlSerializationException" /> at the integer, with its line, column, offset and path, through the
    /// by-name default and the number converter alike, rather than bound as the value it truncates to.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, one step outside the underlying type's range.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="TomlNumberEnumConverter{TEnum}" />.
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
    public void Deserialize_WhenIntegerIsOutsideEnumUnderlyingRange_ShouldThrowTomlSerializationException(
        string member,
        string literal,
        bool numberConverter)
    {
        string document = $"{member} = {literal}\n";

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = DeserializeRangeModel(document, numberConverter);
        });

        Assert.AreEqual(1, ex.LineNumber, "LineNumber");
        Assert.AreEqual(member.Length + 4, ex.ColumnNumber, "ColumnNumber");
        Assert.AreEqual(member.Length + 3, ex.Offset, "Offset");
        Assert.AreEqual(member, ex.Path, "Path");
    }

    /// <summary>
    /// Verifies that an integer at either end of the range of an enumeration's underlying type binds as that value,
    /// through the by-name default and the number converter alike.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">
    /// The integer, at one end of the underlying type's range, or for <see cref="ulong" /> the largest TOML integer.
    /// </param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="TomlNumberEnumConverter{TEnum}" />.
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
    [DataRow("UInt64", "9223372036854775807", false, DisplayName = "ulong, the format's maximum, by name")]
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
        object model = DeserializeRangeModel($"{member} = {literal}\n", numberConverter);

        Assert.AreEqual(literal, UnderlyingText(model, member));
    }

    /// <summary>
    /// Verifies that an integer inside the range of an enumeration's underlying type that names no member binds as that
    /// undefined value, through the by-name default and the number converter alike.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, inside the underlying type's range and naming no member.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="TomlNumberEnumConverter{TEnum}" />.
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
        object model = DeserializeRangeModel($"{member} = {literal}\n", numberConverter);

        Assert.AreEqual(literal, UnderlyingText(model, member));
    }

    /// <summary>
    /// Verifies that <see cref="TomlNumberEnumConverter{TEnum}" /> rejects a value of an enumeration based on
    /// <see cref="ulong" /> above <see cref="long.MaxValue" />, which no TOML integer holds, with
    /// <see cref="TomlSerializationException" /> carrying the <see cref="OverflowException" />, as it rejects such a
    /// <see cref="ulong" /> member.
    /// </summary>
    /// <param name="value">The underlying value, above <see cref="long.MaxValue" />.</param>
    [TestMethod]
    [DataRow(9_223_372_036_854_775_808UL, DisplayName = "long.MaxValue + 1")]
    [DataRow(ulong.MaxValue, DisplayName = "ulong.MaxValue")]
    public void Serialize_WhenUInt64BasedValueExceedsInt64_ForNumberConverter_ShouldThrowTomlSerializationException(
        ulong value)
    {
        var model = new UnsignedNumberModel { Value = (UInt64BasedLevel)value };

        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Serialize(model);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<OverflowException>(ex.InnerException);
    }

    /// <summary>
    /// Deserializes a range model, through the by-name default or the number converter.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="numberConverter">
    /// Whether the members read through <see cref="TomlNumberEnumConverter{TEnum}" />.
    /// </param>
    /// <returns>The bound model.</returns>
    private static object DeserializeRangeModel(string document, bool numberConverter) =>
        numberConverter
            ? TomlSerializer.Deserialize<NumberUnderlyingRangeModel>(document)
            : TomlSerializer.Deserialize<UnderlyingRangeModel>(document);

    /// <summary>
    /// Reads a member of a range model as the decimal text of its underlying integer.
    /// </summary>
    /// <param name="model">The bound model.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>The member's value as the decimal text of its underlying integer.</returns>
    private static string UnderlyingText(object model, string member) =>
        ((Enum)model.GetType().GetProperty(member)!.GetValue(model)!).ToString("D");

    /// <summary>
    /// An enumeration whose members map to member-name strings by default.
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
    /// A model whose status is mapped to a TOML integer through a converter attribute.
    /// </summary>
    private sealed class NumberEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as a TOML integer.
        /// </summary>
        /// <value>The status.</value>
        [Converter(typeof(TomlNumberEnumConverter<Status>))]
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model whose status is mapped to a member-name string through a converter attribute.
    /// </summary>
    private sealed class StringEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as its member-name string.
        /// </summary>
        /// <value>The status.</value>
        [Converter(typeof(TomlStringEnumConverter<Status>))]
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="PermissionFlags" /> value mapped to a member-name string by default.
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
    /// An enumeration read and written by its member names.
    /// </summary>
    private enum ReflectedEnum
    {
        /// <summary>
        /// The default member, which no document names.
        /// </summary>
        None,

        /// <summary>
        /// The member the document names.
        /// </summary>
        EnumValue1,
    }

    /// <summary>
    /// A model with an enumeration member, written <c>enum_value</c> under the snake-case policy.
    /// </summary>
    private sealed class EnumValueModel
    {
        /// <summary>
        /// Gets or sets the enumeration value.
        /// </summary>
        /// <value>The value; <see cref="ReflectedEnum.None" /> until it is read.</value>
        public ReflectedEnum EnumValue { get; set; }
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
    /// A model with a member of each enumeration, each read through <see cref="TomlNumberEnumConverter{TEnum}" />.
    /// </summary>
    private sealed class NumberUnderlyingRangeModel
    {
        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="sbyte" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<SByteBasedLevel>))]
        public SByteBasedLevel SByte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="byte" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<ByteBasedLevel>))]
        public ByteBasedLevel Byte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="short" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<Int16BasedLevel>))]
        public Int16BasedLevel Int16 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="int" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<Int32BasedLevel>))]
        public Int32BasedLevel Int32 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="long" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<Int64BasedLevel>))]
        public Int64BasedLevel Int64 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="ulong" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<UInt64BasedLevel>))]
        public UInt64BasedLevel UInt64 { get; set; }
    }

    /// <summary>
    /// A model with one member of the enumeration based on <see cref="ulong" />, read and written through
    /// <see cref="TomlNumberEnumConverter{TEnum}" />.
    /// </summary>
    private sealed class UnsignedNumberModel
    {
        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(TomlNumberEnumConverter<UInt64BasedLevel>))]
        public UInt64BasedLevel Value { get; set; }
    }
}
