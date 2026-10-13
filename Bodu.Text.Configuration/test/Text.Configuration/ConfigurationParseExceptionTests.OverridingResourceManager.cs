// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseExceptionTests.OverridingResourceManager.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;

namespace Bodu.Text.Configuration;

public partial class ConfigurationParseExceptionTests
{
    /// <summary>
    /// A resource manager that answers one resource name with a fixed value and defers every other lookup to the manager
    /// it wraps, so a test can tell a message read from the resources from a string literal.
    /// </summary>
    private sealed class OverridingResourceManager
        : ResourceManager
    {
        /// <summary>The resource manager every other lookup is deferred to.</summary>
        private readonly ResourceManager _inner;

        /// <summary>The name of the resource entry answered with <see cref="_value" />.</summary>
        private readonly string _name;

        /// <summary>The value answered for <see cref="_name" />.</summary>
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="OverridingResourceManager" /> class.
        /// </summary>
        /// <param name="inner">The resource manager every other lookup is deferred to.</param>
        /// <param name="name">The name of the resource entry to override.</param>
        /// <param name="value">The value answered for <paramref name="name" />.</param>
        public OverridingResourceManager(ResourceManager inner, string name, string value)
        {
            _inner = inner;
            _name = name;
            _value = value;
        }

        /// <inheritdoc />
        public override string? GetString(string name, CultureInfo? culture) =>
            string.Equals(name, _name, StringComparison.Ordinal) ? _value : _inner.GetString(name, culture);
    }
}
