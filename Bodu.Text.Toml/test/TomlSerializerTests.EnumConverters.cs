// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.EnumConverters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the System.Text.Json-style enum-converter surface for TOML: the built-in by-name default, the
/// <see cref="TomlStringEnumMemberNameAttribute" /> override, and the <see cref="TomlStringEnumConverter" />,
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

        var roundTripped = TomlSerializer.Deserialize<StatusModel>(text);
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
        var model = TomlSerializer.Deserialize<StatusModel>("Status = 2\n");

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that <see cref="TomlStringEnumMemberNameAttribute" /> overrides the string used for an individual
    /// enumeration member on both write and read, even with no converter explicitly registered.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemberNameAttributePresent_ShouldUseOverriddenName()
    {
        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"not-found\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<RenamedStatusModel>(text);
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
        options.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"active\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
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
        options.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.CamelCase, allowIntegerValues: true));

        var model = TomlSerializer.Deserialize<StatusModel>("Status = 2\n", options);

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
        options.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.CamelCase, allowIntegerValues: true));

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
    /// Verifies that a property annotated with <see cref="TomlConverterAttribute" /> naming a
    /// <see cref="TomlNumberEnumConverter{TEnum}" /> serializes the enumeration as a TOML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterAttribute_ShouldWriteIntegerAndRoundTrip()
    {
        var model = new NumberEnumModel { Status = Status.Archived };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = 2\n", text);

        var roundTripped = TomlSerializer.Deserialize<NumberEnumModel>(text);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="TomlConverterAttribute" /> naming a
    /// <see cref="TomlStringEnumConverter{TEnum}" /> serializes the enumeration as its member-name string and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterAttribute_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new StringEnumModel { Status = Status.Pending };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"Pending\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<StringEnumModel>(text);
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

        var roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter matches a member name case-insensitively on read, so an upper-cased wire
    /// string binds to a Pascal-case member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumStringCaseDiffers_ShouldMatchCaseInsensitively()
    {
        var model = TomlSerializer.Deserialize<StatusModel>("Status = \"ACTIVE\"\n");

        Assert.AreEqual(Status.Active, model.Status);
    }

    /// <summary>
    /// Verifies that the by-name converter reads a numeric string by parsing it through the runtime, so a member
    /// stored as its decimal text still binds to the enumeration value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumReadsNumericString_ShouldParseToValue()
    {
        var model = TomlSerializer.Deserialize<StatusModel>("Status = \"2\"\n");

        Assert.AreEqual(Status.Archived, model.Status);
    }

    /// <summary>
    /// Verifies that the generic <see cref="TomlStringEnumConverter{TEnum}" /> registered with a camel-case naming
    /// policy on the options writes a Pascal-case member as its camel-cased name and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenGenericStringEnumConverterWithPolicy_ShouldCamelCaseAndRoundTrip()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter<Status>(TomlNamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"active\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
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

        var roundTripped = TomlSerializer.Deserialize<StatusModel>(text, options);
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

        var roundTripped = TomlSerializer.Deserialize<StatusModel>(text);
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

        var roundTripped = TomlSerializer.Deserialize<FlagsModel>(text);
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

        var roundTripped = TomlSerializer.Deserialize<FlagsModel>(text);
        Assert.AreEqual(PermissionFlags.Write, roundTripped.Flags);
    }

    /// <summary>
    /// Verifies that <see cref="TomlStringEnumMemberNameAttribute" /> takes precedence over a naming policy applied by
    /// a <see cref="TomlStringEnumConverter" />, so the explicit name is used unchanged.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberNameAttributeAndNamingPolicy_ShouldPreferAttribute()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };
        string text = TomlSerializer.Serialize(model, options);

        Assert.AreEqual("Status = \"not-found\"\n", text);
    }

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
    /// <see cref="TomlStringEnumMemberNameAttribute" />.
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
        [TomlStringEnumMemberName("not-found")]
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
        /// <returns>The status.</returns>
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
        /// <returns>The status.</returns>
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
        /// <returns>The status.</returns>
        [TomlConverter(typeof(TomlNumberEnumConverter<Status>))]
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
        /// <returns>The status.</returns>
        [TomlConverter(typeof(TomlStringEnumConverter<Status>))]
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
        /// <returns>The flags.</returns>
        public PermissionFlags Flags { get; set; }
    }
}
