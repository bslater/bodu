// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.TypedValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    /// Drives every <see cref="ConfigurationKatKind.TypedValue" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.TypedValueData),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void TypedValue_Kat(ConfigurationKat kat)
    {
        ConfigurationView view = BuildTypedValueView(kat);

        if (kat.Outcome is ConfigurationKatOutcome.Fail)
        {
            ExecuteTypedFail(kat, view);
            return;
        }

        ExecuteTypedPass(kat, view);
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
