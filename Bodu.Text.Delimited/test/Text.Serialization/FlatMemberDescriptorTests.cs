// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatMemberDescriptor" /> as the delimited serializer compiles it.
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
    /// A record with an ordered property and a read-only field.
    /// </summary>
    private sealed class MemberSample
    {
        /// <summary>A read-only field, which can be read but not written.</summary>
        public readonly int ImmutableField = 12;

        /// <summary>
        /// Gets or sets a property with an explicit serialization order.
        /// </summary>
        [PropertyOrder(2)]
        public int Number { get; set; }
    }
}
