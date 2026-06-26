// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatCensus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Reflection;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Produces a normalized, sorted inventory of every <see cref="CryptoKnownAnswer" /> reachable from the test assembly —
/// a go-forward data-fidelity artifact that lets a future change diff the full set of known-answer inputs and outputs.
/// </summary>
/// <remarks>
/// <para>
/// Vectors reach the census through two complementary paths, unioned and de-duplicated:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Specification-carried.</b> Every concrete test class exposing a <c>GetSpecification</c> method whose result derives
/// from <see cref="AlgorithmSpecification{TKat}" /> is instantiated and queried; its <c>KnownAnswers</c> are read for each
/// algorithm variant. This captures the hash, block-cipher, and stream-cipher curated sets together with their variant
/// labels.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b><see cref="DynamicDataAttribute" />-carried.</b> Every test method's dynamic-data provider is invoked and each row
/// scanned for <see cref="CryptoKnownAnswer" /> elements. This captures families with no specification object (AEAD modes)
/// and the file-loaded / inline asymmetric corpora that flow directly into <c>[DynamicData]</c> sites.
/// </description>
/// </item>
/// </list>
/// </remarks>
public static class KatCensus
{
    private const char FieldSeparator = '|';
    private const string PairSeparator = "‖";

    /// <summary>
    /// The census header line, written first and excluded from the sorted body.
    /// </summary>
    public const string Header = "family|class|variant|name|key|input|output";

    /// <summary>
    /// Represents one normalized census row: a single known-answer vector projected to its identifying metadata and its
    /// keying material, primary input, and primary output rendered as lowercase hex (or a short token for boolean and
    /// rejection outcomes).
    /// </summary>
    /// <param name="Family">The KAT family, for example <c>digest</c>, <c>block-cipher</c>, or <c>signature</c>.</param>
    /// <param name="ClassName">The declaring test class.</param>
    /// <param name="Variant">The algorithm variant label, or an empty string when the family has no variant axis.</param>
    /// <param name="Name">The vector's <see cref="CryptoKnownAnswer.Name" />.</param>
    /// <param name="Key">The keying material, hex-encoded, or an empty string.</param>
    /// <param name="Input">The primary input, hex-encoded, or an empty string.</param>
    /// <param name="Output">The primary expected output, hex-encoded or a short outcome token.</param>
    public sealed record CensusEntry(
        string Family,
        string ClassName,
        string Variant,
        string Name,
        string Key,
        string Input,
        string Output)
    {
        /// <summary>
        /// Renders the entry as a single pipe-delimited line.
        /// </summary>
        /// <returns>The seven fields joined by <c>|</c>, with separators scrubbed from free-form text.</returns>
        public string ToLine() =>
            string.Join(
                FieldSeparator,
                Clean(Family), Clean(ClassName), Clean(Variant), Clean(Name), Key, Input, Output);

        private static string Clean(string value) =>
            value.Replace(FieldSeparator, '/').Replace('\r', ' ').Replace('\n', ' ');
    }

