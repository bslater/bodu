// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerAlignmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the additions that align the Bencode serializer's public surface with
/// <see cref="System.Text.Json.JsonSerializer" />: <see cref="BencodeSerializerOptions.DefaultIgnoreCondition" />,
/// the <see cref="BencodeRequiredAttribute" />, <see cref="BencodeIncludeAttribute" />, and
/// <see cref="BencodeExtensionDataAttribute" /> attributes, the <see cref="BencodeSerializerDefaults" /> constructor, and
/// the shared <see cref="BencodeAttribute" /> base type.
/// </summary>
[TestClass]
public class BencodeSerializerAlignmentTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeIgnoreCondition.WhenWritingDefault" /> as the default ignore condition omits a
    /// member equal to its type default while retaining a non-default member.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenDefaultIgnoreConditionIsWhenWritingDefault_ShouldOmitDefaultValuedMembers()
    {
        var options = new BencodeSerializerOptions { DefaultIgnoreCondition = BencodeIgnoreCondition.WhenWritingDefault };

        var zeroCount = BencodeSerializer.Serialize(new CountModel { Count = 0, Label = "x" }, options);
        Assert.AreEqual("d5:Label1:xe", Encoding.Latin1.GetString(zeroCount));

        var nonZeroCount = BencodeSerializer.Serialize(new CountModel { Count = 7, Label = "x" }, options);
        Assert.AreEqual("d5:Counti7e5:Label1:xe", Encoding.Latin1.GetString(nonZeroCount));
    }

    /// <summary>
    /// Verifies that the default <see cref="BencodeIgnoreCondition.Never" /> ignore condition keeps a non-null member
    /// whose value equals its type default.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultIgnoreConditionIsNever_ShouldKeepDefaultValuedMembers()
    {
        var bytes = BencodeSerializer.Serialize(new CountModel { Count = 0, Label = "x" });

        Assert.AreEqual("d5:Counti0e5:Label1:xe", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that setting <see cref="BencodeSerializerOptions.DefaultIgnoreCondition" /> to
    /// <see cref="BencodeIgnoreCondition.Always" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenSetToAlways_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new BencodeSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.DefaultIgnoreCondition = BencodeIgnoreCondition.Always;
        }, "value");
    }

    /// <summary>
    /// Verifies that deserializing a document that omits a member annotated with <see cref="BencodeRequiredAttribute" />
    /// throws <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredMemberMissing_ShouldThrowBencodeSerializationException()
    {
        var bytes = Encoding.Latin1.GetBytes("d8:Optional1:oe");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RequiredMemberModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="BencodeRequiredAttribute" /> round-trips when present in the
    /// input.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRequiredMemberPresent_ShouldRoundTrip()
    {
        var original = new RequiredMemberModel { Name = "torrent", Optional = "o" };

        var bytes = BencodeSerializer.Serialize(original);
        var roundTripped = BencodeSerializer.Deserialize<RequiredMemberModel>(bytes);

        Assert.AreEqual("torrent", roundTripped.Name);
        Assert.AreEqual("o", roundTripped.Optional);
    }

    /// <summary>
    /// Verifies that a property with a non-public setter annotated with <see cref="BencodeIncludeAttribute" /> is both
    /// serialized and round-tripped, binding through the non-public accessor.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeAttributeOnPrivateSetter_ShouldRoundTrip()
    {
        var original = new IncludedModel(42);

        var bytes = BencodeSerializer.Serialize(original);
        Assert.AreEqual("d5:Valuei42ee", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<IncludedModel>(bytes);
        Assert.AreEqual(42, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that deserializing a dictionary with keys that match no normal member captures them into the member
    /// annotated with <see cref="BencodeExtensionDataAttribute" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtraKeysPresent_ShouldCaptureIntoExtensionData()
    {
        var bytes = Encoding.Latin1.GetBytes("d4:Name4:test1:ai1e1:zi9ee");

        var model = BencodeSerializer.Deserialize<ExtensionDataModel>(bytes);

        Assert.AreEqual("test", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(2, model.Extra.Count);
        Assert.AreEqual(1L, model.Extra["a"]!.GetValue<long>());
        Assert.AreEqual(9L, model.Extra["z"]!.GetValue<long>());
    }

    /// <summary>
    /// Verifies that the entries captured by an <see cref="BencodeExtensionDataAttribute" /> member are written back out
    /// merged into canonical ascending key order alongside the type's normal members.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionDataPopulated_ShouldEmitEntriesInCanonicalKeyOrder()
    {
        var model = new ExtensionDataModel
        {
            Name = "test",
            Extra = new BencodeObject
            {
                ["z"] = BencodeValue.Create(9),
                ["a"] = BencodeValue.Create(1),
            },
        };

        var bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d4:Name4:test1:ai1e1:zi9ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that constructing <see cref="BencodeSerializerOptions" /> with
    /// <see cref="BencodeSerializerDefaults.Web" /> lowercases property keys to camel case and matches property names
    /// case-insensitively when reading.
    /// </summary>
    [TestMethod]
    public void WebDefaults_WhenSerializingAndDeserializing_ShouldUseCamelCaseAndMatchCaseInsensitively()
    {
        var options = new BencodeSerializerOptions(BencodeSerializerDefaults.Web);

        var bytes = BencodeSerializer.Serialize(new CountModel { Count = 3, Label = "y" }, options);
        Assert.AreEqual("d5:counti3e5:label1:ye", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<CountModel>(bytes, options);
        Assert.AreEqual(3, roundTripped.Count);
        Assert.AreEqual("y", roundTripped.Label);
    }

    /// <summary>
    /// Verifies that constructing <see cref="BencodeSerializerOptions" /> with an undefined
    /// <see cref="BencodeSerializerDefaults" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>defaults</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new BencodeSerializerOptions((BencodeSerializerDefaults)99);
        }, "defaults");
    }

    /// <summary>
    /// Verifies that every Bencode serialization attribute derives from <see cref="BencodeAttribute" />, mirroring the
    /// way the <c>System.Text.Json</c> attributes derive from
    /// <see cref="System.Text.Json.Serialization.JsonAttribute" />.
    /// </summary>
    [TestMethod]
    public void Attributes_WhenInspected_ShouldDeriveFromBencodeAttribute()
    {
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodePropertyNameAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodeIgnoreAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodeConverterAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodePropertyOrderAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodeConstructorAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodeRequiredAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodeIncludeAttribute)));
        Assert.IsTrue(typeof(BencodeAttribute).IsAssignableFrom(typeof(BencodeExtensionDataAttribute)));
    }

    /// <summary>
    /// A model with an integer member, used to exercise default-value and naming behavior.
    /// </summary>
    private sealed class CountModel
    {
        /// <summary>
        /// Gets or sets the integer count.
        /// </summary>
        /// <returns>The count.</returns>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <returns>The label.</returns>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a member marked required through <see cref="BencodeRequiredAttribute" />.
    /// </summary>
    private sealed class RequiredMemberModel
    {
        /// <summary>
        /// Gets or sets the required name.
        /// </summary>
        /// <returns>The name.</returns>
        [BencodeRequired]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional value.
        /// </summary>
        /// <returns>The optional value.</returns>
        public string Optional { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose member has a non-public setter forced into serialization by
    /// <see cref="BencodeIncludeAttribute" />.
    /// </summary>
    private sealed class IncludedModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IncludedModel" /> class.
        /// </summary>
        public IncludedModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncludedModel" /> class with the specified value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        public IncludedModel(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value, exposed through a non-public setter that <see cref="BencodeIncludeAttribute" /> opts into.
        /// </summary>
        /// <returns>The value.</returns>
        [BencodeInclude]
        public int Value { get; private set; }
    }

    /// <summary>
    /// A model with a normal member and an <see cref="BencodeExtensionDataAttribute" /> member that captures unmatched
    /// dictionary entries.
    /// </summary>
    private sealed class ExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the captured entries that match no other member.
        /// </summary>
        /// <returns>The captured entries.</returns>
        [BencodeExtensionData]
        public BencodeObject? Extra { get; set; }
    }
}
