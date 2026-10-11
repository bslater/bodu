// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatMemberDescriptor" /> as the DotEnv serializer compiles it.
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
    /// A record with a renamed required property.
    /// </summary>
    private sealed class MemberSample
    {
        /// <summary>
        /// Gets or sets a required property written under another name.
        /// </summary>
        [Required]
        [PropertyName("CUSTOM_KEY")]
        public int Count { get; set; }
    }
}
