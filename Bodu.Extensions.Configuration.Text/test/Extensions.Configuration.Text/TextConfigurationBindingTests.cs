// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationBindingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies that the bridge produces a configuration view compatible with standard
/// <c>Microsoft.Extensions.Configuration</c> consumer patterns (POCO binding, section enumeration,
/// <c>GetSection</c>).
/// </summary>
[TestClass]
public sealed partial class TextConfigurationBindingTests
{
    private const string PocoSample = """
logging.level.default = Information
logging.level.console = Warning
logging.provider = Console
""";

    /// <summary>
    /// POCO used to validate binding semantics for the logging section.
    /// </summary>
    private sealed class LoggingOptions
    {
        /// <summary>
        /// Gets or sets the provider identifier.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Gets or sets the per-category log level map keyed by category name.
        /// </summary>
        public Dictionary<string, string>? Level { get; set; }
    }
}
