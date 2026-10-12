// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeEnumConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Globalization;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the System.Text.Json-style enum-converter surface for Bencode: the built-in by-name default,
/// <see cref="StringEnumMemberNameAttribute" />, and the <see cref="BencodeStringEnumConverter" />,
/// <see cref="BencodeStringEnumConverter{TEnum}" />, and <see cref="BencodeNumberEnumConverter{TEnum}" /> factories.
/// </summary>
[TestClass]
public class BencodeEnumConverterTests
{
    /// <summary>
    /// Verifies that an enumeration with no explicit converter serializes as its member-name byte string by default and
    /// round-trips, producing canonical Bencode bytes.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenDefaultEnum_ShouldWriteMemberNameByteString()
    {
        var model = new StatusModel { Status = Status.Active };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Status6:Activee", Encoding.Latin1.GetString(bytes));

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a bare enumeration value serialized at the document root is written as its member-name byte string.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenRootEnumValue_ShouldWriteMemberNameByteString()
    {
        byte[] bytes = BencodeSerializer.Serialize(Status.Pending);

        Assert.AreEqual("7:Pending", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="StringEnumMemberNameAttribute" /> overrides the byte-string name used for an
    /// individual enumeration member on both write and read, even with no converter explicitly registered.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemberNameAttributePresent_ShouldUseOverriddenName()
    {
        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Status9:not-founde", Encoding.Latin1.GetString(bytes));

        RenamedStatusModel roundTripped = BencodeSerializer.Deserialize<RenamedStatusModel>(bytes);
        Assert.AreEqual(RenamedStatus.NotFound, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> registered with a camel-case naming policy writes a
    /// Pascal-case member as its camel-cased name.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringEnumConverterWithCamelCasePolicy_ShouldCamelCaseMemberName()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d6:Status6:activee", Encoding.Latin1.GetString(bytes));

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to allow integer values reads a Bencode
    /// integer as the corresponding numeric enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterAllowsIntegers_ShouldReadIntegerAsEnum()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        byte[] bytes = Encoding.Latin1.GetBytes("d6:Statusi2ee");

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);

        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> throws <see cref="BencodeSerializationException" /> when
    /// it reads a byte string that matches no member name and is not otherwise parseable.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterReadsUnknownName_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        byte[] bytes = Encoding.Latin1.GetBytes("d6:Status7:unknowne");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
        });
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="BencodeSerializationException" /> when it reads a Bencode integer for the enumeration.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegers_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        byte[] bytes = Encoding.Latin1.GetBytes("d6:Statusi2ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
        });
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="BencodeNumberEnumConverter{TEnum}" /> serializes the enumeration as a Bencode integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterAttributeOnProperty_ShouldWriteIntegerAndRoundTrip()
    {
        var model = new NumberEnumModel { Status = Status.Archived };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Statusi2ee", Encoding.Latin1.GetString(bytes));

        NumberEnumModel roundTripped = BencodeSerializer.Deserialize<NumberEnumModel>(bytes);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="BencodeStringEnumConverter{TEnum}" /> serializes the enumeration as its member-name byte string and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterAttributeOnProperty_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new StringEnumModel { Status = Status.Pending };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Status7:Pendinge", Encoding.Latin1.GetString(bytes));

        StringEnumModel roundTripped = BencodeSerializer.Deserialize<StringEnumModel>(bytes);
        Assert.AreEqual(Status.Pending, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a non-generic <see cref="BencodeStringEnumConverter" /> registered without a naming policy writes a
    /// member as its unchanged CLR name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterNoPolicy_ShouldUseMemberName()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter());

        var model = new StatusModel { Status = Status.Active };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d6:Status6:Activee", Encoding.Latin1.GetString(bytes));

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="BencodeStringEnumConverter" /> matches a member name case-insensitively
    /// on read, so a lower-cased wire string binds to a Pascal-case member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterReadsDifferentCase_ShouldMatchCaseInsensitively()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter());

        byte[] bytes = Encoding.Latin1.GetBytes("d6:Status6:activee");

        StatusModel model = BencodeSerializer.Deserialize<StatusModel>(bytes, options);

        Assert.AreEqual(Status.Active, model.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter reads a numeric byte string by parsing it through the runtime, so a member
    /// stored as its decimal text still binds to the enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterReadsNumericString_ShouldParseToValue()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d6:Status1:2e");

        StatusModel model = BencodeSerializer.Deserialize<StatusModel>(bytes);

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="BencodeSerializationException" /> at the byte string when the byte string holds an integer, alone or
    /// as one of its comma-separated parts, rather than binding the integer it holds.
    /// </summary>
    /// <param name="text">The byte string's text.</param>
    [TestMethod]
    [DataRow("2", DisplayName = "the digits of a defined value")]
    [DataRow("99", DisplayName = "the digits of an undefined value")]
    [DataRow(" +2 ", DisplayName = "a signed integer between spaces")]
    [DataRow("-1", DisplayName = "a negative integer")]
    [DataRow("2\0", DisplayName = "an integer followed by a null character")]
    [DataRow("1, 2", DisplayName = "a list of integers")]
    [DataRow("Read, 2", DisplayName = "a member name and an integer")]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndTextHoldsInteger_ShouldThrowBencodeSerializationException(
        string text)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        byte[] bytes = Encoding.Latin1.GetBytes($"d5:Flags{text.Length}:{text}e");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<FlagsModel>(bytes, options);
        });

        // The byte string begins after the dictionary's 'd' and the key's length-prefixed byte string.
        Assert.AreEqual(1 + "5:Flags".Length, ex.BytesOffset, "BytesOffset");
        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter{TEnum}" /> configured to reject integer values throws
    /// <see cref="BencodeSerializationException" /> when the byte string holds an integer, as the converter the
    /// non-generic factory produces does.
    /// </summary>
    /// <param name="text">The byte string's text.</param>
    [TestMethod]
    [DataRow("2", DisplayName = "the digits of a defined value")]
    [DataRow("Read, 2", DisplayName = "a member name and an integer")]
    public void Deserialize_WhenGenericStringEnumConverterDisallowsIntegersAndTextHoldsInteger_ShouldThrowBencodeSerializationException(
        string text)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter<FlagsEnum>(namingPolicy: null, allowIntegerValues: false));
        byte[] bytes = Encoding.Latin1.GetBytes($"d5:Flags{text.Length}:{text}e");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<FlagsModel>(bytes, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to reject integer values still binds a byte
    /// string that is a member's wire name when that name is an integer's text, because the wire names are matched
    /// before the text is checked for an integer.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndWireNameIsInteger_ShouldBindMember()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        MedalModel model = BencodeSerializer.Deserialize<MedalModel>(Encoding.Latin1.GetBytes("d5:Medal1:1e"), options);

        Assert.AreEqual(Medal.Gold, model.Medal);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to allow integer values binds a byte string
    /// that holds an integer as the value it holds, as the default handling does.
    /// </summary>
    /// <param name="text">The byte string's text.</param>
    /// <param name="expected">The underlying value the text holds.</param>
    [TestMethod]
    [DataRow("2", 2, DisplayName = "the digits of a defined value")]
    [DataRow("99", 99, DisplayName = "the digits of an undefined value")]
    [DataRow(" +2 ", 2, DisplayName = "a signed integer between spaces")]
    public void Deserialize_WhenStringEnumConverterAllowsIntegersAndTextHoldsInteger_ShouldBindValue(string text, int expected)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: true));
        byte[] bytes = Encoding.Latin1.GetBytes($"d5:Flags{text.Length}:{text}e");

