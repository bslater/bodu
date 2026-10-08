// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the <see cref="YamlSerializer" /> POCO mapper. Test methods live in the member- and subject-specific
/// partial files (serialize, deserialize, round-trip, coercion, dictionary, diagnostics); this root holds the
/// shared POCO fixtures.
/// </summary>
[TestClass]
public partial class YamlSerializerTests
{
    /// <summary>A simple POCO used across the serializer tests.</summary>
    private sealed class Person
    {
        public string? Name { get; set; }

        public int Age { get; set; }

        public bool Active { get; set; }
    }

    /// <summary>A POCO exercising naming policy and the property-name and ignore attributes.</summary>
    private sealed class Config
    {
        public string? ServerHost { get; set; }

        [PropertyName("port")]
        public int ServerPort { get; set; }

        [Bodu.Text.Serialization.Ignore]
        public string? Secret { get; set; }
    }

    /// <summary>A POCO with nested collections.</summary>
    private sealed class Project
    {
        public string? Title { get; set; }

        public List<string>? Tags { get; set; }

        public Dictionary<string, int>? Counts { get; set; }
    }

    /// <summary>An enumeration used to test enum serialization.</summary>
    private enum Color
    {
        Red,
        Green,
        Blue,
    }

    /// <summary>
    /// A base type whose public field <see cref="HiddenFieldDerivedModel" /> hides with a field of another type.
    /// </summary>
    private class HiddenFieldBaseModel
    {
        /// <summary>
        /// The base field, which the derived type's field of the same name hides.
        /// </summary>
        public int Value = 1;
    }

    /// <summary>
    /// A type that hides its base type's public field with a field of another type.
    /// </summary>
    private sealed class HiddenFieldDerivedModel
        : HiddenFieldBaseModel
    {
        /// <summary>
        /// The derived field, which hides the base field of the same name.
        /// </summary>
        public new string Value = "derived";
    }

    /// <summary>
    /// A base type whose property <see cref="HiddenPropertyDerivedModel" /> hides with a property of another type.
    /// </summary>
    private class HiddenPropertyBaseModel
    {
        /// <summary>
        /// Gets or sets the base property, which the derived type's property of the same name hides.
        /// </summary>
        /// <value>The base value.</value>
        public int Value { get; set; } = 1;
    }

    /// <summary>
    /// A type that hides its base type's property with a property of another type.
    /// </summary>
    private sealed class HiddenPropertyDerivedModel
        : HiddenPropertyBaseModel
    {
        /// <summary>
        /// Gets or sets the derived property, which hides the base property of the same name.
        /// </summary>
        /// <value>The derived value.</value>
        public new string Value { get; set; } = "derived";
    }

    /// <summary>
    /// The base of two types that hide its property in turn, each with a property of another type.
    /// </summary>
    private class HiddenTwiceBaseModel
    {
        /// <summary>
        /// Gets or sets the base property, which both derived types hide.
        /// </summary>
        /// <value>The base value.</value>
        public int Value { get; set; } = 1;
    }

    /// <summary>
    /// The middle type, which hides its base type's property and has its own property hidden in turn.
    /// </summary>
    private class HiddenTwiceMiddleModel
        : HiddenTwiceBaseModel
    {
        /// <summary>
        /// Gets or sets the middle property, which hides the base property and is hidden by the derived one.
        /// </summary>
        /// <value>The middle value.</value>
        public new long Value { get; set; } = 2;
    }

    /// <summary>
    /// The most derived type, whose property hides those of both its base types.
    /// </summary>
    private sealed class HiddenTwiceDerivedModel
        : HiddenTwiceMiddleModel
    {
        /// <summary>
        /// Gets or sets the most derived property, which hides the properties of the same name of both base types.
        /// </summary>
        /// <value>The derived value.</value>
        public new string Value { get; set; } = "derived";
    }

    /// <summary>
    /// A type with a required member of its own and nested types whose required member a document can leave out.
    /// </summary>
    private sealed class RequiredPathRootModel
    {
        /// <summary>
        /// Gets or sets the required identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the nested value one level down.
        /// </summary>
        /// <value>The nested value, or <see langword="null" /> when the document leaves it out.</value>
        public RequiredPathLeafModel? Inner { get; set; }

        /// <summary>
        /// Gets or sets the nested value that holds another one.
        /// </summary>
        /// <value>The nested value, or <see langword="null" /> when the document leaves it out.</value>
        public RequiredPathMiddleModel? Middle { get; set; }

        /// <summary>
        /// Gets or sets the list of nested values.
        /// </summary>
        /// <value>The nested values, or <see langword="null" /> when the document leaves them out.</value>
        public List<RequiredPathLeafModel>? Items { get; set; }
    }

    /// <summary>
    /// A nested type that holds a further nested value.
    /// </summary>
    private sealed class RequiredPathMiddleModel
    {
        /// <summary>
        /// Gets or sets the nested value.
        /// </summary>
        /// <value>The nested value, or <see langword="null" /> when the document leaves it out.</value>
        public RequiredPathLeafModel? Inner { get; set; }
    }

    /// <summary>
    /// A nested type whose only member is required.
    /// </summary>
    private sealed class RequiredPathLeafModel
    {
        /// <summary>
        /// Gets or sets the required name.
        /// </summary>
        /// <value>The name.</value>
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A generic single-member model that holds a value of the type under test under the key <c>Value</c>.
    /// </summary>
    /// <typeparam name="T">The member type.</typeparam>
    private sealed class ValueModel<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public T? Value { get; set; }
    }
}
