// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.IgnoreCondition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the per-member ignore conditions expressed through <see cref="IgnoreAttribute" />: an unconditionally
/// ignored member never round-trips, a <see cref="IgnoreCondition.WhenWritingNull" /> member is omitted only when null,
/// and a <see cref="IgnoreCondition.WhenWritingDefault" /> member is omitted only when it holds its type default.
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
}
