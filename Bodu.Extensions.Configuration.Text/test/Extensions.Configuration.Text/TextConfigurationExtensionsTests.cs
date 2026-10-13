// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Verifies the registration members of <see cref="TextConfigurationExtensions" />, member by member.
/// </summary>
[TestClass]
public sealed partial class TextConfigurationExtensionsTests
{
    /// <summary>The configuration text the tests load, which sets <c>logging:level:default</c>.</summary>
    private const string Sample = "logging.level.default = Information\n";
}
