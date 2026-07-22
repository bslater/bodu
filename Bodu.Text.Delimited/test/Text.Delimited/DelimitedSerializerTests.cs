// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Hosts the shared model types for the <see cref="DelimitedSerializer" /> test backbone.
/// </summary>
[TestClass]
public partial class DelimitedSerializerTests
{
    /// <summary>
    /// A simple record POCO with a string and a typed column.
    /// </summary>
    public sealed class Person
    {
        /// <summary>Gets or sets the person's name.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the person's age.</summary>
        /// <value>The age.</value>
        public int Age { get; set; }
    }
}
