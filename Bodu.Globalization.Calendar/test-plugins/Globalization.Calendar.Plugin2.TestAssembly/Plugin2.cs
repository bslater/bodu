// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Plugin2.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugin2.TestAssembly;

/// <summary>
/// Test-only assembly that deliberately lacks a <c>[assembly: NotableDatePlugin(...)]</c> attribute. Used to verify
/// that <c>ExternalPluginLoader</c> surfaces the missing-attribute failure as a <c>PluginMissingAttributeException</c>.
/// </summary>
public sealed class NonPlugin
{
}
