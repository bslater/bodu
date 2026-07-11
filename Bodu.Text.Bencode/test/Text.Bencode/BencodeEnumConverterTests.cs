// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeEnumConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
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
}
