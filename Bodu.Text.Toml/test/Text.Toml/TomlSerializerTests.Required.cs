// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Required.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies how <see cref="TomlSerializer" /> enforces required members on read: the C# <see langword="required" />
/// keyword, the <see cref="TomlRequiredAttribute" />, and the implicit requirement created by a constructor parameter
/// without a default. A missing required member fails deserialization; a present one round-trips.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that a member declared with the <see langword="required" /> keyword round-trips when its key is
    /// present in the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeywordMemberPresent_ShouldRoundTrip()
    {
        RequiredKeywordModel model = TomlSerializer.Deserialize<RequiredKeywordModel>("Name = \"Alice\"\n");

        Assert.AreEqual("Alice", model.Name);
    }

    /// <summary>
    /// Verifies that a member declared with the <see langword="required" /> keyword causes deserialization to throw
    /// <see cref="TomlSerializationException" /> when its key is absent from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeywordMemberMissing_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RequiredKeywordModel>(string.Empty);
        });

        Assert.IsTrue(ex.Message.Contains("Name", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="TomlRequiredAttribute" /> round-trips when its key is present
    /// in the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberPresent_ShouldRoundTrip()
    {
        RequiredAttributeModel model = TomlSerializer.Deserialize<RequiredAttributeModel>("id = 42\n");

        Assert.AreEqual(42, model.Id);
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="TomlRequiredAttribute" /> causes deserialization to throw
    /// <see cref="TomlSerializationException" /> when its key is absent from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberMissing_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RequiredAttributeModel>(string.Empty);
        });

        Assert.IsTrue(ex.Message.Contains("id", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the message of the exception thrown for a missing required member reports the wire name of the
    /// member rather than its CLR name.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberMissing_ShouldReportWireNameInMessage()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RequiredAttributeModel>(string.Empty);
        });

        Assert.IsTrue(ex.Message.Contains("id", StringComparison.Ordinal));
        Assert.IsFalse(ex.Message.Contains("Id'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a constructor parameter without a default value is treated as required, so its absence from the
    /// input throws <see cref="TomlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterWithoutDefaultMissing_ShouldThrowTomlSerializationException()
    {
        TomlSerializationException ex = Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<RequiredConstructorParameterModel>(string.Empty);
        });

        Assert.IsTrue(ex.Message.Contains("Value", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a constructor parameter without a default value round-trips when its key is present, confirming
    /// the requirement is satisfied by the supplied member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterWithoutDefaultPresent_ShouldRoundTrip()
    {
        RequiredConstructorParameterModel model = TomlSerializer.Deserialize<RequiredConstructorParameterModel>("Value = 7\n");

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that a constructor parameter that declares a default value is not treated as required, so its absence
    /// from the input does not throw and the parameter default is used.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterWithDefaultMissing_ShouldNotThrow()
    {
        DefaultedConstructorParameterModel model = TomlSerializer.Deserialize<DefaultedConstructorParameterModel>(string.Empty);

        Assert.AreEqual(5, model.Value);
    }

    /// <summary>
    /// Verifies that a type with several required members fails deserialization when only one of them is supplied,
    /// confirming each required member is enforced individually.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOneOfMultipleRequiredMembersMissing_ShouldThrowTomlSerializationException()
    {
        Assert.ThrowsExactly<TomlSerializationException>(() =>
        {
            _ = TomlSerializer.Deserialize<TwoRequiredMembersModel>("First = \"a\"\n");
        });
    }

    /// <summary>
    /// Verifies that a type with several required members round-trips when every required key is present.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAllRequiredMembersPresent_ShouldRoundTrip()
    {
        TwoRequiredMembersModel model = TomlSerializer.Deserialize<TwoRequiredMembersModel>("First = \"a\"\nSecond = \"b\"\n");

        Assert.AreEqual("a", model.First);
        Assert.AreEqual("b", model.Second);
    }

    /// <summary>
    /// A type whose member is declared with the C# <see langword="required" /> keyword.
    /// </summary>
    private sealed class RequiredKeywordModel
    {
        /// <summary>
        /// Gets or sets the name, which must be present when reading.
        /// </summary>
        /// <value>The name.</value>
        public required string Name { get; set; }
    }

    /// <summary>
    /// A type whose member is marked required through <see cref="TomlRequiredAttribute" /> and renamed on the wire.
    /// </summary>
    private sealed class RequiredAttributeModel
    {
        /// <summary>
        /// Gets or sets the identifier, required and written under the wire name <c>id</c>.
        /// </summary>
        /// <value>The identifier.</value>
        [TomlRequired]
        [TomlPropertyName("id")]
        public int Id { get; set; }
    }

    /// <summary>
    /// A type whose single read-only member binds a constructor parameter that has no default, making the member
    /// implicitly required.
    /// </summary>
    private sealed class RequiredConstructorParameterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredConstructorParameterModel" /> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public RequiredConstructorParameterModel(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; }
    }

    /// <summary>
    /// A type whose constructor parameter declares a default, so the bound member is not required.
    /// </summary>
    private sealed class DefaultedConstructorParameterModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultedConstructorParameterModel" /> class.
        /// </summary>
        /// <param name="value">The value, defaulting to <c>5</c>.</param>
        public DefaultedConstructorParameterModel(int value = 5)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; }
    }

    /// <summary>
    /// A type with two members that are both required.
    /// </summary>
    private sealed class TwoRequiredMembersModel
    {
        /// <summary>
        /// Gets or sets the first required member.
        /// </summary>
        /// <value>The first value.</value>
        public required string First { get; set; }

        /// <summary>
        /// Gets or sets the second required member.
        /// </summary>
        /// <value>The second value.</value>
        public required string Second { get; set; }
    }
}
