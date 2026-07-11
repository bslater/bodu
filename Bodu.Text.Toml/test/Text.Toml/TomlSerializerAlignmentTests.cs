// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerAlignmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Serialization;

using Bodu.Text.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the additions that align the TOML serializer's public surface with
/// <see cref="System.Text.Json.JsonSerializer" />: the <see cref="TomlSerializerOptions.DefaultIgnoreCondition" />,
/// the required/include/extension-data attributes, the serialization callbacks,
/// <see cref="UnmappedMemberHandling" /> and <see cref="ObjectCreationHandling" /> handling, the per-type
/// naming policy, the <see cref="TomlSerializerDefaults" /> constructor, the shared <see cref="SerializationAttribute" /> base
/// type, and the enum converters.
/// </summary>
[TestClass]
public class TomlSerializerAlignmentTests
{
    /// <summary>
    /// Verifies that <see cref="IgnoreCondition.WhenWritingDefault" /> as the default ignore condition omits a member
    /// equal to its type default while retaining a non-default member.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenDefaultIgnoreConditionIsWhenWritingDefault_ShouldOmitDefaultValuedMembers()
    {
        var options = new TomlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingDefault };

        string zeroCount = TomlSerializer.Serialize(new CountModel { Count = 0, Label = "x" }, options);
        Assert.AreEqual("Label = \"x\"\n", zeroCount);

