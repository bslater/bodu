// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;
using System.Text;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="SDBM" /> hash algorithm.
    /// </summary>
    [TestClass]
    public abstract partial class SnefruTests<TTest, TAlgorithm>
        : Security.Cryptography.HashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
        where TTest : SnefruTests<TTest, TAlgorithm>, new()
        where TAlgorithm : Snefru<TAlgorithm>, new()
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            list.AddRange([
                "BlockSizeBytes"
            ]);

            return list;
        }

        private static readonly IReadOnlyDictionary<string, byte[]> CustomInputs = new Dictionary<string, byte[]>
        {
            ["a"] = Encoding.UTF8.GetBytes("a"),
            ["1234567890"] = Encoding.UTF8.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890")
        };

        protected override IEnumerable<KnownAnswerTest> GetTestVectors(SingleTestVariant variant)
        {
            foreach (var vector in base.GetTestVectors(variant))
                yield return vector;

            var expected = GetExpectedHashesForNamedInputs(variant);
            foreach (var (name, input) in CustomInputs)
            {
                if (expected.TryGetValue(name, out var hex))
                {
                    yield return new KnownAnswerTest
                    {
                        Name = name,
                        Input = input,
                        ExpectedOutput = Convert.FromHexString(hex)
                    };
                }
            }
        }
    }
}