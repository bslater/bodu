// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiNamedPropertyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="MapiNamedProperty" />, the named-property identity.
/// </summary>
[TestClass]
public partial class MapiNamedPropertyTests
{
    /// <summary>The PS_PUBLIC_STRINGS property-set GUID used across the tests.</summary>
    internal static readonly Guid PublicStrings = new("00020329-0000-0000-C000-000000000046");
}
