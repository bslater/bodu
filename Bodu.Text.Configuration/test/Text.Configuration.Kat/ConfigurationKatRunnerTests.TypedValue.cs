// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.TypedValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Configuration.Test.Infrastructure;

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Kat;

public partial class ConfigurationKatRunnerTests
{
    /// <summary>Used by typed-value KAT TYPE-0007 / TYPE-1004 for enum parsing.</summary>
    private enum KatSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Verifies that a valid <see cref="ConfigurationKatKind.TypedValue" /> KAT returns the expected typed value.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.TypedValueDataPass),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TypedValue_WhenValid_ShouldReturnExpectedValue(ConfigurationKat kat)
    {
        ConfigurationView view = BuildTypedValueView(kat);

        ExecuteTypedPass(kat, view);
    }

    /// <summary>
    /// Verifies that an invalid <see cref="ConfigurationKatKind.TypedValue" /> KAT throws the expected exception.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.TypedValueDataFail),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TypedValue_WhenInvalid_ShouldThrowExpectedException(ConfigurationKat kat)
    {
        ConfigurationView view = BuildTypedValueView(kat);

        ExecuteTypedFail(kat, view);
    }

    private static ConfigurationView BuildTypedValueView(ConfigurationKat kat)
    {
        IniDocument doc = new();
        if (kat.RawValue is not null)
        {
            IniSection section = doc.GetOrAddSection("*");
            section.SetEntry(kat.Key!, kat.RawValue);
        }

        return doc.Resolve("any");
    }

    private static void ExecuteTypedPass(ConfigurationKat kat, ConfigurationView view)
    {
        string key = kat.Key!.Replace('.', ':');

        switch (kat.TypedAccessor)
        {
            case "Boolean":
                Assert.AreEqual(kat.ExpectedTypedValue, view.GetBoolean(key), $"{kat.Id}: GetBoolean.");
                break;

            case "Int32":
                Assert.AreEqual(kat.ExpectedTypedValue, view.GetInt32(key), $"{kat.Id}: GetInt32.");
                break;

            case "Int32WithFallback":
                Assert.AreEqual(kat.ExpectedTypedValue, view.GetInt32(key, 42), $"{kat.Id}: GetInt32 fallback.");
                break;

            case "Int64":
                Assert.AreEqual(kat.ExpectedTypedValue, view.GetInt64(key), $"{kat.Id}: GetInt64.");
                break;

            case "Enum":
                Assert.AreEqual(
                    Enum.Parse<KatSeverity>((string)kat.ExpectedTypedValue!),
                    view.GetEnum<KatSeverity>(key),
                    $"{kat.Id}: GetEnum.");
                break;

            default:
                Assert.Fail($"{kat.Id}: unsupported typed accessor '{kat.TypedAccessor}'.");
                break;
        }
    }

    private static void ExecuteTypedFail(ConfigurationKat kat, ConfigurationView view)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

        string key = kat.Key!.Replace('.', ':');

        AssertThrowsExactlyByName(kat.ExpectedException!, () =>
        {
            switch (kat.TypedAccessor)
            {
                case "Boolean":
                    _ = view.GetBoolean(key);
                    break;
                case "Int32":
                    _ = view.GetInt32(key);
                    break;
                case "Int32WithFallback":
                    _ = view.GetInt32(key, 42);
                    break;
                case "Int64":
                    _ = view.GetInt64(key);
                    break;
                case "Enum":
                    _ = view.GetEnum<KatSeverity>(key);
                    break;
                default:
                    throw new InvalidOperationException($"{kat.Id}: unsupported typed accessor '{kat.TypedAccessor}'.");
            }
        });
    }
}
