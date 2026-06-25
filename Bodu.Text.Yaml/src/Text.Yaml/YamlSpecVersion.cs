// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSpecVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Identifies the version of the YAML specification whose implicit type resolution the reader applies. The version
/// controls how unquoted (plain) scalars are interpreted as booleans, null, and numbers.
/// </summary>
/// <remarks>
/// <para>
/// The default is <see cref="V1_2" />, whose core schema resolves only <c>true</c>/<c>false</c> as booleans and
/// <c>null</c>/<c>~</c>/the empty scalar as null. This avoids the well-known "Norway problem" in which YAML 1.1
/// silently converts unquoted tokens such as <c>no</c>, <c>yes</c>, <c>on</c>, and <c>off</c> to booleans.
/// </para>
/// <para>
/// Selecting <see cref="V1_1" /> opts in to the broader YAML 1.1 implicit typing rules, including the additional
/// boolean spellings (<c>yes</c>/<c>no</c>, <c>on</c>/<c>off</c>, <c>y</c>/<c>n</c>) and sexagesimal numbers. The
/// version affects only how plain scalars are typed; structural parsing of anchors, aliases, tags, and collections is
/// identical for both versions.
/// </para>
/// </remarks>
public enum YamlSpecVersion
{
    /// <summary>
    /// The YAML 1.2 core schema. Only <c>true</c> and <c>false</c> resolve to booleans, and an explicit tag is required
    /// to type tokens such as <c>yes</c> or <c>off</c>.
    /// </summary>
    V1_2 = 0,

    /// <summary>
    /// The YAML 1.1 schema. Additionally resolves <c>yes</c>/<c>no</c>, <c>on</c>/<c>off</c>, and <c>y</c>/<c>n</c> as
    /// booleans, and accepts sexagesimal integers and floats.
    /// </summary>
    V1_1 = 1,
}
