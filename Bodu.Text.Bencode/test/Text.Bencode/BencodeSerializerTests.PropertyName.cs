// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.PropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how the <see cref="BencodeSerializer" /> resolves dictionary keys to members on read: the
/// <see cref="BencodeSerializerOptions.PropertyNameCaseInsensitive" /> matching behavior, the precedence of
/// <see cref="BencodePropertyNameAttribute" /> over any naming policy, and the guard on a null attribute name.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that, under the default case-insensitive matching, a dictionary key whose casing differs from the
    /// member's CLR name is still bound to that member on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseInsensitiveDefaultAndKeyCasingDiffers_ShouldBindMember()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:VALUEi7ee");

        var model = BencodeSerializer.Deserialize<CaseInsensitiveModel>(bytes);

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that, when case-insensitive matching is disabled, a dictionary key whose casing differs from the
    /// member's CLR name is treated as unmapped and silently skipped, so the member retains its default value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseSensitiveAndKeyCasingDiffers_ShouldNotBindMember()
    {
        var options = new BencodeSerializerOptions { PropertyNameCaseInsensitive = false };
        byte[] bytes = Encoding.Latin1.GetBytes("d5:VALUEi7ee");

        var model = BencodeSerializer.Deserialize<CaseInsensitiveModel>(bytes, options);

        Assert.AreEqual(0, model.Value);
    }

    /// <summary>
    /// Verifies that, when case-insensitive matching is disabled, a dictionary key matching the member's CLR name
    /// exactly is bound, confirming the setting narrows rather than disables matching.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseSensitiveAndKeyMatchesExactly_ShouldBindMember()
    {
        var options = new BencodeSerializerOptions { PropertyNameCaseInsensitive = false };
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        var model = BencodeSerializer.Deserialize<CaseInsensitiveModel>(bytes, options);

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that <see cref="BencodePropertyNameAttribute" /> on a member overrides an active naming policy on both
    /// write and read, so the member is keyed by its explicit name while a sibling without the attribute follows the
    /// policy.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenExplicitNameAndNamingPolicy_ShouldPreferExplicitName()
    {
        var options = new BencodeSerializerOptions { PropertyNamingPolicy = BencodeNamingPolicy.CamelCase };
        var original = new ExplicitNameModel { FirstName = "a", LastName = "b" };

        byte[] bytes = BencodeSerializer.Serialize(original, options);
        // FirstName follows the camel-case policy; LastName keeps its explicit wire name "surname".
        Assert.AreEqual("d9:firstName1:a7:surname1:be", Encoding.Latin1.GetString(bytes));

        var roundTripped = BencodeSerializer.Deserialize<ExplicitNameModel>(bytes, options);
        Assert.AreEqual("a", roundTripped.FirstName);
        Assert.AreEqual("b", roundTripped.LastName);
    }

    /// <summary>
    /// Verifies that a dictionary key matching the member's CLR name rather than its overriding
    /// <see cref="BencodePropertyNameAttribute" /> wire name is not bound, since reads match on the wire name only.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyMatchesClrNameNotWireName_ShouldNotBindMember()
    {
        // The wire name is "id"; a key of "Identifier" (the CLR name) must not bind.
        byte[] bytes = Encoding.Latin1.GetBytes("d10:Identifieri5ee");

        var model = BencodeSerializer.Deserialize<RenamedKeyModel>(bytes);

        Assert.AreEqual(0, model.Identifier);
    }

    /// <summary>
    /// Verifies that a dictionary key matching the overriding <see cref="BencodePropertyNameAttribute" /> wire name is
    /// bound on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyMatchesWireName_ShouldBindMember()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d2:idi5ee");

        var model = BencodeSerializer.Deserialize<RenamedKeyModel>(bytes);

        Assert.AreEqual(5, model.Identifier);
    }

    /// <summary>
    /// Verifies that constructing <see cref="BencodePropertyNameAttribute" /> with a null name throws
    /// <see cref="ArgumentNullException" />, naming the <c>name</c> parameter.
    /// </summary>
    [TestMethod]
    public void BencodePropertyNameAttribute_WhenNameNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = new BencodePropertyNameAttribute(null!);
        }, "name");
    }

    /// <summary>
    /// Verifies that serializing a type whose members resolve to the same wire name fails fast with
    /// <see cref="InvalidOperationException" /> when the type's metadata is built, naming the colliding members and
    /// the key, mirroring <see cref="System.Text.Json.JsonSerializer" />'s metadata-time collision check.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMembersCollideOnWireName_ShouldThrowInvalidOperationException()
    {
        var model = new CollidingWireNameModel { First = 1, Second = 2 };

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = BencodeSerializer.Serialize(model);
        });

        Assert.IsTrue(ex.Message.Contains(nameof(CollidingWireNameModel.First), StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains(nameof(CollidingWireNameModel.Second), StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("'id'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that deserializing into a type whose members resolve to the same wire name throws the same
    /// metadata-time <see cref="InvalidOperationException" /> as serialization, because the collision is a property
    /// of the type's mapping rather than of either direction.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMembersCollideOnWireName_ShouldThrowInvalidOperationException()
    {
        byte[] data = "d2:idi1ee"u8.ToArray();

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<CollidingWireNameModel>(data);
        });
    }

    /// <summary>
    /// Verifies that members whose wire names differ only by case do not collide, even under the default
    /// case-insensitive read matching: the keys are distinct raw bytes on the wire, so the dictionary remains
    /// canonical and both keys are emitted.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenWireNamesDifferOnlyByCase_ShouldEmitBothKeys()
    {
        var model = new CaseOnlyWireNameModel { Lower = 1, Upper = 2 };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d2:IDi2e2:idi1ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// A model with a single integer member used to exercise case-sensitive and case-insensitive key matching.
    /// </summary>
    private sealed class CaseInsensitiveModel
    {
        /// <summary>
        /// Gets or sets the integer value.
        /// </summary>
        /// <returns>The value.</returns>
        public int Value { get; set; }
    }

    /// <summary>
    /// A model whose two members are named by a camel-case policy except for one that overrides it with an explicit
    /// wire name.
    /// </summary>
    private sealed class ExplicitNameModel
    {
        /// <summary>
        /// Gets or sets the first name, named by the active naming policy.
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

    /// <summary>
    /// A model whose two members carry the same explicit wire name, producing a duplicate-key collision on write.
    /// </summary>
    private sealed class CollidingWireNameModel
    {
        /// <summary>
        /// Gets or sets the first value, keyed under the wire name <c>id</c>.
        /// </summary>
        /// <returns>The first value.</returns>
        [BencodePropertyName("id")]
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the second value, also keyed under the wire name <c>id</c>.
        /// </summary>
        /// <returns>The second value.</returns>
        [BencodePropertyName("id")]
        public int Second { get; set; }
    }

    /// <summary>
    /// A model whose two members carry wire names differing only by case, which are distinct keys on the wire.
    /// </summary>
    private sealed class CaseOnlyWireNameModel
    {
        /// <summary>
        /// Gets or sets the first value, keyed under the lower-case wire name <c>id</c>.
        /// </summary>
        /// <returns>The first value.</returns>
        [BencodePropertyName("id")]
        public int Lower { get; set; }

        /// <summary>
        /// Gets or sets the second value, keyed under the upper-case wire name <c>ID</c>.
        /// </summary>
        /// <returns>The second value.</returns>
        [BencodePropertyName("ID")]
        public int Upper { get; set; }
    }

    /// <summary>
    /// A model whose member carries an explicit wire name that differs from its CLR name.
    /// </summary>
    private sealed class RenamedKeyModel
    {
        /// <summary>
        /// Gets or sets the identifier, keyed under the wire name <c>id</c>.
        /// </summary>
        /// <returns>The identifier.</returns>
        [BencodePropertyName("id")]
        public int Identifier { get; set; }
    }
}
