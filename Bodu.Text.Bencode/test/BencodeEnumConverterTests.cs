// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeEnumConverterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the System.Text.Json-style enum-converter surface for Bencode: the built-in by-name default,
/// <see cref="BencodeStringEnumMemberNameAttribute" />, and the <see cref="BencodeStringEnumConverter" />,
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

        var roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes);
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
    /// Verifies that <see cref="BencodeStringEnumMemberNameAttribute" /> overrides the byte-string name used for an
    /// individual enumeration member on both write and read, even with no converter explicitly registered.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMemberNameAttributePresent_ShouldUseOverriddenName()
    {
        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Status9:not-founde", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<RenamedStatusModel>(bytes);
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
        options.Converters.Add(new BencodeStringEnumConverter(BencodeNamingPolicy.CamelCase, allowIntegerValues: true));

        var model = new StatusModel { Status = Status.Active };
        byte[] bytes = BencodeSerializer.Serialize(model, options);

        Assert.AreEqual("d6:Status6:activee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);
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
        options.Converters.Add(new BencodeStringEnumConverter(BencodeNamingPolicy.CamelCase, allowIntegerValues: true));

        byte[] bytes = Encoding.Latin1.GetBytes("d6:Statusi2ee");

        var roundTripped = BencodeSerializer.Deserialize<StatusModel>(bytes, options);

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
        options.Converters.Add(new BencodeStringEnumConverter(BencodeNamingPolicy.CamelCase, allowIntegerValues: true));

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
    /// Verifies that a property annotated with <see cref="BencodeConverterAttribute" /> naming a
    /// <see cref="BencodeNumberEnumConverter{TEnum}" /> serializes the enumeration as a Bencode integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterAttributeOnProperty_ShouldWriteIntegerAndRoundTrip()
    {
        var model = new NumberEnumModel { Status = Status.Archived };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Statusi2ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<NumberEnumModel>(bytes);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="BencodeConverterAttribute" /> naming a
    /// <see cref="BencodeStringEnumConverter{TEnum}" /> serializes the enumeration as its member-name byte string and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenStringEnumConverterAttributeOnProperty_ShouldWriteMemberNameAndRoundTrip()
    {
        var model = new StringEnumModel { Status = Status.Pending };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d6:Status7:Pendinge", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<StringEnumModel>(bytes);
        Assert.AreEqual(Status.Pending, roundTripped.Status);
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
    /// <see cref="BencodeStringEnumMemberNameAttribute" />.
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
        [BencodeStringEnumMemberName("not-found")]
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
    /// A model whose status is mapped to a Bencode integer through a converter attribute.
    /// </summary>
    private sealed class NumberEnumModel
    {
        /// <summary>
        /// Gets or sets the status, serialized as a Bencode integer.
        /// </summary>
        /// <returns>The status.</returns>
        [BencodeConverter(typeof(BencodeNumberEnumConverter<Status>))]
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
        /// <returns>The status.</returns>
        [BencodeConverter(typeof(BencodeStringEnumConverter<Status>))]
        public Status Status { get; set; }
    }
}
