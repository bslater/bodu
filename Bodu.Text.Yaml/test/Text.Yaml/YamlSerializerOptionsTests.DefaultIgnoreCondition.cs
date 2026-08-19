// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.DefaultIgnoreCondition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializerOptions.DefaultIgnoreCondition" />: the write-path effects of each accepted
/// condition, the member-level override, and the rejection of invalid values.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that the default condition <see cref="IgnoreCondition.Never" /> writes a null member as the YAML null
    /// scalar.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenNever_ShouldWriteNullMember()
    {
        string text = YamlSerializer.Serialize(new Note { Text = null, Count = 0 });

        Assert.AreEqual("Text: null\nCount: 0\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="IgnoreCondition.WhenWritingNull" /> omits null members while keeping default-valued
    /// non-null members.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenWritingNull_ShouldOmitNullMembersOnly()
    {
        var options = new YamlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull };

        string text = YamlSerializer.Serialize(new Note { Text = null, Count = 0 }, options);

        Assert.AreEqual("Count: 0\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="IgnoreCondition.WhenWritingDefault" /> omits both null members and members holding
    /// their type default.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenWritingDefault_ShouldOmitNullAndDefaultMembers()
    {
        var options = new YamlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingDefault };

        string text = YamlSerializer.Serialize(new Note { Text = "kept", Count = 0 }, options);

        Assert.AreEqual("Text: kept\n", text);
    }

    /// <summary>
    /// Verifies that a member-level ignore condition overrides the serializer-wide default, so a member marked
    /// <see cref="IgnoreCondition.Never" /> still writes its null value under a null-omitting default.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenMemberOverridesNever_ShouldWriteNullMember()
    {
        var options = new YamlSerializerOptions { DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull };

        string text = YamlSerializer.Serialize(new PinnedNote { Text = null }, options);

        Assert.AreEqual("Text: null\n", text);
    }

    /// <summary>
    /// Verifies that setting <see cref="IgnoreCondition.Always" /> as the serializer-wide default throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenSetToAlways_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new YamlSerializerOptions();

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            options.DefaultIgnoreCondition = IgnoreCondition.Always;
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that setting an undefined <see cref="IgnoreCondition" /> value throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenSetToUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new YamlSerializerOptions();

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            options.DefaultIgnoreCondition = (IgnoreCondition)99;
        });
    }

    /// <summary>
    /// Verifies that setting the condition after the options have been used to serialize throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void DefaultIgnoreCondition_WhenOptionsReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new YamlSerializerOptions();
        _ = YamlSerializer.Serialize(new Point { X = 1 }, options);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            options.DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull;
        });
    }

    /// <summary>
    /// A target type with a nullable reference member and a value-typed member, used to distinguish null-omission from
    /// default-omission.
    /// </summary>
    private sealed class Note
    {
        /// <summary>Gets or sets the note text.</summary>
        /// <value>The text, or <see langword="null" /> when absent.</value>
        public string? Text { get; set; }

        /// <summary>Gets or sets the note count.</summary>
        /// <value>The count.</value>
        public int Count { get; set; }
    }

    /// <summary>
    /// A target type whose member pins <see cref="IgnoreCondition.Never" /> against the serializer-wide default.
    /// </summary>
    private sealed class PinnedNote
    {
        /// <summary>Gets or sets the note text, always written even when null.</summary>
        /// <value>The text, or <see langword="null" /> when absent.</value>
        [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.Never)]
        public string? Text { get; set; }
    }
}
