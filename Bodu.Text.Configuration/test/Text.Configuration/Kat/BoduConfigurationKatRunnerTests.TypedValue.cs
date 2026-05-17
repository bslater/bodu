// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.TypedValue.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration.Kat;

public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>Used by typed-value KAT TYPE-0007 / TYPE-1004 for enum parsing.</summary>
    private enum KatSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Drives every <see cref="BoduConfigurationKatKind.TypedValue" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(BoduConfigurationKnownAnswerData.TypedValueData),
        typeof(BoduConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void TypedValue_Kat(BoduConfigurationKat kat)
    {
        BoduConfigurationView view = BuildTypedValueView(kat);

        if (kat.Outcome is BoduConfigurationKatOutcome.Fail)
        {
            ExecuteTypedFail(kat, view);
            return;
        }

        ExecuteTypedPass(kat, view);
    }

    private static BoduConfigurationView BuildTypedValueView(BoduConfigurationKat kat)
    {
        IniDocument doc = new();
        if (kat.RawValue is not null)
        {
            IniSection section = doc.GetOrAddSection("*");
            section.SetEntry(kat.Key!, kat.RawValue);
        }

        return doc.Resolve("any");
    }

    private static void ExecuteTypedPass(BoduConfigurationKat kat, BoduConfigurationView view)
    {
        var key = kat.Key!.Replace('.', ':');

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

    private static void ExecuteTypedFail(BoduConfigurationKat kat, BoduConfigurationView view)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

        var key = kat.Key!.Replace('.', ':');

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
