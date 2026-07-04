// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PublicApiSnapshot.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using System.Text;

namespace Bodu.Test;

/// <summary>
/// Produces a deterministic, human-readable fingerprint of an assembly's public surface and compares it against a
/// committed baseline, so that any addition, removal, or signature change to a public or protected member fails the
/// build until the baseline is deliberately regenerated.
/// </summary>
/// <remarks>
/// <para>
/// The fingerprint is intentionally a stable fingerprint rather than a faithful C# reproduction: it lists every exported
/// type and, under each, its declared public and protected members in a fixed order. It does not encode nullable
/// annotations, so a nullability-only change is not flagged; every other surface change — a new type, a new or removed
/// member, or a changed signature — is.
/// </para>
/// <para>
/// The baseline is a plain text file committed under the test project's <c>PublicApi</c> folder and copied to the test
/// output directory. Regenerate it by running the snapshot test with the <c>BODU_APISNAPSHOT_WRITE</c> environment
/// variable set to the source <c>PublicApi</c> directory; in that mode the current fingerprint is written to the
/// baseline and the assertion is skipped.
/// </para>
/// </remarks>
public static class PublicApiSnapshot
{
    /// <summary>The environment variable that, when set to a directory, switches the helper into baseline-write mode.</summary>
    private const string WriteEnvironmentVariable = "BODU_APISNAPSHOT_WRITE";

    /// <summary>
    /// Verifies that the public surface of <paramref name="assembly" /> matches the committed baseline
    /// <paramref name="baselineFileName" />, or regenerates the baseline when running in write mode.
    /// </summary>
    /// <param name="assembly">The assembly whose public surface is snapshotted.</param>
    /// <param name="baselineFileName">The baseline file name (for example <c>Bodu.Numerics.PublicApi.txt</c>).</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="assembly" /> is <see langword="null" />, or <paramref name="baselineFileName" /> is
    /// <see langword="null" />.
    /// </exception>
    public static void Verify(Assembly assembly, string baselineFileName)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(baselineFileName);

        string actual = Normalize(Generate(assembly));

        string? writeDirectory = Environment.GetEnvironmentVariable(WriteEnvironmentVariable);
        if (!string.IsNullOrEmpty(writeDirectory))
        {
            Directory.CreateDirectory(writeDirectory);
            File.WriteAllText(Path.Combine(writeDirectory, baselineFileName), actual);
            return;
        }

        string baselinePath = Path.Combine(AppContext.BaseDirectory, "PublicApi", baselineFileName);
        string expected = File.Exists(baselinePath) ? Normalize(File.ReadAllText(baselinePath)) : string.Empty;

        Assert.AreEqual(
            expected,
            actual,
            $"The public API of '{assembly.GetName().Name}' changed. If this is intentional, regenerate the baseline by "
            + $"running this test with {WriteEnvironmentVariable} set to the test project's 'PublicApi' source directory.");
    }

    /// <summary>
    /// Builds the public-surface fingerprint for <paramref name="assembly" />.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The multi-line fingerprint, one line per type header and one indented line per member.</returns>
    public static string Generate(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var builder = new StringBuilder();

        foreach (Type type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            builder.Append(TypeKind(type)).Append(' ').AppendLine(type.FullName);

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            IEnumerable<string> members = type.GetMembers(flags)
                .Where(IsVisibleApi)
                .Select(FormatMember)
                .OrderBy(line => line, StringComparer.Ordinal);

            foreach (string member in members)
                builder.Append("    ").AppendLine(member);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the declaration keyword describing <paramref name="type" />.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns>One of <c>enum</c>, <c>interface</c>, <c>struct</c>, <c>delegate</c>, or <c>class</c>.</returns>
    private static string TypeKind(Type type) =>
        type.IsEnum ? "enum"
        : type.IsInterface ? "interface"
        : typeof(Delegate).IsAssignableFrom(type) ? "delegate"
        : type.IsValueType ? "struct"
        : "class";

    /// <summary>
    /// Determines whether <paramref name="member" /> is part of the consumer-visible surface — a public or protected
    /// member that is not a compiler-generated property/event accessor.
    /// </summary>
    /// <param name="member">The member to test.</param>
    /// <returns><see langword="true" /> when the member belongs in the snapshot; otherwise <see langword="false" />.</returns>
    private static bool IsVisibleApi(MemberInfo member) =>
        member switch
        {
            MethodInfo method => IsVisibleMethod(method) && (!method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal)),
            ConstructorInfo ctor => ctor.IsPublic || ctor.IsFamily || ctor.IsFamilyOrAssembly,
            PropertyInfo property => (property.GetMethod is { } g && IsVisibleMethod(g)) || (property.SetMethod is { } s && IsVisibleMethod(s)),
            EventInfo evt => evt.AddMethod is { } a && IsVisibleMethod(a),
            FieldInfo field => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly,
            Type nested => nested.IsNestedPublic || nested.IsNestedFamily || nested.IsNestedFamORAssem,
            _ => false,
        };

    /// <summary>
    /// Determines whether <paramref name="method" /> is public or protected (family) and therefore consumer-visible.
    /// </summary>
    /// <param name="method">The method to test.</param>
    /// <returns><see langword="true" /> when the method is visible; otherwise <see langword="false" />.</returns>
    private static bool IsVisibleMethod(MethodBase method) =>
        method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    /// <summary>
    /// Formats <paramref name="member" /> as a single stable line prefixed by its member kind.
    /// </summary>
    /// <param name="member">The member to format.</param>
    /// <returns>The formatted member line.</returns>
    private static string FormatMember(MemberInfo member) =>
        string.Format(CultureInfo.InvariantCulture, "{0} {1}", member.MemberType.ToString().ToLowerInvariant(), member);

    /// <summary>
    /// Normalizes line endings and trailing whitespace so a baseline compares equal regardless of the checkout's
    /// newline style.
    /// </summary>
    /// <param name="value">The text to normalize.</param>
    /// <returns>The normalized text.</returns>
    private static string Normalize(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal).TrimEnd('\n');
}
