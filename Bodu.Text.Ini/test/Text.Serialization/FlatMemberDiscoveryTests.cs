// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDiscoveryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatMemberDiscovery" /> as the INI serializer compiles it.
/// </summary>
[TestClass]
public sealed partial class FlatMemberDiscoveryTests
{
    /// <summary>
    /// A record with a plain property, an ignored property, a public field and an indexer.
    /// </summary>
    private sealed class MemberSample
    {
        /// <summary>A public field, discovered only when fields are included.</summary>
        public int PublicField = 42;

        /// <summary>
        /// Gets or sets a plain property.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a property that discovery skips.
        /// </summary>
        [Ignore]
        public int Hidden { get; set; }

        /// <summary>
        /// Gets the text of an index; an indexer is never a member.
        /// </summary>
        /// <param name="index">The index to write.</param>
        /// <returns>The index as invariant text.</returns>
        public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
