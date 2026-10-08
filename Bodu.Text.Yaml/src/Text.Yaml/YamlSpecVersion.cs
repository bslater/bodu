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
/// boolean spellings (<c>yes</c>/<c>no</c>, <c>on</c>/<c>off</c>, <c>y</c>/<c>n</c>) and the YAML 1.1 int type's
/// integer forms: binary (<c>0b1010</c>), leading-zero octal (<c>0755</c>), decimal, and hexadecimal (<c>0x1F</c>).
/// Each integer form may carry a sign, so <c>-0755</c> is -493 and <c>-0x1F</c> is -31, and may group its digits with
/// underscores after the first digit, so <c>1_000</c> is 1000 while <c>_123</c> remains a string. The core schema's
/// <c>0o</c> octal form is also resolved, in the core schema's form, without a sign or digit groups. Floating-point
/// values group their digits with underscores too. The base-60 ("sexagesimal") numeric form of the historical 1.1
/// schema is deliberately not resolved under either version: a colon-separated plain scalar such as <c>22:22</c>
/// remains a string, avoiding the well-known footgun in which time-, port-, or address-like values were silently
/// reinterpreted as large numbers. The version affects only how plain scalars are typed; structural parsing of anchors,
/// aliases, tags, and collections is identical for both versions.
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
    /// booleans, and accepts the YAML 1.1 integer forms (leading-zero octal, binary, and hexadecimal, each with an
    /// optional sign) and underscore digit grouping after the first digit. Base-60 ("sexagesimal") numbers are
    /// deliberately not resolved; a colon-separated scalar such as <c>22:22</c> stays a string.
    /// </summary>
    V1_1 = 1,
}