    /// <summary>
    /// Collects the full, sorted, de-duplicated census from the supplied test assembly.
    /// </summary>
    /// <param name="assembly">The test assembly to inventory.</param>
    /// <returns>The distinct census entries, ordered by their rendered line.</returns>
    public static IReadOnlyList<CensusEntry> Collect(Assembly assembly)
    {
        var entries = new List<CensusEntry>();
        entries.AddRange(CollectSpecCarried(assembly));
        entries.AddRange(CollectDynamicDataCarried(assembly));

        return entries
            .GroupBy(e => e.ToLine())
            .Select(g => g.First())
            .OrderBy(e => e.ToLine(), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Renders a collected census — the header followed by one line per entry — as a single newline-joined string.
    /// </summary>
    /// <param name="entries">The entries to render.</param>
    /// <returns>The complete census text, terminated by a trailing newline.</returns>
    public static string Render(IReadOnlyList<CensusEntry> entries)
    {
        var lines = new List<string>(entries.Count + 1) { Header };
        lines.AddRange(entries.Select(e => e.ToLine()));
        return string.Join('\n', lines) + '\n';
    }

    /// <summary>
    /// Enumerates the vectors carried on every <see cref="AlgorithmSpecification{TKat}" />-derived specification reachable
    /// from a concrete test class.
    /// </summary>
    /// <param name="assembly">The test assembly to scan.</param>
    /// <returns>One entry per specification-carried vector.</returns>
    private static IEnumerable<CensusEntry> CollectSpecCarried(Assembly assembly)
    {
        foreach (Type type in ConcreteTypes(assembly))
        {
            object? instance = TryCreate(type);
            if (instance is null)
                continue;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.Name == "GetSpecification" && !m.IsGenericMethod))
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length == 0)
                {
                    foreach (CensusEntry entry in ReadSpecification(type, method, instance, variant: null))
                        yield return entry;
                }
                else if (parameters.Length == 1)
                {
                    foreach (object variant in EnumerateVariants(type, instance, parameters[0].ParameterType))
                        foreach (CensusEntry entry in ReadSpecification(type, method, instance, variant, variantArg: variant))
                            yield return entry;
                }
            }
        }
    }

    /// <summary>
    /// Invokes one <c>GetSpecification</c> overload and projects the vectors it carries.
    /// </summary>
    /// <param name="type">The declaring test class.</param>
    /// <param name="method">The <c>GetSpecification</c> method to invoke.</param>
    /// <param name="instance">The test-class instance.</param>
    /// <param name="variant">The variant label, or <see langword="null" /> for a variant-free specification.</param>
    /// <param name="variantArg">The argument to pass to a single-parameter overload.</param>
    /// <returns>One entry per carried vector, or nothing when invocation fails or no vectors are carried.</returns>
    private static IEnumerable<CensusEntry> ReadSpecification(
        Type type, MethodInfo method, object instance, object? variant, object? variantArg = null)
    {
        object? spec;
        try
        {
            spec = method.Invoke(instance, variantArg is null ? null : [variantArg]);
        }
        catch
        {
            yield break;
        }

        if (spec is null)
            yield break;

        PropertyInfo? knownAnswers = spec.GetType().GetProperty("KnownAnswers");
        if (knownAnswers?.GetValue(spec) is not IEnumerable rows)
            yield break;

        string variantLabel = variant?.ToString() ?? string.Empty;
        foreach (object? row in rows)
            if (row is CryptoKnownAnswer kat)
                yield return Project(type.Name, variantLabel, kat);
    }

    /// <summary>
    /// Resolves the algorithm variants for a single-parameter <c>GetSpecification</c> overload, preferring an explicit
    /// <c>Get…Variants</c> enumerator and falling back to the enum values of the parameter type.
    /// </summary>
    /// <param name="type">The declaring test class.</param>
    /// <param name="instance">The test-class instance.</param>
    /// <param name="variantType">The variant parameter type.</param>
    /// <returns>The variants to query.</returns>
    private static IEnumerable<object> EnumerateVariants(Type type, object instance, Type variantType)
    {
        MethodInfo? enumerator = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name.StartsWith("Get", StringComparison.Ordinal)
                && m.Name.EndsWith("Variants", StringComparison.Ordinal)
                && m.GetParameters().Length == 0);

        if (enumerator is not null)
        {
            object? result = null;
            try
            {
                result = enumerator.Invoke(instance, null);
            }
            catch
            {
                // Fall through to enum values below.
            }

            if (result is IEnumerable items)
            {
                foreach (object? item in items)
                    if (item is not null)
                        yield return item;

                yield break;
            }
        }

        if (variantType.IsEnum)
            foreach (object value in Enum.GetValues(variantType))
                yield return value;
    }

    /// <summary>
    /// Enumerates the vectors that flow through every <see cref="DynamicDataAttribute" /> provider in the assembly.
    /// </summary>
    /// <param name="assembly">The test assembly to scan.</param>
    /// <returns>One entry per <see cref="CryptoKnownAnswer" /> found in any dynamic-data row.</returns>
    private static IEnumerable<CensusEntry> CollectDynamicDataCarried(Assembly assembly)
    {
        foreach (Type type in ConcreteTypes(assembly))
        {
            foreach (MethodInfo testMethod in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (DynamicDataAttribute attribute in testMethod.GetCustomAttributes<DynamicDataAttribute>())
                {
                    foreach (CensusEntry entry in ReadDynamicData(type, testMethod, attribute))
                        yield return entry;
                }
            }
        }
    }

    /// <summary>
    /// Resolves and invokes one dynamic-data provider, projecting any known-answer rows it yields.
    /// </summary>
    /// <param name="declaringType">The test class carrying the <see cref="DynamicDataAttribute" />.</param>
    /// <param name="testMethod">The decorated test method, supplying the resolution context for the provider member.</param>
    /// <param name="attribute">The attribute identifying the provider member.</param>
    /// <returns>One entry per <see cref="CryptoKnownAnswer" /> found, or nothing when the provider cannot be invoked.</returns>
    private static IEnumerable<CensusEntry> ReadDynamicData(Type declaringType, MethodInfo testMethod, DynamicDataAttribute attribute)
    {
        IEnumerable<object[]>? rows;
        try
        {
            rows = attribute.GetData(testMethod);
        }
        catch
        {
            yield break;
        }

        if (rows is null)
            yield break;

        // Materialize defensively: a lazy provider can throw mid-enumeration.
        List<object[]> materialized;
        try
        {
            materialized = rows.ToList();
        }
        catch
        {
            yield break;
        }

        foreach (object[] row in materialized)
            foreach (object? element in row)
                if (element is CryptoKnownAnswer kat)
                    yield return Project(declaringType.Name, string.Empty, kat);
    }

    /// <summary>
    /// Projects a single <see cref="CryptoKnownAnswer" /> to its normalized census entry.
    /// </summary>
    /// <param name="className">The declaring test class name.</param>
    /// <param name="variant">The variant label.</param>
    /// <param name="kat">The vector to project.</param>
    /// <returns>The normalized entry.</returns>
    private static CensusEntry Project(string className, string variant, CryptoKnownAnswer kat)
    {
        (string family, string key, string input, string output) = kat switch
        {
            MessageDigestKnownAnswer m =>
                ("digest", Hex(m.Key), Hex(m.Message), Hex(m.Digest)),
            BlockCipherKnownAnswer b =>
                ("block-cipher", Hex(b.Key) + TweakSuffix(b.Tweak), Hex(b.Plaintext), Hex(b.Ciphertext)),
            AeadKnownAnswer a =>
                ("aead", Hex(a.Key), Hex(a.Plaintext), Hex(a.CiphertextWithTag)),
            StreamCipherKnownAnswer s =>
                ("stream-cipher", Hex(s.Key), s.IsKeystream ? string.Empty : Hex(s.Plaintext), Hex(s.Ciphertext)),
            KdfKnownAnswer k =>
                ("kdf", string.Empty, Hex(k.Password), k.ExpectedHex.ToLowerInvariant()),
            KeyAgreementKnownAnswer g =>
                ("key-agreement", Hex(g.PrivateKey), Hex(g.PeerPublicKey),
                    g.ExpectRejection ? "<rejected>" : Hex(g.ExpectedSharedSecret)),
            SignatureKnownAnswer sig =>
                ("signature", Hex(sig.PublicKey), Hex(sig.Message),
                    Hex(sig.Signature) + (sig.ExpectedValid ? string.Empty : PairSeparator + "invalid")),
            KemKnownAnswer kem =>
                ("kem", Hex(kem.Key), Hex(kem.M), Hex(kem.SharedSecret)),
            KeyGenKnownAnswer kg =>
                ("key-generation", string.Empty, Hex(kg.Seed),
                    Hex(kg.ExpectedPublicKey) + PairSeparator + Hex(kg.ExpectedPrivateKey)),
            KeyValidationKnownAnswer kv =>
                ("key-validation", string.Empty, Hex(kv.Key), kv.ExpectedValid ? "valid" : "invalid"),
            _ => ProjectFallback(kat),
        };

        return new CensusEntry(family, className, variant, kat.Name, key, input, output);
    }

    /// <summary>
    /// Projects a vector type the primary switch does not recognize, using the type name as the family so a new leaf
    /// surfaces visibly rather than silently.
    /// </summary>
    /// <param name="kat">The unrecognized vector.</param>
    /// <returns>A best-effort family label with empty payload fields.</returns>
    private static (string Family, string Key, string Input, string Output) ProjectFallback(CryptoKnownAnswer kat)
    {
        string family = kat.GetType().Name
            .Replace("KnownAnswer", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return (family.Length == 0 ? "unknown" : family, string.Empty, string.Empty, string.Empty);
    }

    /// <summary>
    /// Renders a tweak as a hex suffix joined to the key, or an empty string when there is no tweak.
    /// </summary>
    /// <param name="tweak">The optional tweak.</param>
    /// <returns>The pair-separated hex suffix, or an empty string.</returns>
    private static string TweakSuffix(byte[]? tweak) =>
        tweak is null || tweak.Length == 0 ? string.Empty : PairSeparator + "tweak:" + Hex(tweak);

    /// <summary>
    /// Renders bytes as lowercase hex, treating <see langword="null" /> as the empty string.
    /// </summary>
    /// <param name="value">The bytes to render.</param>
    /// <returns>The lowercase hex encoding, or an empty string.</returns>
    private static string Hex(byte[]? value) =>
        value is null ? string.Empty : Convert.ToHexString(value).ToLowerInvariant();

    /// <summary>
    /// Enumerates the concrete, non-generic, public-default-constructible (where relevant) types of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The candidate types.</returns>
    private static IEnumerable<Type> ConcreteTypes(Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        return types.Where(t => t is { IsClass: true, IsAbstract: false }
            && !t.IsGenericTypeDefinition
            && !t.ContainsGenericParameters);
    }

    /// <summary>
    /// Attempts to instantiate a type through its public parameterless constructor.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <returns>The new instance, or <see langword="null" /> when the type has no usable constructor or construction fails.</returns>
    private static object? TryCreate(Type type)
    {
        if (type.GetConstructor(Type.EmptyTypes) is null)
            return null;

        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }
}
