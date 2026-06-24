// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Required.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> enforces required members on read: the C# <see langword="required" />
/// keyword, the <see cref="BencodeRequiredAttribute" />, and the implicit requirement created by a constructor parameter
/// without a default. A missing required member fails deserialization; a present one round-trips.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a member declared with the <see langword="required" /> keyword round-trips when its key is present
    /// in the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeywordMemberPresent_ShouldRoundTrip()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name5:Alicee");

        RequiredKeywordModel model = BencodeSerializer.Deserialize<RequiredKeywordModel>(bytes);

        Assert.AreEqual("Alice", model.Name);
    }

    /// <summary>
    /// Verifies that a member declared with the <see langword="required" /> keyword causes deserialization to throw
    /// <see cref="BencodeSerializationException" /> when its key is absent from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredKeywordMemberMissing_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RequiredKeywordModel>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("Name", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="BencodeRequiredAttribute" /> round-trips when its key is present
    /// in the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberPresent_ShouldRoundTrip()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d2:idi42ee");

        RequiredAttributeModel model = BencodeSerializer.Deserialize<RequiredAttributeModel>(bytes);

        Assert.AreEqual(42, model.Id);
    }

    /// <summary>
    /// Verifies that a member annotated with <see cref="BencodeRequiredAttribute" /> causes deserialization to throw
    /// <see cref="BencodeSerializationException" /> when its key is absent from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenRequiredAttributeMemberMissing_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RequiredAttributeModel>(bytes);
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
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RequiredAttributeModel>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("id", StringComparison.Ordinal));
        Assert.IsFalse(ex.Message.Contains("Id'", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a constructor parameter without a default value is treated as required, so its absence from the
    /// input throws <see cref="BencodeSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterWithoutDefaultMissing_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        BencodeSerializationException ex = Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<RequiredConstructorParameterModel>(bytes);
        });

        Assert.IsTrue(ex.Message.Contains("Value", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that a constructor parameter without a default value round-trips when its key is present, confirming the
    /// requirement is satisfied by the supplied member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterWithoutDefaultPresent_ShouldRoundTrip()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        RequiredConstructorParameterModel model = BencodeSerializer.Deserialize<RequiredConstructorParameterModel>(bytes);

        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that a constructor parameter that declares a default value is not treated as required, so its absence
    /// from the input does not throw and the parameter default is used.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenConstructorParameterWithDefaultMissing_ShouldNotThrow()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        DefaultedConstructorParameterModel model = BencodeSerializer.Deserialize<DefaultedConstructorParameterModel>(bytes);

        Assert.AreEqual(5, model.Value);
    }

    /// <summary>
    /// Verifies that a type with several required members reports the first missing member it detects, and that the
    /// failure occurs even when one of the required members is present.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOneOfMultipleRequiredMembersMissing_ShouldThrowBencodeSerializationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:First1:ae");

        Assert.ThrowsExactly<BencodeSerializationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<TwoRequiredMembersModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that a type with several required members round-trips when every required key is present.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenAllRequiredMembersPresent_ShouldRoundTrip()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:First1:a6:Second1:be");

        TwoRequiredMembersModel model = BencodeSerializer.Deserialize<TwoRequiredMembersModel>(bytes);

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
    /// A type whose member is marked required through <see cref="BencodeRequiredAttribute" /> and renamed on the wire.
    /// </summary>
    private sealed class RequiredAttributeModel
    {
        /// <summary>
        /// Gets or sets the identifier, required and written under the wire name <c>id</c>.
        /// </summary>
        /// <value>The identifier.</value>
        [BencodeRequired]
        [BencodePropertyName("id")]
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
