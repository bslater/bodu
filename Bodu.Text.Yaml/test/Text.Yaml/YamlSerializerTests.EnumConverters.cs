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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text);
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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options);
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
        StatusModel model = YamlSerializer.Deserialize<StatusModel>("Status: 2\n");

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

        RenamedStatusModel roundTripped = YamlSerializer.Deserialize<RenamedStatusModel>(text);
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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options);
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

        NumberEnumModel roundTripped = YamlSerializer.Deserialize<NumberEnumModel>(text);
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

        StringEnumModel roundTripped = YamlSerializer.Deserialize<StringEnumModel>(text);
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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter matches a member name case-insensitively on read, so an upper-cased wire
    /// scalar binds to a Pascal-case member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumStringCaseDiffers_ShouldMatchCaseInsensitively()
    {
        StatusModel model = YamlSerializer.Deserialize<StatusModel>("Status: ACTIVE\n");

        Assert.AreEqual(Status.Active, model.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter reads a numeric string by parsing it through the runtime, so a member
    /// stored as its quoted decimal text still binds to the enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumReadsNumericString_ShouldParseToValue()
    {
        StatusModel model = YamlSerializer.Deserialize<StatusModel>("Status: \"2\"\n");

        Assert.AreEqual(Status.Archived, model.Status);
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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options);
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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text, options);
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
        NumberEnumModel model = YamlSerializer.Deserialize<NumberEnumModel>("Status: null\n");

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

        StatusModel roundTripped = YamlSerializer.Deserialize<StatusModel>(text);
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

        FlagsModel roundTripped = YamlSerializer.Deserialize<FlagsModel>(text);
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

        FlagsModel roundTripped = YamlSerializer.Deserialize<FlagsModel>(text);
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
}