        FlagsModel model = BencodeSerializer.Deserialize<FlagsModel>(bytes, options);

        Assert.AreEqual((FlagsEnum)expected, model.Flags);
    }

    /// <summary>
    /// Verifies that the generic <see cref="BencodeStringEnumConverter{TEnum}" /> registered with a camel-case naming
    /// policy on the options writes a Pascal-case member as its camel-cased name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGenericStringEnumConverterWithPolicy_ShouldCamelCaseAndRoundTrip()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter<Status>(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d6:Status6:activee", Encoding.Latin1.GetString(bytes));

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeNumberEnumConverter{TEnum}" /> registered on the options writes the enumeration
    /// as a Bencode integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterRegistered_ShouldWriteIntegerAndRoundTrip()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeNumberEnumConverter<Status>());

        var model = new StatusModel { Status = Status.Archived };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d6:Statusi2ee", Encoding.Latin1.GetString(bytes));

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property whose <see cref="BencodeNumberEnumConverter{TEnum}" /> reads a non-integer token throws
    /// <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNumberEnumConverterReadsByteString_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d6:Status8:Archivede");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<NumberEnumModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that an undefined enumeration value, which corresponds to no member, is written by the by-name converter
    /// as its decimal text and read back to the same undefined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUndefinedEnumValue_ShouldWriteDecimalTextAndRoundTrip()
    {
        var model = new StatusModel { Status = (Status)99 };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Status2:99e", Encoding.Latin1.GetString(bytes));

        StatusModel roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes);
        Assert.AreEqual((Status)99, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a combination of <see cref="FlagsEnum" /> values, which corresponds to no single member, is written
    /// by the by-name converter as its comma-separated member list and read back to the same combined value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCombinedFlags_ShouldWriteFlagListAndRoundTrip()
    {
        var model = new FlagsModel { Flags = FlagsEnum.Read | FlagsEnum.Write };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d5:Flags11:Read, Writee", Encoding.Latin1.GetString(bytes));

        FlagsModel roundTripped = BencodeSerializer.Deserialize<FlagsModel>(bytes);
        Assert.AreEqual(FlagsEnum.Read | FlagsEnum.Write, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that a single <see cref="FlagsEnum" /> member is written as its member name and round-trips, confirming
    /// the by-name path applies to flags enumerations whose value maps to one member.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenSingleFlag_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new FlagsModel { Flags = FlagsEnum.Write };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d5:Flags5:Writee", Encoding.Latin1.GetString(bytes));

        FlagsModel roundTripped = BencodeSerializer.Deserialize<FlagsModel>(bytes);
        Assert.AreEqual(FlagsEnum.Write, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="BencodeSerializationException" />, naming the value and the enumeration, when it writes a value that
    /// is not a member, rather than writing the value's decimal text, which it would reject on read.
    /// </summary>
    /// <param name="value">The underlying value, which no member of <see cref="Status" /> has.</param>
    [TestMethod]
    [DataRow(99, DisplayName = "an undefined value")]
    [DataRow(-1, DisplayName = "a negative undefined value")]
    public void Serialize_WhenStringEnumConverterDisallowsIntegersAndValueIsUndefined_ShouldThrowBencodeSerializationException(
        int value)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        var model = new StatusModel { Status = (Status)value };

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(model, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(Status).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to reject integer values throws
    /// <see cref="BencodeSerializationException" />, naming the value and the enumeration, when it writes a flags value
    /// with a bit that no member defines, which is not a combination of defined flags.
    /// </summary>
    /// <param name="value">The underlying value, with a bit that no member of <see cref="FlagsEnum" /> defines.</param>
    [TestMethod]
    [DataRow(4, DisplayName = "an undefined flag")]
    [DataRow(7, DisplayName = "defined flags with an undefined flag")]
    public void Serialize_WhenStringEnumConverterDisallowsIntegersAndFlagIsUndefined_ShouldThrowBencodeSerializationException(
        int value)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter<FlagsEnum>(namingPolicy: null, allowIntegerValues: false));
        var model = new FlagsModel { Flags = (FlagsEnum)value };

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Serialize(model, options);
        });

        Assert.IsTrue(ex.Message.Contains("integer values are not allowed", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains(typeof(FlagsEnum).ToString(), StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to reject integer values still writes a
    /// combination of defined flags as its comma-separated member list, and the zero-valued member as its name.
    /// </summary>
    /// <param name="value">The underlying value, a combination of defined flags.</param>
    /// <param name="expected">The expected Bencode document.</param>
    [TestMethod]
    [DataRow(3, "d5:Flags11:Read, Writee", DisplayName = "two defined flags")]
    [DataRow(0, "d5:Flags4:Nonee", DisplayName = "the zero-valued member")]
    public void Serialize_WhenStringEnumConverterDisallowsIntegersAndFlagsAreDefined_ShouldWriteNames(int value, string expected)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        byte[] bytes = BencodeSerializer.Serialize(new FlagsModel { Flags = (FlagsEnum)value }, options);

        Assert.AreEqual(expected, Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> configured to allow integer values writes a value that
    /// is not a member as its decimal text, as the default handling does.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringEnumConverterAllowsIntegersAndValueIsUndefined_ShouldWriteDecimalText()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(namingPolicy: null, allowIntegerValues: true));

        byte[] bytes = BencodeSerializer.Serialize(new StatusModel { Status = (Status)99 }, options);

        Assert.AreEqual("d6:Status2:99e", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="StringEnumMemberNameAttribute" /> takes precedence over a naming policy applied by
    /// a <see cref="BencodeStringEnumConverter" />, so the explicit name is used unchanged.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberNameAttributeAndNamingPolicy_ShouldPreferAttribute()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d6:Status9:not-founde", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeStringEnumConverter.CanConvert(Type)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var converter = new BencodeStringEnumConverter();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = converter.CanConvert(null!);
        }, "typeToConvert");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeStringEnumConverter.CreateConverter(Type, BencodeSerializerOptions)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>typeToConvert</c> when the type is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateConverter_WhenTypeToConvertNull_ShouldThrowArgumentNullException()
    {
        var converter = new BencodeStringEnumConverter();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = converter.CreateConverter(null!, new BencodeSerializerOptions());
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
    /// Verifies that an integer outside the range of an enumeration's underlying type is rejected with
    /// <see cref="BencodeSerializationException" /> at the integer, with the byte offset of the integer token, through
    /// the by-name default and the number converter alike, rather than bound as the value it truncates to.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, one step outside the underlying type's range.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="BencodeNumberEnumConverter{TEnum}" />.
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
    [DataRow("UInt64", "-1", false, DisplayName = "ulong, one below the range, by name")]
    [DataRow("SByte", "128", true, DisplayName = "sbyte, one above the range, number converter")]
    [DataRow("SByte", "-129", true, DisplayName = "sbyte, one below the range, number converter")]
    [DataRow("Byte", "256", true, DisplayName = "byte, one above the range, number converter")]
    [DataRow("Byte", "-1", true, DisplayName = "byte, one below the range, number converter")]
    [DataRow("Int16", "32768", true, DisplayName = "short, one above the range, number converter")]
    [DataRow("Int16", "-32769", true, DisplayName = "short, one below the range, number converter")]
    [DataRow("Int32", "2147483648", true, DisplayName = "int, one above the range, number converter")]
    [DataRow("Int32", "-2147483649", true, DisplayName = "int, one below the range, number converter")]
    [DataRow("Int64", "9223372036854775808", true, DisplayName = "long, one above the range, number converter")]
    [DataRow("UInt64", "-1", true, DisplayName = "ulong, one below the range, number converter")]
    public void Deserialize_WhenIntegerIsOutsideEnumUnderlyingRange_ShouldThrowBencodeSerializationException(
        string member,
        string literal,
        bool numberConverter)
    {
        string document = $"d{member.Length}:{member}i{literal}ee";

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = DeserializeRangeModel(document, numberConverter);
        });

        // The integer token begins after the dictionary's 'd' and the key's length-prefixed byte string.
        Assert.AreEqual(1 + $"{member.Length}:{member}".Length, ex.BytesOffset, "BytesOffset");
    }

    /// <summary>
    /// Verifies that an integer at either end of the range of an enumeration's underlying type binds as that value,
    /// through the by-name default and the number converter alike.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, at one end of the underlying type's range.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="BencodeNumberEnumConverter{TEnum}" />.
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
    [DataRow("UInt64", "18446744073709551615", true, DisplayName = "ulong, the maximum, number converter")]
    [DataRow("UInt64", "0", true, DisplayName = "ulong, the minimum, number converter")]
    public void Deserialize_WhenIntegerIsAtEnumUnderlyingRangeLimit_ShouldBindIt(
        string member,
        string literal,
        bool numberConverter)
    {
        object model = DeserializeRangeModel($"d{member.Length}:{member}i{literal}ee", numberConverter);

        Assert.AreEqual(literal, UnderlyingText(model, member));
    }

    /// <summary>
    /// Verifies that an integer inside the range of an enumeration's underlying type that names no member binds as that
    /// undefined value, through the by-name default and the number converter alike.
    /// </summary>
    /// <param name="member">The member, named for its enumeration's underlying type.</param>
    /// <param name="literal">The integer, inside the underlying type's range and naming no member.</param>
    /// <param name="numberConverter">
    /// Whether the member reads through <see cref="BencodeNumberEnumConverter{TEnum}" />.
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
        object model = DeserializeRangeModel($"d{member.Length}:{member}i{literal}ee", numberConverter);

        Assert.AreEqual(literal, UnderlyingText(model, member));
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNumberEnumConverter{TEnum}" /> writes a value of an enumeration based on
    /// <see cref="ulong" /> above <see cref="long.MaxValue" /> as the unsigned Bencode integer it is.
    /// </summary>
    /// <param name="value">The underlying value, above <see cref="long.MaxValue" />.</param>
    /// <param name="digits">The value's decimal digits.</param>
    [TestMethod]
    [DataRow(9_223_372_036_854_775_808UL, "9223372036854775808", DisplayName = "long.MaxValue + 1")]
    [DataRow(ulong.MaxValue, "18446744073709551615", DisplayName = "ulong.MaxValue")]
    public void Serialize_WhenUInt64BasedValueExceedsInt64_ForNumberConverter_ShouldWriteUnsignedInteger(
        ulong value,
        string digits)
    {
        var model = new UnsignedNumberModel { Value = (UInt64BasedLevel)value };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d5:Valuei" + digits + "ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a value of an enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" />
    /// round-trips through <see cref="BencodeNumberEnumConverter{TEnum}" />.
    /// </summary>
    /// <param name="value">The underlying value, above <see cref="long.MaxValue" />.</param>
    [TestMethod]
    [DataRow(9_223_372_036_854_775_808UL, DisplayName = "long.MaxValue + 1")]
    [DataRow(ulong.MaxValue, DisplayName = "ulong.MaxValue")]
    public void SerializeDeserialize_WhenUInt64BasedValueExceedsInt64_ForNumberConverter_ShouldRoundTrip(ulong value)
    {
        var model = new UnsignedNumberModel { Value = (UInt64BasedLevel)value };

        UnsignedNumberModel roundTripped = BencodeSerializer.Deserialize<UnsignedNumberModel>(
            BencodeSerializer.Serialize(model));

        Assert.AreEqual((UInt64BasedLevel)value, roundTripped.Value);
    }

    /// <summary>
    /// Deserializes a range model, through the by-name default or the number converter.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <param name="numberConverter">
    /// Whether the members read through <see cref="BencodeNumberEnumConverter{TEnum}" />.
    /// </param>
    /// <returns>The bound model.</returns>
    private static object DeserializeRangeModel(string document, bool numberConverter)
    {
        byte[] bytes = Encoding.Latin1.GetBytes(document);
        return numberConverter
            ? BencodeSerializer.Deserialize<NumberUnderlyingRangeModel>(bytes)
            : BencodeSerializer.Deserialize<UnderlyingRangeModel>(bytes);
    }

    /// <summary>
    /// Reads a member of a range model as the decimal text of its underlying integer.
    /// </summary>
    /// <param name="model">The bound model.</param>
    /// <param name="member">The member's name.</param>
    /// <returns>The member's value as the decimal text of its underlying integer.</returns>
    private static string UnderlyingText(object model, string member) =>
        ((Enum)model.GetType().GetProperty(member)!.GetValue(model)!).ToString("D");

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> with a naming policy writes a combination of flags that no single member
    /// names as the comma-separated wire names of its flags, in the order <see cref="Enum.ToString()" /> lists them,
    /// as <c>System.Text.Json</c>'s <c>JsonStringEnumConverter</c> does, rather than as the members' CLR names.
    /// </summary>
    /// <param name="value">The combined underlying value.</param>
    /// <param name="expected">The expected text.</param>
    [TestMethod]
    [DataRow(5, "can_read, can_execute", DisplayName = "two single flags")]
    [DataRow(6, "can_write, can_execute", DisplayName = "two other single flags")]
    [DataRow(7, "can_read_write, can_execute", DisplayName = "a composite member and a flag")]
    public void Serialize_WhenCombinedFlagsUnderNamingPolicy_ShouldWriteEachFlagsWireName(int value, string expected)
    {
        string text = Encoding.UTF8.GetString(BencodeSerializer.Serialize(new AccessModel { Access = (AccessFlags)value }, SnakeCaseEnumOptions(allowIntegerValues: true)));

        Assert.AreEqual($"d6:Access{expected.Length}:{expected}e", text);
    }

    /// <summary>
    /// Verifies that a combination of flags one of which carries a <see cref="StringEnumMemberNameAttribute" /> name
    /// is written with that configured name for that flag.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCombinedFlagsIncludeRenamedMember_ShouldWriteConfiguredName()
    {
        string expected = "r, Write";

        string text = Encoding.UTF8.GetString(BencodeSerializer.Serialize(new RenamedFlagsModel { Flags = RenamedFlags.Read | RenamedFlags.Write }));

        Assert.AreEqual($"d5:Flags{expected.Length}:{expected}e", text);
    }

    /// <summary>
    /// Verifies that a <see cref="BencodeStringEnumConverter" /> with a naming policy reads a comma-separated list of wire names as the
    /// combination of their flags, in any order and with spaces around each name, and tolerates a trailing comma, as
    /// <c>JsonStringEnumConverter</c> does.
    /// </summary>
    /// <param name="text">The string's text.</param>
    /// <param name="expected">The expected combined underlying value.</param>
    [TestMethod]
    [DataRow("can_read, can_execute", 5, DisplayName = "two wire names")]
    [DataRow("can_execute, can_read", 5, DisplayName = "two wire names in descending order")]
    [DataRow(" can_read , can_execute ", 5, DisplayName = "two wire names between spaces")]
    [DataRow("can_read_write, can_execute", 7, DisplayName = "a composite member's wire name and a flag")]
    [DataRow("can_read,", 1, DisplayName = "a wire name and a trailing comma")]
    public void Deserialize_WhenFlagsListUsesWireNames_ShouldBindCombination(string text, int expected)
    {
        AccessModel model = BencodeSerializer.Deserialize<AccessModel>(Encoding.UTF8.GetBytes($"d6:Access{text.Length}:{text}e"), SnakeCaseEnumOptions(allowIntegerValues: true));

        Assert.AreEqual((AccessFlags)expected, model.Access);
    }

    /// <summary>
    /// Verifies that a combination of flags written under a naming policy reads back to the same value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenCombinedFlagsUnderNamingPolicy_ShouldRoundTrip()
    {
        BencodeSerializerOptions options = SnakeCaseEnumOptions(allowIntegerValues: false);
        var model = new AccessModel { Access = AccessFlags.CanReadWrite | AccessFlags.CanExecute };

        string text = Encoding.UTF8.GetString(BencodeSerializer.Serialize(model, options));
        AccessModel roundTripped = BencodeSerializer.Deserialize<AccessModel>(Encoding.UTF8.GetBytes(text), options);

        Assert.AreEqual(model.Access, roundTripped.Access);
    }

    /// <summary>
    /// Verifies that a list with an empty name, before, between or instead of its names, is rejected with
    /// <see cref="BencodeSerializationException" /> rather than read as the names it does hold.
    /// </summary>
    /// <param name="text">The string's text.</param>
    [TestMethod]
    [DataRow("can_read, , can_execute", DisplayName = "an empty name between two names")]
    [DataRow(", can_read", DisplayName = "an empty name before a name")]
    [DataRow("", DisplayName = "the empty string")]
    [DataRow("  ", DisplayName = "white space only")]
    [DataRow("can_read,,", DisplayName = "a name and two trailing commas")]
    public void Deserialize_WhenFlagsListHoldsEmptyName_ShouldThrowBencodeSerializationException(string text)
    {
        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<AccessModel>(Encoding.UTF8.GetBytes($"d6:Access{text.Length}:{text}e"), SnakeCaseEnumOptions(allowIntegerValues: true));
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Verifies that a member that carries a <see cref="StringEnumMemberNameAttribute" /> name does not bind by its CLR
    /// name, in any case, as <c>JsonStringEnumConverter</c> does from .NET 9.
    /// </summary>
    /// <param name="text">The string's text.</param>
    [TestMethod]
    [DataRow("NotFound", DisplayName = "the CLR name")]
    [DataRow("notfound", DisplayName = "the CLR name in lower case")]
    public void Deserialize_WhenRenamedMemberIsReadByClrName_ShouldThrowBencodeSerializationException(string text)
    {
        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RenamedStatusModel>(Encoding.UTF8.GetBytes($"d6:Status{text.Length}:{text}e"));
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Verifies that a flag that carries a <see cref="StringEnumMemberNameAttribute" /> name does not bind by its CLR
    /// name inside a list of flags.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRenamedFlagIsReadByClrName_ShouldThrowBencodeSerializationException()
    {
        string text = "Read, Write";

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RenamedFlagsModel>(Encoding.UTF8.GetBytes($"d5:Flags{text.Length}:{text}e"));
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Verifies that a <see cref="StringEnumMemberNameAttribute" /> name binds only as configured, so the name in
    /// another case is rejected, as <c>JsonStringEnumConverter</c> rejects it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAttributeNameCaseDiffers_ShouldThrowBencodeSerializationException()
    {
        string text = "NOT-FOUND";

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RenamedStatusModel>(Encoding.UTF8.GetBytes($"d6:Status{text.Length}:{text}e"));
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Verifies that a name a naming policy produces binds only as produced, so the name in another case is rejected,
    /// as <c>JsonStringEnumConverter</c> rejects it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenPolicyNameCaseDiffers_ShouldThrowBencodeSerializationException()
    {
        string text = "CAN_READ";

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<AccessModel>(Encoding.UTF8.GetBytes($"d6:Access{text.Length}:{text}e"), SnakeCaseEnumOptions(allowIntegerValues: true));
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Verifies that under a naming policy a member without a <see cref="StringEnumMemberNameAttribute" /> name still
    /// binds by its CLR name, in any case and inside a list, as <c>JsonStringEnumConverter</c> binds it.
    /// </summary>
    /// <param name="text">The string's text.</param>
    /// <param name="expected">The expected underlying value.</param>
    [TestMethod]
    [DataRow("CanRead", 1, DisplayName = "the CLR name")]
    [DataRow("canread", 1, DisplayName = "the CLR name in lower case")]
    [DataRow("CanRead, CanExecute", 5, DisplayName = "a list of CLR names")]
    public void Deserialize_WhenUnrenamedMemberIsReadByClrName_ShouldBindIt(string text, int expected)
    {
        AccessModel model = BencodeSerializer.Deserialize<AccessModel>(Encoding.UTF8.GetBytes($"d6:Access{text.Length}:{text}e"), SnakeCaseEnumOptions(allowIntegerValues: true));

        Assert.AreEqual((AccessFlags)expected, model.Access);
    }

    /// <summary>
    /// Verifies that a list that holds an integer is rejected even by a converter that allows integer values, which
    /// binds an integer only as the whole text, as <c>JsonStringEnumConverter</c> does.
    /// </summary>
    /// <param name="text">The string's text.</param>
    [TestMethod]
    [DataRow("1, 4", DisplayName = "a list of integers")]
    [DataRow("CanRead, 4", DisplayName = "a CLR name and an integer")]
    [DataRow("can_read, 4", DisplayName = "a wire name and an integer")]
    public void Deserialize_WhenListHoldsIntegerAndIntegersAreAllowed_ShouldThrowBencodeSerializationException(string text)
    {
        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<AccessModel>(Encoding.UTF8.GetBytes($"d6:Access{text.Length}:{text}e"), SnakeCaseEnumOptions(allowIntegerValues: true));
        });

        Assert.IsNotNull(ex.Message);
    }

    /// <summary>
    /// Creates options that convert enumerations by name through a <see cref="BencodeStringEnumConverter" /> with the lower snake-case
    /// naming policy.
    /// </summary>
    /// <param name="allowIntegerValues">Whether the converter binds and writes integer values.</param>
    /// <returns>The options.</returns>
    private static BencodeSerializerOptions SnakeCaseEnumOptions(bool allowIntegerValues)
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new BencodeStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues));
        return options;
    }

    /// <summary>
    /// A flags enumeration whose members' names change under a naming policy, with a composite member.
    /// </summary>
    [Flags]
    private enum AccessFlags
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
    /// A flags enumeration one of whose members carries a <see cref="StringEnumMemberNameAttribute" /> name.
    /// </summary>
    [Flags]
    private enum RenamedFlags
    {
        /// <summary>
        /// The read flag, written under the name <c>r</c>.
        /// </summary>
        [StringEnumMemberName("r")]
        Read = 1,

        /// <summary>
        /// The write flag.
        /// </summary>
        Write = 2,
    }

    /// <summary>
    /// A model carrying an <see cref="AccessFlags" /> value.
    /// </summary>
    private sealed class AccessModel
    {
        /// <summary>
        /// Gets or sets the access.
        /// </summary>
        /// <value>The access.</value>
        public AccessFlags Access { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="RenamedFlags" /> value.
    /// </summary>
    private sealed class RenamedFlagsModel
    {
        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>The flags.</value>
        public RenamedFlags Flags { get; set; }
    }

    /// <summary>
    /// An enumeration whose members map to byte-string names by default.
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
    /// An enumeration whose members carry explicit byte-string names through
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
    /// A model whose status is mapped to a Bencode integer through a converter attribute.
    /// </summary>
    private sealed class NumberEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as a Bencode integer.
        /// </summary>
        /// <value>The status.</value>
        [Converter(typeof(BencodeNumberEnumConverter<Status>))]
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model whose status is mapped to a member-name byte string through a converter attribute.
    /// </summary>
    private sealed class StringEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as its member-name byte string.
        /// </summary>
        /// <value>The status.</value>
        [Converter(typeof(BencodeStringEnumConverter<Status>))]
        public Status Status { get; set; }
    }

    /// <summary>
    /// A flags enumeration used to exercise the by-name converter's combined-value fallback.
    /// </summary>
    [Flags]
    private enum FlagsEnum
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
    /// A model carrying a <see cref="FlagsEnum" /> value mapped to a member-name byte string by default.
    /// </summary>
    private sealed class FlagsModel
    {
        /// <summary>
        /// Gets or sets the flags.
        /// </summary>
        /// <value>The flags.</value>
        public FlagsEnum Flags { get; set; }
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
    /// A model with a member of each enumeration, each read through <see cref="BencodeNumberEnumConverter{TEnum}" />.
    /// </summary>
    private sealed class NumberUnderlyingRangeModel
    {
        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="sbyte" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<SByteBasedLevel>))]
        public SByteBasedLevel SByte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="byte" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<ByteBasedLevel>))]
        public ByteBasedLevel Byte { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="short" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<Int16BasedLevel>))]
        public Int16BasedLevel Int16 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="int" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<Int32BasedLevel>))]
        public Int32BasedLevel Int32 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="long" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<Int64BasedLevel>))]
        public Int64BasedLevel Int64 { get; set; }

        /// <summary>
        /// Gets or sets the member whose enumeration is based on <see cref="ulong" />.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<UInt64BasedLevel>))]
        public UInt64BasedLevel UInt64 { get; set; }
    }

    /// <summary>
    /// A model with one member of the enumeration based on <see cref="ulong" />, read and written through
    /// <see cref="BencodeNumberEnumConverter{TEnum}" />.
    /// </summary>
    private sealed class UnsignedNumberModel
    {
        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        /// <value>The value.</value>
        [Converter(typeof(BencodeNumberEnumConverter<UInt64BasedLevel>))]
        public UInt64BasedLevel Value { get; set; }
    }
}
