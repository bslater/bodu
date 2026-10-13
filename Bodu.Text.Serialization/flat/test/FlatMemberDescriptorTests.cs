// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatMemberDescriptor" />, compiled into the test project of each line format
/// that compiles it.
/// </summary>
[TestClass]
public sealed partial class FlatMemberDescriptorTests
{
    /// <summary>
    /// A descriptor over a member, with the member's own name kept as its serialized name.
    /// </summary>
    private sealed class DescriptorProbe : FlatMemberDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorProbe" /> class.
        /// </summary>
        /// <param name="member">The property or field to describe.</param>
        public DescriptorProbe(MemberInfo member)
            : base(member, static name => name)
        {
        }
    }

    /// <summary>
    /// A record with a renamed required property, an ordered property and a read-only field.
    /// </summary>
    private sealed class MemberSample
    {
        /// <summary>A read-only field, which can be read but not written.</summary>
        public readonly int ImmutableField = 12;

        /// <summary>
        /// Gets or sets a required property written under another name.
        /// </summary>
        [Required]
        [PropertyName("CUSTOM_KEY")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets a property with an explicit serialization order.
        /// </summary>
        [PropertyOrder(2)]
        public int Number { get; set; }
    }

    /// <summary>
    /// A record whose property is declared with the C# <see langword="required" /> keyword.
    /// </summary>
    private sealed class RequiredKeywordSample
    {
        /// <summary>
        /// Gets or sets a property the keyword makes required.
        /// </summary>
        public required int Count { get; set; }
    }
}
