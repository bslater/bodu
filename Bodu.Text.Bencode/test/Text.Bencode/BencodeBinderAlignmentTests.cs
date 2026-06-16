// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeBinderAlignmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the binder features that align the Bencode serializer with <see cref="System.Text.Json.JsonSerializer" />:
/// the serialization callback interfaces, <see cref="BencodeUnmappedMemberHandling" /> handling,
/// <see cref="BencodeObjectCreationHandling" /> populate behavior, and the per-type
/// <see cref="BencodeNamingPolicyAttribute" />.
/// </summary>
[TestClass]
public class BencodeBinderAlignmentTests
{
    /// <summary>
    /// Verifies that serializing a value invokes <see cref="IBencodeOnSerializing.OnSerializing" /> before
    /// <see cref="IBencodeOnSerialized.OnSerialized" /> and records both in order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Serialize_WhenValueImplementsCallbacks_ShouldInvokeSerializingThenSerialized()
    {
        var model = new CallbackModel { Value = 1 };

        _ = BencodeSerializer.Serialize(model);

        CollectionAssert.AreEqual(new[] { "OnSerializing", "OnSerialized" }, model.Calls);
    }

    /// <summary>
    /// Verifies that deserializing into a value invokes <see cref="IBencodeOnDeserializing.OnDeserializing" /> before
    /// <see cref="IBencodeOnDeserialized.OnDeserialized" /> and records both in order.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenValueImplementsCallbacks_ShouldInvokeDeserializingThenDeserialized()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        var model = BencodeSerializer.Deserialize<CallbackModel>(bytes);

