// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.IgnoreCondition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the per-member ignore conditions expressed through <see cref="IgnoreAttribute" />: an unconditionally
/// ignored member never round-trips, its getter is never called, and its overrides inherit the ignore, a
/// <see cref="IgnoreCondition.WhenWritingNull" /> member is omitted only when null, and a
/// <see cref="IgnoreCondition.WhenWritingDefault" /> member is omitted only when it holds its type default.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a member marked <c>[Ignore]</c> (unconditionally) is never written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberUnconditionallyIgnored_ShouldOmit()
    {
        string yaml = YamlSerializer.Serialize(new IgnoreModel { Kept = 1, Secret = "x" });

        Assert.AreEqual("Kept: 1\n", yaml);
    }

    /// <summary>
    /// Verifies that a <see cref="IgnoreCondition.WhenWritingNull" /> member is omitted when its value is null.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenWhenWritingNullMemberIsNull_ShouldOmit()
    {
        string yaml = YamlSerializer.Serialize(new ConditionalModel { Kept = 1, Comment = null });

        Assert.AreEqual("Kept: 1\n", yaml);
    }

    /// <summary>
    /// Verifies that a <see cref="IgnoreCondition.WhenWritingNull" /> member is written when its value is non-null.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenWhenWritingNullMemberIsPresent_ShouldWrite()
    {
        string yaml = YamlSerializer.Serialize(new ConditionalModel { Kept = 1, Comment = "hi" });

        Assert.AreEqual("Kept: 1\nComment: hi\n", yaml);
    }

    /// <summary>
    /// Verifies that a <see cref="IgnoreCondition.WhenWritingDefault" /> member is omitted when it holds the type
    /// default and written otherwise.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenWhenWritingDefaultMember_ShouldOmitDefaultAndWriteNonDefault()
    {
        Assert.AreEqual("Kept: 1\n", YamlSerializer.Serialize(new DefaultOmitModel { Kept = 1, Count = 0 }));
        Assert.AreEqual("Kept: 1\nCount: 7\n", YamlSerializer.Serialize(new DefaultOmitModel { Kept = 1, Count = 7 }));
    }

    /// <summary>
    /// Verifies that a member marked <see cref="IgnoreCondition.WhenWritingDefault" /> is omitted when it holds its
    /// default, <c>0</c>, and written when it holds <c>1</c>, while the options' <see cref="IgnoreCondition.Never" />
    /// still writes another member that holds its default.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenMemberIgnoreConditionIsWhenWritingDefault_ShouldOmitDefault()
    {
        var options = new YamlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.Never };

        string atDefault = YamlSerializer.Serialize(new DefaultOmitModel { Kept = 0, Count = 0 }, options);
        string set = YamlSerializer.Serialize(new DefaultOmitModel { Kept = 0, Count = 1 }, options);

        Assert.AreEqual("Kept: 0\n", atDefault);
        Assert.AreEqual("Kept: 0\nCount: 1\n", set);
    }

    /// <summary>
    /// Verifies that a property overriding a base property marked <c>[Ignore]</c> inherits the ignore, so serializing
    /// the derived type writes no <c>Secret</c> key and leaves the mapping empty.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOverridingPropertyOfIgnoredBaseProperty_ShouldOmitIt()
    {
        string yaml = YamlSerializer.Serialize(new OverridingSecretModel { Secret = "x" });

        Assert.AreEqual("{}\n", yaml);
    }

    /// <summary>
    /// Verifies that serializing a model never reads a property marked <c>[Ignore]</c>: its getter throws, yet the
    /// model serializes, writing only its other member.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenIgnoredPropertyGetterThrows_ShouldNotCallGetter()
    {
        string yaml = YamlSerializer.Serialize(new ThrowingIgnoredModel { Ok = 1 });

        Assert.AreEqual("Ok: 1\n", yaml);
    }

    /// <summary>A model with an unconditionally ignored member.</summary>
    private sealed class IgnoreModel
    {
        /// <summary>Gets or sets the retained value.</summary>
        /// <value>The retained value.</value>
        public int Kept { get; set; }

        /// <summary>Gets or sets the ignored value.</summary>
        /// <value>The ignored value.</value>
        [Bodu.Text.Serialization.Ignore]
        public string? Secret { get; set; }
    }

    /// <summary>A model with a member ignored only when it is null on write.</summary>
    private sealed class ConditionalModel
    {
        /// <summary>Gets or sets the retained value.</summary>
        /// <value>The retained value.</value>
        public int Kept { get; set; }

        /// <summary>Gets or sets the value omitted when null.</summary>
        /// <value>The comment.</value>
        [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingNull)]
        public string? Comment { get; set; }
    }

    /// <summary>A model with a member ignored only when it holds its type default on write.</summary>
    private sealed class DefaultOmitModel
    {
        /// <summary>Gets or sets the retained value.</summary>
        /// <value>The retained value.</value>
        public int Kept { get; set; }

        /// <summary>Gets or sets the value omitted when it equals zero.</summary>
        /// <value>The count.</value>
        [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingDefault)]
        public int Count { get; set; }
    }

    /// <summary>A base model whose virtual property is ignored.</summary>
    private class IgnoredSecretBase
    {
        /// <summary>Gets or sets the ignored value.</summary>
        /// <value>The ignored value.</value>
        [Bodu.Text.Serialization.Ignore]
        public virtual string? Secret { get; set; }
    }

    /// <summary>A model that overrides an ignored base property without repeating the attribute.</summary>
    private sealed class OverridingSecretModel
        : IgnoredSecretBase
    {
        /// <summary>Gets or sets the value the base declaration ignores.</summary>
        /// <value>The value.</value>
        public override string? Secret { get; set; }
    }

    /// <summary>A model with an ignored property whose getter throws.</summary>
    private sealed class ThrowingIgnoredModel
    {
        /// <summary>Gets a value that the serializer must never read.</summary>
        /// <value>Never returned; the getter always throws.</value>
        /// <exception cref="InvalidOperationException">Always.</exception>
        [Bodu.Text.Serialization.Ignore]
        public string Bad => throw new InvalidOperationException("The serializer read an ignored property.");

        /// <summary>Gets or sets the written value.</summary>
        /// <value>The written value.</value>
        public int Ok { get; set; }
    }
}
