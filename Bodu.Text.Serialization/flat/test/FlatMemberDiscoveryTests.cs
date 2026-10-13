// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDiscoveryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatMemberDiscovery" />, compiled into the test project of each line format
/// that compiles it.
/// </summary>
[TestClass]
public sealed partial class FlatMemberDiscoveryTests
{
    /// <summary>
    /// A record with a renamed required property, an ignored property, a public field and an indexer.
    /// </summary>
    private sealed class MemberSample
    {
        /// <summary>
        /// Gets or sets a required property written under another name.
        /// </summary>
        [Required]
        [PropertyName("CUSTOM_KEY")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets a property that discovery skips.
        /// </summary>
        [Ignore]
        public string? NotIncluded { get; set; }

        /// <summary>A public field, discovered only when fields are included.</summary>
        public int PublicField = 42;

        /// <summary>
        /// Gets the text of an index; an indexer is never a member.
        /// </summary>
        /// <param name="index">The index to write.</param>
        /// <returns>The index as invariant text.</returns>
        public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A base record whose property and field <see cref="HidingSample" /> hides.
    /// </summary>
    private class HiddenSample
    {
        /// <summary>A field that a derived type hides.</summary>
        public int Code = 1;

        /// <summary>
        /// Gets or sets a property that a derived type hides.
        /// </summary>
        public string? Port { get; set; }
    }

    /// <summary>
    /// A record whose property and field hide the base type's with the <see langword="new" /> modifier.
    /// </summary>
    private sealed class HidingSample
        : HiddenSample
    {
        /// <summary>A field hiding the base type's.</summary>
        public new string? Code = "c";

        /// <summary>
        /// Gets or sets a property hiding the base type's.
        /// </summary>
        public new int Port { get; set; }
    }
}
