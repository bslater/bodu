// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Required.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies required-member enforcement during deserialization: a member marked with the <see langword="required" />
/// keyword or with <see cref="RequiredAttribute" /> must appear in the input, otherwise binding throws
/// <see cref="YamlSerializationException" />.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that omitting a <see langword="required" /> member from the input throws
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeywordMemberMissing_ShouldThrowYamlSerializationException()
    {
        var ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<RequiredKeywordModel>("Other: 1\n");
        });

        Assert.IsTrue(ex.Message.Contains("Name", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that supplying a <see langword="required" /> member binds it and does not throw.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeywordMemberPresent_ShouldBind()
    {
        RequiredKeywordModel model = YamlSerializer.Deserialize<RequiredKeywordModel>("Name: Ada\nOther: 3\n")!;

        Assert.AreEqual("Ada", model.Name);
        Assert.AreEqual(3, model.Other);
    }

    /// <summary>
    /// Verifies that omitting a member marked with <see cref="RequiredAttribute" /> throws
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberMissing_ShouldThrowYamlSerializationException()
    {
        var ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<RequiredAttributeModel>("{}\n");
        });

        Assert.IsTrue(ex.Message.Contains("Token", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that supplying a member marked with <see cref="RequiredAttribute" /> binds it and does not throw.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberPresent_ShouldBind()
    {
        RequiredAttributeModel model = YamlSerializer.Deserialize<RequiredAttributeModel>("Token: abc\n")!;

        Assert.AreEqual("abc", model.Token);
    }

    /// <summary>
    /// A model with a member declared using the <see langword="required" /> keyword.
    /// </summary>
    private sealed class RequiredKeywordModel
    {
        /// <summary>
        /// Gets or sets the required name.
        /// </summary>
        /// <value>The name.</value>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets an optional additional value.
        /// </summary>
        /// <value>The additional value.</value>
        public int Other { get; set; }
    }

    /// <summary>
    /// A model with a member marked required through <see cref="RequiredAttribute" />.
    /// </summary>
    private sealed class RequiredAttributeModel
    {
        /// <summary>
        /// Gets or sets the required token.
        /// </summary>
        /// <value>The token.</value>
        [Required]
        public string? Token { get; set; }
    }
}
