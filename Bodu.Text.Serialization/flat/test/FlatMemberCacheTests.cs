// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Provides tests for the shared <see cref="FlatMemberCache" />, compiled into the test project of each line format that
/// compiles it.
/// </summary>
[TestClass]
public sealed partial class FlatMemberCacheTests
{
    /// <summary>
    /// A record of two plain properties.
    /// </summary>
    private sealed class PlainSample
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        public int Count { get; set; }
    }

    /// <summary>
    /// A record whose two properties are mapped to one name.
    /// </summary>
    private sealed class DuplicateSample
    {
        /// <summary>
        /// Gets or sets the first property mapped to <c>X</c>.
        /// </summary>
        [PropertyName("X")]
        public int First { get; set; }

        /// <summary>
        /// Gets or sets the second property mapped to <c>X</c>.
        /// </summary>
        [PropertyName("X")]
        public int Second { get; set; }
    }

    /// <summary>
    /// Creates the exception the tests expect for a type that maps two members to one name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="name">The name mapped twice.</param>
    /// <returns>The exception.</returns>
    private static InvalidOperationException CreateDuplicateNameException(Type type, string name) =>
        new($"{type.Name}:{name}");
}