        CollectionAssert.AreEqual(new[] { "OnDeserializing", "OnDeserialized" }, model.Calls);
    }

    /// <summary>
    /// Verifies that <see cref="IBencodeOnDeserializing.OnDeserializing" /> runs before the settable members are
    /// assigned, observing the still-unpopulated instance.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCallbackOrdered_ShouldRunDeserializingBeforeMemberAssignment()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        var model = BencodeSerializer.Deserialize<CallbackModel>(bytes);

        Assert.AreEqual(0, model.ValueAtDeserializing);
        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that deserializing a dictionary with an unmapped key succeeds under the default
    /// <see cref="BencodeUnmappedMemberHandling.Skip" /> handling.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndHandlingIsSkip_ShouldIgnoreUnmappedKey()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Extrai9e5:Valuei3ee");

        var model = BencodeSerializer.Deserialize<PlainModel>(bytes);

        Assert.AreEqual(3, model.Value);
    }

    /// <summary>
    /// Verifies that deserializing a dictionary with an unmapped key throws
    /// <see cref="BencodeSerializationException" /> when the options select
    /// <see cref="BencodeUnmappedMemberHandling.Disallow" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeyAndOptionsDisallow_ShouldThrowBencodeSerializationException()
    {
        var options = new BencodeSerializerOptions { UnmappedMemberHandling = BencodeUnmappedMemberHandling.Disallow };
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Extrai9e5:Valuei3ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<PlainModel>(bytes, options);
        });
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="BencodeUnmappedMemberHandlingAttribute" /> set to
    /// <see cref="BencodeUnmappedMemberHandling.Disallow" /> rejects an unmapped key even when the options default to
    /// <see cref="BencodeUnmappedMemberHandling.Skip" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTypeDisallowsUnmapped_ShouldThrowRegardlessOfOptions()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Extrai9e5:Valuei3ee");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<DisallowUnmappedModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that an unmapped key is still captured into an extension-data member rather than rejected, even when
    /// the type disallows unmapped members.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataPresentAndDisallow_ShouldCaptureUnmappedKey()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Extrai9e5:Valuei3ee");

        var model = BencodeSerializer.Deserialize<DisallowWithExtensionDataModel>(bytes);

        Assert.AreEqual(3, model.Value);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(9L, model.Extra["Extra"]!.GetValue<long>());
    }

    /// <summary>
    /// Verifies that a get-only collection property annotated with
    /// <see cref="BencodeObjectCreationHandlingAttribute" /> set to <see cref="BencodeObjectCreationHandling.Populate" />
    /// has the read entries added into its pre-seeded instance rather than replacing it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMemberPopulates_ShouldAddIntoExistingCollection()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Tagsli1ei2eee");

        var model = BencodeSerializer.Deserialize<PopulateMemberModel>(bytes);

        CollectionAssert.AreEqual(new[] { 99, 1, 2 }, model.Tags);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeObjectCreationHandling.Populate" /> selected through the options applies to a
    /// get-only collection property, adding the read entries into its pre-seeded instance.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOptionsPreferPopulate_ShouldAddIntoExistingCollection()
    {
        var options = new BencodeSerializerOptions { PreferredObjectCreationHandling = BencodeObjectCreationHandling.Populate };
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Tagsli1ei2eee");

        var model = BencodeSerializer.Deserialize<GetOnlyCollectionModel>(bytes, options);

        CollectionAssert.AreEqual(new[] { 99, 1, 2 }, model.Tags);
    }

    /// <summary>
    /// Verifies that under the default <see cref="BencodeObjectCreationHandling.Replace" /> handling a get-only
    /// collection property is not populated, so its pre-seeded contents are unchanged.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenHandlingIsReplaceAndMemberGetOnly_ShouldNotPopulate()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Tagsli1ei2eee");

        var model = BencodeSerializer.Deserialize<GetOnlyCollectionModel>(bytes);

        CollectionAssert.AreEqual(new[] { 99 }, model.Tags);
    }

    /// <summary>
    /// Verifies that a populatable dictionary member merges the read entries into its pre-seeded instance under
    /// <see cref="BencodeObjectCreationHandling.Populate" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDictionaryMemberPopulates_ShouldMergeIntoExistingDictionary()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d3:Mapd1:bi2eee");

        var model = BencodeSerializer.Deserialize<PopulateDictionaryModel>(bytes);

        Assert.AreEqual(2, model.Map.Count);
        Assert.AreEqual(1L, model.Map["a"]);
        Assert.AreEqual(2L, model.Map["b"]);
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="BencodeNamingPolicyAttribute" /> set to
    /// <see cref="BencodeKnownNamingPolicy.CamelCase" /> serializes its members with camel-cased keys even when the
    /// options' naming policy is unset.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypeSelectsCamelCasePolicy_ShouldEmitCamelCaseKeys()
    {
        var options = new BencodeSerializerOptions { PropertyNamingPolicy = null };

        byte[] bytes = BencodeSerializer.Serialize(new CamelCaseTypeModel { FirstName = "a", LastName = "b" }, options);

        Assert.AreEqual("d9:firstName1:a8:lastName1:be", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a member with an explicit <see cref="BencodePropertyNameAttribute" /> keeps its literal name while
    /// other members follow the type's <see cref="BencodeNamingPolicyAttribute" /> policy.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypePolicyAndExplicitName_ShouldPreferExplicitName()
    {
        byte[] bytes = BencodeSerializer.Serialize(new CamelCaseWithOverrideModel { FirstName = "a", LastName = "b" });

        Assert.AreEqual("d9:firstName1:a7:surname1:be", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// A model that records each serialization callback the serializer invokes, used to assert callback ordering.
    /// </summary>
    private sealed class CallbackModel
        : IBencodeOnSerializing, IBencodeOnSerialized, IBencodeOnDeserializing, IBencodeOnDeserialized
    {
        /// <summary>
        /// Gets the ordered list of callback names the serializer has invoked.
        /// </summary>
        /// <returns>The recorded callback names.</returns>
        public List<string> Calls { get; } = new();

        /// <summary>
        /// Gets or sets the integer value carried by the model.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; set; }

        /// <summary>
        /// Gets the value observed at the moment <see cref="OnDeserializing" /> ran, used to prove the callback
        /// precedes member assignment.
        /// </summary>
        /// <returns>The value observed during the deserializing callback.</returns>
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
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model that disallows unmapped members through <see cref="BencodeUnmappedMemberHandlingAttribute" />.
    /// </summary>
    [BencodeUnmappedMemberHandling(BencodeUnmappedMemberHandling.Disallow)]
    private sealed class DisallowUnmappedModel
    {
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model that disallows unmapped members yet still captures them into an extension-data member.
    /// </summary>
    [BencodeUnmappedMemberHandling(BencodeUnmappedMemberHandling.Disallow)]
    private sealed class DisallowWithExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the captured entries that match no other member.
        /// </summary>
        /// <returns>The captured entries.</returns>
        [BencodeExtensionData]
        public Nodes.BencodeObject? Extra { get; set; }
    }

    /// <summary>
    /// A model whose get-only collection member is populated through a member-level
    /// <see cref="BencodeObjectCreationHandlingAttribute" />.
    /// </summary>
    private sealed class PopulateMemberModel
    {
        /// <summary>
        /// Gets the pre-seeded tag list that the serializer populates rather than replaces.
        /// </summary>
        /// <returns>The tags.</returns>
        [BencodeObjectCreationHandling(BencodeObjectCreationHandling.Populate)]
        public List<int> Tags { get; } = new() { 99 };
    }

    /// <summary>
    /// A model with a get-only collection member and no creation-handling attribute, whose behavior is driven by the
    /// options-level preference.
    /// </summary>
    private sealed class GetOnlyCollectionModel
    {
        /// <summary>
        /// Gets the pre-seeded tag list.
        /// </summary>
        /// <returns>The tags.</returns>
        public List<int> Tags { get; } = new() { 99 };
    }

    /// <summary>
    /// A model whose get-only dictionary member is populated through a member-level
    /// <see cref="BencodeObjectCreationHandlingAttribute" />.
    /// </summary>
    private sealed class PopulateDictionaryModel
    {
        /// <summary>
        /// Gets the pre-seeded map that the serializer merges into rather than replaces.
        /// </summary>
        /// <returns>The map.</returns>
        [BencodeObjectCreationHandling(BencodeObjectCreationHandling.Populate)]
        public Dictionary<string, int> Map { get; } = new() { ["a"] = 1 };
    }

    /// <summary>
    /// A model that selects camel-case naming for all of its members through
    /// <see cref="BencodeNamingPolicyAttribute" />.
    /// </summary>
    [BencodeNamingPolicy(BencodeKnownNamingPolicy.CamelCase)]
    private sealed class CamelCaseTypeModel
    {
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <returns>The first name.</returns>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <returns>The last name.</returns>
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A camel-case model on which one member overrides the policy through
    /// <see cref="BencodePropertyNameAttribute" />.
    /// </summary>
    [BencodeNamingPolicy(BencodeKnownNamingPolicy.CamelCase)]
    private sealed class CamelCaseWithOverrideModel
    {
        /// <summary>
        /// Gets or sets the first name, named by the type's camel-case policy.
        /// </summary>
        /// <returns>The first name.</returns>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name, named explicitly so the policy does not apply.
        /// </summary>
        /// <returns>The last name.</returns>
        [BencodePropertyName("surname")]
        public string LastName { get; set; } = string.Empty;
    }
}
