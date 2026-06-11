// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.PropertyName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies how the <see cref="TomlSerializer" /> resolves table keys to members on read: the
/// <see cref="TomlSerializerOptions.PropertyNameCaseInsensitive" /> matching behavior, the precedence of
/// <see cref="TomlPropertyNameAttribute" /> over any naming policy, and the guard on a null attribute name.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that, under the default case-insensitive matching, a table key whose casing differs from the member's
    /// CLR name is still bound to that member on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseInsensitiveDefaultAndKeyCasingDiffers_ShouldBindMember()
    {
        var model = TomlSerializer.Deserialize<CaseInsensitiveModel>("VALUE = 7\n");

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that, when case-insensitive matching is disabled, a table key whose casing differs from the member's
    /// CLR name is treated as unmapped and silently skipped, so the member retains its default value.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseSensitiveAndKeyCasingDiffers_ShouldNotBindMember()
    {
        var options = new TomlSerializerOptions { PropertyNameCaseInsensitive = false };

        var model = TomlSerializer.Deserialize<CaseInsensitiveModel>("VALUE = 7\n", options);

        Assert.AreEqual(0, model.Value);
    }

    /// <summary>
    /// Verifies that, when case-insensitive matching is disabled, a table key matching the member's CLR name exactly is
    /// bound, confirming the setting narrows rather than disables matching.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCaseSensitiveAndKeyMatchesExactly_ShouldBindMember()
    {
        var options = new TomlSerializerOptions { PropertyNameCaseInsensitive = false };

        var model = TomlSerializer.Deserialize<CaseInsensitiveModel>("Value = 7\n", options);

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that <see cref="TomlPropertyNameAttribute" /> on a member overrides an active naming policy on both
    /// write and read, so the member is keyed by its explicit name while a sibling without the attribute follows the
    /// policy.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenExplicitNameAndNamingPolicy_ShouldPreferExplicitName()
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.CamelCase };
        var original = new ExplicitNameModel { FirstName = "a", LastName = "b" };

        string text = TomlSerializer.Serialize(original, options);

        // FirstName follows the camel-case policy; LastName keeps its explicit wire name "surname".
        Assert.AreEqual("firstName = \"a\"\nsurname = \"b\"\n", text);

        var roundTripped = TomlSerializer.Deserialize<ExplicitNameModel>(text, options);
        Assert.AreEqual("a", roundTripped.FirstName);
        Assert.AreEqual("b", roundTripped.LastName);
    }

    /// <summary>
    /// Verifies that a table key matching the member's CLR name rather than its overriding
    /// <see cref="TomlPropertyNameAttribute" /> wire name is not bound, since reads match on the wire name only.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyMatchesClrNameNotWireName_ShouldNotBindMember()
    {
        // The wire name is "id"; a key of "Identifier" (the CLR name) must not bind.
        var model = TomlSerializer.Deserialize<RenamedKeyModel>("Identifier = 5\n");

        Assert.AreEqual(0, model.Identifier);
    }

    /// <summary>
    /// Verifies that a table key matching the overriding <see cref="TomlPropertyNameAttribute" /> wire name is bound on
    /// read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyMatchesWireName_ShouldBindMember()
    {
        var model = TomlSerializer.Deserialize<RenamedKeyModel>("id = 5\n");

        Assert.AreEqual(5, model.Identifier);
    }

    /// <summary>
    /// Verifies that the same key supplied twice under case-insensitive matching binds the same member twice and is
    /// rejected with <see cref="TomlSerializationException" />, since a member may be assigned only once.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyBindsSameMemberTwice_ShouldThrowTomlSerializationException()
    {
        var ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<CaseInsensitiveModel>("Value = 1\nvalue = 2\n");
        });

        Assert.IsTrue(ex.Message.Contains("value", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that constructing <see cref="TomlPropertyNameAttribute" /> with a null name throws
    /// <see cref="ArgumentNullException" />, naming the <c>name</c> parameter.
    /// </summary>
    [TestMethod]
    public void TomlPropertyNameAttribute_WhenNameNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TomlPropertyNameAttribute(null!);
        });

        Assert.AreEqual("name", ex.ParamName);
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
        [TomlPropertyName("surname")]
        public string LastName { get; set; } = string.Empty;
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
        [TomlPropertyName("id")]
        public int Identifier { get; set; }
    }
}