        string nonZeroCount = TomlSerializer.Serialize(new CountModel { Count = 7, Label = "x" }, options);
        Assert.AreEqual("Count = 7\nLabel = \"x\"\n", nonZeroCount);
    }

    /// <summary>
    /// Verifies that the default <see cref="IgnoreCondition.Never" /> ignore condition keeps a non-null member whose
    /// value equals its type default.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultIgnoreConditionIsNever_ShouldKeepDefaultValuedMembers()
    {
        string text = TomlSerializer.Serialize(new CountModel { Count = 0, Label = "x" });

        Assert.AreEqual("Count = 0\nLabel = \"x\"\n", text);
    }

    /// <summary>
    /// Verifies that setting <see cref="TomlSerializerOptions.DefaultIgnoreCondition" /> to
    /// <see cref="IgnoreCondition.Always" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenSetToAlways_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new TomlSerializerOptions();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            options.DefaultIgnoreCondition = IgnoreCondition.Always;
        }, "value");
    }

    /// <summary>
    /// Verifies that deserializing a document that omits a member annotated with <see cref="RequiredAttribute" />
    /// throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredMemberMissing_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RequiredMemberModel>("Optional = \"o\"\n");
        });
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="RequiredAttribute" /> round-trips when present in the input.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenRequiredMemberPresent_ShouldRoundTrip()
    {
        var original = new RequiredMemberModel { Name = "server", Optional = "o" };

        string text = TomlSerializer.Serialize(original);
        RequiredMemberModel roundTripped = TomlSerializer.Deserialize<RequiredMemberModel>(text);

        Assert.AreEqual("server", roundTripped.Name);
        Assert.AreEqual("o", roundTripped.Optional);
    }

    /// <summary>
    /// Verifies that a property with a non-public setter annotated with <see cref="IncludeAttribute" /> is both
    /// serialized and round-tripped, binding through the non-public accessor.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenIncludeAttributeOnPrivateSetter_ShouldRoundTrip()
    {
        var original = new IncludedModel(42);

        string text = TomlSerializer.Serialize(original);
        Assert.AreEqual("Value = 42\n", text);

        IncludedModel roundTripped = TomlSerializer.Deserialize<IncludedModel>(text);
        Assert.AreEqual(42, roundTripped.Value);
    }

    /// <summary>
    /// Verifies that deserializing a table with keys that match no normal member captures them into the member annotated
    /// with <see cref="ExtensionDataAttribute" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtraKeysPresent_ShouldCaptureIntoExtensionData()
    {
        ExtensionDataModel model = TomlSerializer.Deserialize<ExtensionDataModel>("Name = \"test\"\na = 1\nz = 9\n");

        Assert.AreEqual("test", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(2, model.Extra.Count);
        Assert.AreEqual(1L, model.Extra["a"]!.GetValue<long>());
        Assert.AreEqual(9L, model.Extra["z"]!.GetValue<long>());
    }

    /// <summary>
    /// Verifies that the entries captured by a <see cref="ExtensionDataAttribute" /> member are written back out in
    /// document order alongside the type's normal members.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionDataPopulated_ShouldEmitEntriesAlongsideMembers()
    {
        var model = new ExtensionDataModel
        {
            Name = "test",
            Extra = new TomlObject
            {
                ["a"] = TomlValue.Create(1),
                ["z"] = TomlValue.Create(9),
            },
        };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Name = \"test\"\na = 1\nz = 9\n", text);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TomlSerializerOptions" /> with <see cref="TomlSerializerDefaults.Web" />
    /// lowercases property keys to camel case and matches property names case-insensitively when reading.
    /// </summary>
    [TestMethod]
    public void WebDefaults_WhenSerializingAndDeserializing_ShouldUseCamelCaseAndMatchCaseInsensitively()
    {
        var options = new TomlSerializerOptions(TomlSerializerDefaults.Web);

        string text = TomlSerializer.Serialize(new CountModel { Count = 3, Label = "y" }, options);
        Assert.AreEqual("count = 3\nlabel = \"y\"\n", text);

        CountModel roundTripped = TomlSerializer.Deserialize<CountModel>(text, options);
        Assert.AreEqual(3, roundTripped.Count);
        Assert.AreEqual("y", roundTripped.Label);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TomlSerializerOptions" /> with an undefined
    /// <see cref="TomlSerializerDefaults" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>defaults</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new TomlSerializerOptions((TomlSerializerDefaults)99);
        }, "defaults");
    }

    /// <summary>
    /// Verifies that every TOML serialization attribute derives from <see cref="SerializationAttribute" />, mirroring the way the
    /// <c>System.Text.Json</c> attributes derive from <see cref="System.Text.Json.Serialization.JsonAttribute" />.
    /// </summary>
    [TestMethod]
    public void Attributes_WhenInspected_ShouldDeriveFromSerializationAttribute()
    {
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(PropertyNameAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(Bodu.Text.Serialization.IgnoreAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(ConverterAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(PropertyOrderAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(ConstructorAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(RequiredAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(IncludeAttribute)));
        Assert.IsTrue(typeof(SerializationAttribute).IsAssignableFrom(typeof(ExtensionDataAttribute)));
    }

    /// <summary>
    /// Verifies that serializing a value invokes <see cref="IOnSerializing.OnSerializing" /> before
    /// <see cref="IOnSerialized.OnSerialized" /> and records both in order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenValueImplementsCallbacks_ShouldInvokeSerializingThenSerialized()
    {
        var model = new CallbackModel { Value = 1 };

        _ = TomlSerializer.Serialize(model);

        CollectionAssert.AreEqual(new[] { "OnSerializing", "OnSerialized" }, model.Calls);
    }

    /// <summary>
    /// Verifies that deserializing into a value invokes <see cref="IOnDeserializing.OnDeserializing" /> before
    /// <see cref="IOnDeserialized.OnDeserialized" /> and records both in order.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueImplementsCallbacks_ShouldInvokeDeserializingThenDeserialized()
    {
        CallbackModel model = TomlSerializer.Deserialize<CallbackModel>("Value = 7\n");

        CollectionAssert.AreEqual(new[] { "OnDeserializing", "OnDeserialized" }, model.Calls);
    }

    /// <summary>
    /// Verifies that <see cref="IOnDeserializing.OnDeserializing" /> runs before the settable members are assigned,
    /// observing the still-unpopulated instance.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCallbackOrdered_ShouldRunDeserializingBeforeMemberAssignment()
    {
        CallbackModel model = TomlSerializer.Deserialize<CallbackModel>("Value = 7\n");

        Assert.AreEqual(0, model.ValueAtDeserializing);
        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that deserializing a table with an unmapped key succeeds under the default
    /// <see cref="UnmappedMemberHandling.Skip" /> handling.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndHandlingIsSkip_ShouldIgnoreUnmappedKey()
    {
        PlainModel model = TomlSerializer.Deserialize<PlainModel>("Extra = 9\nValue = 3\n");

        Assert.AreEqual(3, model.Value);
    }

    /// <summary>
    /// Verifies that deserializing a table with an unmapped key throws <see cref="TomlSerializationException" /> when the
    /// options select <see cref="UnmappedMemberHandling.Disallow" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndOptionsDisallow_ShouldThrowTomlSerializationException()
    {
        var options = new TomlSerializerOptions { UnmappedMemberHandling = UnmappedMemberHandling.Disallow };

        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<PlainModel>("Extra = 9\nValue = 3\n", options);
        });
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="UnmappedMemberHandlingAttribute" /> set to
    /// <see cref="UnmappedMemberHandling.Disallow" /> rejects an unmapped key even when the options default to
    /// <see cref="UnmappedMemberHandling.Skip" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypeDisallowsUnmapped_ShouldThrowRegardlessOfOptions()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<DisallowUnmappedModel>("Extra = 9\nValue = 3\n");
        });
    }

    /// <summary>
    /// Verifies that an unmapped key is still captured into an extension-data member rather than rejected, even when the
    /// type disallows unmapped members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataPresentAndDisallow_ShouldCaptureUnmappedKey()
    {
        DisallowWithExtensionDataModel model = TomlSerializer.Deserialize<DisallowWithExtensionDataModel>("Extra = 9\nValue = 3\n");

        Assert.AreEqual(3, model.Value);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(9L, model.Extra["Extra"]!.GetValue<long>());
    }

    /// <summary>
    /// Verifies that a get-only collection property annotated with <see cref="ObjectCreationHandlingAttribute" /> set
    /// to <see cref="ObjectCreationHandling.Populate" /> has the read entries added into its pre-seeded instance
    /// rather than replacing it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberPopulates_ShouldAddIntoExistingCollection()
    {
        PopulateMemberModel model = TomlSerializer.Deserialize<PopulateMemberModel>("Tags = [1, 2]\n");

        CollectionAssert.AreEqual(new[] { 99, 1, 2 }, model.Tags);
    }

    /// <summary>
    /// Verifies that <see cref="ObjectCreationHandling.Populate" /> selected through the options applies to a
    /// get-only collection property, adding the read entries into its pre-seeded instance.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPreferPopulate_ShouldAddIntoExistingCollection()
    {
        var options = new TomlSerializerOptions { PreferredObjectCreationHandling = ObjectCreationHandling.Populate };

        GetOnlyCollectionModel model = TomlSerializer.Deserialize<GetOnlyCollectionModel>("Tags = [1, 2]\n", options);

        CollectionAssert.AreEqual(new[] { 99, 1, 2 }, model.Tags);
    }

    /// <summary>
    /// Verifies that under the default <see cref="ObjectCreationHandling.Replace" /> handling a get-only collection
    /// property is not populated, so its pre-seeded contents are unchanged.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenHandlingIsReplaceAndMemberGetOnly_ShouldNotPopulate()
    {
        GetOnlyCollectionModel model = TomlSerializer.Deserialize<GetOnlyCollectionModel>("Tags = [1, 2]\n");

        CollectionAssert.AreEqual(new[] { 99 }, model.Tags);
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="NamingPolicyAttribute" /> set to
    /// <see cref="KnownNamingPolicy.CamelCase" /> serializes its members with camel-cased keys even when the options'
    /// naming policy is unset.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeSelectsCamelCasePolicy_ShouldEmitCamelCaseKeys()
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = null };

        string text = TomlSerializer.Serialize(new CamelCaseTypeModel { FirstName = "a", LastName = "b" }, options);

        Assert.AreEqual("firstName = \"a\"\nlastName = \"b\"\n", text);
    }

    /// <summary>
    /// Verifies that an enumeration with no explicit converter serializes as its member-name string by default and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDefaultEnum_ShouldWriteMemberNameString()
    {
        var model = new StatusModel { Status = Status.Active };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"Active\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>(text);
        Assert.AreEqual(Status.Active, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that <see cref="StringEnumMemberNameAttribute" /> overrides the string name used for an individual
    /// enumeration member on both write and read.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenEnumMemberNameAttributePresent_ShouldUseOverriddenName()
    {
        var model = new RenamedStatusModel { Status = RenamedStatus.NotFound };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = \"not-found\"\n", text);

        RenamedStatusModel roundTripped = TomlSerializer.Deserialize<RenamedStatusModel>(text);
        Assert.AreEqual(RenamedStatus.NotFound, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a property annotated with <see cref="ConverterAttribute" /> naming a
    /// <see cref="TomlNumberEnumConverter{TEnum}" /> serializes the enumeration as a TOML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNumberEnumConverterAttributeOnProperty_ShouldWriteIntegerAndRoundTrip()
    {
        var model = new NumberEnumModel { Status = Status.Archived };

        string text = TomlSerializer.Serialize(model);

        Assert.AreEqual("Status = 2\n", text);

        NumberEnumModel roundTripped = TomlSerializer.Deserialize<NumberEnumModel>(text);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// Verifies that a <see cref="TomlStringEnumConverter" /> registered with a camel-case policy writes a Pascal-case
    /// member as its camel-cased name and reads an integer back as the corresponding enumeration value.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringEnumConverterWithCamelCasePolicy_ShouldCamelCaseMemberName()
    {
        var options = new TomlSerializerOptions();
        options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.CamelCase, allowIntegerValues: true));

        string text = TomlSerializer.Serialize(new StatusModel { Status = Status.Active }, options);
        Assert.AreEqual("Status = \"active\"\n", text);

        StatusModel roundTripped = TomlSerializer.Deserialize<StatusModel>("Status = 2\n", options);
        Assert.AreEqual(Status.Archived, roundTripped.Status);
    }

    /// <summary>
    /// A model with an integer member, used to exercise default-value and naming behavior.
    /// </summary>
    private sealed class CountModel
    {
        /// <summary>Gets or sets the integer count.</summary>
        /// <value>The count.</value>
        public int Count { get; set; }

        /// <summary>Gets or sets the label.</summary>
        /// <value>The label.</value>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model with a member marked required through <see cref="RequiredAttribute" />.
    /// </summary>
    private sealed class RequiredMemberModel
    {
        /// <summary>Gets or sets the required name.</summary>
        /// <value>The name.</value>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional value.</summary>
        /// <value>The optional value.</value>
        public string Optional { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose member has a non-public setter forced into serialization by <see cref="IncludeAttribute" />.
    /// </summary>
    private sealed class IncludedModel
    {
        /// <summary>Initializes a new instance of the <see cref="IncludedModel" /> class.</summary>
        public IncludedModel()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="IncludedModel" /> class with the specified value.</summary>
        /// <param name="value">The value to store.</param>
        public IncludedModel(int value)
        {
            Value = value;
        }

        /// <summary>Gets the value, exposed through a non-public setter that <see cref="IncludeAttribute" /> opts into.</summary>
        /// <value>The value.</value>
        [Include]
        public int Value { get; private set; }
    }

    /// <summary>
    /// A model with a normal member and a <see cref="ExtensionDataAttribute" /> member that captures unmatched table
    /// entries.
    /// </summary>
    private sealed class ExtensionDataModel
    {
        /// <summary>Gets or sets the name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the captured entries that match no other member.</summary>
        /// <value>The captured entries.</value>
        [ExtensionData]
        public TomlObject? Extra { get; set; }
    }

    /// <summary>
    /// A model that records each serialization callback the serializer invokes.
    /// </summary>
    private sealed class CallbackModel
        : IOnSerializing, IOnSerialized, IOnDeserializing, IOnDeserialized
    {
        /// <summary>Gets the ordered list of callback names the serializer has invoked.</summary>
        /// <value>The recorded callback names.</value>
        public List<string> Calls { get; } = new();

        /// <summary>Gets or sets the integer value carried by the model.</summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <summary>Gets the value observed at the moment <see cref="OnDeserializing" /> ran.</summary>
        /// <value>The value observed during the deserializing callback.</value>
        public int ValueAtDeserializing { get; private set; }

        /// <inheritdoc />
        public void OnSerializing() =>
            Calls.Add("OnSerializing");

        /// <inheritdoc />
        public void OnSerialized() =>
            Calls.Add("OnSerialized");

        /// <inheritdoc />
        public void OnDeserializing()
        {
            ValueAtDeserializing = Value;
            Calls.Add("OnDeserializing");
        }

        /// <inheritdoc />
        public void OnDeserialized() =>
            Calls.Add("OnDeserialized");
    }

    /// <summary>
    /// A simple model with a single integer member.
    /// </summary>
    private sealed class PlainModel
    {
        /// <summary>Gets or sets the integer value.</summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model that disallows unmapped members through <see cref="UnmappedMemberHandlingAttribute" />.
    /// </summary>
    [UnmappedMemberHandling(UnmappedMemberHandling.Disallow)]
    private sealed class DisallowUnmappedModel
    {
        /// <summary>Gets or sets the integer value.</summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model that disallows unmapped members yet still captures them into an extension-data member.
    /// </summary>
    [UnmappedMemberHandling(UnmappedMemberHandling.Disallow)]
    private sealed class DisallowWithExtensionDataModel
    {
        /// <summary>Gets or sets the integer value.</summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <summary>Gets or sets the captured entries that match no other member.</summary>
        /// <value>The captured entries.</value>
        [ExtensionData]
        public TomlObject? Extra { get; set; }
    }

    /// <summary>
    /// A model whose get-only collection member is populated through a member-level
    /// <see cref="ObjectCreationHandlingAttribute" />.
    /// </summary>
    private sealed class PopulateMemberModel
    {
        /// <summary>Gets the pre-seeded tag list that the serializer populates rather than replaces.</summary>
        /// <value>The tags.</value>
        [ObjectCreationHandling(ObjectCreationHandling.Populate)]
        public List<int> Tags { get; } = new() { 99 };
    }

    /// <summary>
    /// A model with a get-only collection member and no creation-handling attribute, driven by the options-level
    /// preference.
    /// </summary>
    private sealed class GetOnlyCollectionModel
    {
        /// <summary>Gets the pre-seeded tag list.</summary>
        /// <value>The tags.</value>
        public List<int> Tags { get; } = new() { 99 };
    }

    /// <summary>
    /// A model that selects camel-case naming for all of its members through <see cref="NamingPolicyAttribute" />.
    /// </summary>
    [NamingPolicy(KnownNamingPolicy.CamelCase)]
    private sealed class CamelCaseTypeModel
    {
        /// <summary>Gets or sets the first name.</summary>
        /// <value>The first name.</value>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>Gets or sets the last name.</summary>
        /// <value>The last name.</value>
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// An enumeration whose members map to string names by default.
    /// </summary>
    private enum Status
    {
        /// <summary>The pending state, with underlying value <c>0</c>.</summary>
        Pending = 0,

        /// <summary>The active state, with underlying value <c>1</c>.</summary>
        Active = 1,

        /// <summary>The archived state, with underlying value <c>2</c>.</summary>
        Archived = 2,
    }

    /// <summary>
    /// An enumeration whose members carry explicit names through <see cref="StringEnumMemberNameAttribute" />.
    /// </summary>
    private enum RenamedStatus
    {
        /// <summary>The found state.</summary>
        Found = 0,

        /// <summary>The not-found state, written under the name <c>not-found</c>.</summary>
        [StringEnumMemberName("not-found")]
        NotFound = 1,
    }

    /// <summary>
    /// A model carrying a <see cref="Status" /> enumeration value.
    /// </summary>
    private sealed class StatusModel
    {
        /// <summary>Gets or sets the status.</summary>
        /// <value>The status.</value>
        public Status Status { get; set; }
    }

    /// <summary>
    /// A model carrying a <see cref="RenamedStatus" /> enumeration value.
    /// </summary>
    private sealed class RenamedStatusModel
    {
        /// <summary>Gets or sets the status.</summary>
        /// <value>The status.</value>
        public RenamedStatus Status { get; set; }
    }

    /// <summary>
    /// A model whose status is mapped to a TOML integer through a converter attribute.
    /// </summary>
    private sealed class NumberEnumModel
    {
        /// <summary>Gets or sets the status, serialized as a TOML integer.</summary>
        /// <value>The status.</value>
        [Converter(typeof(TomlNumberEnumConverter<Status>))]
        public Status Status { get; set; }
    }
}
